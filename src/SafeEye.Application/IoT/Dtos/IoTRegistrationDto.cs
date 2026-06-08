namespace SafeEye.Application.IoT.Dtos;

public record IoTRegistrationDto(
    Guid DeviceId,
    string DeviceKey,
    string Label,
    string? FirebaseDeviceKey);

public record IoTDeviceDto(
    Guid Id,
    string DeviceKey,
    string Label,
    string? FirebaseDeviceKey,
    int GuardianCount);