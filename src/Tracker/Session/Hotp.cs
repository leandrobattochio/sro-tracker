namespace Tracker.Session;

using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;

public static class Hotp
{
    public static string Generate(string base32Secret, long counter)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(counter);

        var secret = DecodeBase32(base32Secret);

        var key = new byte[32];
        for (var i = 0; i < 32; i++)
            key[i] = secret[i % secret.Length];

        Span<byte> counterBytes = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(counterBytes, (ulong)counter);

        var digest = HMACSHA256.HashData(key, counterBytes);

        var offset = digest[^1] & 0x0F;

        var value = BinaryPrimitives.ReadInt32BigEndian(
            digest.AsSpan(offset, 4)
        ) & 0x7FFFFFFF;
        return (value % 100_000_000).ToString("D8", CultureInfo.InvariantCulture);
    }

    private static byte[] DecodeBase32(string input)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

        var result = new List<byte>();
        var buffer = 0;
        var bits = 0;

        foreach (var value in input.TrimEnd('=').Select(c => alphabet.IndexOf(c)))
        {
            if (value < 0)
                throw new FormatException("Base32 inválido.");

            buffer = (buffer << 5) | value;
            bits += 5;

            if (bits < 8) continue;

            bits -= 8;
            result.Add((byte)(buffer >> bits));
            buffer &= (1 << bits) - 1;
        }

        return result.ToArray();
    }
}