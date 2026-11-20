namespace IndustrialMonitor.Core.Models;

public enum AlarmLevel
{
    Warning = 1,
    Critical = 2
}

/// <summary>
/// 越限报警。报警由服务器端规则产生，客户端只能确认，不能直接修改报警值。
/// </summary>
public sealed class AlarmRecord
{
    public long Id { get; set; }
    public long DeviceId { get; set; }
    public string DeviceCode { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public AlarmLevel Level { get; set; } = AlarmLevel.Warning;
    public string Message { get; set; } = string.Empty;
    public double Value { get; set; }
    public string Unit { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsAcknowledged { get; set; }
    public DateTime? AcknowledgedAtUtc { get; set; }
    public string? AcknowledgedBy { get; set; }

    public string LevelText => Level == AlarmLevel.Critical ? "严重" : "警告";
    public string OccurredAtLocal => OccurredAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");
    public string OccurredAtShort => OccurredAtUtc.ToLocalTime().ToString("MM-dd HH:mm");
    public string OccurredAtTime => OccurredAtUtc.ToLocalTime().ToString("HH:mm");
    public string ValueText => $"{Value:0.##} {Unit}";
    public string AckText => IsAcknowledged
        ? $"已确认 {AcknowledgedAtUtc?.ToLocalTime():MM-dd HH:mm} {AcknowledgedBy}"
        : "未确认";
}
