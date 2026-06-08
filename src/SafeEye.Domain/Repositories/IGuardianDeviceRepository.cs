using SafeEye.Domain.Entities;

namespace SafeEye.Domain.Repositories;

public interface IGuardianDeviceRepository
{
    Task<GuardianDevice?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<GuardianDevice?> GetByGuardianAndDeviceAsync(Guid guardianId, Guid deviceId, CancellationToken ct = default);
    Task<List<GuardianDevice>> GetByGuardianIdAsync(Guid guardianId, CancellationToken ct = default);
    Task<List<GuardianDevice>> GetByDeviceIdAsync(Guid deviceId, CancellationToken ct = default);
    Task<List<GuardianDevice>> GetAllAsync(CancellationToken ct = default);
    Task<int> CountByGuardianAsync(Guid guardianId, CancellationToken ct = default);
    Task AddAsync(GuardianDevice entry, CancellationToken ct = default);
    void Remove(GuardianDevice entry);
}