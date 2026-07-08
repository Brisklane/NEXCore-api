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
public class DesignationController : ControllerBase
{
    private readonly IDesignationService _service;
    private readonly ILogger<DesignationController> _logger;

    public DesignationController(IDesignationService service, ILogger<DesignationController> logger)
    { _service = service; _logger = logger; }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<DesignationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        try { return Ok(new ApiResponse<IEnumerable<DesignationDto>> { Success = true, Data = await _service.GetAllAsync() }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving designations"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<DesignationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var data = await _service.GetByIdAsync(id);
            if (data is null) return NotFound(new ApiErrorResponse { Message = "Designation not found" });
            return Ok(new ApiResponse<DesignationDto> { Success = true, Data = data });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving designation"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<DesignationDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateDesignationDto request)
    {
        try
        {
            var result = await _service.CreateAsync(request, TenantContextHelper.ExtractUserId(User));
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, new ApiResponse<DesignationDto> { Success = true, Message = "Designation created successfully", Data = result });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creating designation"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<DesignationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDesignationDto request)
    {
        try
        {
            var result = await _service.UpdateAsync(id, request, TenantContextHelper.ExtractUserId(User));
            return Ok(new ApiResponse<DesignationDto> { Success = true, Message = "Designation updated successfully", Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating designation"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try { await _service.DeleteAsync(id, TenantContextHelper.ExtractUserId(User)); return NoContent(); }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting designation"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    /// <summary>
    /// Returns active designations as lightweight id/label pairs for use in dropdowns.
    /// Label = DesignationName, Value = Designation Id.
    /// Results are sorted alphabetically.
    /// </summary>
    [HttpGet("lookup")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<HrLookupItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLookupList()
    {
        try
        {
            var data = await _service.GetLookupListAsync();
            return Ok(new ApiResponse<IEnumerable<HrLookupItemDto>> { Success = true, Data = data });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving designation lookup list"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}
