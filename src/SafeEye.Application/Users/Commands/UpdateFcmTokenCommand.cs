using SafeEye.Domain.Exceptions;
using SafeEye.Domain.Repositories;
using FluentValidation;
using MediatR;

namespace SafeEye.Application.Users.Commands;

public record UpdateFcmTokenCommand(Guid UserId, string FcmToken) : IRequest;

public class UpdateFcmTokenCommandValidator : AbstractValidator<UpdateFcmTokenCommand>
{
    public UpdateFcmTokenCommandValidator() => RuleFor(x => x.FcmToken).NotEmpty();
}

public sealed class UpdateFcmTokenCommandHandler(
    IUserRepository users,
    IUnitOfWork uow) : IRequestHandler<UpdateFcmTokenCommand>
{
    public async Task Handle(UpdateFcmTokenCommand cmd, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(cmd.UserId, ct)
                   ?? throw new NotFoundException(nameof(Domain.Entities.User), cmd.UserId);
        user.UpdateFcmToken(cmd.FcmToken);
        await uow.SaveChangesAsync(ct);
    }
}