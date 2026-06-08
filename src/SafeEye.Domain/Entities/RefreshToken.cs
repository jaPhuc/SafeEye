namespace SafeEye.Domain.Entities;

public sealed class RefreshToken
{
    private RefreshToken() { }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public User User { get; private set; } = null!;

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    public static RefreshToken Create(Guid userId, string token, DateTime expiresAt) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Token = token,
        ExpiresAt = expiresAt,
        CreatedAt = DateTime.UtcNow,
    };
}