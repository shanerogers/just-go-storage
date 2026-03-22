using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Web;
using Microsoft.Extensions.Options;
using JustGo.Integrations.JustGo.Features.Auth.Models;
using JustGo.Integrations.JustGo.Features.Clubs.Models;
using JustGo.Integrations.JustGo.Features.Competitions.Models;
using JustGo.Integrations.JustGo.Features.Credentials.Models;
using JustGo.Integrations.JustGo.Features.Events.Models;

namespace JustGo.Integrations.JustGo.Services;

public sealed class JustGoClient(HttpClient httpClient, IOptions<JustGoOptions> options) : IJustGoClient
{
    private readonly string _version = options.Value.ApiVersion;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // ── Auth ─────────────────────────────────────────────────────────────────

    public async Task<LoginResponse> AuthenticateAsync(LoginRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync($"/api/{_version}/Auth", new
        {
            secret = request.Secret
        }, ct);
        await EnsureSuccessAsync(response, ct);
        var raw = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var token = ExtractToken(raw);
        return new LoginResponse(token);
    }

    // ── Clubs ─────────────────────────────────────────────────────────────────

    public async Task<ClubResponse> GetClubAsync(Guid clubId, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/api/{_version}/Clubs/{clubId}", ct);
        await EnsureSuccessAsync(response, ct);
        var envelope = await response.Content.ReadFromJsonAsync<JustGoEnvelope<JustGoClubDto>>(JsonOptions, ct);
        return MapClub(envelope!.Data!);
    }

    public async Task<ClubResponse> UpdateClubAsync(Guid clubId, ClubUpdateRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync($"/api/{_version}/Clubs/{clubId}", request, JsonOptions, ct);
        await EnsureSuccessAsync(response, ct);
        var envelope = await response.Content.ReadFromJsonAsync<JustGoEnvelope<JustGoClubDto>>(JsonOptions, ct);
        return MapClub(envelope!.Data!);
    }

    public async Task<PagedClubSearchResponse> FindClubsByAttributesAsync(FindClubsRequest request, CancellationToken ct = default)
    {
        var query = BuildQuery(new Dictionary<string, string?>
        {
            ["Status"] = request.Status,
            ["Type"] = request.Type,
            ["CredentialId"] = request.CredentialId?.ToString(),
            ["Membership"] = request.Membership,
            ["Club Name"] = request.ClubName,
            ["ClubID"] = request.ClubId,
            ["ModifiedDate"] = request.ModifiedDate?.ToString("o"),
            ["PageNumber"] = request.PageNumber.ToString(),
            ["PageSize"] = request.PageSize.ToString()
        });
        var response = await httpClient.GetAsync($"/api/{_version}/Clubs/FindClubByAttributes{query}", ct);
        await EnsureSuccessAsync(response, ct);
        var paged = await response.Content.ReadFromJsonAsync<JustGoPagedEnvelope<JustGoClubDto>>(JsonOptions, ct);
        return new PagedClubSearchResponse(
            paged!.Data?.Select(MapClub).ToList() ?? [],
            paged.PageNumber, paged.PageSize, paged.TotalCount, paged.TotalPages);
    }

    public async Task<ClubMemberResponse> AddClubMemberAsync(AddClubMemberRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync($"/api/{_version}/Clubs/AddClubMember", request, JsonOptions, ct);
        await EnsureSuccessAsync(response, ct);
        var envelope = await response.Content.ReadFromJsonAsync<JustGoEnvelope<JustGoClubMemberDto>>(JsonOptions, ct);
        return MapClubMember(envelope!.Data!);
    }

    // ── Competitions ──────────────────────────────────────────────────────────

    public async Task<EntryValidationResponse> ValidateEntryAsync(EntryValidationRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync($"/api/{_version}/Competitions/EntryValidation", request, JsonOptions, ct);
        await EnsureSuccessAsync(response, ct);
        var envelope = await response.Content.ReadFromJsonAsync<JustGoEnvelope<EntryValidationDto>>(JsonOptions, ct);
        var dto = envelope!.Data!;
        return new EntryValidationResponse(dto.IsValid, dto.Message, dto.Errors);
    }

