using Nexcore.SharedKernel;
using RealEstate.Domain.Enums;

namespace RealEstate.Domain.Entities;

/// <summary>
/// Module-wide configuration, one row per company.
///
/// Everything here is a policy the whole app reads rather than a preference: which of the four
/// businesses this company runs, how area is spoken about, how a receipt is consumed, how much of
/// a collection is legally locked. Screens reshape around it, so it is loaded once and cached for
/// the session.
/// </summary>
public class RealEstateSettings : BaseEntity
{
    // ── Lines of business ────────────────────────────────────────────────────
    // A line that is off does not register its routes, show its KPIs or run its nightly jobs.

    public bool BrokerageEnabled { get; set; }
    public bool DevelopmentEnabled { get; set; } = true;
    public bool ContractingEnabled { get; set; }
    public bool EstateManagementEnabled { get; set; }

    // ── Measurement & money ──────────────────────────────────────────────────

    /// <summary>What operators read and type. Storage is always square feet.</summary>
    public AreaUnit DisplayAreaUnit { get; set; } = AreaUnit.SquareFeet;

    public string CurrencyCode { get; set; } = AppConstants.DefaultCurrency;

    /// <summary>Currency every cross-project roll-up is expressed in.</summary>
    public string ReportingCurrencyCode { get; set; } = AppConstants.DefaultCurrency;

    /// <summary>Decimal places money is rounded to. Two nearly everywhere, zero for JPY-like currencies.</summary>
    public int CurrencyDecimals { get; set; } = 2;

    // ── Collections policy ───────────────────────────────────────────────────

    public AllocationOrder DefaultAllocationOrder { get; set; } = AllocationOrder.SurchargeFirstThenOldest;

    /// <summary>Days before an instalment falls due that the demand is generated and sent.</summary>
    public int DemandLeadDays { get; set; } = 15;

    /// <summary>Hours a sales hold survives before it auto-releases. The fight-preventing number.</summary>
    public int DefaultHoldHours { get; set; } = 48;

    /// <summary>Longest hold a non-manager may place, in hours. Above this needs approval.</summary>
    public int MaxHoldHoursWithoutApproval { get; set; } = 24;

    /// <summary>Discount percentage a salesperson may give unaided. Above it, the approval ladder runs.</summary>
    public decimal MaxDiscountPercentWithoutApproval { get; set; } = 2m;

    // ── Escrow ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Share of every project collection that must land in the regulated account. 70 in RERA
    /// markets, 0 where no such rule exists. Overridable per project — regulators differ.
    /// </summary>
    public decimal DefaultEscrowPercent { get; set; }

    public bool EscrowEnforced { get; set; }

    // ── Compliance switches ──────────────────────────────────────────────────

    /// <summary>Block completion of a sale or tenancy while KYC is incomplete. A legal requirement in several markets.</summary>
    public bool RequireKycBeforeCompletion { get; set; } = true;

    /// <summary>Keep client money in a segregated ledger with three-way reconciliation.</summary>
    public bool ClientMoneySegregated { get; set; }

    /// <summary>Refuse a transfer while anything is outstanding, rather than warning about it.</summary>
    public bool BlockTransferOnDues { get; set; } = true;

    // ── Communication ────────────────────────────────────────────────────────

    /// <summary>Nothing automated goes out before this hour, local to the office.</summary>
    public int QuietHoursStart { get; set; } = 21;
    public int QuietHoursEnd { get; set; } = 8;

    /// <summary>Most automated messages one customer may receive in a day across all jobs.</summary>
    public int MaxAutomatedMessagesPerDay { get; set; } = 3;

    /// <summary>Minutes a new enquiry may sit unanswered before it is a breach.</summary>
    public int LeadResponseSlaMinutes { get; set; } = 30;

    /// <summary>Days a channel partner's lead registration protects their claim.</summary>
    public int PartnerLeadValidityDays { get; set; } = 90;

    // ── Warranty and delay ────────────────────────────────────────────────────

    /// <summary>Days from receipt within which a tenancy deposit must be protected in a scheme.</summary>
    public int DepositRegistrationDays { get; set; } = 30;

