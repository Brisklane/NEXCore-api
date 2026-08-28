using Nexcore.SharedKernel;
using RealEstate.Domain.Enums;

namespace RealEstate.Domain.Entities;

/// <summary>
/// A development. Land plus approvals plus a bill of quantities plus a sales inventory plus an
/// escrow account plus a landowner's share plus a five-year defect liability — which is why it is
/// not a production order and not a job.
/// </summary>
public class Project : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public ProjectKind Kind { get; set; } = ProjectKind.ApartmentTower;
    public ProjectStatus Status { get; set; } = ProjectStatus.Concept;

    public Guid? OfficeId { get; set; }
    public Guid? GeoAreaId { get; set; }
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string CurrencyCode { get; set; } = AppConstants.DefaultCurrency;

    // ── Programme ────────────────────────────────────────────────────────────

    public DateOnly? LaunchDate { get; set; }
    public DateOnly? BookingOpenDate { get; set; }
    public DateOnly? ConstructionStartDate { get; set; }
    public DateOnly? PlannedCompletionDate { get; set; }

    /// <summary>What the customer was promised. Slipping past it has a compensation cost in some regimes.</summary>
    public DateOnly? PromisedPossessionDate { get; set; }

    /// <summary>Today's honest estimate. The gap to the promised date is the exposure.</summary>
    public DateOnly? ForecastPossessionDate { get; set; }

    public DateOnly? ActualCompletionDate { get; set; }
    public DateOnly? HandoverToSocietyDate { get; set; }

    // ── Area statement ───────────────────────────────────────────────────────
    // Regulator-facing numbers. They must reconcile, so they are stored, not derived on the fly.

    public decimal TotalLandAreaSqFt { get; set; }
    public decimal SaleableAreaSqFt { get; set; }
    public decimal CommonAreaSqFt { get; set; }
    public decimal RoadsAreaSqFt { get; set; }
    public decimal ParksAreaSqFt { get; set; }
    public decimal AmenitiesAreaSqFt { get; set; }
    public decimal CommercialReserveSqFt { get; set; }
    public decimal UtilitiesAreaSqFt { get; set; }

    // ── Money ────────────────────────────────────────────────────────────────

    public decimal TotalBudget { get; set; }
    public decimal TotalSalesValue { get; set; }
    public decimal TotalCollected { get; set; }

    /// <summary>Share of every collection that must land in the regulated account. Overrides the company default.</summary>
    public decimal? EscrowPercent { get; set; }

    public Guid? EscrowAccountId { get; set; }
    public Guid? FreeAccountId { get; set; }

    /// <summary>The IFRIC 15 call. Changes the P&amp;L completely, so it is a project-level decision with reasoning.</summary>
    public RecognitionBasis RecognitionBasis { get; set; } = RecognitionBasis.PointInTime;
    public string? RecognitionRationale { get; set; }

    public CostAllocationBasis CostAllocationBasis { get; set; } = CostAllocationBasis.ByArea;

    // ── Policy defaults inherited by every unit and booking ──────────────────

    public Guid? DefaultPaymentPlanTemplateId { get; set; }
    public Guid? DefaultSurchargePolicyId { get; set; }
    public Guid? DefaultDunningPolicyId { get; set; }
    public Guid? DefaultDeductionPolicyId { get; set; }
    public int HoldHours { get; set; } = 48;
    public decimal TransferFeePerSqFt { get; set; }
    public decimal TransferFeeFlat { get; set; }

    // ── Registration ─────────────────────────────────────────────────────────

    public string? RegistrationNumber { get; set; }
    public DateOnly? RegistrationValidUntil { get; set; }
    public string? RegulatorName { get; set; }

    // ── Microsite content, reused by listings, portals and the customer portal ──

    public string? Tagline { get; set; }
    public string? LongDescription { get; set; }
    public string? UniqueSellingPoints { get; set; }
    public string? LocationAdvantages { get; set; }
    public string? HeroImageUrl { get; set; }
    public string? BrochureUrl { get; set; }

    public Guid? ProjectManagerUserId { get; set; }
    public Guid? SalesHeadUserId { get; set; }

    /// <summary>A JV project's stock is partly the landowner's. Blocks it from general sale.</summary>
    public bool HasJointVenture { get; set; }

    public ICollection<ProjectNode> Nodes { get; set; } = [];
    public ICollection<ProjectMilestone> Milestones { get; set; } = [];
    public ICollection<ProjectTeamMember> TeamMembers { get; set; } = [];
    public ICollection<ProjectBudgetLine> BudgetLines { get; set; } = [];
}

