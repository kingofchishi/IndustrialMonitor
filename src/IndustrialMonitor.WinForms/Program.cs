using IndustrialMonitor.Core.Abstractions;
using IndustrialMonitor.Core.Models;
using IndustrialMonitor.Core.Services;
using IndustrialMonitor.Infrastructure.Data;
using IndustrialMonitor.Infrastructure.Devices;
using IndustrialMonitor.Infrastructure.Export;
using IndustrialMonitor.WinForms.Infrastructure;
using IndustrialMonitor.WinForms.UI.Forms;
using IndustrialMonitor.WinForms.UI.Pages;
using Microsoft.Extensions.DependencyInjection;

namespace IndustrialMonitor.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

        Application.ThreadException += (_, e) =>
            MessageBox.Show(
                e.Exception.Message,
                "未处理异常",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "应用异常",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        };

        var paths = new AppPaths();
        var services = new ServiceCollection();

        services.AddSingleton(paths);
        services.AddSingleton(new AppOptions());
        services.AddSingleton<ISqliteConnectionFactory>(
            _ => new SqliteConnectionFactory(paths.DatabasePath));
        services.AddSingleton<IDatabaseInitializer, DatabaseInitializer>();
        services.AddSingleton<IDeviceRepository, SqliteDeviceRepository>();
        services.AddSingleton<ITelemetryRepository, SqliteTelemetryRepository>();
        services.AddSingleton<IAlarmRepository, SqliteAlarmRepository>();
        services.AddSingleton<ISettingsRepository, SqliteSettingsRepository>();
        services.AddSingleton<IDeviceClient, SimulatedDeviceClient>();
        services.AddSingleton<ITelemetryCsvExporter, TelemetryCsvExporter>();
        services.AddSingleton<MonitoringService>();

        services.AddSingleton<DashboardPage>();
        services.AddSingleton<DevicesPage>();
        services.AddSingleton<HistoryPage>();
        services.AddSingleton<AlarmsPage>();
        services.AddSingleton<SettingsPage>();
        services.AddSingleton<MainForm>();

        using var provider = services.BuildServiceProvider(
            new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true
            });

        try
        {
            provider.GetRequiredService<IDatabaseInitializer>()
                .InitializeAsync()
                .GetAwaiter()
                .GetResult();

            Application.Run(provider.GetRequiredService<MainForm>());
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"应用启动失败：{ex.Message}\n\n数据库：{paths.DatabasePath}",
                "启动失败",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
