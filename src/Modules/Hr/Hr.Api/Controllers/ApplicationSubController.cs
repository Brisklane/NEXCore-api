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
public class ApplicationDetailController : ControllerBase
{
    private readonly IApplicationDetailService _service;
    private readonly ILogger<ApplicationDetailController> _logger;

    public ApplicationDetailController(IApplicationDetailService service, ILogger<ApplicationDetailController> logger)
    { _service = service; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try { return Ok(new ApiResponse<IEnumerable<ApplicationDetailDto>> { Success = true, Data = await _service.GetAllAsync() }); }
        catch (InvalidOperationException ex) { return Unauthorized(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving application details"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound(new ApiErrorResponse { Message = "Application detail not found" });
            return Ok(new ApiResponse<ApplicationDetailDto> { Success = true, Data = item });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving application detail {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("by-application/{applicationId}")]
    public async Task<IActionResult> GetByApplicationId(Guid applicationId)
    {
        try
        {
            var item = await _service.GetByApplicationIdAsync(applicationId);
            if (item == null) return NotFound(new ApiErrorResponse { Message = "Application detail not found" });
            return Ok(new ApiResponse<ApplicationDetailDto> { Success = true, Data = item });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving detail for application {Id}", applicationId); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateApplicationDetailDto request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
            var item = await _service.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = item.Id }, new ApiResponse<ApplicationDetailDto> { Success = true, Data = item });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating application detail"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateApplicationDetailDto request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
            var item = await _service.UpdateAsync(id, request, userId);
            return Ok(new ApiResponse<ApplicationDetailDto> { Success = true, Data = item });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating application detail {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
            await _service.DeleteAsync(id, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting application detail {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}

[ApiController]
[Route("api/v1/hr/[controller]")]
[Produces("application/json")]
[Authorize]
public class ApplicationComplianceController : ControllerBase
{
    private readonly IApplicationComplianceService _service;
    private readonly ILogger<ApplicationComplianceController> _logger;

    public ApplicationComplianceController(IApplicationComplianceService service, ILogger<ApplicationComplianceController> logger)
    { _service = service; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try { return Ok(new ApiResponse<IEnumerable<ApplicationComplianceDto>> { Success = true, Data = await _service.GetAllAsync() }); }
        catch (InvalidOperationException ex) { return Unauthorized(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving application compliances"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound(new ApiErrorResponse { Message = "Application compliance not found" });
            return Ok(new ApiResponse<ApplicationComplianceDto> { Success = true, Data = item });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving application compliance {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("by-application/{applicationId}")]
    public async Task<IActionResult> GetByApplicationId(Guid applicationId)
    {
        try
        {
            var item = await _service.GetByApplicationIdAsync(applicationId);
            if (item == null) return NotFound(new ApiErrorResponse { Message = "Application compliance not found" });
            return Ok(new ApiResponse<ApplicationComplianceDto> { Success = true, Data = item });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving compliance for application {Id}", applicationId); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateApplicationComplianceDto request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
            var item = await _service.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = item.Id }, new ApiResponse<ApplicationComplianceDto> { Success = true, Data = item });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating application compliance"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateApplicationComplianceDto request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
            var item = await _service.UpdateAsync(id, request, userId);
            return Ok(new ApiResponse<ApplicationComplianceDto> { Success = true, Data = item });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating application compliance {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
            await _service.DeleteAsync(id, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting application compliance {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}
