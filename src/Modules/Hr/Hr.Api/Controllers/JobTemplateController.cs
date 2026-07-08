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
public class JobTemplateController : ControllerBase
{
    private readonly IJobTemplateService _service;
    private readonly ILogger<JobTemplateController> _logger;

    public JobTemplateController(IJobTemplateService service, ILogger<JobTemplateController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var items = await _service.GetAllAsync();
            return Ok(new ApiResponse<IEnumerable<JobTemplateDto>> { Success = true, Data = items });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid tenant context");
            return Unauthorized(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job templates");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound(new ApiErrorResponse { Message = "Job template not found" });
            return Ok(new ApiResponse<JobTemplateDto> { Success = true, Data = item });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid tenant context");
            return Unauthorized(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving job template");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateJobTemplateDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, new ApiResponse<JobTemplateDto> { Success = true, Data = result, Message = "Job template created" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation during job template creation");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating job template");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateJobTemplateDto request)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateAsync(id, request, userId);
            return Ok(new ApiResponse<JobTemplateDto> { Success = true, Data = result, Message = "Job template updated" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Job template not found");
            return NotFound(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating job template");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }

    [HttpDelete("{id}")]
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
            _logger.LogWarning(ex, "Error deleting job template");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting job template");
            return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" });
        }
    }
}
