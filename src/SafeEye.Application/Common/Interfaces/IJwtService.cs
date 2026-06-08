namespace SafeEye.Application.Common.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(Guid userId);
    string GenerateRefreshToken();
    Guid? GetUserIdFromToken(string token);
}