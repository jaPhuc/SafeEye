// NotifyLocationUpdateAsync removed
using SafeEye.Application.Common.Interfaces;
using Microsoft.AspNetCore.SignalR;
using SafeEye.Infrastructure.Realtime;

namespace SafeEye.Infrastructure.Realtime;

public sealed class SignalRRealtimeNotifier(IHubContext<TrackingHub, ITrackingClient> hub)
    : IRealtimeNotifier
{
    private static string G(Guid id) => $"device:{id}";

    public Task NotifySosTriggeredAsync(SosPayload p, CancellationToken ct = default)
        => hub.Clients.Group(G(p.DeviceId))
              .ReceiveSosAlert(new SosAlertMessage(
                  p.SosEventId, p.DeviceId, p.DeviceLabel, p.Latitude, p.Longitude, p.Timestamp));

    public Task NotifySosResolvedAsync(SosResolvedPayload p, CancellationToken ct = default)
        => hub.Clients.Group(G(p.DeviceId))
              .ReceiveSosResolved(new SosResolvedMessage(
                  p.SosEventId, p.DeviceId, p.ResolvedByGuardianUuid, p.Timestamp));
    // NotifyLocationUpdateAsync removed
}