/// <summary>
/// One level of the project tree — a phase, a block, a tower, a floor, a street.
///
/// A single self-referencing entity rather than four tables, because the depth is the market's
/// choice: a plot scheme is Project → Phase → Block → Street → Plot, while a tower is
/// Project → Tower → Floor → Unit, and both must render on the same inventory board.
/// </summary>
public class ProjectNode : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    public Guid? ParentNodeId { get; set; }
    public ProjectNode? Parent { get; set; }

    public ProjectNodeKind Kind { get; set; } = ProjectNodeKind.Block;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public int Depth { get; set; }
    public string? Path { get; set; }

    /// <summary>Set on a floor node. Basements are negative; ground is zero.</summary>
    public int? FloorNumber { get; set; }

    public decimal? AreaSqFt { get; set; }
    public int PlannedUnitCount { get; set; }

    public ProjectStatus Status { get; set; } = ProjectStatus.Planning;
    public DateOnly? PlannedCompletionDate { get; set; }

    /// <summary>Percentage physically built. Drives milestone-linked demands for this block only.</summary>
    public decimal ProgressPercent { get; set; }

    public string? FloorPlanUrl { get; set; }
    public string? SitePlanUrl { get; set; }

    public ICollection<ProjectNode> Children { get; set; } = [];
}

/// <summary>
/// The salesroom's view of a unit: the money, the hold and the status, hanging off the physical
/// <see cref="Property"/> record.
///
/// Two records rather than one because they answer different questions and change at different
/// rates. The Property is what the thing *is* — four hundred square yards, corner, park-facing —
/// and outlives every sale. The Unit is what the thing *costs today and who has it*, which is
/// re-priced with every price list and re-held every afternoon.
/// </summary>
public class Unit : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Guid? ProjectNodeId { get; set; }

    /// <summary>The physical record. One-to-one, and always present.</summary>
    public Guid PropertyId { get; set; }
    public Property? Property { get; set; }

    public string UnitNumber { get; set; } = string.Empty;
    public PropertyStatus Status { get; set; } = PropertyStatus.Available;

    // ── Price as it stands today ─────────────────────────────────────────────

    public Guid? PriceListId { get; set; }
    public decimal BaseRatePerSqFt { get; set; }
    public decimal BasePrice { get; set; }

    /// <summary>Base plus every premium and charge. What the cost sheet totals to.</summary>
    public decimal TotalPrice { get; set; }

    public string? CurrencyCode { get; set; }

    // ── Live state ───────────────────────────────────────────────────────────

    public Guid? CurrentHoldId { get; set; }
    public Guid? CurrentBookingId { get; set; }
    public Guid? CurrentBlockId { get; set; }

    /// <summary>Set when this is the landowner's share under a JV. Not ours to sell.</summary>
    public Guid? LandownerAllocationId { get; set; }

    /// <summary>Set for a scheme selling files before the ballot. Null once the file becomes a numbered plot.</summary>
    public Guid? PlotFileId { get; set; }

    // ── Board rendering ──────────────────────────────────────────────────────

    /// <summary>Polygon on the site plan, so the plan itself becomes the inventory board.</summary>
    public Guid? SitePlanShapeId { get; set; }

    public int? StackIndex { get; set; }
    public int? PositionOnFloor { get; set; }

    public bool IsSaleable { get; set; } = true;
    public DateOnly? AvailableFrom { get; set; }

    public ICollection<UnitPremium> Premiums { get; set; } = [];
}

