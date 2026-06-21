namespace SafeEye.Application.Common.Interfaces;

public interface INotificationService
{
    Task SendToGuardiansOfDeviceAsync(Guid deviceId, string title, string body,
        Dictionary<string, string>? data = null, CancellationToken ct = default);

    Task SendToFirebaseUserAsync(string firebaseUserId, string title, string body,
        Dictionary<string, string>? data = null, CancellationToken ct = default);
}