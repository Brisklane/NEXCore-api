using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nexcore.SharedKernel.Api;
using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;

namespace Hr.Api.Controllers;

[ApiController]
[Route("api/v1/hr/[controller]")]
[Produces("application/json")]
[Authorize]
public class LookupController : ControllerBase
{
    private readonly ILookupService _service;
    private readonly ILogger<LookupController> _logger;

    public LookupController(ILookupService service, ILogger<LookupController> logger)
    { _service = service; _logger = logger; }

    [HttpGet("types")]
    public async Task<IActionResult> GetAllTypes()
    {
        try { return Ok(new ApiResponse<IEnumerable<LookupTypeDto>> { Success = true, Data = await _service.GetAllTypesAsync() }); }
        catch (InvalidOperationException ex) { return Unauthorized(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving lookup types"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("types/{id}")]
    public async Task<IActionResult> GetTypeById(Guid id)
    {
        try
        {
            var item = await _service.GetTypeByIdAsync(id);
            if (item == null) return NotFound(new ApiErrorResponse { Message = "Lookup type not found" });
            return Ok(new ApiResponse<LookupTypeDto> { Success = true, Data = item });
        }
        catch (InvalidOperationException ex) { return Unauthorized(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving lookup type {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPost("types")]
    public async Task<IActionResult> CreateType([FromBody] CreateLookupTypeDto request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
            var item = await _service.CreateTypeAsync(request, userId);
            return CreatedAtAction(nameof(GetTypeById), new { id = item.Id }, new ApiResponse<LookupTypeDto> { Success = true, Data = item });
        }
        catch (InvalidOperationException ex) { return Unauthorized(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creating lookup type"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpDelete("types/{id}")]
    public async Task<IActionResult> DeleteType(Guid id)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
            await _service.DeleteTypeAsync(id, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting lookup type {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("types/{typeId}/values")]
    public async Task<IActionResult> GetValuesByTypeId(Guid typeId)
    {
        try { return Ok(new ApiResponse<IEnumerable<LookupValueDto>> { Success = true, Data = await _service.GetValuesByTypeIdAsync(typeId) }); }
        catch (InvalidOperationException ex) { return Unauthorized(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving lookup values for type {TypeId}", typeId); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("values/{id}")]
    public async Task<IActionResult> GetValueById(Guid id)
    {
        try
        {
            var item = await _service.GetValueByIdAsync(id);
            if (item == null) return NotFound(new ApiErrorResponse { Message = "Lookup value not found" });
            return Ok(new ApiResponse<LookupValueDto> { Success = true, Data = item });
        }
        catch (InvalidOperationException ex) { return Unauthorized(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving lookup value {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPost("values")]
    public async Task<IActionResult> CreateValue([FromBody] CreateLookupValueDto request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
            var item = await _service.CreateValueAsync(request, userId);
            return CreatedAtAction(nameof(GetValueById), new { id = item.Id }, new ApiResponse<LookupValueDto> { Success = true, Data = item });
        }
        catch (InvalidOperationException ex) { return Unauthorized(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creating lookup value"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpDelete("values/{id}")]
    public async Task<IActionResult> DeleteValue(Guid id)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
            await _service.DeleteValueAsync(id, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting lookup value {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}
