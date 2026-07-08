namespace Hr.Application.DTOs;

public class OfferNegotiationDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid OfferId { get; set; }
    public int NegotiationRound { get; set; }
    public string InitiatedBy { get; set; } = string.Empty;
    public DateTime NegotiationDate { get; set; }
    public decimal? ProposedSalary { get; set; }
    public string? ProposedTerms { get; set; }
    public string? CounterOfferNotes { get; set; }
    public Guid StatusLookupValueId { get; set; }
    public Guid? ResponseByEmployeeId { get; set; }
    public decimal? PreviousSalary { get; set; }
    public string? HRRecommendation { get; set; }
    public bool RequiresReApproval { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime? Deadline { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateOfferNegotiationDto
{
    public Guid OfferId { get; set; }
    public int NegotiationRound { get; set; }
    public string InitiatedBy { get; set; } = string.Empty;
    public decimal? ProposedSalary { get; set; }
    public string? ProposedTerms { get; set; }
    public string? CounterOfferNotes { get; set; }
    public Guid StatusLookupValueId { get; set; }
    public decimal? PreviousSalary { get; set; }
    public string? HRRecommendation { get; set; }
    public bool RequiresReApproval { get; set; }
    public DateTime? Deadline { get; set; }
    public string? Notes { get; set; }
}

public class UpdateOfferNegotiationDto
{
    public string? ProposedTerms { get; set; }
    public string? CounterOfferNotes { get; set; }
    public Guid? StatusLookupValueId { get; set; }
    public Guid? ResponseByEmployeeId { get; set; }
    public string? HRRecommendation { get; set; }
    public bool? RequiresReApproval { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime? Deadline { get; set; }
    public string? Notes { get; set; }
}
