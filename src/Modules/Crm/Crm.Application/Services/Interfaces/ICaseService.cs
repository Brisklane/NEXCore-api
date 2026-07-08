using Crm.Application.DTOs;
using Nexcore.SharedKernel.Api;

namespace Crm.Application.Services.Interfaces;

public interface ICaseService
{
    Task<CaseDto> CreateAsync(CreateCaseDto dto, Guid userId);
    Task<CaseDto?> GetByIdAsync(Guid id);
    Task<PaginatedResponse<CaseDto>> GetPagedAsync(PaginationParams pagination, string? status = null);
    Task<IEnumerable<CaseDto>> GetByAccountIdAsync(Guid accountId);
    Task<CaseDto> UpdateAsync(Guid id, UpdateCaseDto dto, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);

    // Case Comments
    Task<CaseCommentDto> AddCommentAsync(Guid caseId, CreateCaseCommentDto dto, Guid userId);
    Task<IEnumerable<CaseCommentDto>> GetCommentsAsync(Guid caseId);
    Task DeleteCommentAsync(Guid caseId, Guid commentId, Guid userId);
}
