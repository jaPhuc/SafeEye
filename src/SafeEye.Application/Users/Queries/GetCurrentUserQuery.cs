using SafeEye.Application.Users.Dtos;
using SafeEye.Domain.Exceptions;
using SafeEye.Domain.Repositories;
using MediatR;

namespace SafeEye.Application.Users.Queries;

public record GetCurrentUserQuery(Guid UserId) : IRequest<UserDto>;

public sealed class GetCurrentUserQueryHandler(
    IUserRepository users,
    IGuardianDeviceRepository guardianDevices) : IRequestHandler<GetCurrentUserQuery, UserDto>
{
    public async Task<UserDto> Handle(GetCurrentUserQuery q, CancellationToken ct)
    {
        var user = await users.GetByIdAsync(q.UserId, ct)
                   ?? throw new NotFoundException(nameof(Domain.Entities.User), q.UserId);

        var count = await guardianDevices.CountByGuardianAsync(q.UserId, ct);
        return new UserDto(user.Id, user.Name, user.Email, user.FcmToken, count, user.CreatedAt);
    }
}