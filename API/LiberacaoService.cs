namespace API;

using Microsoft.Extensions.Configuration;

public sealed record AppInfo(string PackageName, string BuildSignature, string Version,
    string BuildNumber, string Platform, string Ts, int KeySelection);

public sealed record LiberacaoResult(int StatusCode, string? Token, string? Cookie, string Body)
{
    public bool Sucesso => StatusCode == 201;
}

public sealed class LiberacaoService(IConfiguration config, ITrackerClient client)
{
    public const string CookieName = "LBprdint2";

    public async Task<LiberacaoResult> AuthAsync(long counter, AppInfo app, CancellationToken cancellationToken = default)
    {
        var secret = config[app.KeySelection == 0 ? "Tracker:One" : "Tracker:Two"];
        if (string.IsNullOrEmpty(secret))
            throw new InvalidOperationException($"Segredo HOTP (Tracker:{(app.KeySelection == 0 ? "One" : "Two")}) não configurado.");

        var (_, token) = Generation.GenerateTokenWithPlainText(
            secret,
            config["Tracker:Ps1"] ?? "",
            config["Tracker:Ps2"] ?? "",
            counter, app.PackageName, app.BuildSignature, app.Version, app.BuildNumber,
            app.Platform, app.Ts, app.KeySelection);

        var response = await client.LiberacaoAsync(new { requestToken = token }, cancellationToken);
        var status = (int?)response.StatusCode ?? 0;

        string? jwt = null;
        var body = string.Empty;
        if (response.Content is { } content)
        {
            body = content.GetRawText();
            if (content.TryGetProperty("token", out var tk))
                jwt = tk.GetString();
        }

        return new LiberacaoResult(status, jwt, ExtractCookie(response), body);
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
