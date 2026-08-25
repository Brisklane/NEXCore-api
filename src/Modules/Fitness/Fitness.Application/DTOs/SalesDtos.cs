using Fitness.Domain.Enums;

namespace Fitness.Application.DTOs;

// ── Leads ────────────────────────────────────────────────────────────────────

public class LeadSummaryDto
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public string? ClubName { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }

    public LeadStatus Status { get; set; }
    public string? SourceName { get; set; }
    public LeadSourceKind SourceKind { get; set; }

    public DateTime ReceivedAt { get; set; }
    public DateTime? FirstContactedAt { get; set; }

    /// <summary>Minutes from arrival to first real contact — the number the board sorts on.</summary>
    public int? ResponseMinutes { get; set; }
    public bool SlaBreached { get; set; }

    /// <summary>Minutes left before the SLA breaches, so the board can count down rather than accuse.</summary>
    public int? MinutesToSlaBreach { get; set; }

    public Guid? AssignedStaffId { get; set; }
    public string? AssignedStaffName { get; set; }

    public DateTime? LastActivityAt { get; set; }
    public DateTime? NextFollowUpOn { get; set; }
    public bool FollowUpOverdue { get; set; }
    public int ContactAttempts { get; set; }
    public int AgeDays { get; set; }

    public DateTime? TourBookedFor { get; set; }
    public DateTime? TrialEndsOn { get; set; }

    public string? Goal { get; set; }
    public string? InterestedInPlanName { get; set; }
    public decimal? EstimatedValue { get; set; }
}

public class LeadDetailDto : LeadSummaryDto
{
    public Guid? MemberId { get; set; }
    public Guid? ContactId { get; set; }
    public DateTime? DateOfBirth { get; set; }

    public Guid? LeadSourceId { get; set; }
    public Guid? CampaignId { get; set; }
    public string? CampaignName { get; set; }
    public Guid? ReferredByMemberId { get; set; }
    public string? ReferredByName { get; set; }
    public string? Notes { get; set; }

    public DateTime? AssignedAt { get; set; }
    public DateTime? TouredOn { get; set; }
    public DateTime? TrialStartedOn { get; set; }

    public DateTime? WonOn { get; set; }
    public Guid? ResultingAgreementId { get; set; }
    public decimal? WonValue { get; set; }

    public DateTime? LostOn { get; set; }
    public Guid? LossReasonId { get; set; }
    public string? LossReasonName { get; set; }
    public string? LossNote { get; set; }
    public decimal? AttributedCost { get; set; }

    public List<LeadActivityDto> Activities { get; set; } = [];
    public List<TourDto> Tours { get; set; } = [];
    public List<TrialPassDto> Trials { get; set; } = [];
}

public class SaveLeadDto
{
    public Guid ClubId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? LastName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public DateTime? DateOfBirth { get; set; }

    public Guid? LeadSourceId { get; set; }
    public Guid? CampaignId { get; set; }
    public Guid? ReferredByMemberId { get; set; }
    public string? PromoCodeText { get; set; }

    public string? Goal { get; set; }
    public Guid? InterestedInPlanId { get; set; }
    public string? Notes { get; set; }

    public Guid? AssignedStaffId { get; set; }
    public DateTime? NextFollowUpOn { get; set; }
}

/// <summary>The pipeline board: leads grouped into columns, with the counts the header shows.</summary>
public class LeadBoardDto
{
    public Guid? ClubId { get; set; }
    public string? ClubName { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    public List<LeadBoardColumnDto> Columns { get; set; } = [];

    public int TotalOpen { get; set; }
    public int BreachingSla { get; set; }
    public int OverdueFollowUps { get; set; }
    public int WonThisMonth { get; set; }
    public int LostThisMonth { get; set; }
    public int ConversionPercent { get; set; }
    public int MedianResponseMinutes { get; set; }
    public int SlaMinutes { get; set; }
}

public class LeadBoardColumnDto
{
    public LeadStatus Status { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal EstimatedValue { get; set; }
    public List<LeadSummaryDto> Leads { get; set; } = [];
}

public class LeadActivityDto
{
    public Guid Id { get; set; }
    public Guid LeadId { get; set; }
    public LeadActivityKind Kind { get; set; }
    public DateTime OccurredAt { get; set; }
    public string? Summary { get; set; }
    public string? Outcome { get; set; }
    public Guid? StaffId { get; set; }
    public string? StaffName { get; set; }
    public LeadStatus? FromStatus { get; set; }
    public LeadStatus? ToStatus { get; set; }
    public bool WasSuccessfulContact { get; set; }
    public DateTime? FollowUpOn { get; set; }
}

public class LogLeadActivityDto
{
    public Guid LeadId { get; set; }
    public LeadActivityKind Kind { get; set; }
    public string? Summary { get; set; }
    public string? Outcome { get; set; }

    /// <summary>Whether they were actually reached, which is what stops the SLA clock.</summary>
    public bool WasSuccessfulContact { get; set; }

