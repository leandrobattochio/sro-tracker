namespace Tracker.Cli;

using Microsoft.Extensions.DependencyInjection;
using Tracker.Tracking;

public sealed class TrackerCli(IServiceProvider services)
{
    public async Task<int> TestAuthAsync(string ts, int keySelection, CancellationToken cancellationToken)
    {
        var tracking = services.GetRequiredService<ITrackingService>();
        var result = await tracking.HandshakeAsync(keySelection, ts, cancellationToken);

        Console.WriteLine($"POST /v4/app-validation/liberacao  HTTP {result.Release}");
        Console.WriteLine($"token: {result.Token}");
        Console.WriteLine($"GET  /v1/versao-atual               HTTP {result.CurrentVersion}");
        Console.WriteLine($"GET  /v1/funcionalidades            HTTP {result.Features}");
        return result.Success ? 0 : 1;
    }

    public async Task<int> TrackAsync(string trackingCode, string ts, int keySelection, CancellationToken cancellationToken)
    {
        var tracking = services.GetRequiredService<ITrackingService>();
        var result = await tracking.TrackAsync(trackingCode, keySelection, ts, cancellationToken);

        Console.WriteLine($"GET /v1/sro-rastro/{trackingCode} HTTP {result.StatusCode}: {result.Body}");
        return result.Success ? 0 : 1;
    }

    public async Task<int> RunInteractiveAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("Tracker CLI");
            Console.WriteLine("  1) Test authentication (release)");
            Console.WriteLine("  2) Track an object");
            Console.WriteLine("  0) Exit");
            Console.Write("Choice: ");

            switch (Console.ReadLine()?.Trim())
            {
                case "1":
                    await TestAuthAsync("", 1, cancellationToken);
                    break;
                case "2":
                    Console.Write("Tracking code: ");
                    var trackingCode = Console.ReadLine()?.Trim();
                    if (string.IsNullOrEmpty(trackingCode))
                        Console.WriteLine("Empty code, back to the menu.");
                    else
                        await TrackAsync(trackingCode, "", 1, cancellationToken);
                    break;
                case "0" or "" or null:
                    return 0;
                default:
                    Console.WriteLine("Invalid option.");
                    break;
            }
        }
    }
}
