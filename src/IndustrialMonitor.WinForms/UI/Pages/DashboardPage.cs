using System.ComponentModel;
using IndustrialMonitor.Core.Abstractions;
using IndustrialMonitor.Core.Models;
using IndustrialMonitor.Core.Services;
using IndustrialMonitor.WinForms.Infrastructure;
using IndustrialMonitor.WinForms.UI.Controls;

namespace IndustrialMonitor.WinForms.UI.Pages;

public sealed class DashboardPage : UserControl, IPage
{
    private readonly IDeviceRepository _devices;
    private readonly ITelemetryRepository _telemetry;
    private readonly IAlarmRepository _alarms;
    private readonly AppPaths _paths;

    private readonly ComboBox _deviceCombo = new();
    private readonly KpiCard _onlineCard = new() { Caption = "在线设备", AccentColor = UiTheme.Accent };
    private readonly KpiCard _alarmCard = new() { Caption = "未确认报警", AccentColor = UiTheme.Danger };
    private readonly KpiCard _samplesCard = new() { Caption = "今日采样", AccentColor = UiTheme.Blue };
    private readonly KpiCard _dbCard = new() { Caption = "本地数据库", AccentColor = UiTheme.Warning };
    private readonly TrendChart _chart = new();
    private readonly Label _chartTitle = UiTheme.Label("设备趋势", UiTheme.BodyBold);
    private readonly Label _lastSampleLabel = UiTheme.Label("等待采集…", UiTheme.Small, UiTheme.Muted);
    private readonly Label _statusLabel = UiTheme.Label(string.Empty, UiTheme.Small, UiTheme.Muted);
    private readonly BindingList<AlarmRecord> _latestAlarms = new();

    private long? _selectedDeviceId;
    private long _todaySamples;
    private int _unacknowledged;
    private bool _loading;

    public DashboardPage(
        IDeviceRepository devices,
        ITelemetryRepository telemetry,
        IAlarmRepository alarms,
        AppPaths paths)
    {
        _devices = devices;
        _telemetry = telemetry;
        _alarms = alarms;
        _paths = paths;

        Dock = DockStyle.Fill;
        BackColor = UiTheme.Background;
        BuildUi();
    }

    public string Key => "dashboard";
    public string Title => "运行总览";
    public string Subtitle => "实时监控设备状态、采样趋势与最新报警";

    public async Task ActivateAsync()
    {
        await RefreshAsync();
    }

    public void HandleTelemetry(TelemetryBatchEventArgs e)
    {
        if (_selectedDeviceId is null)
            return;

        var selected = e.Readings
            .Where(r => r.DeviceId == _selectedDeviceId)
            .OrderBy(r => r.CollectedAtUtc)
            .ToList();

        if (selected.Count == 0)
            return;

        foreach (var reading in selected)
            _chart.Append(reading);

        _todaySamples += selected.Count;
        _samplesCard.SetValue(_todaySamples.ToString("N0"), "今日累计");
        _lastSampleLabel.Text = $"最后采样 {selected[^1].CollectedAtUtc.ToLocalTime():HH:mm:ss}";
    }

