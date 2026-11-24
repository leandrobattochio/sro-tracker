namespace Tracker.Session;

using System.Globalization;
using System.Text;

// Backs the counter with a single file, locked for the duration of the reservation
// (FileShare.None) so two processes can't hand out the same value.
public sealed class FileCounterStore(string path) : ICounterStore
{
    public long Reserve()
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

        stream.Position = 0;
        stream.SetLength(0);
        stream.Write(Encoding.ASCII.GetBytes($"{counter + 1}\n"));
        stream.Flush(flushToDisk: true);

        return counter;
    }
}
