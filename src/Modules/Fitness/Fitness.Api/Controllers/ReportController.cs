using Fitness.Application.DTOs;
using Fitness.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Fitness.Api.Controllers;

/// <summary>
/// The numbers a club is actually run on.
///
/// Two are worth calling out, because most SMB tools in this market get them wrong. **Churn** is
/// leavers over the *average* active count for the period, not the closing count — dividing by
/// the closing figure flatters a shrinking club and punishes a growing one. And **MRR movement**
/// is decomposed into new, expansion, contraction, churn and reactivation, because "revenue is
/// down" is not a finding; "eleven downgrades and four cancellations" is.
/// </summary>
[Route("api/fitness/reports")]
public class ReportController(
    IFitnessReportService reports,
    ILogger<ReportController> logger) : FitnessControllerBase(logger)
{
    /// <summary>Everything the owner sees on opening the app, including what needs fixing today.</summary>
    [HttpGet("dashboard")]
    public Task<IActionResult> GetDashboard([FromQuery] Guid? clubId)
        => Run(() => reports.GetDashboardAsync(clubId));

    [HttpPost("membership")]
    public Task<IActionResult> GetMembershipReport([FromBody] ReportFilterDto filter)
        => Run(() => reports.GetMembershipReportAsync(filter));

    /// <summary>
    /// Retention by join cohort.
    ///
    /// The report that tells an owner which channel brings members who *stay*. A source producing
    /// cheap leads at 40% six-month retention is more expensive than a dear one at 80%.
    /// </summary>
    [HttpPost("cohorts")]
    public Task<IActionResult> GetCohortRetention([FromBody] ReportFilterDto filter)
        => Run(() => reports.GetCohortRetentionAsync(filter));

    [HttpPost("revenue")]
    public Task<IActionResult> GetRevenueReport([FromBody] ReportFilterDto filter)
        => Run(() => reports.GetRevenueReportAsync(filter));

    [HttpPost("mrr")]
    public Task<IActionResult> GetMrrMovement([FromBody] ReportFilterDto filter)
        => Run(() => reports.GetMrrMovementAsync(filter));

    [HttpPost("attendance")]
    public Task<IActionResult> GetAttendanceReport([FromBody] ReportFilterDto filter)
        => Run(() => reports.GetAttendanceReportAsync(filter));

    /// <summary>Class performance, ending with what to add and what to cut.</summary>
    [HttpPost("classes")]
    public Task<IActionResult> GetClassPerformance([FromBody] ReportFilterDto filter)
        => Run(() => reports.GetClassPerformanceAsync(filter));

    [HttpPost("sales")]
    public Task<IActionResult> GetSalesReport([FromBody] ReportFilterDto filter)
        => Run(() => reports.GetSalesReportAsync(filter));

    [HttpPost("staff")]
    public Task<IActionResult> GetStaffPerformance([FromBody] ReportFilterDto filter)
        => Run(() => reports.GetStaffPerformanceAsync(filter));

    [HttpPost("operations")]
    public Task<IActionResult> GetOperationsReport([FromBody] ReportFilterDto filter)
        => Run(() => reports.GetOperationsReportAsync(filter));

    // ── Subscriptions ────────────────────────────────────────────────────────

    [HttpGet("subscriptions")]
    public Task<IActionResult> GetSubscriptions([FromQuery] Guid? clubId)
        => Run(() => reports.GetSubscriptionsAsync(clubId));

    [HttpPost("subscriptions")]
    public Task<IActionResult> SaveSubscription([FromBody] ReportSubscriptionDto request, [FromQuery] Guid? id = null)
        => Run(() => reports.SaveSubscriptionAsync(id, request, UserId), "Subscription saved.");
}
