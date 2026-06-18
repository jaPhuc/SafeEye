namespace SafeEye.Application.Common.Interfaces;

public record GoogleUserInfo(string Subject, string Email, string Name);

public interface IGoogleAuthService
{
    Task<GoogleUserInfo> VerifyIdTokenAsync(string idToken);
}
