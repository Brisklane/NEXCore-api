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
public class CandidateSkillController : ControllerBase
{
    private readonly ICandidateSkillService _service;
    private readonly ILogger<CandidateSkillController> _logger;

    public CandidateSkillController(ICandidateSkillService service, ILogger<CandidateSkillController> logger)
    { _service = service; _logger = logger; }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try { return Ok(new ApiResponse<IEnumerable<CandidateSkillDto>> { Success = true, Data = await _service.GetAllAsync() }); }
        catch (InvalidOperationException ex) { return Unauthorized(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving candidate skills"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var item = await _service.GetByIdAsync(id);
            if (item == null) return NotFound(new ApiErrorResponse { Message = "Candidate skill not found" });
            return Ok(new ApiResponse<CandidateSkillDto> { Success = true, Data = item });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving candidate skill {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("by-candidate/{candidateId}")]
    public async Task<IActionResult> GetByCandidateId(Guid candidateId)
    {
        try { return Ok(new ApiResponse<IEnumerable<CandidateSkillDto>> { Success = true, Data = await _service.GetByCandidateIdAsync(candidateId) }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving skills for candidate {Id}", candidateId); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCandidateSkillDto request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
            var item = await _service.CreateAsync(request, userId);
            return CreatedAtAction(nameof(GetById), new { id = item.Id }, new ApiResponse<CandidateSkillDto> { Success = true, Data = item });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating candidate skill"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCandidateSkillDto request)
    {
        try
        {
            var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
            var item = await _service.UpdateAsync(id, request, userId);
            return Ok(new ApiResponse<CandidateSkillDto> { Success = true, Data = item });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating candidate skill {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
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
        catch (Exception ex) { _logger.LogError(ex, "Error deleting candidate skill {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}
