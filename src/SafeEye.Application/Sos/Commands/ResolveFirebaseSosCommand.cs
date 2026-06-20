using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace SafeEye.Application.Sos.Commands;

public record ResolveFirebaseSosCommand(string PushId, Guid GuardianId) : IRequest;

public sealed class ResolveFirebaseSosCommandHandler(
    IConfiguration config,
    ILogger<ResolveFirebaseSosCommandHandler> logger)
    : IRequestHandler<ResolveFirebaseSosCommand>
{
    private static readonly string[] _scopes =
    [
        "https://www.googleapis.com/auth/firebase.database",
        "https://www.googleapis.com/auth/userinfo.email",
    ];

    public async Task Handle(ResolveFirebaseSosCommand cmd, CancellationToken ct)
    {
        var rtdbUrl = config["Firebase:RealtimeDatabaseUrl"];
        var credPath = config["Firebase:CredentialsPath"];

        if (string.IsNullOrWhiteSpace(rtdbUrl) || string.IsNullOrWhiteSpace(credPath))
        {
            logger.LogWarning("[Firebase] Cannot resolve SOS — Firebase not configured.");
            return;
        }

        var token = await ((ITokenAccess)GoogleCredential.FromFile(credPath).CreateScoped(_scopes))
            .GetAccessTokenForRequestAsync(cancellationToken: ct);

        var url = $"{rtdbUrl.TrimEnd('/')}/sos_requests/{cmd.PushId}.json";

        var patch = new Dictionary<string, object?>
        {
            ["status"] = "resolved",
            ["resolvedAt"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ["resolvedBy"] = cmd.GuardianId.ToString(),
        };

        var json = JsonSerializer.Serialize(patch);
        using var http = new HttpClient();
        using var req = new HttpRequestMessage(new HttpMethod("PATCH"), url)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var res = await http.SendAsync(req, ct);
        res.EnsureSuccessStatusCode();

        logger.LogInformation("[Firebase] SOS request {PushId} resolved by guardian {GuardianId}",
            cmd.PushId, cmd.GuardianId);
    }
}
