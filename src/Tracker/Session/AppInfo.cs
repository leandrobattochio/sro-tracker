namespace Tracker.Session;

using Tracker;

internal sealed record AppInfo(
    string PackageName, string BuildSignature, string Version,
    string BuildNumber, string Platform, string Ts, int KeySelection)
{
    public static AppInfo From(AppMetadata app, string ts, int keySelection) => new(
        app.PackageName, app.BuildSignature, app.Version,
        app.BuildNumber, app.Platform, ts, keySelection);
}
