namespace SafeEye.Domain.Entities;

public sealed class GuardianDevice
{
    private GuardianDevice() { }

    public Guid Id { get; private set; }
    public Guid GuardianId { get; private set; }
    public Guid DeviceId { get; private set; }
    public string Label { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public User Guardian { get; private set; } = null!;
    public IoTDevice Device { get; private set; } = null!;

    public static GuardianDevice Create(Guid guardianId, Guid deviceId, string label) => new()
    {
        Id = Guid.NewGuid(),
        GuardianId = guardianId,
        DeviceId = deviceId,
        Label = label,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow,
    };

    public void UpdateLabel(string label) { Label = label; UpdatedAt = DateTime.UtcNow; }
}