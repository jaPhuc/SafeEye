using SafeEye.Domain.Entities;
using SafeEye.Domain.Enums;
using SafeEye.Domain.Repositories;
using SafeEye.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace SafeEye.Infrastructure.Persistence.Repositories;

public sealed class SosEventRepository(AppDbContext db, ILogger<SosEventRepository> logger) : ISosEventRepository
{
    public Task<SosEvent?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        logger.LogInformation("SosEventRepository.GetById: id={Id}", id);
        return db.SosEvents.FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task<List<SosEvent>> GetByDeviceIdsAsync(
        IEnumerable<Guid> deviceIds, SosStatus? status, CancellationToken ct = default)
    {
        var ids = deviceIds.ToList();
        logger.LogInformation("SosEventRepository.GetByDeviceIds: deviceIds=[{Ids}], status={Status}",
            string.Join(", ", ids), status);

        var q = db.SosEvents.Where(e => ids.Contains(e.DeviceId));
        if (status.HasValue) q = q.Where(e => e.Status == status.Value);

        var sql = q.ToQueryString();
        logger.LogDebug("SosEventRepository SQL: {Sql}", sql);

        var result = await q.OrderByDescending(e => e.CreatedAt).ToListAsync(ct);

        logger.LogInformation("SosEventRepository.GetByDeviceIds returned {Count} event(s)", result.Count);
        return result;
    }

    public async Task AddAsync(SosEvent sosEvent, CancellationToken ct = default)
    {
        logger.LogInformation("SosEventRepository.Add: id={Id}, deviceId={DeviceId}, status={Status}, lat={Lat}, lng={Lng}",
            sosEvent.Id, sosEvent.DeviceId, sosEvent.Status, sosEvent.Latitude, sosEvent.Longitude);
        await db.SosEvents.AddAsync(sosEvent, ct);
    }
}