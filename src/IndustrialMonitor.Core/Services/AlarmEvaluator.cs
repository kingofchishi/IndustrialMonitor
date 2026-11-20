using IndustrialMonitor.Core.Models;

namespace IndustrialMonitor.Core.Services;

public readonly record struct AlarmEvaluation(AlarmLevel? Level, string? Message)
{
    public static AlarmEvaluation None => new(null, null);
    public bool HasAlarm => Level.HasValue;
}

/// <summary>
/// 纯函数式报警规则：给定设备和读数，返回是否越限。
/// 不访问数据库、不依赖 UI，便于单元测试和替换规则。
/// </summary>
public static class AlarmEvaluator
{
    public static AlarmEvaluation Evaluate(Device device, DeviceReading reading)
    {
        if (!device.IsEnabled || device.HighLimit <= device.LowLimit)
            return AlarmEvaluation.None;

        if (reading.Value >= device.HighLimit)
        {
            return new AlarmEvaluation(
                AlarmLevel.Critical,
                $"{device.Name} 超过上限 {device.HighLimit:0.##}{device.Unit}，当前 {reading.Value:0.##}{device.Unit}");
        }

        if (reading.Value <= device.LowLimit)
        {
            return new AlarmEvaluation(
                AlarmLevel.Warning,
                $"{device.Name} 低于下限 {device.LowLimit:0.##}{device.Unit}，当前 {reading.Value:0.##}{device.Unit}");
        }

        return AlarmEvaluation.None;
    }
}