/// <summary>
/// One line of the price ladder for one unit — floor rise, corner, park facing, development
/// charge. Held per unit rather than computed on the fly so the cost sheet a customer signed
/// three years ago can still be reproduced exactly.
/// </summary>
public class UnitPremium : BaseEntity
{
    public Guid UnitId { get; set; }
    public Unit? Unit { get; set; }

    public ChargeKind Kind { get; set; }
    public string Label { get; set; } = string.Empty;
    public ChargeBasis Basis { get; set; } = ChargeBasis.Fixed;

    /// <summary>The rate, percentage or per-floor amount, depending on <see cref="Basis"/>.</summary>
    public decimal Rate { get; set; }

    public decimal Amount { get; set; }
    public bool IsTaxable { get; set; }
    public decimal TaxPercent { get; set; }

    /// <summary>Charges outside the sale price — stamp duty, registration, corpus — bill separately.</summary>
    public bool IsPartOfSalePrice { get; set; } = true;

    /// <summary>Optional extras the customer may decline, like a second parking bay.</summary>
    public bool IsOptional { get; set; }

    public int SortOrder { get; set; }
}

/// <summary>
/// A short-lived claim on a unit for a named lead.
///
/// The single most valuable small feature on the inventory board: without it, two salespeople sell
/// the same apartment on the same afternoon and the developer keeps the argument, not the money.
/// It expires by itself.
/// </summary>
public class UnitHold : BaseEntity
{
    public Guid UnitId { get; set; }
    public Guid? EnquiryId { get; set; }
    public Guid? PartyId { get; set; }
    public Guid HeldByUserId { get; set; }

    public DateTime HeldAt { get; set; } = DateTime.UtcNow;

    /// <summary>The clock. Passing it releases the unit automatically and notifies the holder.</summary>
    public DateTime ExpiresAt { get; set; }

    public HoldStatus Status { get; set; } = HoldStatus.Active;
    public DateTime? ReleasedAt { get; set; }
    public Guid? ReleasedByUserId { get; set; }
    public string? Note { get; set; }

    /// <summary>Set when the hold ran past the role's limit and a manager allowed it.</summary>
    public Guid? ApprovalRequestId { get; set; }

    public Guid? ConvertedToBookingId { get; set; }
}

/// <summary>Stock taken off the market deliberately, with an authority and a way back.</summary>
public class UnitBlockRecord : BaseEntity
{
    public Guid UnitId { get; set; }
    public BlockReason Reason { get; set; }
    public string? Note { get; set; }

    public Guid BlockedByUserId { get; set; }
    public DateTime BlockedAt { get; set; } = DateTime.UtcNow;
    public DateOnly? ExpectedReleaseDate { get; set; }

    public DateTime? ReleasedAt { get; set; }
    public Guid? ReleasedByUserId { get; set; }

    /// <summary>Set when the block is a lender's charge, so releasing it needs the charge released first.</summary>
    public Guid? EncumbranceId { get; set; }
}

