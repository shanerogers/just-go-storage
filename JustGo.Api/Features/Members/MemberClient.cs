using JustGo.Integrations.JustGo.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace JustGo.Api.Features.Members;

public sealed class MemberClient(HttpClient httpClient, IOptions<JustGoOptions> options)
    : JustGoClientBase(httpClient, options), IMemberClient
{
    private const string PageNumber = "PageNumber";
    private const string PageSize = "PageSize";

    public Task<MembersPagedResponse> FindMembersByAttributesAsync(
        FindMembersRequest request, CancellationToken ct)
    {
        var query = new Dictionary<string, string?>();

        if (request.Email is not null) query["Email"] = request.Email;
        if (request.MemberId is not null) query["MemberId"] = request.MemberId;
        if (request.LoginId is not null) query["LoginId"] = request.LoginId;
        if (request.LastName is not null) query["LastName"] = request.LastName;
        if (request.ClubId is not null) query["ClubId"] = request.ClubId.Value.ToString();
        if (request.CredentialId is not null) query["CredentialId"] = request.CredentialId.Value.ToString();
        if (request.EventId is not null) query["EventId"] = request.EventId.Value.ToString();
        if (request.Membership is not null) query["Membership"] = request.Membership;
        if (request.SuspendStatus is not null) query["SuspendStatus"] = request.SuspendStatus;
        if (request.ModifiedBefore is not null) query["ModifiedBefore"] = request.ModifiedBefore.Value.ToString("O");
        if (request.ModifiedAfter is not null) query["ModifiedAfter"] = request.ModifiedAfter.Value.ToString("O");

        query[PageNumber] = request.PageNumber.ToString();
        query[PageSize] = request.PageSize.ToString();

        var uri = QueryHelpers.AddQueryString($"/api/{ApiVersion}/Members/FindByAttributes", query);

        return GetAsync<MembersPagedResponse>(uri, ct);
    }

    public async Task<MemberDetailDto> GetMemberAsync(Guid memberId, CancellationToken ct)
    {
        var response = await GetAsync<MemberDetailResponse>($"/api/{ApiVersion}/Members/{memberId}", ct)
            .ConfigureAwait(false);
        return response.Data ?? throw new InvalidOperationException($"Null data in response for member {memberId}.");
    }

    public Task UpdateMemberAsync(Guid memberId, MemberUpdateRequest request, CancellationToken ct) =>
        PutNoContentAsync($"/api/{ApiVersion}/Members/{memberId}", request, ct);

    public Task<MemberCreatedResponse> CreateMemberAsync(MemberCreateRequest request, CancellationToken ct) =>
        PostAsync<MemberCreatedResponse>($"/api/{ApiVersion}/Members", request, ct);

    public Task SuspendMemberAsync(MemberSuspendRequest request, CancellationToken ct) =>
        PostNoContentAsync($"/api/{ApiVersion}/Members/Suspend", request, ct);

    public async Task UploadProfileImageAsync(Guid memberId, Stream image, string fileName, CancellationToken ct)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(image), "image", fileName);
        await PostFormAsync($"/api/{ApiVersion}/Members/UploadProfileImage/{memberId}", content, ct);
    }

    public Task<object> GetSchemaAsync(CancellationToken ct) =>
        GetAsync<object>($"/api/{ApiVersion}/Members/Schema", ct);
}
