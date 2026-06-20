namespace SafeEye.Application.Common.Interfaces;

public interface IFirebaseUserService
{
    Task CreateUserAsync(string userId, string name, string? phoneNumber, CancellationToken ct = default);
}
