namespace JustGo.Api.Features.Members;

public static class MemberEndpoints
{
    extension(IEndpointRouteBuilder app)

    {

        public IEndpointRouteBuilder MapMemberEndpoints()
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

            group.MapPut("/{memberId:guid}", async (Guid memberId, MemberUpdateRequest request, IMemberClient client, CancellationToken ct) =>
            {
                await client.UpdateMemberAsync(memberId, request, ct);
                return Results.NoContent();
            })
            .WithName("UpdateMember")
            .WithSummary("Update a member");

            group.MapPost("/", async (MemberCreateRequest request, IMemberClient client, CancellationToken ct) =>
            {
                var result = await client.CreateMemberAsync(request, ct);
                return Results.Created($"/members/{result.MemberId}", result);
            })
            .WithName("CreateMember")
            .WithSummary("Create a new member");

            group.MapPost("/suspend", async (MemberSuspendRequest request, IMemberClient client, CancellationToken ct) =>
            {
                await client.SuspendMemberAsync(request, ct);
                return Results.NoContent();
            })
            .WithName("SuspendMember")
            .WithSummary("Suspend or unsuspend a member");

            group.MapPost("/upload-image/{memberId:guid}", async (Guid memberId, IFormFile image, IMemberClient client, CancellationToken ct) =>
            {
                await using var stream = image.OpenReadStream();
                await client.UploadProfileImageAsync(memberId, stream, image.FileName, ct);
                return Results.NoContent();
            })
            .WithName("UploadProfileImage")
            .WithSummary("Upload a profile image for a member")
            .DisableAntiforgery();

            group.MapGet("/schema", async (IMemberClient client, CancellationToken ct) =>
            {
                var result = await client.GetSchemaAsync(ct);
                return Results.Ok(result);
            })
            .WithName("GetMemberSchema")
            .WithSummary("Get the member data schema");

            return app;
        }
    }
}