    public async Task<RankingsResponse> GetRankingsAsync(RankingsRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync($"/api/{_version}/Competitions/Rankings", request, JsonOptions, ct);
        await EnsureSuccessAsync(response, ct);
        var envelope = await response.Content.ReadFromJsonAsync<JustGoEnvelope<RankingsDto>>(JsonOptions, ct);
        var dto = envelope!.Data!;
        return new RankingsResponse(dto.MemberId, dto.Category, dto.Discipline, dto.Ranking, dto.Rating, dto.Year);
    }

    // ── Credentials ───────────────────────────────────────────────────────────

    public async Task<PagedCredentialDefinitionResponse> GetCredentialDefinitionsAsync(int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var query = BuildQuery(new Dictionary<string, string?> { ["PageNumber"] = pageNumber.ToString(), ["PageSize"] = pageSize.ToString() });
        var response = await httpClient.GetAsync($"/api/{_version}/Credentials/CredentialDefinition{query}", ct);
        await EnsureSuccessAsync(response, ct);
        var paged = await response.Content.ReadFromJsonAsync<JustGoPagedEnvelope<JustGoCredentialDefinitionDto>>(JsonOptions, ct);
        return new PagedCredentialDefinitionResponse(
            paged!.Data?.Select(MapCredentialDefinition).ToList() ?? [],
            paged.PageNumber, paged.PageSize, paged.TotalCount, paged.TotalPages);
    }

    public async Task<PagedMemberCredentialResponse> FindCredentialsByAttributesAsync(FindCredentialsRequest request, CancellationToken ct = default)
    {
        var query = BuildQuery(new Dictionary<string, string?>
        {
            ["memberId"] = request.MemberId?.ToString(),
            ["status"] = request.Status,
            ["category"] = request.Category,
            ["PageNumber"] = request.PageNumber.ToString(),
            ["PageSize"] = request.PageSize.ToString()
        });
        var response = await httpClient.GetAsync($"/api/{_version}/Credentials/FindByAttributes{query}", ct);
        await EnsureSuccessAsync(response, ct);
        var paged = await response.Content.ReadFromJsonAsync<JustGoPagedEnvelope<JustGoMemberCredentialDto>>(JsonOptions, ct);
        return new PagedMemberCredentialResponse(
            paged!.Data?.Select(MapMemberCredential).ToList() ?? [],
            paged.PageNumber, paged.PageSize, paged.TotalCount, paged.TotalPages);
    }

    public async Task<MemberCredentialResponse> CreateMemberCredentialAsync(Guid memberId, MemberCredentialCreateRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync($"/api/{_version}/Credentials/{memberId}", request, JsonOptions, ct);
        await EnsureSuccessAsync(response, ct);
        var envelope = await response.Content.ReadFromJsonAsync<JustGoEnvelope<JustGoMemberCredentialDto>>(JsonOptions, ct);
        return MapMemberCredential(envelope!.Data!);
    }

