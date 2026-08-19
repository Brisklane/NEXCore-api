using Distribution.Domain.Enums;
using Nexcore.SharedKernel;

namespace Distribution.Domain.Entities;

/// <summary>
/// A target set against someone or something.
///
/// The metric is part of the record rather than assumed, because a distribution business sets six
/// different kinds of target on the same person in the same month — value, coverage, new outlets,
/// collection — and rolling them into one "target" column is how incentive disputes start.
/// </summary>
public class SalesTarget : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public TargetMetric Metric { get; set; } = TargetMetric.SalesValue;
    public TargetPeriod Period { get; set; } = TargetPeriod.Monthly;
    public TargetScope Scope { get; set; } = TargetScope.FieldRep;

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    // ── Scope keys; the one the scope needs is set. ──────────────────────────
    public Guid? TerritoryId { get; set; }
    public Guid? RouteId { get; set; }
    public Guid? FieldRepId { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? OutletId { get; set; }

    public string CurrencyCode { get; set; } = "USD";
    public decimal TargetValue { get; set; }
    public decimal AchievedValue { get; set; }
    public decimal AchievementPercent { get; set; }

    /// <summary>Straight-line expectation for today, so "behind" and "ahead" mean something mid-month.</summary>
    public decimal ProRataTarget { get; set; }

    /// <summary>Where the month lands at the current run rate.</summary>
    public decimal ProjectedValue { get; set; }

    public decimal LastPeriodValue { get; set; }
    public decimal SamePeriodLastYearValue { get; set; }

    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime? LastComputedAt { get; set; }
    public string? Note { get; set; }

    public ICollection<TargetLine> Lines { get; set; } = [];
}

/// <summary>
/// A target broken down — by SKU, by brand, or phased across the weeks of the period.
///
/// Phasing matters in distribution: a month's target is not four equal weeks when the first week
/// carries the primary loading and the last carries the closing push.
/// </summary>
public class TargetLine : BaseEntity
{
    public Guid TargetId { get; set; }
    public SalesTarget? Target { get; set; }