    /// <summary>Default defect liability, in months, opened on every handover.</summary>
    public int DefectLiabilityMonths { get; set; } = 12;

    /// <summary>Structural cover, which runs far longer than the rest and is set separately.</summary>
    public int StructuralWarrantyMonths { get; set; } = 60;

    /// <summary>Annual rate used to compute compensation for possession delayed past the promise.</summary>
    public decimal DelayCompensationAnnualPercent { get; set; }

    // ── Publishing gates ─────────────────────────────────────────────────────
    // What a market demands on an advertisement differs everywhere, so the gate is configured
    // rather than hard-coded — and it is refused before publishing, not fined afterwards.

    /// <summary>Photographs a listing must carry before it may go live.</summary>
    public int MinimumListingPhotos { get; set; } = 3;

    /// <summary>Markets where an energy certificate must exist before the property is advertised.</summary>
    public bool RequireEnergyCertificateToPublish { get; set; }

    /// <summary>Markets where every advertisement must quote a permit or agency reference.</summary>
    public bool RequirePermitNumberToPublish { get; set; }

    public string? BrandPrimaryColor { get; set; }
    public string? LetterheadUrl { get; set; }
    public string? DefaultLanguage { get; set; } = "en";
}

/// <summary>
/// A place the business operates from. Distinct from the platform's branch: a company can run a
/// head office, three sales offices and a site office inside one branch, and each has its own
/// number series, working calendar and set of lines of business.
/// </summary>
public class RealEstateOffice : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public OfficeType OfficeType { get; set; } = OfficeType.Branch;

    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? AddressLine { get; set; }
    public string? City { get; set; }
    public string? PostCode { get; set; }
    public string? CountryCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? TimeZoneId { get; set; }
    public string CurrencyCode { get; set; } = AppConstants.DefaultCurrency;

    /// <summary>Overrides the company default so a Lahore office says marla and a Dubai office says square feet.</summary>
    public AreaUnit? DisplayAreaUnit { get; set; }

    // Lines of business, overriding the company switches for this office only.
    public bool? BrokerageEnabled { get; set; }
    public bool? DevelopmentEnabled { get; set; }
    public bool? ContractingEnabled { get; set; }
    public bool? EstateManagementEnabled { get; set; }

    /// <summary>Bit flags, Sunday = 1. Drives every "due in N working days" calculation.</summary>
    public int WorkingDaysMask { get; set; } = 0b0111110;

    public TimeSpan? OpensAt { get; set; }
    public TimeSpan? ClosesAt { get; set; }

    /// <summary>A franchisee sees only its own book; the franchisor sees the roll-up and charges a royalty.</summary>
    public bool IsFranchise { get; set; }
    public decimal FranchiseRoyaltyPercent { get; set; }

    public Guid? ManagerUserId { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? TaxNumber { get; set; }
}

/// <summary>A non-working day for one office, so notice periods and grace days skip it.</summary>
public class OfficeHoliday : BaseEntity
{
    public Guid OfficeId { get; set; }
    public DateOnly Date { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsHalfDay { get; set; }
}

/// <summary>
/// Geography below the platform's country/state/city, which is where property actually lives:
/// a zone, a sector, a society, a block, a street. Self-referencing so the depth is the market's
/// choice rather than ours.
/// </summary>
public class GeoArea : BaseEntity
{
    public Guid? ParentAreaId { get; set; }
    public GeoArea? Parent { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>What this level is called locally — "Sector", "Phase", "Mouza", "Neighbourhood".</summary>
    public string? LevelLabel { get; set; }

    public int Depth { get; set; }

    /// <summary>Materialised ancestor path, so "everything under Bahria Town" is one index scan.</summary>
    public string? Path { get; set; }

    public Guid? CityId { get; set; }
    public Guid? StateId { get; set; }
    public Guid? CountryId { get; set; }

    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    /// <summary>GeoJSON polygon, used for draw-your-own-area search and territory overlays.</summary>
    public string? BoundaryGeoJson { get; set; }

    /// <summary>Benchmark rate per square foot, entered manually and used for price-trend context.</summary>
    public decimal? AverageRatePerSqFt { get; set; }

    public ICollection<GeoArea> Children { get; set; } = [];
}

/// <summary>An agent's patch: a slice of geography, price band or property type that routes leads.</summary>
public class Territory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? OfficeId { get; set; }

