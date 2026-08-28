using Nexcore.SharedKernel;
using RealEstate.Domain.Enums;

namespace RealEstate.Domain.Entities;

/// <summary>
/// A residents' association or estate community.
///
/// What the developer becomes on the day after possession. Refusing to model it hands the customer
/// to a separate society app on the day the project completes — and with it, the relationship.
/// </summary>
public class Society : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public Guid? ProjectId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? GeoAreaId { get; set; }

    public string? RegistrationNumber { get; set; }
    public DateOnly? RegisteredOn { get; set; }
    public string? RegistrationAuthority { get; set; }
    public string? ConstitutionUrl { get; set; }

    /// <summary>Month the society's financial year starts. Rarely January in practice.</summary>
    public int FinancialYearStartMonth { get; set; } = 4;

    public int TotalUnits { get; set; }
    public int OccupiedUnits { get; set; }
    public decimal TotalAreaSqFt { get; set; }

    // ── Money ────────────────────────────────────────────────────────────────

    public Guid? BankAccountId { get; set; }
    public Guid? ClientAccountId { get; set; }
    public Guid? SinkingFundId { get; set; }
    public decimal MonthlyBillingTotal { get; set; }
    public decimal OutstandingTotal { get; set; }
    public decimal CorpusFundBalance { get; set; }

    // ── Handover from the developer ──────────────────────────────────────────

    public bool IsHandedOver { get; set; }
    public DateOnly? HandoverDate { get; set; }
    public decimal CorpusTransferred { get; set; }
    public bool CommonAreasTransferred { get; set; }
    public bool DocumentsTransferred { get; set; }
    public Guid? HandoverChecklistId { get; set; }

    /// <summary>The developer still runs it. Common for the first year or two after completion.</summary>
    public bool ManagedByDeveloper { get; set; } = true;

    public Guid? FacilityManagerUserId { get; set; }
    public Guid? DefaultDunningPolicyId { get; set; }

    /// <summary>A defaulter loses the club and the pool. Blunt, effective, and standard practice.</summary>
    public bool SuspendAmenitiesOnDefault { get; set; } = true;

    public decimal AmenitySuspensionThreshold { get; set; }

    public ICollection<CommitteeMember> Committee { get; set; } = [];
    public ICollection<Amenity> Amenities { get; set; } = [];
}

public class SocietyCommittee : BaseEntity
{
    public Guid SocietyId { get; set; }
    public string Name { get; set; } = string.Empty;

    public DateOnly TermFrom { get; set; }
    public DateOnly TermTo { get; set; }
    public DateOnly? ElectedOn { get; set; }
    public string? ElectionMinutesUrl { get; set; }
    public bool IsCurrent { get; set; } = true;
}

public class CommitteeMember : BaseEntity
{
    public Guid SocietyId { get; set; }
    public Society? Society { get; set; }
    public Guid? SocietyCommitteeId { get; set; }

    public Guid PartyId { get; set; }
    public Guid? UnitId { get; set; }

    /// <summary>"President", "Secretary", "Treasurer", "Member", "AuditCommittee".</summary>
    public string Position { get; set; } = "Member";

    public DateOnly FromDate { get; set; }
    public DateOnly? ToDate { get; set; }

    /// <summary>Can approve society spending up to the committee's mandate.</summary>
    public bool CanApproveSpend { get; set; }
    public decimal SpendLimit { get; set; }

}

/// <summary>
/// Somebody living in a unit. Not the same as the owner — most disputes in an estate are about
/// which of the two owes the charge, so both are recorded against the unit at once.
/// </summary>
public class Resident : BaseEntity
{
    public Guid SocietyId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid PartyId { get; set; }

    public ResidentKind Kind { get; set; } = ResidentKind.Owner;
    public Guid? TenancyId { get; set; }
    public Guid? HouseholdId { get; set; }

    public DateOnly MovedInOn { get; set; }
    public DateOnly? MovedOutOn { get; set; }
    public bool IsPrimaryContact { get; set; }

    /// <summary>The society's own membership number, which residents quote rather than a unit id.</summary>
    public string? MembershipNumber { get; set; }

