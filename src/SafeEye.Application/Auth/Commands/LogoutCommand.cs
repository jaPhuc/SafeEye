using SafeEye.Domain.Repositories;
using MediatR;

namespace SafeEye.Application.Auth.Commands;

public record LogoutCommand(string? RefreshToken, bool AllDevices, Guid UserId) : IRequest;

public sealed class LogoutCommandHandler(
    IRefreshTokenRepository tokens,
    IUnitOfWork uow) : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand cmd, CancellationToken ct)
    {
        if (cmd.AllDevices)
            await tokens.RemoveAllForUserAsync(cmd.UserId, ct);
        else if (!string.IsNullOrWhiteSpace(cmd.RefreshToken))
        {
            var stored = await tokens.GetByTokenAsync(cmd.RefreshToken, ct);
            if (stored != null) tokens.Remove(stored);
        }
        await uow.SaveChangesAsync(ct);
    }
}