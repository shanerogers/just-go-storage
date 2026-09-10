using System.Text.Json.Nodes;

namespace JustGo.Api.Features.JustGoUpstream;

/// <summary>
/// Exposes JustGo's own upstream OpenAPI spec for a second Scalar UI, alongside a same-origin
/// reverse proxy so Scalar's "Try it" requests reach the real JustGo sandbox API using this
/// application's bearer token instead of failing on browser CORS.
/// </summary>
public static class JustGoUpstreamEndpoints
{
    private const string SpecPath = "/swagger/v2.2/swagger.json";
    private const string ProxyRoutePrefix = "/justgo-proxy";

    public static IEndpointRouteBuilder MapJustGoUpstreamEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/openapi/justgo-upstream.json", GetUpstreamSpecAsync)
            .WithName("GetJustGoUpstreamSpec")
            .WithSummary("Proxies JustGo's own OpenAPI spec, rewritten so 'Try it' calls route through this API")
            .ExcludeFromDescription();

        app.Map(ProxyRoutePrefix + "/{**path}", ProxyToUpstreamAsync)
            .WithName("JustGoUpstreamProxy")
            .WithSummary("Forwards requests to the real JustGo sandbox API using this API's bearer token")
            .ExcludeFromDescription();

        return app;
    }

    private static async Task<IResult> GetUpstreamSpecAsync(IHttpClientFactory httpClientFactory, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient("JustGoUpstreamProxy");
        var specJson = await client.GetStringAsync(SpecPath, ct);

        var spec = JsonNode.Parse(specJson) ?? throw new InvalidOperationException("Upstream OpenAPI spec was empty.");
        spec["servers"] = new JsonArray(new JsonObject { ["url"] = ProxyRoutePrefix });

        return Results.Content(spec.ToJsonString(), "application/json");
    }

    private static async Task ProxyToUpstreamAsync(HttpContext context, IHttpClientFactory httpClientFactory, CancellationToken ct)
    {
        var path = context.Request.Path.ToString()[ProxyRoutePrefix.Length..];
        var target = path + context.Request.QueryString;

        var client = httpClientFactory.CreateClient("JustGoUpstreamProxy");
        using var upstreamRequest = new HttpRequestMessage(new HttpMethod(context.Request.Method), target);

        var requestHasBody = !HttpMethods.IsGet(context.Request.Method)
            && !HttpMethods.IsHead(context.Request.Method)
            && !HttpMethods.IsOptions(context.Request.Method);

        if (requestHasBody)
        {
            upstreamRequest.Content = new StreamContent(context.Request.Body);
            if (context.Request.ContentType is { } contentType)
            {
                upstreamRequest.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);
            }
        }

        using var upstreamResponse = await client
            .SendAsync(upstreamRequest, HttpCompletionOption.ResponseHeadersRead, ct)
            .ConfigureAwait(false);

        context.Response.StatusCode = (int)upstreamResponse.StatusCode;
        context.Response.ContentType = upstreamResponse.Content.Headers.ContentType?.ToString() ?? "application/json";

        await upstreamResponse.Content.CopyToAsync(context.Response.Body, ct);
    }
}
