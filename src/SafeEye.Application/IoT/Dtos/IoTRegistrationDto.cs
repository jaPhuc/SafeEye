namespace SafeEye.Application.IoT.Dtos;

public record IoTRegistrationDto(
    Guid DeviceId,
    string DeviceKey,
    string Label,
    string? FirebaseDeviceKey,
    string? FirebaseUserId);

public record IoTDeviceDto(
    Guid Id,
    string DeviceKey,
    string Label,
    string? FirebaseDeviceKey,
    string? FirebaseUserId,
    double? BatteryPercent,
    long? UptimeSeconds,
    DateTime? LastSeen,
    int GuardianCount);