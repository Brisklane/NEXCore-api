
using Nexcore.SharedKernel;
using Nexcore.SharedKernel.Enums;

namespace Hr.Domain.Entities;

public class OfferLetter : BaseEntity
{
    public string OfferCode { get; set; } = string.Empty;
    public Guid ApplicationId { get; set; }
    public Guid CandidateId { get; set; }
    public Guid JobId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public Guid ReportingManagerEmployeeId { get; set; }
    public decimal BaseSalary { get; set; }
    public decimal TotalPackage { get; set; }
    public Guid CurrencyId { get; set; }
    public EmploymentType EmploymentType { get; set; } 
    public DateTime StartDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int ProbationPeriodMonths { get; set; }
    public int NoticePeriodDays { get; set; }
    public Guid? ApprovalRequestId { get; set; }
    public Guid CandidateResponseLookupValueId { get; set; }
    public int VersionNumber { get; set; } = 1;
    public Guid StatusLookupValueId { get; set; }
    public DateTime? SentDate { get; set; }
    public Guid? SentByEmployeeId { get; set; }
    public string? SentVia { get; set; }
    public DateTime? ResponseDate { get; set; }
    public DateTime? RevokedDate { get; set; }
    public Guid? RevokedByEmployeeId { get; set; }
    public string? RevokeReason { get; set; }

    public Application? Application { get; set; }
    public Candidate? Candidate { get; set; }
    public Job? Job { get; set; }
    public Employee? ReportingManagerEmployee { get; set; }
    public Employee? SentByEmployee { get; set; }
    public Employee? RevokedByEmployee { get; set; }
    public ApprovalRequest? ApprovalRequest { get; set; }
    public Currency? Currency { get; set; }
    public LookupValue? CandidateResponseLookupValue { get; set; }
    public LookupValue? StatusLookupValue { get; set; }
    public OfferLetterDetail? Detail { get; set; }
    public ICollection<OfferNegotiation>? Negotiations { get; set; }
    public Guid? CommunicationTemplateId { get; set; }
    public CommunicationTemplate? CommunicationTemplate { get; set; }
}
