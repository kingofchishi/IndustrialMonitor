using IndustrialMonitor.Core.Models;

namespace IndustrialMonitor.Core.Abstractions;

public interface ISettingsRepository
{
    Task<AppOptions> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(AppOptions options, CancellationToken cancellationToken = default);
}
