namespace Tracker.Session;

using Microsoft.Extensions.Options;
using Tracker;
using Tracker.Transport;

internal sealed record ReleaseResult(int StatusCode, string? Token, string? Cookie, string Body)
{
    public bool Success => StatusCode == 201;
}

// POST /app-validation/liberacao: reserves a counter, builds the request token and
// returns the JWT plus session cookie. Pulls its secrets from TrackerOptions; the
// host no longer reserves counters or assembles app metadata.
internal sealed class ReleaseService(IOptions<TrackerOptions> options, ITrackerClient client, ICounterStore counter)
{
    public const string CookieName = "LBprdint2";

    readonly TrackerOptions _options = options.Value;

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
        if (response.Content is { } content)
        {
            body = content.GetRawText();
            if (content.TryGetProperty("token", out var tk))
                jwt = tk.GetString();
        }

        return new ReleaseResult(status, jwt, ExtractCookie(response), body);
    }

    static string? ExtractCookie(Refit.IApiResponse response)
    {
        if (response.Headers is not { } headers || !headers.TryGetValues("Set-Cookie", out var cookies))
            return null;

        var prefix = $"{CookieName}=";
        foreach (var cookie in cookies)
            if (cookie.StartsWith(prefix, StringComparison.Ordinal))
                return cookie[prefix.Length..].Split(';')[0];

        return null;
    }
}
