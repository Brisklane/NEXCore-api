using Fitness.Domain.Enums;

namespace Fitness.Application.DTOs;

/// <summary>
/// The owner's home screen.
///
/// Split into "right now" and "this month" deliberately. The live block is for acting in the next
/// five minutes — a lead breaching its SLA, a door offline, a class about to run empty. The
/// monthly block is for judging the business. Mixing them produces a screen that is urgent about
/// everything and therefore about nothing.
/// </summary>
public class FitnessDashboardDto
{
    public Guid? ClubId { get; set; }
    public string ClubName { get; set; } = string.Empty;
    public string CurrencyCode { get; set; } = "USD";
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

    // ── Right now ────────────────────────────────────────────────────────────

    public int InClubNow { get; set; }
    public int? ClubCapacity { get; set; }
    public int OccupancyPercent { get; set; }

    public int CheckInsToday { get; set; }
    public int ClassesToday { get; set; }
    public int ClassesRemainingToday { get; set; }
    public int AppointmentsToday { get; set; }

    public int LeadsBreachingSla { get; set; }
    public int TasksDueToday { get; set; }
    public int DoorsOffline { get; set; }
    public int EquipmentOutOfService { get; set; }
    public int OpenIncidents { get; set; }

    /// <summary>Classes starting soon with places nobody has taken — still fixable today.</summary>
    public int UnderFilledClassesToday { get; set; }

    public decimal TakingsToday { get; set; }

    // ── Membership ───────────────────────────────────────────────────────────

    public int ActiveMembers { get; set; }
    public int ActiveMembersLastMonth { get; set; }
    public int JoinsThisMonth { get; set; }
    public int CancellationsThisMonth { get; set; }
    public int NetGrowth { get; set; }
    public decimal ChurnRatePercent { get; set; }
    public decimal ChurnRateLastMonth { get; set; }

    public int FrozenMembers { get; set; }
    public int PastDueMembers { get; set; }
    public int TrialMembers { get; set; }

    // ── Money ────────────────────────────────────────────────────────────────

    public decimal MonthlyRecurringRevenue { get; set; }
    public decimal MrrLastMonth { get; set; }
    public decimal MrrChangePercent { get; set; }

    public decimal RevenueThisMonth { get; set; }
    public decimal RevenueLastMonth { get; set; }
    public decimal RevenueChangePercent { get; set; }

    public decimal CollectedThisMonth { get; set; }
    public decimal BilledThisMonth { get; set; }
    public int CollectionRatePercent { get; set; }

    public decimal OutstandingBalance { get; set; }
    public int MembersInArrears { get; set; }
    public decimal AverageRevenuePerMember { get; set; }

    // ── Retention ────────────────────────────────────────────────────────────

    public int AtRiskMembers { get; set; }
    public int CriticalRiskMembers { get; set; }
    public decimal ValueAtRisk { get; set; }
    public int Nps { get; set; }

    // ── Sales ────────────────────────────────────────────────────────────────

    public int OpenLeads { get; set; }
    public int LeadsThisMonth { get; set; }
    public int ToursThisMonth { get; set; }
    public int LeadConversionPercent { get; set; }
    public int MedianResponseMinutes { get; set; }

    // ── Charts ───────────────────────────────────────────────────────────────

    public List<TrendPointDto> MemberTrend { get; set; } = [];
    public List<TrendPointDto> RevenueTrend { get; set; } = [];
    public List<HourlyVisitDto> VisitsByHour { get; set; } = [];
    public List<RevenueLineDto> RevenueByStream { get; set; } = [];
    public List<ClassOccurrenceSummaryDto> NextClasses { get; set; } = [];
    public List<AttentionItemDto> NeedsAttention { get; set; } = [];
}

/// <summary>Something wrong that a person can fix, with the route that fixes it.</summary>
public class AttentionItemDto
{
    public string Kind { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Detail { get; set; }
    public int Count { get; set; }
    public decimal? Value { get; set; }

    /// <summary>Info, warning or critical — and never colour alone in the UI.</summary>
    public string Severity { get; set; } = "warning";

    public string? Icon { get; set; }
    public string? Route { get; set; }
}

public class TrendPointDto
{
    public DateTime Period { get; set; }
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public decimal? SecondaryValue { get; set; }
    public int Count { get; set; }
}

public class HourlyVisitDto
{
    public int Hour { get; set; }
    public int Visits { get; set; }
    public int PeakOccupancy { get; set; }
}

// ── Membership reporting ─────────────────────────────────────────────────────

public class MembershipReportDto
{
    public Guid? ClubId { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }

    public int OpeningActive { get; set; }
    public int Joins { get; set; }
    public int Rejoins { get; set; }
    public int Cancellations { get; set; }
    public int Expiries { get; set; }
    public int ClosingActive { get; set; }
    public int NetGrowth { get; set; }

