namespace JustGo.Api.Common;

public static class EndpointGroupExtensions
{
    public static RouteGroupBuilder MapGroup(this IEndpointRouteBuilder app, string prefix, string groupName)
    {
        return app.MapGroup(prefix).WithTags(groupName);
    }
}
