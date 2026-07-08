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
public class SkillController : ControllerBase
{
    private readonly ISkillService _service;
    private readonly ILogger<SkillController> _logger;

    public SkillController(ISkillService service, ILogger<SkillController> logger)
    { _service = service; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try { return Ok(new ApiResponse<IEnumerable<SkillDto>> { Success = true, Data = await _service.GetAllAsync() }); }
        catch (InvalidOperationException ex) { return Unauthorized(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving skills"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound(new ApiErrorResponse { Message = "Skill not found" });
            return Ok(new ApiResponse<SkillDto> { Success = true, Data = item });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving skill {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("by-category/{categoryId}")]
    public async Task<IActionResult> GetByCategory(Guid categoryId)
    {
        try { return Ok(new ApiResponse<IEnumerable<SkillDto>> { Success = true, Data = await _service.GetByCategoryAsync(categoryId) }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving skills for category {Id}", categoryId); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSkillDto request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
            var item = await _service.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = item.Id }, new ApiResponse<SkillDto> { Success = true, Data = item });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating skill"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSkillDto request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
            var item = await _service.UpdateAsync(id, request, userId);
            return Ok(new ApiResponse<SkillDto> { Success = true, Data = item });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating skill {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
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
        catch (Exception ex) { _logger.LogError(ex, "Error deleting skill {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}
