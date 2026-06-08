namespace SafeEye.Application.GuardianDevices.Dtos;

// LastLocation removed — location only in SOS event data
public record GuardianDeviceDto(
    Guid Id,
    Guid DeviceId,
    string Label,
    string HardwareLabel,
    string? FirebaseDeviceKey,   // ← NEW
    DateTime? LastSeen,
    bool HasActiveSos,
    DateTime AddedAt
);