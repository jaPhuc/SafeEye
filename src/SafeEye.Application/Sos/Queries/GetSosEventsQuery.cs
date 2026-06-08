using SafeEye.Application.Sos.Dtos;
using SafeEye.Domain.Enums;
using SafeEye.Domain.Repositories;
using MediatR;

namespace SafeEye.Application.Sos.Queries;

public record GetSosEventsQuery(Guid GuardianId, SosStatus? Status = null) : IRequest<List<SosEventDto>>;

public sealed class GetSosEventsQueryHandler(
    IGuardianDeviceRepository guardianDevices,
    IIoTDeviceRepository iotDevices,
    ISosEventRepository sosEvents) : IRequestHandler<GetSosEventsQuery, List<SosEventDto>>
{
    public async Task<List<SosEventDto>> Handle(GetSosEventsQuery q, CancellationToken ct)
    {
        var entries = await guardianDevices.GetByGuardianIdAsync(q.GuardianId, ct);
        var deviceIds = entries.Select(e => e.DeviceId).ToList();
        if (deviceIds.Count == 0) return [];

        var events = await sosEvents.GetByDeviceIdsAsync(deviceIds, q.Status, ct);
        var result = new List<SosEventDto>(events.Count);

        foreach (var ev in events)
        {
            var device = await iotDevices.GetByIdAsync(ev.DeviceId, ct);
            result.Add(new SosEventDto(ev.Id, ev.DeviceId, device?.Label ?? "Unknown",
                ev.Latitude, ev.Longitude, ev.Status, ev.ResolvedAt, ev.ResolvedById, ev.CreatedAt));
        }
        return result;
    }
}