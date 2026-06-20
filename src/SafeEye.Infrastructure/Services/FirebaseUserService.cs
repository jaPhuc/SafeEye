using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using SafeEye.Application.Common.Interfaces;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SafeEye.Infrastructure.Services;

public sealed class FirebaseUserService(
    IConfiguration config,
    IHttpClientFactory httpClientFactory,
    ILogger<FirebaseUserService> logger) : IFirebaseUserService
{
    private static readonly string[] _scopes =
    [
        "https://www.googleapis.com/auth/firebase.database",
        "https://www.googleapis.com/auth/userinfo.email",
    ];

    public async Task CreateUserAsync(string userId, string name, string? phoneNumber, CancellationToken ct = default)
    {
        var rtdbUrl = config["Firebase:RealtimeDatabaseUrl"];
        var credPath = config["Firebase:CredentialsPath"];

        if (string.IsNullOrWhiteSpace(rtdbUrl) || string.IsNullOrWhiteSpace(credPath))
        {
            logger.LogWarning("[Firebase] Cannot create user — Firebase not configured.");
            return;
        }

        try
        {
            var token = await ((ITokenAccess)GoogleCredential.FromFile(credPath).CreateScoped(_scopes))
                .GetAccessTokenForRequestAsync(cancellationToken: ct);

            var url = $"{rtdbUrl.TrimEnd('/')}/users/{userId}.json";

            var body = new Dictionary<string, object?>
            {
                ["name"] = name,
                ["phone"] = phoneNumber,
                ["latitude"] = null,
                ["longitude"] = null,
                ["sos"] = false,
            };

            var json = JsonSerializer.Serialize(body);
            using var http = httpClientFactory.CreateClient("firebase-rtdb");
            using var req = new HttpRequestMessage(new HttpMethod("PUT"), url)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var res = await http.SendAsync(req, ct);
            res.EnsureSuccessStatusCode();

            logger.LogInformation("[Firebase] Created user node at /users/{UserId}", userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[Firebase] Failed to create user node for user={UserId}", userId);
        }
    }
}
