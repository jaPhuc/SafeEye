using SafeEye.Application.Common.Interfaces;
using SafeEye.Application.Users.Dtos;
using SafeEye.Domain.Exceptions;
using SafeEye.Domain.Repositories;
using FluentValidation;
using MediatR;

namespace SafeEye.Application.Users.Commands;

public record UpdateUserCommand(Guid UserId, string Name, string? CurrentPassword, string? NewPassword)
    : IRequest<UserDto>;

public class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(80);
        When(x => x.NewPassword != null, () =>
        {
            RuleFor(x => x.CurrentPassword).NotEmpty()
                .WithMessage("Current password is required to set a new one.");
            RuleFor(x => x.NewPassword).MinimumLength(8);
        });
    }
}

public sealed class UpdateUserCommandHandler(
    IUserRepository users,
    IGuardianDeviceRepository guardianDevices,
    IUnitOfWork uow,
    IPasswordHasher hasher) : IRequestHandler<UpdateUserCommand, UserDto>
{
    public async Task<UserDto> Handle(UpdateUserCommand cmd, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(cmd.UserId, ct)
                   ?? throw new NotFoundException(nameof(Domain.Entities.User), cmd.UserId);

        user.UpdateProfile(cmd.Name);

        if (cmd.NewPassword is not null)
        {
            if (!hasher.Verify(cmd.CurrentPassword!, user.PasswordHash))
                throw new ForbiddenException("Current password is incorrect.");
            user.UpdatePassword(hasher.Hash(cmd.NewPassword));
        }

        await uow.SaveChangesAsync(ct);
        var count = await guardianDevices.CountByGuardianAsync(user.Id, ct);
        return new UserDto(user.Id, user.Name, user.Email, user.FcmToken, count, user.CreatedAt);
    }
}