using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;
using Accounting.Application.DTOs;
using Accounting.Application.Services.Interfaces;

namespace Accounting.Api.Controllers;

/// <summary>
/// Fiscal Period management controller
/// Tenant context (CompanyId, BranchId, BusinessUnitId) automatically extracted and applied at service/repository level
/// All queries automatically filtered by tenant context
/// Business logic has been moved to IFiscalPeriodService layer
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class FiscalPeriodController : ControllerBase
{
    private readonly IFiscalPeriodService _service;
    private readonly ILogger<FiscalPeriodController> _logger;

    public FiscalPeriodController(IFiscalPeriodService service, ILogger<FiscalPeriodController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Create a new fiscal period
    /// Tenant context (CompanyId, BranchId, BusinessUnitId) automatically populated by service/repository
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<FiscalPeriodDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateFiscalPeriod([FromBody] CreateFiscalPeriodDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var response = await _service.CreateAsync(request, userId);

            return CreatedAtAction(nameof(GetFiscalPeriodById), new { id = response.Id }, new ApiResponse<FiscalPeriodDto>
            {
                Success = true,
                Message = "Fiscal period created successfully",
                Data = response
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation during fiscal period creation");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating fiscal period");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get fiscal period by ID
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<FiscalPeriodDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetFiscalPeriodById(Guid id)
    {
        try
        {
            var period = await _service.GetByIdAsync(id);
            if (period == null)
                return NotFound(new ApiErrorResponse { Message = "Fiscal period not found" });

            return Ok(new ApiResponse<FiscalPeriodDto>
            {
                Success = true,
                Data = period
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid tenant context");
            return Unauthorized(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fiscal period");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get all fiscal periods for current tenant
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<FiscalPeriodDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAllFiscalPeriods([FromQuery] PaginationParams pagination)
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
            _logger.LogError(ex, "Error retrieving fiscal periods");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get periods by fiscal calendar
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpGet("calendar/{calendarId}")]
    [ProducesResponseType(typeof(PaginatedResponse<FiscalPeriodDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetPeriodsByCalendar(Guid calendarId, [FromQuery] PaginationParams pagination)
    {
        try
        {
            var response = await _service.GetByCalendarIdAsync(calendarId, pagination);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid tenant context");
            return Unauthorized(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fiscal periods");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update fiscal period
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<FiscalPeriodDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateFiscalPeriod(Guid id, [FromBody] UpdateFiscalPeriodDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var response = await _service.UpdateAsync(id, request, userId);

            return Ok(new ApiResponse<FiscalPeriodDto>
            {
                Success = true,
                Message = "Fiscal period updated successfully",
                Data = response
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Fiscal period not found");
            return NotFound(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating fiscal period");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Close fiscal period
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpPost("{id}/close")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CloseFiscalPeriod(Guid id)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            await _service.CloseAsync(id, userId);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error closing fiscal period");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing fiscal period");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete fiscal period (soft delete)
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteFiscalPeriod(Guid id)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            await _service.DeleteAsync(id, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error during fiscal period deletion");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting fiscal period");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }
}
