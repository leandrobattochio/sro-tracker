namespace Tracker.Session;

using Microsoft.Extensions.Options;
using Tracker;

// Holds the current release (JWT + cookie) and reuses it until the JWT's own `exp`
// says it has (nearly) expired, instead of reserving a fresh counter and re-running
// /liberacao on every tracking call. Callers can Invalidate() if a request is rejected.
internal sealed class TrackerSession(IOptions<TrackerOptions> options, ReleaseService release)
{
    readonly TrackerOptions _options = options.Value;
    readonly SemaphoreSlim _gate = new(1, 1);

    ReleaseResult? _current;
    DateTimeOffset _expiresAt;

    public string CheckKey =>
        string.IsNullOrEmpty(_options.CheckKey)
            ? throw new InvalidOperationException("App-check key (Tracker:CheckKey) is not configured.")
            : _options.CheckKey;

    public async Task<ReleaseResult> EnsureAsync(int keySelection, string ts, CancellationToken cancellationToken = default)
    {
        if (IsValid())
            return _current!;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (IsValid())
                return _current!;

            var result = await release.ReleaseAsync(keySelection, ts, cancellationToken);
            if (!result.Success || string.IsNullOrEmpty(result.Token) || string.IsNullOrEmpty(result.Cookie))
                throw new InvalidOperationException($"Release failed (HTTP {result.StatusCode}): {result.Body}");

            _current = result;
            // Fall back to a short window when the JWT carries no readable expiry.
            _expiresAt = JwtExpiry.Read(result.Token) ?? DateTimeOffset.UtcNow + _options.FallbackSessionLifetime;
            return result;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Invalidate() => _current = null;

    // Renew a little before `exp` so an in-flight request doesn't race the expiry.
    bool IsValid() =>
        _current is not null && DateTimeOffset.UtcNow < _expiresAt - _options.RenewalSkew;
}