    public PropertyCategory? CategoryFilter { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }

    /// <summary>Drawn on the map. Beats a list of area ids when a patch does not follow boundaries.</summary>
    public string? BoundaryGeoJson { get; set; }

    public ICollection<TerritoryArea> Areas { get; set; } = [];
    public ICollection<TerritoryAssignment> Assignments { get; set; } = [];
}

public class TerritoryArea : BaseEntity
{
    public Guid TerritoryId { get; set; }
    public Territory? Territory { get; set; }
    public Guid GeoAreaId { get; set; }
}

public class TerritoryAssignment : BaseEntity
{
    public Guid TerritoryId { get; set; }
    public Territory? Territory { get; set; }
    public Guid AgentProfileId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }

    /// <summary>Weight in the round-robin. A senior agent on 3 gets three times the flow of one on 1.</summary>
    public int RoutingWeight { get; set; } = 1;

    public bool IsPrimary { get; set; }
}

/// <summary>
/// The selling side of a person. Separate from the HR employee record because a channel partner's
/// staff member and a self-employed negotiator both need one and neither is on our payroll.
/// </summary>
public class AgentProfile : BaseEntity
{
    public Guid? UserId { get; set; }
    public Guid? EmployeeId { get; set; }
    public Guid? OfficeId { get; set; }
    public Guid? SalesTeamId { get; set; }

    public string DisplayName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? PhotoUrl { get; set; }
    public string? JobTitle { get; set; }

    public DateOnly? JoinedOn { get; set; }
    public DateOnly? LeftOn { get; set; }

    /// <summary>Anniversary the commission cap resets on. Often the join date, not 1 January.</summary>
    public DateOnly? CapAnniversary { get; set; }

    public Guid? CommissionPlanId { get; set; }

    /// <summary>Above this many live leads the router skips them, however the weights fall.</summary>
    public int MaxOpenLeads { get; set; } = 60;
    public int MaxLeadsPerDay { get; set; } = 15;

    /// <summary>Position in the round-robin, advanced as leads are handed out.</summary>
    public int RoundRobinPosition { get; set; }

    public bool AcceptsNewLeads { get; set; } = true;
    public bool IsOnLeave { get; set; }
    public Guid? CoveringAgentId { get; set; }

    public string? Languages { get; set; }
    public string? Specialisations { get; set; }

    public ICollection<AgentLicence> Licences { get; set; } = [];
    public ICollection<AgentAvailability> Availability { get; set; } = [];
}

/// <summary>
/// A licence the agent must hold to trade. Expiry matters: in several markets an unlicensed agent
/// cannot lawfully be assigned an instruction, so this can hard-block assignment.
/// </summary>
public class AgentLicence : BaseEntity
{
    public Guid AgentProfileId { get; set; }
    public AgentProfile? Agent { get; set; }

    public string LicenceType { get; set; } = string.Empty;
    public string LicenceNumber { get; set; } = string.Empty;
    public string? IssuingAuthority { get; set; }
    public DateOnly? IssuedOn { get; set; }
    public DateOnly? ExpiresOn { get; set; }
    public string? DocumentUrl { get; set; }

    /// <summary>When true, an expired licence stops new instructions rather than only warning.</summary>
    public bool BlocksAssignmentWhenExpired { get; set; }
    public bool IsVerified { get; set; }
}

/// <summary>Bookable hours, so the viewing diary offers slots the agent can actually attend.</summary>
public class AgentAvailability : BaseEntity
{
    public Guid AgentProfileId { get; set; }
    public AgentProfile? Agent { get; set; }

    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsAvailable { get; set; } = true;

    /// <summary>Set for a one-off override — a specific day off, or a Saturday they will work.</summary>
    public DateOnly? SpecificDate { get; set; }
}

public class SalesTeam : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid? OfficeId { get; set; }
    public Guid? LeaderAgentId { get; set; }

    /// <summary>Override applied to every member unless their own plan says otherwise.</summary>
    public Guid? TeamCommissionPlanId { get; set; }

    /// <summary>Cut the leader takes from each member's deal.</summary>
    public decimal LeaderOverridePercent { get; set; }

    public ICollection<SalesTeamMember> Members { get; set; } = [];
}

