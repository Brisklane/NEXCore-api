using Crm.Application.DTOs;

namespace Crm.Application.Services.Interfaces;

public interface IKnowledgeArticleService
{
    Task<KnowledgeArticleDto> CreateAsync(CreateKnowledgeArticleDto dto, Guid userId);
    Task<KnowledgeArticleDto?> GetByIdAsync(Guid id);
    Task<(IEnumerable<KnowledgeArticleDto> Items, int Total)> GetPagedAsync(int page, int pageSize, string? search = null);
    Task<KnowledgeArticleDto> UpdateAsync(Guid id, UpdateKnowledgeArticleDto dto, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
    Task<KnowledgeArticleDto> PublishAsync(Guid id, Guid userId);
    Task<KnowledgeArticleDto> ArchiveAsync(Guid id, Guid userId);
}

public interface IEntitlementService
{
    Task<EntitlementDto> CreateAsync(CreateEntitlementDto dto, Guid userId);
    Task<EntitlementDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<EntitlementDto>> GetByAccountIdAsync(Guid accountId);
    Task<EntitlementDto> UpdateAsync(Guid id, UpdateEntitlementDto dto, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
