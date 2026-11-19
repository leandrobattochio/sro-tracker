namespace API;

using Microsoft.Extensions.Configuration;

public sealed record HandshakeResult(int Liberacao, string? Token, int VersaoAtual, int Funcionalidades)
{
    public bool Sucesso =>
        Liberacao == 201 && VersaoAtual is >= 200 and < 300 && Funcionalidades is >= 200 and < 300;
}

public sealed record TrackResult(int StatusCode, string Body);

public sealed class TrackingService(IConfiguration config, ITrackerClient client, LiberacaoService liberacao)
{
    // Liberação -> versao-atual -> funcionalidades, na ordem do legado. É o que o --test-auth exercita.
    public async Task<HandshakeResult> HandshakeAsync(long counter, AppInfo app,
        CancellationToken cancellationToken = default)
    {
        var (lib, _, _, versao, funcionalidades) = await DoHandshakeAsync(counter, app, cancellationToken);
        return new HandshakeResult(lib.StatusCode, lib.Token, versao, funcionalidades);
    }

    // Handshake completo + o GET de rastreio.
    public async Task<TrackResult> TrackAsync(string codObjeto, long counter, AppInfo app,
        CancellationToken cancellationToken = default)
    {
        var (lib, cookie, checkKey, _, _) = await DoHandshakeAsync(counter, app, cancellationToken);

        var rastro = await client.GetRastroAsync(codObjeto, cookie,
            AppChecker.BuildHeader(checkKey, lib.Token!), cancellationToken);

        var status = (int?)rastro.StatusCode ?? 0;
        var body = rastro.Content is { } content ? content.GetRawText() : string.Empty;

        return new TrackResult(status, body);
    }

    async Task<(LiberacaoResult Lib, string Cookie, string CheckKey, int Versao, int Funcionalidades)> DoHandshakeAsync(
        long counter, AppInfo app, CancellationToken cancellationToken)
    {
        var lib = await liberacao.AuthAsync(counter, app, cancellationToken);
        if (!lib.Sucesso || string.IsNullOrEmpty(lib.Token) || string.IsNullOrEmpty(lib.Cookie))
            throw new InvalidOperationException($"Liberação falhou (HTTP {lib.StatusCode}): {lib.Body}");

        var checkKey = config["Tracker:CheckKey"];
        if (string.IsNullOrEmpty(checkKey))
            throw new InvalidOperationException("Chave do app-checker (Tracker:CheckKey) não configurada.");

        var cookie = $"{LiberacaoService.CookieName}={lib.Cookie}";

        // Header fresco por request: timestamp e chave AES novos a cada chamada.
        string Header() => AppChecker.BuildHeader(checkKey, lib.Token!);

        var versao = await client.GetVersaoAtualAsync("ANDROID", cookie, Header(), cancellationToken);
        var funcionalidades = await client.GetFuncionalidadesAsync(cookie, Header(), cancellationToken);

        return (lib, cookie, checkKey, (int?)versao.StatusCode ?? 0, (int?)funcionalidades.StatusCode ?? 0);
    }
}
