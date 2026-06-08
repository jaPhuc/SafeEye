using SafeEye.Application.Common.Interfaces;
using SafeEye.Domain.Enums;
using SafeEye.Domain.Exceptions;
using SafeEye.Domain.Repositories;
using MediatR;

namespace SafeEye.Application.Sos.Commands;

public record ResolveSosEventCommand(Guid GuardianId, Guid SosEventId) : IRequest;

public sealed class ResolveSosEventCommandHandler(
    IGuardianDeviceRepository guardianDevices,
    ISosEventRepository sosEvents,
    IRealtimeNotifier realtime,
    IUnitOfWork uow) : IRequestHandler<ResolveSosEventCommand>
{
    public async Task Handle(ResolveSosEventCommand cmd, CancellationToken ct)
    {
        var ev = await sosEvents.GetByIdAsync(cmd.SosEventId, ct)
                 ?? throw new NotFoundException("SosEvent", cmd.SosEventId);

        if (ev.Status == SosStatus.Resolved)
            throw new ConflictException("SOS event is already resolved.");

        var entry = await guardianDevices.GetByGuardianAndDeviceAsync(cmd.GuardianId, ev.DeviceId, ct)
                    ?? throw new ForbiddenException("You are not watching the device that triggered this SOS.");

        ev.Resolve(cmd.GuardianId);
        await uow.SaveChangesAsync(ct);

        await realtime.NotifySosResolvedAsync(
            new SosResolvedPayload(ev.Id, ev.DeviceId, cmd.GuardianId, DateTime.UtcNow), ct);
    }
}