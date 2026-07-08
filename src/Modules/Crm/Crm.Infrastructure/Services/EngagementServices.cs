using Crm.Application.DTOs;
using Crm.Application.Services.Interfaces;
using Crm.Domain.Entities;
using Crm.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Crm.Infrastructure.Services;

public class TagService : ITagService
{
    private readonly CrmDbContext _db;
    private readonly ILogger<TagService> _logger;

    public TagService(CrmDbContext db, ILogger<TagService> logger) { _db = db; _logger = logger; }

    public async Task<TagDto> CreateAsync(CreateTagDto dto, Guid userId)
    {
        var entity = new Tag
        {
            TagName = dto.TagName, Color = dto.Color, Description = dto.Description,
            CreatedByUserId = userId, CreatedAt = DateTime.UtcNow
        };
        _db.Tags.Add(entity);
        await _db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<IEnumerable<TagDto>> GetAllAsync()
    {
        var items = await _db.Tags.AsNoTracking().Where(x => !x.IsDeleted)
            .OrderBy(x => x.TagName).ToListAsync();
        return items.Select(MapToDto);
    }

    public async Task<TagDto?> GetByIdAsync(Guid id)
    {
        var e = await _db.Tags.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        return e is null ? null : MapToDto(e);
    }

    public async Task<TagDto> UpdateAsync(Guid id, UpdateTagDto dto, Guid userId)
    {
        var entity = await _db.Tags.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
            ?? throw new InvalidOperationException("Tag not found");
        entity.TagName = dto.TagName; entity.Color = dto.Color; entity.Description = dto.Description;
        entity.UpdatedAt = DateTime.UtcNow; entity.UpdatedByUserId = userId;
        await _db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var entity = await _db.Tags.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
            ?? throw new InvalidOperationException("Tag not found");
        entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
        await _db.SaveChangesAsync();
    }

    public async Task<EntityTagDto> AssignAsync(AssignTagDto dto, Guid userId)
    {
        var entity = new EntityTag
        {
            TagId = dto.TagId, EntityId = dto.EntityId, EntityType = dto.EntityType,
            CreatedByUserId = userId, CreatedAt = DateTime.UtcNow
        };
        _db.EntityTags.Add(entity);
        await _db.SaveChangesAsync();
        var tag = await _db.Tags.FindAsync(dto.TagId);
        entity.Tag = tag!;
        return MapEntityTagDto(entity);
    }

    public async Task<IEnumerable<EntityTagDto>> GetEntityTagsAsync(Guid entityId, string entityType)
    {
        var items = await _db.EntityTags.AsNoTracking().Include(x => x.Tag)
            .Where(x => x.EntityId == entityId && x.EntityType == entityType && !x.IsDeleted)
            .ToListAsync();
        return items.Select(MapEntityTagDto);
    }

    public async Task UnassignAsync(Guid entityTagId, Guid userId)
    {
        var entity = await _db.EntityTags.FirstOrDefaultAsync(x => x.Id == entityTagId && !x.IsDeleted)
            ?? throw new InvalidOperationException("Entity tag not found");
        entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
        await _db.SaveChangesAsync();
    }

    private static TagDto MapToDto(Tag e) => new() { Id = e.Id, TagName = e.TagName, Color = e.Color, Description = e.Description };
    private static EntityTagDto MapEntityTagDto(EntityTag e) => new()
    {
        Id = e.Id, TagId = e.TagId, TagName = e.Tag?.TagName, EntityId = e.EntityId, EntityType = e.EntityType
    };
}

public class EmailMessageService : IEmailMessageService
{
    private readonly CrmDbContext _db;
    private readonly ILogger<EmailMessageService> _logger;

    public EmailMessageService(CrmDbContext db, ILogger<EmailMessageService> logger) { _db = db; _logger = logger; }

    public async Task<EmailMessageDto> CreateAsync(CreateEmailMessageDto dto, Guid userId)
    {
        var entity = new EmailMessage
        {
            Subject = dto.Subject, HtmlBody = dto.HtmlBody, TextBody = dto.TextBody,
            FromAddress = dto.FromAddress, FromName = dto.FromName, ToAddress = dto.ToAddress,
            CcAddress = dto.CcAddress, BccAddress = dto.BccAddress, Incoming = dto.Incoming,
            MessageDate = dto.MessageDate ?? DateTime.UtcNow, IsTracked = dto.IsTracked,
            RelatedToId = dto.RelatedToId, RelatedToType = dto.RelatedToType,
            MessageId = dto.MessageId, InReplyToId = dto.InReplyToId,
            ThreadIdentifier = dto.ThreadIdentifier, SentByUserId = userId,
            CreatedByUserId = userId, CreatedAt = DateTime.UtcNow
        };
        _db.EmailMessages.Add(entity);
        await _db.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<EmailMessageDto?> GetByIdAsync(Guid id)
    {
        var e = await _db.EmailMessages.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        return e is null ? null : MapToDto(e);
    }

    public async Task<IEnumerable<EmailMessageDto>> GetByRelatedEntityAsync(Guid relatedToId, string relatedToType)
    {
        var items = await _db.EmailMessages.AsNoTracking()
            .Where(x => x.RelatedToId == relatedToId && x.RelatedToType == relatedToType && !x.IsDeleted)
            .OrderByDescending(x => x.MessageDate).ToListAsync();
        return items.Select(MapToDto);
    }

    public async Task<(IEnumerable<EmailMessageDto> Items, int Total)> GetPagedAsync(int page, int pageSize)
    {
        var query = _db.EmailMessages.AsNoTracking().Where(x => !x.IsDeleted);
        var total = await query.CountAsync();
        var items = await query.OrderByDescending(x => x.MessageDate)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return (items.Select(MapToDto), total);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var entity = await _db.EmailMessages.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted)
            ?? throw new InvalidOperationException("Email message not found");
        entity.IsDeleted = true; entity.DeletedAt = DateTime.UtcNow; entity.DeletedByUserId = userId;
        await _db.SaveChangesAsync();
    }

    private static EmailMessageDto MapToDto(EmailMessage e) => new()
    {
        Id = e.Id, Subject = e.Subject, HtmlBody = e.HtmlBody, TextBody = e.TextBody,
        FromAddress = e.FromAddress, FromName = e.FromName, ToAddress = e.ToAddress,
        CcAddress = e.CcAddress, Incoming = e.Incoming, MessageDate = e.MessageDate,
        IsRead = e.IsRead, IsTracked = e.IsTracked, OpenCount = e.OpenCount, ClickCount = e.ClickCount,
        RelatedToId = e.RelatedToId, RelatedToType = e.RelatedToType,
        SentByUserId = e.SentByUserId, CreatedAt = e.CreatedAt
    };
}
