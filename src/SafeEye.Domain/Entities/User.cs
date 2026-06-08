namespace SafeEye.Domain.Entities;

public sealed class User
{
    private User() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string? FcmToken { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public ICollection<GuardianDevice> WatchedDevices { get; private set; } = new List<GuardianDevice>();
    public ICollection<RefreshToken> RefreshTokens { get; private set; } = new List<RefreshToken>();

    public static User Create(string name, string email, string passwordHash) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Email = email.ToLowerInvariant(),
        PasswordHash = passwordHash,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };

    public void UpdateProfile(string name) { Name = name; UpdatedAt = DateTime.UtcNow; }
    public void UpdatePassword(string passwordHash) { PasswordHash = passwordHash; UpdatedAt = DateTime.UtcNow; }
    public void UpdateFcmToken(string? token) { FcmToken = token; UpdatedAt = DateTime.UtcNow; }
}