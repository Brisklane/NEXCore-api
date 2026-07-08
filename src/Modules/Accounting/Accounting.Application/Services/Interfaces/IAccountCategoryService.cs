using Accounting.Application.DTOs;
using Nexcore.SharedKernel.Api;

namespace Accounting.Application.Services.Interfaces;

/// <summary>
/// Service layer for AccountCategory business logic
/// </summary>
public interface IAccountCategoryService
{
    Task<PaginatedResponse<AccountCategoryDto>> GetActiveAsync(PaginationParams pagination);
    Task<AccountCategoryDto?> GetByIdAsync(Guid id);
}
