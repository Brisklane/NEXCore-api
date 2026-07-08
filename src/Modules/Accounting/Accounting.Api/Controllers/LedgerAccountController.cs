using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;
using Accounting.Application.DTOs;
using Accounting.Application.Services.Interfaces;

namespace Accounting.Api.Controllers;

/// <summary>
/// Ledger Account management controller
/// Provides complete CRUD operations for ledger accounts (Chart of Accounts)
/// Tenant context (CompanyId, BranchId, BusinessUnitId) is automatically extracted and applied at service/repository level
/// All queries are automatically filtered by tenant context
/// Business logic has been moved to ILedgerAccountService layer
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class LedgerAccountController : ControllerBase
{
    private readonly ILedgerAccountService _service;
    private readonly ILogger<LedgerAccountController> _logger;

    public LedgerAccountController(ILedgerAccountService service, ILogger<LedgerAccountController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Create a new ledger account
    /// Tenant context (CompanyId, BranchId, BusinessUnitId) automatically populated by service/repository
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<LedgerAccountDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateAccount([FromBody] CreateLedgerAccountDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var response = await _service.CreateAsync(request, userId);

            return CreatedAtAction(nameof(GetAccountById), new { id = response.Id }, new ApiResponse<LedgerAccountDto>
            {
                Success = true,
                Message = "Ledger account created successfully",
                Data = response
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation during account creation");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ledger account");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get all accounts for this tenant (for dropdowns/pickers)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<LedgerAccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAllAccounts([FromQuery] PaginationParams pagination)
    {
        try
        {
            var response = await _service.GetAllAsync(pagination);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ledger accounts");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get all accounts for a specific ledger
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpGet("by-ledger/{ledgerId}")]
    [ProducesResponseType(typeof(PaginatedResponse<LedgerAccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAccountsByLedger(Guid ledgerId, [FromQuery] PaginationParams pagination)
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
            _logger.LogError(ex, "Error retrieving accounts");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get account by ID
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<LedgerAccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAccountById(Guid id)
    {
        try
        {
            var account = await _service.GetByIdAsync(id);
            if (account == null)
                return NotFound(new ApiErrorResponse { Message = "Account not found" });

            return Ok(new ApiResponse<LedgerAccountDto>
            {
                Success = true,
                Data = account
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid tenant context");
            return Unauthorized(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving account");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get account by account number for a specific ledger
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpGet("by-number/{accountNumber}")]
    [ProducesResponseType(typeof(ApiResponse<LedgerAccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAccountByNumber(string accountNumber)
    {
        try
        {
            var account = await _service.GetByAccountNumberAsync(accountNumber);
            if (account == null)
                return NotFound(new ApiErrorResponse { Message = "Account not found" });

            return Ok(new ApiResponse<LedgerAccountDto>
            {
                Success = true,
                Data = account
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid tenant context");
            return Unauthorized(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving account");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get accounts by category for a specific ledger
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpGet("by-category/{categoryId}")]
    [ProducesResponseType(typeof(PaginatedResponse<LedgerAccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAccountsByCategory(Guid categoryId, [FromQuery] Guid ledgerId, [FromQuery] PaginationParams pagination)
    {
        try
        {
            var response = await _service.GetByCategoryIdAsync(categoryId, ledgerId, pagination);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid tenant context");
            return Unauthorized(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving accounts");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get subledger accounts by master account for a specific ledger
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpGet("{ledgerId}/subledger/{masterAccountId}")]
    [ProducesResponseType(typeof(PaginatedResponse<LedgerAccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSubledgerAccounts(Guid ledgerId, Guid masterAccountId, [FromQuery] PaginationParams pagination)
    {
        try
        {
            var response = await _service.GetSubledgerAccountsAsync(ledgerId, masterAccountId, pagination);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Master account not found");
            return NotFound(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subledger accounts");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update ledger account
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<LedgerAccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateAccount(Guid id, [FromBody] UpdateLedgerAccountDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var response = await _service.UpdateAsync(id, request, userId);

            return Ok(new ApiResponse<LedgerAccountDto>
            {
                Success = true,
                Message = "Account updated successfully",
                Data = response
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Account not found");
            return NotFound(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating account");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete account (soft delete)
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteAccount(Guid id)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            await _service.DeleteAsync(id, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error during account deletion");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting account");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Activate account
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpPost("{id}/activate")]
    [ProducesResponseType(typeof(ApiResponse<LedgerAccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ActivateAccount(Guid id)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var response = await _service.ActivateAsync(id, userId);

            return Ok(new ApiResponse<LedgerAccountDto>
            {
                Success = true,
                Message = "Account activated successfully",
                Data = response
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Account not found");
            return NotFound(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating account");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Deactivate account
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpPost("{id}/deactivate")]
    [ProducesResponseType(typeof(ApiResponse<LedgerAccountDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeactivateAccount(Guid id)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var response = await _service.DeactivateAsync(id, userId);

            return Ok(new ApiResponse<LedgerAccountDto>
            {
                Success = true,
                Message = "Account deactivated successfully",
                Data = response
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Account not found");
            return NotFound(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating account");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }
}
