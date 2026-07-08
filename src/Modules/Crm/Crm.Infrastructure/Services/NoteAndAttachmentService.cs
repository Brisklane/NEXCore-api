using Crm.Application.DTOs;
using Crm.Application.Services.Interfaces;
using Crm.Domain.Entities;
using Crm.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;

namespace Crm.Infrastructure.Services;

public class NoteService : INoteService
{
    private readonly INoteRepository _repository;
    private readonly ILogger<NoteService> _logger;

    public NoteService(INoteRepository repository, ILogger<NoteService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<NoteDto> CreateAsync(CreateNoteDto dto, Guid userId)
    {
        var entity = new Note
        {
            Title = dto.Title,
            Body = dto.Body,
            ParentId = dto.ParentId,
            ParentType = dto.ParentType,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<NoteDto?> GetByIdAsync(Guid id)
    {
        var e = await _repository.GetByIdAsync(id);
        return e is null ? null : MapToDto(e);
    }

    public async Task<IEnumerable<NoteDto>> GetByParentAsync(Guid parentId, string parentType)
    {
        var items = await _repository.GetByParentAsync(parentId, parentType);
        return items.OrderByDescending(x => x.CreatedAt).Select(MapToDto);
    }

    public async Task<NoteDto> UpdateAsync(Guid id, UpdateNoteDto dto, Guid userId)
    {
        var entity = await _repository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Note not found");
        entity.Title = dto.Title;
        entity.Body = dto.Body;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedByUserId = userId;
        _repository.Update(entity);
        await _repository.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var entity = await _repository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Note not found");
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedByUserId = userId;
        _repository.Update(entity);
        await _repository.SaveChangesAsync();
    }

    private static NoteDto MapToDto(Note e) => new()
    {
        Id = e.Id, Title = e.Title, Body = e.Body,
        ParentId = e.ParentId, ParentType = e.ParentType, CreatedAt = e.CreatedAt
    };
}

public class AttachmentService : IAttachmentService
{
    private readonly IAttachmentRepository _repository;
    private readonly ILogger<AttachmentService> _logger;

    public AttachmentService(IAttachmentRepository repository, ILogger<AttachmentService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<AttachmentDto> CreateAsync(CreateAttachmentDto dto, Guid userId)
    {
        var entity = new Attachment
        {
            FileName = dto.FileName, FileExtension = dto.FileExtension,
            FileSizeBytes = dto.FileSizeBytes, ContentType = dto.ContentType,
            StoragePath = dto.StoragePath, ParentId = dto.ParentId, ParentType = dto.ParentType,
            Description = dto.Description, CreatedByUserId = userId, CreatedAt = DateTime.UtcNow
        };
        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();
        return MapToDto(entity);
    }

    public async Task<AttachmentDto?> GetByIdAsync(Guid id)
    {
        var e = await _repository.GetByIdAsync(id);
        return e is null ? null : MapToDto(e);
    }

    public async Task<IEnumerable<AttachmentDto>> GetByParentAsync(Guid parentId, string parentType)
    {
        var items = await _repository.GetByParentAsync(parentId, parentType);
        return items.OrderByDescending(x => x.CreatedAt).Select(MapToDto);
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        var entity = await _repository.GetByIdAsync(id)
            ?? throw new InvalidOperationException("Attachment not found");
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        entity.DeletedByUserId = userId;
        _repository.Update(entity);
        await _repository.SaveChangesAsync();
    }

    private static AttachmentDto MapToDto(Attachment e) => new()
    {
        Id = e.Id, FileName = e.FileName, FileExtension = e.FileExtension,
        FileSizeBytes = e.FileSizeBytes, ContentType = e.ContentType,
        StoragePath = e.StoragePath, ParentId = e.ParentId, ParentType = e.ParentType,
        Description = e.Description, CreatedAt = e.CreatedAt
    };
}
