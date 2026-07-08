using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;
using Accounting.Application.DTOs;
using Accounting.Application.Services.Interfaces;

namespace Accounting.Api.Controllers;

/// <summary>
/// Account Balance reporting controller
/// Provides balance information for accounts by period
/// Tenant context (CompanyId, BranchId, BusinessUnitId) automatically extracted and applied at service/repository level
/// Business logic has been moved to IAccountBalanceService layer
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class AccountBalanceController : ControllerBase
{
    private readonly IAccountBalanceService _service;
    private readonly ILogger<AccountBalanceController> _logger;

    public AccountBalanceController(IAccountBalanceService service, ILogger<AccountBalanceController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Get balances by period (trial balance)
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpGet("period/{periodId}")]
    [ProducesResponseType(typeof(PaginatedResponse<AccountBalanceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTrialBalance(Guid periodId, [FromQuery] PaginationParams pagination)
    {
        try
        {
            var response = await _service.GetByPeriodAsync(periodId, pagination);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid tenant context");
            return Unauthorized(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving trial balance");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get balances by account
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpGet("account/{accountId}")]
    [ProducesResponseType(typeof(PaginatedResponse<AccountBalanceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAccountBalances(Guid accountId, [FromQuery] PaginationParams pagination)
    {
        try
        {
            var response = await _service.GetByAccountIdAsync(accountId, pagination);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid tenant context");
            return Unauthorized(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving account balances");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get account balance by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<AccountBalanceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAccountBalance(Guid id)
    {
        try
        {
            var balance = await _service.GetByIdAsync(id);
            if (balance == null)
                return NotFound(new ApiErrorResponse { Message = "Balance not found" });

            return Ok(new ApiResponse<AccountBalanceDto>
            {
                Success = true,
                Data = balance
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid tenant context");
            return Unauthorized(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving account balance");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Create account balance
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<AccountBalanceDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateAccountBalance([FromBody] CreateAccountBalanceDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var response = await _service.CreateAsync(request, userId);

            return CreatedAtAction(nameof(GetAccountBalance), new { id = response.Id }, new ApiResponse<AccountBalanceDto>
            {
                Success = true,
                Message = "Account balance created successfully",
                Data = response
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation during account balance creation");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating account balance");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update account balance
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<AccountBalanceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateAccountBalance(Guid id, [FromBody] UpdateAccountBalanceDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var response = await _service.UpdateAsync(id, request, userId);

            return Ok(new ApiResponse<AccountBalanceDto>
            {
                Success = true,
                Message = "Account balance updated successfully",
                Data = response
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Account balance not found");
            return NotFound(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating account balance");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete account balance
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteAccountBalance(Guid id)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            await _service.DeleteAsync(id, userId);

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error during account balance deletion");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting account balance");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }
}
