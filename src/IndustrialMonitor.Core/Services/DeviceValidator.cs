using System.Text.RegularExpressions;
using IndustrialMonitor.Core.Models;

namespace IndustrialMonitor.Core.Services;

/// <summary>
/// 设备表单/导入共用的校验规则，避免 UI 和数据层出现两套规则。
/// </summary>
public static partial class DeviceValidator
{
    [GeneratedRegex("^[A-Za-z0-9_-]+$")]
    private static partial Regex CodeRegex();

    public static IReadOnlyList<string> Validate(Device device)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(device.Code))
            errors.Add("设备编号不能为空。");
        else if (device.Code.Length > 32)
            errors.Add("设备编号不能超过 32 个字符。");
        else if (!CodeRegex().IsMatch(device.Code))
            errors.Add("设备编号只能包含字母、数字、下划线和短横线。");

        if (string.IsNullOrWhiteSpace(device.Name))
            errors.Add("设备名称不能为空。");
        else if (device.Name.Length > 64)
            errors.Add("设备名称不能超过 64 个字符。");

        if (string.IsNullOrWhiteSpace(device.Unit))
            errors.Add("单位不能为空。");

        if (device.HighLimit <= device.LowLimit)
            errors.Add("上限必须大于下限。");

        if (device.SamplingIntervalMs is < 200 or > 60_000)
            errors.Add("采样周期必须在 200～60000 ms 之间。");

        return errors;
    }
}
