using IndustrialMonitor.Core.Models;

namespace IndustrialMonitor.Core.Abstractions;

public interface ITelemetryCsvExporter
{
    Task ExportAsync(
        string filePath,
        IReadOnlyList<TelemetryRecord> records,
        CancellationToken cancellationToken = default);
}
