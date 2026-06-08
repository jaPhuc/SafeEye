using SafeEye.Application.Sos.Dtos;
using SafeEye.Domain.Exceptions;
using SafeEye.Domain.Repositories;
using MediatR;

namespace SafeEye.Application.Sos.Queries;

public record GetSosEventQuery(Guid GuardianId, Guid SosEventId) : IRequest<SosEventDto>;

public sealed class GetSosEventQueryHandler(
    IGuardianDeviceRepository guardianDevices,
    IIoTDeviceRepository iotDevices,
    ISosEventRepository sosEvents) : IRequestHandler<GetSosEventQuery, SosEventDto>
{
    public async Task<SosEventDto> Handle(GetSosEventQuery q, CancellationToken ct)
    {
        var ev = await sosEvents.GetByIdAsync(q.SosEventId, ct)
                 ?? throw new NotFoundException("SosEvent", q.SosEventId);

        var entry = await guardianDevices.GetByGuardianAndDeviceAsync(q.GuardianId, ev.DeviceId, ct)
                    ?? throw new ForbiddenException("You are not watching the device that triggered this SOS.");

        var device = await iotDevices.GetByIdAsync(ev.DeviceId, ct);
        return new SosEventDto(ev.Id, ev.DeviceId, device?.Label ?? "Unknown",
            ev.Latitude, ev.Longitude, ev.Status, ev.ResolvedAt, ev.ResolvedById, ev.CreatedAt);
    }
}