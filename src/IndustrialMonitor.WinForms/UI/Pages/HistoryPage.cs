using IndustrialMonitor.Core.Abstractions;
using IndustrialMonitor.Core.Models;
using IndustrialMonitor.WinForms.UI.Controls;

namespace IndustrialMonitor.WinForms.UI.Pages;

public sealed class HistoryPage : UserControl, IPage
{
    private readonly IDeviceRepository _devices;
    private readonly ITelemetryRepository _telemetry;
    private readonly ITelemetryCsvExporter _exporter;

    private readonly ComboBox _deviceCombo = new();
    private readonly DateTimePicker _from = new();
    private readonly DateTimePicker _to = new();
    private readonly NumericUpDown _pageSize = new();
    private readonly DataGridView _grid = UiTheme.CreateGrid();
    private readonly Label _pageInfo = UiTheme.Label(string.Empty, UiTheme.Small, UiTheme.Muted);
    private readonly Label _status = UiTheme.Label(string.Empty, UiTheme.Small, UiTheme.Muted);
    private readonly Button _previous = UiTheme.GhostButton("上一页", 76);
    private readonly Button _next = UiTheme.GhostButton("下一页", 76);

    private int _page = 1;
    private long _totalCount;
    private bool _loading;
    private bool _activatedOnce;

    public HistoryPage(
        IDeviceRepository devices,
        ITelemetryRepository telemetry,
        ITelemetryCsvExporter exporter)
    {
        _devices = devices;
        _telemetry = telemetry;
        _exporter = exporter;

        Dock = DockStyle.Fill;
        BackColor = UiTheme.Background;
        BuildUi();
    }

    public string Key => "history";
    public string Title => "历史数据";
    public string Subtitle => "按设备与时间范围查询采样记录，并导出 CSV";

