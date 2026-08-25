using Fitness.Domain.Enums;
using Nexcore.SharedKernel;

namespace Fitness.Domain.Entities;

/// <summary>
/// A liability waiver, versioned.
///
/// Versioning is not bureaucracy here: a waiver is only worth anything if the club can show the
/// exact wording a member agreed to on the day they agreed to it, and a template edited in place
/// destroys every signature that came before it.
/// </summary>
public class WaiverTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; } = 1;

    public Guid? ClubId { get; set; }
    public string? CountryCode { get; set; }
    public string? LanguageCode { get; set; }

    /// <summary>Restricts the waiver to one activity — climbing, contact sport, junior programmes.</summary>
    public Guid? ClassTypeId { get; set; }
    public string? ActivityScope { get; set; }

    public string BodyHtml { get; set; } = string.Empty;

    /// <summary>Separate tick-boxes the member must each agree to, rather than one blanket consent.</summary>
    public string? ConsentClausesJson { get; set; }

    public bool RequiresGuardianSignature { get; set; }
    public int? GuardianRequiredBelowAge { get; set; } = 18;

    /// <summary>Days a signature stays valid. Zero means it does not expire.</summary>
    public int ValidForDays { get; set; }

    /// <summary>Blocks entry until it is signed, rather than merely warning.</summary>
    public bool BlocksAccess { get; set; } = true;

    public DateTime EffectiveFrom { get; set; } = DateTime.UtcNow;
    public DateTime? EffectiveTo { get; set; }

    public bool IsPublished { get; set; }

    /// <summary>Existing signatures are marked superseded, forcing everyone to re-sign.</summary>
    public bool RequiresResignOnNewVersion { get; set; } = true;
}

/// <summary>A signed waiver, with everything needed to stand behind it.</summary>
public class WaiverSignature : BaseEntity
{
    public Guid WaiverTemplateId { get; set; }
    public WaiverTemplate? WaiverTemplate { get; set; }

    public int TemplateVersion { get; set; }

    public Guid? MemberId { get; set; }
    public Member? Member { get; set; }

    /// <summary>Set for a guest or a day-pass visitor with no member record.</summary>
    public string? SignerName { get; set; }
    public string? SignerEmail { get; set; }
    public string? SignerPhone { get; set; }
    public DateTime? SignerDateOfBirth { get; set; }

    public Guid? GuestVisitId { get; set; }
    public Guid? DayPassId { get; set; }
    public Guid ClubId { get; set; }

    public SignatureStatus Status { get; set; } = SignatureStatus.NotSigned;
    public DateTime? SignedAt { get; set; }
    public DateTime? ExpiresOn { get; set; }

    public string? GuardianName { get; set; }
    public string? GuardianRelationship { get; set; }
    public string? GuardianSignatureUrl { get; set; }

    public string? SignatureImageUrl { get; set; }
    public string? DocumentUrl { get; set; }

    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? CapturedVia { get; set; }

    /// <summary>Which clauses were ticked, kept as given.</summary>
    public string? ConsentAnswersJson { get; set; }

    /// <summary>Token for a remote signing link, cleared once used.</summary>
    public string? RemoteToken { get; set; }
    public DateTime? RemoteTokenExpiresOn { get; set; }
}

/// <summary>
/// A health questionnaire — a PAR-Q or the club's own.
///
/// A "yes" on a gating question is what raises a medical-clearance requirement, which is what
/// blocks participation until a doctor's letter is on file. That chain is the whole point; a
/// screening form that just gets filed is a form nobody should have made anyone fill in.
/// </summary>
public class HealthScreening : BaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public Guid ClubId { get; set; }

    public string TemplateName { get; set; } = "PAR-Q+";
    public int TemplateVersion { get; set; } = 1;

    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresOn { get; set; }

    /// <summary>True when any gating answer means they cannot start unsupervised.</summary>
    public bool RequiresClearance { get; set; }

    public ClearanceStatus ClearanceStatus { get; set; } = ClearanceStatus.NotRequired;

    /// <summary>A short line for the desk and the instructor, derived from the answers.</summary>
    public string? RiskSummary { get; set; }

    public Guid? ReviewedByStaffId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }

    public string? CapturedVia { get; set; }

    public ICollection<HealthScreeningAnswer> Answers { get; set; } = [];
}

