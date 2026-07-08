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
public class JobLocationController : ControllerBase
{
    private readonly IJobLocationService _service;
    private readonly ILogger<JobLocationController> _logger;

    public JobLocationController(IJobLocationService service, ILogger<JobLocationController> logger)
    { _service = service; _logger = logger; }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<JobLocationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        try { return Ok(new ApiResponse<IEnumerable<JobLocationDto>> { Success = true, Data = await _service.GetAllAsync() }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving job locations"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<JobLocationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var data = await _service.GetByIdAsync(id);
            if (data is null) return NotFound(new ApiErrorResponse { Message = "Job location not found" });
            return Ok(new ApiResponse<JobLocationDto> { Success = true, Data = data });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving job location"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<JobLocationDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateJobLocationDto request)
    {
        try
        {
            var result = await _service.CreateAsync(request, TenantContextHelper.ExtractUserId(User));
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, new ApiResponse<JobLocationDto> { Success = true, Message = "Job location created successfully", Data = result });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creating job location"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<JobLocationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateJobLocationDto request)
    {
        try
        {
            var result = await _service.UpdateAsync(id, request, TenantContextHelper.ExtractUserId(User));
            return Ok(new ApiResponse<JobLocationDto> { Success = true, Message = "Job location updated successfully", Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating job location"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try { await _service.DeleteAsync(id, TenantContextHelper.ExtractUserId(User)); return NoContent(); }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting job location"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}
