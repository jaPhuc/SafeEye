// ILocationUpdateRepository removed from constructor
using SafeEye.Application.GuardianDevices.Commands;
using SafeEye.Application.GuardianDevices.Dtos;
using SafeEye.Domain.Repositories;
using MediatR;

namespace SafeEye.Application.GuardianDevices.Queries;

public record GetGuardianDevicesQuery(Guid GuardianId) : IRequest<List<GuardianDeviceDto>>;

public sealed class GetGuardianDevicesQueryHandler(
    IGuardianDeviceRepository guardianDevices,
    IIoTDeviceRepository iotDevices,
    ISosEventRepository sosEvents) : IRequestHandler<GetGuardianDevicesQuery, List<GuardianDeviceDto>>
{
    public async Task<List<GuardianDeviceDto>> Handle(GetGuardianDevicesQuery q, CancellationToken ct)
    {
        var entries = await guardianDevices.GetByGuardianIdAsync(q.GuardianId, ct);
        var result = new List<GuardianDeviceDto>(entries.Count);
        foreach (var entry in entries)
        {
            var device = await iotDevices.GetByIdAsync(entry.DeviceId, ct);
            if (device is null) continue;
            result.Add(await AddGuardianDeviceCommandHandler.BuildDto(entry, device, sosEvents, ct));
        }
        return result;
    }
}