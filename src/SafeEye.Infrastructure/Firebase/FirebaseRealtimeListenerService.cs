using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using SafeEye.Application.IoT.Commands;
using Google.Apis.Auth.OAuth2;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace SafeEye.Infrastructure.Firebase;

/// <summary>
/// BackgroundService that opens a Server-Sent Events (SSE) stream to Firebase
/// RTDB for every registered IoT device. When sos.active flips to true:
///   1. Reads GPS from the same device node
///   2. Dispatches HandleSosTriggerCommand
///   3. Resets sos.active = false in Firebase (acknowledge)
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

        var devices = await LoadDevicesAsync(stoppingToken);
        if (devices.Count == 0) { _logger.LogInformation("[Firebase RTDB] No devices with Firebase key. Listener idle."); return; }

        _logger.LogInformation("[Firebase RTDB] Starting listeners for {Count} device(s).", devices.Count);

        var tasks = devices.Select(d => ListenToDeviceAsync(d.DeviceId, d.FirebaseKey, rtdbUrl, credPath, stoppingToken));
        await Task.WhenAll(tasks);
    }

    private async Task ListenToDeviceAsync(Guid deviceId, string firebaseKey,
        string rtdbUrl, string credPath, CancellationToken ct)
    {
        var backoff = TimeSpan.FromSeconds(2);
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var token = await GetAccessTokenAsync(credPath, ct);
                var sosUrl = BuildUrl(rtdbUrl, firebaseKey, "sos");
                using var req = new HttpRequestMessage(HttpMethod.Get, sosUrl);
                req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                using var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
                res.EnsureSuccessStatusCode();
                await using var stream = await res.Content.ReadAsStreamAsync(ct);
                using var reader = new StreamReader(stream, Encoding.UTF8);
                backoff = TimeSpan.FromSeconds(2);
                _logger.LogInformation("[Firebase RTDB] Listening on /{Key}/sos", firebaseKey);
                await ReadSseStreamAsync(reader, deviceId, firebaseKey, rtdbUrl, credPath, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[Firebase RTDB] Lost connection /{Key}. Retry in {D}s.", firebaseKey, backoff.TotalSeconds);
                await Task.Delay(backoff, ct);
                backoff = TimeSpan.FromSeconds(Math.Min(backoff.TotalSeconds * 2, 60));
            }
        }
    }

    private async Task ReadSseStreamAsync(StreamReader reader, Guid deviceId,
        string firebaseKey, string rtdbUrl, string credPath, CancellationToken ct)
    {
        string? eventType = null;
        while (!ct.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is null) break;
            if (line.StartsWith("event: ", StringComparison.Ordinal))
            { eventType = line["event: ".Length..].Trim(); continue; }
            if (line.StartsWith("data: ", StringComparison.Ordinal) && eventType is "put" or "patch")
            {
                await ProcessSseDataAsync(line["data: ".Length..].Trim(),
                    deviceId, firebaseKey, rtdbUrl, credPath, ct);
                eventType = null;
            }
        }
    }

    private async Task ProcessSseDataAsync(string json, Guid deviceId,
        string firebaseKey, string rtdbUrl, string credPath, CancellationToken ct)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            var path = root.GetProperty("path").GetString() ?? "/";
            var data = root.GetProperty("data");

            bool active = path switch
            {
                "/" => data.ValueKind == JsonValueKind.Object
                       && data.TryGetProperty("active", out var a) && a.GetBoolean(),
                "/active" => data.ValueKind == JsonValueKind.True,
                _ => false
            };
            if (!active) return;

            _logger.LogInformation("[Firebase RTDB] SOS active — key={Key}", firebaseKey);
            var gps = await FetchGpsAsync(firebaseKey, rtdbUrl, credPath, ct);

            using var scope = _scopeFactory.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            await sender.Send(new HandleSosTriggerCommand(deviceId, gps?.Lat, gps?.Lng), ct);
            await ResetSosAsync(firebaseKey, rtdbUrl, credPath, ct);
        }
        catch (JsonException ex) { _logger.LogWarning(ex, "[Firebase RTDB] JSON parse error"); }
    }

    private async Task<GpsNode?> FetchGpsAsync(string key, string rtdbUrl, string credPath, CancellationToken ct)
    {
        try
        {
            var token = await GetAccessTokenAsync(credPath, ct);
            using var req = new HttpRequestMessage(HttpMethod.Get, BuildUrl(rtdbUrl, key, "gps"));
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var res = await _http.SendAsync(req, ct);
            if (!res.IsSuccessStatusCode) return null;
            var gps = JsonSerializer.Deserialize<GpsNode>(await res.Content.ReadAsStringAsync(ct), _json);
            return gps?.Valid == true ? gps : null;
        }
        catch (Exception ex) { _logger.LogWarning(ex, "[Firebase RTDB] GPS fetch failed /{Key}", key); return null; }
    }

    private async Task ResetSosAsync(string key, string rtdbUrl, string credPath, CancellationToken ct)
    {
        try
        {
            var token = await GetAccessTokenAsync(credPath, ct);
            using var req = new HttpRequestMessage(new HttpMethod("PATCH"), BuildUrl(rtdbUrl, key, "sos"))
            {
                Content = new StringContent(JsonSerializer.Serialize(new { active = false }), Encoding.UTF8, "application/json")
            };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            await _http.SendAsync(req, ct);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "[Firebase RTDB] SOS reset failed /{Key}", key); }
    }

    private static async Task<string> GetAccessTokenAsync(string credPath, CancellationToken ct)
        => await ((ITokenAccess)GoogleCredential.FromFile(credPath).CreateScoped(_scopes))
               .GetAccessTokenForRequestAsync(cancellationToken: ct);

    private static string BuildUrl(string rtdbUrl, string key, string child)
        => $"{rtdbUrl.TrimEnd('/')}/{key}/{child}.json";

    private async Task<List<(Guid DeviceId, string FirebaseKey)>> LoadDevicesAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<Domain.Repositories.IIoTDeviceRepository>();
        return (await repo.GetAllWithFirebaseKeyAsync(ct))
               .Where(d => !string.IsNullOrWhiteSpace(d.FirebaseDeviceKey))
               .Select(d => (d.Id, d.FirebaseDeviceKey!)).ToList();
    }

    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    private sealed class GpsNode
    {
        [JsonPropertyName("lat")] public double Lat { get; set; }
        [JsonPropertyName("lng")] public double Lng { get; set; }
        [JsonPropertyName("alt")] public double Alt { get; set; }
        [JsonPropertyName("speed")] public double Speed { get; set; }
        [JsonPropertyName("hdop")] public double Hdop { get; set; }
        [JsonPropertyName("satellites")] public int Satellites { get; set; }
        [JsonPropertyName("valid")] public bool Valid { get; set; }
        [JsonPropertyName("timestamp")] public long Timestamp { get; set; }
    }
}