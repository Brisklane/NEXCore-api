
using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class OfferNegotiation : BaseEntity
{
    public Guid OfferId { get; set; }
    public int NegotiationRound { get; set; }
    public string InitiatedBy { get; set; } = string.Empty;
    public DateTime NegotiationDate { get; set; } = DateTime.UtcNow;
    public decimal? ProposedSalary { get; set; }
    public string? ProposedTerms { get; set; }
    public string? CounterOfferNotes { get; set; }
    public Guid StatusLookupValueId { get; set; }
    public Guid? ResponseByEmployeeId { get; set; }
    public decimal? PreviousSalary { get; set; }
    public decimal? SalaryDifference { get; set; }
    public string? NegotiationReasonCode { get; set; }
    public decimal? CandidateCompetingOffer { get; set; }
    public string? CandidateCompetingCompany { get; set; }
    public string? HRRecommendation { get; set; }
    public bool RequiresReApproval { get; set; }
    public Guid? ReApprovalRequestId { get; set; }
    public DateTime? RespondedAt { get; set; }
    public DateTime? Deadline { get; set; }
    public string? Notes { get; set; }

    public OfferLetter? Offer { get; set; }
    public Employee? ResponseByEmployee { get; set; }
    public ApprovalRequest? ReApprovalRequest { get; set; }
    public LookupValue? StatusLookupValue { get; set; }
}
