using API;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CLI;

public sealed class TrackerCli(IServiceProvider services, IConfiguration config)
{
    public async Task<int> TestAuthAsync(string ts, int keySelection, CancellationToken cancellationToken)
    {
        var tracking = services.GetRequiredService<TrackingService>();
        var result = await tracking.HandshakeAsync(NextCounter(), BuildApp(ts, keySelection), cancellationToken);

        Console.WriteLine($"POST /v4/app-validation/liberacao  HTTP {result.Liberacao}");
        Console.WriteLine($"token: {result.Token}");
        Console.WriteLine($"GET  /v1/versao-atual               HTTP {result.VersaoAtual}");
        Console.WriteLine($"GET  /v1/funcionalidades            HTTP {result.Funcionalidades}");
        return result.Sucesso ? 0 : 1;
    }

    public async Task<int> TrackAsync(string codObjeto, string ts, int keySelection, CancellationToken cancellationToken)
    {
        var tracking = services.GetRequiredService<TrackingService>();
        var rastro = await tracking.TrackAsync(codObjeto, NextCounter(), BuildApp(ts, keySelection), cancellationToken);

        Console.WriteLine($"GET /v1/sro-rastro/{codObjeto} HTTP {rastro.StatusCode}: {rastro.Body}");
        return rastro.StatusCode is >= 200 and < 300 ? 0 : 1;
    }

    public async Task<int> RunInteractiveAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("Tracker CLI");
            Console.WriteLine("  1) Testar autenticação (liberação)");
            Console.WriteLine("  2) Rastrear um objeto");
            Console.WriteLine("  0) Sair");
            Console.Write("Escolha: ");

            switch (Console.ReadLine()?.Trim())
            {
                case "1":
                    await TestAuthAsync("", 1, cancellationToken);
                    break;
                case "2":
                    Console.Write("Código do objeto: ");
                    var codObjeto = Console.ReadLine()?.Trim();
                    if (string.IsNullOrEmpty(codObjeto))
                        Console.WriteLine("Código vazio, voltando ao menu.");
                    else
                        await TrackAsync(codObjeto, "", 1, cancellationToken);
                    break;
                case "0" or "" or null:
                    return 0;
                default:
                    Console.WriteLine("Opção inválida.");
                    break;
            }
        }
    }

    AppInfo BuildApp(string ts, int keySelection) => new(
        config["Tracker:App:PackageName"] ?? "",
        config["Tracker:App:BuildSignature"] ?? "",
        config["Tracker:App:Version"] ?? "",
        config["Tracker:App:BuildNumber"] ?? "",
        config["Tracker:App:Platform"] ?? "",
        ts, keySelection);

    long NextCounter() => CounterContext.ReserveCounter(config["Tracker:CounterFile"] ?? "counter.txt");
}
