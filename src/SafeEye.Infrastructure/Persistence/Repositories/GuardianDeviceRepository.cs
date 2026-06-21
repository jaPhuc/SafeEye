using SafeEye.Domain.Entities;
using SafeEye.Domain.Repositories;
using SafeEye.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace SafeEye.Infrastructure.Persistence.Repositories;

public sealed class GuardianDeviceRepository(AppDbContext db) : IGuardianDeviceRepository
{
    public Task<GuardianDevice?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.GuardianDevices.FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<GuardianDevice?> GetByGuardianUuidAndDeviceAsync(string guardianUuid, Guid deviceId, CancellationToken ct = default)
        => db.GuardianDevices.FirstOrDefaultAsync(e => e.GuardianUuid == guardianUuid && e.DeviceId == deviceId, ct);

    public Task<List<GuardianDevice>> GetByGuardianUuidAsync(string guardianUuid, CancellationToken ct = default)
        => db.GuardianDevices.Where(e => e.GuardianUuid == guardianUuid).ToListAsync(ct);

    public Task<List<GuardianDevice>> GetByDeviceIdAsync(Guid deviceId, CancellationToken ct = default)
        => db.GuardianDevices.Where(e => e.DeviceId == deviceId).ToListAsync(ct);

    public Task<List<GuardianDevice>> GetAllAsync(CancellationToken ct = default)
        => db.GuardianDevices.ToListAsync(ct);

    public async Task AddAsync(GuardianDevice entry, CancellationToken ct = default)
        => await db.GuardianDevices.AddAsync(entry, ct);

    public void Remove(GuardianDevice entry) => db.GuardianDevices.Remove(entry);
}