using SafeEye.Domain.Entities;
using SafeEye.Domain.Repositories;
using SafeEye.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace SafeEye.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository(AppDbContext db) : IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken ct = default)
        => db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == token, ct);

    public async Task AddAsync(RefreshToken token, CancellationToken ct = default)
        => await db.RefreshTokens.AddAsync(token, ct);

    public void Remove(RefreshToken token) => db.RefreshTokens.Remove(token);

    public async Task RemoveAllForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var tokens = await db.RefreshTokens.Where(t => t.UserId == userId).ToListAsync(ct);
        db.RefreshTokens.RemoveRange(tokens);
    }
}