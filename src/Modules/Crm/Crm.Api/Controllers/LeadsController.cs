using Crm.Application.DTOs;
using Crm.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;

namespace Crm.Api.Controllers;

/// <summary>Leads - prospective customers before conversion.</summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class LeadsController : ControllerBase
{
    private readonly ILeadService _service;
    private readonly ILogger<LeadsController> _logger;

    public LeadsController(ILeadService service, ILogger<LeadsController> logger)
    { _service = service; _logger = logger; }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<LeadDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] PaginationParams pagination, [FromQuery] string? status = null)
    {
        try { var result = await _service.GetPagedAsync(pagination, status); return Ok(result); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving leads"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<LeadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await _service.GetByIdAsync(id);
            if (result is null) return NotFound(new ApiErrorResponse { Message = "Lead not found" });
            return Ok(new ApiResponse<LeadDto> { Success = true, Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving lead {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<LeadDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateLeadDto dto)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CreateAsync(dto, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, new ApiResponse<LeadDto> { Success = true, Data = result, Message = "Lead created successfully" });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error creating lead"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<LeadDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateLeadDto dto)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateAsync(id, dto, userId);
            return Ok(new ApiResponse<LeadDto> { Success = true, Data = result, Message = "Lead updated successfully" });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating lead {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            await _service.DeleteAsync(id, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting lead {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    /// <summary>Convert a lead into an Account, Contact, and optionally a Deal.</summary>
    [HttpPost("{id:guid}/convert")]
    [ProducesResponseType(typeof(ApiResponse<LeadDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Convert(Guid id, [FromBody] ConvertLeadDto dto)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.ConvertAsync(id, dto, userId);
            return Ok(new ApiResponse<LeadDto> { Success = true, Data = result, Message = "Lead converted successfully" });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error converting lead {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}
