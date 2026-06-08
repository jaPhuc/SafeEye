using SafeEye.Application.Common.Interfaces;
using SafeEye.Domain.Entities;
using SafeEye.Domain.Exceptions;
using SafeEye.Domain.Repositories;
using MediatR;

namespace SafeEye.Application.IoT.Commands;

/// <summary>
/// Unified SOS handler — dispatched by both:
///   - The Firebase Realtime Database listener (when sos.active becomes true)
///   - The REST fallback endpoint POST /api/iot/sos
/// GPS coordinates come from the Firebase device node at trigger time.
/// </summary>
public record HandleSosTriggerCommand(
    Guid DeviceId,
    double? Latitude,
    double? Longitude) : IRequest;

public sealed class HandleSosTriggerCommandHandler(
    IIoTDeviceRepository iotDevices,
    ISosEventRepository sosEvents,
    IRealtimeNotifier realtime,
    INotificationService notifications,
    IUnitOfWork uow) : IRequestHandler<HandleSosTriggerCommand>
{
    public async Task Handle(HandleSosTriggerCommand cmd, CancellationToken ct)
    {
        var device = await iotDevices.GetByIdAsync(cmd.DeviceId, ct)
                     ?? throw new NotFoundException("IoTDevice", cmd.DeviceId);

        device.UpdateLastSeen();

        var sos = SosEvent.Create(cmd.DeviceId, cmd.Latitude, cmd.Longitude);
        await sosEvents.AddAsync(sos, ct);
        await uow.SaveChangesAsync(ct);

        await realtime.NotifySosTriggeredAsync(
            new SosPayload(sos.Id, device.Id, device.Label, cmd.Latitude, cmd.Longitude, sos.CreatedAt), ct);

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
    }
}