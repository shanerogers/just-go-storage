using JustGo.Integrations.JustGo.Features.Credentials.Models;
using JustGo.Integrations.JustGo.Services;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace JustGo.Api.Features.Credentials;

public sealed class CredentialClient(HttpClient httpClient, IOptions<JustGoOptions> options)
    : JustGoClientBase(httpClient, options), ICredentialClient
{
    private const string PageNumber = "PageNumber";
    private const string PageSize = "PageSize";

    public Task<object> GetCredentialDefinitionsAsync(int pageNumber, int pageSize, CancellationToken ct)
    {
        var uri = QueryHelpers.AddQueryString($"/api/{ApiVersion}/Credentials/FindByAttributes",
            new Dictionary<string, string?>
            {
                [PageNumber] = pageNumber.ToString(),
                [PageSize] = pageSize.ToString()
            });
        return GetAsync<object>(uri, ct);
    }

    public Task<object> GetCredentialDefinitionByIdAsync(Guid credentialId, CancellationToken ct) =>
        GetAsync<object>($"/api/{ApiVersion}/Credentials/{credentialId}", ct);

    public Task<object> GetCredentialDetailsAsync(CancellationToken ct) =>
        GetAsync<object>($"/api/{ApiVersion}/Credentials/Member/Details", ct);

    public Task<object> FindCredentialsByAttributesAsync(FindCredentialsRequest request, CancellationToken ct)
    {
        var query = new Dictionary<string, string?>
        {
            [PageNumber] = request.PageNumber.ToString(),
            [PageSize] = request.PageSize.ToString()
        };
        if (request.MemberId is not null) query["memberId"] = request.MemberId.Value.ToString();

        var uri = QueryHelpers.AddQueryString($"/api/{ApiVersion}/Credentials/Member/FindByAttributes", query);
        return GetAsync<object>(uri, ct);
    }

    public Task<MemberCredentialCreatedResponse> CreateMemberCredentialAsync(
        Guid memberId, MemberCredentialCreateRequest request, CancellationToken ct) =>
        PostAsync<MemberCredentialCreatedResponse>($"/api/{ApiVersion}/Credentials/Member/{memberId}", request, ct);

    public Task UpdateMemberCredentialAsync(
        Guid credentialId, MemberCredentialUpdateRequest request, CancellationToken ct) =>
        PutNoContentAsync($"/api/{ApiVersion}/Credentials/member/{credentialId}", request, ct);
}
