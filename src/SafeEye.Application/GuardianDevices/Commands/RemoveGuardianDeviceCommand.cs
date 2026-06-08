using SafeEye.Domain.Exceptions;
using SafeEye.Domain.Repositories;
using MediatR;

namespace SafeEye.Application.GuardianDevices.Commands;

public record RemoveGuardianDeviceCommand(Guid GuardianId, Guid EntryId) : IRequest;

public sealed class RemoveGuardianDeviceCommandHandler(
    IGuardianDeviceRepository guardianDevices,
    IUnitOfWork uow) : IRequestHandler<RemoveGuardianDeviceCommand>
{
    public async Task Handle(RemoveGuardianDeviceCommand cmd, CancellationToken ct)
    {
        var entry = await guardianDevices.GetByIdAsync(cmd.EntryId, ct)
                    ?? throw new NotFoundException("GuardianDevice", cmd.EntryId);

        if (entry.GuardianId != cmd.GuardianId) throw new ForbiddenException();

        guardianDevices.Remove(entry);
        await uow.SaveChangesAsync(ct);
    }
}