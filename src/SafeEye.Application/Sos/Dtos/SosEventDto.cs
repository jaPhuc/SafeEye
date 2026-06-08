using SafeEye.Domain.Enums;

namespace SafeEye.Application.Sos.Dtos;

public record SosEventDto
(
    Guid Id,
    Guid DeviceId,
    string DeviceLabel,
    double? Latitude,
    double? Longitude,
    SosStatus Status,
    DateTime? ResolvedAt,
    Guid? ResolvedById,
    DateTime CreatedAt
);