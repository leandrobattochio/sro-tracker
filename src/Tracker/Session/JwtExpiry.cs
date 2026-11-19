namespace Tracker.Session;

using System.Text.Json;

// Reads the `exp` claim straight from the release JWT, without verifying the
// signature — the server set the expiry, we just reuse the session until then.
internal static class JwtExpiry
{
    public static DateTimeOffset? Read(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length < 2)
            return null;

        if (!TryDecodeBase64Url(parts[1], out var payload))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(payload);
            if (doc.RootElement.TryGetProperty("exp", out var exp) && exp.TryGetInt64(out var seconds))
                return DateTimeOffset.FromUnixTimeSeconds(seconds);
        }
        catch (JsonException)
        {
            // Malformed payload: treat as no expiry we can trust.
        }

        return null;
    }

    static bool TryDecodeBase64Url(string value, out byte[] bytes)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = (padded.Length % 4) switch
        {
            2 => padded + "==",
            3 => padded + "=",
            0 => padded,
            _ => padded
        };

        bytes = new byte[padded.Length];
        if (Convert.TryFromBase64String(padded, bytes, out var written))
        {
            bytes = bytes[..written];
            return true;
        }

        bytes = [];
        return false;
    }
}
