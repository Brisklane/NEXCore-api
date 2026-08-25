using Fitness.Domain.Enums;
using Nexcore.SharedKernel;

namespace Fitness.Domain.Entities;

/// <summary>
/// Someone who works here.
///
/// Links to the HR employee where HR is installed and stands alone where it is not — a
/// single-site gym with four staff should not be made to install a payroll module in order to
/// have a receptionist who can log in.
/// </summary>
public class FitnessStaff : BaseEntity
{
    /// <summary>HR employee record, when HR is installed. Null otherwise, deliberately.</summary>
    public Guid? EmployeeId { get; set; }

    /// <summary>Platform user, so a trainer signs in with the same identity as everyone else.</summary>
    public Guid? UserId { get; set; }

    public Guid ClubId { get; set; }
    public FitnessClub? Club { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }

    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? PhotoUrl { get; set; }

    public StaffRoleKind RoleKind { get; set; } = StaffRoleKind.Receptionist;

    /// <summary>PIN for a shared terminal, hashed. Never stored in the clear, never logged.</summary>
    public string? PinHash { get; set; }

    public DateTime? StartedOn { get; set; }
    public DateTime? LeftOn { get; set; }

    /// <summary>Self-employed and renting space rather than employed.</summary>
    public bool IsContractor { get; set; }

    /// <summary>Members are their own credential; staff need one too, on the same doors.</summary>
    public Guid? AccessCredentialId { get; set; }
    public Guid? AccessRuleId { get; set; }

    public bool CanSell { get; set; }
    public bool CanTrain { get; set; }
    public bool CanTeach { get; set; }
    public bool CanApproveOverrides { get; set; }

    public bool IsBookable { get; set; }

    /// <summary>Clubs they may work at beyond their home club, comma-separated.</summary>
    public string? AdditionalClubIds { get; set; }

    public ICollection<StaffCertification> Certifications { get; set; } = [];
}

/// <summary>
/// A permission set. Roles are configurable rather than hard-coded because "duty manager" means
/// something different in a ten-person studio and a four-hundred-member club.
/// </summary>
public class StaffRole : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? ClubId { get; set; }

    public StaffRoleKind BaseKind { get; set; } = StaffRoleKind.Receptionist;

    /// <summary>Comma-separated permission keys. Each sensitive action has its own key.</summary>
    public string Permissions { get; set; } = string.Empty;

    public decimal? DiscountLimitPercent { get; set; }
    public decimal? RefundLimit { get; set; }
    public decimal? WriteOffLimit { get; set; }

    public bool CanOverridePolicies { get; set; }
    public bool CanViewMedicalData { get; set; }
    public bool CanExportMemberData { get; set; }

    public bool IsSystemRole { get; set; }
}

/// <summary>
/// A qualification with an expiry date.
///
/// Enforced rather than filed: a club whose trainer's first-aid certificate lapsed is uninsured,
/// and the option to stop booking them the day it expires is the whole reason to store it here
/// rather than in a folder.
/// </summary>
public class StaffCertification : BaseEntity
{
    public Guid StaffId { get; set; }
    public FitnessStaff? Staff { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>First aid, CPR/AED, PT level 3, lifeguard, background check, insurance.</summary>
    public string? Category { get; set; }

    public string? IssuingBody { get; set; }
    public string? ReferenceNumber { get; set; }

    public DateTime? IssuedOn { get; set; }
    public DateTime? ExpiresOn { get; set; }

    public CertificationStatus Status { get; set; } = CertificationStatus.Valid;

    public string? DocumentUrl { get; set; }

    /// <summary>Stops this person being booked or rostered once it lapses.</summary>
    public bool BlocksWorkOnExpiry { get; set; }

    public bool ReminderSent { get; set; }
}

/// <summary>A slot on the rota that somebody has to cover.</summary>
public class Shift : BaseEntity
{
    public Guid ClubId { get; set; }

    public string? Title { get; set; }

    /// <summary>Reception, floor, class cover, cleaning, lifeguard, duty manager.</summary>
    public string? Position { get; set; }

    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }

    public int BreakMinutes { get; set; }

    public ShiftStatus Status { get; set; } = ShiftStatus.Draft;

    /// <summary>People needed on this shift; more than one makes it a team slot.</summary>
    public int RequiredHeadcount { get; set; } = 1;
    public int AssignedHeadcount { get; set; }

    /// <summary>The certification someone must hold to take it — lifeguard, first aid.</summary>
    public string? RequiredCertification { get; set; }

    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }

    public string? Note { get; set; }

    public ICollection<ShiftAssignment> Assignments { get; set; } = [];
}

/// <summary>One person on one shift, and whether they actually turned up.</summary>
public class ShiftAssignment : BaseEntity
{
    public Guid ShiftId { get; set; }
    public Shift? Shift { get; set; }

    public Guid StaffId { get; set; }
    public FitnessStaff? Staff { get; set; }

    public ShiftStatus Status { get; set; } = ShiftStatus.Confirmed;

    public DateTime? ConfirmedAt { get; set; }
    public DateTime? ClockedInAt { get; set; }
    public DateTime? ClockedOutAt { get; set; }

    public bool WasNoShow { get; set; }
    public bool WasLate { get; set; }
    public int? LateMinutes { get; set; }

    public string? Note { get; set; }
}

/// <summary>Someone asking for their shift to be covered, and who picked it up.</summary>
public class ShiftSwapRequest : BaseEntity
{
    public Guid ShiftAssignmentId { get; set; }
    public ShiftAssignment? ShiftAssignment { get; set; }