/// <summary>One question and its answer on a health screening.</summary>
public class HealthScreeningAnswer : BaseEntity
{
    public Guid HealthScreeningId { get; set; }
    public HealthScreening? HealthScreening { get; set; }

    public int QuestionNumber { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public ScreeningAnswerKind AnswerKind { get; set; } = ScreeningAnswerKind.YesNo;

    public bool? BooleanAnswer { get; set; }
    public string? TextAnswer { get; set; }
    public decimal? NumericAnswer { get; set; }
    public DateTime? DateAnswer { get; set; }

    /// <summary>An affirmative here raises the clearance requirement on its own.</summary>
    public bool IsGatingQuestion { get; set; }

    /// <summary>Follow-up detail the form asked for once they said yes.</summary>
    public string? FollowUpAnswer { get; set; }
}

/// <summary>A doctor's sign-off that a member may train, and what it permits.</summary>
public class MedicalClearance : BaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public Guid ClubId { get; set; }
    public Guid? HealthScreeningId { get; set; }

    public ClearanceStatus Status { get; set; } = ClearanceStatus.Required;

    public DateTime RequestedOn { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedOn { get; set; }
    public DateTime? ApprovedOn { get; set; }
    public DateTime? ExpiresOn { get; set; }

    public string? PractitionerName { get; set; }
    public string? PractitionerRegistration { get; set; }
    public string? PracticeName { get; set; }

    /// <summary>What they may not do — no high-impact, no supine work, supervised only.</summary>
    public string? Restrictions { get; set; }

    public Guid? DocumentId { get; set; }

    public Guid? ApprovedByStaffId { get; set; }
    public string? RejectionReason { get; set; }

    /// <summary>Blocks class booking and entry until it is approved.</summary>
    public bool BlocksParticipation { get; set; } = true;
}

/// <summary>
/// Something that happened and should not have.
///
/// A legal record: append-only in spirit, exportable, and never quietly edited. Timely
/// documentation is what an insurer asks for and what a club regrets not having.
/// </summary>
public class Incident : BaseEntity
{
    public string IncidentNumber { get; set; } = string.Empty;

    public Guid ClubId { get; set; }
    public Guid? AreaId { get; set; }
    public Guid? EquipmentAssetId { get; set; }

    public IncidentKind Kind { get; set; }
    public IncidentSeverity Severity { get; set; } = IncidentSeverity.Minor;
    public IncidentStatus Status { get; set; } = IncidentStatus.Open;

    public DateTime OccurredAt { get; set; }
    public DateTime ReportedAt { get; set; } = DateTime.UtcNow;

    public Guid? MemberId { get; set; }
    public string? InvolvedPersonName { get; set; }
    public string? InvolvedPersonPhone { get; set; }

    public Guid? ReportedByStaffId { get; set; }

    public string Summary { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public string? WitnessNames { get; set; }
    public string? WitnessStatements { get; set; }

    // ── Response ─────────────────────────────────────────────────────────────

    public bool FirstAidGiven { get; set; }
    public string? FirstAiderName { get; set; }
    public bool AedUsed { get; set; }
    public bool AmbulanceCalled { get; set; }
    public bool HospitalAttended { get; set; }

    public string? ImmediateAction { get; set; }
    public string? PhotoUrls { get; set; }

    // ── Follow-up ────────────────────────────────────────────────────────────

    public Guid? OwnerStaffId { get; set; }
    public DateTime? ReviewDueOn { get; set; }
    public DateTime? ClosedOn { get; set; }
    public string? RootCause { get; set; }
    public string? PreventiveAction { get; set; }

    /// <summary>Reportable to a regulator, and whether it has been.</summary>
    public bool IsReportable { get; set; }
    public bool WasReported { get; set; }
    public DateTime? ReportedToAuthorityOn { get; set; }
    public string? AuthorityReference { get; set; }

    public bool InsurerNotified { get; set; }
    public string? InsurerReference { get; set; }
    public decimal? EstimatedCost { get; set; }

    public ICollection<IncidentAction> Actions { get; set; } = [];
}

/// <summary>Something done about an incident, by whom, and when it was finished.</summary>
public class IncidentAction : BaseEntity
{
    public Guid IncidentId { get; set; }
    public Incident? Incident { get; set; }