    public async Task UpdateMemberCredentialAsync(Guid credentialId, MemberCredentialUpdateRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync($"/api/{_version}/Credentials/GetByMemberId/{credentialId}", request, JsonOptions, ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task<PagedCredentialDefinitionResponse> GetCredentialDefinitionByIdAsync(Guid credentialId, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/api/{_version}/Credentials/CredentialDefinitionById/{credentialId}", ct);
        await EnsureSuccessAsync(response, ct);
        var paged = await response.Content.ReadFromJsonAsync<JustGoPagedEnvelope<JustGoCredentialDefinitionDto>>(JsonOptions, ct);
        return new PagedCredentialDefinitionResponse(
            paged!.Data?.Select(MapCredentialDefinition).ToList() ?? [],
            paged.PageNumber, paged.PageSize, paged.TotalCount, paged.TotalPages);
    }

    public async Task<CredentialDetailsResponse> GetCredentialDetailsAsync(CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/api/{_version}/Credentials/Details", ct);
        await EnsureSuccessAsync(response, ct);
        var raw = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var message = raw.TryGetProperty("message", out var msg) ? msg.GetString() : null;
        var details = raw.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Array
            ? data.EnumerateArray()
                .Select(d => new CredentialDetailItem(
                    d.TryGetProperty("key", out var k) ? k.GetString() : null,
                    d.TryGetProperty("value", out var v) ? v.GetString() : null))
                .ToList()
            : null;
        return new CredentialDetailsResponse(message, details);
    }

    // ── Events ────────────────────────────────────────────────────────────────

    public async Task<PagedEventResponse> FindEventsByAttributesAsync(FindEventsRequest request, CancellationToken ct = default)
    {
        var query = BuildQuery(new Dictionary<string, string?>
        {
            ["category"] = request.Category,
            ["subcategory"] = request.SubCategory,
            ["status"] = request.Status,
            ["ModificationDate"] = request.ModificationDate?.ToString("o"),
            ["EventReference"] = request.EventReference,
            ["PageNumber"] = request.PageNumber.ToString(),
            ["PageSize"] = request.PageSize.ToString()
        });
        var response = await httpClient.GetAsync($"/api/{_version}/Events/FindByAttributes{query}", ct);
        await EnsureSuccessAsync(response, ct);
        var paged = await response.Content.ReadFromJsonAsync<JustGoPagedEnvelope<JustGoEventDto>>(JsonOptions, ct);
        return new PagedEventResponse(
            paged!.Data?.Select(MapEvent).ToList() ?? [],
            paged.PageNumber, paged.PageSize, paged.TotalCount, paged.TotalPages);
    }

    public async Task<EventResponse> GetEventAsync(Guid eventId, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/api/{_version}/Events/{eventId}", ct);
        await EnsureSuccessAsync(response, ct);
        var envelope = await response.Content.ReadFromJsonAsync<JustGoEnvelope<JustGoEventDto>>(JsonOptions, ct);
        return MapEvent(envelope!.Data!);
    }

    public async Task<EventCreateResponse> CreateEventAsync(EventCreateRequest request, Guid? templateId = null, CancellationToken ct = default)
    {
        var url = templateId.HasValue
            ? $"/api/{_version}/Events?templateID={templateId}"
            : $"/api/{_version}/Events";
        var response = await httpClient.PostAsJsonAsync(url, request, JsonOptions, ct);
        await EnsureSuccessAsync(response, ct);
        var envelope = await response.Content.ReadFromJsonAsync<JustGoEnvelope<JustGoEventCreateResponseDto>>(JsonOptions, ct);
        return new EventCreateResponse(envelope!.Data!.EventId, envelope.Data.Name);
    }

    public async Task UpdateEventAsync(Guid eventId, EventUpdateRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync($"/api/{_version}/Events?id={eventId}", request, JsonOptions, ct);
        await EnsureSuccessAsync(response, ct);
    }

    public async Task<PagedEventTicketResponse> GetEventTicketsAsync(Guid eventId, int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var query = BuildQuery(new Dictionary<string, string?> { ["PageNumber"] = pageNumber.ToString(), ["PageSize"] = pageSize.ToString() });
        var response = await httpClient.GetAsync($"/api/{_version}/Events/{eventId}/Tickets{query}", ct);
        await EnsureSuccessAsync(response, ct);
        var paged = await response.Content.ReadFromJsonAsync<JustGoPagedEnvelope<JustGoEventTicketDto>>(JsonOptions, ct);
        return new PagedEventTicketResponse(
            paged!.Data?.Select(MapTicket).ToList() ?? [],
            paged.PageNumber, paged.PageSize, paged.TotalCount, paged.TotalPages);
    }

    // ── Event Candidates ──────────────────────────────────────────────────────

    public async Task<PagedEventCandidateResponse> FindEventCandidatesByAttributesAsync(FindEventCandidatesRequest request, CancellationToken ct = default)
    {
        var query = BuildQuery(new Dictionary<string, string?>
        {
            ["bookingStatus"] = request.BookingStatus,
            ["eventID"] = request.EventId?.ToString(),
            ["category"] = request.Category,
            ["SubCategory"] = request.SubCategory,
            ["ModificationDate"] = request.ModificationDate?.ToString("o"),
            ["PageNumber"] = request.PageNumber.ToString(),
            ["PageSize"] = request.PageSize.ToString()
        });
        var response = await httpClient.GetAsync($"/api/{_version}/Events/Candidate/FindByAttributes{query}", ct);
        await EnsureSuccessAsync(response, ct);
        var paged = await response.Content.ReadFromJsonAsync<JustGoPagedEnvelope<JustGoEventCandidateDto>>(JsonOptions, ct);
        return new PagedEventCandidateResponse(
            paged!.Data?.Select(MapCandidate).ToList() ?? [],
            paged.PageNumber, paged.PageSize, paged.TotalCount, paged.TotalPages);
    }

    public async Task<EventCandidateResponse> AddEventCandidateAsync(Guid eventId, EventCandidateCreateRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync($"/api/{_version}/Events/{eventId}/Candidates", request, JsonOptions, ct);
        await EnsureSuccessAsync(response, ct);
        var envelope = await response.Content.ReadFromJsonAsync<JustGoEnvelope<JustGoEventCandidateDto>>(JsonOptions, ct);
        return MapCandidate(envelope!.Data!);
    }

    public async Task UpdateEventCandidateStatusAsync(Guid bookingId, EventCandidateStatusUpdateRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PutAsJsonAsync($"/api/{_version}/Events/Candidates/{bookingId}", new { bookingStatus = request.BookingStatus }, JsonOptions, ct);
        await EnsureSuccessAsync(response, ct);
    }

    // ── Event Promoters ───────────────────────────────────────────────────────

    public async Task<IReadOnlyList<EventPromoterResponse>> GetEventPromotersAsync(Guid eventId, CancellationToken ct = default)
    {
        var response = await httpClient.GetAsync($"/api/{_version}/Events/{eventId}/Promoters", ct);
        await EnsureSuccessAsync(response, ct);
        var envelope = await response.Content.ReadFromJsonAsync<JustGoEnvelope<List<JustGoEventPromoterDto>>>(JsonOptions, ct);
        return envelope!.Data?.Select(MapPromoter).ToList() ?? [];
    }

    public async Task<EventPromoterResponse> AddEventPromoterAsync(Guid eventId, EventPromoterCreateRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync($"/api/{_version}/Events/{eventId}/Promoters", request, JsonOptions, ct);
        await EnsureSuccessAsync(response, ct);
        var envelope = await response.Content.ReadFromJsonAsync<JustGoEnvelope<JustGoEventPromoterDto>>(JsonOptions, ct);
        return MapPromoter(envelope!.Data!);
    }

    public async Task RemoveEventPromoterAsync(Guid eventId, int promoterId, CancellationToken ct = default)
    {
        var response = await httpClient.DeleteAsync($"/api/{_version}/Events/{eventId}/Promoters/{promoterId}", ct);
        await EnsureSuccessAsync(response, ct);
    }

    // ── Event Stages ──────────────────────────────────────────────────────────

    public async Task<IReadOnlyList<EventStageResponse>> GetEventStagesAsync(Guid eventId, int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var query = BuildQuery(new Dictionary<string, string?> { ["PageNumber"] = pageNumber.ToString(), ["PageSize"] = pageSize.ToString() });
        var response = await httpClient.GetAsync($"/api/{_version}/Events/{eventId}/Stages{query}", ct);
        await EnsureSuccessAsync(response, ct);
        var envelope = await response.Content.ReadFromJsonAsync<JustGoEnvelope<List<JustGoEventStageDto>>>(JsonOptions, ct);
        return envelope!.Data?.Select(MapStage).ToList() ?? [];
    }

    public async Task<EventStageResponse> CreateEventStageAsync(Guid eventId, EventStageCreateRequest request, CancellationToken ct = default)
    {
        var response = await httpClient.PostAsJsonAsync($"/api/{_version}/Events/{eventId}/Stages", request, JsonOptions, ct);
        await EnsureSuccessAsync(response, ct);
        var envelope = await response.Content.ReadFromJsonAsync<JustGoEnvelope<JustGoEventStageDto>>(JsonOptions, ct);
        return MapStage(envelope!.Data!);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static string BuildQuery(Dictionary<string, string?> parameters)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        foreach (var (key, value) in parameters)
            if (value is not null)
                query[key] = value;
        var qs = query.ToString();
        return string.IsNullOrEmpty(qs) ? string.Empty : $"?{qs}";
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken ct)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new JustGoApiException((int)response.StatusCode, body);
        }
    }