    public bool PortalAccessEnabled { get; set; } = true;
    public bool CanApproveVisitors { get; set; } = true;
    public bool CanBookAmenities { get; set; } = true;

    public decimal OutstandingDues { get; set; }
    public bool IsDefaulter { get; set; }
    public bool AmenitiesSuspended { get; set; }

    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? BloodGroup { get; set; }
}

/// <summary>The people sharing one unit, so a visitor approval reaches whoever is home.</summary>
public class ResidentHousehold : BaseEntity
{
    public Guid SocietyId { get; set; }
    public Guid UnitId { get; set; }
    public string? Name { get; set; }

    public int MemberCount { get; set; }
    public int AdultCount { get; set; }
    public int ChildCount { get; set; }
    public int PetCount { get; set; }
    public Guid? PrimaryResidentId { get; set; }
}

public class ResidentVehicle : BaseEntity
{
    public Guid SocietyId { get; set; }
    public Guid? ResidentId { get; set; }
    public Guid? UnitId { get; set; }

    public string RegistrationNumber { get; set; } = string.Empty;
    public string? VehicleType { get; set; }
    public string? MakeModel { get; set; }
    public string? Colour { get; set; }

    public Guid? ParkingSlotId { get; set; }
    public string? StickerNumber { get; set; }
    public DateOnly? StickerExpiresOn { get; set; }

    /// <summary>Read at the gate to open the barrier without a guard intervening.</summary>
    public string? RfidTag { get; set; }

}

/// <summary>
/// A maid, driver, cook or guard employed by a resident. Registered because they pass the gate
/// daily and a society needs to know who is inside it.
/// </summary>
public class DomesticStaff : BaseEntity
{
    public Guid SocietyId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? ResidentId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? PhotoUrl { get; set; }
    public string? IdentityNumber { get; set; }
    public string? IdentityDocumentUrl { get; set; }

    /// <summary>"Maid", "Driver", "Cook", "Nanny", "Gardener", "Guard", "Nurse".</summary>
    public string StaffType { get; set; } = "Maid";

    public string? PassNumber { get; set; }
    public DateOnly? PassIssuedOn { get; set; }
    public DateOnly? PassExpiresOn { get; set; }

    public bool IsPoliceVerified { get; set; }
    public string? VerificationReference { get; set; }
    public DateOnly? VerifiedOn { get; set; }

    /// <summary>Works for several flats — normal, and it must not create four separate people.</summary>
    public bool WorksForMultipleUnits { get; set; }

    public bool IsBlacklisted { get; set; }
    public string? BlacklistReason { get; set; }
}

/// <summary>
/// How the society charges for running itself. All the models the market actually uses, because a
/// society that cannot bill the way its bye-laws say will not buy the software.
/// </summary>
public class MaintenanceChargeScheme : BaseEntity
{
    public Guid SocietyId { get; set; }
    public string Name { get; set; } = string.Empty;

    public MaintenanceBasis Basis { get; set; } = MaintenanceBasis.PerAreaUnit;
    public decimal RatePerSqFt { get; set; }
    public decimal FlatAmount { get; set; }
    public RentFrequency Frequency { get; set; } = RentFrequency.Monthly;

    public PropertySubType? AppliesToSubType { get; set; }
    public decimal? MinAreaSqFt { get; set; }
    public decimal? MaxAreaSqFt { get; set; }

    /// <summary>Empty units are often charged at a lower rate. A bye-law question, not a bug.</summary>
    public decimal VacantUnitPercent { get; set; } = 100m;

    public bool IsTaxable { get; set; }
    public decimal TaxPercent { get; set; }

    // ── Late payment ─────────────────────────────────────────────────────────

    public decimal LateFeePercent { get; set; }
    public decimal LateFeeFlat { get; set; }
    public int GraceDays { get; set; } = 15;

    /// <summary>Rebate for paying the year up front, which is how societies fund their January.</summary>
    public decimal EarlyPaymentDiscountPercent { get; set; }
    public bool AllowAnnualPrepayment { get; set; } = true;

    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }

    public ICollection<MaintenanceChargeSlab> Slabs { get; set; } = [];
}

