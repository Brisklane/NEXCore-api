using Distribution.Domain.Enums;
using Nexcore.SharedKernel;

namespace Distribution.Domain.Entities;

/// <summary>
/// A person who works the market. Links to an HR employee where one exists, so attendance and
/// payroll stay in one place, but exists independently because distributors' own staff are on
/// the system without ever being on our payroll.
/// </summary>
public class FieldRep : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public FieldRole Role { get; set; } = FieldRole.SalesRep;

    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? PhotoUrl { get; set; }

    /// <summary>ERP login. Null for a distributor's own staff who only use the field terminal.</summary>
    public Guid? UserId { get; set; }
    public Guid? EmployeeId { get; set; }

    /// <summary>Employer — null when the rep is ours rather than a distributor's.</summary>
    public Guid? PartnerId { get; set; }
    public ChannelPartner? Partner { get; set; }

    public Guid? TerritoryId { get; set; }
    public Guid? ReportsToFieldRepId { get; set; }

    /// <summary>PIN for the shared-device case. Hashed, never stored in the clear.</summary>
    public string? PinHash { get; set; }

    public Guid? DefaultVanUnitId { get; set; }
    public Guid? DefaultWarehouseId { get; set; }

    public DateTime? JoinedOn { get; set; }
    public DateTime? LeftOn { get; set; }

    /// <summary>Cash the rep is allowed to hold before a deposit is demanded.</summary>
    public decimal CashHoldingLimit { get; set; }

    /// <summary>Deepest discount this rep can give without approval, as a percentage.</summary>
    public decimal DiscountAuthorityPercent { get; set; }

    public bool CanOnboardOutlets { get; set; } = true;
    public bool CanCollectPayments { get; set; } = true;
    public bool CanAcceptReturns { get; set; }

    public string? Note { get; set; }
}

/// <summary>
/// One rep's working day: start, route, stops, close.
///
/// This is the anchor record for everything the field produces. A visit, an order, a collection
/// and a settlement all hang off a day, which is what makes "show me what happened on Tuesday"
/// a single query instead of six joined by timestamp.
/// </summary>
public class FieldDay : BaseEntity
{
    public Guid FieldRepId { get; set; }
    public FieldRep? FieldRep { get; set; }

    public DateTime WorkDate { get; set; }

    public Guid? RouteId { get; set; }
    public SalesRoute? Route { get; set; }

    public Guid? JourneyPlanDayId { get; set; }
    public Guid? VanUnitId { get; set; }

    public FieldDayStatus Status { get; set; } = FieldDayStatus.NotStarted;

    public DateTime? StartedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    /// <summary>Where the day was started, checked against the expected market area.</summary>
    public double? StartLatitude { get; set; }
    public double? StartLongitude { get; set; }
    public double? EndLatitude { get; set; }
    public double? EndLongitude { get; set; }
    public string? StartSelfieUrl { get; set; }

    public decimal DistanceCoveredKm { get; set; }

    // ── The day's numbers, maintained as visits close ────────────────────────
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

    /// <summary>Transactions still sitting in the device outbox. A day should not close above zero.</summary>
    public int UnsyncedCount { get; set; }
    public DateTime? LastSyncAt { get; set; }

    /// <summary>Set when a supervisor closed the day over unsynced or unexplained work.</summary>
    public Guid? ForceClosedByUserId { get; set; }
    public string? ForceCloseReason { get; set; }

    public string? Note { get; set; }

    public ICollection<OutletVisit> Visits { get; set; } = [];
}

/// <summary>
/// One stop at one outlet.
///
/// The visit — not the order — is the unit of field productivity. A call that produced nothing
/// still has to exist and still has to be explained, because coverage and strike rate are both
/// ratios whose denominator is the visit.
/// </summary>
public class OutletVisit : BaseEntity
{
    public Guid FieldDayId { get; set; }
    public FieldDay? FieldDay { get; set; }

    public Guid OutletId { get; set; }
    public RetailOutlet? Outlet { get; set; }

    public Guid? RouteId { get; set; }
    public Guid FieldRepId { get; set; }

    public int StopSequence { get; set; }
    public VisitStatus Status { get; set; } = VisitStatus.Pending;

    /// <summary>False when the rep visited an outlet that was not on today's beat.</summary>
    public bool IsPlanned { get; set; } = true;

    // ── Check-in / check-out ─────────────────────────────────────────────────
    public DateTime? CheckedInAt { get; set; }
    public DateTime? CheckedOutAt { get; set; }
    public double? CheckInLatitude { get; set; }
    public double? CheckInLongitude { get; set; }
    public double? CheckOutLatitude { get; set; }
    public double? CheckOutLongitude { get; set; }

    public GeoValidation GeoValidation { get; set; } = GeoValidation.NoFix;

    /// <summary>Metres from the outlet's stored position at check-in. Null when there was no fix.</summary>
    public decimal? DistanceFromOutletMetres { get; set; }

