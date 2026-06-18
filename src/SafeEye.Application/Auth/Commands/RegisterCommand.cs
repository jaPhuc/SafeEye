using SafeEye.Application.Auth.Dtos;
using SafeEye.Application.Common.Interfaces;
using SafeEye.Domain.Entities;
using SafeEye.Domain.Exceptions;
using SafeEye.Domain.Repositories;
using FluentValidation;
using MediatR;

namespace SafeEye.Application.Auth.Commands;

public record RegisterCommand(string Name, string Email, string Password, string? PhoneNumber) : IRequest<AuthResultDto>;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).MinimumLength(8);
        RuleFor(x => x.PhoneNumber).MaximumLength(20);
    }
}

public sealed class RegisterCommandHandler(
    IUserRepository users,
    IRefreshTokenRepository tokens,
    IUnitOfWork uow,
    IPasswordHasher hasher,
    IJwtService jwt) : IRequestHandler<RegisterCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(RegisterCommand cmd, CancellationToken ct)
    {
        if (await users.ExistsByEmailAsync(cmd.Email, ct))
            throw new ConflictException($"Email '{cmd.Email}' is already registered.");

        var user = User.Create(cmd.Name, cmd.Email, cmd.PhoneNumber, hasher.Hash(cmd.Password));
        await users.AddAsync(user, ct);

        var access = jwt.GenerateAccessToken(user.Id);
        var refresh = jwt.GenerateRefreshToken();
        await tokens.AddAsync(RefreshToken.Create(user.Id, refresh, DateTime.UtcNow.AddDays(7)), ct);
        await uow.SaveChangesAsync(ct);

        return new AuthResultDto(access, refresh, new UserInfoDto(user.Id, user.Name, user.Email, user.PhoneNumber));
    }
}