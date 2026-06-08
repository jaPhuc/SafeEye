using SafeEye.Application.Common.Interfaces;
using SafeEye.Domain.Entities;
using SafeEye.Domain.Exceptions;
using SafeEye.Domain.Repositories;
using MediatR;

namespace SafeEye.Application.IoT.Commands;

public record TriggerIoTSosCommand(Guid DeviceId, double? Latitude, double? Longitude) : IRequest;

public sealed class TriggerIoTSosCommandHandler(
    IIoTDeviceRepository iotDevices,
    ISosEventRepository sosEvents,
    IRealtimeNotifier realtime,
    INotificationService notifications,
    IUnitOfWork uow) : IRequestHandler<TriggerIoTSosCommand>
{
    public async Task Handle(TriggerIoTSosCommand cmd, CancellationToken ct)
    {
        var device = await iotDevices.GetByIdAsync(cmd.DeviceId, ct)
                     ?? throw new NotFoundException("IoTDevice", cmd.DeviceId);

        var sos = SosEvent.Create(cmd.DeviceId, cmd.Latitude, cmd.Longitude);
        await sosEvents.AddAsync(sos, ct);
        await uow.SaveChangesAsync(ct);

        await realtime.NotifySosTriggeredAsync(
            new SosPayload(sos.Id, device.Id, device.Label, cmd.Latitude, cmd.Longitude, sos.CreatedAt), ct);

        await notifications.SendToGuardiansOfDeviceAsync(cmd.DeviceId,
            $"SOS — {device.Label}",
            $"{device.Label} has triggered an SOS signal.",
            new Dictionary<string, string>
            {
                ["type"] = "sos",
                ["sosId"] = sos.Id.ToString(),
                ["deviceId"] = device.Id.ToString(),
                ["lat"] = cmd.Latitude?.ToString() ?? "",
                ["lng"] = cmd.Longitude?.ToString() ?? "",
            }, ct);
    }
}