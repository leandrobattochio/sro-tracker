namespace Tracker.Tracking;

// The handshake the app runs before it is allowed to track: release, then the
// version and feature gates. Reported as-is so the host can show each status.
public sealed record HandshakeResult(int Release, string? Token, int CurrentVersion, int Features)
{
    public bool Success =>
        Release == 201 && CurrentVersion is >= 200 and < 300 && Features is >= 200 and < 300;
}

// Result of a tracking lookup. Body is still the upstream JSON; mapping it onto a
// typed Shipment/TrackingEvent model is a follow-up once the response is captured.
public sealed record TrackResult(int StatusCode, string Body)
{
    public bool Success => StatusCode is >= 200 and < 300;
}
