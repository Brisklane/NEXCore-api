using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;
using Accounting.Application.DTOs;
using Accounting.Application.Services.Interfaces;

namespace Accounting.Api.Controllers;

/// <summary>
/// Tax Code management controller
/// Tenant context (CompanyId, BranchId, BusinessUnitId) automatically extracted and applied at service/repository level
/// All queries automatically filtered by tenant context
/// Business logic has been moved to ITaxCodeService layer
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class TaxCodeController : ControllerBase
{
    private readonly ITaxCodeService _service;
    private readonly ILogger<TaxCodeController> _logger;

    public TaxCodeController(ITaxCodeService service, ILogger<TaxCodeController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Get all active tax codes for current tenant
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<TaxCodeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAllTaxCodes([FromQuery] PaginationParams pagination)
    {
        try
        {
            var response = await _service.GetAllAsync(pagination);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid tenant context");
            return Unauthorized(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tax codes");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get tax code by code
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpGet("{code}")]
    [ProducesResponseType(typeof(ApiResponse<TaxCodeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetTaxCodeByCode(string code)
    {
        try
        {
            var taxCode = await _service.GetByCodeAsync(code);
            if (taxCode == null)
                return NotFound(new ApiErrorResponse { Message = "Tax code not found" });

            return Ok(new ApiResponse<TaxCodeDto>
            {
                Success = true,
                Data = taxCode
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid tenant context");
            return Unauthorized(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tax code");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }
}
