using SafeEye.Application.Common.Interfaces;
using SafeEye.Domain.Entities;
using SafeEye.Domain.Exceptions;
using SafeEye.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace SafeEye.Application.IoT.Commands;

public record HandleSosTriggerCommand(
    Guid DeviceId,
    double? Latitude,
    double? Longitude) : IRequest;

public sealed class HandleSosTriggerCommandHandler(
    IIoTDeviceRepository iotDevices,
    ISosEventRepository sosEvents,
    IRealtimeNotifier realtime,
    INotificationService notifications,
    IUnitOfWork uow,
    ILogger<HandleSosTriggerCommandHandler> logger) : IRequestHandler<HandleSosTriggerCommand>
{
    public async Task Handle(HandleSosTriggerCommand cmd, CancellationToken ct)
    {
        logger.LogInformation("Handling SOS trigger for device {DeviceId}", cmd.DeviceId);

        var device = await iotDevices.GetByIdAsync(cmd.DeviceId, ct);
        if (device is null)
        {
            logger.LogWarning("SOS trigger failed: device {DeviceId} not found", cmd.DeviceId);
            throw new NotFoundException("IoTDevice", cmd.DeviceId);
        }

        logger.LogInformation("SOS device found: {Label} (key={FirebaseKey})", device.Label, device.FirebaseDeviceKey);

        device.UpdateLastSeen();

        var sos = SosEvent.Create(cmd.DeviceId, cmd.Latitude, cmd.Longitude);
        await sosEvents.AddAsync(sos, ct);
        await uow.SaveChangesAsync(ct);

        logger.LogInformation("SOS event {SosEventId} created for device {DeviceId} (status={Status})",
            sos.Id, cmd.DeviceId, sos.Status);

        await realtime.NotifySosTriggeredAsync(
            new SosPayload(sos.Id, device.Id, device.Label, cmd.Latitude, cmd.Longitude, sos.CreatedAt), ct);

        logger.LogInformation("SOS real-time notification sent for event {SosEventId}", sos.Id);

        await notifications.SendToGuardiansOfDeviceAsync(
            cmd.DeviceId,
            $"🆘 SOS — {device.Label}",
            $"{device.Label} has triggered an SOS. Location is attached.",
            new Dictionary<string, string>
            {
                ["type"] = "sos",
                ["sosId"] = sos.Id.ToString(),
                ["deviceId"] = device.Id.ToString(),
                ["lat"] = cmd.Latitude?.ToString("F6") ?? "",
                ["lng"] = cmd.Longitude?.ToString("F6") ?? "",
            }, ct);

        logger.LogInformation("SOS push notification sent for event {SosEventId}", sos.Id);
    }
}