using Crm.Application.DTOs;
using Crm.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;

namespace Crm.Api.Controllers;

/// <summary>CRM home dashboard - aggregated summary data for the home page.</summary>
[ApiController]
[Route("api/v1/crm")]
[Produces("application/json")]
[Authorize]
public class CrmHomeController : ControllerBase
{
    private readonly ICrmHomeDashboardService _dashboardService;
    private readonly ILogger<CrmHomeController> _logger;

    public CrmHomeController(ICrmHomeDashboardService dashboardService, ILogger<CrmHomeController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>Get CRM home dashboard with aggregated data.</summary>
    [HttpGet("home")]
    [ProducesResponseType(typeof(ApiResponse<CrmHomeDashboardDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetHome()
    {
        try
        {
            var result = await _dashboardService.GetDashboardAsync();
            return Ok(new ApiResponse<CrmHomeDashboardDto>
            {
                Success = true,
                Data = result,
                Message = "CRM home dashboard retrieved successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving CRM home dashboard");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }
}
