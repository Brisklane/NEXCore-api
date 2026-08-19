using Distribution.Domain.Enums;
using Nexcore.SharedKernel;

namespace Distribution.Domain.Entities;

/// <summary>
/// A trading partner in the distribution network — distributor, stockist, wholesaler, C&amp;F agent.
///
/// Deliberately not the same record as a CRM account or a Sales customer. A distributor is a
/// *tier*, not a customer: it has children, it holds our stock, it files claims against us, and
/// it reports sales we cannot see any other way. <see cref="CrmAccountId"/> links it to the CRM
/// account when one exists, so campaign history rolls up without forcing the two to be one row.
/// </summary>
public class ChannelPartner : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? TradeName { get; set; }
    public PartnerType PartnerType { get; set; } = PartnerType.Distributor;
    public PartnerStatus Status { get; set; } = PartnerStatus.Lead;
    public ServicingModel ServicingModel { get; set; } = ServicingModel.PreSalesAndDelivery;

    /// <summary>
    /// Parent in the network tree — a sub-distributor's super-stockist. Null at the top.
    /// Roll-ups walk this rather than summing a flat list, so a three-tier network reports right.
    /// </summary>
    public Guid? ParentPartnerId { get; set; }
    public ChannelPartner? ParentPartner { get; set; }

    // ── Identity & contact ───────────────────────────────────────────────────
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

    // ── Statutory ────────────────────────────────────────────────────────────
    public string? TaxRegistrationNumber { get; set; }
    public string? SecondaryTaxNumber { get; set; }

    // ── Commercials ──────────────────────────────────────────────────────────
    public Guid? TerritoryId { get; set; }
    public DistributionTerritory? Territory { get; set; }

    /// <summary>Warehouse this partner is served from. Inventory owns the record.</summary>
    public Guid? ServicingWarehouseId { get; set; }

    /// <summary>Warehouse representing the partner's own godown, when they run Distribution too.</summary>
    public Guid? PartnerWarehouseId { get; set; }

    public Guid? PriceListId { get; set; }
    public Guid? CrmAccountId { get; set; }

    public string CurrencyCode { get; set; } = "USD";
    public decimal CreditLimit { get; set; }
    public int CreditDays { get; set; }
    public CreditEnforcement CreditEnforcement { get; set; } = CreditEnforcement.Warn;

    /// <summary>Held against default. Refundable on clean termination.</summary>
    public decimal SecurityDeposit { get; set; }

    /// <summary>Percentage the partner earns between our price to them and their price onward.</summary>
    public decimal MarginPercent { get; set; }

    /// <summary>Committed monthly offtake, used to flag under-performing appointments.</summary>
    public decimal MinimumMonthlyOfftake { get; set; }

    // ── Appointment ──────────────────────────────────────────────────────────
    public DateTime? AppointedOn { get; set; }
    public DateTime? AgreementExpiresOn { get; set; }
    public DateTime? TerminatedOn { get; set; }
    public string? TerminationReason { get; set; }
    public string? StatusReason { get; set; }

    /// <summary>How this partner's secondary sales reach us. Drives which upload path is offered.</summary>
    public SecondaryCaptureMode SecondaryCaptureMode { get; set; } = SecondaryCaptureMode.Declared;

    /// <summary>Auth user the partner signs into the portal with, when they have one.</summary>
    public Guid? PortalUserId { get; set; }

    public string? Notes { get; set; }

    public ICollection<ChannelPartner> Children { get; set; } = [];
    public ICollection<PartnerContact> Contacts { get; set; } = [];
    public ICollection<PartnerDocument> Documents { get; set; } = [];
    public ICollection<PartnerAuthorisation> Authorisations { get; set; } = [];
}

