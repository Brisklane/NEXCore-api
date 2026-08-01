using Crm.Application.DTOs;
using Crm.Application.Services.Interfaces;
using Crm.Domain.Entities;
using Crm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;

namespace Crm.Infrastructure.Services;

public class CampaignService : ICampaignService
{
    private readonly CrmDbContext _db;
    private readonly ILogger<CampaignService> _logger;

    public CampaignService(CrmDbContext db, ILogger<CampaignService> logger) { _db = db; _logger = logger; }

    public async Task<CampaignDto> CreateAsync(CreateCampaignDto dto, Guid userId)
    {
        var entity = new Campaign
        {
            CampaignName = dto.CampaignName,
            Active = dto.Active,
            Status = dto.Status,
            Type = dto.Type,
            ParentCampaignId = dto.ParentCampaignId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            ExpectedRevenue = dto.ExpectedRevenue,
            BudgetedCost = dto.BudgetedCost,
            ActualCost = dto.ActualCost,
            NumSent = dto.NumSent,
            ExpectedResponsePercent = dto.ExpectedResponsePercent,
            OwnerId = dto.OwnerId ?? userId,
            Description = dto.Description,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        _db.Campaigns.Add(entity);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Campaign created: {CampaignId}", entity.Id);
        return MapToDto(entity);
    }

    public async Task<CampaignDto?> GetByIdAsync(Guid id)
    {
        var e = await _db.Campaigns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        return e is null ? null : MapToDto(e);
    }

    public async Task<PaginatedResponse<CampaignDto>> GetPagedAsync(PaginationParams pagination, string? status = null)
    {
        var query = _db.Campaigns.AsNoTracking().Where(x => !x.IsDeleted);
        if (!string.IsNullOrWhiteSpace(pagination.SearchTerm))
        {
            // Lower-cased on both sides so the match stays case-insensitive. SQL Server's
            // default collation did this implicitly; PostgreSQL compares case-sensitively.
            var term = pagination.SearchTerm.Trim().ToLower();
            query = query.Where(x => x.CampaignName.ToLower().Contains(term));
        }
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(x => x.Status == status);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.CreatedAt)
            .Skip(pagination.CalculateSkip()).Take(pagination.PageSize).ToListAsync();
        return PaginatedResponse<CampaignDto>.Ok(items.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<CampaignDto> UpdateAsync(Guid id, UpdateCampaignDto dto, Guid userId)
    {
        var entity = await _db.Campaigns.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
            ?? throw new InvalidOperationException("Campaign not found");
        entity.CampaignName = dto.CampaignName;
        entity.Active = dto.Active;
        entity.Status = dto.Status;
        entity.Type = dto.Type;
        entity.ParentCampaignId = dto.ParentCampaignId;
        entity.StartDate = dto.StartDate;
        entity.EndDate = dto.EndDate;
        entity.ExpectedRevenue = dto.ExpectedRevenue;
        entity.BudgetedCost = dto.BudgetedCost;
        entity.ActualCost = dto.ActualCost;
        entity.NumSent = dto.NumSent;
        entity.ExpectedResponsePercent = dto.ExpectedResponsePercent;
        entity.OwnerId = dto.OwnerId;
        entity.Description = dto.Description;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedByUserId = userId;
        await _db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var entity = await _db.Campaigns.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
            ?? throw new InvalidOperationException("Campaign not found");
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedByUserId = userId;
        await _db.SaveChangesAsync();
    }

    public async Task<CampaignMemberDto> AddMemberAsync(Guid campaignId, CreateCampaignMemberDto dto, Guid userId)
    {
        _ = await _db.Campaigns.FirstOrDefaultAsync(x => x.Id == campaignId && !x.IsDeleted)
            ?? throw new InvalidOperationException("Campaign not found");
        var entity = new CampaignMember
        {
            CampaignId = campaignId,
            LeadId = dto.LeadId,
            ContactId = dto.ContactId,
            Status = dto.Status,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        _db.CampaignMembers.Add(entity);
        await _db.SaveChangesAsync();
        return MapMemberDto(entity);
    }

    public async Task<IEnumerable<CampaignMemberDto>> GetMembersAsync(Guid campaignId)
    {
        var items = await _db.CampaignMembers.AsNoTracking()
            .Where(x => x.CampaignId == campaignId && !x.IsDeleted)
            .ToListAsync();
        return items.Select(MapMemberDto);
    }

    public async Task RemoveMemberAsync(Guid campaignId, Guid memberId, Guid userId)
    {
        var entity = await _db.CampaignMembers
            .FirstOrDefaultAsync(x => x.Id == memberId && x.CampaignId == campaignId && !x.IsDeleted)
            ?? throw new InvalidOperationException("Campaign member not found");
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedByUserId = userId;
        await _db.SaveChangesAsync();
    }

    private static CampaignDto MapToDto(Campaign e) => new()
    {
        Id = e.Id, CampaignName = e.CampaignName, Active = e.Active, Status = e.Status,
        Type = e.Type, ParentCampaignId = e.ParentCampaignId,
        StartDate = e.StartDate, EndDate = e.EndDate, ExpectedRevenue = e.ExpectedRevenue,
        BudgetedCost = e.BudgetedCost, ActualCost = e.ActualCost,
        NumSent = e.NumSent, ExpectedResponsePercent = e.ExpectedResponsePercent,
        OwnerId = e.OwnerId, Description = e.Description, CreatedAt = e.CreatedAt
    };

    private static CampaignMemberDto MapMemberDto(CampaignMember e) => new()
    {
        Id = e.Id, CampaignId = e.CampaignId, LeadId = e.LeadId,
        ContactId = e.ContactId, Status = e.Status, FirstRespondedDate = e.FirstRespondedDate
    };
}
