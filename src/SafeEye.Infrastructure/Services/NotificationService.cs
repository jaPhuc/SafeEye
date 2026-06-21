using SafeEye.Application.Common.Interfaces;
using SafeEye.Domain.Repositories;
using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Logging;

namespace SafeEye.Infrastructure.Services;

public sealed class NotificationService(
    IGuardianDeviceRepository guardianDevices,
    IIoTDeviceRepository iotDevices,
    ILogger<NotificationService> logger) : INotificationService
{
    private static bool _firebaseInitialized;

    public static void TryInitFirebase(string? credentialsPath, ILogger logger)
    {
        if (_firebaseInitialized || string.IsNullOrWhiteSpace(credentialsPath)) return;
        try
        {
            FirebaseApp.Create(new AppOptions { Credential = GoogleCredential.FromFile(credentialsPath) });
            _firebaseInitialized = true;
            logger.LogInformation("Firebase Admin SDK initialized.");
        }
        catch (Exception ex)
        {
            logger.LogWarning("Firebase init failed — push notifications disabled. {Error}", ex.Message);
        }
    }

    public async Task SendToGuardiansOfDeviceAsync(Guid deviceId, string title, string body,
        Dictionary<string, string>? data = null, CancellationToken ct = default)
    {
        var entries = await guardianDevices.GetByDeviceIdAsync(deviceId, ct);
        await Task.WhenAll(entries.Select(e => SendAsync(e.FcmToken, title, body, data, ct)));
    }

    public async Task SendToFirebaseUserAsync(string firebaseUserId, string title, string body,
        Dictionary<string, string>? data = null, CancellationToken ct = default)
    {
        var device = await iotDevices.GetByDeviceIdAsync(firebaseUserId, ct);
        if (device is null)
        {
            logger.LogWarning("No IoT device found for DeviceId={DeviceId}", firebaseUserId);
            return;
        }

        var guardians = await guardianDevices.GetByDeviceIdAsync(device.Id, ct);
        await Task.WhenAll(guardians.Select(e => SendAsync(e.FcmToken, title, body, data, ct)));
    }

    private async Task SendAsync(string token, string title, string body,
        Dictionary<string, string>? data, CancellationToken ct)
    {
        if (!_firebaseInitialized)
        {
            logger.LogDebug("[FCM mock] title={Title} token={Token}", title, token[..8]);
            return;
        }
        try
        {
            await FirebaseMessaging.DefaultInstance.SendAsync(new Message
            {
                Token = token,
                Notification = new Notification { Title = title, Body = body },
                Data = data ?? [],
                Android = new AndroidConfig { Priority = Priority.High },
                Apns = new ApnsConfig { Aps = new Aps { Sound = "default", Badge = 1 } },
            }, ct);
        }
        catch (Exception ex) { logger.LogError(ex, "FCM failed"); }
    }
}