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
public class CandidateTaskController : ControllerBase
{
    private readonly ICandidateTaskService _service;
    private readonly ILogger<CandidateTaskController> _logger;

    public CandidateTaskController(ICandidateTaskService service, ILogger<CandidateTaskController> logger)
    { _service = service; _logger = logger; }

    /// <summary>Get all candidate tasks</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<CandidateTaskDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        try { return Ok(new ApiResponse<IEnumerable<CandidateTaskDto>> { Success = true, Data = await _service.GetAllAsync() }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving candidate tasks"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    /// <summary>Get candidate task by ID</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<CandidateTaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var data = await _service.GetByIdAsync(id);
            if (data is null) return NotFound(new ApiErrorResponse { Message = "Candidate task not found" });
            return Ok(new ApiResponse<CandidateTaskDto> { Success = true, Data = data });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving candidate task"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    /// <summary>Get candidate tasks by application ID</summary>
    [HttpGet("by-application/{applicationId}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<CandidateTaskDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByApplicationId(Guid applicationId)
    {
        try
        {
            var data = await _service.GetByApplicationIdAsync(applicationId);
            return Ok(new ApiResponse<IEnumerable<CandidateTaskDto>> { Success = true, Data = data });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving tasks for application"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    /// <summary>Get candidate tasks by candidate ID</summary>
    [HttpGet("by-candidate/{candidateId}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<CandidateTaskDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCandidateId(Guid candidateId)
    {
        try
        {
            var data = await _service.GetByCandidateIdAsync(candidateId);
            return Ok(new ApiResponse<IEnumerable<CandidateTaskDto>> { Success = true, Data = data });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving tasks for candidate"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    /// <summary>Create a new candidate task</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CandidateTaskDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateCandidateTaskDto request)
    {
        try
        {
            var result = await _service.CreateAsync(request, TenantContextHelper.ExtractUserId(User));
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, new ApiResponse<CandidateTaskDto> { Success = true, Message = "Candidate task created successfully", Data = result });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creating candidate task"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    /// <summary>Update an existing candidate task</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<CandidateTaskDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCandidateTaskDto request)
    {
        try
        {
            var result = await _service.UpdateAsync(id, request, TenantContextHelper.ExtractUserId(User));
            return Ok(new ApiResponse<CandidateTaskDto> { Success = true, Message = "Candidate task updated successfully", Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating candidate task"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    /// <summary>Delete a candidate task</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try { await _service.DeleteAsync(id, TenantContextHelper.ExtractUserId(User)); return NoContent(); }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting candidate task"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}
