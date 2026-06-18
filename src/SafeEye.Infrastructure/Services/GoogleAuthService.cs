using SafeEye.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace SafeEye.Infrastructure.Services;

public sealed class GoogleAuthService(IConfiguration configuration) : IGoogleAuthService
{
    public async Task<GoogleUserInfo> VerifyIdTokenAsync(string idToken)
    {
        var clientId = configuration["Google:ClientId"]
            ?? throw new InvalidOperationException("Google:ClientId is not configured.");

        var settings = new Google.Apis.Auth.GoogleJsonWebSignature.ValidationSettings
        {
            Audience = new[] { clientId }
        };

        var payload = await Google.Apis.Auth.GoogleJsonWebSignature.ValidateAsync(idToken, settings);

        return new GoogleUserInfo(payload.Subject, payload.Email ?? "", payload.Name ?? "User");
    }
}