    /// <summary>Leavers over average active. The number the whole industry benchmarks on.</summary>
    public decimal GrossChurnPercent { get; set; }

    /// <summary>Churn less rejoins, which is what actually moved the member count.</summary>
    public decimal NetChurnPercent { get; set; }

    public int Frozen { get; set; }
    public int FreezeDaysTaken { get; set; }
    public int Suspended { get; set; }
    public int Upgrades { get; set; }
    public int Downgrades { get; set; }

    public decimal AverageTenureMonths { get; set; }
    public decimal AverageLifetimeValue { get; set; }

    public List<PlanMixLineDto> PlanMix { get; set; } = [];
    public List<LeaveReasonLineDto> LeaveReasons { get; set; } = [];
    public List<TenureBandDto> TenureDistribution { get; set; } = [];
    public List<TrendPointDto> Trend { get; set; } = [];
}

public class PlanMixLineDto
{
    public Guid PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public PlanKind Kind { get; set; }
    public int MemberCount { get; set; }
    public decimal PercentOfBase { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public decimal AveragePrice { get; set; }
    public int JoinsInPeriod { get; set; }
    public int CancellationsInPeriod { get; set; }
    public decimal ChurnPercent { get; set; }
}

public class LeaveReasonLineDto
{
    public LeaveReason Reason { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal PercentOfTotal { get; set; }
    public decimal ValueLost { get; set; }
    public int SavedCount { get; set; }
    public int SaveRatePercent { get; set; }
}

public class TenureBandDto
{
    public string Band { get; set; } = string.Empty;
    public int MemberCount { get; set; }
    public decimal PercentOfBase { get; set; }
    public decimal ChurnPercent { get; set; }
}

/// <summary>
/// Retention by join month: of everyone who joined in March, how many were still here at month
/// one, three, six, twelve. The report that tells an owner which channel brings members who stay.
/// </summary>
public class CohortRetentionDto
{
    public Guid? ClubId { get; set; }
    public string GroupedBy { get; set; } = "JoinMonth";
    public DateTime From { get; set; }
    public DateTime To { get; set; }

    public List<CohortRowDto> Cohorts { get; set; } = [];
    public List<int> Periods { get; set; } = [];
}

public class CohortRowDto
{
    public string Label { get; set; } = string.Empty;
    public DateTime CohortStart { get; set; }
    public int InitialSize { get; set; }

    /// <summary>Retained percentage at each period offset, aligned to the Periods list.</summary>
    public List<int?> RetentionPercent { get; set; } = [];
    public List<int?> RetainedCount { get; set; } = [];

    public decimal AverageLifetimeValue { get; set; }
}

// ── Financial reporting ──────────────────────────────────────────────────────

public class RevenueReportDto
{
    public Guid? ClubId { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public decimal TotalBilled { get; set; }
    public decimal TotalCollected { get; set; }
    public decimal TotalRefunded { get; set; }
    public decimal TotalWrittenOff { get; set; }
    public decimal NetRevenue { get; set; }
    public decimal TaxCollected { get; set; }

    public decimal RecognisedRevenue { get; set; }
    public decimal DeferredBalance { get; set; }

    public List<RevenueLineDto> ByStream { get; set; } = [];
    public List<RevenueLineDto> ByPaymentMethod { get; set; } = [];
    public List<RevenueLineDto> ByClub { get; set; } = [];
    public List<TrendPointDto> Trend { get; set; } = [];

    public decimal AverageRevenuePerMember { get; set; }
    public decimal AverageTransactionValue { get; set; }
}

/// <summary>Monthly recurring revenue and what moved it, which is the shape a subscription business reads.</summary>
public class MrrMovementDto
{
    public Guid? ClubId { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public decimal OpeningMrr { get; set; }
    public decimal NewMrr { get; set; }
    public decimal ExpansionMrr { get; set; }
    public decimal ContractionMrr { get; set; }
    public decimal ChurnedMrr { get; set; }
    public decimal ReactivationMrr { get; set; }
    public decimal ClosingMrr { get; set; }
    public decimal NetChange { get; set; }
    public decimal NetChangePercent { get; set; }

    public List<TrendPointDto> Trend { get; set; } = [];
}

public class ArrearsReportDto
{
    public Guid? ClubId { get; set; }
    public DateTime AsAt { get; set; }
    public string CurrencyCode { get; set; } = "USD";

    public decimal TotalOutstanding { get; set; }
    public int MemberCount { get; set; }

    public decimal Current { get; set; }
    public decimal Days1To30 { get; set; }
    public decimal Days31To60 { get; set; }
    public decimal Days61To90 { get; set; }
    public decimal Over90Days { get; set; }

