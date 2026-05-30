using JustGo.Api.Data;

using JustGo.Api.Features.Credentials;

using JustGo.Api.Features.Members;

using Microsoft.EntityFrameworkCore;



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

                .WithSummary("Get synced members with current grade and eligibility for grading");



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

        ApiDbContext db,

        CancellationToken ct)

    {

        if (pageSize is < 1 or > 200) pageSize = 50;

        if (page < 1) page = 1;



        IQueryable<MemberSyncRecord> query = db.Members

            .TagWith("Get synced members for grading");



        if (!string.IsNullOrWhiteSpace(search))

        {

            var term = search.Trim().ToLower();

            query = query.Where(m =>

                (m.FirstName != null && m.FirstName.ToLower().Contains(term)) ||

                (m.LastName != null && m.LastName.ToLower().Contains(term)));

        }



        var totalCount = await query.CountAsync(ct);



        var members = await query

            .OrderBy(m => m.LastName)

            .ThenBy(m => m.FirstName)

            .Skip((page - 1) * pageSize)

            .Take(pageSize)

            .ToListAsync(ct);



        var gradingMembers = members.Select(m =>

        {

            var credentials = m.MemberInformation.Credentials;

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

                JustGoMemberId = m.JustGoMemberId,

                MemberId = m.MemberInformation.MemberId ?? m.MemberInformation.UserName ?? string.Empty,

                FirstName = m.FirstName ?? string.Empty,

                LastName = m.LastName ?? string.Empty,

                CurrentGrade = currentGrade?.Name,

                CurrentGradeDefinitionId = currentGrade?.DefinitionId,

                LastGradingDate = lastGradingDate,

                NextGrade = nextGrade?.Name,

                NextGradeDefinitionId = nextGrade?.DefinitionId,

                DoubleGrade = doubleGrade?.Name,

                DoubleGradeDefinitionId = doubleGrade?.DefinitionId,

            };

        }).ToList();



        return Results.Ok(new GradingMembersResponse

        {

            Members = gradingMembers,

            TotalCount = totalCount,

        });

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

