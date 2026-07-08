using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using Sales.Application.DTOs;
using Sales.Domain.Entities;
using Sales.Infrastructure.Repositories.Interfaces;

namespace Sales.Api.Controllers;

/// <summary>Sales Teams management — mirrors Odoo's Sales → Configuration → Sales Teams.</summary>
[ApiController]
[Route("api/sales/[controller]")]
[Authorize]
public class SalesTeamController : ControllerBase
{
    private readonly ISalesTeamRepository _teams;
    private readonly ILogger<SalesTeamController> _logger;

    public SalesTeamController(ISalesTeamRepository teams, ILogger<SalesTeamController> logger)
    {
        _teams  = teams;
        _logger = logger;
    }

    /// <summary>Get all sales teams</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<SalesTeamDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var list = await _teams.GetAllAsync();
            return Ok(new ApiResponse<List<SalesTeamDto>> { Success = true, Data = list.Select(MapToDto).ToList(), Message = "Sales teams retrieved" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving sales teams"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving sales teams" }); }
    }

    /// <summary>Get active sales teams only</summary>
    [HttpGet("active")]
    [ProducesResponseType(typeof(ApiResponse<List<SalesTeamDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActive()
    {
        try
        {
            var list = await _teams.GetActiveAsync();
            return Ok(new ApiResponse<List<SalesTeamDto>> { Success = true, Data = list.Select(MapToDto).ToList(), Message = "Active sales teams" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving active sales teams"); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving sales teams" }); }
    }

    /// <summary>Get sales team by ID with members</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<SalesTeamDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var team = await _teams.GetWithMembersAsync(id);
            if (team == null) return NotFound(new ApiErrorResponse { Message = "Sales team not found" });
            return Ok(new ApiResponse<SalesTeamDto> { Success = true, Data = MapToDto(team), Message = "Sales team retrieved" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error retrieving sales team {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error retrieving sales team" }); }
    }

    /// <summary>Create a sales team</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SalesTeamDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateSalesTeamDto dto)
    {
        try
        {
            if (!ModelState.IsValid) return BadRequest(new ApiErrorResponse { Message = "Invalid input" });

            var team = new SalesTeam
            {
                Name             = dto.Name,
                Alias            = dto.Alias,
                TeamLeaderUserId = dto.TeamLeaderUserId,
                TeamLeaderName   = dto.TeamLeaderName,
                Description      = dto.Description,
                IsActive         = true,
            };

            await _teams.AddAsync(team);
            await _teams.SaveChangesAsync();

            _logger.LogInformation("Sales team created: {Name} (ID: {Id})", team.Name, team.Id);
            return CreatedAtAction(nameof(GetById), new { id = team.Id },
                new ApiResponse<SalesTeamDto> { Success = true, Data = MapToDto(team), Message = "Sales team created" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error creating sales team"); return StatusCode(500, new ApiErrorResponse { Message = "Error creating sales team" }); }
    }

    /// <summary>Update a sales team</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<SalesTeamDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSalesTeamDto dto)
    {
        try
        {
            var team = await _teams.GetByIdAsync(id);
            if (team == null) return NotFound(new ApiErrorResponse { Message = "Sales team not found" });

            if (dto.Name             != null) team.Name             = dto.Name;
            if (dto.Alias            != null) team.Alias            = dto.Alias;
            if (dto.TeamLeaderUserId != null) team.TeamLeaderUserId = dto.TeamLeaderUserId;
            if (dto.TeamLeaderName   != null) team.TeamLeaderName   = dto.TeamLeaderName;
            if (dto.Description      != null) team.Description      = dto.Description;
            if (dto.IsActive         != null) team.IsActive         = dto.IsActive.Value;

            _teams.Update(team);
            await _teams.SaveChangesAsync();

            return Ok(new ApiResponse<SalesTeamDto> { Success = true, Data = MapToDto(team), Message = "Sales team updated" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error updating sales team {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error updating sales team" }); }
    }

    /// <summary>Add a member to a sales team</summary>
    [HttpPost("{id}/members")]
    [ProducesResponseType(typeof(ApiResponse<SalesTeamDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddMember(Guid id, [FromBody] AddSalesTeamMemberDto dto)
    {
        try
        {
            var team = await _teams.GetWithMembersAsync(id);
            if (team == null) return NotFound(new ApiErrorResponse { Message = "Sales team not found" });

            if (team.Members.Any(m => m.UserId == dto.UserId && m.IsActive))
                return BadRequest(new ApiErrorResponse { Message = "User is already a member of this team" });

            team.Members.Add(new SalesTeamMember
            {
                SalesTeamId = team.Id,
                UserId      = dto.UserId,
                UserName    = dto.UserName,
                IsActive    = true,
            });

            _teams.Update(team);
            await _teams.SaveChangesAsync();

            return Ok(new ApiResponse<SalesTeamDto> { Success = true, Data = MapToDto(team), Message = "Member added" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error adding member to sales team {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error adding member" }); }
    }

    /// <summary>Remove a member from a sales team</summary>
    [HttpDelete("{id}/members/{memberId}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveMember(Guid id, Guid memberId)
    {
        try
        {
            var team = await _teams.GetWithMembersAsync(id);
            if (team == null) return NotFound(new ApiErrorResponse { Message = "Sales team not found" });

            var member = team.Members.FirstOrDefault(m => m.Id == memberId);
            if (member == null) return NotFound(new ApiErrorResponse { Message = "Member not found" });

            member.IsActive = false;
            _teams.Update(team);
            await _teams.SaveChangesAsync();

            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Member removed" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error removing member from sales team {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error removing member" }); }
    }

    /// <summary>Delete a sales team</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var team = await _teams.GetByIdAsync(id);
            if (team == null) return NotFound(new ApiErrorResponse { Message = "Sales team not found" });

            _teams.Delete(team);
            await _teams.SaveChangesAsync();

            return Ok(new ApiResponse<bool> { Success = true, Data = true, Message = "Sales team deleted" });
        }
        catch (Exception ex) { _logger.LogError(ex, "Error deleting sales team {Id}", id); return StatusCode(500, new ApiErrorResponse { Message = "Error deleting sales team" }); }
    }

    private static SalesTeamDto MapToDto(SalesTeam t) => new()
    {
        Id               = t.Id,
        Name             = t.Name,
        Alias            = t.Alias,
        TeamLeaderUserId = t.TeamLeaderUserId,
        TeamLeaderName   = t.TeamLeaderName,
        Description      = t.Description,
        IsActive         = t.IsActive,
        Members = t.Members.Where(m => m.IsActive).Select(m => new SalesTeamMemberDto
        {
            Id       = m.Id,
            UserId   = m.UserId,
            UserName = m.UserName,
            IsActive = m.IsActive,
        }).ToList(),
    };
}
