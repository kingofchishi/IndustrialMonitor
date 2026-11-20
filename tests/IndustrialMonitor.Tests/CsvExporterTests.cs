using IndustrialMonitor.Core.Models;
using IndustrialMonitor.Infrastructure.Export;

namespace IndustrialMonitor.Tests;

public sealed class CsvExporterTests
{
    [Fact]
    public async Task Export_QuotesCommaAndQuotes()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "TestData");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"industrial-monitor-{Guid.NewGuid():N}.csv");

        try
        {
            var exporter = new TelemetryCsvExporter();
            var records = new[]
            {
                new TelemetryRecord
                {
                    DeviceCode = "TEMP-01",
                    DeviceName = "炉温, \"A\"",
                    Unit = "°C",
                    Value = 88.5,
                    Quality = "Good",
                    CollectedAtUtc = DateTime.UtcNow
                }
            };

            await exporter.ExportAsync(path, records);

            var text = await File.ReadAllTextAsync(path);
            Assert.Contains("时间,设备编号,设备名称,数值,单位,质量", text);
            Assert.Contains("\"炉温, \"\"A\"\"\"", text);
            Assert.Contains("88.5", text);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
