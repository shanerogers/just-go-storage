namespace JustGo.Integrations.JustGo.Services;

public sealed class JustGoOptions
{
    public const string SectionName = "JustGo";
    public string BaseUrl { get; set; } = string.Empty;
    /// <summary>The API secret sent to the JustGo Auth endpoint to obtain a bearer token.</summary>
    public string ApiKey { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "v2.1";
}
