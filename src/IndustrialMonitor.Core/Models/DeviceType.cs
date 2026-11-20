namespace IndustrialMonitor.Core.Models;

/// <summary>
/// 设备类型。数值会写入 SQLite，发布后不要随意修改已有枚举值。
/// </summary>
public enum DeviceType
{
    Temperature = 1,
    Pressure = 2,
    Speed = 3,
    Level = 4
}

public static class DeviceTypeExtensions
{
    public static string ToDisplayName(this DeviceType type) => type switch
    {
        DeviceType.Temperature => "温度",
        DeviceType.Pressure => "压力",
        DeviceType.Speed => "转速",
        DeviceType.Level => "液位",
        _ => "未知"
    };

    public static string DefaultUnit(this DeviceType type) => type switch
    {
        DeviceType.Temperature => "°C",
        DeviceType.Pressure => "MPa",
        DeviceType.Speed => "rpm",
        DeviceType.Level => "%",
        _ => string.Empty
    };

    public static (double Low, double High) DefaultRange(this DeviceType type) => type switch
    {
        DeviceType.Temperature => (60, 90),
        DeviceType.Pressure => (0.4, 1.2),
        DeviceType.Speed => (1000, 1800),
        DeviceType.Level => (30, 85),
        _ => (0, 100)
    };
}