    public int OpenDunningCases { get; set; }
    public decimal InRecovery { get; set; }
    public decimal RecoveredThisMonth { get; set; }
    public int RecoveryRatePercent { get; set; }

    public List<ArrearsLineDto> Members { get; set; } = [];
    public List<RevenueLineDto> ByFailureReason { get; set; } = [];
}

public class ArrearsLineDto
{
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public string? MemberNumber { get; set; }
    public string? Phone { get; set; }
    public decimal Outstanding { get; set; }
    public int DaysOverdue { get; set; }
    public string AgeBand { get; set; } = string.Empty;
    public PaymentFailureReason? LastFailureReason { get; set; }
    public bool HasValidPaymentMethod { get; set; }
    public bool AccessSuspended { get; set; }
    public Guid? DunningCaseId { get; set; }
    public int DunningStep { get; set; }
}

// ── Attendance & utilisation ─────────────────────────────────────────────────

public class AttendanceReportDto
{
    public Guid? ClubId { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }

    public int TotalVisits { get; set; }
    public int UniqueMembers { get; set; }
    public decimal VisitsPerMember { get; set; }
    public decimal AverageVisitMinutes { get; set; }

    public int PeakOccupancy { get; set; }
    public DateTime? PeakAt { get; set; }

    /// <summary>Members who paid and never came, which is a churn cohort waiting to happen.</summary>
    public int ZeroVisitMembers { get; set; }
    public int LowUsageMembers { get; set; }

    public List<HeatmapCellDto> Heatmap { get; set; } = [];
    public List<TrendPointDto> Trend { get; set; } = [];
    public List<RevenueLineDto> ByVisitKind { get; set; } = [];
    public List<RevenueLineDto> ByCheckInMethod { get; set; } = [];
}

public class HeatmapCellDto
{
    public int DayOfWeek { get; set; }
    public int Hour { get; set; }
    public int Visits { get; set; }
    public int AverageOccupancy { get; set; }
    public int Intensity { get; set; }
}

public class ClassPerformanceReportDto
{
    public Guid? ClubId { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }

    public int TotalClasses { get; set; }
    public int CancelledClasses { get; set; }
    public int TotalCapacity { get; set; }
    public int TotalBooked { get; set; }
    public int TotalAttended { get; set; }
    public int NoShows { get; set; }
    public int LateCancels { get; set; }

    public int AverageFillPercent { get; set; }
    public int NoShowRatePercent { get; set; }
    public int WaitlistDemand { get; set; }

    public List<ClassPerformanceLineDto> ByClassType { get; set; } = [];
    public List<ClassPerformanceLineDto> ByInstructor { get; set; } = [];
    public List<ClassPerformanceLineDto> ByTimeSlot { get; set; } = [];

    /// <summary>Classes worth adding, because they fill and then waitlist.</summary>
    public List<ClassPerformanceLineDto> HighDemand { get; set; } = [];

    /// <summary>Classes worth cutting, because they cost an instructor and run near-empty.</summary>
    public List<ClassPerformanceLineDto> UnderPerforming { get; set; } = [];
}

public class ClassPerformanceLineDto
{
    public Guid? Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public int Occurrences { get; set; }
    public int Capacity { get; set; }
    public int Booked { get; set; }
    public int Attended { get; set; }
    public int NoShows { get; set; }
    public int WaitlistTotal { get; set; }
    public int FillPercent { get; set; }
    public int NoShowPercent { get; set; }

    public decimal Revenue { get; set; }
    public decimal InstructorCost { get; set; }
    public decimal Contribution { get; set; }
    public decimal RevenuePerHead { get; set; }
}

// ── Sales reporting ──────────────────────────────────────────────────────────

public class SalesReportDto
{
    public Guid? ClubId { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }

    public int TotalLeads { get; set; }
    public int Tours { get; set; }
    public int Trials { get; set; }
    public int Joins { get; set; }
    public decimal JoinValue { get; set; }

    public int LeadToTourPercent { get; set; }
    public int TourToJoinPercent { get; set; }
    public int TrialToJoinPercent { get; set; }
    public int OverallConversionPercent { get; set; }

    public int MedianResponseMinutes { get; set; }
    public int SlaBreaches { get; set; }
    public int TourNoShows { get; set; }

    public decimal MarketingSpend { get; set; }
    public decimal CostPerLead { get; set; }
    public decimal CostPerAcquisition { get; set; }

