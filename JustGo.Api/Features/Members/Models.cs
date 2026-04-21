using System.Text.Json.Serialization;

namespace JustGo.Api.Features.Members;

/// <summary>
/// Domain object representing the JustGo member data as returned by
/// GET /api/v2.2/Members/FindByAttributes (MemberListV2_2Dto shape).
/// Stored as JSONB in <see cref="MemberSyncRecord.MemberInformation"/>.
/// </summary>
public sealed class JustGoMemberDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("memberDocId")]
    public int? MemberDocId { get; set; }

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
/// Membership (licence) held by a member. Maps <c>MemberLicenseDtoV2_2</c>.
/// Returned as part of <see cref="MemberDetailDto"/>.
/// </summary>
public sealed class MemberMembershipDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("definitionId")]
    public Guid? DefinitionId { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("startDate")]
    public DateOnly StartDate { get; set; }

    [JsonPropertyName("endDate")]
    public DateOnly EndDate { get; set; }
}

/// <summary>
/// Club membership held by a member. Maps <c>ClubMemberDtoV2_2</c>.
/// </summary>
public sealed class MemberDetailClubDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("clubName")]
    public string? ClubName { get; set; }

    [JsonPropertyName("roles")]
    public string? Roles { get; set; }

    [JsonPropertyName("isPrimary")]
    public bool IsPrimary { get; set; }

    [JsonPropertyName("isAdmin")]
    public bool IsAdmin { get; set; }
}

/// <summary>
/// Family member linked to a member. Maps <c>FamilyMemberDtoV2_2</c>.
/// </summary>
public sealed class FamilyMemberDetailDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("reference")]
    public string? Reference { get; set; }

    [JsonPropertyName("memberId")]
    public string? MemberId { get; set; }

    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("emailAddress")]
    public string? EmailAddress { get; set; }

    [JsonPropertyName("dob")]
    public DateOnly Dob { get; set; }
}

/// <summary>
/// Full member detail as returned by <c>GET /api/v2.2/Members/{id}</c> (<c>MemberV2_2Dto</c> shape).
/// A superset of <see cref="JustGoMemberDto"/> — includes nested membership, club,
/// credential, booking and family-member collections.
/// Stored as JSONB in <see cref="MemberSyncRecord.MemberInformation"/>.
/// </summary>
public sealed class MemberDetailDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("userId")]
    public int? UserId { get; set; }

    [JsonPropertyName("memberDocId")]
    public int? MemberDocId { get; set; }

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

    [JsonPropertyName("emailVerified")]
    public bool EmailVerified { get; set; }

    [JsonPropertyName("optin")]
    public object? Optin { get; set; }

    [JsonPropertyName("memberships")]
    public List<MemberMembershipDto>? Memberships { get; set; }

    [JsonPropertyName("clubs")]
    public List<MemberDetailClubDto>? Clubs { get; set; }

    [JsonPropertyName("credentials")]
    public List<MemberCredentialDtoV2_2>? Credentials { get; set; }

    [JsonPropertyName("linkedFamilyMember")]
    public List<FamilyMemberDetailDto>? LinkedFamilyMembers { get; set; }
}

/// <summary>
/// Credential held by a member. Maps <c>MemberCredentialDtoV2_2</c>.
/// </summary>
public sealed class MemberCredentialDtoV2_2
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("definitionId")]
    public Guid DefinitionId { get; set; }

    [JsonPropertyName("refrence")]
    public string? Reference { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("grantedDate")]
    public DateOnly GrantedDate { get; set; }

    [JsonPropertyName("expiryDate")]
    public DateOnly? ExpiryDate { get; set; }

    [JsonPropertyName("lastModificationDate")]
    public DateTimeOffset LastModificationDate { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

/// <summary>
/// Response wrapper for <c>GET /api/v2.2/Members/{id}</c>.
/// Maps <c>MemberV2_2DtoStringHttp200OKWithMessageResponse</c>.
/// </summary>
public sealed class MemberDetailResponse
{
    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public MemberDetailDto? Data { get; set; }
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
    /// Full JustGo API response stored as JSONB. Typed as <see cref="MemberDetailDto"/>.
    /// Populated by <c>GET /api/v2.2/Members/{id}</c> and includes nested memberships, clubs and credentials.
    /// </summary>
    public MemberDetailDto MemberInformation { get; set; } = null!;
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
