using SafeEye.Application.IoT.Dtos;
using SafeEye.Domain.Repositories;
using MediatR;

namespace SafeEye.Application.IoT.Queries;

public record GetAllDevicesQuery : IRequest<List<IoTDeviceDto>>;

public sealed class GetAllDevicesQueryHandler(
    IIoTDeviceRepository iotDevices,
    IGuardianDeviceRepository guardianDevices) : IRequestHandler<GetAllDevicesQuery, List<IoTDeviceDto>>
{
    public async Task<List<IoTDeviceDto>> Handle(GetAllDevicesQuery q, CancellationToken ct)
    {
        var devices = await iotDevices.GetAllAsync(ct);
        var deviceIds = devices.Select(d => d.Id).ToHashSet();
        var allLinks = await guardianDevices.GetAllAsync(ct);
        var groupCounts = allLinks
            .Where(l => deviceIds.Contains(l.DeviceId))
            .GroupBy(l => l.DeviceId)
            .ToDictionary(g => g.Key, g => g.Count());

        return devices.Select(d => new IoTDeviceDto(
            d.Id,
            d.DeviceKey,
            d.Label,
            d.FirebaseDeviceKey,
            d.FirebaseUserId,
            d.BatteryPercent,
            d.UptimeSeconds,
            d.LastSeen,
            groupCounts.GetValueOrDefault(d.Id, 0)
        )).ToList();
    }
}
