using Distribution.Domain.Enums;

namespace Distribution.Application.DTOs;

// ── Field reps & devices ─────────────────────────────────────────────────────

public class FieldRepDto
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public FieldRole Role { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? PhotoUrl { get; set; }
    public Guid? UserId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? PartnerId { get; set; }
    public string? PartnerName { get; set; }
    public Guid? TerritoryId { get; set; }
    public string? TerritoryName { get; set; }
    public Guid? ReportsToFieldRepId { get; set; }
    public string? ReportsToName { get; set; }
    public Guid? DefaultVanUnitId { get; set; }
    public Guid? DefaultWarehouseId { get; set; }
    public DateTime? JoinedOn { get; set; }
    public DateTime? LeftOn { get; set; }
    public decimal CashHoldingLimit { get; set; }
    public decimal DiscountAuthorityPercent { get; set; }
    public bool CanOnboardOutlets { get; set; }
    public bool CanCollectPayments { get; set; }
    public bool CanAcceptReturns { get; set; }
    public bool HasPin { get; set; }
    public bool IsActive { get; set; }
    public string? Note { get; set; }

    public int RouteCount { get; set; }
    public int OutletCount { get; set; }

    /// <summary>Today's day record, when one has been started.</summary>
    public FieldDayStatus? TodayStatus { get; set; }
    public decimal MonthToDateSales { get; set; }
    public decimal CoveragePercent { get; set; }
    public decimal StrikeRatePercent { get; set; }
}

