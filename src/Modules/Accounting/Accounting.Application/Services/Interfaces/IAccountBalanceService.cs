using Accounting.Application.DTOs;
using Nexcore.SharedKernel.Api;

namespace Accounting.Application.Services.Interfaces;

/// <summary>
/// Service layer for AccountBalance business logic
/// </summary>
public interface IAccountBalanceService
{
    Task<PaginatedResponse<AccountBalanceDto>> GetByAccountIdAsync(Guid accountId, PaginationParams pagination);
    Task<PaginatedResponse<AccountBalanceDto>> GetByPeriodAsync(Guid periodId, PaginationParams pagination);
    Task<AccountBalanceDto?> GetByIdAsync(Guid id);
    Task<AccountBalanceDto> CreateAsync(CreateAccountBalanceDto request, Guid userId);
    Task<AccountBalanceDto> UpdateAsync(Guid id, UpdateAccountBalanceDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
