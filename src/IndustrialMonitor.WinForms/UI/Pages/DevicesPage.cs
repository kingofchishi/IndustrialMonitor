using System.ComponentModel;
using IndustrialMonitor.Core.Abstractions;
using IndustrialMonitor.Core.Models;
using IndustrialMonitor.WinForms.UI.Controls;
using IndustrialMonitor.WinForms.UI.Forms;

namespace IndustrialMonitor.WinForms.UI.Pages;

public sealed class DevicesPage : UserControl, IPage
{
    private readonly IDeviceRepository _devices;
    private readonly List<Device> _allDevices = new();
    private readonly BindingList<Device> _view = new();
    private readonly TextBox _search = new();
    private readonly DataGridView _grid = UiTheme.CreateGrid();
    private readonly Button _toggleButton = UiTheme.GhostButton("启用/停用", 100);

    public DevicesPage(IDeviceRepository devices)
    {
        _devices = devices;
        Dock = DockStyle.Fill;
        BackColor = UiTheme.Background;
        BuildUi();
    }

    public string Key => "devices";
    public string Title => "设备管理";
    public string Subtitle => "维护设备编号、量程、采样周期和启用状态";

    public async Task ActivateAsync()
    {
        await ReloadAsync();
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
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = UiTheme.Background,
            Padding = new Padding(0, 2, 0, 10)
        };

        var add = UiTheme.PrimaryButton("新增设备", 96);
        add.Click += async (_, _) => await AddAsync();
        var edit = UiTheme.GhostButton("编辑", 76);
        edit.Click += async (_, _) => await EditAsync();
        var delete = UiTheme.GhostButton("删除", 76);
        delete.Click += async (_, _) => await DeleteAsync();
        _toggleButton.Click += async (_, _) => await ToggleAsync();

        _search.Width = 220;
        _search.Height = 32;
        _search.PlaceholderText = "搜索编号 / 名称…";
        _search.Font = UiTheme.Body;
        _search.Margin = new Padding(16, 1, 8, 0);
        _search.TextChanged += (_, _) => ApplyFilter();

        toolbar.Controls.Add(add);
        toolbar.Controls.Add(edit);
        toolbar.Controls.Add(delete);
        toolbar.Controls.Add(_toggleButton);
        toolbar.Controls.Add(_search);

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
        UiTheme.AddTextColumn(_grid, "编号", nameof(Device.Code), 80);
        UiTheme.AddTextColumn(_grid, "名称", nameof(Device.Name), 120);
        UiTheme.AddTextColumn(_grid, "类型", nameof(Device.TypeText), 60);
        UiTheme.AddTextColumn(_grid, "单位", nameof(Device.Unit), 55);
        UiTheme.AddTextColumn(_grid, "报警范围", nameof(Device.RangeText), 110);
        UiTheme.AddTextColumn(_grid, "采样周期", nameof(Device.SamplingText), 80);
        UiTheme.AddTextColumn(_grid, "状态", nameof(Device.EnabledText), 55);
        UiTheme.AddTextColumn(_grid, "创建时间", nameof(Device.CreatedAtLocal), 105);
        _grid.DataSource = _view;
        _grid.CellDoubleClick += async (_, e) =>
        {
            if (e.RowIndex >= 0)
                await EditAsync();
        };
        _grid.SelectionChanged += (_, _) => UpdateToggleButton();
        _grid.CellFormatting += (_, e) =>
        {
            if (e.RowIndex < 0 || e.RowIndex >= _view.Count)
                return;

            if (e.ColumnIndex == 6)
            {
                e.CellStyle!.ForeColor = _view[e.RowIndex].IsEnabled
                    ? UiTheme.Accent
                    : UiTheme.Muted;
                e.CellStyle.Font = UiTheme.BodyBold;
            }
        };
    }

    private async Task ReloadAsync()
    {
        try
        {
            var devices = await _devices.GetAllAsync();
            _allDevices.Clear();
            _allDevices.AddRange(devices);
            ApplyFilter();
            UpdateToggleButton();
        }
        catch (Exception ex)
        {
            UiTheme.ShowError(FindForm()!, ex.Message);
        }
    }

    private void ApplyFilter()
    {
        var keyword = _search.Text.Trim();
        _view.RaiseListChangedEvents = false;
        try
        {
            _view.Clear();
            foreach (var device in _allDevices
                         .Where(d => string.IsNullOrEmpty(keyword)
                                     || d.Code.Contains(keyword, StringComparison.OrdinalIgnoreCase)
                                     || d.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                         .OrderByDescending(d => d.IsEnabled)
                         .ThenBy(d => d.Code))
            {
                _view.Add(device);
            }
        }
        finally
        {
            _view.RaiseListChangedEvents = true;
            _view.ResetBindings();
        }
    }

    private Device? SelectedDevice() =>
        _grid.CurrentRow?.DataBoundItem as Device;

    private async Task AddAsync()
    {
        using var dialog = new DeviceEditForm();
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
            return;

        try
        {
            dialog.Result.CreatedAtUtc = DateTime.UtcNow;
            await _devices.InsertAsync(dialog.Result);
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            UiTheme.ShowError(FindForm()!, ex.Message);
        }
    }

    private async Task EditAsync()
    {
        var selected = SelectedDevice();
        if (selected is null)
        {
            UiTheme.ShowInfo(FindForm()!, "请先选择一台设备。");
            return;
        }

        using var dialog = new DeviceEditForm(selected);
        if (dialog.ShowDialog(FindForm()) != DialogResult.OK)
            return;

        try
        {
            await _devices.UpdateAsync(dialog.Result);
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            UiTheme.ShowError(FindForm()!, ex.Message);
        }
    }

    private async Task DeleteAsync()
    {
        var selected = SelectedDevice();
        if (selected is null)
        {
            UiTheme.ShowInfo(FindForm()!, "请先选择一台设备。");
            return;
        }

        var result = MessageBox.Show(
            FindForm(),
            $"确定删除设备“{selected.Name} ({selected.Code})”吗？\n相关采样和报警记录也会被级联删除。",
            "确认删除",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (result != DialogResult.Yes)
            return;

        try
        {
            await _devices.DeleteAsync(selected.Id);
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            UiTheme.ShowError(FindForm()!, ex.Message);
        }
    }

    private async Task ToggleAsync()
    {
        var selected = SelectedDevice();
        if (selected is null)
        {
            UiTheme.ShowInfo(FindForm()!, "请先选择一台设备。");
            return;
        }

        try
        {
            var changed = selected.Clone();
            changed.IsEnabled = !changed.IsEnabled;
            await _devices.UpdateAsync(changed);
            await ReloadAsync();
        }
        catch (Exception ex)
        {
            UiTheme.ShowError(FindForm()!, ex.Message);
        }
    }

    private void UpdateToggleButton()
    {
        var selected = SelectedDevice();
        _toggleButton.Text = selected is null
            ? "启用/停用"
            : selected.IsEnabled ? "停用" : "启用";
    }
}
