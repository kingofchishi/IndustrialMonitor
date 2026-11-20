namespace IndustrialMonitor.Core.Models;

public sealed record AlarmQuery(
    long? DeviceId = null,
    bool? Acknowledged = null,
    AlarmLevel? Level = null,
    int Limit = 200);
