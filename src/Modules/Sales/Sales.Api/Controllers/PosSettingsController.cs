using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Sales.Application.DTOs;
using Sales.Application.Services.Interfaces;

namespace Sales.Api.Controllers;

/// <summary>
/// Branch-level POS configuration.
/// One settings row per company/branch/business-unit.
/// All terminals in the same branch share these settings.
/// </summary>
[ApiController]
[Route("api/sales/[controller]")]
[Authorize]
public class PosSettingsController : ControllerBase
{
    private readonly IPosSettingsService _service;
    private readonly ILogger<PosSettingsController> _logger;

    public PosSettingsController(IPosSettingsService service, ILogger<PosSettingsController> logger)
    {
        _service = service;
        _logger  = logger;
    }

    /// <summary>
    /// Returns the POS settings for the current branch.
    /// If no settings row exists yet, one is auto-created with factory defaults.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PosSettingsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get()
    {
        try
        {
            var dto = await _service.GetAsync();
            return Ok(new ApiResponse<PosSettingsDto> { Success = true, Data = dto });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving POS settings");
            return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving POS settings" });
        }
    }

    /// <summary>
    /// Updates POS settings (PATCH semantics — only supplied fields are changed).
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<PosSettingsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update([FromBody] UpdatePosSettingsDto dto)
    {
        try
        {
            var result = await _service.UpdateAsync(dto);
            return Ok(new ApiResponse<PosSettingsDto>
            {
                Success = true,
                Data    = result,
                Message = "POS settings updated",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating POS settings");
            return StatusCode(500, new ApiErrorResponse { Message = "Error updating POS settings" });
        }
    }

    /// <summary>
    /// Resets all POS settings to factory defaults.
    /// </summary>
    [HttpPost("reset")]
    [ProducesResponseType(typeof(ApiResponse<PosSettingsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Reset()
    {
        try
        {
            var result = await _service.ResetAsync();
            return Ok(new ApiResponse<PosSettingsDto>
            {
                Success = true,
                Data    = result,
                Message = "POS settings reset to factory defaults",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting POS settings");
            return StatusCode(500, new ApiErrorResponse { Message = "Error resetting POS settings" });
        }
    }
}