    private static string ExtractToken(JsonElement raw)
    {
        foreach (var prop in new[] { "token", "accessToken", "access_token", "Token", "AccessToken" })
            if (raw.TryGetProperty(prop, out var val) && val.ValueKind == JsonValueKind.String)
                return val.GetString()!;

        // Try nested data object
        if (raw.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.Object)
            return ExtractToken(data);

        throw new InvalidOperationException("Could not extract token from JustGo auth response.");
    }

    // ── Mapping methods ───────────────────────────────────────────────────────

    private static ClubResponse MapClub(JustGoClubDto d) =>
        new(d.ClubId ?? Guid.Empty, d.Name ?? string.Empty, d.Status, d.Type, d.Membership, d.ModifiedDate);

    private static ClubMemberResponse MapClubMember(JustGoClubMemberDto d) =>
        new(d.MemberId, d.ClubId, d.MembershipType, d.StartDate, d.EndDate, d.Status);

    private static CredentialDefinitionResponse MapCredentialDefinition(JustGoCredentialDefinitionDto d) =>
        new(d.CredentialId ?? Guid.Empty, d.Name, d.Category, d.Description, d.IsActive);

    private static MemberCredentialResponse MapMemberCredential(JustGoMemberCredentialDto d) =>
        new(d.CredentialId ?? Guid.Empty, d.MemberId ?? Guid.Empty, d.CredentialName, d.Status, d.IssueDate, d.ExpiryDate, d.Notes);

