namespace IndustrialMonitor.Core.Models;

/// <summary>
/// 设备客户端返回的一次读数，不包含数据库 Id。
/// </summary>
public sealed record DeviceReading(
    long DeviceId,
    double Value,
    string Quality,
    DateTime CollectedAtUtc);
