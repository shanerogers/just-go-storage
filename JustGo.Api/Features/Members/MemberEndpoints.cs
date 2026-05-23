namespace JustGo.Api.Features.Members;

public static class MemberEndpoints
{
    public static IEndpointRouteBuilder MapMemberEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/members").WithTags("Members");

        group.MapGet("/search", async (
            [AsParameters] FindMembersRequest request,
            IMemberClient client,
            CancellationToken ct) =>
        {
            var result = await client.FindMembersByAttributesAsync(request, ct);
            return Results.Ok(result);
        })
        .WithName("FindMembers")
        .WithSummary("Search members by attributes");

        group.MapGet("/{memberId:guid}", async (Guid memberId, IMemberClient client, CancellationToken ct) =>
        {
            var result = await client.GetMemberAsync(memberId, ct);
            return Results.Ok(result);
        })
        .WithName("GetMember")
        .WithSummary("Get a member by ID");

        return app;
    }
}