public class MaintenanceChargeSlab : BaseEntity
{
    public Guid MaintenanceChargeSchemeId { get; set; }
    public MaintenanceChargeScheme? Scheme { get; set; }

    public decimal FromAreaSqFt { get; set; }
    public decimal? ToAreaSqFt { get; set; }
    public decimal Amount { get; set; }
    public decimal RatePerSqFt { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>One period's bill to one unit.</summary>
public class MaintenanceBill : BaseEntity
{
    public string BillNumber { get; set; } = string.Empty;

    public Guid SocietyId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid PartyId { get; set; }

    /// <summary>Owner or tenant, depending on the bye-laws and the tenancy terms.</summary>
    public ResidentKind BilledTo { get; set; } = ResidentKind.Owner;

    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public DateOnly IssuedOn { get; set; }
    public DateOnly DueDate { get; set; }

    public decimal MaintenanceAmount { get; set; }
    public decimal UtilityAmount { get; set; }
    public decimal OtherChargesAmount { get; set; }
    public decimal PenaltyAmount { get; set; }
    public decimal ArrearsBroughtForward { get; set; }
    public decimal LateFeeAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal Balance { get; set; }

    public InstalmentStatus Status { get; set; } = InstalmentStatus.NotDue;
    public int DaysOverdue { get; set; }

    public Guid? DocumentId { get; set; }
    public bool IsSent { get; set; }
    public DateTime? SentAt { get; set; }
    public bool IsDisputed { get; set; }
    public string? DisputeNote { get; set; }

    public ICollection<MaintenanceBillLine> Lines { get; set; } = [];
}

public class MaintenanceBillLine : BaseEntity
{
    public Guid MaintenanceBillId { get; set; }
    public MaintenanceBill? Bill { get; set; }

    /// <summary>"Maintenance", "Water", "Electricity", "Gas", "Generator", "Security", "Parking",
    /// "Club", "SinkingFund", "Penalty", "LateFee", "Arrears", "Adjustment".</summary>
    public string ChargeType { get; set; } = string.Empty;

    public decimal Quantity { get; set; } = 1m;
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }

    public Guid? MeterReadingId { get; set; }
    public Guid? SocietyChargeId { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>A standing extra charge on a unit — a tanker, a parking slot, a club membership.</summary>
public class SocietyCharge : BaseEntity
{
    public Guid SocietyId { get; set; }
    public Guid? UnitId { get; set; }

    /// <summary>"WaterTanker", "GeneratorFuel", "Security", "ParkingRent", "ClubMembership",
    /// "Gym", "Garbage", "Sewerage", "FestivalFund", "SinkingFund".</summary>
    public string ChargeType { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public RentFrequency Frequency { get; set; } = RentFrequency.Monthly;
    public bool IsRecurring { get; set; } = true;

    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsTaxable { get; set; }
}

/// <summary>A fine. Societies run on them, and an unrecorded fine is an unenforceable one.</summary>
public class SocietyPenalty : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid SocietyId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid PartyId { get; set; }

    /// <summary>"IllegalConstruction", "Misuse", "LatePayment", "Nuisance", "ParkingViolation",
    /// "PetViolation", "WasteDisposal", "UnauthorisedSubletting".</summary>
    public string ViolationType { get; set; } = string.Empty;

    public DateOnly ObservedOn { get; set; }
    public decimal Amount { get; set; }

    public Guid? ImposedByUserId { get; set; }
    public Guid? CommitteeResolutionReference { get; set; }
    public string? EvidenceUrl { get; set; }

    public bool NoticeServed { get; set; }
    public DateOnly? NoticeServedOn { get; set; }
    public int AppealWindowDays { get; set; } = 14;
    public bool IsAppealed { get; set; }
    public string? AppealNote { get; set; }
    public bool IsWaived { get; set; }
    public Guid? WaiverApprovalRequestId { get; set; }

    public Guid? MaintenanceBillId { get; set; }
    public bool IsPaid { get; set; }
    public bool IsRectified { get; set; }
    public DateOnly? RectifiedOn { get; set; }
}

/// <summary>
/// Somebody coming in through the gate. Pre-approved with an expiry, or approved live by OTP —
/// the two patterns every society app in this market runs on.
/// </summary>
public class Visitor : BaseEntity
{
    public Guid SocietyId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? PhotoUrl { get; set; }
    public string? IdentityNumber { get; set; }
    public VisitorKind Kind { get; set; } = VisitorKind.Guest;

    public string? Company { get; set; }
    public string? VehicleNumber { get; set; }

    /// <summary>Recognised on return, so a regular delivery rider is not re-registered daily.</summary>
    public bool IsFrequent { get; set; }

    public int VisitCount { get; set; }
    public DateTime? LastVisitAt { get; set; }
    public bool IsBlacklisted { get; set; }
    public string? BlacklistReason { get; set; }
}

/// <summary>A pre-approval a resident issues before the visitor arrives.</summary>
public class VisitorPass : BaseEntity
{
    public string PassNumber { get; set; } = string.Empty;

    public Guid SocietyId { get; set; }
    public Guid? VisitorId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? ResidentId { get; set; }

    public string VisitorName { get; set; } = string.Empty;
    public string? VisitorPhone { get; set; }
    public VisitorKind Kind { get; set; } = VisitorKind.Guest;
    public int GuestCount { get; set; } = 1;
    public string? VehicleNumber { get; set; }

    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }

    /// <summary>A pass for a regular visitor — a tutor, a physiotherapist — rather than one visit.</summary>
    public bool IsRecurring { get; set; }
    public string? RecurrenceDays { get; set; }

    /// <summary>Scanned at the gate. Faster and more reliable than reading a name from a phone.</summary>
    public string? QrCode { get; set; }

    /// <summary>One-time code the guard reads back. The fallback where no phone is involved.</summary>
    public string? OtpCode { get; set; }

    public GateEntryStatus Status { get; set; } = GateEntryStatus.Expected;
    public DateTime? UsedAt { get; set; }
    public bool IsCancelled { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// The gate log. Written from a screen designed for a guard's phone on a bad connection, so it
/// queues locally and syncs — a barrier must never wait for a network.
/// </summary>
public class GateEntry : BaseEntity
{
    public Guid SocietyId { get; set; }
    public Guid? GateId { get; set; }

    public Guid? VisitorId { get; set; }
    public Guid? VisitorPassId { get; set; }
    public Guid? DomesticStaffId { get; set; }
    public Guid? ResidentId { get; set; }
    public Guid? UnitId { get; set; }

    public string? PersonName { get; set; }
    public VisitorKind Kind { get; set; } = VisitorKind.Guest;
    public string? Purpose { get; set; }
    public string? VehicleNumber { get; set; }
    public int PersonCount { get; set; } = 1;

    public GateEntryStatus Status { get; set; } = GateEntryStatus.AwaitingApproval;

    public DateTime? CheckedInAt { get; set; }
    public DateTime? CheckedOutAt { get; set; }
    public Guid? GuardUserId { get; set; }
    public string? EntryPhotoUrl { get; set; }

    // ── Approval ─────────────────────────────────────────────────────────────

    public bool ApprovalRequested { get; set; }
    public DateTime? ApprovalRequestedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedByResidentId { get; set; }

    /// <summary>"Otp", "AppNotification", "Call", "PreApproved", "GuardOverride".</summary>
    public string? ApprovalMethod { get; set; }

    public bool IsDenied { get; set; }
    public string? DenialReason { get; set; }

    /// <summary>Written on a phone with no signal and reconciled later. Never blocks the barrier.</summary>
    public bool WasOffline { get; set; }
    public DateTime? OfflineSyncedAt { get; set; }

    public string? Note { get; set; }
}

/// <summary>A printed or digital pass for goods leaving — the control that stops a defaulter moving out.</summary>
public class GatePass : BaseEntity
{
    public string PassNumber { get; set; } = string.Empty;

    public Guid SocietyId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? ResidentId { get; set; }

    /// <summary>"MaterialIn", "MaterialOut", "MoveIn", "MoveOut", "Delivery", "Contractor".</summary>
    public string PassType { get; set; } = "MaterialOut";

    public string? ItemDescription { get; set; }
    public int? ItemCount { get; set; }
    public string? VehicleNumber { get; set; }
    public string? CarrierName { get; set; }

    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }

    public Guid? ApprovedByUserId { get; set; }
    public bool DuesCleared { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
    public Guid? GateEntryId { get; set; }
    public bool IsCancelled { get; set; }
}

/// <summary>
/// A resident shifting in or out. Gated on dues clearance, because a shifting request is the last
/// point at which a society has any leverage over a departing defaulter.
/// </summary>
public class MoveRequest : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid SocietyId { get; set; }
    public Guid UnitId { get; set; }
    public Guid PartyId { get; set; }

    /// <summary>"MoveIn" or "MoveOut".</summary>
    public string Direction { get; set; } = "MoveIn";

    public DateOnly RequestedDate { get; set; }
    public TimeSpan? SlotFrom { get; set; }
    public TimeSpan? SlotTo { get; set; }
    public string? LiftBooked { get; set; }

    public decimal MoveCharge { get; set; }
    public decimal SecurityDeposit { get; set; }
    public bool DuesCleared { get; set; }
    public Guid? DuesClearanceId { get; set; }

    /// <summary>"Requested", "Approved", "Rejected", "Completed", "Cancelled".</summary>
    public string Status { get; set; } = "Requested";

    public Guid? ApprovedByUserId { get; set; }
    public Guid? GatePassId { get; set; }
    public bool DamageInspectionDone { get; set; }
    public decimal? DamageCharge { get; set; }
    public string? Note { get; set; }
}

/// <summary>A shared facility residents can book.</summary>
public class Amenity : BaseEntity
{
    public Guid SocietyId { get; set; }
    public Society? Society { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>"Clubhouse", "BanquetHall", "Lawn", "Gym", "Pool", "TennisCourt", "PartyRoom",
    /// "GuestRoom", "CoWorking", "Theatre", "Barbecue".</summary>
    public string AmenityType { get; set; } = string.Empty;

    public int Capacity { get; set; }
    public string? Location { get; set; }
    public string? PhotoUrl { get; set; }

    public bool IsBookable { get; set; } = true;
    public bool RequiresApproval { get; set; }

    public TimeSpan OpensAt { get; set; }
    public TimeSpan ClosesAt { get; set; }
    public int SlotMinutes { get; set; } = 60;
    public int MinAdvanceHours { get; set; } = 24;
    public int MaxAdvanceDays { get; set; } = 30;

    /// <summary>Fairness rule: nobody may take the hall every weekend.</summary>
    public int MaxBookingsPerUnitPerMonth { get; set; }

    public decimal ChargePerSlot { get; set; }
    public decimal ChargePerHour { get; set; }
    public decimal SecurityDeposit { get; set; }
    public decimal CleaningCharge { get; set; }

    public int CancellationWindowHours { get; set; } = 24;
    public decimal LateCancellationPenalty { get; set; }

    /// <summary>A defaulter loses access, where the society's rules allow it.</summary>
    public bool BlockedForDefaulters { get; set; } = true;

    public bool IsUnderMaintenance { get; set; }
    public string? Rules { get; set; }
}

/// <summary>A closure or block on an amenity's calendar — maintenance, a society event, a holiday.</summary>
public class AmenitySlot : BaseEntity
{
    public Guid AmenityId { get; set; }
    public DateOnly Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }
    public int Capacity { get; set; }
    public int BookedCount { get; set; }
}

public class AmenityBooking : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid AmenityId { get; set; }
    public Guid SocietyId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? ResidentId { get; set; }
    public Guid PartyId { get; set; }

    public DateOnly BookingDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int GuestCount { get; set; } = 1;
    public string? Purpose { get; set; }

    public AmenityBookingStatus Status { get; set; } = AmenityBookingStatus.Requested;

    public decimal ChargeAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public Guid? ReceiptId { get; set; }

    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }

    public DateTime? CancelledAt { get; set; }
    public decimal? CancellationPenalty { get; set; }

    public bool DepositRefunded { get; set; }
    public decimal? DamageDeduction { get; set; }
    public string? PostUseNote { get; set; }
}

/// <summary>
/// A resident's problem, routed with an SLA. The same work-order engine handles the fix, so a
/// complaint and a repair are never tracked in two places.
/// </summary>
public class Complaint : BaseEntity
{
    public string TicketNumber { get; set; } = string.Empty;

