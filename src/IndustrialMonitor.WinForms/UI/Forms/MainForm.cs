using IndustrialMonitor.Core.Services;
using IndustrialMonitor.WinForms.Infrastructure;
using IndustrialMonitor.WinForms.UI.Pages;

namespace IndustrialMonitor.WinForms.UI.Forms;

public sealed class MainForm : Form
{
    private readonly MonitoringService _monitor;
    private readonly AppPaths _paths;

    private readonly Dictionary<string, (Button Button, IPage Page)> _pages = new();
    private readonly Dictionary<string, Button> _navButtons = new();
    private readonly Panel _content = new();
    private readonly Label _pageTitle = UiTheme.Label(string.Empty, UiTheme.PageTitle);
    private readonly Label _pageSubtitle = UiTheme.Label(string.Empty, UiTheme.Small, UiTheme.Muted);
    private readonly Label _monitorPill = new();
    private readonly Label _clock = UiTheme.Label(string.Empty, UiTheme.Small, UiTheme.Muted);
    private readonly Button _toggleMonitor = UiTheme.PrimaryButton("停止采集", 104);
    private readonly ToolStripStatusLabel _statusDb;
    private readonly ToolStripStatusLabel _statusMonitor;
    private readonly ToolStripStatusLabel _statusLast;
    private readonly System.Windows.Forms.Timer _clockTimer = new() { Interval = 1000 };

    private bool _allowClose;
    private string _activeKey = "dashboard";

    public MainForm(
        DashboardPage dashboard,
        DevicesPage devices,
        HistoryPage history,
        AlarmsPage alarms,
        SettingsPage settings,
        MonitoringService monitor,
        AppPaths paths)
    {
        _monitor = monitor;
        _paths = paths;
        _statusDb = new ToolStripStatusLabel
        {
            Text = $"SQLite：{_paths.DatabasePath}",
            Spring = true,
            TextAlign = ContentAlignment.MiddleLeft
        };
        _statusMonitor = new ToolStripStatusLabel("采集：启动中");
        _statusLast = new ToolStripStatusLabel("最后采样：-");

        Text = "工业设备监控系统 · Industrial Monitor";
        StartPosition = FormStartPosition.CenterScreen;
        WindowState = FormWindowState.Normal;
        ClientSize = new Size(1500, 860);
        MinimumSize = new Size(1180, 760);
        BackColor = UiTheme.Background;
        Font = UiTheme.Body;
        AutoScaleMode = AutoScaleMode.Dpi;

        BuildUi(dashboard, devices, history, alarms, settings);
        WireMonitoring();
        WireClock();
    }

