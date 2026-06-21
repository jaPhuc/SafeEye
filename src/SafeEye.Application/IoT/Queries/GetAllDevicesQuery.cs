using SafeEye.Application.IoT.Dtos;
using SafeEye.Domain.Repositories;
using MediatR;

namespace SafeEye.Application.IoT.Queries;

public record GetAllDevicesQuery : IRequest<List<IoTDeviceDto>>;

public sealed class GetAllDevicesQueryHandler(
    IIoTDeviceRepository iotDevices) : IRequestHandler<GetAllDevicesQuery, List<IoTDeviceDto>>
{
    public async Task<List<IoTDeviceDto>> Handle(GetAllDevicesQuery q, CancellationToken ct)
    {
        var devices = await iotDevices.GetAllAsync(ct);

        return devices.Select(d => new IoTDeviceDto(
            d.Id,
            d.DeviceKey,
            d.DeviceId,
            d.Label,
            d.FirebaseDeviceKey,
            d.BatteryPercent,
            d.UptimeSeconds,
            d.LastSeen
        )).ToList();
    }
}
