using SafeEye.Domain.Enums;

namespace SafeEye.Domain.Entities;

public sealed class SosEvent
{
    private SosEvent() { }

    public Guid Id { get; private set; }
    public Guid DeviceId { get; private set; }
    public double? Latitude { get; private set; }
    public double? Longitude { get; private set; }
    public SosStatus Status { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public string? ResolvedByGuardianUuid { get; private set; }
    public DateTime CreatedAt { get; private set; }

    public IoTDevice Device { get; private set; } = null!;

    public static SosEvent Create(Guid deviceId, double? lat, double? lng) => new()
    {
        Id = Guid.NewGuid(),
        DeviceId = deviceId,
        Latitude = lat,
        Longitude = lng,
        Status = SosStatus.Active,
        CreatedAt = DateTime.UtcNow,
    };

    public void Resolve(string guardianUuid)
    {
        Status = SosStatus.Resolved;
        ResolvedByGuardianUuid = guardianUuid;
        ResolvedAt = DateTime.UtcNow;
    }
}