using Distribution.Domain.Enums;

namespace Distribution.Application.DTOs;

// ── Channel partners ─────────────────────────────────────────────────────────

public class PartnerDto
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TradeName { get; set; }
    public PartnerType PartnerType { get; set; }
    public PartnerStatus Status { get; set; }
    public ServicingModel ServicingModel { get; set; }
    public Guid? ParentPartnerId { get; set; }
    public string? ParentPartnerName { get; set; }

    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? AlternatePhone { get; set; }
    public string? Email { get; set; }
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? StateName { get; set; }
    public string? PostalCode { get; set; }
    public string? CountryCode { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public string? TaxRegistrationNumber { get; set; }
    public string? SecondaryTaxNumber { get; set; }

    public Guid? TerritoryId { get; set; }
    public string? TerritoryName { get; set; }
    public Guid? ServicingWarehouseId { get; set; }
    public Guid? PartnerWarehouseId { get; set; }
    public Guid? PriceListId { get; set; }
    public Guid? CrmAccountId { get; set; }

    public string CurrencyCode { get; set; } = "USD";
    public decimal CreditLimit { get; set; }
    public int CreditDays { get; set; }
    public CreditEnforcement CreditEnforcement { get; set; }
    public decimal SecurityDeposit { get; set; }
    public decimal MarginPercent { get; set; }
    public decimal MinimumMonthlyOfftake { get; set; }

    public DateTime? AppointedOn { get; set; }
    public DateTime? AgreementExpiresOn { get; set; }
    public DateTime? TerminatedOn { get; set; }
    public string? TerminationReason { get; set; }
    public string? StatusReason { get; set; }
    public SecondaryCaptureMode SecondaryCaptureMode { get; set; }
    public Guid? PortalUserId { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; }

    public List<PartnerContactDto> Contacts { get; set; } = [];
    public List<PartnerDocumentDto> Documents { get; set; } = [];
    public List<PartnerAuthorisationDto> Authorisations { get; set; } = [];

    // Roll-ups the list view shows without a second call.
    public int OutletCount { get; set; }
    public int ChildPartnerCount { get; set; }
    public decimal OutstandingAmount { get; set; }
    public decimal OverdueAmount { get; set; }
    public decimal MonthToDateSales { get; set; }
    public int OpenClaimCount { get; set; }

    /// <summary>Licences and registrations expiring inside the alert window.</summary>
    public int ExpiringDocumentCount { get; set; }
}

public class SavePartnerDto
{
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TradeName { get; set; }
    public PartnerType PartnerType { get; set; } = PartnerType.Distributor;
    public PartnerStatus Status { get; set; } = PartnerStatus.Lead;
    public ServicingModel ServicingModel { get; set; } = ServicingModel.PreSalesAndDelivery;
    public Guid? ParentPartnerId { get; set; }

