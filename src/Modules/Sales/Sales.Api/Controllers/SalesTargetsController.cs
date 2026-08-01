using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;
using Sales.Application.DTOs;
using Sales.Application.Services.Interfaces;

namespace Sales.Api.Controllers;

/// <summary>
/// Quota endpoints. Previously served by the CRM module; moved here with SalesTarget itself so
/// commission and forecasting read one record. The route is unchanged, so existing clients keep
/// working — only the owning module moved.
/// </summary>
[ApiController]
[Route("api/v1/sales-targets")]
[Produces("application/json")]
[Authorize]
public class SalesTargetsController : ControllerBase
{
    private readonly ISalesTargetService _service;
    private readonly ILogger<SalesTargetsController> _logger;

    public SalesTargetsController(ISalesTargetService service, ILogger<SalesTargetsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SalesTargetDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await _service.GetByIdAsync(id);
            if (result is null) return NotFound(new ApiErrorResponse { Message = "Sales target not found" });
            return Ok(new ApiResponse<SalesTargetDto> { Success = true, Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving sales target {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    /// <summary>Targets for one sales rep. Route kept as <c>by-user</c> for client compatibility.</summary>
    [HttpGet("by-user/{userId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<SalesTargetDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByUser(Guid userId, [FromQuery] int? fiscalYear = null)
    {
        try { var result = await _service.GetBySalesRepAsync(userId, fiscalYear); return Ok(new ApiResponse<IEnumerable<SalesTargetDto>> { Success = true, Data = result }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving sales targets"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("by-team/{teamId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<SalesTargetDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByTeam(Guid teamId, [FromQuery] int? fiscalYear = null)
    {
        try { var result = await _service.GetByTeamAsync(teamId, fiscalYear); return Ok(new ApiResponse<IEnumerable<SalesTargetDto>> { Success = true, Data = result }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving team sales targets"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("by-territory/{territoryId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<SalesTargetDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByTerritory(Guid territoryId, [FromQuery] int? fiscalYear = null)
    {
        try { var result = await _service.GetByTerritoryAsync(territoryId, fiscalYear); return Ok(new ApiResponse<IEnumerable<SalesTargetDto>> { Success = true, Data = result }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving territory sales targets"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SalesTargetDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateSalesTargetDto dto)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CreateAsync(dto, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, new ApiResponse<SalesTargetDto> { Success = true, Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating sales target"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SalesTargetDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSalesTargetDto dto)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateAsync(id, dto, userId);
            return Ok(new ApiResponse<SalesTargetDto> { Success = true, Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating sales target {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            await _service.DeleteAsync(id, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting sales target {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}
