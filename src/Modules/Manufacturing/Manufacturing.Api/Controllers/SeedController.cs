using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;
using Manufacturing.Infrastructure.Services;

namespace Manufacturing.Api.Controllers;

/// <summary>
/// Developer / demo endpoint for seeding comprehensive manufacturing sample data.
/// Seeds the full manufacturing workflow: BOM, Routing, Standard Costs,
/// Production Orders, Operations, Material Issues, WIP, Inspections,
/// Finished Goods Receipts, Batches, Costing, Variances, Downtimes, Rework
/// and Subcontract Orders.
///
/// Should be disabled or restricted in production environments.
/// </summary>
[ApiController]
[Route("api/v1/manufacturing/seed")]
[Produces("application/json")]
[Authorize]
public class SeedController : ControllerBase
{
    private readonly ManufacturingInitializationService _initService;
    private readonly ManufacturingSeedDataService _seedService;
    private readonly ILogger<SeedController> _logger;

    public SeedController(
        ManufacturingInitializationService initService,
        ManufacturingSeedDataService seedService,
        ILogger<SeedController> logger)
    {
        _initService = initService;
        _seedService = seedService;
        _logger = logger;
    }

    /// <summary>
    /// Deletes ALL manufacturing data for the current tenant (work centers, BOMs,
    /// routings, production orders and all child records). Use before re-seeding
    /// to start from a clean slate.
    /// </summary>
    [HttpDelete]
    [ProducesResponseType(typeof(ApiResponse<SeedResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Clear(CancellationToken cancellationToken = default)
    {
        try
        {
            var (companyId, _, businessUnitId) = TenantContextHelper.ExtractTenantContext(User);

            if (businessUnitId is null)
                return BadRequest(new ApiErrorResponse { Message = "BusinessUnitId claim is required." });

            await _seedService.ClearAsync(companyId);

            var result = new SeedResultDto
            {
                Seeded  = false,
                Message = "All manufacturing data cleared for this company. POST to this endpoint to re-seed."
            };
            return Ok(new ApiResponse<SeedResultDto> { Success = true, Data = result, Message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing manufacturing data");
            return StatusCode(500, new ApiErrorResponse { Message = "Failed to clear manufacturing data: " + ex.Message });
        }
    }

    /// <summary>
    /// Seeds comprehensive sample manufacturing data for the current tenant.
    /// Idempotent — if data already exists for the company it returns 200 without re-seeding.
    /// Call DELETE first if you need to replace existing data.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SeedResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Seed(CancellationToken cancellationToken = default)
    {
        try
        {
            var (companyId, branchId, businessUnitId) = TenantContextHelper.ExtractTenantContext(User);
            var userId = TenantContextHelper.ExtractUserId(User);

            if (businessUnitId is null)
                return BadRequest(new ApiErrorResponse { Message = "BusinessUnitId claim is required." });

            // Ensure master data (WorkCenters, OverheadRules) exists first - SeedAsync depends on them
            await _initService.InitializeForNewCompanyAsync(companyId, branchId, businessUnitId.Value, userId);

            var seeded = await _seedService.SeedAsync(companyId, branchId, businessUnitId.Value, userId);

            var result = new SeedResultDto
            {
                Seeded = seeded,
                Message = seeded
                    ? "Manufacturing data seeded: 3 BOMs (Chair, Desk, Cabinet), 3 Routings, " +
                      "3 Production Orders (1 Completed, 1 InProgress, 1 Planned), " +
                      "WorkCenter Shifts, Standard Costs, Material Planning, Demands, " +
                      "Inspections, FG Receipts, Batches, Costs, Variances, Downtime, Rework and Inventory Transactions."
                    : "Manufacturing data already exists for this company. DELETE first to re-seed."
            };

            return Ok(new ApiResponse<SeedResultDto> { Success = true, Data = result, Message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding manufacturing data");
            return StatusCode(500, new ApiErrorResponse { Message = "Failed to seed manufacturing data: " + ex.Message });
        }
    }
}

public class SeedResultDto
{
    public bool Seeded { get; set; }
    public string Message { get; set; } = string.Empty;
}
