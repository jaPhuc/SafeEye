using SafeEye.Domain.Entities;

namespace SafeEye.Domain.Repositories;

public interface IGuardianDeviceRepository
{
    Task<GuardianDevice?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<GuardianDevice?> GetByGuardianUuidAndDeviceAsync(string guardianUuid, Guid deviceId, CancellationToken ct = default);
    Task<List<GuardianDevice>> GetByGuardianUuidAsync(string guardianUuid, CancellationToken ct = default);
    Task<List<GuardianDevice>> GetByDeviceIdAsync(Guid deviceId, CancellationToken ct = default);
    Task<List<GuardianDevice>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(GuardianDevice entry, CancellationToken ct = default);
    void Remove(GuardianDevice entry);
}