    public Guid SocietyId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? ResidentId { get; set; }
    public Guid PartyId { get; set; }

    public ComplaintCategory Category { get; set; }
    public TicketPriority Priority { get; set; } = TicketPriority.Normal;
    public TicketStatus Status { get; set; } = TicketStatus.Open;

    public string Title { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? PhotoUrls { get; set; }

    public DateTime RaisedAt { get; set; } = DateTime.UtcNow;
    public NotificationChannel RaisedVia { get; set; } = NotificationChannel.InApp;

    public Guid? AssignedToUserId { get; set; }
    public Guid? ContractorId { get; set; }
    public DateTime? AcknowledgedAt { get; set; }

    public DateTime? SlaDueAt { get; set; }
    public bool SlaBreached { get; set; }
    public int EscalationLevel { get; set; }
    public DateTime? EscalatedAt { get; set; }

    public Guid? WorkOrderId { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? Resolution { get; set; }
    public DateTime? ClosedAt { get; set; }

    public int? SatisfactionRating { get; set; }
    public string? FeedbackNote { get; set; }
    public bool WasReopened { get; set; }
    public int ReopenCount { get; set; }

    public ICollection<ComplaintUpdate> Updates { get; set; } = [];
}

public class ComplaintUpdate : BaseEntity
{
    public Guid ComplaintId { get; set; }
    public Complaint? Complaint { get; set; }

    public DateTime PostedAt { get; set; } = DateTime.UtcNow;
    public Guid? UserId { get; set; }
    public TicketStatus? NewStatus { get; set; }
    public string Note { get; set; } = string.Empty;
    public string? PhotoUrls { get; set; }

    /// <summary>Internal notes are not shown in the resident's ticket view.</summary>
    public bool IsVisibleToResident { get; set; } = true;

    public bool IsFromResident { get; set; }
}

/// <summary>An announcement, circular or AGM notice.</summary>
public class SocietyNotice : BaseEntity
{
    public Guid SocietyId { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;

    /// <summary>"Announcement", "Circular", "AgmNotice", "Emergency", "Maintenance", "Event".</summary>
    public string NoticeType { get; set; } = "Announcement";

    public AlertSeverity Severity { get; set; } = AlertSeverity.Info;
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
    public DateOnly? ExpiresOn { get; set; }

    public Guid? PublishedByUserId { get; set; }
    public bool IsPinned { get; set; }

    /// <summary>Also pushed by SMS and WhatsApp. Reserved for things people must actually read.</summary>
    public bool SendAsBroadcast { get; set; }

    public string? AttachmentUrl { get; set; }

    /// <summary>Comma-separated block or tower codes. Empty means the whole estate.</summary>
    public string? TargetBlocks { get; set; }

    public int ReadCount { get; set; }
}

public class SocietyPoll : BaseEntity
{
    public Guid SocietyId { get; set; }

    public string Question { get; set; } = string.Empty;

    /// <summary>"SingleChoice", "MultiChoice", "YesNo".</summary>
    public string PollType { get; set; } = "YesNo";

    /// <summary>Newline-separated options.</summary>
    public string? Options { get; set; }

    public DateTime OpensAt { get; set; }
    public DateTime ClosesAt { get; set; }

    /// <summary>One vote per unit, not per resident. How a society constitution actually works.</summary>
    public bool OneVotePerUnit { get; set; } = true;

    public bool DefaultersMayVote { get; set; }
    public bool IsAnonymous { get; set; }

    public int EligibleCount { get; set; }
    public int VoteCount { get; set; }
    public string? ResultSummary { get; set; }
    public bool IsClosed { get; set; }
    public bool IsBinding { get; set; }
}

public class PollVote : BaseEntity
{
    public Guid SocietyPollId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? PartyId { get; set; }

    public string Choice { get; set; } = string.Empty;
    public DateTime VotedAt { get; set; } = DateTime.UtcNow;
    public string? Comment { get; set; }
}

public class SocietyDocument : BaseEntity
{
    public Guid SocietyId { get; set; }

    /// <summary>"ByeLaws", "Minutes", "AuditedAccounts", "Budget", "Insurance", "Licence",
    /// "Circular", "Agreement", "Layout".</summary>
    public string DocumentType { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateOnly? DocumentDate { get; set; }
    public int? FinancialYear { get; set; }

    /// <summary>Residents can see it in the portal. Committee-only documents are not marked so.</summary>
    public bool IsPublicToResidents { get; set; } = true;

    public int DownloadCount { get; set; }
}

/// <summary>
/// A plot owner's application to build, which the society scrutinises and then inspects.
///
/// Plot schemes run on this and no generic ERP has it: without it, a society has no way to stop a
/// four-storey building going up on a plot zoned for two.
/// </summary>
public class BuildingPlanApplication : BaseEntity
{
    public string Reference { get; set; } = string.Empty;

    public Guid SocietyId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid PartyId { get; set; }

    /// <summary>"NewConstruction", "Addition", "Alteration", "Renovation", "BoundaryWall", "Demolition".</summary>
    public string ApplicationType { get; set; } = "NewConstruction";

    public BuildingApplicationStatus Status { get; set; } = BuildingApplicationStatus.Submitted;
    public DateOnly SubmittedOn { get; set; }

    public decimal ProposedCoveredAreaSqFt { get; set; }
    public int ProposedFloors { get; set; }
    public decimal ProposedHeightFt { get; set; }
    public decimal ProposedCoveragePercent { get; set; }

    // What the bye-laws allow, so the comparison is on the record rather than in an officer's head.
    public decimal? PermittedCoveragePercent { get; set; }
    public decimal? PermittedHeightFt { get; set; }
    public int? PermittedFloors { get; set; }

    public string? ArchitectName { get; set; }
    public string? ArchitectLicence { get; set; }
    public string? DrawingUrl { get; set; }

    public decimal ScrutinyFee { get; set; }
    public decimal SecurityDeposit { get; set; }
    public bool FeesPaid { get; set; }
    public Guid? ReceiptId { get; set; }
    public bool DuesCleared { get; set; }

    public Guid? ScrutinisedByUserId { get; set; }
    public DateOnly? DecidedOn { get; set; }
    public string? Conditions { get; set; }
    public string? RejectionReason { get; set; }
    public DateOnly? ApprovalValidUntil { get; set; }

    public Guid? NocIssuanceId { get; set; }
    public bool IsCompleted { get; set; }
    public DateOnly? CompletionCertifiedOn { get; set; }
}

/// <summary>A site inspection during a plot owner's construction.</summary>
public class BuildingInspection : BaseEntity
{
    public Guid BuildingPlanApplicationId { get; set; }
    public Guid? PropertyId { get; set; }

    /// <summary>"Foundation", "Plinth", "Slab", "Structure", "Completion", "Complaint".</summary>
    public string Stage { get; set; } = string.Empty;

    public DateOnly InspectedOn { get; set; }
    public Guid? InspectorUserId { get; set; }

    public bool IsCompliant { get; set; }
    public string? Findings { get; set; }
    public string? PhotoUrls { get; set; }
    public Guid? ViolationNoticeId { get; set; }
    public DateOnly? ReInspectionDue { get; set; }
}

public class ViolationNotice : BaseEntity
{
    public string NoticeNumber { get; set; } = string.Empty;

    public Guid SocietyId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? BuildingPlanApplicationId { get; set; }
    public Guid PartyId { get; set; }

    public string ViolationType { get; set; } = string.Empty;
    public DateOnly IssuedOn { get; set; }
    public DateOnly ComplyByDate { get; set; }

    public decimal PenaltyAmount { get; set; }

    /// <summary>Work must stop until the violation is cleared. The society's strongest sanction.</summary>
    public bool IsStopWork { get; set; }

    public bool SecurityForfeited { get; set; }
    public string? EvidenceUrl { get; set; }
    public string? DocumentUrl { get; set; }

    public bool IsComplied { get; set; }
    public DateOnly? CompliedOn { get; set; }
    public bool ReferredToAuthority { get; set; }
    public bool IsWithdrawn { get; set; }
}
