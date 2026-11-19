namespace Tracker.Tracking;

using Tracker.Session;
using Tracker.Transport;

// The tracking workflow. Leans on TrackerSession for a valid release and builds a
// fresh app-check header per request (new timestamp and AES key each time).
internal sealed class TrackingService(TrackerSession session, ITrackerClient client) : ITrackingService
{
    public async Task<HandshakeResult> HandshakeAsync(int keySelection, string ts,
        CancellationToken cancellationToken = default)
    {
        var release = await session.EnsureAsync(keySelection, ts, cancellationToken);
        var cookie = CookieHeader(release);

        var version = await client.GetCurrentVersionAsync("ANDROID", cookie, Header(release), cancellationToken);
        var features = await client.GetFeaturesAsync(cookie, Header(release), cancellationToken);

        return new HandshakeResult(release.StatusCode, release.Token,
            (int?)version.StatusCode ?? 0, (int?)features.StatusCode ?? 0);
    }

    public async Task<TrackResult> TrackAsync(string trackingCode, int keySelection, string ts,
        CancellationToken cancellationToken = default)
    {
        var release = await session.EnsureAsync(keySelection, ts, cancellationToken);

        var response = await client.GetTrackingAsync(trackingCode, CookieHeader(release),
            Header(release), cancellationToken);

        var status = (int?)response.StatusCode ?? 0;
        var body = response.Content is { } content ? content.GetRawText() : string.Empty;
        return new TrackResult(status, body);
    }

    static string CookieHeader(ReleaseResult release) =>
        $"{ReleaseService.CookieName}={release.Cookie}";

    string Header(ReleaseResult release) =>
        AppCheck.BuildHeader(session.CheckKey, release.Token!);
}
