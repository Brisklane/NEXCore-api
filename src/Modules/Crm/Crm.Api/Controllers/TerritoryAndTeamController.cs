using Crm.Application.DTOs;
using Crm.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;

namespace Crm.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class TerritoriesController : ControllerBase
{
    private readonly ITerritoryService _service;
    private readonly ILogger<TerritoriesController> _logger;

    public TerritoriesController(ITerritoryService service, ILogger<TerritoriesController> logger)
    { _service = service; _logger = logger; }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<TerritoryDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        try { var result = await _service.GetAllAsync(); return Ok(new ApiResponse<IEnumerable<TerritoryDto>> { Success = true, Data = result }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving territories"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TerritoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await _service.GetByIdAsync(id);
            if (result is null) return NotFound(new ApiErrorResponse { Message = "Territory not found" });
            return Ok(new ApiResponse<TerritoryDto> { Success = true, Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving territory {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TerritoryDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateTerritoryDto dto)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CreateAsync(dto, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, new ApiResponse<TerritoryDto> { Success = true, Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating territory"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TerritoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTerritoryDto dto)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateAsync(id, dto, userId);
            return Ok(new ApiResponse<TerritoryDto> { Success = true, Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating territory {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
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
        catch (Exception ex) { _logger.LogError(ex, "Error deleting territory {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPost("{id:guid}/accounts/{accountId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> AssignAccount(Guid id, Guid accountId)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            await _service.AssignAccountAsync(id, accountId, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error assigning account to territory"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpDelete("{id:guid}/accounts/{accountId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UnassignAccount(Guid id, Guid accountId)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            await _service.UnassignAccountAsync(id, accountId, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error removing account from territory"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
[Authorize]
public class TeamsController : ControllerBase
{
    private readonly ITeamService _service;
    private readonly ILogger<TeamsController> _logger;

    public TeamsController(ITeamService service, ILogger<TeamsController> logger)
    { _service = service; _logger = logger; }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<TeamDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        try { var result = await _service.GetAllAsync(); return Ok(new ApiResponse<IEnumerable<TeamDto>> { Success = true, Data = result }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving teams"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TeamDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var result = await _service.GetByIdAsync(id);
            if (result is null) return NotFound(new ApiErrorResponse { Message = "Team not found" });
            return Ok(new ApiResponse<TeamDto> { Success = true, Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving team {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TeamDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateTeamDto dto)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.CreateAsync(dto, userId);
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, new ApiResponse<TeamDto> { Success = true, Data = result });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating team"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TeamDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTeamDto dto)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.UpdateAsync(id, dto, userId);
            return Ok(new ApiResponse<TeamDto> { Success = true, Data = result });
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error updating team {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
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
        catch (Exception ex) { _logger.LogError(ex, "Error deleting team {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpGet("{id:guid}/members")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<TeamMemberDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMembers(Guid id)
    {
        try { var result = await _service.GetMembersAsync(id); return Ok(new ApiResponse<IEnumerable<TeamMemberDto>> { Success = true, Data = result }); }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving team members {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpPost("{id:guid}/members")]
    [ProducesResponseType(typeof(ApiResponse<TeamMemberDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> AddMember(Guid id, [FromBody] CreateTeamMemberDto dto)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            var result = await _service.AddMemberAsync(id, dto, userId);
            return StatusCode(StatusCodes.Status201Created, new ApiResponse<TeamMemberDto> { Success = true, Data = result });
        }
        catch (InvalidOperationException ex) { return BadRequest(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error adding team member"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }

    [HttpDelete("{id:guid}/members/{memberId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RemoveMember(Guid id, Guid memberId)
    {
        try
        {
            var userId = TenantContextHelper.ExtractUserId(User);
            await _service.RemoveMemberAsync(id, memberId, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex) { return NotFound(new ApiErrorResponse { Message = ex.Message }); }
        catch (Exception ex) { _logger.LogError(ex, "Error removing team member"); return StatusCode(500, new ApiErrorResponse { Message = "Internal server error" }); }
    }
}