    /// <summary>Mandatory when the check-in landed outside the geofence.</summary>
    public string? GeoExceptionReason { get; set; }

    public int? DurationMinutes { get; set; }

    // ── Outcome ──────────────────────────────────────────────────────────────
    /// <summary>True when the visit produced an order — the numerator of strike rate.</summary>
    public bool IsProductive { get; set; }

    public Guid? NoOrderReasonId { get; set; }
    public string? NoOrderNote { get; set; }

    public Guid? OrderId { get; set; }
    public decimal OrderValue { get; set; }
    public int LinesSold { get; set; }
    public decimal CollectedAmount { get; set; }
    public decimal ReturnValue { get; set; }

    /// <summary>Focus SKUs actually sold, against the day's must-sell list.</summary>
    public int MustSellSoldCount { get; set; }
    public int MustSellTargetCount { get; set; }

    public bool SurveyCompleted { get; set; }
    public bool AuditCompleted { get; set; }
    public bool AssetsVerified { get; set; }

    public string? Note { get; set; }

    /// <summary>Client-supplied key that makes a retried submission idempotent.</summary>
    public string? IdempotencyKey { get; set; }
}

/// <summary>
/// A task pushed from head office to a route or an outlet — place this POSM, collect this cheque,
/// audit this planogram. It is what turns a campaign into something the field actually does.
/// </summary>
public class VisitTask : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string? Instructions { get; set; }

    public Guid? OutletId { get; set; }
    public Guid? RouteId { get; set; }
    public Guid? TerritoryId { get; set; }
    public Guid? FieldRepId { get; set; }

    public DateTime DueOn { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Guid? CompletedByFieldRepId { get; set; }
    public Guid? VisitId { get; set; }

    public bool RequiresPhoto { get; set; }
    public string? PhotoUrl { get; set; }
    public string? CompletionNote { get; set; }

    /// <summary>Higher runs first in the field terminal's task list.</summary>
    public int Priority { get; set; }
    public bool IsMandatory { get; set; }
}

/// <summary>
/// A questionnaire the field fills in — market intelligence, competitor pricing, satisfaction.
/// Assignment is by channel, route or campaign so a pharmacy is never asked a HoReCa question.
/// </summary>
public class SurveyForm : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Instructions { get; set; }

    public DateTime? ActiveFrom { get; set; }
    public DateTime? ActiveTo { get; set; }

    /// <summary>Comma-separated <see cref="OutletChannel"/> values. Empty means every channel.</summary>
    public string? ApplicableChannels { get; set; }
    public Guid? TerritoryId { get; set; }
    public Guid? RouteId { get; set; }

    /// <summary>The rep cannot check out until this survey is answered.</summary>
    public bool IsMandatory { get; set; }

    /// <summary>Asked at every visit rather than once per outlet per period.</summary>
    public bool RepeatEveryVisit { get; set; } = true;

    public ICollection<SurveyQuestion> Questions { get; set; } = [];
}

/// <summary>One question on a survey form.</summary>
public class SurveyQuestion : BaseEntity
{
    public Guid SurveyFormId { get; set; }
    public SurveyForm? SurveyForm { get; set; }

    public string Text { get; set; } = string.Empty;
    public SurveyQuestionKind Kind { get; set; } = SurveyQuestionKind.Text;
    public int DisplayOrder { get; set; }
    public bool IsRequired { get; set; }

    /// <summary>Pipe-separated choices for the choice kinds.</summary>
    public string? Options { get; set; }

    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public string? Unit { get; set; }
    public string? Guidance { get; set; }

    /// <summary>Only asked when the referenced question's answer matches <see cref="ShowWhenValue"/>.</summary>
    public Guid? ShowWhenQuestionId { get; set; }
    public string? ShowWhenValue { get; set; }
}

/// <summary>One filled-in survey, tied to the visit that produced it.</summary>
public class SurveyResponse : BaseEntity
{
    public Guid SurveyFormId { get; set; }
    public SurveyForm? SurveyForm { get; set; }

    public Guid OutletId { get; set; }
    public Guid? VisitId { get; set; }
    public Guid FieldRepId { get; set; }

    public DateTime SubmittedAt { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public ICollection<SurveyAnswer> Answers { get; set; } = [];
}

/// <summary>
/// One answer. The question text is copied onto the answer so a later edit to the form does not
/// silently rewrite history — the report has to show what was actually asked at the time.
/// </summary>
public class SurveyAnswer : BaseEntity
{
    public Guid ResponseId { get; set; }
    public SurveyResponse? Response { get; set; }

    public Guid QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public SurveyQuestionKind Kind { get; set; }

    public string? TextValue { get; set; }
    public decimal? NumericValue { get; set; }
    public bool? BoolValue { get; set; }
    public DateTime? DateValue { get; set; }
    public string? PhotoUrl { get; set; }
}

/// <summary>
/// What the competition is doing on this shelf, captured at the counter.
///
/// The single most under-used field in distribution: a rep who notes a rival's scheme today is
/// how trade marketing hears about it three weeks before the sales dip shows up in a report.
/// </summary>
public class CompetitorObservation : BaseEntity
{
    public Guid OutletId { get; set; }
    public Guid? VisitId { get; set; }
    public Guid FieldRepId { get; set; }

