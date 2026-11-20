using IndustrialMonitor.Core.Models;

namespace IndustrialMonitor.Core.Abstractions;

public interface ITelemetryRepository
{
    Task InsertBatchAsync(
        IReadOnlyList<TelemetryRecord> records,
        CancellationToken cancellationToken = default);

    Task<PagedResult<TelemetryRecord>> QueryAsync(
        long? deviceId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TelemetryRecord>> GetRecentAsync(
        long deviceId,
        int count,
        CancellationToken cancellationToken = default);

    Task<long> CountAsync(
        DateTime? fromUtc = null,
        CancellationToken cancellationToken = default);

    Task<int> DeleteOlderThanAsync(
        DateTime cutoffUtc,
        CancellationToken cancellationToken = default);
}
