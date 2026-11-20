using IndustrialMonitor.Core.Models;
using IndustrialMonitor.Core.Services;

namespace IndustrialMonitor.WinForms.UI.Forms;

public sealed class DeviceEditForm : Form
{
    private readonly Device _device;
    private readonly bool _isNew;

    private readonly TextBox _code = new();
    private readonly TextBox _name = new();
    private readonly ComboBox _type = new();
    private readonly TextBox _unit = new();
    private readonly NumericUpDown _low = new();
    private readonly NumericUpDown _high = new();
    private readonly NumericUpDown _interval = new();
    private readonly CheckBox _enabled = new();
    private readonly Label _error = UiTheme.Label(string.Empty, UiTheme.Small, UiTheme.Danger);

    public DeviceEditForm(Device? device = null)
    {
        _isNew = device is null;
        _device = device?.Clone() ?? new Device
        {
            Type = DeviceType.Temperature,
            Unit = DeviceType.Temperature.DefaultUnit(),
            LowLimit = DeviceType.Temperature.DefaultRange().Low,
            HighLimit = DeviceType.Temperature.DefaultRange().High,
            IsEnabled = true
        };

        BuildUi();
        BindDevice();
    }

    public Device Result => _device;

    private void BuildUi()
    {
        Text = _isNew ? "新增设备" : "编辑设备";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(540, 548);
        BackColor = UiTheme.Background;
        Font = UiTheme.Body;
        AutoScaleMode = AutoScaleMode.Dpi;

        var outer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = UiTheme.Background
        };
        outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        outer.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));

        var formTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(22, 18, 22, 8),
            ColumnCount = 2,
            RowCount = 9,
            BackColor = UiTheme.Background
        };
        formTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
        formTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (var i = 0; i < 8; i++)
            formTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        formTable.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));

        ConfigureTextBox(_code, "例如 TEMP-01", 32);
        ConfigureTextBox(_name, "例如 炉温监测", 64);
        ConfigureTextBox(_unit, "例如 °C", 16);

        _type.Dock = DockStyle.Fill;
        _type.DropDownStyle = ComboBoxStyle.DropDownList;
        _type.FormattingEnabled = true;
        _type.DataSource = Enum.GetValues<DeviceType>();
        _type.Format += (_, e) =>
        {
            if (e.ListItem is DeviceType type)
                e.Value = type.ToDisplayName();
        };
        _type.SelectedIndexChanged += (_, _) =>
        {
            if (_type.SelectedItem is not DeviceType type)
                return;

            if (_isNew || string.IsNullOrWhiteSpace(_unit.Text))
            {
                _unit.Text = type.DefaultUnit();
                var range = type.DefaultRange();
                _low.Value = (decimal)range.Low;
                _high.Value = (decimal)range.High;
            }
        };

        ConfigureNumber(_low, -1_000_000, 1_000_000, 0.1m, 2);
        ConfigureNumber(_high, -1_000_000, 1_000_000, 0.1m, 2);
        ConfigureNumber(_interval, 200, 60_000, 100, 0);
        _interval.ThousandsSeparator = true;

        _enabled.Text = "启用该设备（参与后台采集）";
        _enabled.AutoSize = true;
        _enabled.Dock = DockStyle.Fill;
        _enabled.Padding = new Padding(0, 12, 0, 0);

        AddRow(formTable, 0, "设备编号 *", _code);
        AddRow(formTable, 1, "设备名称 *", _name);
        AddRow(formTable, 2, "设备类型", _type);
        AddRow(formTable, 3, "单位 *", _unit);
        AddRow(formTable, 4, "下限 *", _low);
        AddRow(formTable, 5, "上限 *", _high);
        AddRow(formTable, 6, "采样周期 *", _interval);
        AddRow(formTable, 7, "启用状态", _enabled);

        _error.Dock = DockStyle.Fill;
        _error.AutoSize = false;
        _error.TextAlign = ContentAlignment.MiddleLeft;
        formTable.Controls.Add(_error, 0, 8);
        formTable.SetColumnSpan(_error, 2);

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(22, 12, 22, 12),
            BackColor = UiTheme.Surface
        };
        var cancel = UiTheme.GhostButton("取消", 88);
        cancel.DialogResult = DialogResult.Cancel;
        var save = UiTheme.PrimaryButton("保存", 96);
        save.Click += (_, _) => SaveAndClose();
        footer.Controls.Add(cancel);
        footer.Controls.Add(save);

        outer.Controls.Add(formTable, 0, 0);
        outer.Controls.Add(footer, 0, 1);
        Controls.Add(outer);
        AcceptButton = save;
        CancelButton = cancel;
    }

    private void BindDevice()
    {
        _code.Text = _device.Code;
        _name.Text = _device.Name;
        _type.SelectedItem = _device.Type;
        _unit.Text = _device.Unit;
        _low.Value = Math.Clamp((decimal)_device.LowLimit, _low.Minimum, _low.Maximum);
        _high.Value = Math.Clamp((decimal)_device.HighLimit, _high.Minimum, _high.Maximum);
        _interval.Value = Math.Clamp(_device.SamplingIntervalMs, 200, 60_000);
        _enabled.Checked = _device.IsEnabled;
    }

    private void SaveAndClose()
    {
        _device.Code = _code.Text.Trim();
        _device.Name = _name.Text.Trim();
        _device.Type = (DeviceType)(_type.SelectedItem ?? DeviceType.Temperature);
        _device.Unit = _unit.Text.Trim();
        _device.LowLimit = (double)_low.Value;
        _device.HighLimit = (double)_high.Value;
        _device.SamplingIntervalMs = (int)_interval.Value;
        _device.IsEnabled = _enabled.Checked;

        var errors = DeviceValidator.Validate(_device);
        if (errors.Count > 0)
        {
            _error.Text = errors[0];
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private static void ConfigureTextBox(TextBox textBox, string placeholder, int maxLength)
    {
        textBox.Dock = DockStyle.Fill;
        textBox.PlaceholderText = placeholder;
        textBox.MaxLength = maxLength;
    }

    private static void ConfigureNumber(
        NumericUpDown control,
        decimal min,
        decimal max,
        decimal increment,
        int decimals)
    {
        control.Dock = DockStyle.Fill;
        control.Minimum = min;
        control.Maximum = max;
        control.Increment = increment;
        control.DecimalPlaces = decimals;
        control.ThousandsSeparator = true;
    }

    private static void AddRow(
        TableLayoutPanel table,
        int row,
        string label,
        Control control)
    {
        var caption = UiTheme.Label(label, UiTheme.BodyBold);
        caption.AutoSize = false;
        caption.Dock = DockStyle.Fill;
        caption.TextAlign = ContentAlignment.MiddleLeft;

        table.Controls.Add(caption, 0, row);
        table.Controls.Add(control, 1, row);
    }
}
