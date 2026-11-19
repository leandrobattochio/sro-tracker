namespace Tracker.Session;

using Tracker;

// The app identity plus the per-call bits (timestamp, key selection) that go into
// a single request token. Built inside the library from TrackerOptions; the host
// never assembles this itself.
internal sealed record AppInfo(
    string PackageName, string BuildSignature, string Version,
    string BuildNumber, string Platform, string Ts, int KeySelection)
{
    public static AppInfo From(AppMetadata app, string ts, int keySelection) => new(
        app.PackageName, app.BuildSignature, app.Version,
        app.BuildNumber, app.Platform, ts, keySelection);
}
