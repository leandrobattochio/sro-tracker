namespace Tracker.Tracking;

// The library's public tracking surface. Nothing here references the upstream
// protocol — paths, headers, cookies and the session all stay internal.
public interface ITrackingService
{
    // Runs the release handshake (release + version + feature gates) and reports each status.
    Task<HandshakeResult> HandshakeAsync(int keySelection, string ts, CancellationToken cancellationToken = default);

    // Looks up a tracking code, establishing or reusing a session as needed.
    Task<TrackResult> TrackAsync(string trackingCode, int keySelection, string ts, CancellationToken cancellationToken = default);
}
