using SafeEye.Domain.Entities;
using SafeEye.Domain.Enums;

namespace SafeEye.Domain.Repositories;

public interface ISosEventRepository
{
    Task<SosEvent?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<SosEvent>> GetByDeviceIdsAsync(IEnumerable<Guid> deviceIds, SosStatus? status, CancellationToken ct = default);
    Task AddAsync(SosEvent sosEvent, CancellationToken ct = default);
}