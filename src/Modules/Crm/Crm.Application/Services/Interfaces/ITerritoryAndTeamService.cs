using Crm.Application.DTOs;

namespace Crm.Application.Services.Interfaces;

public interface ITerritoryService
{
    Task<TerritoryDto> CreateAsync(CreateTerritoryDto dto, Guid userId);
    Task<TerritoryDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<TerritoryDto>> GetAllAsync();
    Task<TerritoryDto> UpdateAsync(Guid id, UpdateTerritoryDto dto, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
    Task AssignAccountAsync(Guid territoryId, Guid accountId, Guid userId);
    Task UnassignAccountAsync(Guid territoryId, Guid accountId, Guid userId);
}

public interface ITeamService
{
    Task<TeamDto> CreateAsync(CreateTeamDto dto, Guid userId);
    Task<TeamDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<TeamDto>> GetAllAsync();
    Task<TeamDto> UpdateAsync(Guid id, UpdateTeamDto dto, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
    Task<TeamMemberDto> AddMemberAsync(Guid teamId, CreateTeamMemberDto dto, Guid userId);
    Task<IEnumerable<TeamMemberDto>> GetMembersAsync(Guid teamId);
    Task RemoveMemberAsync(Guid teamId, Guid memberId, Guid userId);
}
