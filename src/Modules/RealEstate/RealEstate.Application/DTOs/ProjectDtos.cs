using RealEstate.Domain.Enums;

namespace RealEstate.Application.DTOs;

// =====================================================================================
// Projects and the inventory board they exist to feed.
// =====================================================================================

public class ProjectListItemDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public ProjectKind Kind { get; set; }
    public ProjectStatus Status { get; set; }
    public string? City { get; set; }
    public string? AreaName { get; set; }
    public string? HeroImageUrl { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public int TotalUnits { get; set; }
    public int UnitsAvailable { get; set; }
    public int UnitsBooked { get; set; }
    public int UnitsSold { get; set; }
    public decimal AbsorptionPercent { get; set; }

    public decimal TotalSalesValue { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal Outstanding { get; set; }
    public decimal PhysicalProgressPercent { get; set; }

    public DateOnly? PromisedPossessionDate { get; set; }
    public DateOnly? ForecastPossessionDate { get; set; }

    /// <summary>Forecast beyond the promise. The number a board actually asks about.</summary>
    public int? SlipDays { get; set; }

    public bool HasJointVenture { get; set; }
    public decimal? EscrowBalance { get; set; }
}

public class ProjectDetailDto : ProjectListItemDto
{
    public string? AddressLine { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public Guid? GeoAreaId { get; set; }
    public Guid? OfficeId { get; set; }

    public DateOnly? LaunchDate { get; set; }
    public DateOnly? BookingOpenDate { get; set; }
    public DateOnly? ConstructionStartDate { get; set; }
    public DateOnly? PlannedCompletionDate { get; set; }
    public DateOnly? ActualCompletionDate { get; set; }
    public DateOnly? HandoverToSocietyDate { get; set; }

    public AreaStatementDto AreaStatement { get; set; } = new();

    public decimal TotalBudget { get; set; }
    public decimal? EscrowPercent { get; set; }
    public RecognitionBasis RecognitionBasis { get; set; }
    public string? RecognitionRationale { get; set; }
    public CostAllocationBasis CostAllocationBasis { get; set; }

    public Guid? DefaultPaymentPlanTemplateId { get; set; }
    public string? DefaultPaymentPlanName { get; set; }
    public int HoldHours { get; set; }
    public decimal TransferFeePerSqFt { get; set; }
    public decimal TransferFeeFlat { get; set; }

    public string? RegistrationNumber { get; set; }
    public DateOnly? RegistrationValidUntil { get; set; }
    public string? RegulatorName { get; set; }

    public string? Tagline { get; set; }
    public string? LongDescription { get; set; }
    public string? UniqueSellingPoints { get; set; }
    public string? LocationAdvantages { get; set; }
    public string? BrochureUrl { get; set; }

    public string? ProjectManagerName { get; set; }
    public string? SalesHeadName { get; set; }

    public List<ProjectNodeDto> Structure { get; set; } = [];
    public List<ProjectMilestoneDto> Milestones { get; set; } = [];
    public List<ProjectTeamMemberDto> Team { get; set; } = [];
    public List<LookupDto> PriceLists { get; set; } = [];
    public List<LookupDto> SitePlans { get; set; } = [];
    public List<ApprovalRecordDto> Approvals { get; set; } = [];
}

public class AreaStatementDto
{
    public AreaDto TotalLandArea { get; set; } = new();
    public AreaDto SaleableArea { get; set; } = new();
    public AreaDto CommonArea { get; set; } = new();
    public AreaDto RoadsArea { get; set; } = new();
    public AreaDto ParksArea { get; set; } = new();
    public AreaDto AmenitiesArea { get; set; } = new();
    public AreaDto CommercialReserve { get; set; } = new();
    public AreaDto UtilitiesArea { get; set; } = new();
    public decimal SaleablePercent { get; set; }

    /// <summary>Set when the parts do not sum to the whole — a regulator-facing error.</summary>
    public decimal? UnaccountedSqFt { get; set; }
}

public class ProjectUpsertDto
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public ProjectKind Kind { get; set; }
    public ProjectStatus Status { get; set; }
    public Guid? OfficeId { get; set; }
    public Guid? GeoAreaId { get; set; }
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? CurrencyCode { get; set; }

    public DateOnly? LaunchDate { get; set; }
    public DateOnly? BookingOpenDate { get; set; }
    public DateOnly? ConstructionStartDate { get; set; }
    public DateOnly? PlannedCompletionDate { get; set; }
    public DateOnly? PromisedPossessionDate { get; set; }
    public DateOnly? ForecastPossessionDate { get; set; }

    public AreaUnit InputAreaUnit { get; set; } = AreaUnit.SquareFeet;
    public decimal TotalLandArea { get; set; }
    public decimal SaleableArea { get; set; }
    public decimal CommonArea { get; set; }
    public decimal RoadsArea { get; set; }
    public decimal ParksArea { get; set; }
    public decimal AmenitiesArea { get; set; }
    public decimal CommercialReserve { get; set; }
    public decimal UtilitiesArea { get; set; }

    public decimal TotalBudget { get; set; }
    public decimal? EscrowPercent { get; set; }
    public RecognitionBasis RecognitionBasis { get; set; }
    public string? RecognitionRationale { get; set; }
    public CostAllocationBasis CostAllocationBasis { get; set; }

    public Guid? DefaultPaymentPlanTemplateId { get; set; }
    public Guid? DefaultSurchargePolicyId { get; set; }
    public Guid? DefaultDunningPolicyId { get; set; }
    public Guid? DefaultDeductionPolicyId { get; set; }
    public int HoldHours { get; set; } = 48;
    public decimal TransferFeePerSqFt { get; set; }
    public decimal TransferFeeFlat { get; set; }

    public string? RegistrationNumber { get; set; }
    public DateOnly? RegistrationValidUntil { get; set; }
    public string? RegulatorName { get; set; }

    public string? Tagline { get; set; }
    public string? LongDescription { get; set; }
    public string? UniqueSellingPoints { get; set; }
    public string? LocationAdvantages { get; set; }
    public string? HeroImageUrl { get; set; }
    public string? BrochureUrl { get; set; }

    public Guid? ProjectManagerUserId { get; set; }
    public Guid? SalesHeadUserId { get; set; }
    public List<Guid> LandParcelIds { get; set; } = [];
}

/// <summary>A node in the project tree, with its children inline so the whole structure is one call.</summary>
public class ProjectNodeDto
{
    public Guid Id { get; set; }
    public Guid? ParentNodeId { get; set; }
    public ProjectNodeKind Kind { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public int SortOrder { get; set; }
    public int Depth { get; set; }
    public int? FloorNumber { get; set; }
    public AreaDto? Area { get; set; }
    public int PlannedUnitCount { get; set; }
    public int ActualUnitCount { get; set; }
    public ProjectStatus Status { get; set; }
    public DateOnly? PlannedCompletionDate { get; set; }
    public decimal ProgressPercent { get; set; }
    public string? FloorPlanUrl { get; set; }
    public string? SitePlanUrl { get; set; }

    public int UnitsAvailable { get; set; }
    public int UnitsBooked { get; set; }
    public int UnitsSold { get; set; }

    public List<ProjectNodeDto> Children { get; set; } = [];
}

public class ProjectNodeUpsertDto
{
    public Guid? Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? ParentNodeId { get; set; }
    public ProjectNodeKind Kind { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public int SortOrder { get; set; }
    public int? FloorNumber { get; set; }
    public decimal? Area { get; set; }
    public AreaUnit InputAreaUnit { get; set; } = AreaUnit.SquareFeet;
    public int PlannedUnitCount { get; set; }
    public ProjectStatus Status { get; set; }
    public DateOnly? PlannedCompletionDate { get; set; }
    public string? FloorPlanUrl { get; set; }
    public string? SitePlanUrl { get; set; }
}

/// <summary>
/// Bulk creation of a tower's units — "floors 1 to 20, four units per floor, named A to D".
///
/// A developer setting up a 400-unit project will not type four hundred rows, and a system that
/// makes them will lose to a spreadsheet.
/// </summary>
public class UnitGenerationDto
{
    public Guid ProjectId { get; set; }
    public Guid ProjectNodeId { get; set; }

    public int FromFloor { get; set; }
    public int ToFloor { get; set; }

    /// <summary>Floors to leave out — a services floor, a refuge floor, an unlucky number.</summary>
    public List<int> SkipFloors { get; set; } = [];

    /// <summary>Unit letters or numbers per floor: "A,B,C,D" or "01,02,03".</summary>
    public List<string> UnitCodes { get; set; } = [];

    /// <summary>"{floor}{code}" gives 12A; "{floor}-{code}" gives 12-A.</summary>
    public string NumberPattern { get; set; } = "{floor}{code}";

    public PropertySubType SubType { get; set; }
    public AreaUnit InputAreaUnit { get; set; } = AreaUnit.SquareFeet;
    public decimal SaleableArea { get; set; }
    public decimal? CarpetArea { get; set; }
    public int? Bedrooms { get; set; }
    public int? Bathrooms { get; set; }
    public int? ParkingBays { get; set; }
    public Facing? Facing { get; set; }

    /// <summary>Unit codes that sit on a corner, so the premium is applied as they are created.</summary>
    public List<string> CornerCodes { get; set; } = [];

    public Guid? PriceListId { get; set; }
    public bool CreateFloorNodes { get; set; } = true;

    /// <summary>Preview only. Nothing is written and the caller sees exactly what would be.</summary>
    public bool DryRun { get; set; }
}

public class UnitGenerationResultDto
{
    public int WouldCreateCount { get; set; }
    public int CreatedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<string> Conflicts { get; set; } = [];
    public List<InventoryUnitDto> Preview { get; set; } = [];
}

// ── The inventory board ──────────────────────────────────────────────────────

/// <summary>
/// One unit as the board draws it. Kept deliberately small — a board renders five thousand of
/// these and has to stay interactive, so anything the tile does not paint is not on this row.
/// </summary>
public class InventoryUnitDto
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public string UnitNumber { get; set; } = string.Empty;
    public PropertyStatus Status { get; set; }
    public PropertySubType SubType { get; set; }

    public Guid? ProjectNodeId { get; set; }
    public string? BlockName { get; set; }
    public int? FloorNumber { get; set; }
    public string? FloorLabel { get; set; }
    public int? StackIndex { get; set; }

    public decimal AreaSqFt { get; set; }
    public string AreaDisplay { get; set; } = string.Empty;
    public int? Bedrooms { get; set; }
    public Facing? Facing { get; set; }
    public bool IsCorner { get; set; }

    public decimal BasePrice { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal RatePerSqFt { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    // Live state, so a tile can show who has it without a second call.
    public Guid? HoldId { get; set; }
    public string? HeldForName { get; set; }
    public string? HeldByName { get; set; }
    public DateTime? HoldExpiresAt { get; set; }
    public int? HoldMinutesRemaining { get; set; }

    public Guid? BookingId { get; set; }
    public string? BuyerName { get; set; }
    public decimal? CollectionPercent { get; set; }

    public BlockReason? BlockReason { get; set; }
    public bool IsLandownerShare { get; set; }
    public bool HasLitigation { get; set; }
    public bool IsMortgaged { get; set; }

    public Guid? SitePlanShapeId { get; set; }
    public string? PlanPoints { get; set; }
}

/// <summary>What the board asks for. Every filter here is indexed.</summary>
public class InventoryQueryDto
{
    public Guid ProjectId { get; set; }
    public Guid? ProjectNodeId { get; set; }
    public List<PropertyStatus> Statuses { get; set; } = [];
    public List<PropertySubType> SubTypes { get; set; } = [];

    public decimal? MinArea { get; set; }
    public decimal? MaxArea { get; set; }
    public AreaUnit InputAreaUnit { get; set; } = AreaUnit.SquareFeet;

    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? Bedrooms { get; set; }
    public Facing? Facing { get; set; }
    public bool? CornerOnly { get; set; }
    public int? FromFloor { get; set; }
    public int? ToFloor { get; set; }
    public string? Search { get; set; }

    /// <summary>"plan", "stack" or "grid". Decides what the server bothers to compute.</summary>
    public string ViewMode { get; set; } = "grid";

    public Guid? SitePlanId { get; set; }
}

/// <summary>The board's payload: the units, the legend, and the totals its header shows.</summary>
public class InventoryBoardDto
{
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "USD";
    public AreaUnit DisplayAreaUnit { get; set; }

    public List<InventoryUnitDto> Units { get; set; } = [];
    public List<ProjectNodeDto> Blocks { get; set; } = [];
    public List<BreakdownSliceDto> StatusCounts { get; set; } = [];

    public int TotalUnits { get; set; }
    public decimal TotalValue { get; set; }
    public decimal AvailableValue { get; set; }
    public decimal SoldValue { get; set; }
    public decimal AbsorptionPercent { get; set; }

    /// <summary>Populated for the plan view only.</summary>
    public SitePlanDto? SitePlan { get; set; }

    /// <summary>Distinct floors present, so the stack view can lay out rows without scanning.</summary>
    public List<int> Floors { get; set; } = [];
    public List<string> StackCodes { get; set; } = [];
}

public class SitePlanDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int ImageWidthPx { get; set; }
    public int ImageHeightPx { get; set; }
    public bool IsDefault { get; set; }
    public List<SitePlanShapeDto> Shapes { get; set; } = [];
}

public class SitePlanShapeDto
{
    public Guid Id { get; set; }
    public Guid? UnitId { get; set; }
    public string? Label { get; set; }
    public string ShapeType { get; set; } = "polygon";
    public string Points { get; set; } = string.Empty;
    public decimal? LabelX { get; set; }
    public decimal? LabelY { get; set; }
    public bool IsDecorative { get; set; }
    public string? FillOverride { get; set; }
    public PropertyStatus? Status { get; set; }
}

/// <summary>A request to hold a unit for a named lead, with its clock.</summary>
public class HoldRequestDto
{
    public Guid UnitId { get; set; }
    public Guid? EnquiryId { get; set; }
    public Guid? PartyId { get; set; }
    public string? ContactName { get; set; }

    /// <summary>Above the role's limit this needs an approval, and the server says so rather than failing.</summary>
    public int Hours { get; set; }

    public string? Note { get; set; }
}

public class UnitHoldDto
{
    public Guid Id { get; set; }
    public Guid UnitId { get; set; }
    public string UnitNumber { get; set; } = string.Empty;
    public string? ProjectName { get; set; }
    public string? HeldForName { get; set; }
    public string HeldByName { get; set; } = string.Empty;
    public DateTime HeldAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public int MinutesRemaining { get; set; }
    public HoldStatus Status { get; set; }
    public string? Note { get; set; }
    public bool NeedsApproval { get; set; }
}

public class BlockRequestDto
{
    public Guid UnitId { get; set; }
    public BlockReason Reason { get; set; }
    public string? Note { get; set; }
    public DateOnly? ExpectedReleaseDate { get; set; }
}

// ── Pricing ──────────────────────────────────────────────────────────────────

public class PriceListDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
    public string? PublishedByName { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public string? Note { get; set; }
    public int LineCount { get; set; }
    public int UnitsAffected { get; set; }
    public List<PriceListLineDto> Lines { get; set; } = [];
}

public class PriceListLineDto
{
    public Guid? Id { get; set; }
    public Guid? UnitId { get; set; }
    public string? UnitNumber { get; set; }
    public Guid? ProjectNodeId { get; set; }
    public string? BlockName { get; set; }
    public PropertySubType? SubType { get; set; }
    public decimal? MinAreaSqFt { get; set; }
    public decimal? MaxAreaSqFt { get; set; }
    public int? MinFloor { get; set; }
    public int? MaxFloor { get; set; }
    public decimal RatePerSqFt { get; set; }
    public decimal? FlatPrice { get; set; }
    public int Specificity { get; set; }
}

public class PremiumChargeDto
{
    public Guid? Id { get; set; }
    public Guid ProjectId { get; set; }
    public ChargeKind Kind { get; set; }
    public string Label { get; set; } = string.Empty;
    public ChargeBasis Basis { get; set; }
    public decimal Rate { get; set; }
    public int? AppliesFromFloor { get; set; }
    public bool AppliesToCornerOnly { get; set; }
    public bool AppliesToParkFacingOnly { get; set; }
    public PropertySubType? AppliesToSubType { get; set; }
    public bool IsTaxable { get; set; }
    public decimal TaxPercent { get; set; }
    public bool IsPartOfSalePrice { get; set; }
    public bool IsOptional { get; set; }
    public bool IsAutoApplied { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
/// The printable quotation a buyer signs: unit, area, base price, every premium, every charge,
/// taxes, total, and the payment schedule underneath it.
/// </summary>
public class CostSheetDto
{
    public Guid UnitId { get; set; }
    public string UnitNumber { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string? BlockName { get; set; }
    public int? FloorNumber { get; set; }
    public PropertySubType SubType { get; set; }

    public AreaDto SaleableArea { get; set; } = new();
    public AreaDto? CarpetArea { get; set; }
    public decimal RatePerSqFt { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public List<CostSheetLineDto> SalePriceLines { get; set; } = [];
    public List<CostSheetLineDto> OtherChargeLines { get; set; } = [];

    public decimal BasePrice { get; set; }
    public decimal PremiumTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetSalePrice { get; set; }
    public decimal OtherChargesTotal { get; set; }
    public decimal TaxTotal { get; set; }
    public decimal GrandTotal { get; set; }

    /// <summary>Printed on every document, in the document's language.</summary>
    public string AmountInWords { get; set; } = string.Empty;

    public PaymentPlanPreviewDto? PaymentPlan { get; set; }
    public DateOnly GeneratedOn { get; set; }
    public DateOnly? ValidUntil { get; set; }
    public string? PreparedByName { get; set; }
    public string? Terms { get; set; }
}

public class CostSheetLineDto
{
    public ChargeKind Kind { get; set; }
    public string Label { get; set; } = string.Empty;
    public ChargeBasis Basis { get; set; }
    public decimal Rate { get; set; }
    public decimal Amount { get; set; }
    public decimal TaxPercent { get; set; }
    public decimal TaxAmount { get; set; }
    public bool IsOptional { get; set; }
    public bool IsAccepted { get; set; } = true;
    public int SortOrder { get; set; }
}

// ── Milestones, team and budget ──────────────────────────────────────────────

public class ProjectMilestoneDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? ProjectNodeId { get; set; }
    public string? BlockName { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public int SortOrder { get; set; }
    public MilestoneStatus Status { get; set; }
    public DateOnly? PlannedDate { get; set; }
    public DateOnly? ForecastDate { get; set; }
    public DateOnly? ReachedOn { get; set; }
    public DateOnly? CertifiedOn { get; set; }
    public string? CertifiedByName { get; set; }
    public string? CertificateUrl { get; set; }
    public decimal WeightPercent { get; set; }
    public decimal ProgressPercent { get; set; }
    public bool DemandsRaised { get; set; }

    /// <summary>How much money this certification would release across live payment plans.</summary>
    public decimal LinkedDemandValue { get; set; }
    public int LinkedInstalmentCount { get; set; }
    public int? SlipDays { get; set; }
}

public class ProjectTeamMemberDto
{
    public Guid Id { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? Name { get; set; }
    public Guid? UserId { get; set; }
    public Guid? PartyId { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}

public class ProjectBudgetLineDto
{
    public Guid Id { get; set; }
    public string CostHead { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BudgetAmount { get; set; }
    public decimal CommittedAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal ForecastAmount { get; set; }
    public decimal VarianceAmount { get; set; }
    public decimal VariancePercent { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
/// Projected collections against projected spend, month by month. The single number a developer's
/// board asks for, and the one no SMB tool produces.
/// </summary>
public class ProjectCashFlowDto
{
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "USD";
    public DateOnly FromMonth { get; set; }
    public DateOnly ToMonth { get; set; }

    public List<CashFlowMonthDto> Months { get; set; } = [];

    public decimal TotalProjectedInflow { get; set; }
    public decimal TotalProjectedOutflow { get; set; }
    public decimal NetPosition { get; set; }

    /// <summary>The worst month. What a funding conversation is actually about.</summary>
    public decimal PeakFundingGap { get; set; }
    public DateOnly? PeakGapMonth { get; set; }
}

public class CashFlowMonthDto
{
    public DateOnly Month { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal ProjectedCollections { get; set; }
    public decimal ActualCollections { get; set; }
    public decimal ProjectedSpend { get; set; }
    public decimal ActualSpend { get; set; }
    public decimal NetMovement { get; set; }
    public decimal ClosingPosition { get; set; }
    public bool IsActual { get; set; }
}

public class PlotFileDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public string FileNumber { get; set; } = string.Empty;
    public string CategoryCode { get; set; } = string.Empty;
    public AreaDto NominalArea { get; set; } = new();
    public PropertySubType SubType { get; set; }
    public PropertyStatus Status { get; set; }
    public decimal Price { get; set; }
    public string? OwnerName { get; set; }
    public Guid? CurrentBookingId { get; set; }
    public DateOnly IssuedOn { get; set; }
    public Guid? BallotId { get; set; }
    public Guid? AllottedUnitId { get; set; }
    public string? AllottedPlotNumber { get; set; }
    public DateOnly? AllottedOn { get; set; }
    public bool IsDuplicateIssued { get; set; }
    public bool IsCancelled { get; set; }
}

public class ApprovalRecordDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public ApprovalKind Kind { get; set; }
    public ApprovalState State { get; set; }
    public string? Authority { get; set; }
    public string? ApplicationNumber { get; set; }
    public string? ApprovalNumber { get; set; }
    public DateOnly? AppliedOn { get; set; }
    public DateOnly? GrantedOn { get; set; }
    public DateOnly? ValidUntil { get; set; }
    public decimal TotalCost { get; set; }
    public string? OwnerName { get; set; }
    public string? Conditions { get; set; }
    public string? DocumentUrl { get; set; }
    public bool IsBlocking { get; set; }
    public string? BlocksMilestoneName { get; set; }
    public bool IsMandatory { get; set; }
    public int? DaysToExpiry { get; set; }
    public bool IsOverdue { get; set; }
}