    public LeadStatus? MoveToStatus { get; set; }
    public DateTime? FollowUpOn { get; set; }
}

public class LeadSourceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public LeadSourceKind Kind { get; set; }
    public Guid? ClubId { get; set; }
    public int DisplayOrder { get; set; }
    public decimal MonthlyCost { get; set; }
    public string? TrackingCode { get; set; }
    public bool IsActive { get; set; }

    public int LeadsThisMonth { get; set; }
    public int JoinsThisMonth { get; set; }
    public int ConversionPercent { get; set; }
    public decimal CostPerLead { get; set; }
    public decimal CostPerAcquisition { get; set; }
}

public class LossReasonDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string? Category { get; set; }
    public bool RequiresNote { get; set; }
    public bool IsActive { get; set; }
    public int UseCount { get; set; }
}

public class CloseLeadDto
{
    public Guid LeadId { get; set; }
    public bool Won { get; set; }
    public Guid? LossReasonId { get; set; }
    public string? Note { get; set; }
    public Guid? ResultingAgreementId { get; set; }
}

// ── Tours & trials ───────────────────────────────────────────────────────────

public class TourDto
{
    public Guid Id { get; set; }
    public Guid LeadId { get; set; }
    public string? LeadName { get; set; }
    public string? LeadPhone { get; set; }
    public Guid ClubId { get; set; }
    public Guid? StaffId { get; set; }
    public string? StaffName { get; set; }

    public DateTime ScheduledFor { get; set; }
    public int DurationMinutes { get; set; }
    public DateTime? ArrivedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public bool WasNoShow { get; set; }
    public bool WasCancelled { get; set; }
    public string? CancellationReason { get; set; }
    public bool ConvertedOnDay { get; set; }
    public string? Notes { get; set; }
    public bool ReminderSent { get; set; }
}

public class BookTourDto
{
    public Guid LeadId { get; set; }
    public Guid ClubId { get; set; }
    public Guid? StaffId { get; set; }
    public DateTime ScheduledFor { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public string? Notes { get; set; }
    public bool SendConfirmation { get; set; } = true;
}

public class TrialPassDto
{
    public Guid Id { get; set; }
    public Guid LeadId { get; set; }
    public string? LeadName { get; set; }
    public Guid? MemberId { get; set; }
    public Guid ClubId { get; set; }
    public Guid? PlanId { get; set; }
    public string? PlanName { get; set; }

    public DateTime StartsOn { get; set; }
    public DateTime EndsOn { get; set; }
    public int VisitsAllowed { get; set; }
    public int VisitsUsed { get; set; }
    public bool IsExpired { get; set; }
    public int? DaysRemaining { get; set; }

    public decimal Price { get; set; }
    public bool Converted { get; set; }
    public DateTime? ConvertedOn { get; set; }
    public string? IssuedByName { get; set; }
}

public class IssueTrialDto
{
    public Guid LeadId { get; set; }
    public Guid ClubId { get; set; }
    public Guid? PlanId { get; set; }
    public DateTime StartsOn { get; set; }
    public int DurationDays { get; set; } = 7;
    public int VisitsAllowed { get; set; }
    public decimal Price { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public bool IssueCredential { get; set; } = true;
    public string? CredentialIdentifier { get; set; }

    /// <summary>Starts the conversion nudge sequence, which is why trials work at all.</summary>
    public bool StartConversionSequence { get; set; } = true;
}

// ── Referrals & targets ──────────────────────────────────────────────────────

public class ReferralDto
{
    public Guid Id { get; set; }
    public Guid ReferrerMemberId { get; set; }
    public string? ReferrerName { get; set; }
    public Guid ClubId { get; set; }

    public string ReferredName { get; set; } = string.Empty;
    public string? ReferredPhone { get; set; }
    public string? ReferredEmail { get; set; }

    public Guid? LeadId { get; set; }
    public Guid? ReferredMemberId { get; set; }
    public DateTime ReferredOn { get; set; }
    public string? ReferralCode { get; set; }

    public bool Converted { get; set; }
    public DateTime? ConvertedOn { get; set; }

    public decimal ReferrerRewardValue { get; set; }
    public int ReferrerRewardPoints { get; set; }
    public bool ReferrerRewarded { get; set; }
    public decimal ReferredRewardValue { get; set; }
    public bool ReferredRewarded { get; set; }
    public string? CampaignName { get; set; }
}

public class CreateReferralDto
{
    public Guid ReferrerMemberId { get; set; }
    public Guid ClubId { get; set; }
    public string ReferredName { get; set; } = string.Empty;
    public string? ReferredPhone { get; set; }
    public string? ReferredEmail { get; set; }
    public decimal ReferrerRewardValue { get; set; }
    public int ReferrerRewardPoints { get; set; }
    public decimal ReferredRewardValue { get; set; }
    public Guid? CampaignId { get; set; }

    /// <summary>Creates the lead as well, so the referral actually gets worked.</summary>
    public bool CreateLead { get; set; } = true;
}

public class SalesTargetDto
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public string? ClubName { get; set; }
    public Guid? StaffId { get; set; }
    public string? StaffName { get; set; }
    public string? PhotoUrl { get; set; }

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string MetricName { get; set; } = string.Empty;

