using JustGo.Api.Features.Credentials;
using JustGo.Api.Features.Members;

namespace JustGo.Api.Features.Grading;

public static class GradingEndpoints

{

    extension(IEndpointRouteBuilder app)

    {

        public IEndpointRouteBuilder MapGradingEndpoints()

        {

            var group = app.MapGroup("/grading").WithTags("Grading");



            group.MapGet("/grades", () => Results.Ok(GradeDefinitions.All))

                .WithName("GetGradeDefinitions")

                .WithSummary("Get all Gup and Dan grade definitions in rank order");



            group.MapGet("/members", GetGradingMembersAsync)

                .WithName("GetGradingMembers")

                .WithSummary("Get API members with current grade and eligibility for grading");



            group.MapPost("/submit", SubmitGradingAsync)

                .WithName("SubmitGrading")

                .WithSummary("Issue credentials for graded members");



            return app;

        }

    }



    private static async Task<IResult> GetGradingMembersAsync(

        string? search,

        int page,

        int pageSize,

        IMemberClient memberClient,

        CancellationToken ct)

    {

        if (pageSize is < 1 or > 200) pageSize = 50;

        if (page < 1) page = 1;



        var memberSearchRequest = new FindMembersRequest
        {
            PageNumber = page,
            PageSize = pageSize,
            LastName = string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
        };

        var memberSearchResponse = await memberClient.FindMembersByAttributesAsync(memberSearchRequest, ct);
        var memberRows = memberSearchResponse.Data ?? [];

        var memberDetails = await Task.WhenAll(
            memberRows.Select(member => memberClient.GetMemberAsync(member.Id, ct)));

        var gradingMembers = memberDetails
            .Select(ToGradingMemberDto)
            .OrderBy(member => member.LastName)
            .ThenBy(member => member.FirstName)
            .ToList();



        return Results.Ok(new GradingMembersResponse

        {

            Members = gradingMembers,

            TotalCount = memberSearchResponse.TotalRecords,

        });

    }

    private static GradingMemberDto ToGradingMemberDto(MemberDetailDto member)
    {
        var credentials = member.Credentials;
        var currentGrade = GradeDefinitions.GetCurrentGrade(credentials);
        var lastGradingDate = GradeDefinitions.GetLastGradingDate(credentials);
        var nextGrade = currentGrade is not null
            ? GradeDefinitions.GetNextGrade(currentGrade.DefinitionId)
            : GradeDefinitions.All[0]; // 10th Gup for ungraded members
        var doubleGrade = currentGrade is not null
            ? GradeDefinitions.GetDoubleGrade(currentGrade.DefinitionId)
            : GradeDefinitions.All.Count > 1 ? GradeDefinitions.All[1] : null;

        return new GradingMemberDto
        {
            JustGoMemberId = member.Id,
            MemberId = member.MemberId ?? member.UserName ?? string.Empty,
            FirstName = member.FirstName ?? string.Empty,
            LastName = member.LastName ?? string.Empty,
            CurrentGrade = currentGrade?.Name,
            CurrentGradeDefinitionId = currentGrade?.DefinitionId,
            LastGradingDate = lastGradingDate,
            NextGrade = nextGrade?.Name,
            NextGradeDefinitionId = nextGrade?.DefinitionId,
            DoubleGrade = doubleGrade?.Name,
            DoubleGradeDefinitionId = doubleGrade?.DefinitionId,
        };
    }



    private static async Task<IResult> SubmitGradingAsync(

        GradingSubmitRequest request,

        ICredentialClient credentialClient,

        ILogger<GradingSubmitRequest> logger,

        CancellationToken ct)

    {

        if (request.Results.Count == 0)

            return Results.BadRequest("No grading results to submit.");



        var details = new List<GradingResultStatus>();

        var succeeded = 0;

        var failed = 0;



        foreach (var item in request.Results)

        {

            var gradeName = GradeDefinitions.GetGradeName(item.CredentialDefinitionId);

            if (gradeName is null)

            {

                details.Add(new GradingResultStatus

                {

                    MemberId = item.MemberId,

                    GradeName = item.GradeName,

                    Success = false,

                    Error = $"Unknown credential definition ID: {item.CredentialDefinitionId}",

                });

                failed++;

                continue;

            }



            try

            {

                var createRequest = new Integrations.JustGo.Features.Credentials.Models.MemberCredentialCreateRequest

                {

                    CredentialType = gradeName,

                    Value = gradeName,

                };



                await credentialClient.CreateMemberCredentialAsync(item.MemberId, createRequest, ct);



                details.Add(new GradingResultStatus

                {

                    MemberId = item.MemberId,

                    GradeName = gradeName,

                    Success = true,

                });

                succeeded++;



                logger.LogInformation(

                    "Issued credential {GradeName} to member {MemberId} for event {EventName} on {EventDate}.",

                    gradeName, item.MemberId, request.EventName, request.EventDate);

            }

            catch (Exception ex)

            {

                details.Add(new GradingResultStatus

                {

                    MemberId = item.MemberId,

                    GradeName = gradeName,

                    Success = false,

                    Error = ex.Message,

                });

                failed++;



                logger.LogError(ex,

                    "Failed to issue credential {GradeName} to member {MemberId}.",

                    gradeName, item.MemberId);

            }

        }



        return Results.Ok(new GradingSubmitResponse

        {

            Succeeded = succeeded,

            Failed = failed,

            Details = details,

        });

    }

}
