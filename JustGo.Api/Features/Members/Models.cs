using System.Text.Json.Serialization;

namespace JustGo.Api.Features.Members;

/// <summary>
/// Domain object representing the JustGo member data as returned by
/// GET /api/v2.2/Members/FindByAttributes (MemberListV2_2Dto shape).
/// Stored as JSONB in <see cref="MemberSyncRecord.RawData"/>.
/// </summary>
public sealed class JustGoMemberDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("memberDocId")]
    public int MemberDocId { get; set; }

    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("emailAddress")]
    public string? EmailAddress { get; set; }

    [JsonPropertyName("memberId")]
    public string? MemberId { get; set; }

    [JsonPropertyName("dob")]
    public DateOnly Dob { get; set; }

    [JsonPropertyName("gender")]
    public string? Gender { get; set; }

    [JsonPropertyName("address1")]
    public string? Address1 { get; set; }

    [JsonPropertyName("address2")]
    public string? Address2 { get; set; }

    [JsonPropertyName("town")]
    public string? Town { get; set; }

    [JsonPropertyName("county")]
    public string? County { get; set; }

    [JsonPropertyName("postCode")]
    public string? PostCode { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }

    [JsonPropertyName("phoneNumber")]
    public string? PhoneNumber { get; set; }

    [JsonPropertyName("memberStatus")]
    public string? MemberStatus { get; set; }

    [JsonPropertyName("registerDate")]
    public DateTimeOffset RegisterDate { get; set; }

    [JsonPropertyName("lastModificationDate")]
    public DateTimeOffset LastModificationDate { get; set; }

    [JsonPropertyName("userName")]
    public string? UserName { get; set; }

    [JsonPropertyName("parentFirstName")]
    public string? ParentFirstName { get; set; }

    [JsonPropertyName("parentLastName")]
    public string? ParentLastName { get; set; }

    [JsonPropertyName("parentEmailAddress")]
    public string? ParentEmailAddress { get; set; }

    [JsonPropertyName("suspensionLevel")]
    public string? SuspensionLevel { get; set; }

    [JsonPropertyName("optin")]
    public object? Optin { get; set; }
}

/// <summary>
/// EF Core entity representing a locally synced JustGo member record.
/// </summary>
public sealed class MemberSyncRecord
{
    public Guid Id { get; set; }

    /// <summary>The member's GUID from the JustGo API (unique).</summary>
    public Guid JustGoMemberId { get; set; }

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? EmailAddress { get; set; }
    public string? MemberStatus { get; set; }

    /// <summary>UTC timestamp of the last successful sync for this record.</summary>
    public DateTimeOffset LastSyncedAt { get; set; }

    /// <summary>
    /// Full JustGo API response stored as JSONB. Typed as <see cref="JustGoMemberDto"/>.
    /// </summary>
    public JustGoMemberDto RawData { get; set; } = null!;
}

/// <summary>Query parameters for <c>GET /api/v2.2/Members/FindByAttributes</c>.</summary>
public sealed class FindMembersRequest
{
    public string? Email { get; set; }
    public string? MemberId { get; set; }
    public string? LoginId { get; set; }
    public string? LastName { get; set; }
    public Guid? ClubId { get; set; }
    public Guid? CredentialId { get; set; }
    public Guid? EventId { get; set; }
    public string? Membership { get; set; }
    public string? SuspendStatus { get; set; }
    public DateTimeOffset? ModifiedBefore { get; set; }
    public DateTimeOffset? ModifiedAfter { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

/// <summary>Paged response wrapper for member list results.</summary>
public sealed class MembersPagedResponse
{
    [JsonPropertyName("pageNumber")]
    public int PageNumber { get; set; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; set; }

    [JsonPropertyName("totalPages")]
    public int TotalPages { get; set; }

    [JsonPropertyName("totalRecords")]
    public int TotalRecords { get; set; }

    [JsonPropertyName("data")]
    public List<JustGoMemberDto>? Data { get; set; }
}
