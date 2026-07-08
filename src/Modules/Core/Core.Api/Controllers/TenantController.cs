using Core.Application.DTOs;
using Core.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;

namespace Core.Api.Controllers;

/// <summary>Tenant administration: provisioning, lookup, and lifecycle for the top-level tenant records that own companies.</summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize]
public class TenantController : ControllerBase
{
    private readonly ITenantService _tenantService;
    private readonly ILogger<TenantController> _logger;

    public TenantController(ITenantService tenantService, ILogger<TenantController> logger)
    {
        _tenantService = tenantService;
        _logger = logger;
    }

    // ── Tenant CRUD ────────────────────────────────────────────────────────────

    /// <summary>
    /// Get tenant by ID
    /// </summary>
    [HttpGet("{tenantId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TenantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenant(Guid tenantId)
    {
        try
        {
            var result = await _tenantService.GetTenantByIdAsync(tenantId);
            if (!result.Success)
                return NotFound(new ApiErrorResponse { Message = result.Message });

            return Ok(new ApiResponse<TenantDto> { Data = result.Data, Success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenant {TenantId}", tenantId);
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get all tenants — platform admin only
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<TenantDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllTenants()
    {
        try
        {
            var result = await _tenantService.GetAllTenantsAsync();
            if (!result.Success)
                return BadRequest(new ApiErrorResponse { Message = result.Message });

            return Ok(new ApiResponse<IEnumerable<TenantDto>> { Data = result.Data, Success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tenants");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update tenant name / contact info
    /// </summary>
    [HttpPut("{tenantId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TenantDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTenant(Guid tenantId, [FromBody] UpdateTenantDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _tenantService.UpdateTenantAsync(tenantId, request, userId);
            if (!result.Success)
                return result.Message?.Contains("not found") == true
                    ? NotFound(new ApiErrorResponse { Message = result.Message })
                    : BadRequest(new ApiErrorResponse { Message = result.Message });

            return Ok(new ApiResponse<TenantDto> { Data = result.Data, Success = true, Message = "Tenant updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tenant {TenantId}", tenantId);
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Deactivate (soft-delete) a tenant — platform admin only
    /// </summary>
    [HttpDelete("{tenantId:guid}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeactivateTenant(Guid tenantId)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _tenantService.DeactivateTenantAsync(tenantId, userId);
            if (!result.Success)
                return NotFound(new ApiErrorResponse { Message = result.Message });

            return Ok(new ApiResponse { Message = result.Message, Success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating tenant {TenantId}", tenantId);
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    // ── Subscription / Licensing ───────────────────────────────────────────────

    /// <summary>
    /// Get all available subscription plans
    /// </summary>
    [HttpGet("plans")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<SubscriptionPlanDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubscriptionPlans()
    {
        try
        {
            var result = await _tenantService.GetSubscriptionPlansAsync();
            return Ok(new ApiResponse<IEnumerable<SubscriptionPlanDto>> { Data = result.Data, Success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subscription plans");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get license and subscription info for a tenant
    /// </summary>
    [HttpGet("{tenantId:guid}/license")]
    [ProducesResponseType(typeof(ApiResponse<TenantLicenseInfoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLicenseInfo(Guid tenantId)
    {
        try
        {
            var result = await _tenantService.GetLicenseInfoAsync(tenantId);
            if (!result.Success)
                return NotFound(new ApiErrorResponse { Message = result.Message });

            return Ok(new ApiResponse<TenantLicenseInfoDto> { Data = result.Data, Success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving license info for tenant {TenantId}", tenantId);
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Change the subscription plan for a tenant
    /// </summary>
    [HttpPost("{tenantId:guid}/subscription/change-plan")]
    [ProducesResponseType(typeof(ApiResponse<TenantSubscriptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePlan(Guid tenantId, [FromBody] ChangePlanDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _tenantService.ChangePlanAsync(tenantId, request, userId);
            if (!result.Success)
                return BadRequest(new ApiErrorResponse { Message = result.Message });

            return Ok(new ApiResponse<TenantSubscriptionDto>
            {
                Data = result.Data,
                Success = true,
                Message = "Subscription plan changed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing plan for tenant {TenantId}", tenantId);
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Renew the current subscription for a tenant
    /// </summary>
    [HttpPost("{tenantId:guid}/subscription/renew")]
    [ProducesResponseType(typeof(ApiResponse<TenantSubscriptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RenewSubscription(Guid tenantId)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _tenantService.RenewSubscriptionAsync(tenantId, userId);
            if (!result.Success)
                return BadRequest(new ApiErrorResponse { Message = result.Message });

            return Ok(new ApiResponse<TenantSubscriptionDto>
            {
                Data = result.Data,
                Success = true,
                Message = "Subscription renewed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error renewing subscription for tenant {TenantId}", tenantId);
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }
}
