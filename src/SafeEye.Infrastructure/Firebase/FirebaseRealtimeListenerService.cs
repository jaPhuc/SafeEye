using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SafeEye.Domain.Repositories;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SafeEye.Infrastructure.Firebase;

/// <summary>
/// BackgroundService that monitors Firebase Realtime Database via SSE.
///
/// Listens to:
///   /users.json            — watches for sos button state changes (false→true)
///   /device_status.json    — updates IoT device heartbeat, battery, uptime
///
/// When a user's sos field flips false→true:
///   1. Reads lat/lng from /users/{userId}
///   2. Creates a new SOS request at /sos_requests/{pushId} with status "active"
///   3. Idempotent: repeated true values do NOT create duplicate requests
///
/// When a user's sos flips true→false: the existing SOS request remains "active".
/// Resolution is done exclusively by guardians via the API.
/// </summary>
public sealed class FirebaseRealtimeListenerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<FirebaseRealtimeListenerService> _logger;
    private readonly HttpClient _http;

    private static readonly string[] _scopes =
    [
        "https://www.googleapis.com/auth/firebase.database",
        "https://www.googleapis.com/auth/userinfo.email",
    ];

    /// <summary>Last known sos state per userId. Used to detect false→true transitions.</summary>
    private readonly ConcurrentDictionary<string, bool> _previousSosStates = new();

    public FirebaseRealtimeListenerService(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<FirebaseRealtimeListenerService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
        _http = httpClientFactory.CreateClient("firebase-rtdb");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var rtdbUrl = _config["Firebase:RealtimeDatabaseUrl"];
        var credPath = _config["Firebase:CredentialsPath"];

        if (string.IsNullOrWhiteSpace(rtdbUrl) || string.IsNullOrWhiteSpace(credPath))
        {
            _logger.LogWarning("[Firebase RTDB] Listener disabled — configure RealtimeDatabaseUrl and CredentialsPath.");
            return;
        }

        await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);

        _logger.LogInformation("[Firebase RTDB] Starting listeners (users/sos, device_status).");

        var tasks = new List<Task>
        {
            ListenToUsersAsync(rtdbUrl, credPath, stoppingToken),
            ListenToDeviceStatusAsync(rtdbUrl, credPath, stoppingToken),
        };

        await Task.WhenAll(tasks);
    }

    // ───── users listener (SOS button state) ───────────────────────────────────

    private async Task ListenToUsersAsync(string rtdbUrl, string credPath, CancellationToken ct)
    {
        var backoff = TimeSpan.FromSeconds(2);
        var url = $"{rtdbUrl.TrimEnd('/')}/users.json";

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var token = await GetAccessTokenAsync(credPath, ct);
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                using var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
                res.EnsureSuccessStatusCode();
                await using var stream = await res.Content.ReadAsStreamAsync(ct);
                using var reader = new StreamReader(stream, Encoding.UTF8);
                backoff = TimeSpan.FromSeconds(2);
                _logger.LogInformation("[Firebase RTDB] Listening on /users");
                await ReadUsersSseStreamAsync(reader, rtdbUrl, credPath, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Firebase RTDB] /users connection lost. Retry in {D}s.", backoff.TotalSeconds);
                await Task.Delay(backoff, ct);
                backoff = TimeSpan.FromSeconds(Math.Min(backoff.TotalSeconds * 2, 60));
            }
        }
    }

    private async Task ReadUsersSseStreamAsync(StreamReader reader, string rtdbUrl, string credPath, CancellationToken ct)
    {
        string? eventType = null;
        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null) break;
            if (line.StartsWith("event: ", StringComparison.Ordinal))
            {
                eventType = line["event: ".Length..].Trim();
                continue;
            }
            if (line.StartsWith("data: ", StringComparison.Ordinal) && eventType is "put" or "patch")
            {
                await ProcessUsersSseDataAsync(line["data: ".Length..].Trim(), rtdbUrl, credPath, ct);
                eventType = null;
            }
        }
    }

    private async Task ProcessUsersSseDataAsync(string json, string rtdbUrl, string credPath, CancellationToken ct)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var path = root.GetProperty("path").GetString() ?? "/";
            var data = root.GetProperty("data");

            if (data.ValueKind == JsonValueKind.Null)
                return;

            // ── Initial dump: record state, do NOT create requests ──
            if (path == "/")
            {
                if (data.ValueKind != JsonValueKind.Object) return;
                foreach (var user in data.EnumerateObject())
                {
                    RecordSosState(user.Name, user.Value);
                }
                _logger.LogInformation("[Firebase RTDB] Initial state recorded for {Count} user(s)", _previousSosStates.Count);
                return;
            }

            // ── Extract userId from path segments ──
            var segments = path.TrimStart('/').Split('/');
            var userId = segments[0];
            if (string.IsNullOrEmpty(userId)) return;

            if (segments.Length == 1)
            {
                // Full user object update: /{userId}
                await ProcessFullUserUpdateAsync(userId, data, rtdbUrl, credPath, ct);
            }
            else if (segments.Length == 2 && segments[1] == "sos")
            {
                // Single field update: /{userId}/sos
                await ProcessSosFieldUpdateAsync(userId, data, rtdbUrl, credPath, ct);
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "[Firebase RTDB] JSON parse error in /users SSE data");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Firebase RTDB] Error processing /users SSE data");
        }
    }

    private void RecordSosState(string userId, JsonElement userData)
    {
        if (userData.TryGetProperty("sos", out var sosProp) && sosProp.ValueKind == JsonValueKind.True)
        {
            _previousSosStates[userId] = true;
        }
        else
        {
            _previousSosStates[userId] = false;
        }
    }

    private async Task ProcessFullUserUpdateAsync(string userId, JsonElement userData,
        string rtdbUrl, string credPath, CancellationToken ct)
    {
        var newSos = userData.TryGetProperty("sos", out var sosProp) && sosProp.ValueKind == JsonValueKind.True;
        var prevSos = _previousSosStates.GetValueOrDefault(userId, false);

        _previousSosStates[userId] = newSos;

        if (!prevSos && newSos)
        {
            _logger.LogInformation("[Firebase RTDB] SOS false→true for user={UserId}", userId);
            await CreateSosRequestAsync(userId, userData, rtdbUrl, credPath, ct);
        }
    }

    private async Task ProcessSosFieldUpdateAsync(string userId, JsonElement sosValue,
        string rtdbUrl, string credPath, CancellationToken ct)
    {
        var newSos = sosValue.ValueKind == JsonValueKind.True;
        var prevSos = _previousSosStates.GetValueOrDefault(userId, false);

        _previousSosStates[userId] = newSos;

        if (!prevSos && newSos)
        {
            _logger.LogInformation("[Firebase RTDB] SOS false→true (field update) for user={UserId}", userId);

            // Fetch full user data to get lat/lng
            var userData = await FetchUserDataAsync(userId, rtdbUrl, credPath, ct);
            if (userData is not null)
            {
                await CreateSosRequestAsync(userId, userData.Value, rtdbUrl, credPath, ct);
            }
            else
            {
                _logger.LogWarning("[Firebase RTDB] Could not fetch user data for {UserId} — creating SOS without location", userId);
                await CreateSosRequestAsync(userId, null, rtdbUrl, credPath, ct);
            }
        }
    }

    private async Task<JsonElement?> FetchUserDataAsync(string userId, string rtdbUrl, string credPath, CancellationToken ct)
    {
        try
        {
            var token = await GetAccessTokenAsync(credPath, ct);
            var url = $"{rtdbUrl.TrimEnd('/')}/users/{userId}.json";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var res = await _http.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode) return null;
            var json = await res.Content.ReadAsStringAsync(ct);
            if (string.IsNullOrWhiteSpace(json) || json == "null") return null;
            return JsonDocument.Parse(json).RootElement.Clone();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Firebase RTDB] Failed to fetch user data for {UserId}", userId);
            return null;
        }
    }

    private async Task CreateSosRequestAsync(string userId, JsonElement? userData,
        string rtdbUrl, string credPath, CancellationToken ct)
    {
        try
        {
            double? lat = null;
            double? lng = null;

            if (userData.HasValue)
            {
                var u = userData.Value;
                if (u.TryGetProperty("latitude", out var latProp) && latProp.ValueKind == JsonValueKind.Number)
                    lat = latProp.GetDouble();
                if (u.TryGetProperty("longitude", out var lngProp) && lngProp.ValueKind == JsonValueKind.Number)
                    lng = lngProp.GetDouble();
            }

            var token = await GetAccessTokenAsync(credPath, ct);
            var url = $"{rtdbUrl.TrimEnd('/')}/sos_requests.json";

            var body = new Dictionary<string, object?>
            {
                ["userId"] = userId,
                ["latitude"] = lat,
                ["longitude"] = lng,
                ["status"] = "active",
                ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                ["resolvedAt"] = null,
                ["resolvedBy"] = null,
            };

            var json = JsonSerializer.Serialize(body);
            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var res = await _http.SendAsync(req, ct);
            res.EnsureSuccessStatusCode();

            var responseJson = await res.Content.ReadAsStringAsync(ct);
            var pushId = JsonDocument.Parse(responseJson).RootElement.GetProperty("name").GetString();

            _logger.LogInformation("[Firebase RTDB] SOS request created at /sos_requests/{PushId} for user={UserId}",
                pushId, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Firebase RTDB] Failed to create SOS request for user={UserId}", userId);
        }
    }

    // ───── device_status listener ──────────────────────────────────────────────

    private async Task ListenToDeviceStatusAsync(string rtdbUrl, string credPath, CancellationToken ct)
    {
        var backoff = TimeSpan.FromSeconds(2);
        var url = $"{rtdbUrl.TrimEnd('/')}/device_status.json";

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var token = await GetAccessTokenAsync(credPath, ct);
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                using var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
                res.EnsureSuccessStatusCode();
                await using var stream = await res.Content.ReadAsStreamAsync(ct);
                using var reader = new StreamReader(stream, Encoding.UTF8);
                backoff = TimeSpan.FromSeconds(2);
                _logger.LogInformation("[Firebase RTDB] Listening on /device_status");
                await ReadDeviceStatusSseStreamAsync(reader, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Firebase RTDB] /device_status connection lost. Retry in {D}s.", backoff.TotalSeconds);
                await Task.Delay(backoff, ct);
                backoff = TimeSpan.FromSeconds(Math.Min(backoff.TotalSeconds * 2, 60));
            }
        }
    }

    private async Task ReadDeviceStatusSseStreamAsync(StreamReader reader, CancellationToken ct)
    {
        string? eventType = null;
        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null) break;
            if (line.StartsWith("event: ", StringComparison.Ordinal))
            {
                eventType = line["event: ".Length..].Trim();
                continue;
            }
            if (line.StartsWith("data: ", StringComparison.Ordinal) && eventType is "put")
            {
                await ProcessDeviceStatusSseDataAsync(line["data: ".Length..].Trim(), ct);
                eventType = null;
            }
        }
    }

    private async Task ProcessDeviceStatusSseDataAsync(string json, CancellationToken ct)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var path = root.GetProperty("path").GetString() ?? "/";
            var data = root.GetProperty("data");

            if (path == "/" || data.ValueKind != JsonValueKind.Object)
                return;

            var deviceKey = path.TrimStart('/');
            if (string.IsNullOrEmpty(deviceKey))
                return;

            var battery = data.TryGetProperty("battery_percent", out var b) ? b.GetDouble() : (double?)null;
            var uptime = data.TryGetProperty("uptime_seconds", out var u) ? u.GetInt64() : (long?)null;

            using var scope = _scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<IIoTDeviceRepository>();
            var device = await repo.GetByFirebaseKeyAsync(deviceKey, ct);
            if (device is null)
            {
                _logger.LogDebug("[Firebase RTDB] No IoTDevice found for device_key={Key}", deviceKey);
                return;
            }

            device.UpdateLastSeen(battery, uptime);
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            await uow.SaveChangesAsync(ct);

            _logger.LogDebug("[Firebase RTDB] Updated device {DeviceId} ({Label}) — batt={Batt}, uptime={Uptime}",
                device.Id, device.Label, battery, uptime);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "[Firebase RTDB] JSON parse error in device_status SSE data");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Firebase RTDB] Error processing device_status SSE data");
        }
    }

    // ───── Shared helpers ──────────────────────────────────────────────────────

    private static async Task<string> GetAccessTokenAsync(string credPath, CancellationToken ct)
        => await ((ITokenAccess)GoogleCredential.FromFile(credPath).CreateScoped(_scopes))
               .GetAccessTokenForRequestAsync(cancellationToken: ct);
}