    public string CompetitorName { get; set; } = string.Empty;
    public string? ProductName { get; set; }
    public string? PackSize { get; set; }

    public decimal? ObservedPrice { get; set; }
    public decimal? ObservedMrp { get; set; }
    public string? SchemeDescription { get; set; }

    /// <summary>Units visibly on shelf — a proxy for their offtake we can actually collect.</summary>
    public int? VisibleStock { get; set; }
    public int? Facings { get; set; }

    public bool HasDisplay { get; set; }
    public bool HasPosm { get; set; }
    public string? PhotoUrl { get; set; }
    public DateTime ObservedAt { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// A scored execution check at an outlet — planogram, share of shelf, availability, pricing.
///
/// Scores are stored, not recomputed, because the weighting changes between campaigns and last
/// quarter's perfect-store number has to stay what it was when the bonus was paid on it.
/// </summary>
public class MerchandisingAudit : BaseEntity
{
    public Guid OutletId { get; set; }
    public RetailOutlet? Outlet { get; set; }

    public Guid? VisitId { get; set; }
    public Guid FieldRepId { get; set; }

    public AuditKind Kind { get; set; } = AuditKind.PerfectStore;
    public DateTime AuditedAt { get; set; }

    /// <summary>0–100. The composite the field chases.</summary>
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; } = 100;

    // ── Component scores, kept separately so a low total is diagnosable ──────
    public decimal AvailabilityScore { get; set; }
    public decimal VisibilityScore { get; set; }
    public decimal PlanogramScore { get; set; }
    public decimal PricingScore { get; set; }
    public decimal PosmScore { get; set; }

    /// <summary>Our facings as a percentage of all facings on the fixture.</summary>
    public decimal ShareOfShelfPercent { get; set; }

    /// <summary>Assortment lines present as a percentage of the expected matrix.</summary>
    public decimal OnShelfAvailabilityPercent { get; set; }

    public string? BeforePhotoUrl { get; set; }
    public string? AfterPhotoUrl { get; set; }
    public string? Note { get; set; }

    public ICollection<MerchandisingAuditLine> Lines { get; set; } = [];
}

/// <summary>One SKU's execution facts inside an audit.</summary>
public class MerchandisingAuditLine : BaseEntity
{
    public Guid AuditId { get; set; }
    public MerchandisingAudit? Audit { get; set; }

    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? ItemCode { get; set; }

    public bool IsExpectedInAssortment { get; set; } = true;
    public bool IsPresent { get; set; }

    public int Facings { get; set; }
    public int ExpectedFacings { get; set; }

    /// <summary>Units visible on shelf, which drives the suggested order.</summary>
    public int ShelfStock { get; set; }

    public decimal? ObservedPrice { get; set; }
    public decimal? MandatedPrice { get; set; }

    /// <summary>True when the shelf price is outside tolerance of the mandated price.</summary>
    public bool IsPriceCompliant { get; set; } = true;

    public bool IsCorrectPosition { get; set; } = true;
    public string? Note { get; set; }
}

/// <summary>
/// Point-of-sale material issued and placed: posters, danglers, shelf strips, wobblers.
///
/// Tracked as stock issued and then as an object with an expiry, because a torn poster from a
/// finished campaign is worse for the brand than no poster at all.
/// </summary>
public class PosmPlacement : BaseEntity
{
    public Guid OutletId { get; set; }
    public Guid? VisitId { get; set; }
    public Guid FieldRepId { get; set; }

    public Guid? ItemId { get; set; }
    public string MaterialName { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;

    public DateTime PlacedOn { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public DateTime? RemovedOn { get; set; }

    public Guid? SchemeId { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Position { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? LastVerifiedAt { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// A registered field device.
///
/// Distribution runs on personal Android phones in a market with poor signal. Knowing which
/// device last synced, on what app version, is the first question when a number looks wrong,
/// and a remote wipe flag is the answer when a phone goes missing with a week of orders on it.
/// </summary>
public class FieldDevice : BaseEntity
{
    public Guid? FieldRepId { get; set; }
    public FieldRep? FieldRep { get; set; }

    public string DeviceIdentifier { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
    public string? Platform { get; set; }
    public string? OsVersion { get; set; }
    public string? AppVersion { get; set; }

    public DateTime? RegisteredAt { get; set; }
    public DateTime? LastSeenAt { get; set; }
    public DateTime? LastSyncAt { get; set; }

    /// <summary>Records still queued on the device at its last check-in.</summary>
    public int PendingOutboxCount { get; set; }

    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }

    /// <summary>Set when the device is lost; the app clears local data on next contact.</summary>
    public bool WipeRequested { get; set; }
    public DateTime? WipedAt { get; set; }
}
