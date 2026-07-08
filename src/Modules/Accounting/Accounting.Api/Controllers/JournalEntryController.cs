using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;
using Accounting.Application.DTOs;
using Accounting.Application.Services.Interfaces;

namespace Accounting.Api.Controllers;

/// <summary>
/// Journal Entry management controller
/// Tenant context (CompanyId, BranchId, BusinessUnitId) automatically extracted and applied at service/repository level
/// All queries automatically filtered by tenant context
/// Business logic has been moved to IJournalEntryService layer
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class JournalEntryController : ControllerBase
{
    private readonly IJournalEntryService _service;
    private readonly ILogger<JournalEntryController> _logger;

    public JournalEntryController(IJournalEntryService service, ILogger<JournalEntryController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Create a new journal entry with lines
    /// Tenant context (CompanyId, BranchId, BusinessUnitId) automatically populated by service/repository
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<JournalEntryDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateJournalEntry([FromBody] CreateJournalEntryDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var response = await _service.CreateAsync(request, userId);

            return CreatedAtAction(nameof(GetJournalEntryById), new { id = response.Id }, new ApiResponse<JournalEntryDto>
            {
                Success = true,
                Message = "Journal entry created successfully",
                Data = response
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation during journal entry creation");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating journal entry");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get journal entry by ID
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<JournalEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetJournalEntryById(Guid id)
    {
        try
        {
            var entry = await _service.GetByIdAsync(id);
            if (entry == null)
                return NotFound(new ApiErrorResponse { Message = "Journal entry not found" });

            return Ok(new ApiResponse<JournalEntryDto>
            {
                Success = true,
                Data = entry
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid tenant context");
            return Unauthorized(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving journal entry");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get journal entries by ledger
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpGet("ledger/{ledgerId}")]
    [ProducesResponseType(typeof(PaginatedResponse<JournalEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetByLedger(Guid ledgerId, [FromQuery] PaginationParams pagination)
    {
        try
        {
            var response = await _service.GetByLedgerIdAsync(ledgerId, pagination);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid tenant context");
            return Unauthorized(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving journal entries");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get journal entries by date range
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpGet("by-date-range")]
    [ProducesResponseType(typeof(PaginatedResponse<JournalEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetByDateRange(
        [FromQuery] DateTime fromDate,
        [FromQuery] DateTime toDate,
        [FromQuery] PaginationParams pagination)
    {
        try
        {
            var response = await _service.GetByDateRangeAsync(fromDate, toDate, pagination);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid tenant context");
            return Unauthorized(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving journal entries");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get journal entries by status
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpGet("status/{status}")]
    [ProducesResponseType(typeof(PaginatedResponse<JournalEntryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByStatus(string status, [FromQuery] PaginationParams pagination)
    {
        try
        {
            var response = await _service.GetByStatusAsync(status, pagination);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid status value");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving journal entries by status");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update journal entry (only in Draft status)
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<JournalEntryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateJournalEntry(Guid id, [FromBody] UpdateJournalEntryDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var response = await _service.UpdateAsync(id, request, userId);

            return Ok(new ApiResponse<JournalEntryDto>
            {
                Success = true,
                Message = "Journal entry updated successfully",
                Data = response
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation during journal entry update");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating journal entry");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete a journal entry (Draft only). Soft-deletes the entry.
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteJournalEntry(Guid id)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            await _service.DeleteAsync(id, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation during journal entry deletion");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting journal entry");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Submit journal entry for approval (Draft ? Submitted)
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpPost("{id}/submit")]
    [ProducesResponseType(typeof(ApiResponse<JournalEntryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SubmitJournalEntry(Guid id)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var response = await _service.SubmitAsync(id, userId);

            return Ok(new ApiResponse<JournalEntryDto>
            {
                Success = true,
                Message = "Journal entry submitted for approval",
                Data = response
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation during journal entry submission");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting journal entry");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Post journal entry to GL (ApprovedByFinance ? Posted)
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpPost("{id}/post")]
    [ProducesResponseType(typeof(ApiResponse<JournalEntryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> PostJournalEntry(Guid id)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var response = await _service.PostAsync(id, userId);

            return Ok(new ApiResponse<JournalEntryDto>
            {
                Success = true,
                Message = "Journal entry posted to general ledger",
                Data = response
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation during journal entry posting");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error posting journal entry");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Reverse/Void journal entry (creates reversal entry)
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpPost("{id}/reverse")]
    [ProducesResponseType(typeof(ApiResponse<JournalEntryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReverseJournalEntry(Guid id)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var response = await _service.ReverseAsync(id, userId);

            return Ok(new ApiResponse<JournalEntryDto>
            {
                Success = true,
                Message = "Journal entry reversed successfully",
                Data = response
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation during journal entry reversal");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reversing journal entry");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }
}
