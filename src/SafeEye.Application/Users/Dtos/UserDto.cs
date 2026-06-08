namespace SafeEye.Application.Users.Dtos;

public record UserDto
(
    Guid Id,
    string Name,
    string Email,
    string? FcmToken,
    int DeviceCount,
    DateTime CreatedAt
);