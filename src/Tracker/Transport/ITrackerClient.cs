namespace Tracker.Transport;

using System.Text.Json;
using Refit;

internal interface ITrackerClient
{
    [Headers("User-Agent: Dart/3.13 (dart:io)")]
    [Post("/v4/app-validation/liberacao")]
    Task<IApiResponse<JsonElement>> ReleaseAsync([Body] object request, CancellationToken cancellationToken = default);

    [Headers("User-Agent: Dart/3.13 (dart:io)")]
    [Get("/v1/versao-atual")]
    Task<IApiResponse<JsonElement>> GetCurrentVersionAsync(
        [AliasAs("plataforma")] string platform,
        [Header("app-correios-check-token")] string appCheck,
        CancellationToken cancellationToken = default);

    [Headers("User-Agent: Dart/3.13 (dart:io)")]
    [Get("/v1/funcionalidades")]
    Task<IApiResponse<JsonElement>> GetFeaturesAsync(
        [Header("app-correios-check-token")] string appCheck,
        CancellationToken cancellationToken = default);

    [Headers("User-Agent: Dart/3.13 (dart:io)")]
    [Get("/v1/sro-rastro/{cod_objeto}")]
    Task<IApiResponse<JsonElement>> GetTrackingAsync(
        [AliasAs("cod_objeto")] string trackingCode,
        [Header("app-correios-check-token")] string appCheck,
        CancellationToken cancellationToken = default);
}
