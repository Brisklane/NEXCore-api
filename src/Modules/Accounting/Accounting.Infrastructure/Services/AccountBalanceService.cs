using Accounting.Application.DTOs;
using Accounting.Application.Services.Interfaces;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;

namespace Accounting.Infrastructure.Services;

/// <summary>
/// Implementation of account balance service
/// Provides balance information for accounts by period
/// Coordinates between controllers and repositories
/// </summary>
public class AccountBalanceService : IAccountBalanceService
{
    private readonly IAccountBalanceRepository _repository;
    private readonly ILogger<AccountBalanceService> _logger;

    public AccountBalanceService(IAccountBalanceRepository repository, ILogger<AccountBalanceService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<PaginatedResponse<AccountBalanceDto>> GetByAccountIdAsync(Guid accountId, PaginationParams pagination)
    {
        try
        {
            var (items, total) = await _repository.GetPagedAsync(
                pagination.PageNumber, pagination.PageSize,
                predicate: b => b.LedgerAccountId == accountId,
                orderBy: q => q.OrderByDescending(b => b.FiscalPeriodId));
            return PaginatedResponse<AccountBalanceDto>.Ok(items.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving account balances: {AccountId}", accountId);
            throw;
        }
    }

    public async Task<PaginatedResponse<AccountBalanceDto>> GetByPeriodAsync(Guid periodId, PaginationParams pagination)
    {
        try
        {
            var (items, total) = await _repository.GetPagedAsync(
                pagination.PageNumber, pagination.PageSize,
                predicate: b => b.FiscalPeriodId == periodId,
                orderBy: q => q.OrderBy(b => b.LedgerAccountId));
            return PaginatedResponse<AccountBalanceDto>.Ok(items.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving trial balance for period: {PeriodId}", periodId);
            throw;
        }
    }

    public async Task<AccountBalanceDto?> GetByIdAsync(Guid id)
    {
        try
        {
            var balance = await _repository.GetByIdAsync(id);
            if (balance == null || balance.IsDeleted)
                return null;

            return MapToDto(balance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving account balance: {BalanceId}", id);
            throw;
        }
    }

    public async Task<AccountBalanceDto?> GetByAccountAndPeriodAsync(Guid accountId, Guid periodId)
    {
        try
        {
            var balance = await _repository.GetByAccountAndPeriodAsync(accountId, periodId);
            if (balance == null)
                return null;

            return MapToDto(balance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving account balance: Account={AccountId}, Period={PeriodId}", 
                accountId, periodId);
            throw;
        }
    }

    public async Task<AccountBalanceDto> CreateAsync(CreateAccountBalanceDto request, Guid userId)
    {
        try
        {
            var balance = new AccountBalance
            {
                LedgerAccountId = request.LedgerAccountId,
                FiscalPeriodId = request.FiscalPeriodId,
                OpeningBalance = request.Amount,
                DebitTotal = 0,
                CreditTotal = 0,
                ClosingBalance = request.Amount,
                BalanceType = request.BalanceType!,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            await _repository.AddAsync(balance);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Account balance created: Account={AccountId}, Period={PeriodId}", 
                balance.LedgerAccountId, balance.FiscalPeriodId);

            return MapToDto(balance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating account balance");
            throw;
        }
    }

    public async Task<AccountBalanceDto> UpdateAsync(Guid id, UpdateAccountBalanceDto request, Guid userId)
    {
        try
        {
            var balance = await _repository.GetByIdAsync(id);
            if (balance == null || balance.IsDeleted)
                throw new InvalidOperationException("Account balance not found");

            if (request.Amount.HasValue)
                balance.ClosingBalance = request.Amount.Value;

            if (!string.IsNullOrWhiteSpace(request.BalanceType))
                balance.BalanceType = request.BalanceType;

            balance.UpdatedAt = DateTime.UtcNow;
            balance.UpdatedByUserId = userId;

            _repository.Update(balance);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Account balance updated: {BalanceId}", balance.Id);

            return MapToDto(balance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating account balance: {BalanceId}", id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var balance = await _repository.GetByIdAsync(id);
            if (balance == null || balance.IsDeleted)
                throw new InvalidOperationException("Account balance not found");

            balance.IsDeleted = true;
            balance.DeletedAt = DateTime.UtcNow;
            balance.DeletedByUserId = userId;

            _repository.Update(balance);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Account balance deleted: {BalanceId}", balance.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting account balance: {BalanceId}", id);
            throw;
        }
    }

    private static AccountBalanceDto MapToDto(AccountBalance balance)
    {
        return new AccountBalanceDto
        {
            Id = balance.Id,
            CompanyId = balance.CompanyId,
            LedgerAccountId = balance.LedgerAccountId,
            FiscalPeriodId = balance.FiscalPeriodId,
            OpeningBalance = balance.OpeningBalance,
            DebitTotal = balance.DebitTotal,
            CreditTotal = balance.CreditTotal,
            ClosingBalance = balance.ClosingBalance,
            BalanceType = balance.BalanceType
        };
    }
}
