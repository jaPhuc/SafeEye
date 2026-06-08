using SafeEye.Domain.Entities;

namespace SafeEye.Domain.Repositories;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task AddAsync(RefreshToken token, CancellationToken ct = default);
    void Remove(RefreshToken token);
    Task RemoveAllForUserAsync(Guid userId, CancellationToken ct = default);
}