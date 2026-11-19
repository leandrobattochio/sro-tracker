namespace API;

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

public static class Generation
{
    public static (string, string) GenerateTokenWithPlainText(string secret, string ps1, string ps2, long counter,
        string packageName, string buildSignature,
        string version, string buildNumber, string platform, string ts, int keySelection = 1)
    {
        var plainText = BuildPlainText(secret, counter, packageName, buildSignature, version, buildNumber,
            platform, ts);

        var publicKey = keySelection == 0 ? ps1 : ps2;
        var token = EncryptToken(plainText, publicKey);

        return (plainText, token);
    }

    static string BuildPlainText(string secret, long counter, string packageName, string buildSignature,
        string version, string buildNumber, string platform, string ts)
    {
        return string.Join(';', new string[]
        {
            Hotp.HotpCode(secret, counter),
            counter.ToString(),
            ts != string.Empty ? ts : IsoTimestamp(),
            platform,
            packageName,
            buildSignature,
            version,
            buildNumber
        });
    }

    // RSA-OAEP com SHA-256 (hash do OAEP e do MGF1), igual ao PKCS1_OAEP do pycryptodome.
    static string EncryptToken(string plainText, string publicKeyPem)
    {
        using var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem);

        var cipher = rsa.Encrypt(Encoding.UTF8.GetBytes(plainText), RSAEncryptionPadding.OaepSHA256);

        return Convert.ToBase64String(cipher);
    }

    static string IsoTimestamp(DateTimeOffset? now = null)
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
