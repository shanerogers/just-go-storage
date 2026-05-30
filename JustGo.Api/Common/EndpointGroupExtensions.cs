namespace JustGo.Api.Common;

public static class EndpointGroupExtensions
{
    extension(IEndpointRouteBuilder app)

    {

        public RouteGroupBuilder MapGroup(string prefix, string groupName)
        {
            return app.MapGroup(prefix).WithTags(groupName);
        }
    }
}
