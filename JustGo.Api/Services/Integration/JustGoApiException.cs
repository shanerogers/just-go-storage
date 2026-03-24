namespace JustGo.Integrations.JustGo.Services;

public sealed class JustGoApiException(int statusCode, string body) : Exception(body)
{
    public int StatusCode { get; } = statusCode;
    public string Body { get; } = body;
}
