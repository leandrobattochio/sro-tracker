namespace Tracker;

// Everything the client needs, bound from the "Tracker" configuration section.
// Replaces the scattered config["Tracker:..."] string lookups.
public sealed class TrackerOptions
{
    public const string SectionName = "Tracker";

    public string BaseUrl { get; set; } = "http://localhost:9898";
    public string CounterFile { get; set; } = "counter.txt";

    // Renew the session this long before the JWT's `exp`, so an in-flight request
    // doesn't race the expiry.
    public TimeSpan RenewalSkew { get; set; } = TimeSpan.FromSeconds(30);

    // Used only when the release JWT has no readable `exp` claim.
    public TimeSpan FallbackSessionLifetime { get; set; } = TimeSpan.FromMinutes(2);

    // HOTP secrets, selected by key selection (0 -> One, 1 -> Two).
    public string One { get; set; } = "";
    public string Two { get; set; } = "";

    // RSA public keys that encrypt the request token (0 -> Ps1, 1 -> Ps2).
    public string Ps1 { get; set; } = "";
    public string Ps2 { get; set; } = "";

    // RSA public key for the per-request app-check header.
    public string CheckKey { get; set; } = "";

    public AppMetadata App { get; set; } = new();

    public string SecretFor(int keySelection) => keySelection == 0 ? One : Two;
}

public sealed class AppMetadata
{
    public string PackageName { get; set; } = "";
    public string BuildSignature { get; set; } = "";
    public string Version { get; set; } = "";
    public string BuildNumber { get; set; } = "";
    public string Platform { get; set; } = "";
}
