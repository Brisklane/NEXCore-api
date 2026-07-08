using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;
using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;

namespace Hr.Api.Controllers;

/// <summary>
/// Interview management controller - CRUD for interview scheduling
/// Tenant context automatically extracted and applied at service/repository level
/// </summary>
[ApiController]
[Route("api/v1/hr/[controller]")]
[Produces("application/json")]
[Authorize]
public class InterviewController : ControllerBase
{
    private readonly IInterviewService _service;
    private readonly ILogger<InterviewController> _logger;

    public InterviewController(IInterviewService service, ILogger<InterviewController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Get all interviews for the current tenant</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<InterviewDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var interviews = await _service.GetAllAsync();
            return Ok(new ApiResponse<IEnumerable<InterviewDto>> { Success = true, Data = interviews });
        }
        catch (InvalidOperationException ex) { return Unauthorized(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving interviews"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    /// <summary>Get interview by ID</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<InterviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var interview = await _service.GetByIdAsync(id);
            if (interview == null) return NotFound(new ApiErrorResponse { Message = "Interview not found" });
            return Ok(new ApiResponse<InterviewDto> { Success = true, Data = interview });
        }
        catch (InvalidOperationException ex) { return Unauthorized(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving interview"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    /// <summary>Get interviews by application</summary>
    [HttpGet("by-application/{applicationId}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<InterviewDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByApplication(Guid applicationId)
    {
        try
        {
            var interviews = await _service.GetByApplicationIdAsync(applicationId);
            return Ok(new ApiResponse<IEnumerable<InterviewDto>> { Success = true, Data = interviews });
        }
        catch (InvalidOperationException ex) { return Unauthorized(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving interviews by application"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    /// <summary>Create a new interview</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<InterviewDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateInterviewDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id },
                new ApiResponse<InterviewDto> { Success = true, Message = "Interview created successfully", Data = result });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creating interview"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    /// <summary>Update an existing interview</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<InterviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateInterviewDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateAsync(id, request, userId);
            return Ok(new ApiResponse<InterviewDto> { Success = true, Message = "Interview updated successfully", Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating interview"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    /// <summary>Delete an interview (soft delete)</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            await _service.DeleteAsync(id, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting interview"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}
