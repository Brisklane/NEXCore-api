using Accounting.Application.DTOs;
using Accounting.Application.Services.Interfaces;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;

namespace Accounting.Infrastructure.Services;

/// <summary>
/// Implementation of ledger account service
/// Handles all business logic for ledger account operations
/// Coordinates between controllers and repositories
/// Applies domain rules and validations
/// </summary>
public class LedgerAccountService : ILedgerAccountService
{
    private readonly ILedgerAccountRepository _repository;
    private readonly ILogger<LedgerAccountService> _logger;

    public LedgerAccountService(ILedgerAccountRepository repository, ILogger<LedgerAccountService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<LedgerAccountDto> CreateAsync(CreateLedgerAccountDto request, Guid userId)
    {
        try
        {
            // Validate account number is unique per company
            var existingAccount = await _repository.GetByAccountNumberAsync(request.AccountNumber);
            if (existingAccount != null)
                throw new InvalidOperationException("Account number already exists for this company");

            var account = new LedgerAccount
            {
                // CompanyId, BranchId, BusinessUnitId automatically set by repository.AddAsync()
                LedgerId = request.LedgerId,
                AccountNumber = request.AccountNumber,
                AccountName = request.AccountName,
                CategoryId = request.CategoryId,
                ParentAccountId = request.ParentAccountId,
                IsPostingAllowed = request.IsPostingAllowed,
                IsControlAccount = request.IsControlAccount,
                CurrencyCode = request.CurrencyCode ?? "USD",
                AllowManualEntry = request.AllowManualEntry,
                IsSubledgerAccount = request.IsSubledgerAccount,
                SubledgerMasterAccountId = request.SubledgerMasterAccountId,
                Description = request.Description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            await _repository.AddAsync(account);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Ledger account created: {AccountNumber} (ID: {AccountId})", 
                account.AccountNumber, account.Id);

            return MapToDto(account);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ledger account: {AccountNumber}", request.AccountNumber);
            throw;
        }
    }

    public async Task<PaginatedResponse<LedgerAccountDto>> GetAllAsync(PaginationParams pagination)
    {
        try
        {
            var search = pagination.SearchTerm?.Trim().ToLower();
            var (items, total) = await _repository.GetPagedAsync(
                pagination.PageNumber, pagination.PageSize,
                predicate: a => !a.IsDeleted && a.IsActive &&
                    (string.IsNullOrEmpty(search) ||
                        a.AccountNumber.ToLower().Contains(search) ||
                        a.AccountName.ToLower().Contains(search) ||
                        (a.Description != null && a.Description.ToLower().Contains(search)) ||
                        (a.CurrencyCode != null && a.CurrencyCode.ToLower().Contains(search))),
                orderBy: q => q.ApplyOrderNewestFirst(pagination.SortBy, pagination.SortDirection, "AccountNumber"));
            return PaginatedResponse<LedgerAccountDto>.Ok(items.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all ledger accounts");
            throw;
        }
    }

    public async Task<PaginatedResponse<LedgerAccountDto>> GetByLedgerIdAsync(Guid ledgerId, PaginationParams pagination)
    {
        try
        {
            var search = pagination.SearchTerm?.Trim().ToLower();
            var (items, total) = await _repository.GetPagedAsync(
                pagination.PageNumber, pagination.PageSize,
                predicate: a => a.LedgerId == ledgerId &&
                    (string.IsNullOrEmpty(search) ||
                        a.AccountNumber.ToLower().Contains(search) ||
                        a.AccountName.ToLower().Contains(search) ||
                        (a.Description != null && a.Description.ToLower().Contains(search)) ||
                        (a.CurrencyCode != null && a.CurrencyCode.ToLower().Contains(search))),
                orderBy: q => q.ApplyOrderNewestFirst(pagination.SortBy, pagination.SortDirection, "AccountNumber"));
            return PaginatedResponse<LedgerAccountDto>.Ok(items.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving accounts for ledger: {LedgerId}", ledgerId);
            throw;
        }
    }

    public async Task<LedgerAccountDto?> GetByIdAsync(Guid id)
    {
        try
        {
            var account = await _repository.GetByIdAsync(id);
            if (account == null || account.IsDeleted)
                return null;

            return MapToDto(account);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving account: {AccountId}", id);
            throw;
        }
    }

    public async Task<LedgerAccountDto?> GetByAccountNumberAsync(string accountNumber)
    {
        try
        {
            var account = await _repository.GetByAccountNumberAsync(accountNumber);
            if (account == null || account.IsDeleted)
                return null;

            return MapToDto(account);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving account by number: {AccountNumber}", accountNumber);
            throw;
        }
    }

    public async Task<PaginatedResponse<LedgerAccountDto>> GetByCategoryIdAsync(Guid categoryId, Guid ledgerId, PaginationParams pagination)
    {
        try
        {
            var (items, total) = await _repository.GetPagedAsync(
                pagination.PageNumber, pagination.PageSize,
                predicate: a => a.CategoryId == categoryId && a.LedgerId == ledgerId,
                orderBy: q => q.OrderBy(a => a.AccountNumber));
            return PaginatedResponse<LedgerAccountDto>.Ok(items.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving accounts by category: {CategoryId}", categoryId);
            throw;
        }
    }

    public async Task<PaginatedResponse<LedgerAccountDto>> GetSubledgerAccountsAsync(Guid ledgerId, Guid masterAccountId, PaginationParams pagination)
    {
        try
        {
            var masterAccount = await _repository.GetByIdAsync(masterAccountId);
            if (masterAccount == null || masterAccount.LedgerId != ledgerId)
                throw new InvalidOperationException("Master account not found");

            var (items, total) = await _repository.GetPagedAsync(
                pagination.PageNumber, pagination.PageSize,
                predicate: a => a.IsSubledgerAccount && a.SubledgerMasterAccountId == masterAccountId && a.LedgerId == ledgerId,
                orderBy: q => q.OrderBy(a => a.AccountNumber));
            return PaginatedResponse<LedgerAccountDto>.Ok(items.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving subledger accounts for master: {MasterAccountId}", masterAccountId);
            throw;
        }
    }

    public async Task<LedgerAccountDto> UpdateAsync(Guid id, UpdateLedgerAccountDto request, Guid userId)
    {
        try
        {
            var account = await _repository.GetByIdAsync(id);
            if (account == null || account.IsDeleted)
                throw new InvalidOperationException("Account not found");

            // Update allowed fields only
            if (!string.IsNullOrWhiteSpace(request.AccountName))
                account.AccountName = request.AccountName;

            if (!string.IsNullOrWhiteSpace(request.CurrencyCode))
                account.CurrencyCode = request.CurrencyCode;

            if (request.IsPostingAllowed.HasValue)
                account.IsPostingAllowed = request.IsPostingAllowed.Value;

            if (request.IsControlAccount.HasValue)
                account.IsControlAccount = request.IsControlAccount.Value;

            if (request.AllowManualEntry.HasValue)
                account.AllowManualEntry = request.AllowManualEntry.Value;

            if (request.IsActive.HasValue)
                account.IsActive = request.IsActive.Value;

            if (request.CategoryId.HasValue && request.CategoryId.Value != Guid.Empty)
                account.CategoryId = request.CategoryId.Value;

            if (request.ParentAccountId.HasValue)
                account.ParentAccountId = request.ParentAccountId.Value == Guid.Empty ? null : request.ParentAccountId.Value;

            if (request.Description != null)
                account.Description = request.Description;

            account.UpdatedAt = DateTime.UtcNow;
            account.UpdatedByUserId = userId;

            _repository.Update(account);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Ledger account updated: {AccountNumber} (ID: {AccountId})", 
                account.AccountNumber, account.Id);

            return MapToDto(account);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ledger account: {AccountId}", id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var account = await _repository.GetByIdAsync(id);
            if (account == null || account.IsDeleted)
                throw new InvalidOperationException("Account not found");

            // Check if account has posted transactions
            if (account.JournalLines?.Any() == true)
                throw new InvalidOperationException("Cannot delete account with posted transactions");

            account.IsDeleted = true;
            account.DeletedAt = DateTime.UtcNow;
            account.DeletedByUserId = userId;

            _repository.Update(account);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Ledger account deleted: {AccountNumber} (ID: {AccountId})", 
                account.AccountNumber, account.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting ledger account: {AccountId}", id);
            throw;
        }
    }

    public async Task<LedgerAccountDto> ActivateAsync(Guid id, Guid userId)
    {
        try
        {
            var account = await _repository.GetByIdAsync(id);
            if (account == null)
                throw new InvalidOperationException("Account not found");

            account.IsActive = true;
            account.UpdatedAt = DateTime.UtcNow;
            account.UpdatedByUserId = userId;

            _repository.Update(account);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Ledger account activated: {AccountNumber} (ID: {AccountId})", 
                account.AccountNumber, account.Id);

            return MapToDto(account);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating ledger account: {AccountId}", id);
            throw;
        }
    }

    public async Task<LedgerAccountDto> DeactivateAsync(Guid id, Guid userId)
    {
        try
        {
            var account = await _repository.GetByIdAsync(id);
            if (account == null)
                throw new InvalidOperationException("Account not found");

            account.IsActive = false;
            account.UpdatedAt = DateTime.UtcNow;
            account.UpdatedByUserId = userId;

            _repository.Update(account);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Ledger account deactivated: {AccountNumber} (ID: {AccountId})", 
                account.AccountNumber, account.Id);

            return MapToDto(account);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating ledger account: {AccountId}", id);
            throw;
        }
    }

    private static LedgerAccountDto MapToDto(LedgerAccount account)
    {
        return new LedgerAccountDto
        {
            Id = account.Id,
            CompanyId = account.CompanyId,
            AccountNumber = account.AccountNumber,
            AccountName = account.AccountName,
            CategoryId = account.CategoryId,
            ParentAccountId = account.ParentAccountId,
            IsPostingAllowed = account.IsPostingAllowed,
            IsControlAccount = account.IsControlAccount,
            CurrencyCode = account.CurrencyCode,
            AllowManualEntry = account.AllowManualEntry,
            IsActive = account.IsActive,
            Description = account.Description
        };
    }
}
