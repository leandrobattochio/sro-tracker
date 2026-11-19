namespace Tracker;

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Refit;
using Tracker.Session;
using Tracker.Tracking;
using Tracker.Transport;

public static class ServiceCollectionExtensions
{
    // The library's whole public surface: bind the "Tracker" section and register
    // the session and tracking services. Everything else is internal.
    public static IServiceCollection AddTracker(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<TrackerOptions>()
            .Bind(configuration.GetSection(TrackerOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<ICounterStore>(sp =>
            new FileCounterStore(sp.GetRequiredService<IOptions<TrackerOptions>>().Value.CounterFile));

        var settings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                TypeInfoResolver = new DefaultJsonTypeInfoResolver()
            })
        };

        services
            .AddRefitGeneratedClient<ITrackerClient>(settings)
            .ConfigureHttpClient((sp, client) =>
                client.BaseAddress = new Uri(sp.GetRequiredService<IOptions<TrackerOptions>>().Value.BaseUrl))
            .AddHttpMessageHandler(() => new LegacyHeadersHandler())
            // Cookie is sent by hand (Cookie: LBprdint2=...); auto redirects off, matching the legacy script.
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                UseCookies = false,
                AllowAutoRedirect = false
            });

        services.AddTransient<ReleaseService>();
        services.AddSingleton<TrackerSession>();
        services.AddTransient<ITrackingService, TrackingService>();

        return services;
    }
}
