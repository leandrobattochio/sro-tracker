using System.CommandLine;
using Microsoft.Extensions.Hosting;
using Tracker;
using Tracker.Cli;
using Tracker.Cli.Tracking;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});
builder.Services.AddTracker(builder.Configuration);

using var host = builder.Build();

var root = new RootCommand("Tracker command-line client");

var testAuth = new Option<bool>("--test-auth")
    { Description = "Run the release handshake and print the token and each status" };
var track = new Option<string>("--track")
    { Description = "Tracking code to look up (runs release + features + tracking)" };
var preview = new Option<string>("--preview")
    { Description = "Render a saved tracking JSON file through the timeline view (offline)" };
var ts = new Option<string>("--ts")
    { Description = "Timestamp; empty uses the current time", DefaultValueFactory = _ => "" };
var keySelection = new Option<int>("--key-selection")
{
    Description = "0 uses Tracker:One + Tracker:Ps1, 1 uses Tracker:Two + Tracker:Ps2",
    DefaultValueFactory = _ => 1
}.AcceptOnlyFromAmong("0", "1");

foreach (var option in new Option[] { testAuth, track, preview, ts, keySelection })
    root.Options.Add(option);

var cli = new TrackerCli(host.Services);

root.SetAction(async Task<int> (parseResult, cancellationToken) =>
{
    var previewPath = parseResult.GetValue(preview);
    if (!string.IsNullOrEmpty(previewPath))
    {
        TrackingView.Render(await File.ReadAllTextAsync(previewPath, cancellationToken));
        return 0;
    }

    var tsValue = parseResult.GetValue(ts)!;
    var keySelectionValue = parseResult.GetValue(keySelection);

    var trackingCode = parseResult.GetValue(track);
    if (!string.IsNullOrEmpty(trackingCode))
        return await cli.TrackAsync(trackingCode, tsValue, keySelectionValue, cancellationToken);

    if (parseResult.GetValue(testAuth))
        return await cli.TestAuthAsync(tsValue, keySelectionValue, cancellationToken);

    return await cli.RunInteractiveAsync(cancellationToken);
});

return await root.Parse(args).InvokeAsync();
