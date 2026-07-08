using Accounting.Application.DTOs;
using Nexcore.SharedKernel.Api;

namespace Accounting.Application.Services.Interfaces;

/// <summary>
/// Service layer for PostingProfile business logic
/// </summary>
public interface IPostingProfileService
{
    Task<PaginatedResponse<PostingProfileDto>> GetAllAsync(PaginationParams pagination);
    Task<PostingProfileDto?> GetByIdAsync(Guid id);
    Task<PostingProfileDto> CreateAsync(CreatePostingProfileDto request, Guid userId);
    Task<PostingProfileDto> UpdateAsync(Guid id, UpdatePostingProfileDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
