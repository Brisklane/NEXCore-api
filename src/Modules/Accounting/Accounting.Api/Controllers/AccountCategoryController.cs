using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;
using Accounting.Application.DTOs;
using Accounting.Application.Services.Interfaces;

namespace Accounting.Api.Controllers;

/// <summary>
/// Account Category management controller
/// Tenant context (CompanyId, BranchId, BusinessUnitId) automatically extracted and applied at service/repository level
/// All queries automatically filtered by tenant context
/// Business logic has been moved to IAccountCategoryService layer
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class AccountCategoryController : ControllerBase
{
    private readonly IAccountCategoryService _service;
    private readonly ILogger<AccountCategoryController> _logger;

    public AccountCategoryController(IAccountCategoryService service, ILogger<AccountCategoryController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Get all active account categories for current tenant
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<AccountCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAllCategories([FromQuery] PaginationParams pagination)
    {
        try
        {
            var response = await _service.GetActiveAsync(pagination);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid tenant context");
            return Unauthorized(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving account categories");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get category by ID
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<AccountCategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCategoryById(Guid id)
    {
        try
        {
            var category = await _service.GetByIdAsync(id);
            if (category == null)
                return NotFound(new ApiErrorResponse { Message = "Category not found" });

            return Ok(new ApiResponse<AccountCategoryDto>
            {
                Success = true,
                Data = category
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid tenant context");
            return Unauthorized(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving account category");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }
}
