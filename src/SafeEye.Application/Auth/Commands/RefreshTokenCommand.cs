using SafeEye.Application.Auth.Dtos;
using SafeEye.Application.Common.Interfaces;
using SafeEye.Domain.Entities;
using SafeEye.Domain.Exceptions;
using SafeEye.Domain.Repositories;
using FluentValidation;
using MediatR;

namespace SafeEye.Application.Auth.Commands;

public record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResultDto>;

public class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator() => RuleFor(x => x.RefreshToken).NotEmpty();
}

public sealed class RefreshTokenCommandHandler(
    IUserRepository users,
    IRefreshTokenRepository tokens,
    IUnitOfWork uow,
    IJwtService jwt) : IRequestHandler<RefreshTokenCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(RefreshTokenCommand cmd, CancellationToken ct)
    {
        var stored = await tokens.GetByTokenAsync(cmd.RefreshToken, ct)
                     ?? throw new UnauthorizedException("Invalid or expired refresh token.");

        if (stored.IsExpired)
        {
            tokens.Remove(stored);
            await uow.SaveChangesAsync(ct);
            throw new UnauthorizedException("Refresh token expired. Please log in again.");
        }

        var user = await users.GetByIdAsync(stored.UserId, ct)
                   ?? throw new UnauthorizedException("User not found.");

        tokens.Remove(stored);
        var newRefresh = jwt.GenerateRefreshToken();
        await tokens.AddAsync(RefreshToken.Create(user.Id, newRefresh, DateTime.UtcNow.AddDays(7)), ct);
        await uow.SaveChangesAsync(ct);

        return new AuthResultDto(
            jwt.GenerateAccessToken(user.Id),
            newRefresh,
            new UserInfoDto(user.Id, user.Name, user.Email, user.PhoneNumber));
    }
}