    private void BuildUi(
        DashboardPage dashboard,
        DevicesPage devices,
        HistoryPage history,
        AlarmsPage alarms,
        SettingsPage settings)
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            BackColor = UiTheme.Background
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 236));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 96));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

        var sidebar = BuildSidebar();
        root.Controls.Add(sidebar, 0, 0);
        root.SetRowSpan(sidebar, 3);

        var header = BuildHeader();
        root.Controls.Add(header, 1, 0);

        _content.Dock = DockStyle.Fill;
        _content.BackColor = UiTheme.Background;
        _content.Padding = new Padding(22, 8, 22, 10);
        root.Controls.Add(_content, 1, 1);

        var status = BuildStatusStrip();
        root.Controls.Add(status, 1, 2);
        Controls.Add(root);

        AddPage(dashboard);
        AddPage(devices);
        AddPage(history);
        AddPage(alarms);
        AddPage(settings);

        foreach (var page in _pages.Values)
        {
            var control = (Control)page.Page;
            control.Visible = false;
            control.Dock = DockStyle.Fill;
            _content.Controls.Add(control);
        }
    }

    private Control BuildSidebar()
    {
        var sidebar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = UiTheme.Sidebar,
            Padding = new Padding(14, 18, 14, 16)
        };
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 286));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        sidebar.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));

        var brand = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Sidebar };
        var mark = new Label
        {
            Text = "IM",
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = UiTheme.Accent,
            ForeColor = Color.White,
            Font = new Font("Microsoft YaHei UI", 12F, FontStyle.Bold),
            Size = new Size(42, 42),
            Location = new Point(4, 4)
        };
        var brandTitle = new Label
        {
            Text = "工业监控",
            ForeColor = Color.White,
            Font = new Font("Microsoft YaHei UI", 11F, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(56, 8),
            BackColor = Color.Transparent
        };
        var brandSub = new Label
        {
            Text = "INDUSTRIAL MONITOR",
            ForeColor = Color.FromArgb(148, 163, 184),
            Font = new Font("Microsoft YaHei UI", 6.5F, FontStyle.Regular),
            AutoSize = true,
            Location = new Point(57, 31),
            BackColor = Color.Transparent
        };
        brand.Controls.Add(mark);
        brand.Controls.Add(brandTitle);
        brand.Controls.Add(brandSub);

        var nav = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = UiTheme.Sidebar,
            Padding = new Padding(0, 4, 0, 0),
            AutoScroll = false
        };
        nav.Controls.Add(CreateNavButton("dashboard", "运行总览"));
        nav.Controls.Add(CreateNavButton("devices", "设备管理"));
        nav.Controls.Add(CreateNavButton("history", "历史数据"));
        nav.Controls.Add(CreateNavButton("alarms", "报警中心"));
        nav.Controls.Add(CreateNavButton("settings", "系统设置"));

        var footer = new Label
        {
            Text = "v1.0 · .NET 8 / SQLite\n本地桌面版",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(100, 116, 139),
            Font = new Font("Microsoft YaHei UI", 7.5F),
            TextAlign = ContentAlignment.MiddleLeft,
            BackColor = UiTheme.Sidebar
        };

        sidebar.Controls.Add(brand, 0, 0);
        sidebar.Controls.Add(nav, 0, 1);
        sidebar.Controls.Add(new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Sidebar }, 0, 2);
        sidebar.Controls.Add(footer, 0, 3);
        return sidebar;
    }

    private Button CreateNavButton(string key, string title)
    {
        var button = new Button
        {
            Text = $"    {title}",
            Tag = key,
            Width = 208,
            Height = 44,
            Margin = new Padding(0, 3, 0, 3),
            FlatStyle = FlatStyle.Flat,
            FlatAppearance = { BorderSize = 0 },
            BackColor = UiTheme.Sidebar,
            ForeColor = UiTheme.SidebarText,
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0),
            Cursor = Cursors.Hand
        };
        button.Click += async (_, _) => await ShowPageAsync(key);
        _navButtons[key] = button;
        return button;
    }

    private Control BuildHeader()
    {
        var header = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.Surface,
            Padding = new Padding(26, 12, 24, 8)
        };

        var titlePanel = new Panel { Dock = DockStyle.Fill, BackColor = UiTheme.Surface };
        _pageTitle.Location = new Point(0, 10);
        _pageSubtitle.Location = new Point(2, 48);
        titlePanel.Controls.Add(_pageTitle);
        titlePanel.Controls.Add(_pageSubtitle);

        var actions = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 380,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 16, 0, 0),
            BackColor = UiTheme.Surface
        };

        _monitorPill.AutoSize = false;
        _monitorPill.Size = new Size(92, 30);
        _monitorPill.TextAlign = ContentAlignment.MiddleCenter;
        _monitorPill.Font = new Font("Microsoft YaHei UI", 8F, FontStyle.Bold);
        _monitorPill.BackColor = UiTheme.AccentSoft;
        _monitorPill.ForeColor = UiTheme.AccentDark;
        _monitorPill.Text = "采集启动中";
        _monitorPill.Margin = new Padding(0, 3, 10, 0);

        _toggleMonitor.Width = 104;
        _toggleMonitor.Click += async (_, _) => await ToggleMonitoringAsync();
        _clock.Margin = new Padding(12, 10, 0, 0);

        actions.Controls.Add(_monitorPill);
        actions.Controls.Add(_toggleMonitor);
        actions.Controls.Add(_clock);

        header.Controls.Add(titlePanel);
        header.Controls.Add(actions);
        return header;
    }

    private Control BuildStatusStrip()
    {
        var status = new StatusStrip
        {
            Dock = DockStyle.Fill,
            BackColor = UiTheme.Surface,
            ForeColor = UiTheme.Muted,
            SizingGrip = false,
            Padding = new Padding(12, 0, 8, 0)
        };
        _statusDb.Font = new Font("Microsoft YaHei UI", 7.5F);
        _statusMonitor.Font = new Font("Microsoft YaHei UI", 7.5F);
        _statusLast.Font = new Font("Microsoft YaHei UI", 7.5F);
        status.Items.AddRange(new ToolStripItem[] { _statusDb, _statusMonitor, _statusLast });
        return status;
    }

    private void AddPage(IPage page)
    {
        if (!_navButtons.TryGetValue(page.Key, out var button))
            throw new InvalidOperationException($"Navigation button not found: {page.Key}");

        _pages[page.Key] = (button, page);
    }

    private void WireMonitoring()
    {
        _monitor.TelemetryBatchCompleted += OnTelemetryBatch;
        _monitor.AlarmRaised += OnAlarmRaised;
        _monitor.StatusChanged += OnMonitoringStatusChanged;
        Shown += async (_, _) =>
        {
            await ShowPageAsync("dashboard");
            await StartMonitoringAsync();
        };
        FormClosing += OnFormClosing;
        FormClosed += (_, _) =>
        {
            _clockTimer.Stop();
            _clockTimer.Dispose();
            _monitor.TelemetryBatchCompleted -= OnTelemetryBatch;
            _monitor.AlarmRaised -= OnAlarmRaised;
            _monitor.StatusChanged -= OnMonitoringStatusChanged;
        };
    }

    private void WireClock()
    {
        _clockTimer.Tick += (_, _) => _clock.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        _clock.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        _clockTimer.Start();
    }

    private async Task ShowPageAsync(string key)
    {
        if (!_pages.TryGetValue(key, out var entry))
            return;

        _activeKey = key;
        foreach (var page in _pages.Values)
            ((Control)page.Page).Visible = ReferenceEquals(page.Page, entry.Page);

        _pageTitle.Text = entry.Page.Title;
        _pageSubtitle.Text = entry.Page.Subtitle;
        ApplyNavStyles();

        try
        {
            await entry.Page.ActivateAsync();
        }
        catch (Exception ex)
        {
            UiTheme.ShowError(this, ex.Message, "页面加载失败");
        }
    }

    private void ApplyNavStyles()
    {
        foreach (var (key, (button, _)) in _pages)
        {
            var active = key == _activeKey;
            button.BackColor = active ? UiTheme.AccentDark : UiTheme.Sidebar;
            button.ForeColor = active ? Color.White : UiTheme.SidebarText;
            button.Font = new Font(
                "Microsoft YaHei UI",
                9F,
                active ? FontStyle.Bold : FontStyle.Regular);
        }
    }

    private async Task StartMonitoringAsync()
    {
        try
        {
            await _monitor.StartAsync();
        }
        catch (Exception ex)
        {
            SetMonitorState("启动失败", UiTheme.DangerSoft, UiTheme.Danger);
            _statusMonitor.Text = "采集：启动失败";
            UiTheme.ShowError(this, ex.Message, "采集启动失败");
        }
    }

    private async Task ToggleMonitoringAsync()
    {
        _toggleMonitor.Enabled = false;
        try
        {
            if (_monitor.IsRunning)
            {
                await _monitor.StopAsync();
            }
            else
            {
                await _monitor.StartAsync();
            }
        }
        catch (Exception ex)
        {
            UiTheme.ShowError(this, ex.Message);
        }
        finally
        {
            _toggleMonitor.Enabled = true;
        }
    }

    private void OnTelemetryBatch(object? sender, TelemetryBatchEventArgs e)
    {
        RunOnUi(() =>
        {
            var dashboard = _pages.TryGetValue("dashboard", out var entry)
                ? entry.Page as DashboardPage
                : null;
            dashboard?.HandleTelemetry(e);
            _statusLast.Text = $"最后采样：{DateTime.Now:HH:mm:ss}";
        });
    }

    private void OnAlarmRaised(object? sender, AlarmRaisedEventArgs e)
    {
        RunOnUi(() =>
        {
            if (_pages.TryGetValue("dashboard", out var dashboardEntry)
                && dashboardEntry.Page is DashboardPage dashboard)
            {
                dashboard.HandleAlarm(e.Alarm);
            }

            if (_pages.TryGetValue("alarms", out var alarmsEntry)
                && alarmsEntry.Page is AlarmsPage alarms)
            {
                alarms.HandleAlarm(e.Alarm);
            }
        });
    }

    private void OnMonitoringStatusChanged(object? sender, MonitoringStatusEventArgs e)
    {
        RunOnUi(() =>
        {
            switch (e.State)
            {
                case MonitoringState.Running:
                    SetMonitorState("采集运行中", UiTheme.AccentSoft, UiTheme.AccentDark);
                    _statusMonitor.Text = "采集：运行中";
                    _toggleMonitor.Text = "停止采集";
                    break;
                case MonitoringState.Error:
                    SetMonitorState("采集异常", UiTheme.DangerSoft, UiTheme.Danger);
                    _statusMonitor.Text = "采集：异常";
                    _toggleMonitor.Text = _monitor.IsRunning ? "停止采集" : "启动采集";
                    break;
                default:
                    SetMonitorState("已停止", UiTheme.WarningSoft, UiTheme.Warning);
                    _statusMonitor.Text = "采集：已停止";
                    _toggleMonitor.Text = "启动采集";
                    break;
            }
        });
    }

    private void SetMonitorState(string text, Color background, Color foreground)
    {
        _monitorPill.Text = text;
        _monitorPill.BackColor = background;
        _monitorPill.ForeColor = foreground;
    }

    private void RunOnUi(Action action)
    {
        if (IsDisposed || Disposing)
            return;

        if (InvokeRequired)
        {
            if (!IsHandleCreated)
                return;

            try
            {
                BeginInvoke(action);
            }
            catch (InvalidOperationException)
            {
                // 窗口正在销毁
            }
            return;
        }

        action();
    }

    private async void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_allowClose)
            return;

        e.Cancel = true;
        _allowClose = true;
        _toggleMonitor.Enabled = false;

        try
        {
            await _monitor.StopAsync();
        }
        catch
        {
            // 关闭阶段不阻断退出
        }
        finally
        {
            Close();
        }
    }
}