/// <summary>A named person at a partner, with the role that says who to call about what.</summary>
public class PartnerContact : BaseEntity
{
    public Guid PartnerId { get; set; }
    public ChannelPartner? Partner { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string? Designation { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }

    /// <summary>The one who answers when nobody else does.</summary>
    public bool IsPrimary { get; set; }

    /// <summary>Receives claim and settlement correspondence.</summary>
    public bool HandlesClaims { get; set; }
    public bool HandlesPayments { get; set; }
}

/// <summary>
/// A licence, registration or agreement scan held against a partner.
///
/// <see cref="ExpiresOn"/> is the point of the record. A distributor trading on a lapsed drug
/// licence is a regulatory problem that belongs on a dashboard, not in a filing cabinet.
/// </summary>
public class PartnerDocument : BaseEntity
{
    public Guid PartnerId { get; set; }
    public ChannelPartner? Partner { get; set; }

    public string DocumentType { get; set; } = string.Empty;
    public string? DocumentNumber { get; set; }
    public string? FileUrl { get; set; }
    public DateTime? IssuedOn { get; set; }
    public DateTime? ExpiresOn { get; set; }

    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public Guid? VerifiedByUserId { get; set; }
    public string? VerificationNote { get; set; }
}

/// <summary>
/// Which brands or categories a partner may carry, with dates.
///
/// Ordering an unauthorised SKU is refused at order entry with a readable reason — exclusivity
/// only means something if the system enforces it at the moment someone tries to break it.
/// </summary>
public class PartnerAuthorisation : BaseEntity
{
    public Guid PartnerId { get; set; }
    public ChannelPartner? Partner { get; set; }

