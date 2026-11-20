using IndustrialMonitor.Core.Models;
using IndustrialMonitor.Infrastructure.Data;

namespace IndustrialMonitor.Tests;

public sealed class SqliteRepositoryTests : IDisposable
{
    private readonly string _directory;
    private readonly string _databasePath;
    private readonly SqliteConnectionFactory _factory;

    public SqliteRepositoryTests()
    {
        _directory = Path.Combine(
            AppContext.BaseDirectory,
            "TestData",
            $"industrial-monitor-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
        _databasePath = Path.Combine(_directory, "test.db");
        _factory = new SqliteConnectionFactory(_databasePath);
    }

    [Fact]
    public async Task Initialize_SeedsFourDevicesAndSettings()
    {
        await InitializeAsync();

        var deviceRepository = new SqliteDeviceRepository(_factory);
        var settingsRepository = new SqliteSettingsRepository(_factory);

        var devices = await deviceRepository.GetAllAsync();
        var options = await settingsRepository.LoadAsync();

        Assert.Equal(4, devices.Count);
        Assert.InRange(options.RetentionDays, 1, 3650);
        Assert.False(string.IsNullOrWhiteSpace(options.OperatorName));
    }

    [Fact]
    public async Task TelemetryQuery_SupportsPagingAndDeviceFilter()
    {
        await InitializeAsync();

        var deviceRepository = new SqliteDeviceRepository(_factory);
        var telemetryRepository = new SqliteTelemetryRepository(_factory);
        var device = (await deviceRepository.GetAllAsync()).First();

        var records = Enumerable.Range(0, 25)
            .Select(i => new TelemetryRecord
            {
                DeviceId = device.Id,
                DeviceCode = device.Code,
                DeviceName = device.Name,
                Unit = device.Unit,
                Value = 70 + i / 10d,
                Quality = "Good",
                CollectedAtUtc = DateTime.UtcNow.AddSeconds(-i)
            })
            .ToList();

        await telemetryRepository.InsertBatchAsync(records);

        var page = await telemetryRepository.QueryAsync(
            device.Id, null, null, page: 2, pageSize: 10);

        Assert.Equal(25L, page.TotalCount);
        Assert.Equal(10, page.Items.Count);
        Assert.True(page.HasPrevious);
        Assert.True(page.HasNext);
    }

    [Fact]
    public async Task AlarmCanBeAcknowledged()
    {
        await InitializeAsync();

        var deviceRepository = new SqliteDeviceRepository(_factory);
        var alarmRepository = new SqliteAlarmRepository(_factory);
        var device = (await deviceRepository.GetAllAsync()).First();

        var alarm = new AlarmRecord
        {
            DeviceId = device.Id,
            DeviceCode = device.Code,
            DeviceName = device.Name,
            Level = AlarmLevel.Critical,
            Message = "test alarm",
            Value = 100,
            Unit = device.Unit,
            OccurredAtUtc = DateTime.UtcNow
        };

        var id = await alarmRepository.InsertAsync(alarm);
        var active = await alarmRepository.CountUnacknowledgedAsync();
        var affected = await alarmRepository.AcknowledgeAsync(new[] { id }, "tester");
        var after = await alarmRepository.CountUnacknowledgedAsync();

        Assert.Equal(1, active);
        Assert.Equal(1, affected);
        Assert.Equal(0, after);

        var acknowledged = (await alarmRepository.QueryAsync(
            new AlarmQuery(Acknowledged: true))).Single();
        Assert.True(acknowledged.IsAcknowledged);
        Assert.Equal("tester", acknowledged.AcknowledgedBy);
    }

    private async Task InitializeAsync()
    {
        var initializer = new DatabaseInitializer(_factory);
        await initializer.InitializeAsync();
    }

    public void Dispose()
    {
        // Microsoft.Data.Sqlite 默认池化连接，清理池后测试临时库才能删除。
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }
}
