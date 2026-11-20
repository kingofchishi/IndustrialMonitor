using IndustrialMonitor.Core.Models;
using IndustrialMonitor.Core.Services;

namespace IndustrialMonitor.Tests;

public sealed class AlarmEvaluatorTests
{
    private static Device Device() => new()
    {
        Id = 1,
        Code = "TEMP-01",
        Name = "炉温",
        Unit = "°C",
        LowLimit = 60,
        HighLimit = 90,
        IsEnabled = true
    };

    [Fact]
    public void HighValue_ProducesCritical()
    {
        var result = AlarmEvaluator.Evaluate(
            Device(),
            new DeviceReading(1, 95, "Good", DateTime.UtcNow));

        Assert.Equal(AlarmLevel.Critical, result.Level);
        Assert.Contains("超过上限", result.Message);
    }

    [Fact]
    public void LowValue_ProducesWarning()
    {
        var result = AlarmEvaluator.Evaluate(
            Device(),
            new DeviceReading(1, 50, "Good", DateTime.UtcNow));

        Assert.Equal(AlarmLevel.Warning, result.Level);
        Assert.Contains("低于下限", result.Message);
    }

    [Fact]
    public void NormalValue_ProducesNoAlarm()
    {
        var result = AlarmEvaluator.Evaluate(
            Device(),
            new DeviceReading(1, 75, "Good", DateTime.UtcNow));

        Assert.False(result.HasAlarm);
    }

    [Fact]
    public void DisabledDevice_IsIgnored()
    {
        var device = Device();
        device.IsEnabled = false;

        var result = AlarmEvaluator.Evaluate(
            device,
            new DeviceReading(1, 99, "Good", DateTime.UtcNow));

        Assert.False(result.HasAlarm);
    }
}
