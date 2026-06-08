using SafeEye.Application.Auth.Dtos;
using SafeEye.Application.Common.Interfaces;
using SafeEye.Domain.Entities;
using SafeEye.Domain.Exceptions;
using SafeEye.Domain.Repositories;
using FluentValidation;
using MediatR;

namespace SafeEye.Application.Auth.Commands;

public record LoginCommand(string Email, string Password) : IRequest<AuthResultDto>;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class LoginCommandHandler(
    IUserRepository users,
    IRefreshTokenRepository tokens,
    IUnitOfWork uow,
    IPasswordHasher hasher,
    IJwtService jwt) : IRequestHandler<LoginCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(LoginCommand cmd, CancellationToken ct)
    {
        var user = await users.GetByEmailAsync(cmd.Email, ct)
                   ?? throw new UnauthorizedException("Invalid email or password.");

        if (!hasher.Verify(cmd.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        var access = jwt.GenerateAccessToken(user.Id);
        var refresh = jwt.GenerateRefreshToken();
        await tokens.AddAsync(RefreshToken.Create(user.Id, refresh, DateTime.UtcNow.AddDays(7)), ct);
        await uow.SaveChangesAsync(ct);

        return new AuthResultDto(access, refresh, new UserInfoDto(user.Id, user.Name, user.Email));
    }
}