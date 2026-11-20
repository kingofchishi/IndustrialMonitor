namespace IndustrialMonitor.Core.Models;

/// <summary>
/// 用户可修改的运行设置。保存在 SQLite 的 Settings 表中。
/// </summary>
public sealed class AppOptions
{
    public string OperatorName { get; set; } = Environment.UserName;
    public int RetentionDays { get; set; } = 30;

    public AppOptions Clone() => new()
    {
        OperatorName = OperatorName,
        RetentionDays = RetentionDays
    };
}
