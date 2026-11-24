namespace Tracker.Session;

using System.Text.Json;
using Microsoft.Extensions.Options;
using Tracker;
using Transport;

internal sealed record ReleaseResult(int StatusCode, string? Token, string Body)
{
    public bool Success => StatusCode == 201;
}

internal sealed class ReleaseService(IOptions<TrackerOptions> options, ITrackerClient client, ICounterStore counter)
{
    private readonly TrackerOptions _options = options.Value;

    public async Task<ReleaseResult> ReleaseAsync(int keySelection, string ts, CancellationToken cancellationToken = default)
    {
        var secret = _options.SecretFor(keySelection);
        if (string.IsNullOrEmpty(secret))
            throw new InvalidOperationException($"HOTP secret (Tracker:{(keySelection == 0 ? "One" : "Two")}) is not configured.");

        var app = AppInfo.From(_options.App, ts, keySelection);
        var token = RequestToken.Build(secret, _options.Ps1, _options.Ps2, counter.Reserve(), app);

        var response = await client.ReleaseAsync(new { requestToken = token }, cancellationToken);
        var status = (int?)response.StatusCode ?? 0;

        string? jwt = null;
        var body = string.Empty;
        var content = response.Content;
        
        if (content.ValueKind is JsonValueKind.Undefined)
            return new ReleaseResult(status, jwt, body);

        body = content.GetRawText();
        if (content.TryGetProperty("token", out var tk))
            jwt = tk.GetString();

        return new ReleaseResult(status, jwt, body);
    }
}