    public string? ContactPerson { get; set; }
    public string? Phone { get; set; }
    public string? AlternatePhone { get; set; }
    public string? Email { get; set; }
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? StateName { get; set; }
    public string? PostalCode { get; set; }
    public string? CountryCode { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public string? TaxRegistrationNumber { get; set; }
    public string? SecondaryTaxNumber { get; set; }

    public Guid? TerritoryId { get; set; }
    public Guid? ServicingWarehouseId { get; set; }
    public Guid? PartnerWarehouseId { get; set; }
    public Guid? PriceListId { get; set; }
    public Guid? CrmAccountId { get; set; }

    public string CurrencyCode { get; set; } = "USD";
    public decimal CreditLimit { get; set; }
    public int CreditDays { get; set; }
    public CreditEnforcement CreditEnforcement { get; set; } = CreditEnforcement.Warn;
    public decimal SecurityDeposit { get; set; }
    public decimal MarginPercent { get; set; }
    public decimal MinimumMonthlyOfftake { get; set; }

    public DateTime? AppointedOn { get; set; }
    public DateTime? AgreementExpiresOn { get; set; }
    public SecondaryCaptureMode SecondaryCaptureMode { get; set; } = SecondaryCaptureMode.Declared;
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public List<PartnerContactDto> Contacts { get; set; } = [];
    public List<PartnerAuthorisationDto> Authorisations { get; set; } = [];
}

public class PartnerContactDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Designation { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsPrimary { get; set; }
    public bool HandlesClaims { get; set; }
    public bool HandlesPayments { get; set; }
}

public class PartnerDocumentDto
{
    public Guid Id { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string? DocumentNumber { get; set; }
    public string? FileUrl { get; set; }
    public DateTime? IssuedOn { get; set; }
    public DateTime? ExpiresOn { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? VerificationNote { get; set; }

    /// <summary>Negative once expired. Drives the colour of the chip.</summary>
    public int? DaysToExpiry { get; set; }
}

public class PartnerAuthorisationDto
{
    public Guid Id { get; set; }
    public Guid? BrandId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? ItemId { get; set; }
    public string? ScopeName { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsExclusive { get; set; }
}

public class ChangePartnerStatusDto
{
    public PartnerStatus Status { get; set; }
    public string? Reason { get; set; }
}

/// <summary>One node of the network tree, for the hierarchy view.</summary>
public class PartnerTreeNodeDto
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public PartnerType PartnerType { get; set; }
    public PartnerStatus Status { get; set; }
    public int OutletCount { get; set; }
    public decimal MonthToDateSales { get; set; }
    public decimal OutstandingAmount { get; set; }
    public List<PartnerTreeNodeDto> Children { get; set; } = [];
}

// ── Outlets ──────────────────────────────────────────────────────────────────

public class OutletDto
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? OwnerName { get; set; }
    public string? OwnerPhone { get; set; }
    public string? DecisionMakerName { get; set; }
    public string? AlternatePhone { get; set; }
    public string? Email { get; set; }

    public OutletChannel Channel { get; set; }
    public string? SubChannel { get; set; }
    public OutletGrade Grade { get; set; }
    public OutletStatus Status { get; set; }
    public string? StatusReason { get; set; }
    public string? ChainName { get; set; }
    public string? StoreFormat { get; set; }
    public int? ShelfCount { get; set; }
    public bool HasRefrigeration { get; set; }

    public string? AddressLine { get; set; }
    public string? Landmark { get; set; }
    public string? Area { get; set; }
    public string? City { get; set; }
    public string? StateName { get; set; }
    public string? PostalCode { get; set; }
    public string? CountryCode { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int GeofenceRadiusMetres { get; set; }
    public Guid? GeoNodeId { get; set; }

    public Guid? PartnerId { get; set; }
    public string? PartnerName { get; set; }
    public Guid? TerritoryId { get; set; }
    public string? TerritoryName { get; set; }
    public Guid? PriceListId { get; set; }
    public Guid? SchemeGroupId { get; set; }
    public Guid? CrmContactId { get; set; }

    public string CurrencyCode { get; set; } = "USD";
    public decimal CreditLimit { get; set; }
    public int CreditDays { get; set; }
    public CreditEnforcement CreditEnforcement { get; set; }
    public PaymentTender PreferredTender { get; set; }
    public string? TaxRegistrationNumber { get; set; }
    public string? LicenceNumber { get; set; }
    public DateTime? LicenceExpiresOn { get; set; }

    public TimeSpan? OpensAt { get; set; }
    public TimeSpan? ClosesAt { get; set; }
    public int? WeeklyOffDay { get; set; }
    public TimeSpan? PreferredDeliveryFrom { get; set; }
    public TimeSpan? PreferredDeliveryTo { get; set; }

    public DateTime? OnboardedAt { get; set; }
    public DateTime? FirstOrderAt { get; set; }
    public DateTime? LastVisitAt { get; set; }
    public DateTime? LastOrderAt { get; set; }
    public DateTime? LastPaymentAt { get; set; }
    public decimal LifetimeSales { get; set; }
    public decimal AverageMonthlyOfftake { get; set; }
    public decimal OutstandingAmount { get; set; }
    public int TotalVisits { get; set; }
    public int ProductiveVisits { get; set; }
    public decimal PerfectStoreScore { get; set; }

    public string? PhotoUrl { get; set; }
    public string? Notes { get; set; }
    public bool IsApproved { get; set; }
    public bool IsActive { get; set; }

    public List<OutletRouteLinkDto> Routes { get; set; } = [];
    public List<OutletAssetDto> Assets { get; set; } = [];
    public List<OutletContactDto> Contacts { get; set; } = [];

    /// <summary>Days since the last visit. Null when never visited.</summary>
    public int? DaysSinceLastVisit { get; set; }

    /// <summary>Live credit position, so the field terminal can block without a second call.</summary>
    public CreditSnapshotDto? Credit { get; set; }
}

public class SaveOutletDto
{
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? OwnerName { get; set; }
    public string? OwnerPhone { get; set; }
    public string? DecisionMakerName { get; set; }
    public string? AlternatePhone { get; set; }
    public string? Email { get; set; }

    public OutletChannel Channel { get; set; } = OutletChannel.GeneralTrade;
    public string? SubChannel { get; set; }
    public OutletGrade Grade { get; set; } = OutletGrade.C;
    public OutletStatus Status { get; set; } = OutletStatus.Active;
    public string? ChainName { get; set; }
    public string? StoreFormat { get; set; }
    public int? ShelfCount { get; set; }
    public bool HasRefrigeration { get; set; }

    public string? AddressLine { get; set; }
    public string? Landmark { get; set; }
    public string? Area { get; set; }
    public string? City { get; set; }
    public string? StateName { get; set; }
    public string? PostalCode { get; set; }
    public string? CountryCode { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int GeofenceRadiusMetres { get; set; }
    public Guid? GeoNodeId { get; set; }

    public Guid? PartnerId { get; set; }
    public Guid? TerritoryId { get; set; }
    public Guid? PriceListId { get; set; }
    public Guid? SchemeGroupId { get; set; }
    public Guid? CrmContactId { get; set; }

    public string CurrencyCode { get; set; } = "USD";
    public decimal CreditLimit { get; set; }
    public int CreditDays { get; set; }
    public CreditEnforcement CreditEnforcement { get; set; } = CreditEnforcement.Warn;
    public PaymentTender PreferredTender { get; set; } = PaymentTender.Cash;
    public string? TaxRegistrationNumber { get; set; }
    public string? LicenceNumber { get; set; }
    public DateTime? LicenceExpiresOn { get; set; }

    public TimeSpan? OpensAt { get; set; }
    public TimeSpan? ClosesAt { get; set; }
    public int? WeeklyOffDay { get; set; }
    public TimeSpan? PreferredDeliveryFrom { get; set; }
    public TimeSpan? PreferredDeliveryTo { get; set; }

    public string? PhotoUrl { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Route ids to place this outlet on, appended at the end of each beat.</summary>
    public List<Guid> RouteIds { get; set; } = [];
    public List<OutletContactDto> Contacts { get; set; } = [];
}

/// <summary>
/// An outlet created at the door by a rep. Lands as pending approval and cannot be invoiced
/// until someone clears it — the guard against a retail universe full of ghosts.
/// </summary>
public class OnboardOutletDto
{
    public string Name { get; set; } = string.Empty;
    public string? OwnerName { get; set; }
    public string OwnerPhone { get; set; } = string.Empty;
    public OutletChannel Channel { get; set; } = OutletChannel.GeneralTrade;
    public OutletGrade Grade { get; set; } = OutletGrade.C;
    public string? AddressLine { get; set; }
    public string? Landmark { get; set; }
    public string? Area { get; set; }
    public string? City { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? PhotoUrl { get; set; }
    public string? TaxRegistrationNumber { get; set; }
    public Guid? RouteId { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? FieldRepId { get; set; }
    public string? Note { get; set; }

    /// <summary>Proceed even though the duplicate check found a candidate.</summary>
    public bool OverrideDuplicateWarning { get; set; }
    public string? IdempotencyKey { get; set; }
}

/// <summary>
/// A possible duplicate found during onboarding, with the reason it was flagged. Returned rather
/// than silently blocking, because the rep at the door is the one who can actually tell.
/// </summary>
public class DuplicateCandidateDto
{
    public Guid OutletId { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? OwnerPhone { get; set; }
    public string? AddressLine { get; set; }

    /// <summary>"SamePhone", "SameTaxNumber", "NearbyGeo", "SimilarName".</summary>
    public string MatchReason { get; set; } = string.Empty;
    public decimal? DistanceMetres { get; set; }
}

public class OutletContactDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Designation { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsPrimary { get; set; }
}

public class OutletRouteLinkDto
{
    public Guid RouteId { get; set; }
    public string RouteName { get; set; } = string.Empty;
    public RouteKind Kind { get; set; }
    public int StopSequence { get; set; }
    public bool IsMustVisit { get; set; }
    public VisitFrequency Frequency { get; set; }
}

public class OutletAssetDto
{
    public Guid Id { get; set; }
    public Guid OutletId { get; set; }
    public string? OutletName { get; set; }
    public OutletAssetKind Kind { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string? Model { get; set; }
    public string? Manufacturer { get; set; }
    public DateTime PlacedOn { get; set; }
    public DateTime? RetrievedOn { get; set; }
    public decimal AssetValue { get; set; }
    public decimal DepositTaken { get; set; }
    public AssetCondition Condition { get; set; }
    public DateTime? LastVerifiedAt { get; set; }
    public DateTime? ServiceDueOn { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Note { get; set; }

    /// <summary>Days since the last verification. A stale asset is itself a finding.</summary>
    public int? DaysSinceVerified { get; set; }
}

public class OutletPhotoDto
{
    public Guid Id { get; set; }
    public Guid OutletId { get; set; }
    public Guid? VisitId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public string? Tag { get; set; }
    public DateTime CapturedAt { get; set; }
    public Guid? PairedPhotoId { get; set; }
}

public class OutletNoteDto
{
    public Guid Id { get; set; }
    public Guid OutletId { get; set; }
    public Guid? VisitId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string? AuthorName { get; set; }
    public DateTime NotedAt { get; set; }
    public bool IsPinned { get; set; }
}

/// <summary>
/// Everything about one outlet on one screen: who they are, what they owe, what they buy, what
/// they never buy, and what happened on the last few visits.
/// </summary>
public class Outlet360Dto
{
    public OutletDto Outlet { get; set; } = new();
    public CreditSnapshotDto Credit { get; set; } = new();

    public List<VisitSummaryDto> RecentVisits { get; set; } = [];
    public List<OrderSummaryDto> RecentOrders { get; set; } = [];
    public List<CollectionSummaryDto> RecentCollections { get; set; } = [];
    public List<ReturnSummaryDto> RecentReturns { get; set; } = [];
    public List<OutletPhotoDto> Photos { get; set; } = [];
    public List<OutletNoteDto> Notes { get; set; } = [];
    public List<SchemeApplicationDto> SchemeHistory { get; set; } = [];
    public List<MerchandisingAuditSummaryDto> Audits { get; set; } = [];

    /// <summary>What this outlet buys, ranked.</summary>
    public List<OutletItemOfftakeDto> TopItems { get; set; } = [];

    /// <summary>
    /// What its peers buy and it does not — the gap list, and the most directly actionable
    /// thing the rep sees on this screen.
    /// </summary>
    public List<OutletItemOfftakeDto> GapItems { get; set; } = [];

    public decimal MonthToDateSales { get; set; }
    public decimal LastMonthSales { get; set; }
    public decimal YearToDateSales { get; set; }
    public decimal GrowthPercent { get; set; }
}

public class OutletItemOfftakeDto
{
    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? ItemCode { get; set; }
    public string? BrandName { get; set; }
    public decimal Quantity { get; set; }
    public decimal Value { get; set; }
    public DateTime? LastPurchasedAt { get; set; }

    /// <summary>How many comparable outlets buy this, for the gap list.</summary>
    public int PeerBuyerCount { get; set; }
    public decimal PeerAverageQuantity { get; set; }
}

// ── Geography, territories, routes ───────────────────────────────────────────

public class GeoNodeDto
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public string LevelName { get; set; } = string.Empty;
    public int Depth { get; set; }
    public string? Path { get; set; }
    public Guid? ManagerUserId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool IsActive { get; set; }
    public List<GeoNodeDto> Children { get; set; } = [];
    public int OutletCount { get; set; }
}

public class TerritoryDto
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public string? ParentName { get; set; }
    public Guid? GeoNodeId { get; set; }
    public string? GeoNodeName { get; set; }
    public Guid? ManagerFieldRepId { get; set; }
    public string? ManagerName { get; set; }
    public Guid? DefaultPartnerId { get; set; }
    public Guid? DefaultWarehouseId { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public bool IsActive { get; set; }
    public string? Description { get; set; }

    public int RouteCount { get; set; }
    public int OutletCount { get; set; }
    public int PartnerCount { get; set; }
    public decimal MonthToDateSales { get; set; }
    public List<TerritoryDto> Children { get; set; } = [];
}

public class SaveTerritoryDto
{
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public Guid? GeoNodeId { get; set; }
    public Guid? ManagerFieldRepId { get; set; }
    public Guid? DefaultPartnerId { get; set; }
    public Guid? DefaultWarehouseId { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

public class RouteDto
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public RouteKind Kind { get; set; }
    public VisitFrequency Frequency { get; set; }

    public Guid TerritoryId { get; set; }
    public string? TerritoryName { get; set; }
    public Guid? PartnerId { get; set; }
    public string? PartnerName { get; set; }
    public Guid? FieldRepId { get; set; }
    public string? FieldRepName { get; set; }
    public Guid? VehicleId { get; set; }
    public Guid? VanUnitId { get; set; }
    public Guid? WarehouseId { get; set; }

    public string? ActiveDays { get; set; }
    public string? ActiveWeeks { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }

    public int TargetCallsPerDay { get; set; }
    public int MinimumProductiveCalls { get; set; }
    public decimal PlannedDistanceKm { get; set; }
    public int PlannedDurationMinutes { get; set; }
    public int OutletCount { get; set; }
    public DateTime? LastRunAt { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }

    public List<RouteStopDto> Stops { get; set; } = [];

    // Rolling performance, so a route list is a scoreboard rather than a directory.
    public decimal CoveragePercent { get; set; }
    public decimal StrikeRatePercent { get; set; }
    public decimal MonthToDateSales { get; set; }
}

public class SaveRouteDto
{
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public RouteKind Kind { get; set; } = RouteKind.PreSales;
    public VisitFrequency Frequency { get; set; } = VisitFrequency.Weekly;
    public Guid TerritoryId { get; set; }
    public Guid? PartnerId { get; set; }
    public Guid? FieldRepId { get; set; }
    public Guid? VehicleId { get; set; }
    public Guid? VanUnitId { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? ActiveDays { get; set; }
    public string? ActiveWeeks { get; set; }
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }
    public int TargetCallsPerDay { get; set; }
    public int MinimumProductiveCalls { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

public class RouteStopDto
{
    public Guid Id { get; set; }
    public Guid OutletId { get; set; }
    public string OutletName { get; set; } = string.Empty;
    public string? OutletCode { get; set; }
    public OutletChannel Channel { get; set; }
    public OutletGrade Grade { get; set; }
    public OutletStatus Status { get; set; }
    public string? AddressLine { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public int StopSequence { get; set; }
    public int ServiceMinutes { get; set; }
    public decimal DistanceFromPreviousKm { get; set; }
    public VisitFrequency? FrequencyOverride { get; set; }
    public bool IsMustVisit { get; set; }

    public decimal OutstandingAmount { get; set; }
    public DateTime? LastVisitAt { get; set; }
}

/// <summary>Reordering a beat. The whole ordered list is sent, so the result is unambiguous.</summary>
public class ResequenceRouteDto
{
    public Guid RouteId { get; set; }
    public List<Guid> OrderedRouteOutletIds { get; set; } = [];
}

public class AssignRouteDto
{
    public Guid RouteId { get; set; }
    public Guid FieldRepId { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public bool IsTemporary { get; set; }
    public string? Reason { get; set; }
}

public class AddOutletsToRouteDto
{
    public Guid RouteId { get; set; }
    public List<Guid> OutletIds { get; set; } = [];
    public bool MarkMustVisit { get; set; }
}

// ── Journey plan ─────────────────────────────────────────────────────────────

public class JourneyPlanDto
{
    public Guid Id { get; set; }
    public Guid FieldRepId { get; set; }
    public string? FieldRepName { get; set; }
    public Guid? TerritoryId { get; set; }
    public string? TerritoryName { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string? Name { get; set; }
    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }

    public int PlannedCalls { get; set; }
    public int ActualCalls { get; set; }
    public int ProductiveCalls { get; set; }
    public int UnplannedCalls { get; set; }
    public int MissedCalls { get; set; }
    public decimal CoveragePercent { get; set; }
    public decimal StrikeRatePercent { get; set; }

    public List<JourneyPlanDayDto> Days { get; set; } = [];
}

public class JourneyPlanDayDto
{
    public Guid Id { get; set; }
    public DateTime PlanDate { get; set; }
    public Guid? RouteId { get; set; }
    public string? RouteName { get; set; }
    public RouteKind? RouteKind { get; set; }
    public JourneyPlanDayStatus Status { get; set; }
    public Guid? ReassignedToFieldRepId { get; set; }
    public string? ReassignedToName { get; set; }
    public string? SkipReason { get; set; }
    public int PlannedCalls { get; set; }
    public int ActualCalls { get; set; }
    public int ProductiveCalls { get; set; }
    public Guid? FieldDayId { get; set; }
}

/// <summary>Builds next month's plan from route frequency. Existing days are preserved unless forced.</summary>
public class GenerateJourneyPlanDto
{
    public Guid FieldRepId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    /// <summary>Routes to include. Empty means every route currently assigned to the rep.</summary>
    public List<Guid> RouteIds { get; set; } = [];

    /// <summary>Weekday numbers the rep does not work, 0 = Sunday.</summary>
    public List<int> NonWorkingDays { get; set; } = [];

    /// <summary>Dates the rep is on leave or the market is shut.</summary>
    public List<DateTime> HolidayDates { get; set; } = [];

    /// <summary>Discards and rebuilds an existing plan rather than filling the gaps in it.</summary>
    public bool Overwrite { get; set; }
}

public class UpdateJourneyPlanDayDto
{
    public Guid? RouteId { get; set; }
    public JourneyPlanDayStatus? Status { get; set; }
    public Guid? ReassignedToFieldRepId { get; set; }
    public string? SkipReason { get; set; }
}
