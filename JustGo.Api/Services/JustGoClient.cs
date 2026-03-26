using System.Text.Json;
using JustGo.Api.Features.Members;
using JustGo.Integrations.JustGo.Features.Auth.Models;
using JustGo.Integrations.JustGo.Features.Clubs.Models;
using JustGo.Integrations.JustGo.Features.Competitions.Models;
using JustGo.Integrations.JustGo.Features.Credentials.Models;
using JustGo.Integrations.JustGo.Features.Events.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace JustGo.Integrations.JustGo.Services;

/// <summary>
/// HTTP client implementation of <see cref="IJustGoClient"/> that communicates
/// with the JustGo API using a bearer token obtained from <see cref="JustGoTokenService"/>.
/// </summary>
public sealed class JustGoClient(HttpClient httpClient, IOptions<JustGoOptions> options) : IJustGoClient
{
    private const string PageNumber = "PageNumber";
    private const string PageSize = "PageSize";
    private string ApiVersion => options.Value.ApiVersion;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // ── Members ─────────────────────────────────────────────────────────────

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public async Task<MemberDetailDto> GetMemberAsync(Guid memberId, CancellationToken ct)
    {
        var response = await GetAsync<MemberDetailResponse>($"/api/{ApiVersion}/Members/{memberId}", ct)
            .ConfigureAwait(false);
        return response.Data ?? throw new InvalidOperationException($"Null data in response for member {memberId}.");
    }

    // ── Auth ────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public Task<object> AuthenticateAsync(LoginRequest request, CancellationToken ct) =>
        PostAsync<object>($"/api/{ApiVersion}/Members/LogInCheck",
            new { userName = request.Username, password = request.Password }, ct);

    // ── Clubs ────────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public Task<object> GetClubAsync(Guid clubId, CancellationToken ct) =>
        GetAsync<object>($"/api/{ApiVersion}/Clubs/{clubId}", ct);

    /// <inheritdoc/>
    public Task<object> UpdateClubAsync(Guid clubId, ClubUpdateRequest request, CancellationToken ct) =>
        PutAsync<object>($"/api/{ApiVersion}/Clubs/{clubId}", new { clubName = request.ClubName }, ct);

    /// <inheritdoc/>
    public Task<object> FindClubsByAttributesAsync(FindClubsRequest request, CancellationToken ct)
    {
        var query = new Dictionary<string, string?>
        {
            [PageNumber] = request.PageNumber.ToString(),
            [PageSize] = request.PageSize.ToString()
        };
        if (request.ClubName is not null) query["ClubName"] = request.ClubName;

        var uri = QueryHelpers.AddQueryString($"/api/{ApiVersion}/Clubs/FindByAttributes", query);
        return GetAsync<object>(uri, ct);
    }

    /// <inheritdoc/>
    public Task<ClubMemberAddedResponse> AddClubMemberAsync(
        AddClubMemberRequest request, CancellationToken ct) =>
        PostAsync<ClubMemberAddedResponse>($"/api/{ApiVersion}/Clubs/AddClubMember",
            new { id = request.ClubId, member_Id = request.MemberId }, ct);

    // ── Competitions ─────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public Task<object> ValidateEntryAsync(EntryValidationRequest request, CancellationToken ct) =>
        PostAsync<object>($"/api/{ApiVersion}/Competitions/EntryValidation",
            new { memberId = request.MemberId, eventId = request.EventId }, ct);

    /// <inheritdoc/>
    public Task<object> GetRankingsAsync(RankingsRequest request, CancellationToken ct) =>
        PostAsync<object>($"/api/{ApiVersion}/Competitions/Rankings",
            new { memberId = request.MemberId.ToString() }, ct);

    // ── Credentials ──────────────────────────────────────────────────────────

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public Task<object> GetCredentialDefinitionByIdAsync(Guid credentialId, CancellationToken ct) =>
        GetAsync<object>($"/api/{ApiVersion}/Credentials/{credentialId}", ct);

    /// <inheritdoc/>
    public Task<object> GetCredentialDetailsAsync(CancellationToken ct) =>
        GetAsync<object>($"/api/{ApiVersion}/Credentials/Member/Details", ct);

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public Task<MemberCredentialCreatedResponse> CreateMemberCredentialAsync(
        Guid memberId, MemberCredentialCreateRequest request, CancellationToken ct) =>
        PostAsync<MemberCredentialCreatedResponse>($"/api/{ApiVersion}/Credentials/Member/{memberId}", request, ct);

    /// <inheritdoc/>
    public async Task UpdateMemberCredentialAsync(
        Guid credentialId, MemberCredentialUpdateRequest request, CancellationToken ct)
    {
        var response = await httpClient.PutAsJsonAsync(
            $"/api/{ApiVersion}/Credentials/member/{credentialId}", request, ct).ConfigureAwait(false);
        await EnsureSuccessAsync(response, ct);
    }

    // ── Events ───────────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public Task<object> FindEventsByAttributesAsync(FindEventsRequest request, CancellationToken ct)
    {
        var query = new Dictionary<string, string?>
        {
            ["PageNumber"] = request.PageNumber.ToString(),
            ["PageSize"] = request.PageSize.ToString()
        };
        if (request.Name is not null) query["EventName"] = request.Name;

        var uri = QueryHelpers.AddQueryString($"/api/{ApiVersion}/Events/FindByAttributes", query);
        return GetAsync<object>(uri, ct);
    }

    /// <inheritdoc/>
    public Task<object> GetEventAsync(Guid eventId, CancellationToken ct) =>
        GetAsync<object>($"/api/{ApiVersion}/Events/{eventId}", ct);

