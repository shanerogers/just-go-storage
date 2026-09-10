namespace JustGo.Integrations.JustGo.Services;

public sealed class JustGoApiException(int statusCode, string body, string? method = null, string? requestUri = null)
    : Exception(FormatMessage(statusCode, body, method, requestUri))
{
    public int StatusCode { get; } = statusCode;
    public string Body { get; } = body;
    public string? Method { get; } = method;
    public string? RequestUri { get; } = requestUri;

    private static string FormatMessage(int statusCode, string body, string? method, string? requestUri)
    {
        var target = method is not null && requestUri is not null ? $"{method} {requestUri}" : "unknown request";
        return $"JustGo API call failed ({statusCode}) for {target}: {body}";
    }
}
