using Crm.Application.DTOs;
using Nexcore.SharedKernel.Api;

namespace Crm.Application.Services.Interfaces;

public interface IAccountService
{
    Task<AccountDto> CreateAsync(CreateAccountDto dto, Guid userId);
    Task<AccountDto?> GetByIdAsync(Guid id);
    Task<PaginatedResponse<AccountDto>> GetPagedAsync(PaginationParams pagination);
    Task<AccountDto> UpdateAsync(Guid id, UpdateAccountDto dto, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
