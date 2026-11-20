using IndustrialMonitor.Core.Models;

namespace IndustrialMonitor.Core.Services;

public enum MonitoringState
{
    Stopped,
    Running,
    Error
}

public sealed class TelemetryBatchEventArgs : EventArgs
{
    public TelemetryBatchEventArgs(IReadOnlyList<TelemetryRecord> readings)
    {
        Readings = readings;
    }

    public IReadOnlyList<TelemetryRecord> Readings { get; }
}

public sealed class AlarmRaisedEventArgs : EventArgs
{
    public AlarmRaisedEventArgs(AlarmRecord alarm)
    {
        Alarm = alarm;
    }

    public AlarmRecord Alarm { get; }
}

public sealed class MonitoringStatusEventArgs : EventArgs
{
    public MonitoringStatusEventArgs(
        MonitoringState state,
        string? message,
        DateTime occurredAtUtc)
    {
        State = state;
        Message = message;
        OccurredAtUtc = occurredAtUtc;
    }

    public MonitoringState State { get; }
    public string? Message { get; }
    public DateTime OccurredAtUtc { get; }
}
