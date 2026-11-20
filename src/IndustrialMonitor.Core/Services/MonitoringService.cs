using IndustrialMonitor.Core.Abstractions;
using IndustrialMonitor.Core.Models;

namespace IndustrialMonitor.Core.Services;

/// <summary>
/// 采集调度器：
/// 1. 按设备自己的采样周期轮询 IDeviceClient；
/// 2. 批量写入采样记录；
/// 3. 只在“正常 → 越限”或“越限等级变化”时生成新报警，避免每个周期刷屏；
/// 4. 通过事件通知 UI，UI 负责切回主线程。
/// </summary>
public sealed class MonitoringService : IAsyncDisposable
{
    private const int SchedulerTickMs = 250;
    private const int MinSamplingMs = 200;
    private const int MaxSamplingMs = 60_000;

    private readonly IDeviceRepository _devices;
    private readonly ITelemetryRepository _telemetry;
    private readonly IAlarmRepository _alarms;
    private readonly IDeviceClient _deviceClient;
    private readonly ISettingsRepository _settings;
    private readonly AppOptions _options;

    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    private readonly SemaphoreSlim _collectGate = new(1, 1);
    private readonly Dictionary<long, DateTime> _lastSampleAt = new();
    private readonly Dictionary<long, AlarmLevel?> _lastAlarmLevel = new();

    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private MonitoringState _lastState = MonitoringState.Stopped;
    private string? _lastMessage;

    public MonitoringService(
        IDeviceRepository devices,
        ITelemetryRepository telemetry,
        IAlarmRepository alarms,
        IDeviceClient deviceClient,
        ISettingsRepository settings,
        AppOptions options)
    {
        _devices = devices;
        _telemetry = telemetry;
        _alarms = alarms;
        _deviceClient = deviceClient;
        _settings = settings;
        _options = options;
    }

    public event EventHandler<TelemetryBatchEventArgs>? TelemetryBatchCompleted;
    public event EventHandler<AlarmRaisedEventArgs>? AlarmRaised;
    public event EventHandler<MonitoringStatusEventArgs>? StatusChanged;

    public bool IsRunning => _loopTask is { IsCompleted: false };
    public DateTime? LastCycleAtUtc { get; private set; }
    public string? LastError { get; private set; }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleGate.WaitAsync(cancellationToken);
        try
        {
            if (IsRunning)
                return;

            var loaded = await _settings.LoadAsync(cancellationToken);
            _options.OperatorName = loaded.OperatorName;
            _options.RetentionDays = loaded.RetentionDays;

            _cts = new CancellationTokenSource();
            _loopTask = Task.Run(() => RunAsync(_cts.Token));
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async Task StopAsync()
    {
        await _lifecycleGate.WaitAsync();
        try
        {
            if (_cts is null)
                return;

            _cts.Cancel();

            if (_loopTask is not null)
            {
                try
                {
                    await _loopTask;
                }
                catch (OperationCanceledException)
                {
                    // 正常停止
                }
            }

            _cts.Dispose();
            _cts = null;
            _loopTask = null;
            _lastSampleAt.Clear();
            _lastAlarmLevel.Clear();
            LastError = null;
            RaiseStatus(MonitoringState.Stopped, "采集已停止");
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    /// <summary>
    /// 立即执行一次调度检查。后台循环和“立即采集”按钮都走这里，避免重入。
    /// </summary>
    public async Task CollectOnceAsync(CancellationToken cancellationToken = default)
    {
        await _collectGate.WaitAsync(cancellationToken);
        try
        {
            var now = DateTime.UtcNow;
            var devices = await _devices.GetAllAsync(cancellationToken);
            var readings = new List<TelemetryRecord>();
            var newAlarms = new List<AlarmRecord>();

            var activeIds = devices.Select(d => d.Id).ToHashSet();
            foreach (var staleId in _lastSampleAt.Keys.Where(id => !activeIds.Contains(id)).ToArray())
                _lastSampleAt.Remove(staleId);
            foreach (var staleId in _lastAlarmLevel.Keys.Where(id => !activeIds.Contains(id)).ToArray())
                _lastAlarmLevel.Remove(staleId);

            foreach (var device in devices.Where(d => d.IsEnabled))
            {
                var interval = TimeSpan.FromMilliseconds(
                    Math.Clamp(device.SamplingIntervalMs, MinSamplingMs, MaxSamplingMs));

                if (_lastSampleAt.TryGetValue(device.Id, out var lastSample)
                    && now - lastSample < interval)
                {
                    continue;
                }

                _lastSampleAt[device.Id] = now;
                var reading = await _deviceClient.ReadAsync(device, cancellationToken);

                readings.Add(new TelemetryRecord
                {
                    DeviceId = device.Id,
                    DeviceCode = device.Code,
                    DeviceName = device.Name,
                    Unit = device.Unit,
                    Value = reading.Value,
                    Quality = reading.Quality,
                    CollectedAtUtc = reading.CollectedAtUtc
                });

                var evaluation = AlarmEvaluator.Evaluate(device, reading);
                _lastAlarmLevel.TryGetValue(device.Id, out var previousLevel);

                if (evaluation.Level is { } level)
                {
                    if (previousLevel != level)
                    {
                        newAlarms.Add(new AlarmRecord
                        {
                            DeviceId = device.Id,
                            DeviceCode = device.Code,
                            DeviceName = device.Name,
                            Level = level,
                            Message = evaluation.Message ?? "设备越限",
                            Value = reading.Value,
                            Unit = device.Unit,
                            OccurredAtUtc = reading.CollectedAtUtc
                        });
                        _lastAlarmLevel[device.Id] = level;
                    }
                }
                else
                {
                    _lastAlarmLevel[device.Id] = null;
                }
            }

            if (readings.Count > 0)
                await _telemetry.InsertBatchAsync(readings, cancellationToken);

            foreach (var alarm in newAlarms)
            {
                alarm.Id = await _alarms.InsertAsync(alarm, cancellationToken);
            }

            LastCycleAtUtc = now;
            LastError = null;

            if (readings.Count > 0)
                TelemetryBatchCompleted?.Invoke(this, new TelemetryBatchEventArgs(readings));

            foreach (var alarm in newAlarms)
                AlarmRaised?.Invoke(this, new AlarmRaisedEventArgs(alarm));

            RaiseStatus(MonitoringState.Running, null);
        }
        finally
        {
            _collectGate.Release();
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        RaiseStatus(MonitoringState.Running, null);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await CollectOnceAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                RaiseStatus(MonitoringState.Error, ex.Message);
            }

            try
            {
                await Task.Delay(SchedulerTickMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void RaiseStatus(MonitoringState state, string? message)
    {
        if (_lastState == state && _lastMessage == message)
            return;

        _lastState = state;
        _lastMessage = message;
        StatusChanged?.Invoke(
            this,
            new MonitoringStatusEventArgs(state, message, DateTime.UtcNow));
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _lifecycleGate.Dispose();
        _collectGate.Dispose();
    }
}
