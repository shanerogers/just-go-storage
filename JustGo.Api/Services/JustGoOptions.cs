using System.ComponentModel.DataAnnotations;

namespace JustGo.Integrations.JustGo.Services;

/// <summary>Configuration settings for the JustGo API integration.</summary>
public sealed class JustGoOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "JustGo";

    /// <summary>Base URL for the JustGo API, e.g. https://apiaus-sandbox.justgo.com</summary>
    [Required]
    public required string BaseUrl { get; init; }

    /// <summary>API secret key used to obtain a bearer token via POST /api/{ApiVersion}/Auth.</summary>
    [Required]
    public required string ApiKey { get; init; }

    /// <summary>API version segment, defaults to v2.2.</summary>
    [Required]
    public required string ApiVersion { get; init; }
}