    public decimal TargetValue { get; set; }
    public decimal ActualValue { get; set; }
    public int AchievementPercent { get; set; }
    public decimal? BonusOnAchievement { get; set; }
    public bool IsAchieved { get; set; }

    /// <summary>Pace against the calendar — behind, on track or ahead, at this point in the period.</summary>
    public decimal ExpectedByNow { get; set; }
    public bool IsOnPace { get; set; }
    public int Rank { get; set; }
}

// ── Corporate ────────────────────────────────────────────────────────────────

public class CorporateAccountDto
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid ClubId { get; set; }
    public string? ClubName { get; set; }
    public Guid? CrmAccountId { get; set; }

    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? AddressLine { get; set; }
    public string? TaxRegistrationNumber { get; set; }

    public CorporateBillingModel BillingModel { get; set; }
    public decimal? NegotiatedRate { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal SubsidyPerMember { get; set; }
    public decimal SubsidyPercent { get; set; }
    public Guid? DefaultPlanId { get; set; }
    public string? DefaultPlanName { get; set; }

    public DateTime ContractStartsOn { get; set; }
    public DateTime? ContractEndsOn { get; set; }
    public bool ContractExpiringSoon { get; set; }

    public int MaxMembers { get; set; }
    public int CurrentMemberCount { get; set; }
    public int SpacesRemaining { get; set; }

    public int InvoiceDayOfMonth { get; set; }
    public int PaymentTermsDays { get; set; }
    public bool ReceivesUsageReport { get; set; }
    public bool IsActive { get; set; }

    public List<CorporateEligibilityRuleDto> EligibilityRules { get; set; } = [];

    public decimal MonthlyValue { get; set; }
    public decimal OutstandingBalance { get; set; }
    public int ActiveUsersLast30Days { get; set; }
    public int UtilisationPercent { get; set; }
}

public class CorporateEligibilityRuleDto
{
    public Guid Id { get; set; }
    public Guid CorporateAccountId { get; set; }
    public EligibilityProof Proof { get; set; }
    public string? MatchValue { get; set; }
    public bool RequiresManualApproval { get; set; }
    public int RevalidateEveryDays { get; set; }
    public bool IsActive { get; set; }
}

public class CorporateMemberDto
{
    public Guid Id { get; set; }
    public Guid CorporateAccountId { get; set; }
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string? MemberNumber { get; set; }
    public MemberStatus MemberStatus { get; set; }
    public Guid? AgreementId { get; set; }

    public string? EmployeeReference { get; set; }
    public string? Department { get; set; }

    public DateTime JoinedSchemeOn { get; set; }
    public DateTime? LeftSchemeOn { get; set; }
    public DateTime? EligibilityVerifiedOn { get; set; }
    public DateTime? EligibilityExpiresOn { get; set; }
    public bool EligibilityExpired { get; set; }

    public decimal EmployerContribution { get; set; }
    public decimal EmployeeContribution { get; set; }
    public bool IsActive { get; set; }

    public int VisitsLast30Days { get; set; }
    public DateTime? LastVisitOn { get; set; }
}

public class CorporateInvoiceDto
{
    public Guid Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public Guid CorporateAccountId { get; set; }
    public string? CorporateAccountName { get; set; }
    public Guid ClubId { get; set; }

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime IssuedOn { get; set; }
    public DateTime DueOn { get; set; }
    public DateTime? PaidOn { get; set; }
    public InvoiceStatus Status { get; set; }
    public int DaysOverdue { get; set; }

    public int MemberCount { get; set; }
    public decimal Subtotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal Total { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal BalanceDue { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public string? BreakdownUrl { get; set; }
    public string? DocumentUrl { get; set; }
    public string? PurchaseOrderReference { get; set; }

    public List<CorporateInvoiceLineDto> Breakdown { get; set; } = [];
}

public class CorporateInvoiceLineDto
{
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string? EmployeeReference { get; set; }
    public string? PlanName { get; set; }
    public decimal Amount { get; set; }
    public int Visits { get; set; }
}

public class ThirdPartyPayerDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid ClubId { get; set; }
    public string? PayerType { get; set; }
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public int PaymentTermsDays { get; set; }
    public decimal? AgreedRate { get; set; }
    public bool RequiresAuthorisationNumber { get; set; }
    public bool IsActive { get; set; }

    public int ActiveAuthorisations { get; set; }
    public decimal OutstandingValue { get; set; }
}

public class PayerAuthorisationDto
{
    public Guid Id { get; set; }
    public Guid ThirdPartyPayerId { get; set; }
    public string? PayerName { get; set; }
    public Guid MemberId { get; set; }
    public string? MemberName { get; set; }

    public string AuthorisationNumber { get; set; } = string.Empty;
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public bool IsExpired { get; set; }

    public int ApprovedUnits { get; set; }
    public int UsedUnits { get; set; }
    public int RemainingUnits { get; set; }

    public decimal RatePerUnit { get; set; }
    public decimal ApprovedValue { get; set; }
    public decimal InvoicedValue { get; set; }

    public string? Purpose { get; set; }
    public string? ReferrerName { get; set; }
    public bool IsExhausted { get; set; }
    public bool IsActive { get; set; }
}
