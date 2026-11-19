using System.Text.Json;
using Refit;

namespace API;

public interface ITrackerClient
{
    [Headers("User-Agent: Dart/3.13 (dart:io)")]
    [Post("/v4/app-validation/liberacao")]
    Task<IApiResponse<JsonElement>> LiberacaoAsync([Body] object request, CancellationToken cancellationToken = default);

    [Headers("User-Agent: Dart/3.13 (dart:io)")]
    [Get("/v1/versao-atual")]
    Task<IApiResponse<JsonElement>> GetVersaoAtualAsync(
        [AliasAs("plataforma")] string plataforma,
        [Header("Cookie")] string cookie,
        [Header("app-correios-check-token")] string appChecker,
        CancellationToken cancellationToken = default);

    [Headers("User-Agent: Dart/3.13 (dart:io)")]
    [Get("/v1/funcionalidades")]
    Task<IApiResponse<JsonElement>> GetFuncionalidadesAsync(
        [Header("Cookie")] string cookie,
        [Header("app-correios-check-token")] string appChecker,
        CancellationToken cancellationToken = default);

    [Headers("User-Agent: Dart/3.13 (dart:io)")]
    [Get("/v1/sro-rastro/{cod_objeto}")]
    Task<IApiResponse<JsonElement>> GetRastroAsync(
        [AliasAs("cod_objeto")] string codObjeto,
        [Header("Cookie")] string cookie,
        [Header("app-correios-check-token")] string appChecker,
        CancellationToken cancellationToken = default);
}