    public string Action { get; set; } = string.Empty;
    public Guid? AssignedStaffId { get; set; }

    public DateTime RaisedOn { get; set; } = DateTime.UtcNow;
    public DateTime? DueOn { get; set; }
    public DateTime? CompletedOn { get; set; }

    public string? CompletionNote { get; set; }
    public bool IsOverdue { get; set; }
}

/// <summary>Something found, where it is, and what happened to it.</summary>
public class LostPropertyItem : BaseEntity
{
    public Guid ClubId { get; set; }

    public string ItemDescription { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? PhotoUrl { get; set; }

    public DateTime FoundOn { get; set; } = DateTime.UtcNow;
    public string? FoundLocation { get; set; }
    public Guid? FoundByStaffId { get; set; }

    public string? StorageLocation { get; set; }
    public LostPropertyStatus Status { get; set; } = LostPropertyStatus.Held;

    public Guid? ClaimedByMemberId { get; set; }
    public string? ClaimedByName { get; set; }
    public DateTime? ClaimedOn { get; set; }
    public Guid? ReleasedByStaffId { get; set; }

    /// <summary>Date the club may dispose of it under its own policy.</summary>
    public DateTime? DisposeAfter { get; set; }
    public DateTime? DisposedOn { get; set; }
    public string? DisposalNote { get; set; }
}

/// <summary>A member complaint, with an owner and a clock on it.</summary>
public class Complaint : BaseEntity
{
    public string ComplaintNumber { get; set; } = string.Empty;

    public Guid ClubId { get; set; }
    public Guid? MemberId { get; set; }
    public Member? Member { get; set; }

    public string? ComplainantName { get; set; }
    public string? ComplainantContact { get; set; }

    /// <summary>Cleanliness, equipment, staff, classes, billing, noise, other.</summary>
    public string Category { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? Detail { get; set; }

    public ComplaintStatus Status { get; set; } = ComplaintStatus.Open;
    public int Priority { get; set; } = 2;

    public DateTime RaisedOn { get; set; } = DateTime.UtcNow;
    public DateTime? AcknowledgedOn { get; set; }
    public DateTime? TargetResolutionOn { get; set; }
    public DateTime? ResolvedOn { get; set; }

    public Guid? OwnerStaffId { get; set; }
    public string? Resolution { get; set; }

    /// <summary>Goodwill given — a free month, a refund, points.</summary>
    public decimal? CompensationValue { get; set; }
    public string? CompensationNote { get; set; }

    /// <summary>Whether the complainant was satisfied, which is the only measure that counts.</summary>
    public bool? ComplainantSatisfied { get; set; }

    public string? Channel { get; set; }
}

/// <summary>A recurring check the club has to do — opening, closing, cleaning, pool chemistry.</summary>
public class FacilityCheck : BaseEntity
{
    public Guid ClubId { get; set; }
    public Guid? AreaId { get; set; }

    public string Name { get; set; } = string.Empty;
    public FacilityCheckKind Kind { get; set; } = FacilityCheckKind.Opening;

    /// <summary>Bit flags, Sunday = 1 … Saturday = 64.</summary>
    public int DaysOfWeekMask { get; set; } = 127;
    public TimeSpan DueAt { get; set; }

