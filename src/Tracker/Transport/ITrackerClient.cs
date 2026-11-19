namespace Tracker.Transport;

using System.Text.Json;
using Refit;

// The raw HTTP surface of the upstream service. Internal: the protocol paths,
// headers and cookie live here and never leak to callers of the library.
internal interface ITrackerClient
{
    [Headers("User-Agent: Dart/3.13 (dart:io)")]
    [Post("/v4/app-validation/liberacao")]
    Task<IApiResponse<JsonElement>> ReleaseAsync([Body] object request, CancellationToken cancellationToken = default);

    [Headers("User-Agent: Dart/3.13 (dart:io)")]
    [Get("/v1/versao-atual")]
    Task<IApiResponse<JsonElement>> GetCurrentVersionAsync(
        [AliasAs("plataforma")] string platform,
        [Header("Cookie")] string cookie,
        [Header("app-correios-check-token")] string appCheck,
        CancellationToken cancellationToken = default);

    [Headers("User-Agent: Dart/3.13 (dart:io)")]
    [Get("/v1/funcionalidades")]
    Task<IApiResponse<JsonElement>> GetFeaturesAsync(
        [Header("Cookie")] string cookie,
        [Header("app-correios-check-token")] string appCheck,
        CancellationToken cancellationToken = default);

    [Headers("User-Agent: Dart/3.13 (dart:io)")]
    [Get("/v1/sro-rastro/{cod_objeto}")]
    Task<IApiResponse<JsonElement>> GetTrackingAsync(
        [AliasAs("cod_objeto")] string trackingCode,
        [Header("Cookie")] string cookie,
        [Header("app-correios-check-token")] string appCheck,
        CancellationToken cancellationToken = default);
}
