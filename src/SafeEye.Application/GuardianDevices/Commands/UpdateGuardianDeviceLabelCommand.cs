// ILocationUpdateRepository dependency removed
using SafeEye.Application.GuardianDevices.Commands;
using SafeEye.Application.GuardianDevices.Dtos;
using SafeEye.Domain.Exceptions;
using SafeEye.Domain.Repositories;
using FluentValidation;
using MediatR;

namespace SafeEye.Application.GuardianDevices.Commands;

public record UpdateGuardianDeviceLabelCommand(Guid GuardianId, Guid EntryId, string Label)
    : IRequest<GuardianDeviceDto>;

public class UpdateGuardianDeviceLabelCommandValidator : AbstractValidator<UpdateGuardianDeviceLabelCommand>
{
    public UpdateGuardianDeviceLabelCommandValidator()
        => RuleFor(x => x.Label).NotEmpty().MaximumLength(80);
}

public sealed class UpdateGuardianDeviceLabelCommandHandler(
    IGuardianDeviceRepository guardianDevices,
    IIoTDeviceRepository iotDevices,
    ISosEventRepository sosEvents,
    IUnitOfWork uow) : IRequestHandler<UpdateGuardianDeviceLabelCommand, GuardianDeviceDto>
{
    public async Task<GuardianDeviceDto> Handle(UpdateGuardianDeviceLabelCommand cmd, CancellationToken ct)
    {
        var entry = await guardianDevices.GetByIdAsync(cmd.EntryId, ct)
                    ?? throw new NotFoundException("GuardianDevice", cmd.EntryId);
        if (entry.GuardianId != cmd.GuardianId) throw new ForbiddenException();
        entry.UpdateLabel(cmd.Label);
        await uow.SaveChangesAsync(ct);
        var device = await iotDevices.GetByIdAsync(entry.DeviceId, ct)
                     ?? throw new NotFoundException("IoTDevice", entry.DeviceId);
        return await AddGuardianDeviceCommandHandler.BuildDto(entry, device, sosEvents, ct);
    }
}