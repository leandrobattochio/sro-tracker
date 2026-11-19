namespace Tracker.Session;

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

// Builds the requestToken for POST /app-validation/liberacao: a semicolon-joined
// plaintext (HOTP code + counter + metadata), RSA-OAEP(SHA-256) encrypted with the
// public key the key selection picks. Mirrors PKCS1_OAEP from the legacy script.
internal static class RequestToken
{
    public static string Build(string secret, string ps1, string ps2, long counter, AppInfo app)
    {
        var plainText = BuildPlainText(secret, counter, app);
        var publicKey = app.KeySelection == 0 ? ps1 : ps2;
        return Encrypt(plainText, publicKey);
    }

    private static string BuildPlainText(string secret, long counter, AppInfo app)
    {
        return string.Join(';', new[]
        {
            Hotp.Generate(secret, counter),
            counter.ToString(CultureInfo.InvariantCulture),
            app.Ts != string.Empty ? app.Ts : IsoTimestamp(),
            app.Platform,
            app.PackageName,
            app.BuildSignature,
            app.Version,
            app.BuildNumber
        });
    }

    private static string Encrypt(string plainText, string publicKeyPem)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem);
        var cipher = rsa.Encrypt(Encoding.UTF8.GetBytes(plainText), RSAEncryptionPadding.OaepSHA256);
        return Convert.ToBase64String(cipher);
    }

    private static string IsoTimestamp(DateTimeOffset? now = null)
    {
        var dt = now ?? DateTimeOffset.Now;
        var baseDate = dt.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
        var nanos = (dt.Ticks % TimeSpan.TicksPerSecond / 10) * 1000;
        var totalMin = (int)dt.Offset.TotalMinutes;
        var sign = totalMin >= 0 ? "+" : "-";
        var hh = Math.Min(Math.Abs(totalMin) / 60, 99);
        return $"{baseDate}.{nanos:D9}{sign}{hh:D2}:00";
    }
}
