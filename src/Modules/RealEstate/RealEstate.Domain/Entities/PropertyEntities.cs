using Nexcore.SharedKernel;
using RealEstate.Domain.Enums;

namespace RealEstate.Domain.Entities;

/// <summary>
/// The universal asset master. A plot in a scheme, an apartment in a tower, a shop in a mart, a
/// villa on a client's own land and a warehouse being let are all rows in this table.
///
/// One table rather than four, because every line of business needs to ask the same questions of
/// all of them — where is it, who owns it, what is it worth, what is outstanding on it — and a
/// property routinely changes which business it belongs to. A developer's unit becomes a resale
/// listing becomes a tenancy becomes a society member's home, and splitting that across four
/// tables would break the one thing operators care about: the property's whole history in one place.
///
/// A developer's unit is a Property with <see cref="ProjectId"/> set; an agency's instruction is
/// one with it null. Nothing else differs.
/// </summary>
public class Property : BaseEntity
{
    public string Reference { get; set; } = string.Empty;
    public string? Name { get; set; }

    public PropertyCategory Category { get; set; } = PropertyCategory.Residential;
    public PropertySubType SubType { get; set; } = PropertySubType.Apartment;
    public Tenure Tenure { get; set; } = Tenure.Freehold;
    public PropertyStatus Status { get; set; } = PropertyStatus.Draft;
    public OccupancyState Occupancy { get; set; } = OccupancyState.Vacant;

    // ── Where it is ──────────────────────────────────────────────────────────

    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? Street { get; set; }
    public string? PostCode { get; set; }
    public Guid? GeoAreaId { get; set; }
    public GeoArea? GeoArea { get; set; }
    public Guid? CityId { get; set; }
    public Guid? CountryId { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    /// <summary>Plus code or equivalent, because half this market has no postal address.</summary>
    public string? LocationCode { get; set; }

    // ── Where it sits in a building ──────────────────────────────────────────

    /// <summary>
    /// The building or estate this unit sits inside. A rent roll, a service-charge apportionment
    /// and a tenant-mix analysis all run over "everything in this building", and without the link
    /// that set can only be assembled by guessing at addresses.
    /// </summary>
    public Guid? MasterPropertyId { get; set; }

    /// <summary>Retail classification, for tenant-mix management in a mall or a scheme.</summary>
    public Guid? TenantCategoryId { get; set; }

    public Guid? ProjectId { get; set; }
    public Guid? ProjectNodeId { get; set; }

    /// <summary>Basements are negative, ground is 0, mezzanine is fractional-by-convention (stored as 0 with a label).</summary>
    public int? FloorNumber { get; set; }
    public string? FloorLabel { get; set; }
    public string? UnitNumber { get; set; }

    /// <summary>The vertical line a unit sits on — "A", "02". Two units on the same stack share a view and a plan.</summary>
    public string? StackCode { get; set; }

    public Facing? Facing { get; set; }
    public bool IsCorner { get; set; }
    public string? ViewDescription { get; set; }

    // ── How big it is ────────────────────────────────────────────────────────
    // All areas are stored in square feet. The display unit is an office preference.

    public decimal? PlotAreaSqFt { get; set; }
    public decimal? CoveredAreaSqFt { get; set; }
    public decimal? BuiltUpAreaSqFt { get; set; }

    /// <summary>Super built-up. What the customer is charged for, and usually the largest number.</summary>
    public decimal? SaleableAreaSqFt { get; set; }

    /// <summary>What they can actually walk on. The gap between this and saleable is the loading.</summary>
    public decimal? CarpetAreaSqFt { get; set; }

    public decimal? TerraceAreaSqFt { get; set; }
    public decimal? GardenAreaSqFt { get; set; }

    /// <summary>Percentage added to carpet to reach saleable. Regulated disclosure in several markets.</summary>
    public decimal? LoadingFactorPercent { get; set; }

    public decimal? FrontageFt { get; set; }
    public decimal? DepthFt { get; set; }
    public decimal? RoadWidthFt { get; set; }
    public decimal? CeilingHeightFt { get; set; }

    // ── What is in it ────────────────────────────────────────────────────────

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

    /// <summary>A real buying factor across South Asia, and worth money. Not a curiosity.</summary>
    public Facing? EntranceDirection { get; set; }

    // ── What it is worth ─────────────────────────────────────────────────────

    public decimal? AskingPrice { get; set; }
    public decimal? ReservePrice { get; set; }
    public string? CurrencyCode { get; set; }
    public decimal? RatePerSqFt { get; set; }
    public decimal? MonthlyRent { get; set; }
    public decimal? LastSoldPrice { get; set; }
    public DateOnly? LastSoldOn { get; set; }
    public decimal? CurrentValuation { get; set; }
    public DateOnly? ValuedOn { get; set; }
    public decimal? ServiceChargeRatePerSqFt { get; set; }
    public string? PropertyTaxReference { get; set; }
    public decimal? AnnualPropertyTax { get; set; }

    // ── Flags ────────────────────────────────────────────────────────────────

    /// <summary>Shown on every screen this property appears on. A disputed unit must never be quietly sold.</summary>
    public bool HasLitigation { get; set; }
    public bool IsMortgaged { get; set; }

    /// <summary>Landowner's share under a JV. Ours to build, not ours to sell.</summary>
    public bool IsLandownerShare { get; set; }

    public Guid? CurrentTenancyId { get; set; }
    public Guid? CurrentBookingId { get; set; }
    public Guid? ListedByAgentId { get; set; }

    public string? Notes { get; set; }

    /// <summary>Set when a duplicate check matched and a human decided they were different after all.</summary>
    public bool DuplicateCheckOverridden { get; set; }

    public ICollection<PropertyFeature> Features { get; set; } = [];
    public ICollection<PropertyMedia> Media { get; set; } = [];
    public ICollection<PropertyOwnership> Ownerships { get; set; } = [];
    public ICollection<PropertyStatusHistory> StatusHistory { get; set; } = [];
    public ICollection<PropertyNote> NoteEntries { get; set; } = [];
}

/// <summary>
/// A tagged amenity. A join table rather than fifty booleans, because every market wants five
/// features nobody else has ever asked for, and a schema change per customer is not a product.
/// </summary>
public class PropertyFeature : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Property? Property { get; set; }

