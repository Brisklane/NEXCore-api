using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;
using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;

namespace Hr.Api.Controllers;

[ApiController]
[Route("api/v1/hr/[controller]")]
[Produces("application/json")]
[Authorize]
public class AllowancesProfileController : ControllerBase
{
    private readonly IAllowancesProfileService _service;
    private readonly ILogger<AllowancesProfileController> _logger;

    public AllowancesProfileController(IAllowancesProfileService service, ILogger<AllowancesProfileController> logger)
    { _service = service; _logger = logger; }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<AllowancesProfileDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        try { return Ok(new ApiResponse<IEnumerable<AllowancesProfileDto>> { Success = true, Data = await _service.GetAllAsync() }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving allowances profiles"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<AllowancesProfileDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var data = await _service.GetByIdAsync(id);
            if (data is null) return NotFound(new ApiErrorResponse { Message = "Allowances profile not found" });
            return Ok(new ApiResponse<AllowancesProfileDto> { Success = true, Data = data });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving allowances profile"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<AllowancesProfileDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateAllowancesProfileDto request)
    {
        try
        {
            var result = await _service.CreateAsync(request, TenantContextHelper.ExtractUserId(User));
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, new ApiResponse<AllowancesProfileDto> { Success = true, Message = "Allowances profile created successfully", Data = result });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creating allowances profile"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<AllowancesProfileDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAllowancesProfileDto request)
    {
        try
        {
            var result = await _service.UpdateAsync(id, request, TenantContextHelper.ExtractUserId(User));
            return Ok(new ApiResponse<AllowancesProfileDto> { Success = true, Message = "Allowances profile updated successfully", Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating allowances profile"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try { await _service.DeleteAsync(id, TenantContextHelper.ExtractUserId(User)); return NoContent(); }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting allowances profile"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}
