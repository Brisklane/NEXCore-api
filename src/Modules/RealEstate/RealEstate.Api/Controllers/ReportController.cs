using Microsoft.AspNetCore.Mvc;
using RealEstate.Application.DTOs;
using RealEstate.Application.Services.Interfaces;

namespace RealEstate.Api.Controllers;

/// <summary>
/// The dashboard, the attention list, the portfolio view and the report catalogue.
///
/// The attention list is computed live rather than read from a table, because a stale warning is
/// worse than none: somebody clears the problem, the banner stays up, and within a fortnight
/// everybody has learned to ignore the banner.
/// </summary>
[Route("api/realestate/reports")]
public class ReportController(
    IRealEstateReportService reports,
    ILogger<ReportController> logger) : RealEstateControllerBase(logger)
{
    [HttpGet("dashboard")]
    public Task<IActionResult> GetDashboard([FromQuery] Guid? officeId, [FromQuery] Guid? projectId)
        => Run(() => reports.GetDashboardAsync(officeId, projectId));

    /// <summary>What would go wrong today if nobody looked, ranked by what it costs.</summary>
    [HttpGet("attention")]
    public Task<IActionResult> GetAttention([FromQuery] Guid? officeId, [FromQuery] Guid? projectId)
        => Run(() => reports.GetAttentionAsync(officeId, projectId));

    [HttpGet("portfolio")]
    public Task<IActionResult> GetPortfolio([FromQuery] ListQueryDto query)
        => Run(() => reports.GetPortfolioAsync(query));

    /// <summary>Every report this company can run, filtered to the lines of business it operates.</summary>
    [HttpGet("catalogue")]
    public Task<IActionResult> GetCatalogue() => Run(reports.GetReportCatalogueAsync);

    [HttpPost("run")]
    public Task<IActionResult> Run([FromBody] ReportRequestDto request)
        => Run(() => reports.RunReportAsync(request));
}
