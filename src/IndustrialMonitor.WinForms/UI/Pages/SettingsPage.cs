using System.Diagnostics;
using IndustrialMonitor.Core.Abstractions;
using IndustrialMonitor.Core.Models;
using IndustrialMonitor.WinForms.Infrastructure;
using IndustrialMonitor.WinForms.UI.Controls;

namespace IndustrialMonitor.WinForms.UI.Pages;

public sealed class SettingsPage : UserControl, IPage
{
    private readonly ISettingsRepository _settings;
    private readonly ITelemetryRepository _telemetry;
    private readonly AppOptions _options;
    private readonly AppPaths _paths;

    private readonly TextBox _operator = new();
    private readonly NumericUpDown _retention = new();
    private readonly Label _dbPath = UiTheme.Label(string.Empty, UiTheme.Small, UiTheme.Muted);
    private readonly Label _dbSize = UiTheme.Label(string.Empty, UiTheme.Small, UiTheme.Muted);
    private readonly Label _status = UiTheme.Label(string.Empty, UiTheme.Small, UiTheme.Muted);

    public SettingsPage(
        ISettingsRepository settings,
        ITelemetryRepository telemetry,
        AppOptions options,
        AppPaths paths)
    {
        _settings = settings;
        _telemetry = telemetry;
        _options = options;
        _paths = paths;

        Dock = DockStyle.Fill;
        BackColor = UiTheme.Background;
        BuildUi();
    }

    public string Key => "settings";
    public string Title => "系统设置";
    public string Subtitle => "操作员、数据保留策略与本地数据库信息";

