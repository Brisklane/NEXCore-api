using RealEstate.Domain.Enums;

namespace RealEstate.Application.DTOs;

// =====================================================================================
// The society console, the gate, and the facilities engine behind both.
// =====================================================================================

public class SocietyListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public Guid? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public string? RegistrationNumber { get; set; }
    public int TotalUnits { get; set; }
    public int OccupiedUnits { get; set; }
    public decimal OccupancyPercent { get; set; }
    public decimal MonthlyBillingTotal { get; set; }
    public decimal OutstandingTotal { get; set; }
    public decimal CollectionEfficiencyPercent { get; set; }
    public decimal CorpusFundBalance { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public bool IsHandedOver { get; set; }
    public bool ManagedByDeveloper { get; set; }
    public int DefaulterCount { get; set; }
    public int OpenComplaints { get; set; }
    public int ComplaintsBreachingSla { get; set; }
    public bool IsActive { get; set; }
}

public class SocietyDetailDto : SocietyListItemDto
{
    public Guid? PropertyId { get; set; }
    public Guid? GeoAreaId { get; set; }
    public DateOnly? RegisteredOn { get; set; }
    public string? RegistrationAuthority { get; set; }
    public string? ConstitutionUrl { get; set; }
    public int FinancialYearStartMonth { get; set; }
    public AreaDto TotalArea { get; set; } = new();

    public Guid? BankAccountId { get; set; }
    public Guid? SinkingFundId { get; set; }
    public decimal SinkingFundBalance { get; set; }

    public DateOnly? HandoverDate { get; set; }
    public decimal CorpusTransferred { get; set; }
    public bool CommonAreasTransferred { get; set; }
    public bool DocumentsTransferred { get; set; }
    public string? FacilityManagerName { get; set; }
    public bool SuspendAmenitiesOnDefault { get; set; }
    public decimal AmenitySuspensionThreshold { get; set; }

    public List<CommitteeMemberDto> Committee { get; set; } = [];
    public List<AmenityDto> Amenities { get; set; } = [];
    public List<MaintenanceChargeSchemeDto> ChargeSchemes { get; set; } = [];
    public List<SocietyNoticeDto> RecentNotices { get; set; } = [];
    public SocietyDashboardDto Dashboard { get; set; } = new();
}

/// <summary>The society console's header: what is happening in the estate right now.</summary>
public class SocietyDashboardDto
{
    public int VisitorsInsideNow { get; set; }
    public int VisitorsToday { get; set; }
    public int PendingGateApprovals { get; set; }
    public int OpenComplaints { get; set; }
    public int ComplaintsBreachingSla { get; set; }
    public int AmenityBookingsToday { get; set; }
    public int PendingAmenityApprovals { get; set; }
    public int MoveRequestsPending { get; set; }

    public decimal BilledThisMonth { get; set; }
    public decimal CollectedThisMonth { get; set; }
    public decimal OutstandingTotal { get; set; }
    public int DefaulterCount { get; set; }
    public decimal DefaulterExposure { get; set; }

    public int OpenWorkOrders { get; set; }
    public int PpmOverdue { get; set; }
    public int AssetsFaulty { get; set; }
    public int BuildingApplicationsPending { get; set; }
    public int ViolationsOpen { get; set; }

    public List<BreakdownSliceDto> ComplaintsByCategory { get; set; } = [];
    public List<TrendPointDto> CollectionTrend { get; set; } = [];
}

public class SocietyUpsertDto
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? PropertyId { get; set; }
    public Guid? GeoAreaId { get; set; }
    public string? RegistrationNumber { get; set; }
    public DateOnly? RegisteredOn { get; set; }
    public string? RegistrationAuthority { get; set; }
    public string? ConstitutionUrl { get; set; }
    public int FinancialYearStartMonth { get; set; } = 4;
    public Guid? BankAccountId { get; set; }
    public Guid? DefaultDunningPolicyId { get; set; }
    public Guid? FacilityManagerUserId { get; set; }
    public bool ManagedByDeveloper { get; set; } = true;
    public bool SuspendAmenitiesOnDefault { get; set; } = true;
    public decimal AmenitySuspensionThreshold { get; set; }
}