public class UnitStatusHistory : BaseEntity
{
    public Guid UnitId { get; set; }
    public PropertyStatus FromStatus { get; set; }
    public PropertyStatus ToStatus { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    public Guid ChangedByUserId { get; set; }
    public string? SourceDocumentType { get; set; }
    public Guid? SourceDocumentId { get; set; }
    public Guid? ReasonCodeId { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// A dated, approved price schedule for a project.
///
/// Versioned rather than edited so that "the price on the day of booking" is provable years later
/// in front of a customer or a regulator. Publishing a new list re-prices available stock only;
/// booked units keep the list they were sold on.
/// </summary>
public class PriceList : BaseEntity
{
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Version { get; set; } = 1;

    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }

    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
    public Guid? PublishedByUserId { get; set; }
    public Guid? ApprovalRequestId { get; set; }

    public string? CurrencyCode { get; set; }
    public string? Note { get; set; }

    public ICollection<PriceListLine> Lines { get; set; } = [];
}

/// <summary>
/// A rate for a slice of stock — by unit type, by size band, by floor band, or by named unit.
/// Applied top-down: the most specific line that matches a unit wins.
/// </summary>
public class PriceListLine : BaseEntity
{
    public Guid PriceListId { get; set; }
    public PriceList? PriceList { get; set; }

    public Guid? UnitId { get; set; }
    public Guid? ProjectNodeId { get; set; }
    public PropertySubType? SubType { get; set; }
    public decimal? MinAreaSqFt { get; set; }
    public decimal? MaxAreaSqFt { get; set; }
    public int? MinFloor { get; set; }
    public int? MaxFloor { get; set; }

    public decimal RatePerSqFt { get; set; }
    public decimal? FlatPrice { get; set; }

    /// <summary>Higher wins when two lines both match a unit.</summary>
    public int Specificity { get; set; }
}

/// <summary>
/// A reusable premium or charge definition for a project, from which <see cref="UnitPremium"/>
/// rows are stamped when a unit is priced.
/// </summary>
public class PremiumCharge : BaseEntity
{
    public Guid ProjectId { get; set; }
    public ChargeKind Kind { get; set; }
    public string Label { get; set; } = string.Empty;
    public ChargeBasis Basis { get; set; } = ChargeBasis.Fixed;
    public decimal Rate { get; set; }

    /// <summary>Floor from which a floor-rise premium starts accruing. Usually the first residential floor.</summary>
    public int? AppliesFromFloor { get; set; }

    public bool AppliesToCornerOnly { get; set; }
    public bool AppliesToParkFacingOnly { get; set; }
    public PropertySubType? AppliesToSubType { get; set; }

    public bool IsTaxable { get; set; }
    public decimal TaxPercent { get; set; }
    public bool IsPartOfSalePrice { get; set; } = true;
    public bool IsOptional { get; set; }
    public bool IsAutoApplied { get; set; } = true;
    public int SortOrder { get; set; }
}

/// <summary>
/// A construction stage that both the payment plan and the progress billing refer to, so
/// "on completion of grey structure" means one date everywhere in the app.
/// </summary>
public class ProjectMilestone : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    /// <summary>Set when the milestone is per tower rather than per project — towers finish at different times.</summary>
    public Guid? ProjectNodeId { get; set; }

    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    public MilestoneStatus Status { get; set; } = MilestoneStatus.NotStarted;
    public DateOnly? PlannedDate { get; set; }
    public DateOnly? ForecastDate { get; set; }
    public DateOnly? ReachedOn { get; set; }

    /// <summary>The certification is the event. Reaching a milestone raises nothing until it is certified.</summary>
    public DateOnly? CertifiedOn { get; set; }
    public Guid? CertifiedByUserId { get; set; }
    public string? CertificateUrl { get; set; }

    /// <summary>Share of the whole build this stage represents, for percentage-of-completion.</summary>
    public decimal WeightPercent { get; set; }

    public decimal ProgressPercent { get; set; }

    /// <summary>Set true once the demands tied to this milestone have been raised, so they raise once.</summary>
    public bool DemandsRaised { get; set; }
}

public class ProjectTeamMember : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    public Guid? UserId { get; set; }
    public Guid? PartyId { get; set; }
    public string? Name { get; set; }

    /// <summary>"ProjectManager", "SalesHead", "QuantitySurveyor", "SiteEngineer", "Architect", "Contractor", "Consultant".</summary>
    public string Role { get; set; } = string.Empty;

    public string? Phone { get; set; }
    public string? Email { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }
}

