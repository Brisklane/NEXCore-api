using Crm.Application.DTOs;
using Crm.Application.Services.Interfaces;
using Crm.Domain.Entities;
using Crm.Domain.Enums;
using Crm.Infrastructure.Persistence;
using Crm.Infrastructure.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Crm.Infrastructure.Services;

public class KnowledgeArticleService : IKnowledgeArticleService
{
    private readonly IKnowledgeArticleRepository _repository;
    private readonly ILogger<KnowledgeArticleService> _logger;

    public KnowledgeArticleService(IKnowledgeArticleRepository repository, ILogger<KnowledgeArticleService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<KnowledgeArticleDto> CreateAsync(CreateKnowledgeArticleDto dto, Guid userId)
    {
        var count = await _repository.CountAsync() + 1;
        var entity = new KnowledgeArticle
        {
            ArticleNumber = $"KA-{count:D5}",
            Title = dto.Title,
            UrlName = string.IsNullOrWhiteSpace(dto.UrlName) ? GenerateSlug(dto.Title) : dto.UrlName,
            ArticleType = dto.ArticleType, CategoryGroup = dto.CategoryGroup,
            Status = ArticleStatus.Draft, Summary = dto.Summary, Body = dto.Body,
            IsVisibleInApp = dto.IsVisibleInApp, IsVisibleInCsp = dto.IsVisibleInCsp,
            IsVisibleInPkb = dto.IsVisibleInPkb, VersionNumber = 1,
            OwnerId = dto.OwnerId ?? userId, CreatedByUserId = userId, CreatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<KnowledgeArticleDto?> GetByIdAsync(Guid id)
    {
        var e = await _repository.GetByIdAsync(id);
        return e is null ? null : MapToDto(e);
    }

    public async Task<(IEnumerable<KnowledgeArticleDto> Items, int Total)> GetPagedAsync(int page, int pageSize, string? search = null)
    {
        var (items, total) = await _repository.SearchPagedAsync(page, pageSize, search);
        return (items.Select(MapToDto), total);
    }

    public async Task<KnowledgeArticleDto> UpdateAsync(Guid id, UpdateKnowledgeArticleDto dto, Guid userId)
    {
        var entity = await _repository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Knowledge article not found");
        entity.Title = dto.Title;
        entity.UrlName = string.IsNullOrWhiteSpace(dto.UrlName) ? GenerateSlug(dto.Title) : dto.UrlName;
        entity.ArticleType = dto.ArticleType; entity.CategoryGroup = dto.CategoryGroup;
        entity.Summary = dto.Summary; entity.Body = dto.Body;
        entity.IsVisibleInApp = dto.IsVisibleInApp; entity.IsVisibleInCsp = dto.IsVisibleInCsp;
        entity.IsVisibleInPkb = dto.IsVisibleInPkb; entity.OwnerId = dto.OwnerId;
        entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
        _repository.Update(entity);
        await _repository.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var entity = await _repository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Knowledge article not found");
        entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
        _repository.Update(entity);
        await _repository.SaveChangesAsync();
    }

    public async Task<KnowledgeArticleDto> PublishAsync(Guid id, Guid userId)
    {
        var entity = await _repository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Knowledge article not found");
        entity.Status = ArticleStatus.Published;
        entity.PublishedDate = DateTime.UtcNow;
        entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
        _repository.Update(entity);
        await _repository.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<KnowledgeArticleDto> ArchiveAsync(Guid id, Guid userId)
    {
        var entity = await _repository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Knowledge article not found");
        entity.Status = ArticleStatus.Archived;
        entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
        _repository.Update(entity);
        await _repository.SaveChangesAsync();
        return MapToDto(entity);
    }

    private static string GenerateSlug(string title)
    {
        var slug = title.ToLowerInvariant();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", "-");
        slug = Regex.Replace(slug, @"-+", "-").Trim('-');
        return slug;
    }

    private static KnowledgeArticleDto MapToDto(KnowledgeArticle e) => new()
    {
        Id = e.Id, ArticleNumber = e.ArticleNumber, Title = e.Title, UrlName = e.UrlName,
        ArticleType = e.ArticleType, CategoryGroup = e.CategoryGroup, Status = e.Status,
        Summary = e.Summary, Body = e.Body, IsVisibleInApp = e.IsVisibleInApp,
        IsVisibleInCsp = e.IsVisibleInCsp, IsVisibleInPkb = e.IsVisibleInPkb,
        PublishedDate = e.PublishedDate, VersionNumber = e.VersionNumber,
        OwnerId = e.OwnerId, CreatedAt = e.CreatedAt
    };
}

public class EntitlementService : IEntitlementService
{
    private readonly CrmDbContext _db;
    private readonly ILogger<EntitlementService> _logger;

    public EntitlementService(CrmDbContext db, ILogger<EntitlementService> logger) { _db = db; _logger = logger; }

    public async Task<EntitlementDto> CreateAsync(CreateEntitlementDto dto, Guid userId)
    {
        var entity = new Entitlement
        {
            EntitlementName = dto.EntitlementName, AccountId = dto.AccountId,
            ContactId = dto.ContactId, ServiceLevelName = dto.ServiceLevelName,
            Type = dto.Type, IsPerIncident = dto.IsPerIncident,
            StartDate = dto.StartDate, EndDate = dto.EndDate,
            CasesPerEntitlement = dto.CasesPerEntitlement, IsActive = dto.IsActive,
            CreatedByUserId = userId, CreatedAt = DateTime.UtcNow
        };
        _db.Entitlements.Add(entity);
        await _db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<EntitlementDto?> GetByIdAsync(Guid id)
    {
        var e = await _db.Entitlements.AsNoTracking().Include(x => x.Account)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        return e is null ? null : MapToDto(e);
    }

    public async Task<IEnumerable<EntitlementDto>> GetByAccountIdAsync(Guid accountId)
    {
        var items = await _db.Entitlements.AsNoTracking()
            .Where(x => x.AccountId == accountId && !x.IsDeleted).ToListAsync();
        return items.Select(MapToDto);
    }

    public async Task<EntitlementDto> UpdateAsync(Guid id, UpdateEntitlementDto dto, Guid userId)
    {
        var entity = await _db.Entitlements.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
            ?? throw new InvalidOperationException("Entitlement not found");
        entity.EntitlementName = dto.EntitlementName; entity.AccountId = dto.AccountId;
        entity.ContactId = dto.ContactId; entity.ServiceLevelName = dto.ServiceLevelName;
        entity.Type = dto.Type; entity.IsPerIncident = dto.IsPerIncident;
        entity.StartDate = dto.StartDate; entity.EndDate = dto.EndDate;
        entity.CasesPerEntitlement = dto.CasesPerEntitlement; entity.IsActive = dto.IsActive;
        entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
        await _db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var entity = await _db.Entitlements.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
            ?? throw new InvalidOperationException("Entitlement not found");
        entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
        await _db.SaveChangesAsync();
    }

    private static EntitlementDto MapToDto(Entitlement e) => new()
    {
        Id = e.Id, EntitlementName = e.EntitlementName, AccountId = e.AccountId,
        AccountName = e.Account?.AccountName, ContactId = e.ContactId,
        ServiceLevelName = e.ServiceLevelName, Type = e.Type, IsPerIncident = e.IsPerIncident,
        StartDate = e.StartDate, EndDate = e.EndDate, CasesPerEntitlement = e.CasesPerEntitlement,
        CasesUsed = e.CasesUsed, IsActive = e.IsActive, CreatedAt = e.CreatedAt
    };
}
