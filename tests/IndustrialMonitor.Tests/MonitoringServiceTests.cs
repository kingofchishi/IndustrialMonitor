using IndustrialMonitor.Core.Abstractions;
using IndustrialMonitor.Core.Models;
using IndustrialMonitor.Core.Services;

namespace IndustrialMonitor.Tests;

public sealed class MonitoringServiceTests
{
    [Fact]
    public async Task ContinuousCritical_DoesNotInsertDuplicateAlarms()
    {
        var device = new Device
        {
            Id = 1,
            Code = "TEMP-01",
            Name = "炉温",
            Type = DeviceType.Temperature,
            Unit = "°C",
            LowLimit = 60,
            HighLimit = 90,
            SamplingIntervalMs = 200,
            IsEnabled = true
        };

        var deviceRepository = new FakeDeviceRepository(device);
        var telemetryRepository = new FakeTelemetryRepository();
        var alarmRepository = new FakeAlarmRepository();
        var settingsRepository = new FakeSettingsRepository();
        var client = new SequenceDeviceClient(95, 96, 97, 75, 98);

        var service = new MonitoringService(
            deviceRepository,
            telemetryRepository,
            alarmRepository,
            client,
            settingsRepository,
            new AppOptions());

        for (var i = 0; i < 5; i++)
        {
            await service.CollectOnceAsync();
            await Task.Delay(230);
        }

        Assert.Equal(2, alarmRepository.InsertCount);
        Assert.Equal(5, telemetryRepository.InsertedCount);
    }

    private sealed class FakeDeviceRepository : IDeviceRepository
    {
        private readonly Device _device;

        public FakeDeviceRepository(Device device) => _device = device;

        public Task<IReadOnlyList<Device>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Device>>(new[] { _device });

        public Task<Device?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
            => Task.FromResult<Device?>(_device);

        public Task<long> InsertAsync(Device device, CancellationToken cancellationToken = default)
            => Task.FromResult(device.Id);

        public Task UpdateAsync(Device device, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task DeleteAsync(long id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class FakeTelemetryRepository : ITelemetryRepository
    {
        public int InsertedCount { get; private set; }

        public Task InsertBatchAsync(
            IReadOnlyList<TelemetryRecord> records,
            CancellationToken cancellationToken = default)
        {
            InsertedCount += records.Count;
            return Task.CompletedTask;
        }

        public Task<PagedResult<TelemetryRecord>> QueryAsync(
            long? deviceId, DateTime? fromUtc, DateTime? toUtc,
            int page, int pageSize, CancellationToken cancellationToken = default)
            => Task.FromResult(new PagedResult<TelemetryRecord>(
                Array.Empty<TelemetryRecord>(), 0, page, pageSize));

        public Task<IReadOnlyList<TelemetryRecord>> GetRecentAsync(
            long deviceId, int count, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TelemetryRecord>>(Array.Empty<TelemetryRecord>());

        public Task<long> CountAsync(
            DateTime? fromUtc = null, CancellationToken cancellationToken = default)
            => Task.FromResult(0L);

        public Task<int> DeleteOlderThanAsync(
            DateTime cutoffUtc, CancellationToken cancellationToken = default)
            => Task.FromResult(0);
    }

    private sealed class FakeAlarmRepository : IAlarmRepository
    {
        public int InsertCount { get; private set; }

        public Task<long> InsertAsync(
            AlarmRecord alarm, CancellationToken cancellationToken = default)
            => Task.FromResult((long)++InsertCount);

        public Task<IReadOnlyList<AlarmRecord>> QueryAsync(
            AlarmQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<AlarmRecord>>(Array.Empty<AlarmRecord>());

        public Task<int> AcknowledgeAsync(
            IReadOnlyCollection<long> alarmIds, string operatorName,
            CancellationToken cancellationToken = default)
            => Task.FromResult(alarmIds.Count);

        public Task<int> CountUnacknowledgedAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(InsertCount);
    }

    private sealed class FakeSettingsRepository : ISettingsRepository
    {
        public Task<AppOptions> LoadAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(new AppOptions());

        public Task SaveAsync(AppOptions options, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class SequenceDeviceClient : IDeviceClient
    {
        private readonly Queue<double> _values;
        private readonly object _gate = new();

        public SequenceDeviceClient(params double[] values)
            => _values = new Queue<double>(values);

        public Task<DeviceReading> ReadAsync(
            Device device, CancellationToken cancellationToken = default)
        {
            lock (_gate)
            {
                var value = _values.Count > 0 ? _values.Dequeue() : 75;
                return Task.FromResult(new DeviceReading(
                    device.Id, value, "Good", DateTime.UtcNow));
            }
        }
    }
}
