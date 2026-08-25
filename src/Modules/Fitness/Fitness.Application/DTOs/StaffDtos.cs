using Fitness.Domain.Enums;

namespace Fitness.Application.DTOs;

// ── Staff ────────────────────────────────────────────────────────────────────

public class StaffSummaryDto
{
    public Guid Id { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? UserId { get; set; }
    public Guid ClubId { get; set; }
    public string? ClubName { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }

    public StaffRoleKind RoleKind { get; set; }
    public DateTime? StartedOn { get; set; }
    public DateTime? LeftOn { get; set; }
    public bool IsContractor { get; set; }
    public bool IsBookable { get; set; }
    public bool IsActive { get; set; }

    /// <summary>The worst certification state they hold, so an expiry is visible in the list.</summary>
    public CertificationStatus CertificationStatus { get; set; }
    public int ExpiringCertifications { get; set; }

    public bool IsClockedIn { get; set; }
    public DateTime? ClockedInAt { get; set; }
}

public class StaffDetailDto : StaffSummaryDto
{
    public bool HasPin { get; set; }
    public Guid? AccessCredentialId { get; set; }
    public Guid? AccessRuleId { get; set; }
    public string? AccessRuleName { get; set; }

    public bool CanSell { get; set; }
    public bool CanTrain { get; set; }
    public bool CanTeach { get; set; }
    public bool CanApproveOverrides { get; set; }

    public List<Guid> AdditionalClubIds { get; set; } = [];
    public List<StaffCertificationDto> Certifications { get; set; } = [];
    public List<StaffRoleDto> Roles { get; set; } = [];
    public BookableStaffDto? BookableProfile { get; set; }

    // This period's performance, so the record is worth opening.
    public int SessionsThisPeriod { get; set; }
    public int ClassesThisPeriod { get; set; }
    public int SalesThisPeriod { get; set; }
    public decimal CommissionThisPeriod { get; set; }
    public int HoursThisPeriod { get; set; }
    public int AssignedClients { get; set; }
    public int ClientRetentionPercent { get; set; }
}

public class SaveStaffDto
{
    public Guid? EmployeeId { get; set; }
    public Guid? UserId { get; set; }
    public Guid ClubId { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? PhotoUrl { get; set; }

    public StaffRoleKind RoleKind { get; set; } = StaffRoleKind.Receptionist;

    /// <summary>Plain PIN, hashed on the way in and never stored or logged in the clear.</summary>
    public string? Pin { get; set; }

    public DateTime? StartedOn { get; set; }
    public DateTime? LeftOn { get; set; }
    public bool IsContractor { get; set; }
    public Guid? AccessRuleId { get; set; }

    public bool CanSell { get; set; }
    public bool CanTrain { get; set; }
    public bool CanTeach { get; set; }
    public bool CanApproveOverrides { get; set; }
    public bool IsBookable { get; set; }

    public List<Guid> AdditionalClubIds { get; set; } = [];
    public List<Guid> RoleIds { get; set; } = [];
    public bool IsActive { get; set; } = true;
}

public class StaffRoleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ClubId { get; set; }
    public StaffRoleKind BaseKind { get; set; }
    public List<string> Permissions { get; set; } = [];
    public decimal? DiscountLimitPercent { get; set; }
    public decimal? RefundLimit { get; set; }
    public decimal? WriteOffLimit { get; set; }
    public bool CanOverridePolicies { get; set; }
    public bool CanViewMedicalData { get; set; }
    public bool CanExportMemberData { get; set; }
    public bool IsSystemRole { get; set; }
    public bool IsActive { get; set; }
    public int StaffCount { get; set; }
}

public class StaffCertificationDto
{
    public Guid Id { get; set; }
    public Guid StaffId { get; set; }
    public string? StaffName { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? IssuingBody { get; set; }
    public string? ReferenceNumber { get; set; }
    public DateTime? IssuedOn { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public CertificationStatus Status { get; set; }
    public int? DaysToExpiry { get; set; }
    public string? DocumentUrl { get; set; }
    public bool BlocksWorkOnExpiry { get; set; }
}

/// <summary>A staff member signing in on a shared terminal with their PIN.</summary>
public class StaffPinLoginDto
{
    public Guid ClubId { get; set; }
    public string Pin { get; set; } = string.Empty;
}

public class StaffPinResultDto
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public StaffSummaryDto? Staff { get; set; }
    public List<string> Permissions { get; set; } = [];
}

/// <summary>A manager authorising something above someone else's limit.</summary>
public class ManagerOverrideDto
{
    public Guid ClubId { get; set; }
    public string Pin { get; set; } = string.Empty;