    public Guid RequestedByStaffId { get; set; }

    /// <summary>Null means it was offered to everybody rather than to one person.</summary>
    public Guid? OfferedToStaffId { get; set; }

    public Guid? AcceptedByStaffId { get; set; }

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }

    public string? Reason { get; set; }

    public bool IsApproved { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public bool IsCancelled { get; set; }
}

/// <summary>A clock-in and clock-out pair, which is what payroll is actually paid on.</summary>
public class TimeClockEntry : BaseEntity
{
    public Guid StaffId { get; set; }
    public FitnessStaff? Staff { get; set; }

    public Guid ClubId { get; set; }
    public Guid? ShiftAssignmentId { get; set; }

    public DateTime ClockedInAt { get; set; }
    public DateTime? ClockedOutAt { get; set; }

    public int BreakMinutes { get; set; }

    /// <summary>Worked minutes less breaks. Held so a timesheet is a sum, not a calculation per row.</summary>
    public int? WorkedMinutes { get; set; }

    /// <summary>Desk terminal, kiosk, mobile — and where they were, when the club checks.</summary>
    public string? Device { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public bool GeofencePassed { get; set; } = true;

    public bool IsApproved { get; set; }
    public Guid? ApprovedByUserId { get; set; }

    /// <summary>A manager corrected the times; the original is kept in the note.</summary>
    public bool WasEdited { get; set; }
    public string? EditNote { get; set; }
}

/// <summary>
/// How a member of staff earns beyond their hourly rate.
///
/// The part generic systems get wrong. A trainer is often paid three ways at once — a rate per
/// session delivered, a percentage of the packages they sell, and a bonus on hitting a target —
/// and each has its own base, its own tiers and its own period. So the rules are rows, and a
/// payslip is the sum of however many of them apply.
/// </summary>
public class CommissionRule : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? ClubId { get; set; }

    /// <summary>Null applies it to everyone matching <see cref="AppliesToRole"/>.</summary>
    public Guid? StaffId { get; set; }
    public StaffRoleKind? AppliesToRole { get; set; }

    public CommissionBasis Basis { get; set; }

    /// <summary>Flat amount per unit, for a per-session or per-class rule.</summary>
    public decimal RatePerUnit { get; set; }

    /// <summary>Percentage, for a value-based rule.</summary>
    public decimal Percentage { get; set; }

    /// <summary>Units or value that must be passed before the rule pays anything.</summary>
    public decimal Threshold { get; set; }

    /// <summary>Rate above the threshold, for a tiered rule. Zero means the same rate throughout.</summary>
    public decimal AcceleratedRate { get; set; }

    /// <summary>Most a single period can pay under this rule. Zero is uncapped.</summary>
    public decimal PeriodCap { get; set; }

    /// <summary>Restricts the rule to one service or one class type.</summary>
    public Guid? ServiceId { get; set; }
    public Guid? ClassTypeId { get; set; }
    public Guid? PlanId { get; set; }

    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    public DateTime? EffectiveTo { get; set; }

    public int Priority { get; set; }
}

/// <summary>
/// One earned commission line, accrued when the thing that earns it happens rather than at
/// month end — so a trainer can see today what they have earned today.
/// </summary>
public class CommissionAccrual : BaseEntity
{
    public Guid StaffId { get; set; }
    public FitnessStaff? Staff { get; set; }

    public Guid ClubId { get; set; }
    public Guid? CommissionRuleId { get; set; }
    public Guid? CommissionStatementId { get; set; }

    public CommissionBasis Basis { get; set; }
    public DateTime EarnedOn { get; set; } = DateTime.UtcNow;

    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    /// <summary>The number the rate was applied to — a session count, a sale value, a headcount.</summary>
    public decimal BaseValue { get; set; }
    public decimal Quantity { get; set; } = 1;

    /// <summary>What earned it, so a statement line drills to the session or the sale behind it.</summary>
    public Guid? SourceEntityId { get; set; }
    public string? SourceEntityType { get; set; }
    public string? Narrative { get; set; }

    public Guid? MemberId { get; set; }

    /// <summary>Reversed when the underlying sale is refunded or the session is un-signed.</summary>
    public bool IsReversed { get; set; }
    public string? ReversalReason { get; set; }
}

/// <summary>A period's commission for one person: the total, the lines behind it, and its approval.</summary>
public class CommissionStatement : BaseEntity
{
    public string StatementNumber { get; set; } = string.Empty;

    public Guid StaffId { get; set; }
    public FitnessStaff? Staff { get; set; }

    public Guid ClubId { get; set; }

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    public CommissionStatementStatus Status { get; set; } = CommissionStatementStatus.Draft;

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
    public Guid? ApprovedByUserId { get; set; }
    public string? RejectionNote { get; set; }

    /// <summary>Handed to HR payroll as one posted total rather than as a hundred lines.</summary>
    public DateTime? ExportedAt { get; set; }
    public string? PayrollReference { get; set; }
}

/// <summary>A performance target for a member of staff, and how it is tracking.</summary>
public class StaffTarget : BaseEntity
{
    public Guid StaffId { get; set; }
    public Guid ClubId { get; set; }

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    /// <summary>Sessions, classes, sales, retention, NPS — whatever this club manages against.</summary>
    public string MetricName { get; set; } = string.Empty;

    public decimal TargetValue { get; set; }
    public decimal ActualValue { get; set; }
    public int AchievementPercent { get; set; }

    public decimal? Bonus { get; set; }
    public bool IsAchieved { get; set; }
    public bool BonusPaid { get; set; }
}
