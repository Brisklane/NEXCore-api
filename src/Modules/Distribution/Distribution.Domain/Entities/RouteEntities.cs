using Distribution.Domain.Enums;
using Nexcore.SharedKernel;

namespace Distribution.Domain.Entities;

/// <summary>
/// One node of the geography tree: Country → Region → Zone → State → City → Area → Locality.
///
/// Depth is deliberately not fixed. Every market slices itself differently, and a hard-coded
/// four-level hierarchy is the reason most systems end up with "Zone" holding a city name.
/// </summary>
public class GeoNode : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Null at the root. The tree is walked for roll-ups.</summary>
    public Guid? ParentId { get; set; }
    public GeoNode? Parent { get; set; }

    /// <summary>Label for this level ("Region", "Zone", "City"), set by the tenant.</summary>
    public string LevelName { get; set; } = string.Empty;

    /// <summary>0 at the root, incrementing downward. Kept denormalised so trees render without recursion.</summary>
    public int Depth { get; set; }

    /// <summary>Materialised path of ancestor ids, so a subtree query is one indexed LIKE.</summary>
    public string? Path { get; set; }

    public Guid? ManagerUserId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    public ICollection<GeoNode> Children { get; set; } = [];
}

/// <summary>
/// A sales territory: the geography a person and a partner are jointly accountable for.
///
/// Separate from <see cref="GeoNode"/> because geography is a fact and a territory is a decision —
/// two reps can split one city, and one rep can hold three districts.
/// </summary>
public class DistributionTerritory : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public Guid? ParentId { get; set; }
    public DistributionTerritory? Parent { get; set; }

    public Guid? GeoNodeId { get; set; }
    public GeoNode? GeoNode { get; set; }

    /// <summary>The person accountable for the number, not necessarily the one who visits.</summary>
    public Guid? ManagerFieldRepId { get; set; }
    public Guid? DefaultPartnerId { get; set; }
    public Guid? DefaultWarehouseId { get; set; }

    public string CurrencyCode { get; set; } = "USD";

    public ICollection<DistributionTerritory> Children { get; set; } = [];
    public ICollection<SalesRoute> Routes { get; set; } = [];
}

/// <summary>
/// A route (beat) — the ordered list of outlets one person covers in one working day.
///
/// The route is the unit of work in distribution the way the table is in a restaurant. Almost
/// every number worth having (coverage, strike rate, cost to serve, settlement) is computed
/// per route per day, which is why the ordering and the frequency live here rather than being
/// inferred from wherever the salesman happened to go.
/// </summary>
public class SalesRoute : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public RouteKind Kind { get; set; } = RouteKind.PreSales;
    public VisitFrequency Frequency { get; set; } = VisitFrequency.Weekly;

    public Guid TerritoryId { get; set; }
    public DistributionTerritory? Territory { get; set; }

    public Guid? PartnerId { get; set; }
    public ChannelPartner? Partner { get; set; }

    public Guid? FieldRepId { get; set; }
    public FieldRep? FieldRep { get; set; }

    public Guid? VehicleId { get; set; }
    public Guid? VanUnitId { get; set; }
    public Guid? WarehouseId { get; set; }

    /// <summary>
    /// Weekday mask, "0"–"6" comma separated (0 = Sunday). Which days of the week this route runs.
    /// </summary>
    public string? ActiveDays { get; set; }

    /// <summary>
    /// Week-of-month mask for fortnightly and custom cycles, "1,3" meaning weeks one and three.
    /// Empty means every week.
    /// </summary>
    public string? ActiveWeeks { get; set; }

    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }

    // ── Coverage expectations ────────────────────────────────────────────────
    public int TargetCallsPerDay { get; set; }
    public int MinimumProductiveCalls { get; set; }

    /// <summary>Total planned kilometres, captured so cost-to-serve is not a guess.</summary>
    public decimal PlannedDistanceKm { get; set; }
    public int PlannedDurationMinutes { get; set; }

    /// <summary>Rolling facts maintained by the visit service.</summary>
    public int OutletCount { get; set; }
    public DateTime? LastRunAt { get; set; }

    public ICollection<RouteOutlet> Outlets { get; set; } = [];
    public ICollection<RouteAssignment> Assignments { get; set; } = [];
}

