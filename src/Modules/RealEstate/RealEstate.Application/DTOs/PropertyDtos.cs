using RealEstate.Domain.Enums;

namespace RealEstate.Application.DTOs;

// =====================================================================================
// The property master, the land bank behind it, and the valuation evidence.
// =====================================================================================

/// <summary>A property as it appears in a list or on a map pin. Deliberately small.</summary>
public class PropertyListItemDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string? Name { get; set; }
    public PropertyCategory Category { get; set; }
    public PropertySubType SubType { get; set; }
    public string SubTypeLabel { get; set; } = string.Empty;
    public PropertyStatus Status { get; set; }
    public OccupancyState Occupancy { get; set; }

    public string? AreaName { get; set; }
    public string? City { get; set; }
    public string AddressOneLine { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    public AreaDto? SaleableArea { get; set; }
    public int? Bedrooms { get; set; }
    public int? Bathrooms { get; set; }

    public decimal? AskingPrice { get; set; }
    public decimal? MonthlyRent { get; set; }
    public string? CurrencyCode { get; set; }

    public string? HeroImageUrl { get; set; }
    public string? ProjectName { get; set; }
    public string? UnitNumber { get; set; }
    public string? OwnerName { get; set; }

    public bool HasLitigation { get; set; }
    public bool IsMortgaged { get; set; }
    public int LiveListingCount { get; set; }
}

/// <summary>Everything about one property, for the 360 screen.</summary>
public class PropertyDetailDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string? Name { get; set; }

    public PropertyCategory Category { get; set; }
    public PropertySubType SubType { get; set; }
    public Tenure Tenure { get; set; }
    public PropertyStatus Status { get; set; }
    public OccupancyState Occupancy { get; set; }

    public AddressDto Address { get; set; } = new();
    public Guid? GeoAreaId { get; set; }
    public string? AreaPath { get; set; }

    // Position in a building
    public Guid? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public Guid? ProjectNodeId { get; set; }
    public string? BlockName { get; set; }
    public int? FloorNumber { get; set; }
    public string? FloorLabel { get; set; }
    public string? UnitNumber { get; set; }
    public string? StackCode { get; set; }
    public Facing? Facing { get; set; }
    public bool IsCorner { get; set; }
    public string? ViewDescription { get; set; }

    // Measurements, all in both units
    public AreaDto? PlotArea { get; set; }
    public AreaDto? CoveredArea { get; set; }
    public AreaDto? BuiltUpArea { get; set; }
    public AreaDto? SaleableArea { get; set; }
    public AreaDto? CarpetArea { get; set; }
    public AreaDto? TerraceArea { get; set; }
    public decimal? LoadingFactorPercent { get; set; }
    public decimal? FrontageFt { get; set; }
    public decimal? DepthFt { get; set; }
    public decimal? RoadWidthFt { get; set; }

    // Layout
    public int? Bedrooms { get; set; }
    public int? Bathrooms { get; set; }
    public int? HalfBaths { get; set; }
    public int? Kitchens { get; set; }
    public int? LivingRooms { get; set; }
    public int? ServantRooms { get; set; }
    public int? StoreRooms { get; set; }
    public int? ParkingBays { get; set; }
    public int? FloorsInUnit { get; set; }
    public FurnishingState? Furnishing { get; set; }
    public PropertyCondition? Condition { get; set; }
    public int? YearBuilt { get; set; }
    public Facing? EntranceDirection { get; set; }

    // Money
    public decimal? AskingPrice { get; set; }
    public decimal? ReservePrice { get; set; }
    public decimal? RatePerSqFt { get; set; }
    public decimal? MonthlyRent { get; set; }
    public decimal? LastSoldPrice { get; set; }
    public DateOnly? LastSoldOn { get; set; }
    public decimal? CurrentValuation { get; set; }
    public decimal? ServiceChargeRatePerSqFt { get; set; }
    public decimal? AnnualPropertyTax { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    // Flags
    public bool HasLitigation { get; set; }
    public bool IsMortgaged { get; set; }
    public bool IsLandownerShare { get; set; }

    public string? Notes { get; set; }

    public List<PropertyFeatureDto> Features { get; set; } = [];
    public List<PropertyMediaDto> Media { get; set; } = [];
    public List<PropertyOwnershipDto> Owners { get; set; } = [];
    public List<PropertyOwnershipDto> OwnershipHistory { get; set; } = [];
    public List<ListingListItemDto> Listings { get; set; } = [];
    public List<PropertyDocumentDto> Documents { get; set; } = [];
    public List<EncumbranceDto> Encumbrances { get; set; } = [];
    public List<ComplianceCertificateDto> Certificates { get; set; } = [];
    public List<TimelineEntryDto> Timeline { get; set; } = [];

    // Live state
    public Guid? CurrentTenancyId { get; set; }
    public string? CurrentTenantName { get; set; }
    public Guid? CurrentBookingId { get; set; }
    public string? CurrentBuyerName { get; set; }
    public decimal OutstandingDues { get; set; }
    public int OpenWorkOrders { get; set; }
}

