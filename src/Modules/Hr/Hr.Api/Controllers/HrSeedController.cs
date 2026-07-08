using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;
using Hr.Infrastructure.Services;

namespace Hr.Api.Controllers;

/// <summary>
/// One-time setup endpoint to seed required LookupTypes and LookupValues for the HR module.
///
/// MUST be called once per tenant before submitting any approval requests.
/// Safe to call multiple times — fully idempotent (no duplicates created).
///
/// Seeds the following:
///   APPROVAL_STATUS ? PENDING, WAITING, APPROVED, REJECTED, CANCELLED, IN_PROGRESS
///   JOB_STATUS      ? JOB_DRAFT, JOB_PENDING, JOB_APPROVED, JOB_REJECTED, JOB_OPEN, JOB_CLOSED, JOB_CANCELLED
/// </summary>
[ApiController]
[Route("api/v1/hr/seed")]
[Produces("application/json")]
[Authorize]
public class HrSeedController : ControllerBase
{
    private readonly HrLookupSeedService _seedService;
    private readonly ILogger<HrSeedController> _logger;

    public HrSeedController(HrLookupSeedService seedService, ILogger<HrSeedController> logger)
    {
        _seedService = seedService;
        _logger      = logger;
    }

    /// <summary>
    /// Seeds all required HR LookupTypes and LookupValues for the current tenant.
    /// Run this once after tenant provisioning and before using the approval workflow.
    /// Idempotent — calling multiple times will not create duplicates.
    /// </summary>
    [HttpPost("lookup-values")]
    [ProducesResponseType(typeof(ApiResponse<SeedResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SeedLookupValues()
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _seedService.SeedAllAsync(userId);

            return Ok(new ApiResponse<SeedResultDto>
            {
                Success = true,
                Message = result.Summary,
                Data    = new SeedResultDto { Created = result.Created, Skipped = result.Skipped }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding HR lookup values");
            return StatusCode(500, new ApiErrorResponse { Message = ex.Message });
        }
    }
}

public class SeedResultDto
{
    public int Created { get; set; }
    public int Skipped { get; set; }
}
