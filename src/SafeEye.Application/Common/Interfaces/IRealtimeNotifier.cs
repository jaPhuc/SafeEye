namespace SafeEye.Application.Common.Interfaces;

// LocationPayload removed — location is only captured at SOS trigger time

public record SosPayload(
    Guid SosEventId,
    Guid DeviceId,
    string DeviceLabel,
    double? Latitude,
    double? Longitude,
    DateTime Timestamp);

public record SosResolvedPayload(
    Guid SosEventId,
    Guid DeviceId,
    Guid ResolvedById,
    DateTime Timestamp);

public interface IRealtimeNotifier
{
    Task NotifySosTriggeredAsync(SosPayload payload, CancellationToken ct = default);
    Task NotifySosResolvedAsync(SosResolvedPayload payload, CancellationToken ct = default);
    // NotifyLocationUpdateAsync removed
}