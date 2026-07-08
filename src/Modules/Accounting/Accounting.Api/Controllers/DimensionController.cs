using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;
using Accounting.Application.DTOs;
using Accounting.Application.Services.Interfaces;

namespace Accounting.Api.Controllers;

/// <summary>
/// Dimension management controller
/// Manages analytical dimensions (cost centers, departments, etc.)
/// Tenant context (CompanyId, BranchId, BusinessUnitId) automatically extracted and applied at service/repository level
/// Business logic has been moved to IDimensionService layer
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class DimensionController : ControllerBase
{
    private readonly IDimensionService _service;
    private readonly ILogger<DimensionController> _logger;

    public DimensionController(IDimensionService service, ILogger<DimensionController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Get dimension by ID
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<DimensionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDimensionById(Guid id)
    {
        try
        {
            var dimension = await _service.GetByIdAsync(id);
            if (dimension == null)
                return NotFound(new ApiErrorResponse { Message = "Dimension not found" });

            return Ok(new ApiResponse<DimensionDto>
            {
                Success = true,
                Data = dimension
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid tenant context");
            return Unauthorized(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dimension");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get dimension by code
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpGet("code/{code}")]
    [ProducesResponseType(typeof(ApiResponse<DimensionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDimensionByCode(string code)
    {
        try
        {
            var dimension = await _service.GetByCodeAsync(code);
            if (dimension == null)
                return NotFound(new ApiErrorResponse { Message = "Dimension not found" });

            return Ok(new ApiResponse<DimensionDto>
            {
                Success = true,
                Data = dimension
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid tenant context");
            return Unauthorized(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dimension");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get all dimensions for current tenant
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<DimensionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllDimensions([FromQuery] PaginationParams pagination)
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
            _logger.LogError(ex, "Error retrieving dimensions");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Create a new dimension
    /// Tenant context (CompanyId, BranchId, BusinessUnitId) automatically populated by service/repository
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<DimensionDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateDimension([FromBody] CreateDimensionDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var response = await _service.CreateAsync(request, userId);

            return CreatedAtAction(nameof(GetDimensionById), new { id = response.Id }, new ApiResponse<DimensionDto>
            {
                Success = true,
                Message = "Dimension created successfully",
                Data = response
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Dimension code already exists");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating dimension");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Update dimension
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<DimensionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateDimension(Guid id, [FromBody] UpdateDimensionDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var response = await _service.UpdateAsync(id, request, userId);

            return Ok(new ApiResponse<DimensionDto>
            {
                Success = true,
                Message = "Dimension updated successfully",
                Data = response
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Dimension not found");
            return NotFound(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating dimension");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete dimension (soft delete)
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteDimension(Guid id)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            await _service.DeleteAsync(id, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error during dimension deletion");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting dimension");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Get dimension values for a dimension
    /// Tenant filtering automatic at service/repository level
    /// </summary>
    [HttpGet("{dimensionId}/values")]
    [ProducesResponseType(typeof(PaginatedResponse<DimensionValueDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDimensionValues(Guid dimensionId, [FromQuery] PaginationParams pagination)
    {
        try
        {
            var response = await _service.GetDimensionValuesAsync(dimensionId, pagination);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Dimension not found");
            return NotFound(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dimension values");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }
}