    /// <summary>Inventory brand. Either this or <see cref="CategoryId"/> is set, not both.</summary>
    public Guid? BrandId { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? ItemId { get; set; }

    public string? ScopeName { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    /// <summary>True when nobody else may carry it in this partner's territory.</summary>
    public bool IsExclusive { get; set; }
}

/// <summary>The signed appointment terms, kept as a record so a dispute has a document behind it.</summary>
public class PartnerAgreement : BaseEntity
{
    public Guid PartnerId { get; set; }
    public ChannelPartner? Partner { get; set; }

    public string AgreementNumber { get; set; } = string.Empty;
    public DateTime SignedOn { get; set; }
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? FileUrl { get; set; }
    public string? Terms { get; set; }
    public decimal SecurityDeposit { get; set; }
    public decimal TargetTurnover { get; set; }
    public bool IsCurrent { get; set; } = true;
}

/// <summary>
/// A retail outlet — the shop a salesman walks into.
///
/// This is the retail universe, and it is the single most valuable master a distribution business
/// owns. It is not a Sales customer: an outlet carries a channel, a grade, a geofence, a beat, a
/// weekly-off day and an owner's mobile number, and most of them will never appear in the general
/// ledger because their distributor invoices them, not us.
/// </summary>
public class RetailOutlet : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? OwnerName { get; set; }
    public string? OwnerPhone { get; set; }
    public string? DecisionMakerName { get; set; }
    public string? AlternatePhone { get; set; }
    public string? Email { get; set; }

    public OutletChannel Channel { get; set; } = OutletChannel.GeneralTrade;
    public string? SubChannel { get; set; }
    public OutletGrade Grade { get; set; } = OutletGrade.C;
    public OutletStatus Status { get; set; } = OutletStatus.PendingApproval;
    public string? StatusReason { get; set; }

    /// <summary>Banner or chain this outlet belongs to, when it is not independent.</summary>
    public string? ChainName { get; set; }
    public string? StoreFormat { get; set; }
    public int? ShelfCount { get; set; }
    public bool HasRefrigeration { get; set; }

    // ── Location ─────────────────────────────────────────────────────────────
    public string? AddressLine { get; set; }
    public string? Landmark { get; set; }
    public string? Area { get; set; }
    public string? City { get; set; }
    public string? StateName { get; set; }
    public string? PostalCode { get; set; }
    public string? CountryCode { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    /// <summary>
    /// Metres from the stored coordinates within which a check-in counts as "at the outlet".
    /// Zero falls back to the module default; a check-in outside it is allowed but recorded.
    /// </summary>
    public int GeofenceRadiusMetres { get; set; }

    public Guid? GeoNodeId { get; set; }
    public GeoNode? GeoNode { get; set; }

    // ── Commercials ──────────────────────────────────────────────────────────
    public Guid? PartnerId { get; set; }
    public ChannelPartner? Partner { get; set; }

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

    // ── Operating pattern ────────────────────────────────────────────────────
    public TimeSpan? OpensAt { get; set; }
    public TimeSpan? ClosesAt { get; set; }

    /// <summary>0 = Sunday … 6 = Saturday. Null when the outlet never closes for a weekly off.</summary>
    public int? WeeklyOffDay { get; set; }
    public TimeSpan? PreferredDeliveryFrom { get; set; }
    public TimeSpan? PreferredDeliveryTo { get; set; }

    // ── Rolling facts, maintained by the services rather than recomputed on read ──
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

    /// <summary>Latest perfect-store score, 0–100, so the list can be sorted by execution quality.</summary>
    public decimal PerfectStoreScore { get; set; }

    public string? PhotoUrl { get; set; }
    public string? Notes { get; set; }

    /// <summary>Created in the field and not yet cleared by an approver. Cannot be invoiced.</summary>
    public bool IsApproved { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedByUserId { get; set; }

    public ICollection<RouteOutlet> RouteLinks { get; set; } = [];
    public ICollection<OutletAsset> Assets { get; set; } = [];
    public ICollection<OutletContact> Contacts { get; set; } = [];
}

/// <summary>An extra named contact at an outlet — the manager who is there when the owner is not.</summary>
public class OutletContact : BaseEntity
{
    public Guid OutletId { get; set; }
    public RetailOutlet? Outlet { get; set; }

    public string FullName { get; set; } = string.Empty;
    public string? Designation { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsPrimary { get; set; }
}

/// <summary>
/// Company equipment placed at an outlet — a cooler, a rack, a signboard.
///
/// These are assets on our books sitting in someone else's shop, which is why every visit can
/// verify one and why <see cref="LastVerifiedAt"/> going stale is itself a finding.
/// </summary>
public class OutletAsset : BaseEntity
{
    public Guid OutletId { get; set; }
    public RetailOutlet? Outlet { get; set; }

    public OutletAssetKind Kind { get; set; }
    public string AssetTag { get; set; } = string.Empty;
    public string? SerialNumber { get; set; }
    public string? Model { get; set; }
    public string? Manufacturer { get; set; }

    public DateTime PlacedOn { get; set; }
    public DateTime? RetrievedOn { get; set; }
    public decimal AssetValue { get; set; }
    public decimal DepositTaken { get; set; }

    public AssetCondition Condition { get; set; } = AssetCondition.Working;
    public DateTime? LastVerifiedAt { get; set; }
    public DateTime? ServiceDueOn { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Note { get; set; }
}

/// <summary>A timestamped, geo-stamped photo of an outlet — shelf, storefront, display, POSM.</summary>
public class OutletPhoto : BaseEntity
{
    public Guid OutletId { get; set; }
    public Guid? VisitId { get; set; }

    public string ImageUrl { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public string? Tag { get; set; }
    public DateTime CapturedAt { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    /// <summary>Pairs a "before" with its "after" so a display claim shows the change, not a photo.</summary>
    public Guid? PairedPhotoId { get; set; }
}

/// <summary>A free-text note against an outlet, attributed and dated.</summary>
public class OutletNote : BaseEntity
{
    public Guid OutletId { get; set; }
    public Guid? VisitId { get; set; }

    public string Text { get; set; } = string.Empty;
    public string? AuthorName { get; set; }
    public DateTime NotedAt { get; set; }

    /// <summary>Surfaces on the field terminal at the next visit rather than sinking into history.</summary>
    public bool IsPinned { get; set; }
}
