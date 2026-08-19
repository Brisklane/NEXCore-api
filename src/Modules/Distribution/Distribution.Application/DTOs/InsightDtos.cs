using Distribution.Domain.Enums;

namespace Distribution.Application.DTOs;

// ── Secondary sales & distributor stock ──────────────────────────────────────

public class SecondarySaleDto
{
    public Guid Id { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public Guid PartnerId { get; set; }
    public string? PartnerName { get; set; }
    public Guid? OutletId { get; set; }
    public string? OutletName { get; set; }
    public string? OutletNameRaw { get; set; }
    public SecondaryCaptureMode CaptureMode { get; set; }
    public Guid? UploadBatchId { get; set; }
    public Guid? SourceOrderId { get; set; }
    public DateTime SaleDate { get; set; }
    public Guid? TerritoryId { get; set; }
    public Guid? RouteId { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal SchemeAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ReturnAmount { get; set; }
    public int LineCount { get; set; }
    public decimal TotalQuantity { get; set; }
    public bool IsMapped { get; set; }
    public string? MappingNote { get; set; }
    public bool IsPosted { get; set; }
    public List<SecondarySaleLineDto> Lines { get; set; } = [];
}

public class SecondarySaleLineDto
{
    public Guid Id { get; set; }
    public Guid? ItemId { get; set; }
    public string? ItemName { get; set; }
    public string? ItemCodeRaw { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? BatchId { get; set; }
    public string? BatchNumber { get; set; }
    public string Uom { get; set; } = "PCS";
    public decimal UomFactor { get; set; } = 1;
    public decimal Quantity { get; set; }
    public decimal BaseQuantity { get; set; }
    public decimal FreeQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal SchemeAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal LineTotal { get; set; }
    public bool IsMapped { get; set; }
    public string? MappingNote { get; set; }
}

/// <summary>The simple portal form for partners who will never send a file.</summary>
public class DeclareSecondarySalesDto
{
    public Guid PartnerId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string? CurrencyCode { get; set; }
    public List<DeclareSecondaryLineDto> Lines { get; set; } = [];
    public string? Note { get; set; }
}

public class DeclareSecondaryLineDto
{
    public Guid? ItemId { get; set; }
    public string? ItemCodeRaw { get; set; }
    public Guid? OutletId { get; set; }
    public string? OutletNameRaw { get; set; }
    public string Uom { get; set; } = "PCS";
    public decimal Quantity { get; set; }
    public decimal Value { get; set; }
    public DateTime? SaleDate { get; set; }
}

public class SecondaryUploadDto
{
    public Guid Id { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public Guid PartnerId { get; set; }
    public string? PartnerName { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? DueOn { get; set; }
    public int LatenessDays { get; set; }
    public UploadBatchStatus Status { get; set; }
    public string? FileName { get; set; }
    public string? FileUrl { get; set; }
    public Guid? MappingProfileId { get; set; }
    public string? MappingProfileName { get; set; }
    public int TotalRows { get; set; }
    public int MappedRows { get; set; }
    public int UnmappedRows { get; set; }
    public int RejectedRows { get; set; }
    public decimal TotalValue { get; set; }
    public decimal MappedValue { get; set; }
    public decimal MappingAccuracyPercent { get; set; }
    public DateTime? PostedAt { get; set; }
    public string? RejectionReason { get; set; }
    public string? ValidationSummary { get; set; }
}

/// <summary>A row that would not map, and the best guesses at what it meant.</summary>
public class MappingExceptionDto
{
    public Guid LineId { get; set; }
    public Guid SecondarySaleId { get; set; }
    public string? RawItemCode { get; set; }
    public string? RawOutletName { get; set; }
    public decimal Quantity { get; set; }
    public decimal Value { get; set; }
    public DateTime SaleDate { get; set; }
    public string? Reason { get; set; }
    public List<MappingSuggestionDto> Suggestions { get; set; } = [];
}

public class MappingSuggestionDto
{
    public Guid? ItemId { get; set; }
    public Guid? OutletId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }

    /// <summary>0–100. How close the fuzzy match was.</summary>
    public decimal Confidence { get; set; }
}

/// <summary>Resolving an exception teaches the profile, so the same code maps itself next month.</summary>
public class ResolveMappingDto
{
    public Guid LineId { get; set; }
    public Guid? ItemId { get; set; }
    public Guid? OutletId { get; set; }
    public bool RememberForFuture { get; set; } = true;
    public bool Reject { get; set; }
    public string? Note { get; set; }
}

public class MappingProfileDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid PartnerId { get; set; }
    public string? PartnerName { get; set; }
    public string FileFormat { get; set; } = "Csv";
    public string? OutletColumn { get; set; }
    public string? ItemColumn { get; set; }
    public string? QuantityColumn { get; set; }
    public string? ValueColumn { get; set; }
    public string? DateColumn { get; set; }
    public string? UomColumn { get; set; }
    public string? BatchColumn { get; set; }
    public string? InvoiceColumn { get; set; }
    public string? DateFormat { get; set; }
    public int HeaderRowIndex { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public int MappedCodeCount { get; set; }
}

public class StockDeclarationDto
{
    public Guid Id { get; set; }
    public string DeclarationNumber { get; set; } = string.Empty;
    public Guid PartnerId { get; set; }
    public string? PartnerName { get; set; }
    public DateTime AsOfDate { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? DueOn { get; set; }
    public int LatenessDays { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal TotalValue { get; set; }
    public decimal NearExpiryValue { get; set; }
    public decimal ExpiredValue { get; set; }
    public decimal DamagedValue { get; set; }
    public int LineCount { get; set; }
    public decimal DaysOfCover { get; set; }
    public bool IsVerified { get; set; }
    public string? Note { get; set; }
    public List<StockDeclarationLineDto> Lines { get; set; } = [];
}

public class StockDeclarationLineDto
{
    public Guid Id { get; set; }
    public Guid? ItemId { get; set; }
    public string? ItemName { get; set; }
    public string? ItemCodeRaw { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? BatchId { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string Uom { get; set; } = "PCS";
    public decimal UomFactor { get; set; } = 1;
    public decimal Quantity { get; set; }
    public decimal BaseQuantity { get; set; }
    public decimal UnitValue { get; set; }
    public decimal TotalValue { get; set; }
    public decimal Age0To30 { get; set; }
    public decimal Age31To60 { get; set; }
    public decimal Age61To90 { get; set; }
    public decimal Age90Plus { get; set; }
    public decimal NearExpiryQuantity { get; set; }
    public decimal ExpiredQuantity { get; set; }
    public decimal DamagedQuantity { get; set; }
    public decimal DaysOfCover { get; set; }
    public bool IsMapped { get; set; }

    // Compared against the norm so under- and over-stock are visible on the line itself.
    public decimal NormDaysOfCover { get; set; }
    public bool IsUnderStocked { get; set; }
    public bool IsOverStocked { get; set; }
}

public class SubmitStockDeclarationDto
{
    public Guid PartnerId { get; set; }
    public DateTime AsOfDate { get; set; }
    public string? CurrencyCode { get; set; }
    public string? Note { get; set; }
    public List<StockDeclarationLineDto> Lines { get; set; } = [];
}

public class StockNormDto
{
    public Guid Id { get; set; }
    public Guid PartnerId { get; set; }
    public string? PartnerName { get; set; }
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string Uom { get; set; } = "PCS";
    public decimal TargetDaysOfCover { get; set; }
    public decimal MinQuantity { get; set; }
    public decimal MaxQuantity { get; set; }
    public decimal ReorderQuantity { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public decimal CurrentQuantity { get; set; }
    public decimal CurrentDaysOfCover { get; set; }
    public bool IsUnderStocked { get; set; }
    public bool IsOverStocked { get; set; }
    public DateTime? EvaluatedAt { get; set; }
}

public class ReconciliationDto
{
    public Guid Id { get; set; }
    public Guid PartnerId { get; set; }
    public string? PartnerName { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public Guid? ItemId { get; set; }
    public string? ItemName { get; set; }
    public Guid? BrandId { get; set; }
    public string Uom { get; set; } = "PCS";
    public decimal OpeningQuantity { get; set; }
    public decimal PrimaryQuantity { get; set; }
    public decimal SecondaryQuantity { get; set; }
    public decimal ReturnQuantity { get; set; }
    public decimal DeclaredClosingQuantity { get; set; }
    public decimal ComputedClosingQuantity { get; set; }
    public decimal VarianceQuantity { get; set; }
    public decimal VariancePercent { get; set; }
    public decimal VarianceValue { get; set; }
    public ReconciliationOutcome Outcome { get; set; }
    public bool IsExplained { get; set; }
    public Guid? ReasonCodeId { get; set; }
    public string? ExplanationNote { get; set; }
    public DateTime ComputedAt { get; set; }
}

public class ExplainReconciliationDto
{
    public Guid ReconciliationId { get; set; }
    public Guid ReasonCodeId { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// The channel picture: what we sold in, what sold through, and what is still sitting in the
/// trade. The single most valuable screen a consumer-goods business has.
/// </summary>
public class ChannelInventoryDto
{
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public decimal PrimarySalesValue { get; set; }
    public decimal SecondarySalesValue { get; set; }
    public decimal ChannelStockValue { get; set; }
    public decimal ChannelStockDaysOfCover { get; set; }

    /// <summary>Secondary ÷ primary. Below 100% means the pipeline is filling rather than selling.</summary>
    public decimal SellThroughPercent { get; set; }

    public decimal NearExpiryValue { get; set; }
    public decimal ExpiredValue { get; set; }

    public int PartnerCount { get; set; }
    public int ReportingPartnerCount { get; set; }
    public decimal ReportingCompliancePercent { get; set; }
    public int UnexplainedVarianceCount { get; set; }

    public List<ChannelInventoryRowDto> ByPartner { get; set; } = [];
    public List<ChannelInventoryRowDto> ByBrand { get; set; } = [];
}

public class ChannelInventoryRowDto
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal PrimaryValue { get; set; }
    public decimal SecondaryValue { get; set; }
    public decimal StockValue { get; set; }
    public decimal DaysOfCover { get; set; }
    public decimal SellThroughPercent { get; set; }
    public decimal NearExpiryValue { get; set; }
    public bool HasVariance { get; set; }
}

/// <summary>How well a partner actually reports, which is a commercial fact, not an IT one.</summary>
public class PartnerDataQualityDto
{
    public Guid PartnerId { get; set; }
    public string PartnerName { get; set; } = string.Empty;
    public SecondaryCaptureMode CaptureMode { get; set; }
    public int PeriodsExpected { get; set; }
    public int PeriodsSubmitted { get; set; }
    public decimal SubmissionRatePercent { get; set; }
    public decimal AverageLatenessDays { get; set; }
    public decimal AverageMappingAccuracyPercent { get; set; }
    public decimal AverageVariancePercent { get; set; }

    /// <summary>0–100 composite. Chase the bottom of this list, not the loudest complainer.</summary>
    public decimal QualityScore { get; set; }
    public DateTime? LastSubmissionAt { get; set; }
}

// ── Targets, incentives & KPIs ───────────────────────────────────────────────

public class TargetDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public TargetMetric Metric { get; set; }
    public TargetPeriod Period { get; set; }
    public TargetScope Scope { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public Guid? TerritoryId { get; set; }
    public string? TerritoryName { get; set; }
    public Guid? RouteId { get; set; }
    public string? RouteName { get; set; }
    public Guid? FieldRepId { get; set; }
    public string? FieldRepName { get; set; }
    public Guid? PartnerId { get; set; }
    public string? PartnerName { get; set; }
    public Guid? OutletId { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal TargetValue { get; set; }
    public decimal AchievedValue { get; set; }
    public decimal AchievementPercent { get; set; }
    public decimal ProRataTarget { get; set; }
    public decimal ProjectedValue { get; set; }
    public decimal LastPeriodValue { get; set; }
    public decimal SamePeriodLastYearValue { get; set; }
    public bool IsPublished { get; set; }
    public DateTime? LastComputedAt { get; set; }
    public string? Note { get; set; }
    public List<TargetLineDto> Lines { get; set; } = [];

    /// <summary>Ahead of, on, or behind the pro-rata line. What the dashboard colours by.</summary>
    public string PaceStatus { get; set; } = "OnTrack";
}

public class TargetLineDto
{
    public Guid Id { get; set; }
    public Guid? ItemId { get; set; }
    public string? ItemName { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? CategoryId { get; set; }
    public int? WeekNumber { get; set; }
    public DateTime? PhaseStart { get; set; }
    public DateTime? PhaseEnd { get; set; }
    public string Uom { get; set; } = "PCS";
    public decimal TargetValue { get; set; }
    public decimal AchievedValue { get; set; }
    public decimal AchievementPercent { get; set; }
    public int DisplayOrder { get; set; }
}

public class SaveTargetDto
{
    public string Name { get; set; } = string.Empty;
    public TargetMetric Metric { get; set; } = TargetMetric.SalesValue;
    public TargetPeriod Period { get; set; } = TargetPeriod.Monthly;
    public TargetScope Scope { get; set; } = TargetScope.FieldRep;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public Guid? TerritoryId { get; set; }
    public Guid? RouteId { get; set; }
    public Guid? FieldRepId { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? OutletId { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal TargetValue { get; set; }
    public bool IsPublished { get; set; }
    public string? Note { get; set; }
    public List<TargetLineDto> Lines { get; set; } = [];
}

public class IncentiveSchemeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public IncentiveBasis Basis { get; set; }
    public TargetMetric Metric { get; set; }
    public TargetPeriod Period { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public string? ApplicableRoles { get; set; }
    public Guid? TerritoryId { get; set; }
    public TargetMetric? GateMetric { get; set; }
    public decimal GateThresholdPercent { get; set; }
    public decimal MinimumAchievementPercent { get; set; }
    public decimal LinearRatePercent { get; set; }
    public decimal MaxPayout { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public Guid? SpiffItemId { get; set; }
    public string? SpiffItemName { get; set; }
    public decimal SpiffRatePerUnit { get; set; }
    public bool IsApproved { get; set; }
    public string? Terms { get; set; }
    public bool IsActive { get; set; }
    public List<IncentiveSlabDto> Slabs { get; set; } = [];
}

public class IncentiveSlabDto
{
    public Guid Id { get; set; }
    public int SlabNumber { get; set; }
    public decimal FromAchievementPercent { get; set; }
    public decimal? ToAchievementPercent { get; set; }
    public decimal PayoutAmount { get; set; }
    public decimal PayoutPercent { get; set; }
    public string? Label { get; set; }
}

public class IncentivePayoutDto
{
    public Guid Id { get; set; }
    public Guid SchemeId { get; set; }
    public string? SchemeName { get; set; }
    public Guid? FieldRepId { get; set; }
    public string? FieldRepName { get; set; }
    public Guid? PartnerId { get; set; }
    public string? PartnerName { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public decimal TargetValue { get; set; }
    public decimal AchievedValue { get; set; }
    public decimal AchievementPercent { get; set; }
    public bool GatePassed { get; set; }
    public string? GateFailureReason { get; set; }
    public int? SlabNumber { get; set; }
    public decimal PayoutAmount { get; set; }
    public decimal SpiffAmount { get; set; }
    public decimal DeductionAmount { get; set; }
    public decimal NetPayout { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public bool IsApproved { get; set; }
    public bool IsPaid { get; set; }
    public string? PayrollReference { get; set; }
    public string? Note { get; set; }
}

public class KpiDto
{
    public DateTime SnapshotDate { get; set; }
    public TargetScope Scope { get; set; }
    public Guid? FieldRepId { get; set; }
    public string? FieldRepName { get; set; }
    public Guid? RouteId { get; set; }
    public string? RouteName { get; set; }
    public Guid? TerritoryId { get; set; }
    public string? TerritoryName { get; set; }
    public Guid? PartnerId { get; set; }
    public string? PartnerName { get; set; }

    public int PlannedCalls { get; set; }
    public int ActualCalls { get; set; }
    public int ProductiveCalls { get; set; }
    public int UnplannedCalls { get; set; }
    public int MissedCalls { get; set; }
    public decimal CoveragePercent { get; set; }
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
    public int TimeInMarketMinutes { get; set; }
    public decimal AverageTimePerCallMinutes { get; set; }
    public TimeSpan? FirstCallAt { get; set; }
    public TimeSpan? LastCallAt { get; set; }
    public decimal DistanceCoveredKm { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public decimal SalesValue { get; set; }
    public decimal CollectionValue { get; set; }
    public decimal ReturnValue { get; set; }
    public decimal OverdueAmount { get; set; }
    public decimal CollectionEfficiencyPercent { get; set; }
}

// ── Planning ─────────────────────────────────────────────────────────────────

public class ForecastDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ForecastBasis Basis { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? PartnerId { get; set; }
    public string? PartnerName { get; set; }
    public Guid? TerritoryId { get; set; }
    public int HistoryMonths { get; set; }
    public decimal SeasonalityFactor { get; set; }
    public decimal TrendFactor { get; set; }
    public decimal TotalForecastQuantity { get; set; }
    public decimal TotalForecastValue { get; set; }
    public decimal TotalActualQuantity { get; set; }
    public decimal AccuracyPercent { get; set; }
    public bool IsApproved { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string? Note { get; set; }
    public List<ForecastLineDto> Lines { get; set; } = [];
}

public class ForecastLineDto
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public Guid? BrandId { get; set; }
    public string Uom { get; set; } = "PCS";
    public decimal HistoricAverage { get; set; }
    public decimal ComputedQuantity { get; set; }
    public decimal? OverrideQuantity { get; set; }
    public string? OverrideReason { get; set; }
    public decimal FinalQuantity { get; set; }
    public decimal ForecastValue { get; set; }
    public decimal ActualQuantity { get; set; }
    public decimal AccuracyPercent { get; set; }
    public decimal DemandStdDeviation { get; set; }
}

public class GenerateForecastDto
{
    public string Name { get; set; } = string.Empty;
    public ForecastBasis Basis { get; set; } = ForecastBasis.SecondarySalesHistory;
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? TerritoryId { get; set; }
    public int HistoryMonths { get; set; } = 6;
    public decimal SeasonalityFactor { get; set; } = 1;
    public decimal TrendFactor { get; set; } = 1;
    public List<Guid> ItemIds { get; set; } = [];
    public string? Note { get; set; }
}

public class OverrideForecastLineDto
{
    public Guid LineId { get; set; }
    public decimal OverrideQuantity { get; set; }
    public string OverrideReason { get; set; } = string.Empty;
}

public class ReplenishmentSuggestionDto
{
    public Guid Id { get; set; }
    public ReplenishmentTargetKind TargetKind { get; set; }
    public Guid? PartnerId { get; set; }
    public string? PartnerName { get; set; }
    public Guid? VanUnitId { get; set; }
    public string? VanUnitName { get; set; }
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
    public int UrgencyScore { get; set; }
    public decimal EstimatedValue { get; set; }
    public DateTime GeneratedAt { get; set; }
    public bool IsActioned { get; set; }
    public bool IsDismissed { get; set; }
    public string? DismissReason { get; set; }
}

public class TransferRequestDto
{
    public Guid Id { get; set; }
    public string TransferNumber { get; set; } = string.Empty;
    public Guid? SourceWarehouseId { get; set; }
    public Guid? DestinationWarehouseId { get; set; }
    public Guid? DestinationPartnerId { get; set; }
    public string? DestinationPartnerName { get; set; }
    public Guid? DestinationVanUnitId { get; set; }
    public TransferRequestStatus Status { get; set; }
    public DateTime RequestedOn { get; set; }
    public DateTime? RequiredBy { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public Guid? TripId { get; set; }
    public Guid? DispatchId { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal TotalValue { get; set; }
    public int LineCount { get; set; }
    public string? RejectionReason { get; set; }
    public string? Note { get; set; }
}

// ── Traceability & compliance ────────────────────────────────────────────────

public class ColdChainCheckpointDto
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public ColdChainPointKind Kind { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? VehicleId { get; set; }
    public string? VehicleRegistration { get; set; }
    public Guid? VanUnitId { get; set; }
    public Guid? OutletId { get; set; }
    public string? OutletName { get; set; }
    public Guid? OutletAssetId { get; set; }
    public decimal MinSafeCelsius { get; set; }
    public decimal MaxSafeCelsius { get; set; }
    public int CheckIntervalHours { get; set; }
    public string? Location { get; set; }
    public string? SensorIdentifier { get; set; }
    public DateTime? LastReadingAt { get; set; }
    public decimal? LastReadingCelsius { get; set; }
    public bool IsInBreach { get; set; }
    public bool IsActive { get; set; }
    public string? Note { get; set; }

    /// <summary>True when the next reading is already past due.</summary>
    public bool IsCheckOverdue { get; set; }
    public int UnresolvedExcursionCount { get; set; }
}

public class ColdChainLogDto
{
    public Guid Id { get; set; }
    public Guid CheckpointId { get; set; }
    public string? CheckpointName { get; set; }
    public DateTime RecordedAt { get; set; }
    public decimal ReadingCelsius { get; set; }
    public bool IsOutOfRange { get; set; }
    public int? ExcursionMinutes { get; set; }
    public string? RecordedByName { get; set; }
    public bool IsAutomatic { get; set; }
    public string? CorrectiveAction { get; set; }
    public bool IsResolved { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public decimal AffectedStockValue { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Note { get; set; }
}

public class RecordColdChainReadingDto
{
    public Guid CheckpointId { get; set; }
    public decimal ReadingCelsius { get; set; }
    public DateTime? RecordedAt { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Note { get; set; }

    /// <summary>Required when the reading is outside the safe range.</summary>
    public string? CorrectiveAction { get; set; }
    public decimal AffectedStockValue { get; set; }
}

public class BatchTraceDto
{
    public Guid? ItemId { get; set; }
    public string? ItemName { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public DateTime? ExpiryDate { get; set; }
    public DateTime? ManufactureDate { get; set; }
    public string? SupplierLotReference { get; set; }
    public Guid? SupplierId { get; set; }
    public string? SupplierName { get; set; }

    public decimal ReceivedQuantity { get; set; }
    public decimal DespatchedQuantity { get; set; }
    public decimal ReturnedQuantity { get; set; }
    public decimal RemainingQuantity { get; set; }

    /// <summary>Every hop, oldest first.</summary>
    public List<BatchTraceHopDto> Hops { get; set; } = [];

    /// <summary>Distinct outlets that received any of it — the recall notice list.</summary>
    public List<BatchTraceDestinationDto> Destinations { get; set; } = [];
}

public class BatchTraceHopDto
{
    public DateTime MovedAt { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public string? DocumentNumber { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? VanUnitId { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? OutletId { get; set; }
    public string? OutletName { get; set; }
    public string Uom { get; set; } = "PCS";
    public decimal Quantity { get; set; }
    public decimal Value { get; set; }
}

public class BatchTraceDestinationDto
{
    public Guid? OutletId { get; set; }
    public string? OutletName { get; set; }
    public Guid? PartnerId { get; set; }
    public string? PartnerName { get; set; }
    public string? Phone { get; set; }
    public decimal Quantity { get; set; }
    public decimal Value { get; set; }
    public DateTime LastSuppliedAt { get; set; }
}

public class RecallDto
{
    public Guid Id { get; set; }
    public string RecallNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public RecallStatus Status { get; set; }
    public RecallSeverity Severity { get; set; }
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public Guid? BatchId { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public Guid? SupplierId { get; set; }
    public DateTime InitiatedOn { get; set; }
    public DateTime? AnnouncedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? TargetCompletionOn { get; set; }
    public string? Reason { get; set; }
    public string? RegulatoryReference { get; set; }
    public string? PublicNotice { get; set; }
    public decimal DespatchedQuantity { get; set; }
    public decimal RecoveredQuantity { get; set; }
    public decimal DestroyedQuantity { get; set; }
    public decimal RecoveryPercent { get; set; }
    public decimal EstimatedValue { get; set; }
    public decimal RecoveredValue { get; set; }
    public int AffectedOutletCount { get; set; }
    public int NotifiedOutletCount { get; set; }
    public int RespondedOutletCount { get; set; }
    public string? ClosureReport { get; set; }
    public string? Note { get; set; }
    public List<RecallNoticeDto> Notices { get; set; } = [];
}

public class RecallNoticeDto
{
    public Guid Id { get; set; }
    public Guid? OutletId { get; set; }
    public string? OutletName { get; set; }
    public Guid? PartnerId { get; set; }
    public string? DestinationName { get; set; }
    public string? Phone { get; set; }
    public decimal SuppliedQuantity { get; set; }
    public decimal ReturnedQuantity { get; set; }
    public decimal Value { get; set; }
    public DateTime? NotifiedAt { get; set; }
    public string? NotificationChannel { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime? CollectedAt { get; set; }
    public Guid? ReturnId { get; set; }
    public bool IsClosed { get; set; }
    public string? Note { get; set; }
}

public class InitiateRecallDto
{
    public string Title { get; set; } = string.Empty;
    public RecallSeverity Severity { get; set; } = RecallSeverity.ClassII;
    public Guid ItemId { get; set; }
    public Guid? BatchId { get; set; }
    public string? BatchNumber { get; set; }
    public Guid? SupplierId { get; set; }
    public DateTime? TargetCompletionOn { get; set; }
    public string? Reason { get; set; }
    public string? RegulatoryReference { get; set; }
    public string? PublicNotice { get; set; }
}

/// <summary>Stock approaching its expiry, ranked by how soon and how much it is worth.</summary>
public class NearExpiryDto
{
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? ItemCode { get; set; }
    public Guid? BatchId { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime ExpiryDate { get; set; }
    public int DaysToExpiry { get; set; }
    public string Location { get; set; } = string.Empty;
    public Guid? WarehouseId { get; set; }
    public Guid? VanUnitId { get; set; }
    public Guid? PartnerId { get; set; }
    public string Uom { get; set; } = "PCS";
    public decimal Quantity { get; set; }
    public decimal Value { get; set; }

    /// <summary>Critical, Warning or Watch, from the configured horizons.</summary>
    public string Severity { get; set; } = "Watch";

    /// <summary>A live liquidation scheme this batch already qualifies for, if one exists.</summary>
    public Guid? LiquidationSchemeId { get; set; }
    public string? LiquidationSchemeName { get; set; }
}

// ── Reason codes, settings, notifications ────────────────────────────────────

public class ReasonCodeDto
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public ReasonSurface Surface { get; set; }
    public int DisplayOrder { get; set; }
    public bool RequiresNote { get; set; }
    public bool RequiresApproval { get; set; }
    public bool IsRecoverable { get; set; }
    public bool IsNegative { get; set; }
    public string? ColorHex { get; set; }
    public string? IconName { get; set; }
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
}

public class DistributionSettingsDto
{
    public Guid Id { get; set; }

    public int DefaultGeofenceRadiusMetres { get; set; }
    public bool RequireGeoOnCheckIn { get; set; }
    public bool AllowOutOfFenceCheckIn { get; set; }
    public bool RequireReasonOnNoOrder { get; set; }
    public bool RequireStartSelfie { get; set; }
    public int MaxUnplannedVisitsPerDay { get; set; }
    public bool BlockDayCloseWithUnsynced { get; set; }
    public TimeSpan DayStartDeadline { get; set; }

    public decimal MinimumOrderValue { get; set; }
    public decimal DiscountApprovalThreshold { get; set; }
    public decimal MarginFloorPercent { get; set; }
    public bool AllowBackorders { get; set; }
    public int OrderEditWindowMinutes { get; set; }
    public AllocationStrategy DefaultAllocationStrategy { get; set; }
    public int SoftAllocationHoldHours { get; set; }

    public CreditEnforcement CreditEnforcementAtOrder { get; set; }
    public CreditEnforcement CreditEnforcementAtDispatch { get; set; }
    public CreditEnforcement CreditEnforcementAtVanSale { get; set; }
    public int AgeingBucket1Days { get; set; }
    public int AgeingBucket2Days { get; set; }
    public int AgeingBucket3Days { get; set; }
    public bool AutoBlockOnBouncedCheque { get; set; }
    public decimal ChequeBounceCharge { get; set; }

    public bool EnforceFefo { get; set; }
    public bool AllowFefoOverride { get; set; }
    public int NearExpiryWarningDays { get; set; }
    public int NearExpiryCriticalDays { get; set; }
    public decimal MinimumShelfLifePercentOnDespatch { get; set; }
    public bool AutoQuarantineExpired { get; set; }

    public decimal CashVarianceTolerance { get; set; }
    public decimal StockVarianceTolerancePercent { get; set; }
    public bool BlockSettlementOnUnexplainedVariance { get; set; }
    public decimal VarianceApprovalThreshold { get; set; }
    public TimeSpan SettlementCutOff { get; set; }

    public bool AutoApplySchemes { get; set; }
    public bool ShowNextSlabPrompt { get; set; }
    public bool StopSchemeOnBudgetExhausted { get; set; }

    public int ClaimSubmissionWindowDays { get; set; }
    public int ClaimSettlementSlaDays { get; set; }
    public bool AutoGenerateDeferredSchemeClaims { get; set; }

    public int SecondaryUploadDueDayOfMonth { get; set; }
    public decimal MinimumMappingAccuracyPercent { get; set; }
    public decimal ReconciliationTolerancePercent { get; set; }

    public string BaseCurrencyCode { get; set; } = "USD";
    public bool PrintThermalInvoices { get; set; }
    public string? InvoiceFooter { get; set; }
    public bool SendDigitalReceipts { get; set; }
}

public class NotificationDto
{
    public Guid Id { get; set; }
    public DistributionAlertKind Kind { get; set; }
    public AlertSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Body { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
    public string? ActionRoute { get; set; }
    public DateTime RaisedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime? DismissedAt { get; set; }
    public bool IsRead { get; set; }
}
