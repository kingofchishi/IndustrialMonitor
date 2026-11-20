using System.Globalization;
using System.Text;
using IndustrialMonitor.Core.Abstractions;
using IndustrialMonitor.Core.Models;

namespace IndustrialMonitor.Infrastructure.Export;

public sealed class TelemetryCsvExporter : ITelemetryCsvExporter
{
    public async Task ExportAsync(
        string filePath,
        IReadOnlyList<TelemetryRecord> records,
        CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var builder = new StringBuilder();
        builder.AppendLine("时间,设备编号,设备名称,数值,单位,质量");

        foreach (var record in records)
        {
            builder.Append(Escape(record.CollectedAtLocal)).Append(',');
            builder.Append(Escape(record.DeviceCode)).Append(',');
            builder.Append(Escape(record.DeviceName)).Append(',');
            builder.Append(record.Value.ToString("0.##", CultureInfo.InvariantCulture)).Append(',');
            builder.Append(Escape(record.Unit)).Append(',');
            builder.AppendLine(Escape(record.Quality));
        }

        await File.WriteAllTextAsync(
            filePath,
            builder.ToString(),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
            cancellationToken);
    }

    private static string Escape(string? value)
    {
        value ??= string.Empty;
        if (value.Contains('"') || value.Contains(',') || value.Contains('\r') || value.Contains('\n'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }
}