public class SaveFieldRepDto
{
    public string? Code { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public FieldRole Role { get; set; } = FieldRole.SalesRep;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? PhotoUrl { get; set; }
    public Guid? UserId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? TerritoryId { get; set; }
    public Guid? ReportsToFieldRepId { get; set; }
    public Guid? DefaultVanUnitId { get; set; }
    public Guid? DefaultWarehouseId { get; set; }
    public DateTime? JoinedOn { get; set; }
    public DateTime? LeftOn { get; set; }
    public decimal CashHoldingLimit { get; set; }
    public decimal DiscountAuthorityPercent { get; set; }
    public bool CanOnboardOutlets { get; set; } = true;
    public bool CanCollectPayments { get; set; } = true;
    public bool CanAcceptReturns { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Note { get; set; }

    /// <summary>Plain PIN; hashed before storage and never returned.</summary>
    public string? Pin { get; set; }
}

public class FieldDeviceDto
{
    public Guid Id { get; set; }
    public Guid? FieldRepId { get; set; }
    public string? FieldRepName { get; set; }
    public string DeviceIdentifier { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
    public string? Platform { get; set; }
    public string? OsVersion { get; set; }
    public string? AppVersion { get; set; }
    public DateTime? RegisteredAt { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public int PendingOutboxCount { get; set; }
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }
    public bool WipeRequested { get; set; }
    public DateTime? WipedAt { get; set; }

    /// <summary>Minutes since the last sync. The first number to look at when a figure looks wrong.</summary>
    public int? MinutesSinceSync { get; set; }
}

// ── The working day ──────────────────────────────────────────────────────────

public class FieldDayDto
{
    public Guid Id { get; set; }
    public Guid FieldRepId { get; set; }
    public string? FieldRepName { get; set; }
    public DateTime WorkDate { get; set; }
    public Guid? RouteId { get; set; }
    public string? RouteName { get; set; }
    public RouteKind? RouteKind { get; set; }
    public Guid? VanUnitId { get; set; }
    public string? VanUnitName { get; set; }
    public FieldDayStatus Status { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public double? StartLatitude { get; set; }
    public double? StartLongitude { get; set; }
    public string? StartSelfieUrl { get; set; }
    public decimal DistanceCoveredKm { get; set; }

    public int PlannedCalls { get; set; }
    public int ActualCalls { get; set; }
    public int ProductiveCalls { get; set; }
    public int UnplannedCalls { get; set; }
    public int NewOutletsAdded { get; set; }
    public int DistinctLinesSold { get; set; }

    public decimal OrderValue { get; set; }
    public decimal InvoicedValue { get; set; }
    public decimal CollectedAmount { get; set; }
    public decimal ReturnValue { get; set; }
    public decimal CashDeclared { get; set; }

    public int UnsyncedCount { get; set; }
    public DateTime? LastSyncAt { get; set; }
    public string? ForceCloseReason { get; set; }
    public string? Note { get; set; }

    public decimal CoveragePercent { get; set; }
    public decimal StrikeRatePercent { get; set; }
    public decimal LinesPerCall { get; set; }

    /// <summary>Set once the day has been settled.</summary>
    public Guid? SettlementId { get; set; }
    public SettlementStatus? SettlementStatus { get; set; }
}

public class StartDayDto
{
    public Guid FieldRepId { get; set; }
    public DateTime WorkDate { get; set; }
    public Guid? RouteId { get; set; }
    public Guid? VanUnitId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? SelfieUrl { get; set; }
    public string? DeviceIdentifier { get; set; }
    public string? IdempotencyKey { get; set; }
}

public class CloseDayDto
{
    public Guid FieldDayId { get; set; }
    public decimal CashDeclared { get; set; }
    public decimal DistanceCoveredKm { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Note { get; set; }

    /// <summary>Supervisor closing over unsynced or unexplained work. Always logged.</summary>
    public bool ForceClose { get; set; }
    public string? ForceCloseReason { get; set; }
}

/// <summary>
/// The rep's whole day in one payload — the field terminal's boot call.
///
/// Deliberately fat: one round trip on a bad connection is worth twenty small ones, and every
/// field on it is something the terminal needs before the first shop.
/// </summary>
public class FieldDayBoardDto
{
    public FieldDayDto Day { get; set; } = new();
    public List<VisitCardDto> Stops { get; set; } = [];
    public List<VisitTaskDto> Tasks { get; set; } = [];
    public List<FocusItemDto> MustSellItems { get; set; } = [];
    public List<SurveyFormDto> Surveys { get; set; } = [];
    public VanStockSummaryDto? VanStock { get; set; }

    public int PendingStops { get; set; }
    public int CompletedStops { get; set; }
    public decimal TargetValue { get; set; }
    public decimal AchievedValue { get; set; }
    public decimal CollectionTarget { get; set; }
    public decimal CollectedAmount { get; set; }
}

/// <summary>One stop as the beat list renders it.</summary>
public class VisitCardDto
{
    public Guid? VisitId { get; set; }
    public Guid OutletId { get; set; }
    public string OutletName { get; set; } = string.Empty;
    public string? OutletCode { get; set; }
    public OutletChannel Channel { get; set; }
    public OutletGrade Grade { get; set; }
    public OutletStatus OutletStatus { get; set; }
    public string? AddressLine { get; set; }
    public string? Landmark { get; set; }
    public string? OwnerName { get; set; }
    public string? OwnerPhone { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int GeofenceRadiusMetres { get; set; }

    public int StopSequence { get; set; }
    public bool IsMustVisit { get; set; }
    public bool IsPlanned { get; set; }
    public VisitStatus Status { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public DateTime? CheckedOutAt { get; set; }
    public int? DurationMinutes { get; set; }

    public bool IsProductive { get; set; }
    public decimal OrderValue { get; set; }
    public decimal CollectedAmount { get; set; }

    public decimal OutstandingAmount { get; set; }
    public decimal OverdueAmount { get; set; }
    public bool IsCreditBlocked { get; set; }
    public DateTime? LastVisitAt { get; set; }
    public DateTime? LastOrderAt { get; set; }
    public decimal AverageMonthlyOfftake { get; set; }

    public int OpenTaskCount { get; set; }
    public int AssetCount { get; set; }
    public bool HasPinnedNote { get; set; }
    public string? PinnedNote { get; set; }
}

public class VisitDto
{
    public Guid Id { get; set; }
    public Guid FieldDayId { get; set; }
    public Guid OutletId { get; set; }
    public string? OutletName { get; set; }
    public Guid? RouteId { get; set; }
    public Guid FieldRepId { get; set; }
    public string? FieldRepName { get; set; }

    public int StopSequence { get; set; }
    public VisitStatus Status { get; set; }
    public bool IsPlanned { get; set; }

    public DateTime? CheckedInAt { get; set; }
    public DateTime? CheckedOutAt { get; set; }
    public double? CheckInLatitude { get; set; }
    public double? CheckInLongitude { get; set; }
    public GeoValidation GeoValidation { get; set; }
    public decimal? DistanceFromOutletMetres { get; set; }
    public string? GeoExceptionReason { get; set; }
    public int? DurationMinutes { get; set; }

    public bool IsProductive { get; set; }
    public Guid? NoOrderReasonId { get; set; }
    public string? NoOrderReasonName { get; set; }
    public string? NoOrderNote { get; set; }
    public Guid? OrderId { get; set; }
    public string? OrderNumber { get; set; }
    public decimal OrderValue { get; set; }
    public int LinesSold { get; set; }
    public decimal CollectedAmount { get; set; }
    public decimal ReturnValue { get; set; }
    public int MustSellSoldCount { get; set; }
    public int MustSellTargetCount { get; set; }
    public bool SurveyCompleted { get; set; }
    public bool AuditCompleted { get; set; }
    public bool AssetsVerified { get; set; }
    public string? Note { get; set; }
}

public class VisitSummaryDto
{
    public Guid Id { get; set; }
    public Guid OutletId { get; set; }
    public string? OutletName { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public int? DurationMinutes { get; set; }
    public VisitStatus Status { get; set; }
    public GeoValidation GeoValidation { get; set; }
    public bool IsProductive { get; set; }
    public bool IsPlanned { get; set; }
    public decimal OrderValue { get; set; }
    public decimal CollectedAmount { get; set; }
    public string? FieldRepName { get; set; }
    public string? RouteName { get; set; }
    public string? NoOrderReasonName { get; set; }
}

public class CheckInDto
{
    public Guid FieldDayId { get; set; }
    public Guid OutletId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    /// <summary>Mandatory when the fix lands outside the outlet's geofence.</summary>
    public string? GeoExceptionReason { get; set; }

    /// <summary>True when this outlet was not on today's beat.</summary>
    public bool IsUnplanned { get; set; }
    public string? IdempotencyKey { get; set; }
}

public class CheckOutDto
{
    public Guid VisitId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    /// <summary>Required when the visit produced no order.</summary>
    public Guid? NoOrderReasonId { get; set; }
    public string? NoOrderNote { get; set; }
    public bool AssetsVerified { get; set; }
    public string? Note { get; set; }
}

public class VisitTaskDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public Guid? OutletId { get; set; }
    public string? OutletName { get; set; }
    public Guid? RouteId { get; set; }
    public Guid? TerritoryId { get; set; }
    public Guid? FieldRepId { get; set; }
    public DateTime DueOn { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool RequiresPhoto { get; set; }
    public string? PhotoUrl { get; set; }
    public string? CompletionNote { get; set; }
    public int Priority { get; set; }
    public bool IsMandatory { get; set; }
    public bool IsOverdue { get; set; }
}

public class SaveVisitTaskDto
{
    public string Title { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public Guid? OutletId { get; set; }
    public Guid? RouteId { get; set; }
    public Guid? TerritoryId { get; set; }
    public Guid? FieldRepId { get; set; }
    public DateTime DueOn { get; set; }
    public bool RequiresPhoto { get; set; }
    public int Priority { get; set; }
    public bool IsMandatory { get; set; }
}

public class CompleteVisitTaskDto
{
    public Guid TaskId { get; set; }
    public Guid? VisitId { get; set; }
    public string? PhotoUrl { get; set; }
    public string? CompletionNote { get; set; }
}

/// <summary>A SKU the field is being pushed on today, with the compliance target behind it.</summary>
public class FocusItemDto
{
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? ItemCode { get; set; }
    public string? ImageUrl { get; set; }
    public string? BrandName { get; set; }
    public decimal TargetQuantityPerOutlet { get; set; }
    public Guid? SchemeId { get; set; }
    public string? SchemeName { get; set; }
    public decimal SpiffRatePerUnit { get; set; }
}

// ── Surveys ──────────────────────────────────────────────────────────────────

public class SurveyFormDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public DateTime? ActiveFrom { get; set; }
    public DateTime? ActiveTo { get; set; }
    public string? ApplicableChannels { get; set; }
    public Guid? TerritoryId { get; set; }
    public Guid? RouteId { get; set; }
    public bool IsMandatory { get; set; }
    public bool RepeatEveryVisit { get; set; }
    public bool IsActive { get; set; }
    public List<SurveyQuestionDto> Questions { get; set; } = [];
    public int ResponseCount { get; set; }
}

public class SurveyQuestionDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public SurveyQuestionKind Kind { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsRequired { get; set; }
    public string? Options { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public string? Unit { get; set; }
    public string? Guidance { get; set; }
    public Guid? ShowWhenQuestionId { get; set; }
    public string? ShowWhenValue { get; set; }
}

public class SubmitSurveyDto
{
    public Guid SurveyFormId { get; set; }
    public Guid OutletId { get; set; }
    public Guid? VisitId { get; set; }
    public Guid FieldRepId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public List<SurveyAnswerDto> Answers { get; set; } = [];
}

public class SurveyAnswerDto
{
    public Guid QuestionId { get; set; }
    public string? TextValue { get; set; }
    public decimal? NumericValue { get; set; }
    public bool? BoolValue { get; set; }
    public DateTime? DateValue { get; set; }
    public string? PhotoUrl { get; set; }
}

public class SurveyResponseDto
{
    public Guid Id { get; set; }
    public Guid SurveyFormId { get; set; }
    public string? SurveyName { get; set; }
    public Guid OutletId { get; set; }
    public string? OutletName { get; set; }
    public Guid? VisitId { get; set; }
    public Guid FieldRepId { get; set; }
    public string? FieldRepName { get; set; }
    public DateTime SubmittedAt { get; set; }
    public List<SurveyAnswerResultDto> Answers { get; set; } = [];
}

public class SurveyAnswerResultDto
{
    public Guid QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public SurveyQuestionKind Kind { get; set; }
    public string? TextValue { get; set; }
    public decimal? NumericValue { get; set; }
    public bool? BoolValue { get; set; }
    public DateTime? DateValue { get; set; }
    public string? PhotoUrl { get; set; }
}

// ── Merchandising ────────────────────────────────────────────────────────────

public class MerchandisingAuditDto
{
    public Guid Id { get; set; }
    public Guid OutletId { get; set; }
    public string? OutletName { get; set; }
    public Guid? VisitId { get; set; }
    public Guid FieldRepId { get; set; }
    public string? FieldRepName { get; set; }
    public AuditKind Kind { get; set; }
    public DateTime AuditedAt { get; set; }

    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public decimal AvailabilityScore { get; set; }
    public decimal VisibilityScore { get; set; }
    public decimal PlanogramScore { get; set; }
    public decimal PricingScore { get; set; }
    public decimal PosmScore { get; set; }
    public decimal ShareOfShelfPercent { get; set; }
    public decimal OnShelfAvailabilityPercent { get; set; }

    public string? BeforePhotoUrl { get; set; }
    public string? AfterPhotoUrl { get; set; }
    public string? Note { get; set; }
    public List<MerchandisingAuditLineDto> Lines { get; set; } = [];
}

public class MerchandisingAuditSummaryDto
{
    public Guid Id { get; set; }
    public AuditKind Kind { get; set; }
    public DateTime AuditedAt { get; set; }
    public decimal Score { get; set; }
    public decimal ShareOfShelfPercent { get; set; }
    public decimal OnShelfAvailabilityPercent { get; set; }
    public string? FieldRepName { get; set; }
}

public class MerchandisingAuditLineDto
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? ItemCode { get; set; }
    public bool IsExpectedInAssortment { get; set; }
    public bool IsPresent { get; set; }
    public int Facings { get; set; }
    public int ExpectedFacings { get; set; }
    public int ShelfStock { get; set; }
    public decimal? ObservedPrice { get; set; }
    public decimal? MandatedPrice { get; set; }
    public bool IsPriceCompliant { get; set; }
    public bool IsCorrectPosition { get; set; }
    public string? Note { get; set; }
}

public class SubmitAuditDto
{
    public Guid OutletId { get; set; }
    public Guid? VisitId { get; set; }
    public Guid FieldRepId { get; set; }
    public AuditKind Kind { get; set; } = AuditKind.PerfectStore;
    public string? BeforePhotoUrl { get; set; }
    public string? AfterPhotoUrl { get; set; }
    public string? Note { get; set; }
    public List<MerchandisingAuditLineDto> Lines { get; set; } = [];
}

public class CompetitorObservationDto
{
    public Guid Id { get; set; }
    public Guid OutletId { get; set; }
    public string? OutletName { get; set; }
    public Guid? VisitId { get; set; }
    public Guid FieldRepId { get; set; }
    public string? FieldRepName { get; set; }
    public string CompetitorName { get; set; } = string.Empty;
    public string? ProductName { get; set; }
    public string? PackSize { get; set; }
    public decimal? ObservedPrice { get; set; }
    public decimal? ObservedMrp { get; set; }
    public string? SchemeDescription { get; set; }
    public int? VisibleStock { get; set; }
    public int? Facings { get; set; }
    public bool HasDisplay { get; set; }
    public bool HasPosm { get; set; }
    public string? PhotoUrl { get; set; }
    public DateTime ObservedAt { get; set; }
    public string? Note { get; set; }
}

public class PosmPlacementDto
{
    public Guid Id { get; set; }
    public Guid OutletId { get; set; }
    public string? OutletName { get; set; }
    public Guid? VisitId { get; set; }
    public Guid FieldRepId { get; set; }
    public Guid? ItemId { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime PlacedOn { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public DateTime? RemovedOn { get; set; }
    public Guid? SchemeId { get; set; }
    public string? SchemeName { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Position { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? LastVerifiedAt { get; set; }
    public string? Note { get; set; }
    public bool IsExpired { get; set; }
}
