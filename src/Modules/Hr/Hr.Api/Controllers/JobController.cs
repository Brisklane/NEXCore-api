using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;
using Nexcore.SharedKernel.Enums;
using Hr.Application.DTOs;
using Hr.Application.Services.Interfaces;

namespace Hr.Api.Controllers;

/// <summary>
/// Job management controller - CRUD for job requisitions and postings
/// Tenant context automatically extracted and applied at service/repository level
/// </summary>
[ApiController]
[Route("api/v1/hr/[controller]")]
[Produces("application/json")]
[Authorize]
public class JobController : ControllerBase
{
    private readonly IJobService _service;
    private readonly ILogger<JobController> _logger;

    public JobController(IJobService service, ILogger<JobController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Get all jobs for the current tenant</summary>
    /// <param name="recordType">Optional filter: 0 = Job, 1 = Requisition. Omit to return all.</param>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<JobDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll([FromQuery] JobRecordType? recordType = null)
    {
        try
        {
            var jobs = await _service.GetAllAsync(recordType);
            return Ok(new ApiResponse<IEnumerable<JobDto>> { Success = true, Data = jobs });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid tenant context");
            return Unauthorized(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving jobs");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>Get job by ID</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<JobDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var job = await _service.GetByIdAsync(id);
            if (job == null) return NotFound(new ApiErrorResponse { Message = "Job not found" });
            return Ok(new ApiResponse<JobDto> { Success = true, Data = job });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid tenant context");
            return Unauthorized(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>Create a new job</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<JobDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateJobDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id },
                new ApiResponse<JobDto> { Success = true, Message = "Job created successfully", Data = result });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation during job creation");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating job");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>Update an existing job</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<JobDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateJobDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateAsync(id, request, userId);
            return Ok(new ApiResponse<JobDto> { Success = true, Message = "Job updated successfully", Data = result });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Job not found");
            return NotFound(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating job");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    /// <summary>Delete a job (soft delete)</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            await _service.DeleteAsync(id, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Error deleting job");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting job");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }
}
