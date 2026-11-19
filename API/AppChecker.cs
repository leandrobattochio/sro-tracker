namespace API;

using System.Security.Cryptography;
using System.Text;

// Port de build_app_token_check: base64( RSA-OAEP(chave AES) || IV || ciphertext || tag ).
public static class AppChecker
{
    public static string BuildHeader(string publicKeyPem, string jwt)
    {
        var ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var plainText = Encoding.UTF8.GetBytes($"{ms};{jwt}");

        var aesKey = RandomNumberGenerator.GetBytes(32);
        var iv = RandomNumberGenerator.GetBytes(12);

        using var rsa = RSA.Create();
        rsa.ImportFromPem(publicKeyPem);
        var encryptedKey = rsa.Encrypt(aesKey, RSAEncryptionPadding.OaepSHA256);

        var cipherText = new byte[plainText.Length];
        var tag = new byte[16];
        using (var gcm = new AesGcm(aesKey, tag.Length))
            gcm.Encrypt(iv, plainText, cipherText, tag);

        var payload = new byte[encryptedKey.Length + iv.Length + cipherText.Length + tag.Length];
        var offset = 0;
        foreach (var part in new[] { encryptedKey, iv, cipherText, tag })
        {
            part.CopyTo(payload, offset);
            offset += part.Length;
        }

        return Convert.ToBase64String(payload);
    }
}
