namespace IndustrialMonitor.Core.Models;

/// <summary>
/// 被监控设备。Id 由数据库生成；Code 是业务唯一编号。
/// </summary>
public sealed class Device
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DeviceType Type { get; set; } = DeviceType.Temperature;
    public string Unit { get; set; } = "°C";
    public double LowLimit { get; set; }
    public double HighLimit { get; set; }
    public int SamplingIntervalMs { get; set; } = 1000;
    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public string DisplayName => $"{Name} ({Code})";
    public string TypeText => Type.ToDisplayName();
    public string RangeText => $"{LowLimit:0.##} ~ {HighLimit:0.##} {Unit}";
    public string SamplingText => $"{SamplingIntervalMs} ms";
    public string EnabledText => IsEnabled ? "启用" : "停用";
    public string CreatedAtLocal => CreatedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");

    public Device Clone() => (Device)MemberwiseClone();
}