public class CommitteeMemberDto
{
    public Guid Id { get; set; }
    public Guid PartyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitLabel { get; set; }
    public string Position { get; set; } = "Member";
    public DateOnly FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public bool CanApproveSpend { get; set; }
    public decimal SpendLimit { get; set; }
    public bool IsActive { get; set; }
}

// ── Residents ────────────────────────────────────────────────────────────────

public class ResidentListItemDto
{
    public Guid Id { get; set; }
    public Guid SocietyId { get; set; }
    public Guid? UnitId { get; set; }
    public string UnitLabel { get; set; } = string.Empty;
    public string? BlockName { get; set; }
    public Guid PartyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? PhotoUrl { get; set; }
    public ResidentKind Kind { get; set; }
    public string? MembershipNumber { get; set; }
    public DateOnly MovedInOn { get; set; }
    public DateOnly? MovedOutOn { get; set; }
    public bool IsPrimaryContact { get; set; }
    public bool PortalAccessEnabled { get; set; }
    public decimal OutstandingDues { get; set; }
    public bool IsDefaulter { get; set; }
    public bool AmenitiesSuspended { get; set; }
    public int VehicleCount { get; set; }
    public int StaffCount { get; set; }
    public int HouseholdSize { get; set; }
    public bool IsActive { get; set; }
}

public class ResidentDetailDto : ResidentListItemDto
{
    public Guid PropertyId { get; set; }
    public Guid? TenancyId { get; set; }
    public Guid? HouseholdId { get; set; }
    public bool CanApproveVisitors { get; set; }
    public bool CanBookAmenities { get; set; }
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? BloodGroup { get; set; }

    public List<ResidentVehicleDto> Vehicles { get; set; } = [];
    public List<DomesticStaffDto> Staff { get; set; } = [];
    public List<MaintenanceBillDto> Bills { get; set; } = [];
    public List<AmenityBookingDto> AmenityBookings { get; set; } = [];
    public List<ComplaintListItemDto> Complaints { get; set; } = [];
    public List<GateEntryDto> RecentVisitors { get; set; } = [];
    public List<TimelineEntryDto> Timeline { get; set; } = [];
}

public class ResidentUpsertDto
{
    public Guid? Id { get; set; }
    public Guid SocietyId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid PropertyId { get; set; }
    public Guid? PartyId { get; set; }
    public PartyUpsertDto? NewParty { get; set; }
    public ResidentKind Kind { get; set; }
    public Guid? TenancyId { get; set; }
    public DateOnly MovedInOn { get; set; }
    public string? MembershipNumber { get; set; }
    public bool IsPrimaryContact { get; set; }
    public bool PortalAccessEnabled { get; set; } = true;
    public bool CanApproveVisitors { get; set; } = true;
    public bool CanBookAmenities { get; set; } = true;
    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? BloodGroup { get; set; }
}

public class ResidentVehicleDto
{
    public Guid? Id { get; set; }
    public string RegistrationNumber { get; set; } = string.Empty;
    public string? VehicleType { get; set; }
    public string? MakeModel { get; set; }
    public string? Colour { get; set; }
    public Guid? ParkingSlotId { get; set; }
    public string? ParkingSlotNumber { get; set; }
    public string? StickerNumber { get; set; }
    public DateOnly? StickerExpiresOn { get; set; }
    public string? RfidTag { get; set; }
    public bool IsActive { get; set; }
}

