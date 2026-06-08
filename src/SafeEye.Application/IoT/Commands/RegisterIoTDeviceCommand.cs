using System.Security.Cryptography;
using SafeEye.Application.IoT.Dtos;
using SafeEye.Domain.Entities;
using SafeEye.Domain.Repositories;
using FluentValidation;
using MediatR;

namespace SafeEye.Application.IoT.Commands;

public record RegisterIoTDeviceCommand(
    string Label,
    string? FirebaseDeviceKey = null)   // ← NEW optional param
    : IRequest<IoTRegistrationDto>;

public class RegisterIoTDeviceCommandValidator : AbstractValidator<RegisterIoTDeviceCommand>
{
    public RegisterIoTDeviceCommandValidator()
    {
        RuleFor(x => x.Label).NotEmpty().MaximumLength(80);
        RuleFor(x => x.FirebaseDeviceKey).MaximumLength(100).When(x => x.FirebaseDeviceKey != null);
    }
}

public sealed class RegisterIoTDeviceCommandHandler(
    IIoTDeviceRepository iotDevices,
    IUnitOfWork uow) : IRequestHandler<RegisterIoTDeviceCommand, IoTRegistrationDto>
{
    public async Task<IoTRegistrationDto> Handle(RegisterIoTDeviceCommand cmd, CancellationToken ct)
    {
        var deviceKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var device = IoTDevice.Create(deviceKey, cmd.Label, cmd.FirebaseDeviceKey);
        await iotDevices.AddAsync(device, ct);
        await uow.SaveChangesAsync(ct);
        return new IoTRegistrationDto(device.Id, deviceKey, device.Label, device.FirebaseDeviceKey);
    }
}