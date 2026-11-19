using System.CommandLine;
using API;
using CLI;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory
});
builder.Services.AddTrackerClient(builder.Configuration["Tracker:BaseUrl"]);

using var host = builder.Build();

var root = new RootCommand("Cliente de linha de comando do Tracker");

var testAuth = new Option<bool>("--test-auth")
    { Description = "Gera o token com GenerateTokenWithPlainText e imprime o texto puro" };
var track = new Option<string>("--track")
    { Description = "Código do objeto a rastrear (faz liberação + funcionalidades + rastreio)" };
var ts = new Option<string>("--ts")
    { Description = "Timestamp; vazio usa o horário atual", DefaultValueFactory = _ => "" };
var keySelection = new Option<int>("--key-selection")
{
    Description = "0 usa Tracker:One + Tracker:Ps1, 1 usa Tracker:Two + Tracker:Ps2",
    DefaultValueFactory = _ => 1
}.AcceptOnlyFromAmong("0", "1");

foreach (var option in new Option[]
         {
             testAuth, track, ts, keySelection
         })
    root.Options.Add(option);

var cli = new TrackerCli(host.Services, builder.Configuration);

root.SetAction(async Task<int> (parseResult, cancellationToken) =>
{
    var tsValue = parseResult.GetValue(ts)!;
    var keySelectionValue = parseResult.GetValue(keySelection);

    var codObjeto = parseResult.GetValue(track);
    if (!string.IsNullOrEmpty(codObjeto))
        return await cli.TrackAsync(codObjeto, tsValue, keySelectionValue, cancellationToken);

    if (parseResult.GetValue(testAuth))
        return await cli.TestAuthAsync(tsValue, keySelectionValue, cancellationToken);

    return await cli.RunInteractiveAsync(cancellationToken);
});

return await root.Parse(args).InvokeAsync();
