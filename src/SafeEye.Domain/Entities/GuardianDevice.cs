namespace SafeEye.Domain.Entities;

public sealed class GuardianDevice
{
    private GuardianDevice() { }

    public Guid Id { get; private set; }
    public string GuardianUuid { get; private set; } = string.Empty;
    public string FcmToken { get; private set; } = string.Empty;
    public Guid DeviceId { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public IoTDevice Device { get; private set; } = null!;

    public static GuardianDevice Create(string guardianUuid, Guid deviceId, string fcmToken, string label) => new()
    {
        Id = Guid.NewGuid(),
        GuardianUuid = guardianUuid,
        DeviceId = deviceId,
        FcmToken = fcmToken,
        Label = label,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };

    public void UpdateFcmToken(string fcmToken) { FcmToken = fcmToken; UpdatedAt = DateTime.UtcNow; }
    public void UpdateLabel(string label) { Label = label; UpdatedAt = DateTime.UtcNow; }
}