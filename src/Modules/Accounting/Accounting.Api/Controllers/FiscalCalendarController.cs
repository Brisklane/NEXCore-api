using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;
using Accounting.Application.DTOs;
using Accounting.Application.Services.Interfaces;

namespace Accounting.Api.Controllers;

/// <summary>
/// Fiscal Calendar management controller
/// Tenant context (CompanyId, BranchId, BusinessUnitId) automatically extracted and applied at service/repository level
/// All queries automatically filtered by tenant context
/// Business logic has been moved to IFiscalCalendarService layer
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class FiscalCalendarController : ControllerBase
{
    private readonly IFiscalCalendarService _service;
    private readonly ILogger<FiscalCalendarController> _logger;

    public FiscalCalendarController(IFiscalCalendarService service, ILogger<FiscalCalendarController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Get fiscal calendar by ID
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<FiscalCalendarDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCalendarById(Guid id)
    {
        try
        {
            var calendar = await _service.GetByIdAsync(id);
            if (calendar == null)
                return NotFound(new ApiErrorResponse { Message = "Fiscal calendar not found" });

            return Ok(new ApiResponse<FiscalCalendarDto>
            {
                Success = true,
                Data = calendar
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid tenant context");
            return Unauthorized(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fiscal calendar");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get all fiscal calendars for current tenant
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<FiscalCalendarDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCompanyCalendars([FromQuery] PaginationParams pagination)
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
            _logger.LogError(ex, "Error retrieving fiscal calendars");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Create a new fiscal year.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<FiscalCalendarDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCalendar([FromBody] CreateFiscalCalendarDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var created = await _service.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetCalendarById), new { id = created.Id },
                new ApiResponse<FiscalCalendarDto> { Success = true, Message = "Fiscal year created", Data = created });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation creating fiscal calendar");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating fiscal calendar");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update a fiscal year (only while open and with no transactions).
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<FiscalCalendarDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCalendar(Guid id, [FromBody] UpdateFiscalCalendarDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var updated = await _service.UpdateAsync(id, request, userId);
            return Ok(new ApiResponse<FiscalCalendarDto> { Success = true, Message = "Fiscal year updated", Data = updated });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation updating fiscal calendar");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating fiscal calendar");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete a fiscal year (blocked if closed or it contains transactions).
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCalendar(Guid id)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            await _service.DeleteAsync(id, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation deleting fiscal calendar");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting fiscal calendar");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Close a fiscal year — it becomes read-only.
    /// </summary>
    [HttpPost("{id}/close")]
    [ProducesResponseType(typeof(ApiResponse<FiscalCalendarDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CloseCalendar(Guid id)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CloseAsync(id, userId);
            return Ok(new ApiResponse<FiscalCalendarDto> { Success = true, Message = "Fiscal year closed", Data = result });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation closing fiscal calendar");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing fiscal calendar");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Reopen a closed fiscal year.
    /// </summary>
    [HttpPost("{id}/reopen")]
    [ProducesResponseType(typeof(ApiResponse<FiscalCalendarDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReopenCalendar(Guid id)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.ReopenAsync(id, userId);
            return Ok(new ApiResponse<FiscalCalendarDto> { Success = true, Message = "Fiscal year reopened", Data = result });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation reopening fiscal calendar");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reopening fiscal calendar");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get fiscal periods for calendar
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpGet("{calendarId}/periods")]
    [ProducesResponseType(typeof(PaginatedResponse<FiscalPeriodDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCalendarPeriods(Guid calendarId, [FromQuery] PaginationParams pagination)
    {
        try
        {
            var response = await _service.GetCalendarPeriodsAsync(calendarId, pagination);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Calendar not found or invalid tenant context");
            return NotFound(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving fiscal periods");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }
}