    /// <inheritdoc/>
    public Task<EventCreatedResponse> CreateEventAsync(
        EventCreateRequest request, Guid? templateId, CancellationToken ct)
    {
        var path = templateId is not null
            ? $"/api/{ApiVersion}/Events?templateId={templateId}"
            : $"/api/{ApiVersion}/Events";
        return PostAsync<EventCreatedResponse>(path, new { eventName = request.Name }, ct);
    }

    /// <inheritdoc/>
    public async Task UpdateEventAsync(Guid eventId, EventUpdateRequest request, CancellationToken ct)
    {
        var uri = QueryHelpers.AddQueryString($"/api/{ApiVersion}/Events",
            new Dictionary<string, string?> { ["id"] = eventId.ToString() });
        var response = await httpClient.PutAsJsonAsync(uri, new { eventName = request.Name }, ct)
            .ConfigureAwait(false);
        await EnsureSuccessAsync(response, ct);
    }

    /// <inheritdoc/>
    public Task<object> GetEventTicketsAsync(Guid eventId, int pageNumber, int pageSize, CancellationToken ct)
    {
        var uri = QueryHelpers.AddQueryString($"/api/{ApiVersion}/Events/{eventId}/Tickets",
            new Dictionary<string, string?>
            {
                ["PageNumber"] = pageNumber.ToString(),
                ["PageSize"] = pageSize.ToString()
            });
        return GetAsync<object>(uri, ct);
    }

    // ── Event Candidates ──────────────────────────────────────────────────────

    /// <inheritdoc/>
    public Task<object> FindEventCandidatesByAttributesAsync(FindEventCandidatesRequest request, CancellationToken ct)
    {
        var query = new Dictionary<string, string?>
        {
            [PageNumber] = request.PageNumber.ToString(),
            [PageSize] = request.PageSize.ToString()
        };
        if (request.EventId is not null) query["Id"] = request.EventId.Value.ToString();

        var uri = QueryHelpers.AddQueryString($"/api/{ApiVersion}/Events/Candidate/FindByAttributes", query);
        return GetAsync<object>(uri, ct);
    }

    /// <inheritdoc/>
    public Task<EventCandidateCreatedResponse> AddEventCandidateAsync(
        Guid eventId, EventCandidateCreateRequest request, CancellationToken ct) =>
        PostAsync<EventCandidateCreatedResponse>($"/api/{ApiVersion}/Events/Candidates",
            new { candidateId = request.MemberId, ticketId = eventId }, ct);

    /// <inheritdoc/>
    public async Task UpdateEventCandidateStatusAsync(
        Guid bookingId, EventCandidateStatusUpdateRequest request, CancellationToken ct)
    {
        var response = await httpClient.PutAsJsonAsync(
            $"/api/{ApiVersion}/Events/Candidates/{bookingId}", new { status = request.Status }, ct)
            .ConfigureAwait(false);
        await EnsureSuccessAsync(response, ct);
    }

    // ── Event Promoters ───────────────────────────────────────────────────────

    /// <inheritdoc/>
    public Task<object> GetEventPromotersAsync(Guid eventId, CancellationToken ct) =>
        GetAsync<object>($"/api/{ApiVersion}/Events/{eventId}/Promoters", ct);

    /// <inheritdoc/>
    public Task<EventPromoterCreatedResponse> AddEventPromoterAsync(
        Guid eventId, EventPromoterCreateRequest request, CancellationToken ct) =>
        PostAsync<EventPromoterCreatedResponse>($"/api/{ApiVersion}/Events/{eventId}/Promoters",
            new { promoterId = request.PromoterId }, ct);

    /// <inheritdoc/>
    public async Task RemoveEventPromoterAsync(Guid eventId, int promoterId, CancellationToken ct)
    {
        var response = await httpClient
            .DeleteAsync($"/api/{ApiVersion}/Events/{eventId}/Promoters/{promoterId}", ct)
            .ConfigureAwait(false);
        await EnsureSuccessAsync(response, ct);
    }

    // ── Event Stages ──────────────────────────────────────────────────────────

    /// <inheritdoc/>
    public Task<object> GetEventStagesAsync(Guid eventId, int pageNumber, int pageSize, CancellationToken ct)
    {
        var uri = QueryHelpers.AddQueryString($"/api/{ApiVersion}/Events/{eventId}/Stages",
            new Dictionary<string, string?>
            {
                [PageNumber] = pageNumber.ToString(),
                [PageSize] = pageSize.ToString()
            });
        return GetAsync<object>(uri, ct);
    }

    /// <inheritdoc/>
    public Task<EventStageCreatedResponse> CreateEventStageAsync(
        Guid eventId, EventStageCreateRequest request, CancellationToken ct) =>
        PostAsync<EventStageCreatedResponse>($"/api/{ApiVersion}/Events/{eventId}/Stages",
            new { fixtureName = request.Name }, ct);

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<T> GetAsync<T>(string uri, CancellationToken ct)
    {
        var response = await httpClient.GetAsync(uri, ct).ConfigureAwait(false);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct).ConfigureAwait(false)
               ?? throw new InvalidOperationException($"Null response from GET {uri}.");
    }

    private async Task<T> PostAsync<T>(string uri, object body, CancellationToken ct)
    {
        var response = await httpClient.PostAsJsonAsync(uri, body, ct).ConfigureAwait(false);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct).ConfigureAwait(false)
               ?? throw new InvalidOperationException($"Null response from POST {uri}.");
    }

    private async Task<T> PutAsync<T>(string uri, object body, CancellationToken ct)
    {
        var response = await httpClient.PutAsJsonAsync(uri, body, ct).ConfigureAwait(false);
        await EnsureSuccessAsync(response, ct);
        return await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct).ConfigureAwait(false)
               ?? throw new InvalidOperationException($"Null response from PUT {uri}.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode) return;
        var body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        throw new JustGoApiException((int)response.StatusCode, body);
    }
}
