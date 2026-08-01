using Crm.Application.DTOs;
using Crm.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;

namespace Crm.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class ForecastsController : ControllerBase
{
    private readonly IForecastService _service;
    private readonly ILogger<ForecastsController> _logger;

    public ForecastsController(IForecastService service, ILogger<ForecastsController> logger)
    { _service = service; _logger = logger; }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ForecastDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await _service.GetByIdAsync(id);
            if (result is null) return NotFound(new ApiErrorResponse { Message = "Forecast not found" });
            return Ok(new ApiResponse<ForecastDto> { Success = true, Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving forecast {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("by-user/{userId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ForecastDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByUser(Guid userId, [FromQuery] int fiscalYear = 0, [FromQuery] int fiscalQuarter = 0)
    {
        try { var result = await _service.GetByUserAsync(userId, fiscalYear, fiscalQuarter); return Ok(new ApiResponse<IEnumerable<ForecastDto>> { Success = true, Data = result }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving forecasts"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ForecastDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateForecastDto dto)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CreateAsync(dto, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, new ApiResponse<ForecastDto> { Success = true, Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating forecast"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ForecastDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateForecastDto dto)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateAsync(id, dto, userId);
            return Ok(new ApiResponse<ForecastDto> { Success = true, Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating forecast {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPost("{id:guid}/submit")]
    [ProducesResponseType(typeof(ApiResponse<ForecastDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Submit(Guid id)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.SubmitAsync(id, userId);
            return Ok(new ApiResponse<ForecastDto> { Success = true, Data = result, Message = "Forecast submitted" });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error submitting forecast {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
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
        catch (Exception ex) { _logger.LogError(ex, "Error deleting forecast {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}
