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
public class CompetencyFrameworkController : ControllerBase
{
    private readonly ICompetencyFrameworkService _service;
    private readonly ILogger<CompetencyFrameworkController> _logger;

    public CompetencyFrameworkController(ICompetencyFrameworkService service, ILogger<CompetencyFrameworkController> logger)
    { _service = service; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try { return Ok(new ApiResponse<IEnumerable<CompetencyFrameworkDto>> { Success = true, Data = await _service.GetAllAsync() }); }
        catch (InvalidOperationException ex) { return Unauthorized(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving competency frameworks"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound(new ApiErrorResponse { Message = "Competency framework not found" });
            return Ok(new ApiResponse<CompetencyFrameworkDto> { Success = true, Data = item });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving competency framework {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCompetencyFrameworkDto request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
            var item = await _service.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = item.Id }, new ApiResponse<CompetencyFrameworkDto> { Success = true, Data = item });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating competency framework"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCompetencyFrameworkDto request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
            var item = await _service.UpdateAsync(id, request, userId);
            return Ok(new ApiResponse<CompetencyFrameworkDto> { Success = true, Data = item });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating competency framework {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
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
        catch (Exception ex) { _logger.LogError(ex, "Error deleting competency framework {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}

[ApiController]
[Route("api/v1/hr/[controller]")]
[Produces("application/json")]
[Authorize]
public class CompetencyFrameworkItemController : ControllerBase
{
    private readonly ICompetencyFrameworkItemService _service;
    private readonly ILogger<CompetencyFrameworkItemController> _logger;

    public CompetencyFrameworkItemController(ICompetencyFrameworkItemService service, ILogger<CompetencyFrameworkItemController> logger)
    { _service = service; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try { return Ok(new ApiResponse<IEnumerable<CompetencyFrameworkItemDto>> { Success = true, Data = await _service.GetAllAsync() }); }
        catch (InvalidOperationException ex) { return Unauthorized(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving competency framework items"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound(new ApiErrorResponse { Message = "Competency framework item not found" });
            return Ok(new ApiResponse<CompetencyFrameworkItemDto> { Success = true, Data = item });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving competency framework item {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("by-framework/{frameworkId}")]
    public async Task<IActionResult> GetByFramework(Guid frameworkId)
    {
        try { return Ok(new ApiResponse<IEnumerable<CompetencyFrameworkItemDto>> { Success = true, Data = await _service.GetByFrameworkIdAsync(frameworkId) }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving items for framework {Id}", frameworkId); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCompetencyFrameworkItemDto request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
            var item = await _service.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = item.Id }, new ApiResponse<CompetencyFrameworkItemDto> { Success = true, Data = item });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating competency framework item"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCompetencyFrameworkItemDto request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
            var item = await _service.UpdateAsync(id, request, userId);
            return Ok(new ApiResponse<CompetencyFrameworkItemDto> { Success = true, Data = item });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating competency framework item {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
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
        catch (Exception ex) { _logger.LogError(ex, "Error deleting competency framework item {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}
