using SafeEye.Domain.Entities;
using SafeEye.Domain.Repositories;
using SafeEye.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace SafeEye.Infrastructure.Persistence.Repositories;

public sealed class GuardianDeviceRepository(AppDbContext db) : IGuardianDeviceRepository
{
    public Task<GuardianDevice?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.GuardianDevices.FirstOrDefaultAsync(e => e.Id == id, ct);

    public Task<GuardianDevice?> GetByGuardianAndDeviceAsync(Guid guardianId, Guid deviceId, CancellationToken ct = default)
        => db.GuardianDevices.FirstOrDefaultAsync(e => e.GuardianId == guardianId && e.DeviceId == deviceId, ct);

    public Task<List<GuardianDevice>> GetByGuardianIdAsync(Guid guardianId, CancellationToken ct = default)
        => db.GuardianDevices.Where(e => e.GuardianId == guardianId).ToListAsync(ct);

    public Task<List<GuardianDevice>> GetByDeviceIdAsync(Guid deviceId, CancellationToken ct = default)
        => db.GuardianDevices.Where(e => e.DeviceId == deviceId).ToListAsync(ct);

    public Task<List<GuardianDevice>> GetAllAsync(CancellationToken ct = default)
        => db.GuardianDevices.ToListAsync(ct);

    public Task<int> CountByGuardianAsync(Guid guardianId, CancellationToken ct = default)
        => db.GuardianDevices.CountAsync(e => e.GuardianId == guardianId, ct);

    public async Task AddAsync(GuardianDevice entry, CancellationToken ct = default)
        => await db.GuardianDevices.AddAsync(entry, ct);

    public void Remove(GuardianDevice entry) => db.GuardianDevices.Remove(entry);
}