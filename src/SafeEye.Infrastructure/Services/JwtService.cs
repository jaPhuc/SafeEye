using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using SafeEye.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace SafeEye.Infrastructure.Services;

public sealed class JwtService(IConfiguration config) : IJwtService
{
    private SymmetricSecurityKey Key =>
        new(Encoding.UTF8.GetBytes(config["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is not configured.")));

    public string GenerateAccessToken(Guid userId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: new SigningCredentials(Key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
        => Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();

    public Guid? GetUserIdFromToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = config["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = config["Jwt:Audience"],
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = Key,
                ValidateLifetime = false,
            };
            var principal = handler.ValidateToken(token, parameters, out _);
            var claim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(claim, out var id) ? id : null;
        }
        catch { return null; }
    }
}