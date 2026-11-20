using IndustrialMonitor.Core.Models;

namespace IndustrialMonitor.Core.Abstractions;

public interface IDeviceRepository
{
    Task<IReadOnlyList<Device>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Device?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<long> InsertAsync(Device device, CancellationToken cancellationToken = default);
    Task UpdateAsync(Device device, CancellationToken cancellationToken = default);
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}
