// ILocationUpdateRepository removed from constructor
using SafeEye.Application.GuardianDevices.Commands;
using SafeEye.Application.GuardianDevices.Dtos;
using SafeEye.Domain.Exceptions;
using SafeEye.Domain.Repositories;
using MediatR;

namespace SafeEye.Application.GuardianDevices.Queries;

public record GetGuardianDeviceQuery(Guid GuardianId, Guid EntryId) : IRequest<GuardianDeviceDto>;

public sealed class GetGuardianDeviceQueryHandler(
    IGuardianDeviceRepository guardianDevices,
    IIoTDeviceRepository iotDevices,
    ISosEventRepository sosEvents) : IRequestHandler<GetGuardianDeviceQuery, GuardianDeviceDto>
{
    public async Task<GuardianDeviceDto> Handle(GetGuardianDeviceQuery q, CancellationToken ct)
    {
        var entry = await guardianDevices.GetByIdAsync(q.EntryId, ct)
                    ?? throw new NotFoundException("GuardianDevice", q.EntryId);
        if (entry.GuardianId != q.GuardianId) throw new ForbiddenException();
        var device = await iotDevices.GetByIdAsync(entry.DeviceId, ct)
                     ?? throw new NotFoundException("IoTDevice", entry.DeviceId);
        return await AddGuardianDeviceCommandHandler.BuildDto(entry, device, sosEvents, ct);
    }
}