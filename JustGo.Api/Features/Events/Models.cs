using System.Text.Json.Serialization;

namespace JustGo.Integrations.JustGo.Features.Events.Models;

public sealed class FindEventsRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Name { get; set; }
}

public sealed class EventCreateRequest
{
    public string Name { get; set; } = string.Empty;
}

public sealed class EventUpdateRequest
{
    public string? Name { get; set; }
}

public sealed class FindEventCandidatesRequest
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public Guid? EventId { get; set; }
}

public sealed class EventCandidateCreateRequest
{
    public Guid MemberId { get; set; }
    public Guid TicketId { get; set; }
}

public sealed class EventCandidateStatusUpdateRequest
{
    public string Status { get; set; } = "Pending";
}

public sealed class EventPromoterCreateRequest
{
    public int PromoterId { get; set; }
}

public sealed class EventStageCreateRequest
{
    public string Name { get; set; } = string.Empty;
}

public sealed class EventCreatedResponse
{
    public Guid EventId { get; set; } = Guid.NewGuid();
}

public sealed class EventCandidateCreatedResponse
{
    public Guid BookingId { get; set; } = Guid.NewGuid();
}

public sealed class EventPromoterCreatedResponse
{
    public int PromoterId { get; set; }
}

public sealed class EventStageCreatedResponse
{
    public Guid StageId { get; set; } = Guid.NewGuid();
}

public sealed class EventCollectionResponse<T>
{
    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("data")]
    public List<T>? Data { get; set; }
}

public sealed class EventTicketDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("ticketName")]
    public string? TicketName { get; set; }

    [JsonPropertyName("totalBooked")]
    public decimal TotalBooked { get; set; }

    [JsonPropertyName("remainingPlaces")]
    public decimal RemainingPlaces { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("ticketCode")]
    public string? TicketCode { get; set; }

    [JsonPropertyName("endDate")]
    public DateOnly? EndDate { get; set; }
}

public sealed class EventCandidateDto
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("ticketId")]
    public Guid TicketId { get; set; }

    [JsonPropertyName("candidateId")]
    public Guid CandidateId { get; set; }

    [JsonPropertyName("bookingId")]
    public Guid BookingId { get; set; }

    [JsonPropertyName("memberNumber")]
    public string? MemberNumber { get; set; }

    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("emailAddress")]
    public string? EmailAddress { get; set; }

    [JsonPropertyName("ticketCode")]
    public string? TicketCode { get; set; }

    [JsonPropertyName("courseName")]
    public string? CourseName { get; set; }

    [JsonPropertyName("eventNumber")]
    public string? EventNumber { get; set; }

    [JsonPropertyName("bookingDate")]
    public DateTimeOffset? BookingDate { get; set; }

    [JsonPropertyName("courseDate")]
    public DateTimeOffset? CourseDate { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }

    [JsonPropertyName("registerDate")]
    public DateTimeOffset? RegisterDate { get; set; }

    [JsonPropertyName("lastModificationDate")]
    public DateTimeOffset? LastModificationDate { get; set; }
}
