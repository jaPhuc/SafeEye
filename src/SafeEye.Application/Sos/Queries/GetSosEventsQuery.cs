using SafeEye.Application.Sos.Dtos;
using SafeEye.Domain.Enums;
using SafeEye.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;

namespace SafeEye.Application.Sos.Queries;

public record GetSosEventsQuery(Guid GuardianId, SosStatus? Status = null) : IRequest<List<SosEventDto>>;

public sealed class GetSosEventsQueryHandler(
    IGuardianDeviceRepository guardianDevices,
    IIoTDeviceRepository iotDevices,
    ISosEventRepository sosEvents,
    ILogger<GetSosEventsQueryHandler> logger) : IRequestHandler<GetSosEventsQuery, List<SosEventDto>>
{
    public async Task<List<SosEventDto>> Handle(GetSosEventsQuery q, CancellationToken ct)
    {
        logger.LogInformation("GetSosEvents: guardian={GuardianId}, status={Status}", q.GuardianId, q.Status);

        var entries = await guardianDevices.GetByGuardianIdAsync(q.GuardianId, ct);
        var deviceIds = entries.Select(e => e.DeviceId).ToList();

        logger.LogInformation("Guardian {GuardianId} watches {Count} device(s): [{DeviceIds}]",
            q.GuardianId, deviceIds.Count, string.Join(", ", deviceIds));

        if (deviceIds.Count == 0)
        {
            logger.LogWarning("Guardian {GuardianId} has no linked devices — returning empty SOS list", q.GuardianId);
            return [];
        }

        var events = await sosEvents.GetByDeviceIdsAsync(deviceIds, q.Status, ct);

        logger.LogInformation("Found {Count} SOS event(s) for status={Status} among {DeviceCount} device(s)",
            events.Count, q.Status, deviceIds.Count);

        var result = new List<SosEventDto>(events.Count);

        foreach (var ev in events)
        {
            var device = await iotDevices.GetByIdAsync(ev.DeviceId, ct);
            logger.LogInformation("SOS event {Id}: device={DeviceId} ({Label}), status={Status}, created={CreatedAt}",
                ev.Id, ev.DeviceId, device?.Label ?? "Unknown", ev.Status, ev.CreatedAt);
            result.Add(new SosEventDto(ev.Id, ev.DeviceId, device?.Label ?? "Unknown",
                ev.Latitude, ev.Longitude, ev.Status, ev.ResolvedAt, ev.ResolvedById, ev.CreatedAt));
        }
        return result;
    }
}