    /// <summary>Several times a day, for pool chemistry and busy-period sweeps.</summary>
    public int TimesPerDay { get; set; } = 1;

    public Guid? DefaultAssigneeRoleId { get; set; }

    /// <summary>Alerts a manager when it is skipped, which is the only reason to have it in software.</summary>
    public bool AlertOnMissed { get; set; } = true;
    public int MissedAfterMinutes { get; set; } = 60;

    public bool RequiresSignature { get; set; }

    public ICollection<FacilityCheckItem> Items { get; set; } = [];
}

/// <summary>One line on a check, and what was recorded against it.</summary>
public class FacilityCheckItem : BaseEntity
{
    public Guid FacilityCheckId { get; set; }
    public FacilityCheck? FacilityCheck { get; set; }

    public string ItemDescription { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    /// <summary>Tick, a number (a temperature, a pH), or free text.</summary>
    public ScreeningAnswerKind AnswerKind { get; set; } = ScreeningAnswerKind.YesNo;
    public string? Unit { get; set; }

    /// <summary>Acceptable band, so an out-of-range reading raises a corrective action by itself.</summary>
    public decimal? AcceptableLow { get; set; }
    public decimal? AcceptableHigh { get; set; }

    public bool IsCritical { get; set; }
    public bool RequiresPhoto { get; set; }

    // ── The most recent run's answer, so today's board is one read ───────────

    public DateTime? LastCompletedAt { get; set; }
    public Guid? LastCompletedByStaffId { get; set; }
    public bool? LastPassed { get; set; }
    public decimal? LastValue { get; set; }
    public string? LastNote { get; set; }
}

/// <summary>What the next shift needs to know, timestamped so nothing gets lost in a handover.</summary>
public class ShiftHandover : BaseEntity
{
    public Guid ClubId { get; set; }

    public DateTime ShiftEndedAt { get; set; } = DateTime.UtcNow;
    public Guid? FromStaffId { get; set; }
    public Guid? ToStaffId { get; set; }

    public string Notes { get; set; } = string.Empty;

    /// <summary>Things left undone that the next person has to pick up.</summary>
    public string? OutstandingItems { get; set; }

    public bool HasUrgentItems { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public Guid? AcknowledgedByStaffId { get; set; }
}

/// <summary>A notice for members, shown on the kiosk, the app and the club screens.</summary>
public class Announcement : BaseEntity
{
    public Guid? ClubId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }

    public DateTime ShowFrom { get; set; } = DateTime.UtcNow;
    public DateTime? ShowUntil { get; set; }

    public bool ShowOnKiosk { get; set; } = true;
    public bool ShowInApp { get; set; } = true;
    public bool ShowOnClubScreens { get; set; }

    /// <summary>Pinned to the top and styled as urgent — a closure, a class cancellation.</summary>
    public bool IsUrgent { get; set; }

    public Guid? SegmentId { get; set; }
    public bool IsPublished { get; set; }
}

/// <summary>
/// A record that somebody looked at, changed or exported something sensitive.
///
/// Health data, money movements and entitlement changes are all logged here. Reading a member's
/// medical flags is itself an auditable event, which is the standard special-category data is
/// held to and the reason this is a table rather than a log line.
/// </summary>
public class AuditEntry : BaseEntity
{
    public Guid? ClubId { get; set; }

    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    public Guid? ActorUserId { get; set; }
    public Guid? ActorStaffId { get; set; }
    public string? ActorName { get; set; }

    /// <summary>Viewed, created, updated, deleted, exported, overrode, approved.</summary>
    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;
    public Guid? EntityId { get; set; }
    public Guid? MemberId { get; set; }

    /// <summary>What changed, for an update. Never contains card data or a biometric template.</summary>
    public string? ChangeSummary { get; set; }

    /// <summary>True for medical, biometric, screening and progress-photo access.</summary>
    public bool IsSensitiveAccess { get; set; }

    public string? Reason { get; set; }
    public string? IpAddress { get; set; }
}