    public async Task ActivateAsync()
    {
        try
        {
            var loaded = await _settings.LoadAsync();
            _options.OperatorName = loaded.OperatorName;
            _options.RetentionDays = loaded.RetentionDays;

            _operator.Text = _options.OperatorName;
            _retention.Value = Math.Clamp(_options.RetentionDays, 1, 3650);
            _dbPath.Text = _paths.DatabasePath;
            RefreshSize();
        }
        catch (Exception ex)
        {
            _status.Text = "设置加载失败：" + ex.Message;
        }
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = UiTheme.Background
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));

        var settingsCard = new CardPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 12, 0),
            Padding = new Padding(22)
        };

        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 7,
            BackColor = UiTheme.Surface
        };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
        form.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        form.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var title = UiTheme.Label("运行设置", UiTheme.Title);
        form.Controls.Add(title, 0, 0);
        form.SetColumnSpan(title, 2);

        form.Controls.Add(FieldLabel("操作员"), 0, 1);
        _operator.Dock = DockStyle.Fill;
        _operator.PlaceholderText = "确认报警时记录的操作员";
        _operator.MaxLength = 32;
        form.Controls.Add(_operator, 1, 1);

        form.Controls.Add(FieldLabel("数据保留（天）"), 0, 2);
        _retention.Minimum = 1;
        _retention.Maximum = 3650;
        _retention.Value = 30;
        _retention.Dock = DockStyle.Fill;
        _retention.ThousandsSeparator = true;
        form.Controls.Add(_retention, 1, 2);

        form.Controls.Add(FieldLabel("数据库类型"), 0, 3);
        form.Controls.Add(UiTheme.Label("SQLite（Microsoft.Data.Sqlite，本地单文件）", UiTheme.Body), 1, 3);

        form.Controls.Add(FieldLabel("数据库位置"), 0, 4);
        _dbPath.Dock = DockStyle.Fill;
        _dbPath.AutoSize = false;
        _dbPath.TextAlign = ContentAlignment.MiddleLeft;
        form.Controls.Add(_dbPath, 1, 4);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = UiTheme.Surface,
            Padding = new Padding(0, 8, 0, 0)
        };
        var save = UiTheme.PrimaryButton("保存设置", 100);
        save.Click += async (_, _) => await SaveAsync();
        var open = UiTheme.GhostButton("打开数据目录", 118);
        open.Click += (_, _) => OpenDataDirectory();
        var cleanup = UiTheme.GhostButton("清理过期数据", 118);
        cleanup.Click += async (_, _) => await CleanupAsync();
        actions.Controls.Add(save);
        actions.Controls.Add(open);
        actions.Controls.Add(cleanup);
        form.Controls.Add(actions, 0, 5);
        form.SetColumnSpan(actions, 2);

        _status.Dock = DockStyle.Fill;
        _status.AutoSize = false;
        form.Controls.Add(_status, 0, 6);
        form.SetColumnSpan(_status, 2);

        settingsCard.Controls.Add(form);

        var infoCard = new CardPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(22)
        };
        var info = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 8,
            BackColor = UiTheme.Surface
        };
        info.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
        info.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        info.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        info.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        info.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        info.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        info.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        info.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        info.Controls.Add(UiTheme.Label("关于系统", UiTheme.Title), 0, 0);
        info.Controls.Add(UiTheme.Label("工业设备监控与数据采集系统", UiTheme.BodyBold), 0, 1);
        info.Controls.Add(UiTheme.Label("版本：v1.0", UiTheme.Body), 0, 2);
        info.Controls.Add(UiTheme.Label("技术栈：.NET 8 / WinForms / SQLite", UiTheme.Body), 0, 3);
        info.Controls.Add(UiTheme.Label("架构：分层 + Repository + DI + 事件驱动", UiTheme.Body), 0, 4);
        info.Controls.Add(UiTheme.Label("采集：250ms 调度 + 设备独立周期", UiTheme.Body), 0, 5);
        info.Controls.Add(UiTheme.Label("报警：正常 ↔ 越限状态迁移去重", UiTheme.Body), 0, 6);

        _dbSize.Dock = DockStyle.Bottom;
        _dbSize.Height = 28;
        info.Controls.Add(_dbSize, 0, 7);
        infoCard.Controls.Add(info);

        root.Controls.Add(settingsCard, 0, 0);
        root.Controls.Add(infoCard, 1, 0);
        Controls.Add(root);
    }

    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_operator.Text))
        {
            UiTheme.ShowInfo(FindForm()!, "操作员不能为空。");
            return;
        }

        try
        {
            _options.OperatorName = _operator.Text.Trim();
            _options.RetentionDays = (int)_retention.Value;
            await _settings.SaveAsync(_options);
            _status.Text = $"设置已保存 {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            UiTheme.ShowError(FindForm()!, ex.Message);
        }
    }

    private async Task CleanupAsync()
    {
        var days = (int)_retention.Value;
        var cutoff = DateTime.UtcNow.AddDays(-days);
        var result = MessageBox.Show(
            FindForm(),
            $"将删除 {days} 天以前的采样记录，是否继续？",
            "清理历史数据",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (result != DialogResult.Yes)
            return;

        try
        {
            var deleted = await _telemetry.DeleteOlderThanAsync(cutoff);
            RefreshSize();
            _status.Text = $"已删除 {deleted:N0} 条过期采样记录";
        }
        catch (Exception ex)
        {
            UiTheme.ShowError(FindForm()!, ex.Message);
        }
    }

    private void OpenDataDirectory()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"\"{_paths.DataDirectory}\"",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            UiTheme.ShowError(FindForm()!, ex.Message);
        }
    }

    private void RefreshSize()
    {
        var bytes = _paths.GetDatabaseSizeBytes();
        _dbSize.Text = bytes > 0
            ? $"数据库大小：{FormatBytes(bytes)}（含 WAL）"
            : "数据库文件尚未创建";
    }

    private static Label FieldLabel(string text)
    {
        var label = UiTheme.Label(text, UiTheme.BodyBold);
        label.AutoSize = false;
        label.Dock = DockStyle.Fill;
        label.TextAlign = ContentAlignment.MiddleLeft;
        return label;
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{bytes / 1024d:0.0} KB";
        return $"{bytes / 1024d / 1024d:0.0} MB";
    }
}
