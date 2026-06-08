// ReceiveLocationUpdate removed from ITrackingClient
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace SafeEye.Infrastructure.Realtime;

public interface ITrackingClient
{
    Task ReceiveSosAlert(SosAlertMessage message);      // kept
    Task ReceiveSosResolved(SosResolvedMessage message);// kept
    Task ReceiveError(string error);
    // ReceiveLocationUpdate removed
}

/// <summary>GPS snapshot captured by Firebase listener at SOS trigger time.</summary>
public record SosAlertMessage(
    Guid SosEventId,
    Guid DeviceId,
    string DeviceLabel,
    double? Latitude,
    double? Longitude,
    DateTime Timestamp);

public record SosResolvedMessage(
    Guid SosEventId, Guid DeviceId, Guid ResolvedById, DateTime Timestamp);

[Authorize]
public sealed class TrackingHub(ILogger<TrackingHub> logger) : Hub<ITrackingClient>
{
    private static string DeviceGroup(Guid id) => $"device:{id}";

    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("Guardian {U} connected ({C})", GetUserId(), Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? ex)
    {
        logger.LogInformation("Guardian {U} disconnected", GetUserId());
        return base.OnDisconnectedAsync(ex);
    }

    public async Task WatchDevice(Guid deviceId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, DeviceGroup(deviceId));

    public async Task UnwatchDevice(Guid deviceId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, DeviceGroup(deviceId));

    private Guid GetUserId()
    {
        var v = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(v, out var id) ? id : Guid.Empty;
    }
}