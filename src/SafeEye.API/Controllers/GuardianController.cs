using System.Net.Http.Headers;
using System.Text.Json;
using SafeEye.Domain.Entities;
using SafeEye.Domain.Repositories;
using Google.Apis.Auth.OAuth2;
using Microsoft.AspNetCore.Mvc;

namespace SafeEye.API.Controllers;

[ApiController]
[Route("api/guardian")]
public sealed class GuardianController(
    IIoTDeviceRepository iotDevices,
    IGuardianDeviceRepository guardianDevices,
    IUnitOfWork uow,
    IConfiguration config,
    IHttpClientFactory httpClientFactory,
    ILogger<GuardianController> logger) : ControllerBase
{
    private static readonly string[] _scopes =
    [
        "https://www.googleapis.com/auth/firebase.database",
        "https://www.googleapis.com/auth/userinfo.email",
    ];

    /// <summary>Link a guardian to an IoT device using the device's secretKey and pass.</summary>
    [HttpPost("link")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Link([FromBody] LinkRequest req, CancellationToken ct)
    {
        var rtdbUrl = config["Firebase:RealtimeDatabaseUrl"];
        var credPath = config["Firebase:CredentialsPath"];
        if (string.IsNullOrWhiteSpace(rtdbUrl) || string.IsNullOrWhiteSpace(credPath))
            return Unauthorized(new { error = "Firebase not configured." });

        // 1. Query Firebase for device with matching secretKey
        var http = httpClientFactory.CreateClient("firebase-rtdb");
        var token = await GetAccessTokenAsync(credPath, ct);
        var devicesUrl = $"{rtdbUrl.TrimEnd('/')}/devices.json";

        using var reqMsg = new HttpRequestMessage(HttpMethod.Get, devicesUrl);
        reqMsg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var res = await http.SendAsync(reqMsg, ct);
        res.EnsureSuccessStatusCode();

        var json = await res.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
            return Unauthorized(new { error = "No devices found." });

        // Find device by secretKey
        string? foundDeviceId = null;
        string? foundPass = null;
        string? foundLabel = null;

        foreach (var entry in root.EnumerateObject())
        {
            var data = entry.Value;
            if (data.TryGetProperty("secretKey", out var sk) && sk.GetString() == req.SecretKey)
            {
                foundDeviceId = entry.Name;
                foundPass = data.TryGetProperty("pass", out var p) ? p.GetString() : null;
                foundLabel = data.TryGetProperty("label", out var l) ? l.GetString() : null;
                break;
            }
        }

        if (foundDeviceId is null)
            return Unauthorized(new { error = "Invalid device credentials." });

        if (foundPass != req.Pass)
            return Unauthorized(new { error = "Invalid device credentials." });

        // 2. Create IoTDevice if it doesn't exist in PostgreSQL
        var device = await iotDevices.GetByDeviceIdAsync(foundDeviceId, ct);
        if (device is null)
        {
            device = IoTDevice.Create(
                deviceKey: Guid.NewGuid().ToString("N"),
                deviceId: foundDeviceId,
                secretKeyHash: req.SecretKey,
                label: foundLabel ?? foundDeviceId,
                firebaseDeviceKey: foundDeviceId);
            await iotDevices.AddAsync(device, ct);
        }

        // 3. Create or update GuardianDevice
        var existing = await guardianDevices.GetByGuardianUuidAndDeviceAsync(req.GuardianUuid, device.Id, ct);
        if (existing is null)
        {
            var entry = GuardianDevice.Create(
                req.GuardianUuid, device.Id, req.FcmToken ?? string.Empty,
                foundLabel ?? foundDeviceId);
            await guardianDevices.AddAsync(entry, ct);
        }
        else
        {
            existing.UpdateFcmToken(req.FcmToken ?? string.Empty);
        }

        await uow.SaveChangesAsync(ct);

        logger.LogInformation("Guardian {Uuid} linked to device {DeviceId}", req.GuardianUuid, foundDeviceId);

        return Ok(new
        {
            success = true,
            device = new
            {
                id = device.Id,
                deviceId = device.DeviceId,
                label = device.Label,
            }
        });
    }

    private static async Task<string> GetAccessTokenAsync(string credPath, CancellationToken ct)
        => await ((ITokenAccess)GoogleCredential.FromFile(credPath).CreateScoped(_scopes))
               .GetAccessTokenForRequestAsync(cancellationToken: ct);
}

public record LinkRequest(string SecretKey, string Pass, string GuardianUuid, string? FcmToken = null);