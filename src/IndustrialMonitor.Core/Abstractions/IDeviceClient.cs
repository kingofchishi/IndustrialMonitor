using IndustrialMonitor.Core.Models;

namespace IndustrialMonitor.Core.Abstractions;

/// <summary>
/// 设备读取抽象。当前是模拟实现，后续可以替换成 Modbus TCP/RTU、OPC UA 或串口实现。
/// </summary>
public interface IDeviceClient
{
    Task<DeviceReading> ReadAsync(Device device, CancellationToken cancellationToken = default);
}
