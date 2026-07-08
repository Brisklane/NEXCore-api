using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Procurement.Application.DTOs;
using Procurement.Application.Services.Interfaces;

namespace Procurement.Api.Controllers;

/// <summary>Procurement analytics — purchase analysis, vendor analysis, AP aging, 3-way match, spend by category.</summary>
[ApiController]
[Route("api/v1/procurement/reports")]
[Produces("application/json")]
[Authorize]
public class ProcurementReportsController : ControllerBase
{
    private readonly IProcurementReportsService _reports;
    private readonly ILogger<ProcurementReportsController> _logger;

    public ProcurementReportsController(IProcurementReportsService reports, ILogger<ProcurementReportsController> logger)
    {
        _reports = reports;
        _logger  = logger;
    }

    [HttpGet("purchase-analysis")]
    [ProducesResponseType(typeof(ApiResponse<PurchaseAnalysisDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> PurchaseAnalysis()
        => await Run(() => _reports.GetPurchaseAnalysisAsync(), "purchase analysis");

    [HttpGet("vendor-analysis")]
    [ProducesResponseType(typeof(ApiResponse<VendorAnalysisDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> VendorAnalysis()
        => await Run(() => _reports.GetVendorAnalysisAsync(), "vendor analysis");

    [HttpGet("ap-aging")]
    [ProducesResponseType(typeof(ApiResponse<ApAgingDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApAging()
        => await Run(() => _reports.GetApAgingAsync(), "AP aging");

    [HttpGet("three-way-match")]
    [ProducesResponseType(typeof(ApiResponse<ThreeWayMatchDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ThreeWayMatch()
        => await Run(() => _reports.GetThreeWayMatchAsync(), "3-way match");

    [HttpGet("spend-by-category")]
    [ProducesResponseType(typeof(ApiResponse<SpendByCategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SpendByCategory()
        => await Run(() => _reports.GetSpendByCategoryAsync(), "spend by category");

    private async Task<IActionResult> Run<T>(Func<Task<T>> action, string name)
    {
        try { return Ok(new ApiResponse<T> { Success = true, Data = await action() }); }
        catch (Exception ex) { _logger.LogError(ex, "Error building {Report}", name); return StatusCode(500, new ApiErrorResponse { Message = $"Error building {name} report" }); }
    }
}
