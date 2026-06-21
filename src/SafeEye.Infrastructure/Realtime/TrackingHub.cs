using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace SafeEye.Infrastructure.Realtime;

public interface ITrackingClient
{
    Task ReceiveSosAlert(SosAlertMessage message);
    Task ReceiveSosResolved(SosResolvedMessage message);
    Task ReceiveError(string error);
}

public record SosAlertMessage(
    Guid SosEventId,
    Guid DeviceId,
    string DeviceLabel,
    double? Latitude,
    double? Longitude,
    DateTime Timestamp);

public record SosResolvedMessage(
    Guid SosEventId, Guid DeviceId, string ResolvedByGuardianUuid, DateTime Timestamp);

public sealed class TrackingHub(ILogger<TrackingHub> logger) : Hub<ITrackingClient>
{
    private static string DeviceGroup(Guid id) => $"device:{id}";

    public override async Task OnConnectedAsync()
    {
        var guardianUuid = Context.GetHttpContext()?.Request.Query["guardianUuid"].FirstOrDefault() ?? "unknown";
        logger.LogInformation("Guardian {U} connected ({C})", guardianUuid, Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? ex)
    {
        logger.LogInformation("Guardian disconnected ({C})", Context.ConnectionId);
        return base.OnDisconnectedAsync(ex);
    }

    public async Task WatchDevice(Guid deviceId)
        => await Groups.AddToGroupAsync(Context.ConnectionId, DeviceGroup(deviceId));

    public async Task UnwatchDevice(Guid deviceId)
        => await Groups.RemoveFromGroupAsync(Context.ConnectionId, DeviceGroup(deviceId));
}