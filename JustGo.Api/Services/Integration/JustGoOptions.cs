using System.ComponentModel.DataAnnotations;

namespace JustGo.Integrations.JustGo.Services;

/// <summary>Configuration settings for the JustGo API integration.</summary>
public sealed class JustGoOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "JustGo";

    /// <summary>Base URL for the JustGo API, e.g. https://apiaus-sandbox.justgo.com</summary>
    [Required]
    public string BaseUrl { get; init; } = string.Empty;

    /// <summary>API secret key used to obtain a bearer token via POST /api/{ApiVersion}/Auth.</summary>
    [Required]
    public string ApiKey { get; init; } = string.Empty;

    /// <summary>API version segment, defaults to v2.2.</summary>
    public string ApiVersion { get; init; } = "v2.2";
}