public class PropertyUpsertDto
{
    public Guid? Id { get; set; }
    public string? Reference { get; set; }
    public string? Name { get; set; }

    public PropertyCategory Category { get; set; }
    public PropertySubType SubType { get; set; }
    public Tenure Tenure { get; set; } = Tenure.Freehold;
    public PropertyStatus Status { get; set; } = PropertyStatus.Draft;
    public OccupancyState Occupancy { get; set; } = OccupancyState.Vacant;

    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? Street { get; set; }
    public string? PostCode { get; set; }
    public Guid? GeoAreaId { get; set; }
    public Guid? CityId { get; set; }
    public Guid? CountryId { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? LocationCode { get; set; }

    public Guid? ProjectId { get; set; }
    public Guid? ProjectNodeId { get; set; }
    public int? FloorNumber { get; set; }
    public string? FloorLabel { get; set; }
    public string? UnitNumber { get; set; }
    public string? StackCode { get; set; }
    public Facing? Facing { get; set; }
    public bool IsCorner { get; set; }
    public string? ViewDescription { get; set; }

    /// <summary>Areas arrive in the operator's own unit; the server converts and stores square feet.</summary>
    public AreaUnit InputAreaUnit { get; set; } = AreaUnit.SquareFeet;
    public decimal? PlotArea { get; set; }
    public decimal? CoveredArea { get; set; }
    public decimal? BuiltUpArea { get; set; }
    public decimal? SaleableArea { get; set; }
    public decimal? CarpetArea { get; set; }
    public decimal? TerraceArea { get; set; }
    public decimal? GardenArea { get; set; }
    public decimal? FrontageFt { get; set; }
    public decimal? DepthFt { get; set; }
    public decimal? RoadWidthFt { get; set; }
    public decimal? CeilingHeightFt { get; set; }

    public int? Bedrooms { get; set; }
    public int? Bathrooms { get; set; }
    public int? HalfBaths { get; set; }
    public int? Kitchens { get; set; }
    public int? LivingRooms { get; set; }
    public int? ServantRooms { get; set; }
    public int? StoreRooms { get; set; }
    public int? ParkingBays { get; set; }
    public int? FloorsInUnit { get; set; }
    public FurnishingState? Furnishing { get; set; }
    public PropertyCondition? Condition { get; set; }
    public int? YearBuilt { get; set; }
    public Facing? EntranceDirection { get; set; }

    public decimal? AskingPrice { get; set; }
    public decimal? ReservePrice { get; set; }
    public decimal? MonthlyRent { get; set; }
    public decimal? ServiceChargeRatePerSqFt { get; set; }
    public string? PropertyTaxReference { get; set; }
    public decimal? AnnualPropertyTax { get; set; }
    public string? CurrencyCode { get; set; }

    public string? Notes { get; set; }
    public List<PropertyFeatureDto> Features { get; set; } = [];

    /// <summary>Set when the duplicate check matched and a human decided they are different.</summary>
    public bool AcknowledgeDuplicate { get; set; }
}

public class PropertyFeatureDto
{
    public Guid? Id { get; set; }
    public string FeatureKey { get; set; } = string.Empty;
    public string? Label { get; set; }
    public string? Category { get; set; }
    public string? Value { get; set; }
    public bool IsHighlight { get; set; }
    public int SortOrder { get; set; }
}

public class PropertyMediaDto
{
    public Guid Id { get; set; }
    public MediaKind Kind { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? Caption { get; set; }
    public string? RoomTag { get; set; }
    public int SortOrder { get; set; }
    public bool IsHero { get; set; }
    public bool ExcludeFromPortals { get; set; }
    public int? WidthPx { get; set; }
    public int? HeightPx { get; set; }
}

public class PropertyOwnershipDto
{
    public Guid Id { get; set; }
    public Guid PartyId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public string? FatherOrGuardianName { get; set; }
    public string? IdentityNumber { get; set; }
    public string? Phone { get; set; }
    public decimal SharePercent { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
    public TitleInstrument? AcquiredBy { get; set; }
    public string? DeedNumber { get; set; }
    public DateOnly? DeedDate { get; set; }
    public decimal? ConsiderationAmount { get; set; }
    public bool IsPrimaryOwner { get; set; }
    public bool IsCurrent { get; set; }
}

public class PropertyDocumentDto
{
    public Guid Id { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string Url { get; set; } = string.Empty;
    public DocumentState State { get; set; }
    public DateOnly? IssuedOn { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public bool IsExpired { get; set; }
    public bool IsConfidential { get; set; }
    public string? VerifiedByName { get; set; }
}

/// <summary>A duplicate the server found at creation time, with why it matched.</summary>
public class DuplicateCandidateDto
{
    public Guid PropertyId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string AddressOneLine { get; set; } = string.Empty;
    public string? ProjectName { get; set; }
    public string? UnitNumber { get; set; }
    public PropertyStatus Status { get; set; }

    /// <summary>"address", "geolocation", "unit number in project", "title reference".</summary>
    public string MatchedOn { get; set; } = string.Empty;

    public int ConfidencePercent { get; set; }
}

// ── Land bank ────────────────────────────────────────────────────────────────

public class LandParcelListItemDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? SurveyNumber { get; set; }
    public string? Mouza { get; set; }
    public string? District { get; set; }
    public AreaDto RecordArea { get; set; } = new();
    public AreaDto? SurveyedArea { get; set; }
    public decimal? AreaVarianceSqFt { get; set; }
    public AcquisitionStageKind Stage { get; set; }
    public bool IsAcquired { get; set; }
    public string? ProjectName { get; set; }
    public decimal? AgreedPrice { get; set; }
    public decimal TotalAcquisitionCost { get; set; }
    public decimal MonthlyHoldingCost { get; set; }
    public bool HasEncumbrance { get; set; }
    public bool HasLitigation { get; set; }
    public int OpenVerificationCount { get; set; }
}

public class LandParcelDetailDto : LandParcelListItemDto
{
    public string? KhasraNumber { get; set; }
    public string? KhewatNumber { get; set; }
    public string? KhatuniNumber { get; set; }
    public string? Village { get; set; }
    public string? Tehsil { get; set; }
    public string? RegistrarOffice { get; set; }

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? BoundaryGeoJson { get; set; }