public class SalesTeamMember : BaseEntity
{
    public Guid SalesTeamId { get; set; }
    public SalesTeam? Team { get; set; }
    public Guid AgentProfileId { get; set; }
    public DateOnly JoinedOn { get; set; }
    public DateOnly? LeftOn { get; set; }
}

/// <summary>
/// The controlled vocabulary behind every override, waiver, rejection and loss.
///
/// Free text here would make §3.42's reports unanalysable — "why do 40% of our bookings cancel"
/// cannot be answered from a comment box.
/// </summary>
public class ReasonCode : BaseEntity
{
    /// <summary>What it explains: "LeadLoss", "Cancellation", "DiscountApproval", "SurchargeWaiver", "FallThrough".</summary>
    public string Context { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;
    public int SortOrder { get; set; }

    /// <summary>Force the user to add detail as well as pick the code.</summary>
    public bool RequiresNote { get; set; }

    /// <summary>Shipped with the module and not deletable, only deactivatable.</summary>
    public bool IsSystem { get; set; }
}

/// <summary>
/// Who may approve what, banded by value. One row per document type per band, so a 2% discount
/// goes to the sales manager and a 12% discount goes to the director without anybody deciding
/// that by memory.
/// </summary>
public class ApprovalMatrix : BaseEntity
{
    /// <summary>"Discount", "Booking", "Cancellation", "Refund", "SurchargeWaiver", "Transfer", "Ipc", "EscrowWithdrawal".</summary>
    public string DocumentType { get; set; } = string.Empty;

    public Guid? OfficeId { get; set; }
    public Guid? ProjectId { get; set; }

    public decimal MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }

    /// <summary>Order in the ladder. Every level must approve, lowest first.</summary>
    public int Level { get; set; } = 1;

    public Guid? ApproverRoleId { get; set; }
    public Guid? ApproverUserId { get; set; }

    /// <summary>Below this, nothing is asked and the request is stamped auto-approved.</summary>
    public bool AutoApproveBelowMin { get; set; }

    /// <summary>Hours before the request escalates to the next level unanswered.</summary>
    public int? EscalateAfterHours { get; set; }
}

/// <summary>One pending or settled approval, whatever it is about.</summary>
public class ApprovalRequest : BaseEntity
{
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>Id of the booking, cancellation, IPC or withdrawal being approved.</summary>
    public Guid EntityId { get; set; }

    public string? EntityReference { get; set; }
    public string? Summary { get; set; }
    public decimal Amount { get; set; }

    public int CurrentLevel { get; set; } = 1;
    public int RequiredLevels { get; set; } = 1;
    public ApprovalOutcome Outcome { get; set; } = ApprovalOutcome.Pending;

    public Guid RequestedByUserId { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DecidedAt { get; set; }
    public DateTime? EscalatesAt { get; set; }

    public Guid? ReasonCodeId { get; set; }
    public string? RequestNote { get; set; }

    public ICollection<ApprovalDecisionLog> Decisions { get; set; } = [];
}

public class ApprovalDecisionLog : BaseEntity
{
    public Guid ApprovalRequestId { get; set; }
    public ApprovalRequest? Request { get; set; }

    public int Level { get; set; }
    public ApprovalOutcome Outcome { get; set; }
    public Guid DecidedByUserId { get; set; }
    public DateTime DecidedAt { get; set; } = DateTime.UtcNow;
    public string? Comment { get; set; }

    /// <summary>Set when someone decided on another approver's behalf while they were away.</summary>
    public Guid? OnBehalfOfUserId { get; set; }
}

/// <summary>
/// A saved list configuration — filters, columns, sort — so an operator's daily view survives a
/// refresh and can be shared with the team.
/// </summary>
public class SavedView : BaseEntity
{
    public string ScreenKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid OwnerUserId { get; set; }
    public bool IsShared { get; set; }
    public bool IsDefault { get; set; }

    /// <summary>JSON blob of filter state. Opaque to the server on purpose — the screen owns its shape.</summary>
    public string ConfigJson { get; set; } = "{}";
}