    private static EventResponse MapEvent(JustGoEventDto d) =>
        new(d.EventId ?? Guid.Empty, d.Name, d.Category, d.SubCategory, d.Status, d.EventReference, d.StartDate, d.EndDate, d.ModifiedDate);

    private static EventTicketResponse MapTicket(JustGoEventTicketDto d) =>
        new(d.TicketId ?? Guid.Empty, d.TicketType, d.Price, d.Quantity, d.Remaining);

    private static EventCandidateResponse MapCandidate(JustGoEventCandidateDto d) =>
        new(d.BookingId ?? Guid.Empty, d.EventId ?? Guid.Empty, d.MemberId ?? Guid.Empty, d.BookingStatus, d.Category, d.SubCategory, d.ModifiedDate);

    private static EventPromoterResponse MapPromoter(JustGoEventPromoterDto d) =>
        new(d.PromoterId, d.EventId ?? Guid.Empty, d.Name, d.ContactEmail, d.ContactPhone);

    private static EventStageResponse MapStage(JustGoEventStageDto d) =>
        new(d.StageId ?? Guid.Empty, d.EventId ?? Guid.Empty, d.Name, d.Order, d.StartDate, d.EndDate);

    // ── JustGo raw DTO types (internal) ──────────────────────────────────────

    private sealed class JustGoEnvelope<T>
    {
        [JsonPropertyName("data")] public T? Data { get; set; }
        [JsonPropertyName("message")] public string? Message { get; set; }
    }

    private sealed class JustGoPagedEnvelope<T>
    {
        [JsonPropertyName("data")] public List<T>? Data { get; set; }
        [JsonPropertyName("pageNumber")] public int PageNumber { get; set; }
        [JsonPropertyName("pageSize")] public int PageSize { get; set; }
        [JsonPropertyName("totalCount")] public int TotalCount { get; set; }
        [JsonPropertyName("totalPages")] public int TotalPages { get; set; }
    }

