using Crm.Application.DTOs;
using Crm.Application.Services.Interfaces;
using Crm.Domain.Entities;
using Crm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Crm.Infrastructure.Services;

public class TerritoryService : ITerritoryService
{
    private readonly CrmDbContext _db;
    private readonly ILogger<TerritoryService> _logger;

    public TerritoryService(CrmDbContext db, ILogger<TerritoryService> logger) { _db = db; _logger = logger; }

    public async Task<TerritoryDto> CreateAsync(CreateTerritoryDto dto, Guid userId)
    {
        var entity = new Territory
        {
            TerritoryName = dto.TerritoryName, Description = dto.Description,
            ParentTerritoryId = dto.ParentTerritoryId, OwnerId = dto.OwnerId ?? userId,
            CreatedByUserId = userId, CreatedAt = DateTime.UtcNow
        };
        _db.Territories.Add(entity);
        await _db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<TerritoryDto?> GetByIdAsync(Guid id)
    {
        var e = await _db.Territories.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        return e is null ? null : MapToDto(e);
    }

    public async Task<IEnumerable<TerritoryDto>> GetAllAsync()
    {
        var items = await _db.Territories.AsNoTracking().Where(x => !x.IsDeleted)
            .OrderBy(x => x.TerritoryName).ToListAsync();
        return items.Select(MapToDto);
    }

    public async Task<TerritoryDto> UpdateAsync(Guid id, UpdateTerritoryDto dto, Guid userId)
    {
        var entity = await _db.Territories.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
            ?? throw new InvalidOperationException("Territory not found");
        entity.TerritoryName = dto.TerritoryName; entity.Description = dto.Description;
        entity.ParentTerritoryId = dto.ParentTerritoryId; entity.OwnerId = dto.OwnerId;
        entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
        await _db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var entity = await _db.Territories.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
            ?? throw new InvalidOperationException("Territory not found");
        entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
        await _db.SaveChangesAsync();
    }

    public async Task AssignAccountAsync(Guid territoryId, Guid accountId, Guid userId)
    {
        var exists = await _db.TerritoryAccounts.AnyAsync(x => x.TerritoryId == territoryId && x.AccountId == accountId && !x.IsDeleted);
        if (exists) throw new InvalidOperationException("Account already assigned to this territory");
        _db.TerritoryAccounts.Add(new TerritoryAccount
        {
            TerritoryId = territoryId, AccountId = accountId,
            CreatedByUserId = userId, CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    public async Task UnassignAccountAsync(Guid territoryId, Guid accountId, Guid userId)
    {
        var entity = await _db.TerritoryAccounts
            .FirstOrDefaultAsync(x => x.TerritoryId == territoryId && x.AccountId == accountId && !x.IsDeleted)
            ?? throw new InvalidOperationException("Territory account assignment not found");
        entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
        await _db.SaveChangesAsync();
    }

    private static TerritoryDto MapToDto(Territory e) => new()
    {
        Id = e.Id, TerritoryName = e.TerritoryName, Description = e.Description,
        ParentTerritoryId = e.ParentTerritoryId, OwnerId = e.OwnerId, CreatedAt = e.CreatedAt
    };
}

public class TeamService : ITeamService
{
    private readonly CrmDbContext _db;
    private readonly ILogger<TeamService> _logger;

    public TeamService(CrmDbContext db, ILogger<TeamService> logger) { _db = db; _logger = logger; }

    public async Task<TeamDto> CreateAsync(CreateTeamDto dto, Guid userId)
    {
        var entity = new Team
        {
            TeamName = dto.TeamName, Description = dto.Description, ManagerId = dto.ManagerId,
            CreatedByUserId = userId, CreatedAt = DateTime.UtcNow
        };
        _db.Teams.Add(entity);
        await _db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<TeamDto?> GetByIdAsync(Guid id)
    {
        var e = await _db.Teams.AsNoTracking().Include(x => x.Members)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        return e is null ? null : MapToDto(e);
    }

    public async Task<IEnumerable<TeamDto>> GetAllAsync()
    {
        var items = await _db.Teams.AsNoTracking().Where(x => !x.IsDeleted)
            .OrderBy(x => x.TeamName).ToListAsync();
        return items.Select(MapToDto);
    }

    public async Task<TeamDto> UpdateAsync(Guid id, UpdateTeamDto dto, Guid userId)
    {
        var entity = await _db.Teams.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
            ?? throw new InvalidOperationException("Team not found");
        entity.TeamName = dto.TeamName; entity.Description = dto.Description; entity.ManagerId = dto.ManagerId;
        entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
        await _db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var entity = await _db.Teams.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
            ?? throw new InvalidOperationException("Team not found");
        entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
        await _db.SaveChangesAsync();
    }

    public async Task<TeamMemberDto> AddMemberAsync(Guid teamId, CreateTeamMemberDto dto, Guid userId)
    {
        var entity = new TeamMember
        {
            TeamId = teamId, UserId = dto.UserId, TeamRole = dto.TeamRole,
            AccountAccessLevel = dto.AccountAccessLevel, OpportunityAccessLevel = dto.OpportunityAccessLevel,
            CaseAccessLevel = dto.CaseAccessLevel, CreatedByUserId = userId, CreatedAt = DateTime.UtcNow
        };
        _db.TeamMembers.Add(entity);
        await _db.SaveChangesAsync();
        return MapMemberDto(entity);
    }

    public async Task<IEnumerable<TeamMemberDto>> GetMembersAsync(Guid teamId)
    {
        var items = await _db.TeamMembers.AsNoTracking()
            .Where(x => x.TeamId == teamId && !x.IsDeleted).ToListAsync();
        return items.Select(MapMemberDto);
    }

    public async Task RemoveMemberAsync(Guid teamId, Guid memberId, Guid userId)
    {
        var entity = await _db.TeamMembers
            .FirstOrDefaultAsync(x => x.Id == memberId && x.TeamId == teamId && !x.IsDeleted)
            ?? throw new InvalidOperationException("Team member not found");
        entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
        await _db.SaveChangesAsync();
    }

    private static TeamDto MapToDto(Team e) => new()
    {
        Id = e.Id, TeamName = e.TeamName, Description = e.Description,
        ManagerId = e.ManagerId, CreatedAt = e.CreatedAt,
        Members = e.Members?.Where(m => !m.IsDeleted).Select(MapMemberDto).ToList() ?? []
    };

    private static TeamMemberDto MapMemberDto(TeamMember m) => new()
    {
        Id = m.Id, TeamId = m.TeamId, UserId = m.UserId, TeamRole = m.TeamRole,
        AccountAccessLevel = m.AccountAccessLevel, OpportunityAccessLevel = m.OpportunityAccessLevel,
        CaseAccessLevel = m.CaseAccessLevel
    };
}
