using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace API;

public static class ServiceCollectionExtensions
{
    public const string DefaultBaseUrl = "http://localhost:9898";

    public static IServiceCollection AddTrackerClient(this IServiceCollection services, string? baseUrl = null)
    {
        var settings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                TypeInfoResolver = new DefaultJsonTypeInfoResolver()
            })
        };

        services
            .AddRefitGeneratedClient<ITrackerClient>(settings)
            .ConfigureHttpClient(client => client.BaseAddress = new Uri(baseUrl ?? DefaultBaseUrl))
            .AddHttpMessageHandler(() => new LegacyHeadersHandler())
            // Cookie enviado manualmente (header Cookie: LBprdint2=...), igual ao script que funciona.
            // AllowAutoRedirect=false = allow_redirects=False.
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                UseCookies = false,
                AllowAutoRedirect = false
            });

        services.AddTransient<LiberacaoService>();
        services.AddTransient<TrackingService>();

        return services;
    }
}