    public Guid? ItemId { get; set; }
    public string? ItemName { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? CategoryId { get; set; }

    /// <summary>Set for phased targets: 1..5 inside the period.</summary>
    public int? WeekNumber { get; set; }
    public DateTime? PhaseStart { get; set; }
    public DateTime? PhaseEnd { get; set; }

    public string Uom { get; set; } = "PCS";
    public decimal TargetValue { get; set; }
    public decimal AchievedValue { get; set; }
    public decimal AchievementPercent { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>
/// How performance turns into money for the field.
///
/// The gate is the important part. A rep who hits value by hammering three big outlets and
/// visiting nobody else has not done the job, and a gated scheme is how the business says so
/// without arguing about it every month.
/// </summary>
public class IncentiveScheme : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public IncentiveBasis Basis { get; set; } = IncentiveBasis.Slab;

    public TargetMetric Metric { get; set; } = TargetMetric.SalesValue;
    public TargetPeriod Period { get; set; } = TargetPeriod.Monthly;

    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }

    /// <summary>Comma-separated <see cref="FieldRole"/> values this applies to.</summary>
    public string? ApplicableRoles { get; set; }
    public Guid? TerritoryId { get; set; }

    // ── Gate: nothing pays until these are cleared ───────────────────────────
    public TargetMetric? GateMetric { get; set; }
    public decimal GateThresholdPercent { get; set; }

    /// <summary>Minimum achievement before any payout at all.</summary>
    public decimal MinimumAchievementPercent { get; set; }

    public decimal LinearRatePercent { get; set; }
    public decimal MaxPayout { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    /// <summary>A short bonus on a focus SKU, on top of the main scheme.</summary>
    public Guid? SpiffItemId { get; set; }
    public decimal SpiffRatePerUnit { get; set; }

    public bool IsApproved { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public string? Terms { get; set; }

    public ICollection<IncentiveSlab> Slabs { get; set; } = [];
}

/// <summary>One payout tier of an incentive scheme.</summary>
public class IncentiveSlab : BaseEntity
{
    public Guid SchemeId { get; set; }
    public IncentiveScheme? Scheme { get; set; }

    public int SlabNumber { get; set; }
    public decimal FromAchievementPercent { get; set; }
    public decimal? ToAchievementPercent { get; set; }

    public decimal PayoutAmount { get; set; }
    public decimal PayoutPercent { get; set; }
    public string? Label { get; set; }
}

/// <summary>What one person actually earned in one period, and whether it has been paid.</summary>
public class IncentivePayout : BaseEntity
{
    public Guid SchemeId { get; set; }
    public IncentiveScheme? Scheme { get; set; }

    public Guid? FieldRepId { get; set; }
    public FieldRep? FieldRep { get; set; }
    public Guid? PartnerId { get; set; }

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    public decimal TargetValue { get; set; }
    public decimal AchievedValue { get; set; }
    public decimal AchievementPercent { get; set; }

    public bool GatePassed { get; set; } = true;
    public string? GateFailureReason { get; set; }

    public int? SlabNumber { get; set; }
    public decimal PayoutAmount { get; set; }
    public decimal SpiffAmount { get; set; }
    public decimal DeductionAmount { get; set; }
    public decimal NetPayout { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public bool IsApproved { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedByUserId { get; set; }

    /// <summary>Handed to payroll. HR owns what happens after that.</summary>
    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? PayrollReference { get; set; }

    public string? Note { get; set; }
}

/// <summary>
/// A day's KPI snapshot for one rep, route or territory.
///
/// Stored rather than computed on read because coverage and strike rate are ratios over a window,
/// and recomputing a quarter of them across a thousand routes every time a dashboard opens is how
/// reporting screens end up taking forty seconds.
/// </summary>
public class KpiSnapshot : BaseEntity
{
    public DateTime SnapshotDate { get; set; }

    public TargetScope Scope { get; set; } = TargetScope.FieldRep;
    public Guid? FieldRepId { get; set; }
    public Guid? RouteId { get; set; }
    public Guid? TerritoryId { get; set; }
    public Guid? PartnerId { get; set; }

    // ── Coverage & productivity ──────────────────────────────────────────────
    public int PlannedCalls { get; set; }
    public int ActualCalls { get; set; }
    public int ProductiveCalls { get; set; }
    public int UnplannedCalls { get; set; }
    public int MissedCalls { get; set; }

    /// <summary>Actual ÷ planned.</summary>
    public decimal CoveragePercent { get; set; }

    /// <summary>Productive ÷ actual. The single most-quoted field number.</summary>
    public decimal StrikeRatePercent { get; set; }

    public decimal LinesPerCall { get; set; }
    public decimal AverageBillValue { get; set; }
    public decimal DropSize { get; set; }

    public int BillCount { get; set; }
    public int NewOutletsAdded { get; set; }
    public int ActiveOutlets { get; set; }
    public int TotalOutlets { get; set; }

    public decimal MustSellCompliancePercent { get; set; }
    public decimal RangeSellingPercent { get; set; }

    // ── Time in market ───────────────────────────────────────────────────────
    public int TimeInMarketMinutes { get; set; }
    public decimal AverageTimePerCallMinutes { get; set; }
    public TimeSpan? FirstCallAt { get; set; }
    public TimeSpan? LastCallAt { get; set; }
    public decimal DistanceCoveredKm { get; set; }

    // ── Money ────────────────────────────────────────────────────────────────
    public string CurrencyCode { get; set; } = "USD";
    public decimal SalesValue { get; set; }
    public decimal CollectionValue { get; set; }
    public decimal ReturnValue { get; set; }
    public decimal OverdueAmount { get; set; }

    /// <summary>Collected ÷ due.</summary>
    public decimal CollectionEfficiencyPercent { get; set; }

    public DateTime ComputedAt { get; set; }
}

/// <summary>
/// A demand forecast for one SKU at one location.
///
/// Defaulting to secondary-sales history is deliberate. Forecasting on primary sales forecasts
/// your own pipeline stuffing, and the resulting plan replenishes the warehouse for a demand that
/// only ever existed on paper.
/// </summary>
public class DemandForecast : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public ForecastBasis Basis { get; set; } = ForecastBasis.SecondarySalesHistory;

    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    public Guid? WarehouseId { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? TerritoryId { get; set; }

    /// <summary>Months of history the forecast was built from.</summary>
    public int HistoryMonths { get; set; } = 6;

    public decimal SeasonalityFactor { get; set; } = 1;
    public decimal TrendFactor { get; set; } = 1;

    public decimal TotalForecastQuantity { get; set; }
    public decimal TotalForecastValue { get; set; }

    /// <summary>Filled in after the period closes, so the forecast can be scored.</summary>
    public decimal TotalActualQuantity { get; set; }
    public decimal AccuracyPercent { get; set; }

    public bool IsApproved { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string? Note { get; set; }

    public ICollection<ForecastLine> Lines { get; set; } = [];
}

/// <summary>One SKU's forecast, with the manual override kept beside the computed number.</summary>
public class ForecastLine : BaseEntity
{
    public Guid ForecastId { get; set; }
    public DemandForecast? Forecast { get; set; }

    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public Guid? BrandId { get; set; }

    public string Uom { get; set; } = "PCS";

    public decimal HistoricAverage { get; set; }
    public decimal ComputedQuantity { get; set; }

    /// <summary>Set when a planner disagreed with the maths. The rationale is not optional.</summary>
    public decimal? OverrideQuantity { get; set; }
    public string? OverrideReason { get; set; }

    /// <summary>Override where present, computed otherwise. What downstream planning uses.</summary>
    public decimal FinalQuantity { get; set; }

    public decimal ForecastValue { get; set; }
    public decimal ActualQuantity { get; set; }
    public decimal AccuracyPercent { get; set; }

    /// <summary>Variability of demand — feeds safety stock rather than a flat rule of thumb.</summary>
    public decimal DemandStdDeviation { get; set; }
}

/// <summary>
/// A computed suggestion to move stock somewhere: to a distributor, to a van, to another warehouse.
///
/// Held as a record rather than recalculated on screen so that a suggestion someone dismissed
/// stays dismissed, and so the ones that were acted on can be scored later.
/// </summary>
public class ReplenishmentSuggestion : BaseEntity
{
    public ReplenishmentTargetKind TargetKind { get; set; } = ReplenishmentTargetKind.Distributor;

    public Guid? PartnerId { get; set; }
    public Guid? VanUnitId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? SourceWarehouseId { get; set; }

    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;

    public string Uom { get; set; } = "PCS";

    public decimal CurrentStock { get; set; }
    public decimal InTransitStock { get; set; }
    public decimal ReorderPoint { get; set; }
    public decimal SafetyStock { get; set; }
    public decimal TargetStock { get; set; }
    public decimal SuggestedQuantity { get; set; }

    public decimal AverageDailyDemand { get; set; }
    public decimal DaysOfCover { get; set; }
    public int LeadTimeDays { get; set; }

    /// <summary>Higher means closer to stocking out. Drives the ordering of the planner's queue.</summary>
    public int UrgencyScore { get; set; }

    public decimal EstimatedValue { get; set; }
    public DateTime GeneratedAt { get; set; }

    public bool IsActioned { get; set; }
    public DateTime? ActionedAt { get; set; }

    /// <summary>The order or transfer this suggestion turned into.</summary>
    public Guid? ResultingOrderId { get; set; }
    public Guid? ResultingTransferId { get; set; }

    public bool IsDismissed { get; set; }
    public string? DismissReason { get; set; }
}

/// <summary>A request to move stock between locations, with in-transit visibility.</summary>
public class StockTransferRequest : BaseEntity
{
    public string TransferNumber { get; set; } = string.Empty;

    public Guid? SourceWarehouseId { get; set; }
    public Guid? DestinationWarehouseId { get; set; }
    public Guid? DestinationPartnerId { get; set; }
    public Guid? DestinationVanUnitId { get; set; }

    public TransferRequestStatus Status { get; set; } = TransferRequestStatus.Draft;

    public DateTime RequestedOn { get; set; }
    public DateTime? RequiredBy { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }

    public Guid? TripId { get; set; }
    public Guid? DispatchId { get; set; }

    public decimal TotalQuantity { get; set; }
    public decimal TotalValue { get; set; }
    public int LineCount { get; set; }

    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public string? RejectionReason { get; set; }
    public string? Note { get; set; }
}
