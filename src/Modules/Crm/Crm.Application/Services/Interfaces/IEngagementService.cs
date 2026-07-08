using Crm.Application.DTOs;

namespace Crm.Application.Services.Interfaces;

public interface ITagService
{
    Task<TagDto> CreateAsync(CreateTagDto dto, Guid userId);
    Task<IEnumerable<TagDto>> GetAllAsync();
    Task<TagDto?> GetByIdAsync(Guid id);
    Task<TagDto> UpdateAsync(Guid id, UpdateTagDto dto, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
    Task<EntityTagDto> AssignAsync(AssignTagDto dto, Guid userId);
    Task<IEnumerable<EntityTagDto>> GetEntityTagsAsync(Guid entityId, string entityType);
    Task UnassignAsync(Guid entityTagId, Guid userId);
}

public interface IEmailMessageService
{
    Task<EmailMessageDto> CreateAsync(CreateEmailMessageDto dto, Guid userId);
    Task<EmailMessageDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<EmailMessageDto>> GetByRelatedEntityAsync(Guid relatedToId, string relatedToType);
    Task<(IEnumerable<EmailMessageDto> Items, int Total)> GetPagedAsync(int page, int pageSize);
    Task DeleteAsync(Guid id, Guid userId);
}
