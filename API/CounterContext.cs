using System.Globalization;
using System.Text;

namespace API;

public static class CounterContext
{
    public static long ReserveCounter(string path)
    {
        using var stream = new FileStream(
            path,
            FileMode.OpenOrCreate,
            FileAccess.ReadWrite,
            FileShare.None
        );

        var buffer = new byte[(int)stream.Length];
        stream.ReadExactly(buffer);

        var saved = Encoding.ASCII.GetString(buffer).Trim();

        var counter = string.IsNullOrEmpty(saved)
            ? 0
            : long.Parse(saved, CultureInfo.InvariantCulture);
        
        // Validation?

        stream.Position = 0;
        stream.SetLength(0);

        var next = Encoding.ASCII.GetBytes($"{counter + 1}\n");
        stream.Write(next);

        // Garante a gravação em disco
        stream.Flush(flushToDisk: true);

        return counter;
    }
    
}
