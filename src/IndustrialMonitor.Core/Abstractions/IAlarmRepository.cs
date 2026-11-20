using IndustrialMonitor.Core.Models;

namespace IndustrialMonitor.Core.Abstractions;

public interface IAlarmRepository
{
    Task<long> InsertAsync(
        AlarmRecord alarm,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AlarmRecord>> QueryAsync(
        AlarmQuery query,
        CancellationToken cancellationToken = default);

    Task<int> AcknowledgeAsync(
        IReadOnlyCollection<long> alarmIds,
        string operatorName,
        CancellationToken cancellationToken = default);

    Task<int> CountUnacknowledgedAsync(
        CancellationToken cancellationToken = default);
}