    public async Task ActivateAsync()
    {
        if (!_activatedOnce)
        {
            _from.Value = DateTime.Now.Date.AddDays(-1);
            _to.Value = DateTime.Now;
            _activatedOnce = true;
        }

        await LoadDevicesAsync();
        await SearchAsync(1);
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = UiTheme.Background
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));

        var filter = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = UiTheme.Background,
            Padding = new Padding(0, 2, 0, 10)
        };

        filter.Controls.Add(FilterLabel("设备"));
        _deviceCombo.Width = 200;
        _deviceCombo.Height = 32;
        _deviceCombo.DropDownStyle = ComboBoxStyle.DropDownList;
        _deviceCombo.DisplayMember = nameof(Device.DisplayName);
        _deviceCombo.ValueMember = nameof(Device.Id);
        _deviceCombo.Margin = new Padding(0, 1, 12, 0);
        filter.Controls.Add(_deviceCombo);

        filter.Controls.Add(FilterLabel("从"));
        ConfigureDatePicker(_from, DateTime.Now.Date.AddDays(-1));
        filter.Controls.Add(_from);

        filter.Controls.Add(FilterLabel("到"));
        ConfigureDatePicker(_to, DateTime.Now);
        filter.Controls.Add(_to);

        filter.Controls.Add(FilterLabel("每页"));
        _pageSize.Minimum = 10;
        _pageSize.Maximum = 500;
        _pageSize.Increment = 10;
        _pageSize.Value = 50;
        _pageSize.Width = 70;
        _pageSize.Height = 32;
        _pageSize.Margin = new Padding(0, 1, 12, 0);
        filter.Controls.Add(_pageSize);

        var search = UiTheme.PrimaryButton("查询", 82);
        search.Click += async (_, _) => await SearchAsync(1);
        filter.Controls.Add(search);

        var export = UiTheme.GhostButton("导出 CSV", 100);
        export.Click += async (_, _) => await ExportAsync();
        filter.Controls.Add(export);

        _status.Margin = new Padding(12, 8, 0, 0);
        filter.Controls.Add(_status);

        var card = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14)
        };
        ConfigureGrid();
        card.Controls.Add(_grid);

        var pager = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = UiTheme.Background,
            Padding = new Padding(0, 10, 0, 0)
        };
        _previous.Click += async (_, _) =>
        {
            if (_page > 1)
                await SearchAsync(_page - 1);
        };
        _next.Click += async (_, _) =>
        {
            if (_page < Math.Max(1, (int)Math.Ceiling(_totalCount / (double)(int)_pageSize.Value)))
                await SearchAsync(_page + 1);
        };
        _pageInfo.Margin = new Padding(8, 8, 0, 0);
        pager.Controls.Add(_previous);
        pager.Controls.Add(_next);
        pager.Controls.Add(_pageInfo);

        root.Controls.Add(filter, 0, 0);
        root.Controls.Add(card, 0, 1);
        root.Controls.Add(pager, 0, 2);
        Controls.Add(root);
    }

    private void ConfigureGrid()
    {
        UiTheme.AddTextColumn(_grid, "时间", nameof(TelemetryRecord.CollectedAtLocal), 125);
        UiTheme.AddTextColumn(_grid, "设备", nameof(TelemetryRecord.DeviceName), 120);
        UiTheme.AddTextColumn(_grid, "编号", nameof(TelemetryRecord.DeviceCode), 90);
        UiTheme.AddTextColumn(_grid, "数值", nameof(TelemetryRecord.ValueText), 90);
        UiTheme.AddTextColumn(_grid, "质量", nameof(TelemetryRecord.Quality), 60);
        _grid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.ColumnIndex != 4)
                return;
            var quality = _grid.Rows[e.RowIndex].Cells[4].Value?.ToString();
            e.CellStyle!.ForeColor = quality == "Good" ? UiTheme.Accent : UiTheme.Danger;
        };
    }

    private async Task LoadDevicesAsync()
    {
        try
        {
            var selectedId = (_deviceCombo.SelectedItem as Device)?.Id;
            var devices = await _devices.GetAllAsync();
            var all = new List<Device>
            {
                new() { Id = 0, Code = "ALL", Name = "全部设备" }
            };
            all.AddRange(devices);

            _deviceCombo.DataSource = all;
            if (selectedId is not null)
            {
                _deviceCombo.SelectedItem = all.FirstOrDefault(d => d.Id == selectedId) ?? all[0];
            }
        }
        catch (Exception ex)
        {
            _status.Text = "设备加载失败：" + ex.Message;
        }
    }

    private async Task SearchAsync(int page)
    {
        if (_loading)
            return;

        if (_from.Value > _to.Value)
        {
            UiTheme.ShowInfo(FindForm()!, "开始时间不能晚于结束时间。");
            return;
        }

        _loading = true;
        _previous.Enabled = false;
        _next.Enabled = false;
        _status.Text = "查询中…";

        try
        {
            var selected = _deviceCombo.SelectedItem as Device;
            long? deviceId = selected is { Id: > 0 } ? selected.Id : null;
            var result = await _telemetry.QueryAsync(
                deviceId,
                _from.Value.ToUniversalTime(),
                _to.Value.ToUniversalTime(),
                page,
                (int)_pageSize.Value);

            _page = result.Page;
            _totalCount = result.TotalCount;
            _grid.DataSource = result.Items.ToList();
            _pageInfo.Text = $"第 {_page}/{result.TotalPages} 页 · 共 {_totalCount:N0} 条";
            _previous.Enabled = result.HasPrevious;
            _next.Enabled = result.HasNext;
            _status.Text = $"已加载 {result.Items.Count} 条";
        }
        catch (Exception ex)
        {
            _status.Text = "查询失败";
            UiTheme.ShowError(FindForm()!, ex.Message);
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task ExportAsync()
    {
        if (_from.Value > _to.Value)
        {
            UiTheme.ShowInfo(FindForm()!, "开始时间不能晚于结束时间。");
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "CSV 文件 (*.csv)|*.csv",
            FileName = $"采样数据_{DateTime.Now:yyyyMMdd_HHmm}.csv",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            AddExtension = true,
            DefaultExt = "csv"
        };
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
            return;

        try
        {
            var selected = _deviceCombo.SelectedItem as Device;
            long? deviceId = selected is { Id: > 0 } ? selected.Id : null;
            var result = await _telemetry.QueryAsync(
                deviceId,
                _from.Value.ToUniversalTime(),
                _to.Value.ToUniversalTime(),
                1,
                5000);

            if (result.TotalCount > 5000)
            {
                UiTheme.ShowInfo(FindForm()!, "当前条件超过 5000 条，只导出前 5000 条。请缩小时间范围。");
            }

            await _exporter.ExportAsync(dialog.FileName, result.Items.ToList());
            UiTheme.ShowInfo(FindForm()!, "导出完成。");
        }
        catch (Exception ex)
        {
            UiTheme.ShowError(FindForm()!, ex.Message);
        }
    }

    private static Label FilterLabel(string text)
    {
        var label = UiTheme.Label(text, UiTheme.Body, UiTheme.Muted);
        label.Margin = new Padding(0, 8, 6, 0);
        return label;
    }

    private static void ConfigureDatePicker(DateTimePicker picker, DateTime value)
    {
        picker.Format = DateTimePickerFormat.Custom;
        picker.CustomFormat = "yyyy-MM-dd HH:mm";
        picker.ShowUpDown = true;
        picker.Value = value;
        picker.Width = 166;
        picker.Font = UiTheme.Body;
        picker.Margin = new Padding(0, 1, 12, 0);
    }
}
