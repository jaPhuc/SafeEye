namespace SafeEye.Application.IoT.Dtos;

public record IoTRegistrationDto(
    Guid Id,
    string DeviceId,
    string DeviceKey,
    string Label,
    string? FirebaseDeviceKey,
    string? FirebaseUserId);

public record IoTDeviceDto(
    Guid Id,
    string DeviceKey,
    string DeviceId,
    string Label,
    string? FirebaseDeviceKey,
    double? BatteryPercent,
    long? UptimeSeconds,
    DateTime? LastSeen);