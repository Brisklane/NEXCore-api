using Fitness.Domain.Enums;
using Nexcore.SharedKernel;

namespace Fitness.Domain.Entities;

/// <summary>
/// A person the club has a relationship with.
///
/// One record covers the whole arc — lead, trial, member, ex-member, rejoiner — rather than
/// copying a prospect into a new row when they sign. That matters more than it looks: the day
/// someone joins, everything already known about them (which class they trialled, who toured
/// them, what they said they wanted) has to still be attached, and a system that starts a fresh
/// record throws that away exactly when it becomes valuable.
///
/// <see cref="ContactId"/> points at the CRM contact. Fitness does not build a second contact
/// master; it owns the *membership*, not the person.
/// </summary>
public class Member : BaseEntity
{
    /// <summary>Human-facing membership number. Distinct from <see cref="BaseEntity.Code"/> only in that it is never null for a joined member.</summary>
    public string MemberNumber { get; set; } = string.Empty;

    /// <summary>CRM contact this person is. Null until they are more than a walk-in enquiry.</summary>
    public Guid? ContactId { get; set; }

    // ── Identity ─────────────────────────────────────────────────────────────

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    /// <summary>What they actually want to be called, which is what the kiosk greets them with.</summary>
    public string? PreferredName { get; set; }

    public DateTime? DateOfBirth { get; set; }
    public Gender Gender { get; set; } = Gender.Unspecified;
    public string? NationalId { get; set; }
    public string? Occupation { get; set; }
    public string? PhotoUrl { get; set; }

    // ── Contact ──────────────────────────────────────────────────────────────

    public string? Phone { get; set; }
    public string? AlternatePhone { get; set; }
    public string? Email { get; set; }
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? PostCode { get; set; }
    public string? CountryCode { get; set; }
    public string? PreferredLanguage { get; set; }
    public MessageChannel PreferredChannel { get; set; } = MessageChannel.Email;

    // ── Membership ───────────────────────────────────────────────────────────

    public MemberStatus Status { get; set; } = MemberStatus.Lead;

    /// <summary>The club they belong to. Cross-club access is an entitlement, not a second home.</summary>
    public Guid HomeClubId { get; set; }
    public FitnessClub? HomeClub { get; set; }

    public DateTime? JoinedOn { get; set; }
    public DateTime? LeftOn { get; set; }

    /// <summary>Set on rejoin so tenure and win-back reporting stay honest across a gap.</summary>
    public DateTime? FirstJoinedOn { get; set; }

    public Guid? HouseholdId { get; set; }
    public Household? Household { get; set; }

    /// <summary>Corporate scheme this membership sits under, when one pays or discounts it.</summary>
    public Guid? CorporateAccountId { get; set; }

    /// <summary>The coach or trainer who owns this member's retention. Null means the club does.</summary>
    public Guid? AssignedCoachId { get; set; }

    /// <summary>Where they originally came from. Kept forever — it is how marketing spend is judged.</summary>
    public Guid? LeadSourceId { get; set; }
    public Guid? ReferredByMemberId { get; set; }

    // ── Rolled-up state ──────────────────────────────────────────────────────
    // Denormalised because the desk reads them on every check-in and the at-risk board reads them
    // for thousands of members at once; recomputing either from the event tables on each read is
    // the difference between a screen that opens and one that does not.

    public decimal AccountBalance { get; set; }
    public decimal CreditBalance { get; set; }
    public DateTime? LastVisitOn { get; set; }
    public int TotalVisits { get; set; }
    public int VisitsThisMonth { get; set; }

    /// <summary>Average visits per week over the last 90 days — the baseline churn scoring compares against.</summary>
    public decimal VisitFrequencyBaseline { get; set; }

    public DateTime? NextBillingOn { get; set; }
    public ChurnRiskBand RiskBand { get; set; } = ChurnRiskBand.Healthy;
    public int CurrentStreakDays { get; set; }
    public int LoyaltyPoints { get; set; }

    // ── Safety & compliance ──────────────────────────────────────────────────

    public bool WaiverSigned { get; set; }
    public DateTime? WaiverSignedOn { get; set; }
    public Guid? WaiverTemplateVersionId { get; set; }

    public ClearanceStatus MedicalClearance { get; set; } = ClearanceStatus.NotRequired;

    /// <summary>A short line the desk sees on every check-in — allergies, a condition, a caution.</summary>
    public string? MedicalSummary { get; set; }

