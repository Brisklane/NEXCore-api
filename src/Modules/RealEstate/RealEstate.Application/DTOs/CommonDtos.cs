using RealEstate.Domain.Enums;

namespace RealEstate.Application.DTOs;

// =====================================================================================
// Shapes shared by every area: paging, pickers, the dashboard, and the small records the
// UI needs everywhere.
//
// DTOs are deliberately flat and deliberately not the entities. A screen needs a unit's
// project name, block name, hold state and holder in one row; the entity has four Guids.
// Sending the entity would make the client do four more round trips or the server ship a
// graph nobody asked for.
// =====================================================================================

/// <summary>Standard list request. Every list screen in the module posts this shape.</summary>
public class ListQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;

    /// <summary>Free text. Each service decides which columns it searches, and says so.</summary>
    public string? Search { get; set; }

    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }

    public Guid? OfficeId { get; set; }
    public Guid? ProjectId { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }

    /// <summary>Include soft-deleted rows. Only honoured for roles that may see them.</summary>
    public bool IncludeInactive { get; set; }
}

/// <summary>A minimal id-and-label pair for dropdowns, so a picker never pulls a full record.</summary>
public class LookupDto
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string? SubLabel { get; set; }
    public string? Code { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>A money amount with the currency it is expressed in. Never a bare decimal on the wire.</summary>
public class MoneyDto
{
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
}

/// <summary>
/// An area, sent in both the canonical unit and the operator's own.
///
/// The UI must never do this conversion: a Lahore office types "10 marla" and a Dubai office types
/// "2,722 sq ft" for the same plot, and the arithmetic has to be identical on both.
/// </summary>
public class AreaDto
{
    public decimal SquareFeet { get; set; }
    public decimal DisplayValue { get; set; }
    public AreaUnit DisplayUnit { get; set; } = AreaUnit.SquareFeet;
    public string DisplayText { get; set; } = string.Empty;
}

public class AddressDto
{
    public string? Line1 { get; set; }
    public string? Line2 { get; set; }
    public string? Street { get; set; }
    public string? Area { get; set; }
    public string? City { get; set; }
    public string? PostCode { get; set; }
    public string? CountryCode { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string OneLine { get; set; } = string.Empty;
}

/// <summary>One entry on a 360 screen's timeline, whatever produced it.</summary>
public class TimelineEntryDto
{
    public Guid Id { get; set; }
    public DateTime OccurredAt { get; set; }

    /// <summary>"activity", "status", "money", "document", "task", "note".</summary>
    public string Kind { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public string? Icon { get; set; }
    public string? Tone { get; set; }
    public string? ActorName { get; set; }
    public decimal? Amount { get; set; }
    public string? Route { get; set; }
}

/// <summary>A row on the dashboard's "needs attention" list, ranked by damage rather than by count.</summary>
public class AttentionItemDto
{
    public string Key { get; set; } = string.Empty;
    public AlertSeverity Severity { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public string? Icon { get; set; }
    public string? Route { get; set; }
    public int Count { get; set; }
    public decimal? Amount { get; set; }
    public int Rank { get; set; }
}

/// <summary>A single point on a trend strip.</summary>
public class TrendPointDto
{
    public string Label { get; set; } = string.Empty;
    public DateOnly? Date { get; set; }
    public decimal Value { get; set; }
    public decimal? SecondaryValue { get; set; }
}

/// <summary>A labelled slice for a breakdown chart or a legend.</summary>
public class BreakdownSliceDto
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public decimal Percent { get; set; }
    public int Count { get; set; }
    public string? Tone { get; set; }
}

/// <summary>
/// The Real Estate home screen.
///
/// Split the way the Fitness dashboard is: what needs doing in the next five minutes, then how the
/// business is going. Every figure is arithmetic over live data rather than a stored counter, so a
/// dashboard can never disagree with the report it links to.
/// </summary>
public class RealEstateDashboardDto
{
    public DateTime GeneratedAt { get; set; }
    public string? OfficeName { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    /// <summary>Which lines of business are switched on. The dashboard reshapes around it.</summary>
    public bool BrokerageEnabled { get; set; }
    public bool DevelopmentEnabled { get; set; }
    public bool ContractingEnabled { get; set; }
    public bool EstateManagementEnabled { get; set; }

    public List<AttentionItemDto> NeedsAttention { get; set; } = [];

    // ── Right now ────────────────────────────────────────────────────────────

    public int HoldsExpiringToday { get; set; }
    public int ViewingsToday { get; set; }
    public int SiteVisitsToday { get; set; }
    public int UnansweredLeads { get; set; }
    public int LeadsBreachingSla { get; set; }
    public int DemandsDueToday { get; set; }
    public int ReceiptsToday { get; set; }
    public decimal CollectedToday { get; set; }
    public int OpenWorkOrders { get; set; }
    public int VisitorsInsideNow { get; set; }

    // ── Development ──────────────────────────────────────────────────────────

    public int TotalUnits { get; set; }
    public int UnitsAvailable { get; set; }
    public int UnitsHeld { get; set; }
    public int UnitsBooked { get; set; }
    public int UnitsSold { get; set; }
    public decimal AbsorptionPercent { get; set; }
    public decimal BookingValueThisMonth { get; set; }
    public decimal BookingValueLastMonth { get; set; }
    public int BookingsThisMonth { get; set; }
    public int CancellationsThisMonth { get; set; }

    // ── Money ────────────────────────────────────────────────────────────────

    public decimal DemandedThisMonth { get; set; }
    public decimal CollectedThisMonth { get; set; }
    public decimal CollectionEfficiencyPercent { get; set; }
    public decimal TotalOutstanding { get; set; }
    public decimal OverdueAmount { get; set; }
    public int DefaulterCount { get; set; }
    public decimal SurchargeAccrued { get; set; }
    public decimal EscrowBalance { get; set; }

    // ── Brokerage ────────────────────────────────────────────────────────────

    public int LiveListings { get; set; }
    public int OpenEnquiries { get; set; }
    public int DealsInProgress { get; set; }
    public decimal PipelineValue { get; set; }
    public decimal CommissionEarnedThisMonth { get; set; }
    public decimal AverageSpeedToLeadMinutes { get; set; }
    public decimal FallThroughRatePercent { get; set; }

    // ── Estate management ────────────────────────────────────────────────────

    public int ActiveTenancies { get; set; }
    public decimal MonthlyRentRoll { get; set; }
    public decimal RentArrears { get; set; }
    public int VoidUnits { get; set; }
    public decimal OccupancyPercent { get; set; }
    public int TenanciesExpiringIn90Days { get; set; }
    public int ComplianceCertificatesExpiring { get; set; }
    public int OpenComplaints { get; set; }
    public int ComplaintsBreachingSla { get; set; }

    // ── Construction ─────────────────────────────────────────────────────────

    public int ActiveConstructionProjects { get; set; }
    public decimal AveragePhysicalProgressPercent { get; set; }
    public decimal CertifiedValueThisMonth { get; set; }
    public decimal RetentionHeld { get; set; }
    public int OpenVariations { get; set; }
    public int ProjectsAtRiskOfDelay { get; set; }

    // ── Trends ───────────────────────────────────────────────────────────────

    public List<TrendPointDto> CollectionTrend { get; set; } = [];
    public List<TrendPointDto> BookingTrend { get; set; } = [];
    public List<BreakdownSliceDto> InventoryByStatus { get; set; } = [];
    public List<BreakdownSliceDto> LeadsBySource { get; set; } = [];
}

/// <summary>Which lines of business are on, and the resulting shape of the app.</summary>
public class LinesOfBusinessDto
{
    public bool Brokerage { get; set; }
    public bool Development { get; set; }
    public bool Contracting { get; set; }
    public bool EstateManagement { get; set; }

    /// <summary>Where the sidebar sends a user who has just signed in.</summary>
    public string DefaultRoute { get; set; } = "/realestate/dashboard";
}

/// <summary>The settings block the UI reads once at start-up and caches for the session.</summary>
public class RealEstateSettingsDto
{
    public Guid Id { get; set; }
    public LinesOfBusinessDto LinesOfBusiness { get; set; } = new();

    public AreaUnit DisplayAreaUnit { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public string ReportingCurrencyCode { get; set; } = "USD";
    public int CurrencyDecimals { get; set; } = 2;

    public AllocationOrder DefaultAllocationOrder { get; set; }
    public int DemandLeadDays { get; set; }
    public int DefaultHoldHours { get; set; }
    public int MaxHoldHoursWithoutApproval { get; set; }
    public decimal MaxDiscountPercentWithoutApproval { get; set; }

    public decimal DefaultEscrowPercent { get; set; }
    public bool EscrowEnforced { get; set; }

    public bool RequireKycBeforeCompletion { get; set; }
    public bool ClientMoneySegregated { get; set; }
    public bool BlockTransferOnDues { get; set; }

    public int LeadResponseSlaMinutes { get; set; }
    public int PartnerLeadValidityDays { get; set; }
    public string DefaultLanguage { get; set; } = "en";
    public string? BrandPrimaryColor { get; set; }
}

/// <summary>An approval waiting on somebody, whatever it is about.</summary>
public class ApprovalRequestDto
{
    public Guid Id { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string? EntityReference { get; set; }
    public string? Summary { get; set; }
    public decimal Amount { get; set; }
    public int CurrentLevel { get; set; }
    public int RequiredLevels { get; set; }
    public ApprovalOutcome Outcome { get; set; }
    public string RequestedByName { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public DateTime? EscalatesAt { get; set; }
    public bool IsOverdue { get; set; }
    public string? ReasonLabel { get; set; }
    public string? RequestNote { get; set; }
    public string? Route { get; set; }
}

public class ApprovalDecisionDto
{
    public Guid ApprovalRequestId { get; set; }
    public ApprovalOutcome Outcome { get; set; }
    public string? Comment { get; set; }
    public Guid? OnBehalfOfUserId { get; set; }
}

/// <summary>A reason code as the UI needs it — the controlled list behind every override.</summary>
public class ReasonCodeDto
{
    public Guid Id { get; set; }
    public string Context { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool RequiresNote { get; set; }
    public bool IsSystem { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>A document produced by the template engine, ready to print, send or archive.</summary>
public class GeneratedDocumentDto
{
    public Guid Id { get; set; }
    public string DocumentNumber { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Url { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string? GeneratedByName { get; set; }
    public string LanguageCode { get; set; } = "en";
    public string? VerificationCode { get; set; }
    public bool IsSent { get; set; }
    public bool IsSigned { get; set; }
    public bool IsSuperseded { get; set; }
    public int? PageCount { get; set; }
}

/// <summary>A checklist item on any gated process, with why it blocks.</summary>
public class ChecklistItemDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsSatisfied { get; set; }
    public bool IsMandatory { get; set; }
    public DateOnly? SatisfiedOn { get; set; }
    public string? Note { get; set; }
    public string? Url { get; set; }
    public DocumentState? State { get; set; }
    public bool WasOverridden { get; set; }
    public string? OverrideReason { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>The result of a gated action the server refused, with what to do about it.</summary>
public class GateResultDto
{
    public bool Passed { get; set; }

    /// <summary>Written for the person holding the phone, not for a log file.</summary>
    public string? Message { get; set; }

    public List<ChecklistItemDto> Failures { get; set; } = [];

    /// <summary>True when an authority could override this. False when nothing can.</summary>
    public bool CanOverride { get; set; }

    public string? OverrideRole { get; set; }
}

/// <summary>A saved list view — filters, columns and sort — shared or private.</summary>
public class SavedViewDto
{
    public Guid Id { get; set; }
    public string ScreenKey { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsShared { get; set; }
    public bool IsDefault { get; set; }
    public bool IsMine { get; set; }
    public string ConfigJson { get; set; } = "{}";
}
