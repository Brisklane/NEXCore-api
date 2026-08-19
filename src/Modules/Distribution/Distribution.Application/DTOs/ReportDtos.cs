using Distribution.Domain.Enums;

namespace Distribution.Application.DTOs;

/// <summary>
/// The filter every distribution report accepts.
///
/// One shared shape rather than a bespoke filter per report, because the questions a distribution
/// business asks are always the same six dimensions crossed with a date range, and a user who
/// learns the filter on one screen should not have to relearn it on the next.
/// </summary>
public class DistributionReportFilter
{
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public Guid? TerritoryId { get; set; }
    public Guid? RouteId { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? OutletId { get; set; }
    public Guid? FieldRepId { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? ItemId { get; set; }
    public OutletChannel? Channel { get; set; }
    public OutletGrade? Grade { get; set; }

    /// <summary>"Day", "Week", "Month" — the bucket the trend series is grouped into.</summary>
    public string? Granularity { get; set; }
}

/// <summary>The owner's home screen: today against target, and what needs attention.</summary>
public class DistributionDashboardDto
{
    public DateTime AsOf { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    // ── Today ────────────────────────────────────────────────────────────────
    public decimal TodaySales { get; set; }
    public decimal TodayCollections { get; set; }
    public int TodayOrders { get; set; }
    public int TodayVisits { get; set; }
    public int TodayProductiveVisits { get; set; }
    public decimal TodayStrikeRatePercent { get; set; }

    // ── Period against target ────────────────────────────────────────────────
    public decimal MonthToDateSales { get; set; }
    public decimal MonthTarget { get; set; }
    public decimal MonthAchievementPercent { get; set; }
    public decimal MonthProjectedSales { get; set; }
    public decimal LastMonthSales { get; set; }
    public decimal SameMonthLastYearSales { get; set; }
    public decimal GrowthPercent { get; set; }

    // ── Field ────────────────────────────────────────────────────────────────
    public int ActiveFieldReps { get; set; }
    public int RepsStartedToday { get; set; }
    public int RepsNotStarted { get; set; }
    public decimal CoveragePercent { get; set; }
    public int NewOutletsThisMonth { get; set; }
    public int ActiveOutlets { get; set; }
    public int TotalOutlets { get; set; }

    // ── Supply ───────────────────────────────────────────────────────────────
    public int OpenOrders { get; set; }
    public decimal OpenOrderValue { get; set; }
    public int OrdersAwaitingDispatch { get; set; }
    public int TripsInProgress { get; set; }
    public decimal FillRatePercent { get; set; }
    public decimal OnTimeDeliveryPercent { get; set; }

    // ── Money ────────────────────────────────────────────────────────────────
    public decimal Receivables { get; set; }
    public decimal Overdue { get; set; }
    public decimal CollectionEfficiencyPercent { get; set; }
    public decimal UndepositedCash { get; set; }
    public int UnsettledRoutes { get; set; }

    // ── Trade ────────────────────────────────────────────────────────────────
    public decimal SchemeSpendMonthToDate { get; set; }
    public decimal SchemeBudgetRemaining { get; set; }
    public int OpenClaims { get; set; }
    public decimal OpenClaimValue { get; set; }
    public int ClaimsBreachingSla { get; set; }

    // ── Channel ──────────────────────────────────────────────────────────────
    public decimal ChannelStockValue { get; set; }
    public decimal SellThroughPercent { get; set; }
    public decimal NearExpiryValue { get; set; }

    public List<TrendPointDto> SalesTrend { get; set; } = [];
    public List<RankedRowDto> TopTerritories { get; set; } = [];
    public List<RankedRowDto> TopReps { get; set; } = [];
    public List<RankedRowDto> TopItems { get; set; } = [];
    public List<ExceptionRowDto> Exceptions { get; set; } = [];
}

public class TrendPointDto
{
    public DateTime Bucket { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public decimal SecondaryValue { get; set; }
    public decimal Comparison { get; set; }
    public int Count { get; set; }
}

public class RankedRowDto
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? SubLabel { get; set; }
    public decimal Value { get; set; }
    public decimal Quantity { get; set; }
    public decimal SharePercent { get; set; }
    public decimal GrowthPercent { get; set; }
    public int Rank { get; set; }
}

/// <summary>
/// One thing that needs a human today.
///
/// The exception list is deliberately a flat, uniform shape across every category — unexplained
/// variances, breached limits, near-expiry stock, unmapped data, overdue claims — because a
/// manager wants one queue to work through, not eleven dashboards to remember to open.
/// </summary>
public class ExceptionRowDto
{
    /// <summary>"UnsettledRoute", "CreditBreach", "NearExpiry", "UnmappedSecondary", "OverdueClaim", …</summary>
    public string Kind { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public decimal? Amount { get; set; }
    public DateTime? OccurredAt { get; set; }
    public int? AgeingDays { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }

    /// <summary>Where the UI should navigate when the row is opened.</summary>
    public string? ActionRoute { get; set; }
}

// ── Report payloads ──────────────────────────────────────────────────────────

/// <summary>Primary against secondary, by whatever dimension was asked for.</summary>
public class SalesReportDto
{
    public DistributionReportFilter Filter { get; set; } = new();
    public string CurrencyCode { get; set; } = "USD";
    public decimal PrimaryValue { get; set; }
    public decimal SecondaryValue { get; set; }
    public decimal ReturnValue { get; set; }
    public decimal NetValue { get; set; }
    public decimal Quantity { get; set; }
    public int OrderCount { get; set; }
    public decimal AverageOrderValue { get; set; }
    public decimal MarginAmount { get; set; }
    public decimal MarginPercent { get; set; }
    public List<TrendPointDto> Trend { get; set; } = [];
    public List<RankedRowDto> Rows { get; set; } = [];
}

/// <summary>The field KPI set, one row per rep, route or territory.</summary>
public class ProductivityReportDto
{
    public DistributionReportFilter Filter { get; set; } = new();
    public KpiDto Totals { get; set; } = new();
    public List<KpiDto> Rows { get; set; } = [];

    /// <summary>Plan-vs-actual by day, for the coverage heatmap.</summary>
    public List<CoverageCellDto> Heatmap { get; set; } = [];
}

public class CoverageCellDto
{
    public DateTime Date { get; set; }
    public Guid? FieldRepId { get; set; }
    public string? FieldRepName { get; set; }
    public int PlannedCalls { get; set; }
    public int ActualCalls { get; set; }
    public int ProductiveCalls { get; set; }
    public decimal CoveragePercent { get; set; }
    public decimal SalesValue { get; set; }
    public bool IsHoliday { get; set; }
}

/// <summary>Active, dormant, new and lost — the health of the retail universe.</summary>
public class OutletAnalyticsDto
{
    public DistributionReportFilter Filter { get; set; } = new();
    public int TotalOutlets { get; set; }
    public int ActiveOutlets { get; set; }
    public int BilledOutlets { get; set; }
    public int DormantOutlets { get; set; }
    public int NewOutlets { get; set; }
    public int LostOutlets { get; set; }
    public decimal ChurnPercent { get; set; }
    public decimal AverageOfftake { get; set; }
    public List<RankedRowDto> ByChannel { get; set; } = [];
    public List<RankedRowDto> ByGrade { get; set; } = [];
    public List<RankedRowDto> TopOutlets { get; set; } = [];
    public List<RankedRowDto> DormantList { get; set; } = [];
    public List<TrendPointDto> UniverseTrend { get; set; } = [];
}

/// <summary>Fill rate, cycle time, failed drops and what a drop actually costs.</summary>
public class LogisticsReportDto
{
    public DistributionReportFilter Filter { get; set; } = new();
    public int TripCount { get; set; }
    public int StopCount { get; set; }
    public int CompletedStops { get; set; }
    public int FailedStops { get; set; }
    public decimal FillRatePercent { get; set; }
    public decimal OnTimePercent { get; set; }
    public decimal AverageCycleTimeHours { get; set; }
    public decimal TotalDistanceKm { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal CostPerDrop { get; set; }
    public decimal CostPerKm { get; set; }
    public decimal DeliveredValue { get; set; }
    public List<RankedRowDto> FailureReasons { get; set; } = [];
    public List<RankedRowDto> ByVehicle { get; set; } = [];
    public List<RankedRowDto> ByRoute { get; set; } = [];
    public List<TrendPointDto> Trend { get; set; } = [];
}

/// <summary>Where the money is, how old it is, and how well it is being brought in.</summary>
public class ReceivablesReportDto
{
    public DistributionReportFilter Filter { get; set; } = new();
    public string CurrencyCode { get; set; } = "USD";
    public decimal TotalOutstanding { get; set; }
    public decimal TotalOverdue { get; set; }
    public decimal Bucket0To30 { get; set; }
    public decimal Bucket31To60 { get; set; }
    public decimal Bucket61To90 { get; set; }
    public decimal Bucket90Plus { get; set; }
    public decimal CollectedInPeriod { get; set; }
    public decimal DueInPeriod { get; set; }
    public decimal CollectionEfficiencyPercent { get; set; }
    public decimal AverageCollectionDays { get; set; }
    public int BlockedOutletCount { get; set; }
    public int BouncedChequeCount { get; set; }
    public decimal BouncedChequeValue { get; set; }
    public decimal ProvisionedAmount { get; set; }
    public List<CreditProfileDto> Rows { get; set; } = [];
    public List<TrendPointDto> Trend { get; set; } = [];
}

/// <summary>Return rate and cost, sliced by the reason it happened.</summary>
public class ReturnsReportDto
{
    public DistributionReportFilter Filter { get; set; } = new();
    public string CurrencyCode { get; set; } = "USD";
    public int ReturnCount { get; set; }
    public decimal ReturnValue { get; set; }
    public decimal SalesValue { get; set; }
    public decimal ReturnRatePercent { get; set; }
    public decimal SaleableValue { get; set; }
    public decimal DamagedValue { get; set; }
    public decimal ExpiredValue { get; set; }
    public decimal ScrappedValue { get; set; }
    public decimal RestockedValue { get; set; }
    public List<RankedRowDto> ByReason { get; set; } = [];
    public List<RankedRowDto> ByItem { get; set; } = [];
    public List<RankedRowDto> ByOutlet { get; set; } = [];
    public List<RankedRowDto> ByRoute { get; set; } = [];
    public List<TrendPointDto> Trend { get; set; } = [];
}

/// <summary>Claim volume, approval rate and — the one that matters — how long settlement takes.</summary>
public class ClaimsReportDto
{
    public DistributionReportFilter Filter { get; set; } = new();
    public string CurrencyCode { get; set; } = "USD";
    public int TotalClaims { get; set; }
    public decimal ClaimedValue { get; set; }
    public decimal ApprovedValue { get; set; }
    public decimal SettledValue { get; set; }
    public decimal RejectedValue { get; set; }
    public decimal ApprovalRatePercent { get; set; }
    public decimal AverageSettlementDays { get; set; }
    public int OpenClaims { get; set; }
    public decimal OpenClaimValue { get; set; }
    public int BreachingSlaCount { get; set; }
    public decimal Bucket0To15 { get; set; }
    public decimal Bucket16To30 { get; set; }
    public decimal Bucket31To60 { get; set; }
    public decimal Bucket60Plus { get; set; }
    public List<RankedRowDto> ByKind { get; set; } = [];
    public List<RankedRowDto> ByPartner { get; set; } = [];
    public List<RankedRowDto> RejectionReasons { get; set; } = [];
    public List<TrendPointDto> Trend { get; set; } = [];
}

/// <summary>Stock across every location including vans, with the ageing that makes it actionable.</summary>
public class StockReportDto
{
    public DistributionReportFilter Filter { get; set; } = new();
    public string CurrencyCode { get; set; } = "USD";
    public decimal WarehouseStockValue { get; set; }
    public decimal VanStockValue { get; set; }
    public decimal ChannelStockValue { get; set; }
    public decimal InTransitValue { get; set; }
    public decimal TotalStockValue { get; set; }
    public decimal NearExpiryValue { get; set; }
    public decimal ExpiredValue { get; set; }
    public decimal DeadStockValue { get; set; }
    public decimal DaysOfCover { get; set; }
    public int StockOutSkuCount { get; set; }
    public List<RankedRowDto> ByLocation { get; set; } = [];
    public List<RankedRowDto> ByBrand { get; set; } = [];
    public List<NearExpiryDto> NearExpiryLines { get; set; } = [];
}

/// <summary>The exception queue: everything that needs a human, in one ranked list.</summary>
public class ExceptionDashboardDto
{
    public DateTime AsOf { get; set; }
    public int TotalCount { get; set; }
    public int CriticalCount { get; set; }
    public int WarningCount { get; set; }

    public int UnsettledRoutes { get; set; }
    public int UnexplainedVariances { get; set; }
    public int CreditBreaches { get; set; }
    public int NearExpiryLines { get; set; }
    public int UnmappedSecondaryRows { get; set; }
    public int OverdueClaims { get; set; }
    public int ExpiringLicences { get; set; }
    public int OutOfFenceVisits { get; set; }
    public int FailedDeliveries { get; set; }
    public int ColdChainExcursions { get; set; }
    public int BouncedCheques { get; set; }
    public int StaleDevices { get; set; }

    public List<ExceptionRowDto> Rows { get; set; } = [];
}
