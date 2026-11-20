using IndustrialMonitor.Core.Abstractions;
using IndustrialMonitor.Core.Models;
using IndustrialMonitor.WinForms.UI.Controls;

namespace IndustrialMonitor.WinForms.UI.Pages;

public sealed class AlarmsPage : UserControl, IPage
{
    private readonly IAlarmRepository _alarms;
    private readonly AppOptions _options;

    private readonly ComboBox _status = new();
    private readonly ComboBox _level = new();
    private readonly DataGridView _grid = UiTheme.CreateGrid();
    private readonly Label _summary = UiTheme.Label(string.Empty, UiTheme.Small, UiTheme.Muted);
    private bool _loading;

    public AlarmsPage(IAlarmRepository alarms, AppOptions options)
    {
        _alarms = alarms;
        _options = options;

        Dock = DockStyle.Fill;
        BackColor = UiTheme.Background;
        BuildUi();
    }

    public string Key => "alarms";
    public string Title => "报警中心";
    public string Subtitle => "查看越限报警，记录确认人和确认时间";

    public async Task ActivateAsync()
    {
        await RefreshAsync();
    }

    public void HandleAlarm(AlarmRecord alarm)
    {
        if (!Visible || _loading)
            return;

        _ = RefreshAsync();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = UiTheme.Background
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = UiTheme.Background,
            Padding = new Padding(0, 2, 0, 10)
        };

        toolbar.Controls.Add(FilterLabel("状态"));
        _status.Width = 110;
        _status.Height = 32;
        _status.DropDownStyle = ComboBoxStyle.DropDownList;
        _status.Items.AddRange(new object[] { "全部", "未确认", "已确认" });
        _status.SelectedIndex = 1;
        _status.Margin = new Padding(0, 1, 12, 0);
        toolbar.Controls.Add(_status);

        toolbar.Controls.Add(FilterLabel("级别"));
        _level.Width = 110;
        _level.Height = 32;
        _level.DropDownStyle = ComboBoxStyle.DropDownList;
        _level.Items.AddRange(new object[] { "全部", "警告", "严重" });
        _level.SelectedIndex = 0;
        _level.Margin = new Padding(0, 1, 12, 0);
        toolbar.Controls.Add(_level);

        var refresh = UiTheme.GhostButton("刷新", 76);
        refresh.Click += async (_, _) => await RefreshAsync();
        toolbar.Controls.Add(refresh);

        var acknowledge = UiTheme.PrimaryButton("确认选中", 100);
        acknowledge.Click += async (_, _) => await AcknowledgeAsync();
        toolbar.Controls.Add(acknowledge);

        _summary.Margin = new Padding(12, 8, 0, 0);
        toolbar.Controls.Add(_summary);

        var card = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14)
        };
        ConfigureGrid();
        card.Controls.Add(_grid);

        root.Controls.Add(toolbar, 0, 0);
        root.Controls.Add(card, 0, 1);
        Controls.Add(root);
    }

    private void ConfigureGrid()
    {
        _grid.MultiSelect = true;
        UiTheme.AddTextColumn(_grid, "时间", nameof(AlarmRecord.OccurredAtLocal), 120);
        UiTheme.AddTextColumn(_grid, "设备", nameof(AlarmRecord.DeviceName), 100);
        UiTheme.AddTextColumn(_grid, "级别", nameof(AlarmRecord.LevelText), 50);
        UiTheme.AddTextColumn(_grid, "报警信息", nameof(AlarmRecord.Message), 260);
        UiTheme.AddTextColumn(_grid, "数值", nameof(AlarmRecord.ValueText), 75);
        UiTheme.AddTextColumn(_grid, "确认状态", nameof(AlarmRecord.AckText), 140);

        _grid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.RowIndex >= _grid.Rows.Count)
                return;
            if (e.ColumnIndex != 2)
                return;

            var level = (_grid.Rows[e.RowIndex].DataBoundItem as AlarmRecord)?.Level;
            e.CellStyle!.ForeColor = level == AlarmLevel.Critical
                ? UiTheme.Danger
                : UiTheme.Warning;
            e.CellStyle.Font = UiTheme.BodyBold;
        };
    }

    private async Task RefreshAsync()
    {
        if (_loading)
            return;

        _loading = true;
        _summary.Text = "加载中…";
        try
        {
            var query = new AlarmQuery(
                Acknowledged: _status.SelectedIndex switch
                {
                    1 => false,
                    2 => true,
                    _ => null
                },
                Level: _level.SelectedIndex switch
                {
                    1 => AlarmLevel.Warning,
                    2 => AlarmLevel.Critical,
                    _ => null
                },
                Limit: 500);

            var alarms = await _alarms.QueryAsync(query);
            _grid.DataSource = alarms.ToList();
            _summary.Text = $"显示 {alarms.Count} 条（最多 500 条）";
        }
        catch (Exception ex)
        {
            _summary.Text = "加载失败";
            UiTheme.ShowError(FindForm()!, ex.Message);
        }
        finally
        {
            _loading = false;
        }
    }

    private async Task AcknowledgeAsync()
    {
        var ids = _grid.SelectedRows
            .Cast<DataGridViewRow>()
            .Select(row => row.DataBoundItem as AlarmRecord)
            .Where(alarm => alarm is { IsAcknowledged: false })
            .Select(alarm => alarm!.Id)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
        {
            UiTheme.ShowInfo(FindForm()!, "请选择未确认的报警记录。");
            return;
        }

        try
        {
            var count = await _alarms.AcknowledgeAsync(ids, _options.OperatorName);
            await RefreshAsync();
            UiTheme.ShowInfo(FindForm()!, $"已确认 {count} 条报警。");
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
}