    public List<SalesFunnelStageDto> Funnel { get; set; } = [];
    public List<LeadSourceDto> BySource { get; set; } = [];
    public List<SalesPerformerDto> ByStaff { get; set; } = [];
    public List<LossReasonDto> LossReasons { get; set; } = [];
    public List<TrendPointDto> Trend { get; set; } = [];
}

public class SalesFunnelStageDto
{
    public string Stage { get; set; } = string.Empty;
    public int Count { get; set; }
    public int ConversionFromPreviousPercent { get; set; }
    public int ConversionFromTopPercent { get; set; }
    public decimal AverageDaysInStage { get; set; }
}

public class SalesPerformerDto
{
    public Guid StaffId { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public int LeadsAssigned { get; set; }
    public int Tours { get; set; }
    public int Joins { get; set; }
    public decimal Value { get; set; }
    public int ConversionPercent { get; set; }
    public int MedianResponseMinutes { get; set; }
    public decimal TargetValue { get; set; }
    public int AchievementPercent { get; set; }
    public int Rank { get; set; }
}

// ── Staff reporting ──────────────────────────────────────────────────────────

public class StaffPerformanceReportDto
{
    public Guid? ClubId { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }

    public List<StaffPerformanceLineDto> Staff { get; set; } = [];

    public int TotalSessionsDelivered { get; set; }
    public int TotalClassesTaught { get; set; }
    public decimal TotalCommission { get; set; }
    public decimal TotalHours { get; set; }
    public int AverageUtilisationPercent { get; set; }
}

public class StaffPerformanceLineDto
{
    public Guid StaffId { get; set; }
    public string StaffName { get; set; } = string.Empty;
    public string? PhotoUrl { get; set; }
    public StaffRoleKind RoleKind { get; set; }

    public int SessionsDelivered { get; set; }
    public int ClassesTaught { get; set; }
    public int ClassAttendance { get; set; }
    public int AverageClassFillPercent { get; set; }

    public decimal HoursWorked { get; set; }
    public decimal HoursAvailable { get; set; }
    public int UtilisationPercent { get; set; }

    public int MembershipsSold { get; set; }
    public int PackagesSold { get; set; }
    public decimal SalesValue { get; set; }
    public decimal Commission { get; set; }

    public int AssignedClients { get; set; }
    public int ClientsRetained { get; set; }
    public int ClientRetentionPercent { get; set; }
    public int? Nps { get; set; }

    public int LateCount { get; set; }
    public int NoShowCount { get; set; }
    public int ExpiringCertifications { get; set; }
}

// ── Operational reporting ────────────────────────────────────────────────────

public class OperationsReportDto
{
    public Guid? ClubId { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }

    public int Incidents { get; set; }
    public int ReportableIncidents { get; set; }
    public int OpenIncidents { get; set; }
    public decimal IncidentCost { get; set; }

    public int Complaints { get; set; }
    public int ComplaintsResolved { get; set; }
    public decimal AverageResolutionDays { get; set; }
    public decimal CompensationPaid { get; set; }

    public int WorkOrders { get; set; }
    public int OpenWorkOrders { get; set; }
    public decimal MaintenanceCost { get; set; }
    public int EquipmentDowntimeHours { get; set; }
    public int AssetsOutOfService { get; set; }

    public int FacilityChecksDue { get; set; }
    public int FacilityChecksCompleted { get; set; }
    public int ComplianceRatePercent { get; set; }

    public int AccessDenials { get; set; }
    public int ManualOverrides { get; set; }
    public int ControllerOutages { get; set; }

    public int LostPropertyHeld { get; set; }

    public List<RevenueLineDto> IncidentsByKind { get; set; } = [];
    public List<RevenueLineDto> DenialsByReason { get; set; } = [];
    public List<RevenueLineDto> ComplaintsByCategory { get; set; } = [];
}

// ── Report plumbing ──────────────────────────────────────────────────────────

/// <summary>The filter every report takes, so the toolbars look and behave the same everywhere.</summary>
public class ReportFilterDto
{
    public Guid? ClubId { get; set; }
    public ReportPeriod Period { get; set; } = ReportPeriod.ThisMonth;
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public Guid? PlanId { get; set; }
    public Guid? StaffId { get; set; }
    public Guid? ClassTypeId { get; set; }
    public string? GroupBy { get; set; }
    public bool CompareToPreviousPeriod { get; set; } = true;
}

/// <summary>A report emailed on a schedule, so a manager does not have to remember to open it.</summary>
public class ReportSubscriptionDto
{
    public Guid Id { get; set; }
    public Guid? ClubId { get; set; }
    public string ReportKey { get; set; } = string.Empty;
    public string ReportName { get; set; } = string.Empty;

    /// <summary>Daily, weekly or monthly, plus the day it lands on.</summary>
    public string Cadence { get; set; } = "Weekly";
    public int? DayOfWeek { get; set; }
    public int? DayOfMonth { get; set; }
    public TimeSpan SendAt { get; set; }

    public List<string> Recipients { get; set; } = [];
    public string Format { get; set; } = "Pdf";
    public string? FilterJson { get; set; }

    public DateTime? LastSentAt { get; set; }
    public DateTime? NextSendAt { get; set; }
    public bool IsActive { get; set; }
}
