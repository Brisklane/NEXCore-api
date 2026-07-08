using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;
using Accounting.Application.DTOs;
using Accounting.Application.Services.Interfaces;

namespace Accounting.Api.Controllers;

/// <summary>
/// Ledger management controller
/// Tenant context (CompanyId, BranchId, BusinessUnitId) automatically extracted and applied at service/repository level
/// All queries automatically filtered by tenant context
/// Business logic has been moved to ILedgerService layer
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class LedgerController : ControllerBase
{
    private readonly ILedgerService _service;
    private readonly ILogger<LedgerController> _logger;

    public LedgerController(ILedgerService service, ILogger<LedgerController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Create a new ledger
    /// Tenant context (CompanyId, BranchId, BusinessUnitId) automatically populated by service/repository
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<LedgerDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateLedger([FromBody] CreateLedgerDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var response = await _service.CreateAsync(request, userId);

            return CreatedAtAction(nameof(GetLedgerById), new { id = response.Id }, new ApiResponse<LedgerDto> 
            { 
                Success = true, 
                Message = "Ledger created successfully",
                Data = response 
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation during ledger creation");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ledger");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get ledger by ID
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<LedgerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetLedgerById(Guid id)
    {
        try
        {
            var ledger = await _service.GetByIdAsync(id);
            if (ledger == null)
                return NotFound(new ApiErrorResponse { Message = "Ledger not found" });

            return Ok(new ApiResponse<LedgerDto> 
            { 
                Success = true,
                Data = ledger
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid tenant context");
            return Unauthorized(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ledger");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get all ledgers for current tenant
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<LedgerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCompanyLedgers([FromQuery] PaginationParams pagination)
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
            _logger.LogError(ex, "Error retrieving ledgers");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get default ledger for current tenant
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpGet("default")]
    [ProducesResponseType(typeof(ApiResponse<LedgerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetDefaultLedger()
    {
        try
        {
            var ledger = await _service.GetDefaultAsync();
            if (ledger == null)
                return NotFound(new ApiErrorResponse { Message = "Default ledger not found" });

            return Ok(new ApiResponse<LedgerDto>
            {
                Success = true,
                Data = ledger
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid tenant context");
            return Unauthorized(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving default ledger");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update ledger
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<LedgerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateLedger(Guid id, [FromBody] UpdateLedgerDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var response = await _service.UpdateAsync(id, request, userId);

            return Ok(new ApiResponse<LedgerDto>
            {
                Success = true,
                Message = "Ledger updated successfully",
                Data = response
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Ledger not found");
            return NotFound(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ledger");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete ledger (soft delete)
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteLedger(Guid id)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            await _service.DeleteAsync(id, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error during ledger deletion");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting ledger");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }
}