    public string? CurrentLandUse { get; set; }
    public string? IntendedLandUse { get; set; }
    public decimal? MaxFloorAreaRatio { get; set; }
    public decimal? MaxCoveragePercent { get; set; }
    public decimal? MaxHeightFt { get; set; }

    public List<TitleChainEntryDto> TitleChain { get; set; } = [];
    public List<EncumbranceDto> Encumbrances { get; set; } = [];
    public List<TitleVerificationItemDto> VerificationItems { get; set; } = [];
    public List<AcquisitionCostLineDto> CostLines { get; set; } = [];
    public LandAcquisitionDto? Acquisition { get; set; }
}

public class TitleChainEntryDto
{
    public Guid Id { get; set; }
    public int SequenceNumber { get; set; }
    public TitleInstrument Instrument { get; set; }
    public DateOnly InstrumentDate { get; set; }
    public string? InstrumentNumber { get; set; }
    public string? RegistrarOffice { get; set; }
    public string? TransferorName { get; set; }
    public string? TransfereeName { get; set; }
    public decimal? Consideration { get; set; }
    public string? DocumentUrl { get; set; }
    public VerificationVerdict Verdict { get; set; }
    public string? VerificationNote { get; set; }
}

public class EncumbranceDto
{
    public Guid Id { get; set; }
    public EncumbranceKind Kind { get; set; }
    public EncumbranceStatus Status { get; set; }
    public string? HolderName { get; set; }
    public decimal? Amount { get; set; }
    public DateOnly? CreatedOn { get; set; }
    public DateOnly? ExpectedClearanceDate { get; set; }
    public DateOnly? ClearedOn { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? DocumentUrl { get; set; }
    public bool BlocksTransaction { get; set; }
    public string? Note { get; set; }
}

public class TitleVerificationItemDto
{
    public Guid Id { get; set; }
    public string CheckKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public VerificationVerdict Verdict { get; set; }
    public string? AssignedToName { get; set; }
    public DateOnly? DueDate { get; set; }
    public DateOnly? CompletedOn { get; set; }
    public string? EvidenceUrl { get; set; }
    public string? Findings { get; set; }
    public string? Condition { get; set; }
    public bool IsMandatory { get; set; }
    public bool IsOverdue { get; set; }
    public int SortOrder { get; set; }
}

public class LandAcquisitionDto
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public Guid LandParcelId { get; set; }
    public string? ParcelReference { get; set; }
    public string? SellerName { get; set; }
    public AcquisitionStageKind Stage { get; set; }
    public DateOnly? StartedOn { get; set; }
    public DateOnly? TargetCompletionDate { get; set; }
    public DateOnly? CompletedOn { get; set; }
    public decimal? AskingPrice { get; set; }
    public decimal? OfferedPrice { get; set; }
    public decimal? AgreedPrice { get; set; }
    public decimal AdvancePaid { get; set; }
    public decimal TotalPaid { get; set; }
    public string? OwnerName { get; set; }
    public string? BlockingIssue { get; set; }
    public bool IsAborted { get; set; }
    public int DaysInStage { get; set; }
    public List<AcquisitionStageDto> Stages { get; set; } = [];
}

public class AcquisitionStageDto
{
    public Guid Id { get; set; }
    public AcquisitionStageKind Kind { get; set; }
    public DateOnly? TargetDate { get; set; }
    public DateOnly? ActualDate { get; set; }
    public string? OwnerName { get; set; }
    public decimal? EstimatedCost { get; set; }
    public decimal? ActualCost { get; set; }
    public string? Note { get; set; }
    public bool IsOverdue { get; set; }
    public int SortOrder { get; set; }
}

public class AcquisitionCostLineDto
{
    public Guid Id { get; set; }
    public string CostType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BudgetAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal Variance { get; set; }
    public DateOnly? IncurredOn { get; set; }
    public string? PayeeName { get; set; }
    public string? Reference { get; set; }
    public bool IsCapitalised { get; set; }
}

// ── Valuation ────────────────────────────────────────────────────────────────

public class PropertyValuationDto
{
    public Guid Id { get; set; }
    public Guid PropertyId { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public decimal? ValueLow { get; set; }
    public decimal? ValueHigh { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public DateOnly ValuedOn { get; set; }
    public string? ValuerName { get; set; }
    public string? ReportUrl { get; set; }
    public string? Rationale { get; set; }
    public decimal? AnnualRent { get; set; }
    public decimal? CapRatePercent { get; set; }
    public decimal? GrossYieldPercent { get; set; }
    public decimal? NetYieldPercent { get; set; }
    public bool IsCurrent { get; set; }
    public List<PropertyComparableDto> Comparables { get; set; } = [];
}

public class PropertyComparableDto
{
    public Guid Id { get; set; }
    public Guid? ComparablePropertyId { get; set; }
    public string? Address { get; set; }
    public PropertySubType? SubType { get; set; }
    public decimal? AreaSqFt { get; set; }
    public int? Bedrooms { get; set; }
    public decimal TransactionPrice { get; set; }
    public DateOnly TransactionDate { get; set; }
    public string EvidenceType { get; set; } = "Sold";
    public string? Source { get; set; }
    public decimal AdjustmentPercent { get; set; }
    public string? AdjustmentNote { get; set; }
    public decimal AdjustedPricePerSqFt { get; set; }
    public decimal Weight { get; set; }
    public int MonthsAgo { get; set; }
}

/// <summary>A price opinion with its working shown. Never presented as a certified valuation.</summary>
public class PriceOpinionDto
{
    public Guid PropertyId { get; set; }
    public decimal SuggestedPrice { get; set; }
    public decimal LowPrice { get; set; }
    public decimal HighPrice { get; set; }
    public decimal SuggestedRatePerSqFt { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public int ComparableCount { get; set; }
    public string Basis { get; set; } = string.Empty;
    public string Confidence { get; set; } = "Medium";
    public List<PropertyComparableDto> Comparables { get; set; } = [];

    /// <summary>Each adjustment, in words, so the number can be argued with rather than believed.</summary>
    public List<string> Workings { get; set; } = [];
}
