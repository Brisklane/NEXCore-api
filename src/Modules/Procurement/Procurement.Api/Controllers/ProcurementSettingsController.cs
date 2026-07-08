using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Procurement.Application.DTOs;
using Procurement.Application.Services.Interfaces;

namespace Procurement.Api.Controllers;

/// <summary>Procurement Settings — company-wide policy configuration (3-way match, approval thresholds, default terms).</summary>
[ApiController]
[Route("api/v1/procurement/settings")]
[Produces("application/json")]
[Authorize]
public class ProcurementSettingsController : ControllerBase
{
    private readonly IProcurementSettingsService _service;
    private readonly ILogger<ProcurementSettingsController> _logger;

    public ProcurementSettingsController(IProcurementSettingsService service, ILogger<ProcurementSettingsController> logger)
    {
        _service = service;
        _logger  = logger;
    }

    /// <summary>Get the current procurement settings for this company.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<ProcurementSettingsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get()
    {
        try
        {
            var settings = await _service.GetAsync();
            if (settings is null) return NotFound(new ApiErrorResponse { Message = "Procurement settings not initialised for this company" });
            return Ok(new ApiResponse<ProcurementSettingsDto> { Success = true, Data = settings });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving procurement settings"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving settings" }); }
    }

    /// <summary>Update procurement settings (3-way match toggle, approval workflows, payment terms defaults, etc.).</summary>
    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<ProcurementSettingsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromBody] UpdateProcurementSettingsDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });
            var settings = await _service.UpdateAsync(dto);
            return Ok(new ApiResponse<ProcurementSettingsDto> { Success = true, Data = settings, Message = "Procurement settings updated" });
        }
        catch (KeyNotFoundException) { return NotFound(new ApiErrorResponse { Message = "Procurement settings not found — run company initialisation first" }); }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating procurement settings"); return StatusCode(500, new ApiErrorResponse { Message = "Error updating settings" }); }
    }
}
