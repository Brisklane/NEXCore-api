using Nexcore.SharedKernel.Enums;

namespace Hr.Application.DTOs;

public class OfferLetterDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string OfferCode { get; set; } = string.Empty;
    public Guid ApplicationId { get; set; }
    public Guid CandidateId { get; set; }
    public Guid JobId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public Guid ReportingManagerEmployeeId { get; set; }
    public decimal BaseSalary { get; set; }
    public decimal TotalPackage { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public EmploymentType EmploymentType { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int ProbationPeriodMonths { get; set; }
    public int NoticePeriodDays { get; set; }
    public Guid StatusLookupValueId { get; set; }
    public Guid CandidateResponseLookupValueId { get; set; }
    public int VersionNumber { get; set; }
    public Guid? ApprovalRequestId { get; set; }
    public DateTime? SentDate { get; set; }
    public Guid? SentByEmployeeId { get; set; }
    public string? SentVia { get; set; }
    public DateTime? ResponseDate { get; set; }
    public DateTime? RevokedDate { get; set; }
    public Guid? RevokedByEmployeeId { get; set; }
    public string? RevokeReason { get; set; }
    public Guid? CommunicationTemplateId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateOfferLetterDto
{
    // Required — provided by the frontend
    public Guid ApplicationId { get; set; }
    public Guid CandidateId { get; set; }
    public Guid JobId { get; set; }
    public Guid StatusLookupValueId { get; set; }

    // Optional frontend fields
    public string? OfferCode { get; set; }
    public string? LetterContent { get; set; }
    public DateTime? IssuedDate { get; set; }       // maps to StartDate
    public DateTime? ExpiryDate { get; set; }
    public Guid? GeneratedByEmployeeId { get; set; } // maps to SentByEmployeeId
    public string? Notes { get; set; }

    // Salary — optional, can be set later via update
    public decimal BaseSalary { get; set; }
    public decimal TotalPackage { get; set; }
    public int ProbationPeriodMonths { get; set; } = 3;
    public int NoticePeriodDays { get; set; } = 30;

    // CurrencyCode, JobTitle, EmploymentType, ReportingManagerEmployeeId
    // are derived automatically from the Job record — do not send from frontend
}

public class UpdateOfferLetterDto
{
    public Guid? StatusLookupValueId { get; set; }
    public decimal? BaseSalary { get; set; }
    public decimal? TotalPackage { get; set; }
    public DateTime? IssuedDate { get; set; }          // maps to StartDate
    public DateTime? ExpiryDate { get; set; }
    public Guid? GeneratedByEmployeeId { get; set; }   // maps to SentByEmployeeId
    public string? LetterContent { get; set; }
    public string? Notes { get; set; }
    public string? RevokeReason { get; set; }
    public int? ProbationPeriodMonths { get; set; }
    public int? NoticePeriodDays { get; set; }
}