    public bool IsBanned { get; set; }
    public string? BanReason { get; set; }
    public DateTime? BanUntil { get; set; }
    public Guid? BannedByUserId { get; set; }

    /// <summary>Consent to appear in photos, leaderboards and marketing. Absent means no.</summary>
    public bool PhotoConsent { get; set; }
    public bool LeaderboardOptIn { get; set; } = true;

    // ── Data protection ──────────────────────────────────────────────────────

    /// <summary>
    /// Set when the member has exercised erasure. Identity fields are replaced with a tombstone
    /// while the financial and incident records law requires the club to keep are preserved.
    /// </summary>
    public bool IsAnonymised { get; set; }
    public DateTime? AnonymisedAt { get; set; }

    public ICollection<MemberNote> Notes { get; set; } = [];
    public ICollection<MemberTag> Tags { get; set; } = [];
    public ICollection<MemberCredential> Credentials { get; set; } = [];
    public ICollection<Agreement> Agreements { get; set; } = [];
    public ICollection<EmergencyContact> EmergencyContacts { get; set; } = [];
}

/// <summary>
/// A family or a couple. One payer, several people, and the rules about who may check whom in.
/// </summary>
public class Household : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>The member who pays and who the agreement is with.</summary>
    public Guid PrimaryMemberId { get; set; }

    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? PostCode { get; set; }

    /// <summary>Lets any adult in the household check a child in without the payer present.</summary>
    public bool AnyAdultMayCheckInChildren { get; set; } = true;

    public ICollection<HouseholdMember> Members { get; set; } = [];
}

/// <summary>Who is in a household, and in what capacity.</summary>
public class HouseholdMember : BaseEntity
{
    public Guid HouseholdId { get; set; }
    public Household? Household { get; set; }

    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public HouseholdRole Role { get; set; } = HouseholdRole.Other;

    /// <summary>This person may collect the household's children from the creche.</summary>
    public bool MayCollectChildren { get; set; }

    /// <summary>
    /// Date a junior ages out of the family plan. Set from the child's birthday when they join so
    /// the eighteenth-birthday conversation is a task in the diary rather than a surprise.
    /// </summary>
    public DateTime? AgesOutOn { get; set; }
}

/// <summary>Who to call, and how they are related. Surfaced at the top of the record because that is when it is needed.</summary>
public class EmergencyContact : BaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? AlternatePhone { get; set; }
    public string? Email { get; set; }

    /// <summary>The one the desk calls first.</summary>
    public bool IsPrimary { get; set; }
}

/// <summary>
/// Something clinically or practically relevant about this member's body, shown to instructors on
/// the class roster and to trainers on the session screen.
///
/// Special-category data: reads are logged and it is excluded from ordinary exports.
/// </summary>
public class MedicalFlag : BaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    /// <summary>Condition, medication, allergy, injury, mobility limitation, pregnancy.</summary>
    public string Category { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;

    public AlertSeverity Severity { get; set; } = AlertSeverity.Info;

    /// <summary>Shows on the class roster and PT screen. Some notes are for the office only.</summary>
    public bool VisibleToInstructors { get; set; } = true;

    public DateTime? ReviewOn { get; set; }
    public DateTime? ResolvedOn { get; set; }
}

/// <summary>
/// Anything that happened with this member — a note, a call, a complaint, a status change — on one
/// timeline. One table rather than six, because the value is in reading them interleaved.
/// </summary>
public class MemberNote : BaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public InteractionKind Kind { get; set; } = InteractionKind.Note;
    public string Body { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    /// <summary>Visible only to managers — a conduct note, a negotiation position.</summary>
    public bool IsPrivate { get; set; }

    /// <summary>Kept on the timeline until it is dealt with, and shown at check-in.</summary>
    public bool IsPinned { get; set; }

    public Guid? StaffId { get; set; }
    public string? AuthorName { get; set; }

    /// <summary>Links the entry to whatever it was about — a booking, an invoice, an incident.</summary>
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
}

/// <summary>
/// A label on a member. System tags (at-risk, VIP, corporate) are maintained by the app; free tags
/// are whatever the club finds useful. Both drive segments, automations and reports.
/// </summary>
public class MemberTag : BaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public string Tag { get; set; } = string.Empty;

    /// <summary>True when the app maintains it, so a human edit does not get silently overwritten.</summary>
    public bool IsSystemTag { get; set; }

    public string? ColourHex { get; set; }
}

