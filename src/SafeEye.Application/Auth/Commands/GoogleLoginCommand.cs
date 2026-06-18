using SafeEye.Application.Auth.Dtos;
using SafeEye.Application.Common.Interfaces;
using SafeEye.Domain.Entities;
using SafeEye.Domain.Exceptions;
using SafeEye.Domain.Repositories;
using FluentValidation;
using MediatR;

namespace SafeEye.Application.Auth.Commands;

public record GoogleLoginCommand(string IdToken, string? FcmToken) : IRequest<AuthResultDto>;

public class GoogleLoginCommandValidator : AbstractValidator<GoogleLoginCommand>
{
    public GoogleLoginCommandValidator() => RuleFor(x => x.IdToken).NotEmpty();
}

public sealed class GoogleLoginCommandHandler(
    IUserRepository users,
    IRefreshTokenRepository tokens,
    IUnitOfWork uow,
    IJwtService jwt,
    IGoogleAuthService googleAuth) : IRequestHandler<GoogleLoginCommand, AuthResultDto>
{
    public async Task<AuthResultDto> Handle(GoogleLoginCommand cmd, CancellationToken ct)
    {
        GoogleUserInfo googleUser;
        try
        {
            googleUser = await googleAuth.VerifyIdTokenAsync(cmd.IdToken);
        }
        catch
        {
            throw new UnauthorizedException("Invalid Google ID token.");
        }

        var email = googleUser.Email.ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
            throw new UnauthorizedException("Google account must have an email address.");

        var user = await users.GetByGoogleIdAsync(googleUser.Subject, ct);

        if (user is null)
            user = await users.GetByEmailAsync(email, ct);

        if (user is null)
        {
            user = User.CreateFromGoogle(googleUser.Name, email, googleUser.Subject);
            await users.AddAsync(user, ct);
        }
        else if (user.GoogleId is null)
        {
            user.UpdateGoogleId(googleUser.Subject);
        }

        if (cmd.FcmToken is not null)
            user.UpdateFcmToken(cmd.FcmToken);

        var access = jwt.GenerateAccessToken(user.Id);
        var refresh = jwt.GenerateRefreshToken();
        await tokens.AddAsync(RefreshToken.Create(user.Id, refresh, DateTime.UtcNow.AddDays(7)), ct);
        await uow.SaveChangesAsync(ct);

        return new AuthResultDto(access, refresh, new UserInfoDto(user.Id, user.Name, user.Email, user.PhoneNumber));
    }
}