    /// <summary>Stable key: "lift", "generator", "central_ac", "gated", "mosque_nearby".</summary>
    public string FeatureKey { get; set; } = string.Empty;

    public string? Label { get; set; }

    /// <summary>Grouping for the UI: "Utilities", "Security", "Community", "Interior".</summary>
    public string? Category { get; set; }

    /// <summary>For features that carry a number — "backup power: 12 kVA", "parking: 2".</summary>
    public string? Value { get; set; }

    public bool IsHighlight { get; set; }
    public int SortOrder { get; set; }
}

public class PropertyMedia : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Property? Property { get; set; }
    public Guid? ListingId { get; set; }

    public MediaKind Kind { get; set; } = MediaKind.Photo;
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? Caption { get; set; }

    /// <summary>Which room it shows, so a portal that wants "kitchen first" can be satisfied.</summary>
    public string? RoomTag { get; set; }

    public int SortOrder { get; set; }

    /// <summary>The one used on cards, feeds and the brochure cover. Exactly one per property.</summary>
    public bool IsHero { get; set; }

    public bool IsWatermarked { get; set; }
    public long? FileSizeBytes { get; set; }
    public int? WidthPx { get; set; }
    public int? HeightPx { get; set; }

    /// <summary>Some portals reject media below their minimum; this records that decision per push.</summary>
    public bool ExcludeFromPortals { get; set; }
}

/// <summary>
/// Who owns it now. Closed rows are the ownership chain — never updated in place, so the chain
/// reads end to end forever and survives a court challenge twenty years later.
/// </summary>
public class PropertyOwnership : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Property? Property { get; set; }
    public Guid PartyId { get; set; }

    /// <summary>Percentage held. Joint owners sum to 100.</summary>
    public decimal SharePercent { get; set; } = 100m;

    public DateOnly FromDate { get; set; }

    /// <summary>Null while current. Set — never deleted — when ownership moves on.</summary>
    public DateOnly? ToDate { get; set; }

    public TitleInstrument? AcquiredBy { get; set; }

    /// <summary>The transfer, booking or deed that created this row.</summary>
    public Guid? SourceTransferId { get; set; }
    public Guid? SourceBookingId { get; set; }

    public string? DeedNumber { get; set; }
    public DateOnly? DeedDate { get; set; }
    public decimal? ConsiderationAmount { get; set; }

    public bool IsPrimaryOwner { get; set; }
    public Guid? PowerOfAttorneyPartyId { get; set; }
    public Guid? NomineePartyId { get; set; }
}

/// <summary>
/// A structural link between two properties, so a merge or a split keeps its history rather than
/// orphaning it. Deleting the parent must never cascade — the child outlives it.
/// </summary>
public class PropertyRelationship : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Guid RelatedPropertyId { get; set; }
    public PropertyRelationKind Kind { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public string? Note { get; set; }
}

