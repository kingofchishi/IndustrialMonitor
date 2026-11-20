using IndustrialMonitor.Core.Abstractions;
using IndustrialMonitor.Core.Models;

namespace IndustrialMonitor.Infrastructure.Devices;

/// <summary>
/// 无硬件演示客户端。用正弦波 + 噪声模拟温度/压力/转速/液位，
/// 会周期性越过上下限，便于演示报警和确认流程。
/// 真实项目替换为 ModbusTcpDeviceClient / SerialPortDeviceClient 即可。
/// </summary>
public sealed class SimulatedDeviceClient : IDeviceClient
{
    public Task<DeviceReading> ReadAsync(
        Device device,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var seconds = DateTime.UtcNow.TimeOfDay.TotalSeconds + device.Id * 17;
        var noise = (Random.Shared.NextDouble() - 0.5) * 2;

        var value = device.Type switch
        {
            DeviceType.Temperature => 82 + Math.Sin(seconds / 20) * 12 + noise * 4,
            DeviceType.Pressure => 0.9 + Math.Sin(seconds / 18) * 0.3 + noise * 0.06,
            DeviceType.Speed => 1600 + Math.Sin(seconds / 15) * 300 + noise * 60,
            DeviceType.Level => 60 + Math.Sin(seconds / 22) * 30 + noise * 8,
            _ => 50 + noise * 5
        };

        value = Math.Round(value, 2);
        return Task.FromResult(new DeviceReading(
            device.Id,
            value,
            "Good",
            DateTime.UtcNow));
    }
}
