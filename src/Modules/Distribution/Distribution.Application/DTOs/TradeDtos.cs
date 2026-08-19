using Distribution.Domain.Enums;

namespace Distribution.Application.DTOs;

// ── Pricing ──────────────────────────────────────────────────────────────────

public class PriceListDto
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public PriceScope Scope { get; set; }
    public OutletChannel? Channel { get; set; }
    public Guid? TerritoryId { get; set; }
    public string? TerritoryName { get; set; }
    public PartnerType? PartnerTier { get; set; }
    public Guid? PartnerId { get; set; }
    public string? PartnerName { get; set; }
    public Guid? OutletId { get; set; }
    public string? OutletName { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public int Priority { get; set; }
    public bool IsTaxInclusive { get; set; }
    public bool IsApproved { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? SupersedesPriceListId { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public int LineCount { get; set; }
    public List<PriceListLineDto> Lines { get; set; } = [];
}

public class SavePriceListDto
{
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public PriceScope Scope { get; set; } = PriceScope.Company;
    public OutletChannel? Channel { get; set; }
    public Guid? TerritoryId { get; set; }
    public PartnerType? PartnerTier { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? OutletId { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public int Priority { get; set; }
    public bool IsTaxInclusive { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public List<PriceListLineDto> Lines { get; set; } = [];
}

public class PriceListLineDto
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? ItemCode { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? CategoryId { get; set; }
    public string Uom { get; set; } = "PCS";
    public decimal UomFactor { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal Mrp { get; set; }
    public decimal MinimumPrice { get; set; }
    public decimal MaxDiscountPercent { get; set; }
    public decimal TaxPercent { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public List<PriceSlabDto> Slabs { get; set; } = [];
}

public class PriceSlabDto
{
    public Guid Id { get; set; }
    public decimal FromQuantity { get; set; }
    public decimal? ToQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountPercent { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>
/// The answer to "why this price" — the question asked at every distributor's counter.
///
/// Returned as a chain rather than a number so the rep can show which rule won and which ones
/// were considered, instead of asserting a figure the retailer has no reason to believe.
/// </summary>
public class PriceResolutionDto
{
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string Uom { get; set; } = "PCS";
    public decimal ResolvedPrice { get; set; }
    public decimal Mrp { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal MinimumPrice { get; set; }
    public PriceScope WinningScope { get; set; }
    public Guid? WinningPriceListId { get; set; }
    public string? WinningPriceListName { get; set; }
    public int? AppliedSlabIndex { get; set; }
    public List<PriceCandidateDto> Considered { get; set; } = [];
}

public class PriceCandidateDto
{
    public PriceScope Scope { get; set; }
    public Guid PriceListId { get; set; }
    public string PriceListName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Priority { get; set; }
    public bool IsWinner { get; set; }
    public string? SkipReason { get; set; }
}

public class MarginLadderDto
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? ItemCode { get; set; }
    public Guid? TerritoryId { get; set; }
    public Guid? PartnerId { get; set; }
    public string? PartnerName { get; set; }
    public OutletChannel? Channel { get; set; }
    public string Uom { get; set; } = "PCS";
    public string CurrencyCode { get; set; } = "USD";
    public decimal LandedCost { get; set; }
    public decimal PriceToDistributor { get; set; }
    public decimal PriceToWholesaler { get; set; }
    public decimal PriceToRetailer { get; set; }
    public decimal Mrp { get; set; }
    public decimal CompanyMarginPercent { get; set; }
    public decimal DistributorMarginPercent { get; set; }
    public decimal WholesalerMarginPercent { get; set; }
    public decimal RetailerMarginPercent { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? Note { get; set; }
}

public class MrpRevisionDto
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public Guid? BatchId { get; set; }
    public string? BatchNumber { get; set; }
    public string Uom { get; set; } = "PCS";
    public decimal OldMrp { get; set; }
    public decimal NewMrp { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public bool TriggersPriceProtection { get; set; }
    public decimal ProtectionPerUnit { get; set; }
    public DateTime? ProtectionClaimWindowEnds { get; set; }
    public string? Reason { get; set; }
    public DateTime? ApprovedAt { get; set; }
}

// ── Trade schemes ────────────────────────────────────────────────────────────

public class TradeSchemeDto
{
    public Guid Id { get; set; }
    public string SchemeNumber { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public TradeSchemeKind Kind { get; set; }
    public SchemeStatus Status { get; set; }
    public SchemeSettlementMode SettlementMode { get; set; }
    public SchemeStacking Stacking { get; set; }
    public string? StackingGroup { get; set; }
    public int Priority { get; set; }

    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public string? ActiveDays { get; set; }

    public decimal MinQuantity { get; set; }
    public decimal MinValue { get; set; }
    public decimal FreeQuantity { get; set; }
    public Guid? FreeItemId { get; set; }
    public string? FreeItemName { get; set; }
    public string? FreeItemUom { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal MaxBenefitPerOrder { get; set; }
    public decimal MaxBenefitPerOutlet { get; set; }
    public bool IsRecurringPerBlock { get; set; }
    public bool IsPeriodScheme { get; set; }
    public int PaymentWithinDays { get; set; }
    public int DisplayDurationDays { get; set; }
    public decimal DisplayPayout { get; set; }
    public bool RequiresPhotoEvidence { get; set; }

    public string CurrencyCode { get; set; } = "USD";
    public decimal BudgetAmount { get; set; }
    public decimal ConsumedAmount { get; set; }
    public decimal RemainingBudget { get; set; }
    public decimal BudgetUsedPercent { get; set; }
    public bool StopWhenBudgetExhausted { get; set; }

    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public string? CommunicationPackUrl { get; set; }
    public string? TermsAndConditions { get; set; }
    public string? Note { get; set; }

    public int ApplicationCount { get; set; }
    public int BeneficiaryOutletCount { get; set; }
    public decimal QualifyingSalesValue { get; set; }
    public bool IsActive { get; set; }

    public List<TradeSchemeSlabDto> Slabs { get; set; } = [];
    public List<TradeSchemeProductDto> Products { get; set; } = [];
    public List<TradeSchemeScopeDto> Scopes { get; set; } = [];
}

public class SaveTradeSchemeDto
{
    public string Name { get; set; } = string.Empty;
    public TradeSchemeKind Kind { get; set; } = TradeSchemeKind.QuantityFreeGoods;
    public SchemeSettlementMode SettlementMode { get; set; } = SchemeSettlementMode.OnInvoice;
    public SchemeStacking Stacking { get; set; } = SchemeStacking.Combinable;
    public string? StackingGroup { get; set; }
    public int Priority { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }
    public string? ActiveDays { get; set; }

    public decimal MinQuantity { get; set; }
    public decimal MinValue { get; set; }
    public decimal FreeQuantity { get; set; }
    public Guid? FreeItemId { get; set; }
    public string? FreeItemUom { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal MaxBenefitPerOrder { get; set; }
    public decimal MaxBenefitPerOutlet { get; set; }
    public bool IsRecurringPerBlock { get; set; } = true;
    public bool IsPeriodScheme { get; set; }
    public int PaymentWithinDays { get; set; }
    public int DisplayDurationDays { get; set; }
    public decimal DisplayPayout { get; set; }
    public bool RequiresPhotoEvidence { get; set; }

    public string CurrencyCode { get; set; } = "USD";
    public decimal BudgetAmount { get; set; }
    public bool StopWhenBudgetExhausted { get; set; } = true;
    public string? CommunicationPackUrl { get; set; }
    public string? TermsAndConditions { get; set; }
    public string? Note { get; set; }

    public List<TradeSchemeSlabDto> Slabs { get; set; } = [];
    public List<TradeSchemeProductDto> Products { get; set; } = [];
    public List<TradeSchemeScopeDto> Scopes { get; set; } = [];
}

public class TradeSchemeSlabDto
{
    public Guid Id { get; set; }
    public int SlabNumber { get; set; }
    public decimal FromQuantity { get; set; }
    public decimal? ToQuantity { get; set; }
    public decimal FromValue { get; set; }
    public decimal? ToValue { get; set; }
    public decimal FreeQuantity { get; set; }
    public decimal DiscountPercent { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal PayoutAmount { get; set; }
    public decimal PointsAwarded { get; set; }
    public Guid? FreeItemId { get; set; }
    public string? FreeItemName { get; set; }
    public string? Label { get; set; }
}

public class TradeSchemeProductDto
{
    public Guid Id { get; set; }
    public Guid? ItemId { get; set; }
    public string? ItemName { get; set; }
    public Guid? BrandId { get; set; }
    public string? BrandName { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public bool IsQualifying { get; set; } = true;
    public decimal RequiredQuantity { get; set; }
    public string? Uom { get; set; }
    public decimal UomFactor { get; set; } = 1;
    public bool IsExcluded { get; set; }
}

public class TradeSchemeScopeDto
{
    public Guid Id { get; set; }
    public OutletChannel? Channel { get; set; }
    public OutletGrade? OutletGrade { get; set; }
    public PartnerType? PartnerTier { get; set; }
    public Guid? TerritoryId { get; set; }
    public string? TerritoryName { get; set; }
    public Guid? RouteId { get; set; }
    public string? RouteName { get; set; }
    public Guid? PartnerId { get; set; }
    public string? PartnerName { get; set; }
    public Guid? OutletId { get; set; }
    public string? OutletName { get; set; }
    public bool IsExcluded { get; set; }
}

public class SchemeApplicationDto
{
    public Guid Id { get; set; }
    public Guid SchemeId { get; set; }
    public string SchemeName { get; set; } = string.Empty;
    public TradeSchemeKind SchemeKind { get; set; }
    public Guid? OrderId { get; set; }
    public string? OrderNumber { get; set; }
    public Guid? OrderLineId { get; set; }
    public Guid? OutletId { get; set; }
    public string? OutletName { get; set; }
    public Guid? PartnerId { get; set; }
    public DateTime AppliedAt { get; set; }
    public decimal QualifyingQuantity { get; set; }
    public decimal QualifyingValue { get; set; }
    public int? SlabNumber { get; set; }
    public decimal FreeQuantity { get; set; }
    public Guid? FreeItemId { get; set; }
    public string? FreeItemName { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal PayoutAmount { get; set; }
    public decimal PointsAwarded { get; set; }
    public decimal BenefitValue { get; set; }
    public SchemeSettlementMode SettlementMode { get; set; }
    public Guid? ClaimId { get; set; }
    public bool IsSettled { get; set; }
    public string? BenefitDescription { get; set; }
    public bool IsReversed { get; set; }
}

public class SchemeBudgetEntryDto
{
    public DateTime OccurredAt { get; set; }
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string? Reason { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? ClaimId { get; set; }
}

public class SchemeDecisionDto
{
    public bool IsApproved { get; set; }
    public string? Comment { get; set; }
}

/// <summary>What a scheme would have cost against a past period's volumes, before it goes live.</summary>
public class SchemeSimulationDto
{
    public Guid? SchemeId { get; set; }
    public string SchemeName { get; set; } = string.Empty;
    public DateTime SimulatedFrom { get; set; }
    public DateTime SimulatedTo { get; set; }

    public decimal QualifyingSalesValue { get; set; }
    public decimal QualifyingQuantity { get; set; }
    public int QualifyingOrderCount { get; set; }
    public int BeneficiaryOutletCount { get; set; }

    public decimal EstimatedBenefitValue { get; set; }
    public decimal EstimatedFreeGoodsCost { get; set; }
    public decimal EstimatedDiscountCost { get; set; }
    public decimal TotalEstimatedCost { get; set; }

    /// <summary>Cost as a percentage of the sales it would have applied to.</summary>
    public decimal CostAsPercentOfSales { get; set; }

    public decimal SuggestedBudget { get; set; }
    public List<SchemeSimulationSlabDto> SlabBreakdown { get; set; } = [];
}

public class SchemeSimulationSlabDto
{
    public int SlabNumber { get; set; }
    public string? Label { get; set; }
    public int OrderCount { get; set; }
    public decimal QualifyingQuantity { get; set; }
    public decimal BenefitValue { get; set; }
}

/// <summary>How a live scheme is actually performing against the period before it.</summary>
public class SchemePerformanceDto
{
    public Guid SchemeId { get; set; }
    public string SchemeName { get; set; } = string.Empty;
    public TradeSchemeKind Kind { get; set; }
    public SchemeStatus Status { get; set; }
    public DateTime ValidFrom { get; set; }
    public DateTime ValidTo { get; set; }

    public decimal BudgetAmount { get; set; }
    public decimal ConsumedAmount { get; set; }
    public decimal RemainingBudget { get; set; }

    public int ApplicationCount { get; set; }
    public int BeneficiaryOutletCount { get; set; }
    public int EligibleOutletCount { get; set; }

    /// <summary>Beneficiaries ÷ eligible. A low number means the field never told anyone.</summary>
    public decimal RedemptionRatePercent { get; set; }

    public decimal QualifyingSalesValue { get; set; }
    public decimal BaselineSalesValue { get; set; }
    public decimal UpliftValue { get; set; }
    public decimal UpliftPercent { get; set; }

    public decimal IncrementalQuantity { get; set; }

    /// <summary>Scheme cost ÷ incremental units. The number that says whether it was worth it.</summary>
    public decimal CostPerIncrementalUnit { get; set; }

    public List<SchemeItemPerformanceDto> ByItem { get; set; } = [];
}

public class SchemeItemPerformanceDto
{
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal QualifyingQuantity { get; set; }
    public decimal BaselineQuantity { get; set; }
    public decimal UpliftPercent { get; set; }
    public decimal BenefitValue { get; set; }
}