public class PropertyStatusHistory : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Property? Property { get; set; }

    public PropertyStatus FromStatus { get; set; }
    public PropertyStatus ToStatus { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public Guid ChangedByUserId { get; set; }
    public Guid? ReasonCodeId { get; set; }
    public string? Note { get; set; }

    /// <summary>What caused it — a booking, a cancellation, a transfer, a hold expiry.</summary>
    public string? SourceDocumentType { get; set; }
    public Guid? SourceDocumentId { get; set; }
}

public class PropertyNote : BaseEntity
{
    public Guid PropertyId { get; set; }
    public Property? Property { get; set; }
    public string Body { get; set; } = string.Empty;

    /// <summary>Internal notes never reach a portal, a brochure or a customer statement.</summary>
    public bool IsInternal { get; set; } = true;

    public bool IsPinned { get; set; }
    public Guid AuthorUserId { get; set; }
}

public class PropertyDocument : BaseEntity
{
    public Guid PropertyId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string Url { get; set; } = string.Empty;
    public DocumentState State { get; set; } = DocumentState.Received;
    public DateOnly? IssuedOn { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public Guid? VerifiedByUserId { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public bool IsConfidential { get; set; }
}

/// <summary>
/// An opinion of value with its working attached. Never presented as a certified valuation —
/// that is a licensed activity — but always defensible, because the inputs are stored.
/// </summary>
public class PropertyValuation : BaseEntity
{
    public Guid PropertyId { get; set; }

    /// <summary>"Sale", "Mortgage", "Insurance", "Book", "Tax", "Dispute", "RentReview".</summary>
    public string Purpose { get; set; } = string.Empty;

    /// <summary>"Comparable", "Income", "Cost", "Residual".</summary>
    public string Method { get; set; } = "Comparable";

    public decimal Value { get; set; }
    public decimal? ValueLow { get; set; }
    public decimal? ValueHigh { get; set; }
    public string? CurrencyCode { get; set; }
    public DateOnly ValuedOn { get; set; }

    public Guid? ValuerPartyId { get; set; }
    public string? ValuerName { get; set; }
    public string? ReportUrl { get; set; }

    /// <summary>The reasoning, shown on screen. An opinion nobody can interrogate is worthless.</summary>
    public string? Rationale { get; set; }

    // Income-method inputs, kept so a yield can be re-derived rather than re-typed.
    public decimal? AnnualRent { get; set; }
    public decimal? CapRatePercent { get; set; }
    public decimal? GrossYieldPercent { get; set; }
    public decimal? NetYieldPercent { get; set; }

    public bool IsCurrent { get; set; }

    public ICollection<PropertyComparable> Comparables { get; set; } = [];
}

/// <summary>One piece of evidence behind a valuation, with the adjustment that made it comparable.</summary>
public class PropertyComparable : BaseEntity
{
    public Guid PropertyValuationId { get; set; }
    public PropertyValuation? Valuation { get; set; }

    /// <summary>Set when the comparable is our own stock. Null for market evidence typed in.</summary>
    public Guid? ComparablePropertyId { get; set; }

    public string? Address { get; set; }
    public PropertySubType? SubType { get; set; }
    public decimal? AreaSqFt { get; set; }
    public int? Bedrooms { get; set; }
    public decimal TransactionPrice { get; set; }
    public DateOnly TransactionDate { get; set; }

    /// <summary>"Sold", "Listed", "Let", "Withdrawn". A listing price is weaker evidence than a sale.</summary>
    public string EvidenceType { get; set; } = "Sold";

    public string? Source { get; set; }

    /// <summary>Net adjustment applied for area, floor, condition, age and date. Can be negative.</summary>
    public decimal AdjustmentPercent { get; set; }

    public string? AdjustmentNote { get; set; }
    public decimal AdjustedPricePerSqFt { get; set; }

    /// <summary>How much this comparable counts. Weights across a valuation sum to 100.</summary>
    public decimal Weight { get; set; }
}

/// <summary>A property tax demand or payment, tracked because it follows the asset, not the owner.</summary>
public class PropertyTaxRecord : BaseEntity
{
    public Guid PropertyId { get; set; }
    public string? Authority { get; set; }
    public string? AssessmentNumber { get; set; }
    public int TaxYear { get; set; }
    public decimal AssessedValue { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public DateOnly? DueDate { get; set; }
    public DateOnly? PaidOn { get; set; }
    public string? ReceiptReference { get; set; }
    public CostBearer BorneBy { get; set; } = CostBearer.Landlord;
}