    private sealed class JustGoClubDto
    {
        [JsonPropertyName("clubId")] public Guid? ClubId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("membership")] public string? Membership { get; set; }
        [JsonPropertyName("modifiedDate")] public DateTimeOffset? ModifiedDate { get; set; }
    }

    private sealed class JustGoClubMemberDto
    {
        [JsonPropertyName("memberId")] public Guid? MemberId { get; set; }
        [JsonPropertyName("clubId")] public Guid? ClubId { get; set; }
        [JsonPropertyName("membershipType")] public string? MembershipType { get; set; }
        [JsonPropertyName("startDate")] public DateTimeOffset? StartDate { get; set; }
        [JsonPropertyName("endDate")] public DateTimeOffset? EndDate { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
    }

    private sealed class EntryValidationDto
    {
        [JsonPropertyName("isValid")] public bool IsValid { get; set; }
        [JsonPropertyName("message")] public string? Message { get; set; }
        [JsonPropertyName("errors")] public List<string>? Errors { get; set; }
    }

    private sealed class RankingsDto
    {
        [JsonPropertyName("memberId")] public Guid? MemberId { get; set; }
        [JsonPropertyName("category")] public string? Category { get; set; }
        [JsonPropertyName("discipline")] public string? Discipline { get; set; }
        [JsonPropertyName("ranking")] public int? Ranking { get; set; }
        [JsonPropertyName("rating")] public decimal? Rating { get; set; }
        [JsonPropertyName("year")] public int? Year { get; set; }
    }

    private sealed class JustGoCredentialDefinitionDto
    {
        [JsonPropertyName("credentialId")] public Guid? CredentialId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("category")] public string? Category { get; set; }
        [JsonPropertyName("description")] public string? Description { get; set; }
        [JsonPropertyName("isActive")] public bool? IsActive { get; set; }
    }

    private sealed class JustGoMemberCredentialDto
    {
        [JsonPropertyName("credentialId")] public Guid? CredentialId { get; set; }
        [JsonPropertyName("memberId")] public Guid? MemberId { get; set; }
        [JsonPropertyName("credentialName")] public string? CredentialName { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("issueDate")] public DateTimeOffset? IssueDate { get; set; }
        [JsonPropertyName("expiryDate")] public DateTimeOffset? ExpiryDate { get; set; }
        [JsonPropertyName("notes")] public string? Notes { get; set; }
    }

    private sealed class JustGoEventDto
    {
        [JsonPropertyName("eventId")] public Guid? EventId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("category")] public string? Category { get; set; }
        [JsonPropertyName("subCategory")] public string? SubCategory { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("eventReference")] public string? EventReference { get; set; }
        [JsonPropertyName("startDate")] public DateTimeOffset? StartDate { get; set; }
        [JsonPropertyName("endDate")] public DateTimeOffset? EndDate { get; set; }
        [JsonPropertyName("modifiedDate")] public DateTimeOffset? ModifiedDate { get; set; }
    }

    private sealed class JustGoEventCreateResponseDto
    {
        [JsonPropertyName("eventId")] public Guid EventId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
    }

    private sealed class JustGoEventTicketDto
    {
        [JsonPropertyName("ticketId")] public Guid? TicketId { get; set; }
        [JsonPropertyName("ticketType")] public string? TicketType { get; set; }
        [JsonPropertyName("price")] public decimal? Price { get; set; }
        [JsonPropertyName("quantity")] public int? Quantity { get; set; }
        [JsonPropertyName("remaining")] public int? Remaining { get; set; }
    }

    private sealed class JustGoEventCandidateDto
    {
        [JsonPropertyName("bookingId")] public Guid? BookingId { get; set; }
        [JsonPropertyName("eventId")] public Guid? EventId { get; set; }
        [JsonPropertyName("memberId")] public Guid? MemberId { get; set; }
        [JsonPropertyName("bookingStatus")] public string? BookingStatus { get; set; }
        [JsonPropertyName("category")] public string? Category { get; set; }
        [JsonPropertyName("subCategory")] public string? SubCategory { get; set; }
        [JsonPropertyName("modifiedDate")] public DateTimeOffset? ModifiedDate { get; set; }
    }

    private sealed class JustGoEventPromoterDto
    {
        [JsonPropertyName("promoterId")] public int PromoterId { get; set; }
        [JsonPropertyName("eventId")] public Guid? EventId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("contactEmail")] public string? ContactEmail { get; set; }
        [JsonPropertyName("contactPhone")] public string? ContactPhone { get; set; }
    }

    private sealed class JustGoEventStageDto
    {
        [JsonPropertyName("stageId")] public Guid? StageId { get; set; }
        [JsonPropertyName("eventId")] public Guid? EventId { get; set; }
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("order")] public int? Order { get; set; }
        [JsonPropertyName("startDate")] public DateTimeOffset? StartDate { get; set; }
        [JsonPropertyName("endDate")] public DateTimeOffset? EndDate { get; set; }
    }
}

public sealed class JustGoApiException(int statusCode, string body)
    : Exception($"JustGo API returned {statusCode}: {body}")
{
    public int StatusCode { get; } = statusCode;
    public string Body { get; } = body;
}
