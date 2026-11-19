namespace Tracker.Tracking;

using Session;
using Transport;

// The tracking workflow. Leans on TrackerSession for a valid release and builds a
// fresh app-check header per request (new timestamp and AES key each time).
internal sealed class TrackingService(TrackerSession session, ITrackerClient client) : ITrackingService
{
    public async Task<HandshakeResult> HandshakeAsync(int keySelection, string ts,
        CancellationToken cancellationToken = default)
    {
        var release = await session.EnsureAsync(keySelection, ts, cancellationToken);

        var version = await client.GetCurrentVersionAsync("ANDROID", Header(release), cancellationToken);
        var features = await client.GetFeaturesAsync(Header(release), cancellationToken);

        return new HandshakeResult(release.StatusCode, release.Token,
            (int?)version.StatusCode ?? 0, (int?)features.StatusCode ?? 0);
    }

    public async Task<TrackResult> TrackAsync(string trackingCode, int keySelection, string ts,
        CancellationToken cancellationToken = default)
    {
        var result = await TrackOnceAsync(trackingCode, keySelection, ts, cancellationToken);
        
        if (!IsSessionRejected(result.StatusCode)) return result;
        
        // 401/403 can mean the session died before its JWT exp; drop it and release once.
        // The guard above spares other statuses (e.g. 404) so we don't burn a counter.
        session.Invalidate();
        result = await TrackOnceAsync(trackingCode, keySelection, ts, cancellationToken);

        return result;
    }

    private async Task<TrackResult> TrackOnceAsync(string trackingCode, int keySelection, string ts,
        CancellationToken cancellationToken)
    {
        var release = await session.EnsureAsync(keySelection, ts, cancellationToken);

        var response = await client.GetTrackingAsync(trackingCode, Header(release), cancellationToken);

        var status = (int?)response.StatusCode ?? 0;
        var body = response.Content is { } content ? content.GetRawText() : string.Empty;
        return new TrackResult(status, body);
    }

    private static bool IsSessionRejected(int status) => status is 401 or 403;

    private string Header(ReleaseResult release) =>
        AppCheck.BuildHeader(session.CheckKey, release.Token!);
}