public class DomesticStaffDto
{
    public Guid Id { get; set; }
    public Guid SocietyId { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitLabel { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? PhotoUrl { get; set; }
    public string? IdentityNumber { get; set; }
    public string StaffType { get; set; } = "Maid";
    public string? PassNumber { get; set; }
    public DateOnly? PassIssuedOn { get; set; }
    public DateOnly? PassExpiresOn { get; set; }
    public bool PassExpired { get; set; }
    public bool IsPoliceVerified { get; set; }
    public DateOnly? VerifiedOn { get; set; }
    public bool WorksForMultipleUnits { get; set; }
    public bool IsBlacklisted { get; set; }
    public string? BlacklistReason { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastEntryAt { get; set; }
}

// ── Maintenance billing ──────────────────────────────────────────────────────

public class MaintenanceChargeSchemeDto
{
    public Guid Id { get; set; }
    public Guid SocietyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public MaintenanceBasis Basis { get; set; }
    public decimal RatePerSqFt { get; set; }
    public decimal FlatAmount { get; set; }
    public RentFrequency Frequency { get; set; }
    public PropertySubType? AppliesToSubType { get; set; }
    public decimal? MinAreaSqFt { get; set; }
    public decimal? MaxAreaSqFt { get; set; }
    public decimal VacantUnitPercent { get; set; }
    public bool IsTaxable { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal LateFeePercent { get; set; }
    public decimal LateFeeFlat { get; set; }
    public int GraceDays { get; set; }
    public decimal EarlyPaymentDiscountPercent { get; set; }
    public bool AllowAnnualPrepayment { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
    public int UnitsCovered { get; set; }
    public decimal MonthlyValue { get; set; }
    public List<MaintenanceChargeSlabDto> Slabs { get; set; } = [];
}

public class MaintenanceChargeSlabDto
{
    public Guid? Id { get; set; }
    public decimal FromAreaSqFt { get; set; }
    public decimal? ToAreaSqFt { get; set; }
    public decimal Amount { get; set; }
    public decimal RatePerSqFt { get; set; }
    public int SortOrder { get; set; }
}

public class MaintenanceBillDto
{
    public Guid Id { get; set; }
    public string BillNumber { get; set; } = string.Empty;
    public Guid SocietyId { get; set; }
    public string? SocietyName { get; set; }
    public Guid? UnitId { get; set; }
    public string UnitLabel { get; set; } = string.Empty;
    public string? BlockName { get; set; }
    public Guid PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public string? PartyPhone { get; set; }
    public ResidentKind BilledTo { get; set; }

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
    public string CurrencyCode { get; set; } = "USD";

    public InstalmentStatus Status { get; set; }
    public int DaysOverdue { get; set; }
    public bool IsSent { get; set; }
    public bool IsDisputed { get; set; }
    public string? DocumentUrl { get; set; }
    public List<MaintenanceBillLineDto> Lines { get; set; } = [];
}

public class MaintenanceBillLineDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ChargeType { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public Guid? MeterReadingId { get; set; }
    public int SortOrder { get; set; }
}

public class MaintenanceBillRunDto
{
    public Guid SocietyId { get; set; }
    public DateOnly PeriodFrom { get; set; }
    public DateOnly PeriodTo { get; set; }
    public DateOnly DueDate { get; set; }
    public bool IncludeUtilities { get; set; } = true;
    public bool IncludeArrears { get; set; } = true;
    public bool ApplyLateFees { get; set; } = true;
    public bool IsDryRun { get; set; } = true;
    public List<Guid> ExcludeUnitIds { get; set; } = [];
    public bool SendImmediately { get; set; }
    public List<NotificationChannel> Channels { get; set; } = [];
}

public class MaintenanceBillRunResultDto
{
    public int CandidateCount { get; set; }
    public int GeneratedCount { get; set; }
    public int SkippedCount { get; set; }
    public int FailedCount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal MaintenanceTotal { get; set; }
    public decimal UtilityTotal { get; set; }
    public decimal ArrearsTotal { get; set; }
    public decimal LateFeeTotal { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public bool IsDryRun { get; set; }
    public List<MaintenanceBillDto> Preview { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

public class SocietyChargeDto
{
    public Guid? Id { get; set; }
    public Guid SocietyId { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitLabel { get; set; }
    public string ChargeType { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public RentFrequency Frequency { get; set; }
    public bool IsRecurring { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsTaxable { get; set; }
    public bool IsActive { get; set; }
}

public class SocietyPenaltyDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid SocietyId { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitLabel { get; set; }
    public Guid PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public string ViolationType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateOnly ObservedOn { get; set; }
    public decimal Amount { get; set; }
    public string? ImposedByName { get; set; }
    public string? EvidenceUrl { get; set; }
    public bool NoticeServed { get; set; }
    public DateOnly? NoticeServedOn { get; set; }
    public int AppealWindowDays { get; set; }
    public bool IsAppealed { get; set; }
    public string? AppealNote { get; set; }
    public bool IsWaived { get; set; }
    public bool IsPaid { get; set; }
    public bool IsRectified { get; set; }
    public DateOnly? RectifiedOn { get; set; }
}

// ── The gate ─────────────────────────────────────────────────────────────────

public class VisitorPassDto
{
    public Guid Id { get; set; }
    public string PassNumber { get; set; } = string.Empty;
    public Guid SocietyId { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitLabel { get; set; }
    public string? ResidentName { get; set; }
    public string VisitorName { get; set; } = string.Empty;
    public string? VisitorPhone { get; set; }
    public VisitorKind Kind { get; set; }
    public int GuestCount { get; set; }
    public string? VehicleNumber { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public bool IsRecurring { get; set; }
    public string? RecurrenceDays { get; set; }
    public string? QrCode { get; set; }
    public GateEntryStatus Status { get; set; }
    public DateTime? UsedAt { get; set; }
    public bool IsCancelled { get; set; }
    public bool IsExpired { get; set; }
    public string? Note { get; set; }
}

public class VisitorPassCreateDto
{
    public Guid SocietyId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? ResidentId { get; set; }
    public string VisitorName { get; set; } = string.Empty;
    public string? VisitorPhone { get; set; }
    public VisitorKind Kind { get; set; } = VisitorKind.Guest;
    public int GuestCount { get; set; } = 1;
    public string? VehicleNumber { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public bool IsRecurring { get; set; }
    public string? RecurrenceDays { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// One row on the gate log. Written from a guard's phone, so it stays small and never blocks on a
/// network round trip.
/// </summary>
public class GateEntryDto
{
    public Guid Id { get; set; }
    public Guid SocietyId { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitLabel { get; set; }
    public string? BlockName { get; set; }
    public string? ResidentName { get; set; }
    public string? ResidentPhone { get; set; }

    public string? PersonName { get; set; }
    public VisitorKind Kind { get; set; }
    public string? Purpose { get; set; }
    public string? VehicleNumber { get; set; }
    public int PersonCount { get; set; }
    public string? EntryPhotoUrl { get; set; }

    public GateEntryStatus Status { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public DateTime? CheckedOutAt { get; set; }
    public int? MinutesInside { get; set; }
    public string? GuardName { get; set; }

    public bool ApprovalRequested { get; set; }
    public DateTime? ApprovalRequestedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovalMethod { get; set; }
    public bool IsDenied { get; set; }
    public string? DenialReason { get; set; }
    public bool WasOffline { get; set; }
    public string? Note { get; set; }

    public Guid? VisitorPassId { get; set; }
    public Guid? DomesticStaffId { get; set; }
}

public class GateEntryCreateDto
{
    public Guid SocietyId { get; set; }
    public Guid? GateId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? VisitorPassId { get; set; }
    public Guid? VisitorId { get; set; }
    public Guid? DomesticStaffId { get; set; }
    public Guid? ResidentId { get; set; }

    public string? PersonName { get; set; }
    public string? Phone { get; set; }
    public VisitorKind Kind { get; set; } = VisitorKind.Guest;
    public string? Purpose { get; set; }
    public string? VehicleNumber { get; set; }
    public int PersonCount { get; set; } = 1;
    public string? EntryPhotoUrl { get; set; }
    public string? IdentityNumber { get; set; }

    /// <summary>Ask the flat before letting them in. The OTP or push goes out on save.</summary>
    public bool RequestApproval { get; set; } = true;

    /// <summary>Guard let them in without waiting. Recorded as an override, never hidden.</summary>
    public bool GuardOverride { get; set; }
    public string? OverrideReason { get; set; }

    /// <summary>Captured with no signal, queued locally, posted when the connection returned.</summary>
    public bool WasOffline { get; set; }
    public DateTime? OfflineCapturedAt { get; set; }
    public string? ClientReference { get; set; }
}

public class GateApprovalDto
{
    public Guid GateEntryId { get; set; }
    public bool Approved { get; set; }
    public string? Method { get; set; }
    public string? OtpCode { get; set; }
    public string? DenialReason { get; set; }
}

/// <summary>A batch of gate entries captured offline, posted when the guard's phone reconnects.</summary>
public class GateSyncBatchDto
{
    public Guid SocietyId { get; set; }
    public List<GateEntryCreateDto> Entries { get; set; } = [];
}

public class GatePassDto
{
    public Guid Id { get; set; }
    public string PassNumber { get; set; } = string.Empty;
    public Guid SocietyId { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitLabel { get; set; }
    public string? ResidentName { get; set; }
    public string PassType { get; set; } = "MaterialOut";
    public string? ItemDescription { get; set; }
    public int? ItemCount { get; set; }
    public string? VehicleNumber { get; set; }
    public string? CarrierName { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public string? ApprovedByName { get; set; }
    public bool DuesCleared { get; set; }
    public bool IsUsed { get; set; }
    public DateTime? UsedAt { get; set; }
    public bool IsCancelled { get; set; }
    public bool IsExpired { get; set; }
}

public class MoveRequestDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid SocietyId { get; set; }
    public Guid UnitId { get; set; }
    public string UnitLabel { get; set; } = string.Empty;
    public Guid PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Direction { get; set; } = "MoveIn";
    public DateOnly RequestedDate { get; set; }
    public TimeSpan? SlotFrom { get; set; }
    public TimeSpan? SlotTo { get; set; }
    public string? LiftBooked { get; set; }
    public decimal MoveCharge { get; set; }
    public decimal SecurityDeposit { get; set; }
    public bool DuesCleared { get; set; }
    public decimal OutstandingDues { get; set; }
    public string Status { get; set; } = "Requested";
    public string? ApprovedByName { get; set; }
    public Guid? GatePassId { get; set; }
    public bool DamageInspectionDone { get; set; }
    public decimal? DamageCharge { get; set; }
    public string? Note { get; set; }
}

// ── Amenities ────────────────────────────────────────────────────────────────

public class AmenityDto
{
    public Guid Id { get; set; }
    public Guid SocietyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AmenityType { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public string? Location { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsBookable { get; set; }
    public bool RequiresApproval { get; set; }
    public TimeSpan OpensAt { get; set; }
    public TimeSpan ClosesAt { get; set; }
    public int SlotMinutes { get; set; }
    public int MinAdvanceHours { get; set; }
    public int MaxAdvanceDays { get; set; }
    public int MaxBookingsPerUnitPerMonth { get; set; }
    public decimal ChargePerSlot { get; set; }
    public decimal ChargePerHour { get; set; }
    public decimal SecurityDeposit { get; set; }
    public decimal CleaningCharge { get; set; }
    public int CancellationWindowHours { get; set; }
    public decimal LateCancellationPenalty { get; set; }
    public bool BlockedForDefaulters { get; set; }
    public bool IsUnderMaintenance { get; set; }
    public string? Rules { get; set; }
    public bool IsActive { get; set; }
    public int BookingsThisMonth { get; set; }
    public decimal UtilisationPercent { get; set; }
}

public class AmenityAvailabilityDto
{
    public Guid AmenityId { get; set; }
    public string AmenityName { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public List<AmenitySlotDto> Slots { get; set; } = [];
}

public class AmenitySlotDto
{
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int Capacity { get; set; }
    public int BookedCount { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }
    public decimal Charge { get; set; }
    public string? BookedByUnitLabel { get; set; }
}

public class AmenityBookingDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid AmenityId { get; set; }
    public string AmenityName { get; set; } = string.Empty;
    public Guid SocietyId { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitLabel { get; set; }
    public Guid PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public string? Phone { get; set; }

    public DateOnly BookingDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int GuestCount { get; set; }
    public string? Purpose { get; set; }
    public AmenityBookingStatus Status { get; set; }

    public decimal ChargeAmount { get; set; }
    public decimal DepositAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public string? ReceiptNumber { get; set; }

    public string? ApprovedByName { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime? CancelledAt { get; set; }
    public decimal? CancellationPenalty { get; set; }
    public bool DepositRefunded { get; set; }
    public decimal? DamageDeduction { get; set; }
    public string? PostUseNote { get; set; }
}

public class AmenityBookingCreateDto
{
    public Guid AmenityId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? ResidentId { get; set; }
    public Guid? PartyId { get; set; }
    public DateOnly BookingDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int GuestCount { get; set; } = 1;
    public string? Purpose { get; set; }
    public ReceiptCreateDto? Payment { get; set; }
}

// ── Helpdesk ─────────────────────────────────────────────────────────────────

public class ComplaintListItemDto
{
    public Guid Id { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public Guid SocietyId { get; set; }
    public string? SocietyName { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitLabel { get; set; }
    public string? BlockName { get; set; }
    public Guid PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public string? Phone { get; set; }

    public ComplaintCategory Category { get; set; }
    public TicketPriority Priority { get; set; }
    public TicketStatus Status { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Location { get; set; }

    public DateTime RaisedAt { get; set; }
    public NotificationChannel RaisedVia { get; set; }
    public string? AssignedToName { get; set; }
    public string? ContractorName { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime? SlaDueAt { get; set; }
    public bool SlaBreached { get; set; }
    public int? MinutesToSla { get; set; }
    public int EscalationLevel { get; set; }
    public Guid? WorkOrderId { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public int? SatisfactionRating { get; set; }
    public bool WasReopened { get; set; }
    public int AgeHours { get; set; }
}

public class ComplaintDetailDto : ComplaintListItemDto
{
    public string Description { get; set; } = string.Empty;
    public List<string> PhotoUrls { get; set; } = [];
    public string? Resolution { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? FeedbackNote { get; set; }
    public int ReopenCount { get; set; }
    public List<ComplaintUpdateDto> Updates { get; set; } = [];
    public WorkOrderListItemDto? WorkOrder { get; set; }
}

public class ComplaintUpdateDto
{
    public Guid Id { get; set; }
    public DateTime PostedAt { get; set; }
    public string? UserName { get; set; }
    public TicketStatus? NewStatus { get; set; }
    public string Note { get; set; } = string.Empty;
    public List<string> PhotoUrls { get; set; } = [];
    public bool IsVisibleToResident { get; set; }
    public bool IsFromResident { get; set; }
}

public class ComplaintCreateDto
{
    public Guid SocietyId { get; set; }
    public Guid? UnitId { get; set; }
    public Guid? ResidentId { get; set; }
    public Guid? PartyId { get; set; }
    public ComplaintCategory Category { get; set; }
    public TicketPriority Priority { get; set; } = TicketPriority.Normal;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Location { get; set; }
    public List<string> PhotoUrls { get; set; } = [];
    public NotificationChannel RaisedVia { get; set; } = NotificationChannel.InApp;
}

public class SocietyNoticeDto
{
    public Guid Id { get; set; }
    public Guid SocietyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string NoticeType { get; set; } = "Announcement";
    public AlertSeverity Severity { get; set; }
    public DateTime PublishedAt { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public string? PublishedByName { get; set; }
    public bool IsPinned { get; set; }
    public bool SendAsBroadcast { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? TargetBlocks { get; set; }
    public int ReadCount { get; set; }
    public bool IsActive { get; set; }
    public bool IsExpired { get; set; }
}

public class SocietyPollDto
{
    public Guid Id { get; set; }
    public Guid SocietyId { get; set; }
    public string Question { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string PollType { get; set; } = "YesNo";
    public List<string> Options { get; set; } = [];
    public DateTime OpensAt { get; set; }
    public DateTime ClosesAt { get; set; }
    public bool OneVotePerUnit { get; set; }
    public bool DefaultersMayVote { get; set; }
    public bool IsAnonymous { get; set; }
    public int EligibleCount { get; set; }
    public int VoteCount { get; set; }
    public decimal TurnoutPercent { get; set; }
    public bool IsClosed { get; set; }
    public bool IsBinding { get; set; }
    public bool HasVoted { get; set; }
    public List<BreakdownSliceDto> Results { get; set; } = [];
}

// ── Building control on plots ────────────────────────────────────────────────

public class BuildingPlanApplicationDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid SocietyId { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitLabel { get; set; }
    public Guid PropertyId { get; set; }
    public string? AddressOneLine { get; set; }
    public Guid PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public string? Phone { get; set; }

    public string ApplicationType { get; set; } = "NewConstruction";
    public BuildingApplicationStatus Status { get; set; }
    public DateOnly SubmittedOn { get; set; }

    public AreaDto ProposedCoveredArea { get; set; } = new();
    public int ProposedFloors { get; set; }
    public decimal ProposedHeightFt { get; set; }
    public decimal ProposedCoveragePercent { get; set; }
    public decimal? PermittedCoveragePercent { get; set; }
    public decimal? PermittedHeightFt { get; set; }
    public int? PermittedFloors { get; set; }

    /// <summary>Set where the proposal exceeds what the bye-laws allow, with the number.</summary>
    public List<string> Breaches { get; set; } = [];

    public string? ArchitectName { get; set; }
    public string? ArchitectLicence { get; set; }
    public string? DrawingUrl { get; set; }
    public decimal ScrutinyFee { get; set; }
    public decimal SecurityDeposit { get; set; }
    public bool FeesPaid { get; set; }
    public bool DuesCleared { get; set; }
    public string? ScrutinisedByName { get; set; }
    public DateOnly? DecidedOn { get; set; }
    public string? Conditions { get; set; }
    public string? RejectionReason { get; set; }
    public DateOnly? ApprovalValidUntil { get; set; }
    public Guid? NocIssuanceId { get; set; }
    public bool IsCompleted { get; set; }
    public List<BuildingInspectionDto> Inspections { get; set; } = [];
}

public class BuildingInspectionDto
{
    public Guid Id { get; set; }
    public string Stage { get; set; } = string.Empty;
    public DateOnly InspectedOn { get; set; }
    public string? InspectorName { get; set; }
    public bool IsCompliant { get; set; }
    public string? Findings { get; set; }
    public List<string> PhotoUrls { get; set; } = [];
    public Guid? ViolationNoticeId { get; set; }
    public DateOnly? ReInspectionDue { get; set; }
}

public class ViolationNoticeDto
{
    public Guid Id { get; set; }
    public string NoticeNumber { get; set; } = string.Empty;
    public Guid SocietyId { get; set; }
    public Guid? PropertyId { get; set; }
    public string? AddressOneLine { get; set; }
    public Guid PartyId { get; set; }
    public string PartyName { get; set; } = string.Empty;
    public string ViolationType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateOnly IssuedOn { get; set; }
    public DateOnly ComplyByDate { get; set; }
    public decimal PenaltyAmount { get; set; }
    public bool IsStopWork { get; set; }
    public bool SecurityForfeited { get; set; }
    public string? EvidenceUrl { get; set; }
    public string? DocumentUrl { get; set; }
    public bool IsComplied { get; set; }
    public DateOnly? CompliedOn { get; set; }
    public bool ReferredToAuthority { get; set; }
    public bool IsWithdrawn { get; set; }
    public bool IsOverdue { get; set; }
}
