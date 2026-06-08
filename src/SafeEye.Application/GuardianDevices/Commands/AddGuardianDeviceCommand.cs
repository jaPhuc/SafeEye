using SafeEye.Application.GuardianDevices.Dtos;
using SafeEye.Domain.Entities;
using SafeEye.Domain.Enums;
using SafeEye.Domain.Exceptions;
using SafeEye.Domain.Repositories;
using FluentValidation;
using MediatR;

namespace SafeEye.Application.GuardianDevices.Commands;

public record AddGuardianDeviceCommand(Guid GuardianId, string DeviceKey, string Label)
    : IRequest<GuardianDeviceDto>;

public class AddGuardianDeviceCommandValidator : AbstractValidator<AddGuardianDeviceCommand>
{
    public AddGuardianDeviceCommandValidator()
    {
        RuleFor(x => x.DeviceKey).NotEmpty();
        RuleFor(x => x.Label).NotEmpty().MaximumLength(80);
    }
}

public sealed class AddGuardianDeviceCommandHandler(
    IGuardianDeviceRepository guardianDevices,
    IIoTDeviceRepository iotDevices,
    ISosEventRepository sosEvents,       // ILocationUpdateRepository removed
    IUnitOfWork uow) : IRequestHandler<AddGuardianDeviceCommand, GuardianDeviceDto>
{
    public async Task<GuardianDeviceDto> Handle(AddGuardianDeviceCommand cmd, CancellationToken ct)
    {
        var device = await iotDevices.GetByDeviceKeyAsync(cmd.DeviceKey, ct)
                     ?? throw new NotFoundException("IoTDevice", cmd.DeviceKey);
        var existing = await guardianDevices.GetByGuardianAndDeviceAsync(cmd.GuardianId, device.Id, ct);
        if (existing is not null) throw new ConflictException("You are already watching this device.");
        var entry = GuardianDevice.Create(cmd.GuardianId, device.Id, cmd.Label);
        await guardianDevices.AddAsync(entry, ct);
        await uow.SaveChangesAsync(ct);
        return await BuildDto(entry, device, sosEvents, ct);
    }

    internal static async Task<GuardianDeviceDto> BuildDto(
        GuardianDevice entry, IoTDevice device,
        ISosEventRepository sosEvents, CancellationToken ct)
    {
        var active = await sosEvents.GetByDeviceIdsAsync([device.Id], SosStatus.Active, ct);
        return new GuardianDeviceDto(
            entry.Id, device.Id, entry.Label, device.Label,
            device.FirebaseDeviceKey,    // ← NEW field
            device.LastSeen,
            active.Count > 0,
            entry.CreatedAt);
    }
}