/// <summary>
/// An outlet's place in a route, with its stop number.
///
/// An outlet can sit on more than one route — a pre-sales beat that takes the order and a
/// delivery beat that brings it — so this is a real join row, not a foreign key on the outlet.
/// </summary>
public class RouteOutlet : BaseEntity
{
    public Guid RouteId { get; set; }
    public SalesRoute? Route { get; set; }

    public Guid OutletId { get; set; }
    public RetailOutlet? Outlet { get; set; }

    /// <summary>Position in the day. Drag-to-reorder rewrites this.</summary>
    public int StopSequence { get; set; }

    /// <summary>Expected minutes in store, feeding a realistic day plan.</summary>
    public int ServiceMinutes { get; set; } = 10;

    /// <summary>Distance from the previous stop, captured for cost-to-serve and for a future solver.</summary>
    public decimal DistanceFromPreviousKm { get; set; }

    /// <summary>Overrides the route frequency for this one outlet — an A-grade shop on a C-grade beat.</summary>
    public VisitFrequency? FrequencyOverride { get; set; }

    /// <summary>Must be visited every cycle; a missed one is an exception, not a statistic.</summary>
    public bool IsMustVisit { get; set; }
}

/// <summary>
/// Who owned a route, and when.
///
/// Kept as history rather than overwritten so last quarter's performance still attributes to the
/// person who actually did it after a beat changes hands.
/// </summary>
public class RouteAssignment : BaseEntity
{
    public Guid RouteId { get; set; }
    public SalesRoute? Route { get; set; }

    public Guid FieldRepId { get; set; }
    public FieldRep? FieldRep { get; set; }

    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? Reason { get; set; }

    /// <summary>A stand-in covering leave, rather than a permanent handover.</summary>
    public bool IsTemporary { get; set; }
}

/// <summary>
/// A month of planned work for one field rep — the Permanent Journey Plan.
///
/// Generated from route frequency, then edited. Holding it as a real record (rather than deriving
/// it every time) is what makes plan-vs-actual answerable, because the plan is what it was on the
/// first of the month, not what the routes say today.
/// </summary>
public class JourneyPlan : BaseEntity
{
    public Guid FieldRepId { get; set; }
    public FieldRep? FieldRep { get; set; }

    public Guid? TerritoryId { get; set; }

    /// <summary>First day of the planned month.</summary>
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }

    public string? Name { get; set; }
    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
    public Guid? PublishedByUserId { get; set; }

    // ── Rolling plan-vs-actual, maintained as days close ─────────────────────
    public int PlannedCalls { get; set; }
    public int ActualCalls { get; set; }
    public int ProductiveCalls { get; set; }
    public int UnplannedCalls { get; set; }
    public int MissedCalls { get; set; }

    public ICollection<JourneyPlanDay> Days { get; set; } = [];
}

/// <summary>One working day inside a journey plan: this rep runs this route on this date.</summary>
public class JourneyPlanDay : BaseEntity
{
    public Guid JourneyPlanId { get; set; }
    public JourneyPlan? JourneyPlan { get; set; }

    public DateTime PlanDate { get; set; }

    public Guid? RouteId { get; set; }
    public SalesRoute? Route { get; set; }

    public JourneyPlanDayStatus Status { get; set; } = JourneyPlanDayStatus.Planned;

    /// <summary>Set when someone else ran the beat — leave cover, resignation, a market emergency.</summary>
    public Guid? ReassignedToFieldRepId { get; set; }
    public string? SkipReason { get; set; }

    public int PlannedCalls { get; set; }
    public int ActualCalls { get; set; }
    public int ProductiveCalls { get; set; }

    /// <summary>The day this plan actually produced, once the rep starts work.</summary>
    public Guid? FieldDayId { get; set; }
}
