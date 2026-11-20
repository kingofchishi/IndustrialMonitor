using IndustrialMonitor.Core.Models;
using IndustrialMonitor.Core.Services;

namespace IndustrialMonitor.Tests;

public sealed class DeviceValidatorTests
{
    private static Device ValidDevice() => new()
    {
        Code = "TEMP-01",
        Name = "炉温监测",
        Unit = "°C",
        LowLimit = 60,
        HighLimit = 90,
        SamplingIntervalMs = 1000,
        IsEnabled = true
    };

    [Fact]
    public void ValidDevice_HasNoErrors()
    {
        Assert.Empty(DeviceValidator.Validate(ValidDevice()));
    }

    [Fact]
    public void EmptyCode_IsRejected()
    {
        var device = ValidDevice();
        device.Code = " ";

        Assert.Contains(
            DeviceValidator.Validate(device),
            error => error.Contains("设备编号"));
    }

    [Fact]
    public void InvalidRange_IsRejected()
    {
        var device = ValidDevice();
        device.LowLimit = 100;

        Assert.Contains(
            DeviceValidator.Validate(device),
            error => error.Contains("上限必须大于下限"));
    }

    [Fact]
    public void TooFastSampling_IsRejected()
    {
        var device = ValidDevice();
        device.SamplingIntervalMs = 100;

        Assert.Contains(
            DeviceValidator.Validate(device),
            error => error.Contains("采样周期"));
    }
}
