namespace JustGo.Grading.Models;

public sealed class EventTicketApiDto
{
    public Guid TicketId { get; set; }
    public string TicketName { get; set; } = string.Empty;
    public Guid? CredentialDefinitionId { get; set; }
    public string? GradeName { get; set; }
    public decimal TotalBooked { get; set; }
    public decimal RemainingPlaces { get; set; }
}
