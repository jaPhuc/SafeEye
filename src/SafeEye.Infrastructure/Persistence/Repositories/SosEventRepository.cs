using SafeEye.Domain.Entities;
using SafeEye.Domain.Enums;
using SafeEye.Domain.Repositories;
using SafeEye.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace SafeEye.Infrastructure.Persistence.Repositories;

public sealed class SosEventRepository(AppDbContext db) : ISosEventRepository
{
    public Task<SosEvent?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.SosEvents.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<List<SosEvent>> GetByDeviceIdsAsync(
        IEnumerable<Guid> deviceIds, SosStatus? status, CancellationToken ct = default)
    {
        var q = db.SosEvents.Where(e => deviceIds.Contains(e.DeviceId));
        if (status.HasValue) q = q.Where(e => e.Status == status.Value);
        return await q.OrderByDescending(e => e.CreatedAt).ToListAsync(ct);
    }

    public async Task AddAsync(SosEvent sosEvent, CancellationToken ct = default)
        => await db.SosEvents.AddAsync(sosEvent, ct);
}