/// <summary>
/// Something the desk must see when this member arrives.
///
/// Held as rows rather than computed on the fly so the check-in path is a single indexed read: a
/// turnstile has 300 ms, and evaluating twelve business rules per scan does not fit in it.
/// </summary>
public class MemberAlert : BaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public MemberAlertKind Kind { get; set; }
    public AlertSeverity Severity { get; set; } = AlertSeverity.Info;

    public string Message { get; set; } = string.Empty;

    /// <summary>What the desk can do about it right now — "Take payment", "Sign waiver".</summary>
    public string? ActionLabel { get; set; }
    public string? ActionRoute { get; set; }

    /// <summary>Stops entry outright rather than just warning.</summary>
    public bool BlocksAccess { get; set; }

    public DateTime? ExpiresOn { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public Guid? AcknowledgedByUserId { get; set; }
}

/// <summary>Every status change, with who made it and why. The record that settles disputes about dates.</summary>
public class MemberStatusHistory : BaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public MemberStatus FromStatus { get; set; }
    public MemberStatus ToStatus { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    public string? Reason { get; set; }
    public Guid? ChangedByUserId { get; set; }

    /// <summary>The agreement, freeze or dunning case that caused it.</summary>
    public Guid? SourceEntityId { get; set; }
    public string? SourceEntityType { get; set; }
}

/// <summary>A file attached to a member — a signed waiver, an ID scan, a medical letter, a contract PDF.</summary>
public class MemberDocument : BaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public DocumentKind Kind { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long SizeBytes { get; set; }

    public DateTime? ValidFrom { get; set; }
    public DateTime? ExpiresOn { get; set; }

    /// <summary>Special-category documents are hidden from ordinary exports and their reads are logged.</summary>
    public bool IsSensitive { get; set; }

    public Guid? UploadedByUserId { get; set; }
}

/// <summary>
/// How this member identifies themselves at a door or a desk.
///
/// One credential belongs to one member at a time — that is enforced by a unique index rather
/// than by hope, because a fob that opens for two people makes every attendance number a lie.
/// </summary>
public class MemberCredential : BaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public CredentialType Type { get; set; }

    /// <summary>
    /// The card number, tag id, or device identifier. For biometrics this is a *reference* to a
    /// template held on the reader — never the template itself.
    /// </summary>
    public string Identifier { get; set; } = string.Empty;

    public CredentialStatus Status { get; set; } = CredentialStatus.Active;

    public DateTime IssuedOn { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresOn { get; set; }
    public DateTime? DeactivatedOn { get; set; }
    public string? DeactivationReason { get; set; }

    /// <summary>Charged when a lost fob is replaced.</summary>
    public decimal ReplacementFee { get; set; }

    /// <summary>The credential this one replaced, so the history reads as a chain.</summary>
    public Guid? ReplacesCredentialId { get; set; }

    public DateTime? LastUsedAt { get; set; }
    public Guid? IssuedByUserId { get; set; }
}

/// <summary>
/// What this member has agreed to be contacted about, per channel and per purpose.
///
/// Stored with the wording they consented to and the timestamp, because "we have consent" is not
/// a defensible answer to a regulator — "they ticked this sentence on this date" is.
/// </summary>
public class MemberConsent : BaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public MessageChannel Channel { get; set; }

    /// <summary>Marketing, service messages, class reminders, research, photography.</summary>
    public string Purpose { get; set; } = string.Empty;

    public bool Granted { get; set; }
    public DateTime DecidedAt { get; set; } = DateTime.UtcNow;

    /// <summary>The exact sentence agreed to, kept verbatim.</summary>
    public string? ConsentText { get; set; }

    /// <summary>Where it was given: join form, app, kiosk, imported from the old system.</summary>
    public string? CapturedVia { get; set; }

    public string? IpAddress { get; set; }
}

/// <summary>Per-member display and communication preferences that are not consent decisions.</summary>
public class MemberPreference : BaseEntity
{
    public Guid MemberId { get; set; }
    public Member? Member { get; set; }

    public UnitSystem Units { get; set; } = UnitSystem.Metric;

    /// <summary>Suppresses class reminder pushes for someone who finds them noisy.</summary>
    public bool ClassReminders { get; set; } = true;
    public int ClassReminderMinutesBefore { get; set; } = 120;

    public bool BillingReminders { get; set; } = true;
    public bool MarketingMessages { get; set; } = true;
    public bool ShowOnLeaderboards { get; set; } = true;

    /// <summary>Favourite classes, trainers or times, used to personalise the app home.</summary>
    public string? InterestsJson { get; set; }
}