/// <summary>
/// Budget versus committed versus actual versus forecast, per cost head. The four-column report
/// a developer's financial controller lives in.
/// </summary>
public class ProjectBudgetLine : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }
    public Guid? WbsNodeId { get; set; }

    /// <summary>"Land", "Approvals", "Infrastructure", "Construction", "Marketing", "SalesCommission",
    /// "FinanceCost", "Overhead", "Contingency".</summary>
    public string CostHead { get; set; } = string.Empty;

    public decimal BudgetAmount { get; set; }

    /// <summary>Contracted but not yet spent — awarded subcontracts and open purchase orders.</summary>
    public decimal CommittedAmount { get; set; }

    public decimal ActualAmount { get; set; }

    /// <summary>Today's best estimate of the final number, including what is still to come.</summary>
    public decimal ForecastAmount { get; set; }

    public int SortOrder { get; set; }
}

/// <summary>The uploaded layout drawing an inventory board can be rendered directly on top of.</summary>
public class SitePlan : BaseEntity
{
    public Guid ProjectId { get; set; }
    public Guid? ProjectNodeId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public int ImageWidthPx { get; set; }
    public int ImageHeightPx { get; set; }

    /// <summary>Georeference corners, so plan coordinates and map coordinates can be reconciled.</summary>
    public decimal? NorthWestLatitude { get; set; }
    public decimal? NorthWestLongitude { get; set; }
    public decimal? SouthEastLatitude { get; set; }
    public decimal? SouthEastLongitude { get; set; }

    public bool IsDefault { get; set; }
    public int SortOrder { get; set; }

    public ICollection<SitePlanShape> Shapes { get; set; } = [];
}

/// <summary>
/// One clickable polygon on a site plan, bound to a unit. This is what turns a drawing into the
/// inventory board — click the plot on the plan, hold it, book it.
/// </summary>
public class SitePlanShape : BaseEntity
{
    public Guid SitePlanId { get; set; }
    public SitePlan? SitePlan { get; set; }

    public Guid? UnitId { get; set; }

    /// <summary>Shown on the plan even when no unit is bound yet — "Park", "Mosque", "Commercial".</summary>
    public string? Label { get; set; }

    /// <summary>"polygon", "rect", "circle".</summary>
    public string ShapeType { get; set; } = "polygon";

    /// <summary>Point list in image pixel space: "x1,y1 x2,y2 ...". Rendered as an SVG path.</summary>
    public string Points { get; set; } = string.Empty;

    /// <summary>Where the unit number is drawn, when the polygon's centroid reads badly.</summary>
    public decimal? LabelX { get; set; }
    public decimal? LabelY { get; set; }

    /// <summary>Non-saleable furniture on the plan — roads, parks, amenities. Not clickable.</summary>
    public bool IsDecorative { get; set; }

    public string? FillOverride { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>
/// A right to a plot that does not exist yet.
///
/// Whole schemes sell files years before land is developed; the file is the tradeable instrument
/// and it changes hands many times before a ballot ever assigns it a plot number. Modelling it as
/// a unit with a null plot would make every inventory report lie, so it is its own record.
/// </summary>
public class PlotFile : BaseEntity
{
    public Guid ProjectId { get; set; }
    public string FileNumber { get; set; } = string.Empty;

    /// <summary>The size class being sold — "5 Marla", "1 Kanal", "10 Marla Commercial".</summary>
    public string CategoryCode { get; set; } = string.Empty;

    public decimal NominalAreaSqFt { get; set; }
    public PropertySubType SubType { get; set; } = PropertySubType.ResidentialPlot;

    public PropertyStatus Status { get; set; } = PropertyStatus.Available;
    public decimal Price { get; set; }

    public Guid? CurrentBookingId { get; set; }
    public DateOnly IssuedOn { get; set; }

    // ── After the ballot ─────────────────────────────────────────────────────

    public Guid? BallotId { get; set; }
    public Guid? AllottedUnitId { get; set; }
    public DateOnly? AllottedOn { get; set; }

    /// <summary>Both numbers appear on every document after allotment, so neither is ever lost.</summary>
    public string? AllottedPlotNumber { get; set; }

    /// <summary>A physical file that has been reported lost has a fraud story attached to it.</summary>
    public bool IsDuplicateIssued { get; set; }
    public bool IsCancelled { get; set; }
}
