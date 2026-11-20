namespace IndustrialMonitor.Core.Models;

/// <summary>
/// 一条采样记录。时间统一以 UTC 存储，展示时转成本地时间。
/// </summary>
public sealed class TelemetryRecord
{
    public long Id { get; set; }
    public long DeviceId { get; set; }
    public string DeviceCode { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Quality { get; set; } = "Good";
    public DateTime CollectedAtUtc { get; set; } = DateTime.UtcNow;

    public string CollectedAtLocal => CollectedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
    public string ValueText => $"{Value:0.##} {Unit}";
}
