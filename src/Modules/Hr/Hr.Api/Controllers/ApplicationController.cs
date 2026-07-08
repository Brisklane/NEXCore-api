using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;
using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;

namespace Hr.Api.Controllers;

/// <summary>
/// Application management controller - CRUD for job applications
/// Tenant context automatically extracted and applied at service/repository level
/// </summary>
[ApiController]
[Route("api/v1/hr/[controller]")]
[Produces("application/json")]
[Authorize]
public class ApplicationController : ControllerBase
{
    private readonly IApplicationService _service;
    private readonly ILogger<ApplicationController> _logger;

    public ApplicationController(IApplicationService service, ILogger<ApplicationController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Get all applications for the current tenant</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ApplicationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var apps = await _service.GetAllAsync();
            return Ok(new ApiResponse<IEnumerable<ApplicationDto>> { Success = true, Data = apps });
        }
        catch (InvalidOperationException ex) { return Unauthorized(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving applications"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    /// <summary>Get application by ID</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ApplicationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var app = await _service.GetByIdAsync(id);
            if (app == null) return NotFound(new ApiErrorResponse { Message = "Application not found" });
            return Ok(new ApiResponse<ApplicationDto> { Success = true, Data = app });
        }
        catch (InvalidOperationException ex) { return Unauthorized(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving application"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    /// <summary>Get applications by job</summary>
    [HttpGet("by-job/{jobId}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ApplicationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByJob(Guid jobId)
    {
        try
        {
            var apps = await _service.GetByJobIdAsync(jobId);
            return Ok(new ApiResponse<IEnumerable<ApplicationDto>> { Success = true, Data = apps });
        }
        catch (InvalidOperationException ex) { return Unauthorized(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving applications by job"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    /// <summary>Get applications by candidate</summary>
    [HttpGet("by-candidate/{candidateId}")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ApplicationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCandidate(Guid candidateId)
    {
        try
        {
            var apps = await _service.GetByCandidateIdAsync(candidateId);
            return Ok(new ApiResponse<IEnumerable<ApplicationDto>> { Success = true, Data = apps });
        }
        catch (InvalidOperationException ex) { return Unauthorized(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving applications by candidate"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    /// <summary>Create a new application</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ApplicationDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateApplicationDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id },
                new ApiResponse<ApplicationDto> { Success = true, Message = "Application created successfully", Data = result });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creating application"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    /// <summary>Update an existing application</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ApplicationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateApplicationDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateAsync(id, request, userId);
            return Ok(new ApiResponse<ApplicationDto> { Success = true, Message = "Application updated successfully", Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating application"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    /// <summary>Delete an application (soft delete)</summary>
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
        catch (Exception ex) { _logger.LogError(ex, "Error deleting application"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}