    /// <summary>What is being authorised, so the audit entry says something useful.</summary>
    public string Action { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public decimal? Amount { get; set; }
    public Guid? EntityId { get; set; }
    public string? EntityType { get; set; }
}

// ── Rota ─────────────────────────────────────────────────────────────────────

public class ShiftDto
{
    public Guid Id { get; set; }
    public Guid ClubId { get; set; }
    public string? ClubName { get; set; }
    public string? Title { get; set; }
    public string? Position { get; set; }

    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public int BreakMinutes { get; set; }
    public decimal Hours { get; set; }

    public ShiftStatus Status { get; set; }
    public int RequiredHeadcount { get; set; }
    public int AssignedHeadcount { get; set; }
    public bool IsUnderStaffed { get; set; }

    public string? RequiredCertification { get; set; }
    public bool IsPublished { get; set; }
    public string? Note { get; set; }

    public List<ShiftAssignmentDto> Assignments { get; set; } = [];
}

public class ShiftAssignmentDto
{
    public Guid Id { get; set; }
    public Guid ShiftId { get; set; }
    public Guid StaffId { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public ShiftStatus Status { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? ClockedInAt { get; set; }
    public DateTime? ClockedOutAt { get; set; }
    public bool WasNoShow { get; set; }
    public bool WasLate { get; set; }
    public int? LateMinutes { get; set; }
    public string? Note { get; set; }
}

public class SaveShiftDto
{
    public Guid ClubId { get; set; }
    public string? Title { get; set; }
    public string? Position { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public int BreakMinutes { get; set; }
    public int RequiredHeadcount { get; set; } = 1;
    public string? RequiredCertification { get; set; }
    public string? Note { get; set; }
    public List<Guid> StaffIds { get; set; } = [];
}

/// <summary>The rota grid: a date range of shifts by staff member, with the coverage warnings.</summary>
public class RotaDto
{
    public Guid ClubId { get; set; }
    public string ClubName { get; set; } = string.Empty;
    public DateTime From { get; set; }
    public DateTime To { get; set; }

    public List<ShiftDto> Shifts { get; set; } = [];
    public List<StaffSummaryDto> Staff { get; set; } = [];
    public List<StaffTimeOffDto> TimeOff { get; set; } = [];

    public decimal TotalHours { get; set; }
    public decimal EstimatedCost { get; set; }
    public int UnassignedShifts { get; set; }
    public int OpenSwapRequests { get; set; }

    /// <summary>Hours the club is open with nobody rostered, which is the point of the screen.</summary>
    public List<CoverageGapDto> CoverageGaps { get; set; } = [];
}

public class CoverageGapDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public string? Position { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsCritical { get; set; }
}

public class ShiftSwapRequestDto
{
    public Guid Id { get; set; }
    public Guid ShiftAssignmentId { get; set; }
    public Guid ShiftId { get; set; }
    public DateTime ShiftStartsAt { get; set; }
    public DateTime ShiftEndsAt { get; set; }
    public string? ShiftPosition { get; set; }

    public Guid RequestedByStaffId { get; set; }
    public string? RequestedByName { get; set; }
    public Guid? OfferedToStaffId { get; set; }
    public string? OfferedToName { get; set; }
    public Guid? AcceptedByStaffId { get; set; }
    public string? AcceptedByName { get; set; }

    public DateTime RequestedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public string? Reason { get; set; }
    public bool IsApproved { get; set; }
    public bool IsCancelled { get; set; }
    public bool IsOpenToAll { get; set; }
}

// ── Time clock ───────────────────────────────────────────────────────────────

public class TimeClockEntryDto
{
    public Guid Id { get; set; }
    public Guid StaffId { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public Guid ClubId { get; set; }
    public Guid? ShiftAssignmentId { get; set; }

    public DateTime ClockedInAt { get; set; }
    public DateTime? ClockedOutAt { get; set; }
    public int BreakMinutes { get; set; }
    public int? WorkedMinutes { get; set; }
    public decimal? WorkedHours { get; set; }

    public string? Device { get; set; }
    public bool GeofencePassed { get; set; }
    public bool IsApproved { get; set; }
    public string? ApprovedByName { get; set; }
    public bool WasEdited { get; set; }
    public string? EditNote { get; set; }

    /// <summary>Difference against the rostered shift, which is what a manager reviews.</summary>
    public int? VarianceMinutes { get; set; }
}

public class ClockDto
{
    public Guid StaffId { get; set; }
    public Guid ClubId { get; set; }
    public string? Device { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int? BreakMinutes { get; set; }
}

public class TimesheetDto
{
    public Guid StaffId { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public Guid ClubId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    public decimal TotalHours { get; set; }
    public decimal RosteredHours { get; set; }
    public decimal VarianceHours { get; set; }
    public int LateCount { get; set; }
    public int NoShowCount { get; set; }

    public bool IsApproved { get; set; }
    public List<TimeClockEntryDto> Entries { get; set; } = [];
}

// ── Commission ───────────────────────────────────────────────────────────────

public class CommissionRuleDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ClubId { get; set; }
    public Guid? StaffId { get; set; }
    public string? StaffName { get; set; }
    public StaffRoleKind? AppliesToRole { get; set; }

    public CommissionBasis Basis { get; set; }
    public decimal RatePerUnit { get; set; }
    public decimal Percentage { get; set; }
    public decimal Threshold { get; set; }
    public decimal AcceleratedRate { get; set; }
    public decimal PeriodCap { get; set; }

    public Guid? ServiceId { get; set; }
    public string? ServiceName { get; set; }
    public Guid? ClassTypeId { get; set; }
    public string? ClassTypeName { get; set; }
    public Guid? PlanId { get; set; }
    public string? PlanName { get; set; }

    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }

    /// <summary>Plain-English rendering, because a commission rule nobody can read is a dispute waiting to happen.</summary>
    public string? Explanation { get; set; }
}

public class CommissionAccrualDto
{
    public Guid Id { get; set; }
    public Guid StaffId { get; set; }
    public string? StaffName { get; set; }
    public Guid ClubId { get; set; }
    public Guid? CommissionRuleId { get; set; }
    public string? RuleName { get; set; }

    public CommissionBasis Basis { get; set; }
    public DateTime EarnedOn { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal BaseValue { get; set; }
    public decimal Quantity { get; set; }

    public string? Narrative { get; set; }
    public Guid? MemberId { get; set; }
    public string? MemberName { get; set; }
    public Guid? SourceEntityId { get; set; }
    public string? SourceEntityType { get; set; }
    public string? DrillRoute { get; set; }

    public bool IsReversed { get; set; }
    public string? ReversalReason { get; set; }
}

public class CommissionStatementDto
{
    public Guid Id { get; set; }
    public string StatementNumber { get; set; } = string.Empty;
    public Guid StaffId { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public Guid ClubId { get; set; }
    public string? ClubName { get; set; }

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public CommissionStatementStatus Status { get; set; }

    public decimal SessionCommission { get; set; }
    public decimal ClassCommission { get; set; }
    public decimal SalesCommission { get; set; }
    public decimal RetailCommission { get; set; }
    public decimal Bonus { get; set; }
    public decimal Adjustments { get; set; }
    public decimal Total { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public int SessionsDelivered { get; set; }
    public int ClassesTaught { get; set; }
    public int MembershipsSold { get; set; }
    public int PackagesSold { get; set; }

    public DateTime? SubmittedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovedByName { get; set; }
    public string? RejectionNote { get; set; }
    public DateTime? ExportedAt { get; set; }
    public string? PayrollReference { get; set; }

    public List<CommissionAccrualDto> Accruals { get; set; } = [];
}

public class GenerateCommissionDto
{
    public Guid ClubId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public List<Guid> StaffIds { get; set; } = [];
    public bool PreviewOnly { get; set; } = true;
}

public class ApproveStatementDto
{
    public Guid StatementId { get; set; }
    public bool Approve { get; set; }
    public string? Note { get; set; }
    public decimal? AdjustmentAmount { get; set; }
    public string? AdjustmentReason { get; set; }
}

public class StaffTargetDto
{
    public Guid Id { get; set; }
    public Guid StaffId { get; set; }
    public string? StaffName { get; set; }
    public Guid ClubId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string MetricName { get; set; } = string.Empty;
    public decimal TargetValue { get; set; }
    public decimal ActualValue { get; set; }
    public int AchievementPercent { get; set; }
    public decimal? Bonus { get; set; }
    public bool IsAchieved { get; set; }
    public bool BonusPaid { get; set; }
}

/// <summary>The trainer's own screen: today's diary, their clients and what they have earned.</summary>
public class TrainerDayDto
{
    public Guid StaffId { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public Guid ClubId { get; set; }
    public DateTime ForDate { get; set; }

    public List<AppointmentSummaryDto> Appointments { get; set; } = [];
    public List<ClassOccurrenceSummaryDto> Classes { get; set; } = [];
    public List<CoachCheckInDto> DueCheckIns { get; set; } = [];
    public List<RetentionTaskDto> Tasks { get; set; } = [];

    public int SessionsToday { get; set; }
    public int SessionsCompleted { get; set; }
    public decimal HoursBooked { get; set; }
    public decimal CommissionThisPeriod { get; set; }
    public int ActiveClients { get; set; }
    public int ClientsAtRisk { get; set; }

    public bool IsClockedIn { get; set; }
    public ShiftDto? CurrentShift { get; set; }
}