    public void HandleAlarm(AlarmRecord alarm)
    {
        _unacknowledged++;
        _alarmCard.SetValue(_unacknowledged.ToString("N0"), "需人工确认");

        _latestAlarms.Insert(0, alarm);
        while (_latestAlarms.Count > 8)
            _latestAlarms.RemoveAt(_latestAlarms.Count - 1);
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = UiTheme.Background,
            Padding = new Padding(0)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 116));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        root.Controls.Add(BuildFilterBar(), 0, 0);
        root.Controls.Add(BuildKpiRow(), 0, 1);
        root.Controls.Add(BuildContentRow(), 0, 2);
        Controls.Add(root);

        _deviceCombo.SelectedIndexChanged += async (_, _) =>
        {
            if (_loading)
                return;

            _selectedDeviceId = _deviceCombo.SelectedItem is Device device ? device.Id : null;
            await LoadChartAsync();
        };
    }

    private Control BuildFilterBar()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 4, 0, 8),
            BackColor = UiTheme.Background
        };

        panel.Controls.Add(PaddingLabel("监控设备", 4));
        _deviceCombo.Width = 220;
        _deviceCombo.Height = 32;
        _deviceCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _deviceCombo.DisplayMember = nameof(Device.DisplayName);
        _deviceCombo.Font = UiTheme.Body;
        _deviceCombo.Margin = new Padding(0, 0, 12, 0);
        panel.Controls.Add(_deviceCombo);

        var refresh = UiTheme.GhostButton("刷新数据", 100);
        refresh.Click += async (_, _) => await RefreshAsync();
        panel.Controls.Add(refresh);

        _statusLabel.Margin = new Padding(12, 8, 0, 0);
        panel.Controls.Add(_statusLabel);
        return panel;
    }

    private Control BuildKpiRow()
    {
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            RowCount = 1,
            BackColor = UiTheme.Background,
            Padding = new Padding(0, 0, 0, 12)
        };
        for (var i = 0; i < 4; i++)
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));

        table.Controls.Add(_onlineCard, 0, 0);
        table.Controls.Add(_alarmCard, 1, 0);
        table.Controls.Add(_samplesCard, 2, 0);
        table.Controls.Add(_dbCard, 3, 0);
        return table;
    }

    private Control BuildContentRow()
    {
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = UiTheme.Background
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 67));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33));

        var chartCard = new CardPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 12, 0),
            Padding = new Padding(18)
        };
        var chartLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = UiTheme.Surface
        };
        chartLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        chartLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var chartHeader = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface };
        _chartTitle.Location = new Point(0, 4);
        chartHeader.Controls.Add(_chartTitle);
        _lastSampleLabel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _lastSampleLabel.Location = new Point(430, 7);
        _lastSampleLabel.AutoSize = false;
        _lastSampleLabel.Width = 220;
        _lastSampleLabel.TextAlign = ContentAlignment.MiddleRight;
        chartHeader.Resize += (_, _) => _lastSampleLabel.Left = chartHeader.Width - _lastSampleLabel.Width;
        chartHeader.Controls.Add(_lastSampleLabel);

        chartLayout.Controls.Add(chartHeader, 0, 0);
        _chart.Dock = DockStyle.Fill;
        chartLayout.Controls.Add(_chart, 0, 1);
        chartCard.Controls.Add(chartLayout);

        var alarmCard = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18)
        };
        var alarmLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = UiTheme.Surface
        };
        alarmLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        alarmLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var alarmHeader = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface };
        alarmHeader.Controls.Add(UiTheme.Label("最新报警", UiTheme.BodyBold));
        alarmLayout.Controls.Add(alarmHeader, 0, 0);

        var grid = UiTheme.CreateGrid();
        UiTheme.AddTextColumn(grid, "时间", nameof(AlarmRecord.OccurredAtTime), 72);
        UiTheme.AddTextColumn(grid, "级别", nameof(AlarmRecord.LevelText), 60);
        UiTheme.AddTextColumn(grid, "信息", nameof(AlarmRecord.Message), 260);
        grid.DefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(4, 0, 4, 0);
        grid.CellFormatting += AlarmGridOnCellFormatting;
        grid.DataSource = _latestAlarms;
        alarmLayout.Controls.Add(grid, 0, 1);
        alarmCard.Controls.Add(alarmLayout);

        table.Controls.Add(chartCard, 0, 0);
        table.Controls.Add(alarmCard, 1, 0);
        return table;
    }

    private static Label PaddingLabel(string text, int top)
    {
        var label = UiTheme.Label(text, UiTheme.Body, UiTheme.Muted);
        label.Margin = new Padding(0, top, 8, 0);
        label.AutoSize = true;
        return label;
    }

    private async Task RefreshAsync()
    {
        if (_loading)
            return;

        _loading = true;
        try
        {
            var devices = await _devices.GetAllAsync();
            long? selectedId = _selectedDeviceId;
            if (selectedId is null && _deviceCombo.SelectedItem is Device current)
                selectedId = current.Id;
            selectedId ??= devices.FirstOrDefault()?.Id;

            _deviceCombo.DataSource = devices.ToList();
            if (devices.Count > 0)
            {
                var target = devices.FirstOrDefault(d => d.Id == selectedId) ?? devices[0];
                _deviceCombo.SelectedItem = target;
                _selectedDeviceId = target.Id;
            }
            else
            {
                _selectedDeviceId = null;
                _chart.Clear();
            }

            _onlineCard.SetValue(
                devices.Count(d => d.IsEnabled).ToString("N0"),
                $"共 {devices.Count} 台设备");

            _unacknowledged = await _alarms.CountUnacknowledgedAsync();
            _alarmCard.SetValue(_unacknowledged.ToString("N0"), "需人工确认");

            _todaySamples = await _telemetry.CountAsync(DateTime.Today.ToUniversalTime());
            _samplesCard.SetValue(_todaySamples.ToString("N0"), "今日累计");

            _dbCard.SetValue(FormatBytes(_paths.GetDatabaseSizeBytes()), "SQLite 本地库");

            await RefreshLatestAlarmsAsync();
            await LoadChartAsync();
            _statusLabel.Text = $"已刷新 {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "刷新失败：" + ex.Message;
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task LoadChartAsync()
    {
        if (_selectedDeviceId is not { } deviceId)
            return;

        var device = _deviceCombo.SelectedItem as Device;
        _chart.Unit = device?.Unit ?? string.Empty;
        _chartTitle.Text = device is null
            ? "设备趋势"
            : $"设备趋势 · {device.Name}";

        var records = await _telemetry.GetRecentAsync(deviceId, 80);
        _chart.SetSeries(records);
        if (records.Count > 0)
        {
            _lastSampleLabel.Text = $"最后采样 {records[0].CollectedAtUtc.ToLocalTime():HH:mm:ss}";
        }
    }

    private async Task RefreshLatestAlarmsAsync()
    {
        var alarms = await _alarms.QueryAsync(new AlarmQuery(Limit: 8));
        _latestAlarms.RaiseListChangedEvents = false;
        try
        {
            _latestAlarms.Clear();
            foreach (var alarm in alarms)
                _latestAlarms.Add(alarm);
        }
        finally
        {
            _latestAlarms.RaiseListChangedEvents = true;
            _latestAlarms.ResetBindings();
        }
    }

    private void AlarmGridOnCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _latestAlarms.Count)
            return;
        if (e.ColumnIndex != 1)
            return;

        e.CellStyle!.ForeColor = _latestAlarms[e.RowIndex].Level == AlarmLevel.Critical
            ? UiTheme.Danger
            : UiTheme.Warning;
        e.CellStyle.Font = UiTheme.BodyBold;
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024d:0.0} KB";
        return $"{bytes / 1024d / 1024d:0.0} MB";
    }
}
