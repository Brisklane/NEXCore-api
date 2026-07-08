using Accounting.Application.DTOs;
using Nexcore.SharedKernel.Api;

namespace Accounting.Application.Services.Interfaces;

/// <summary>
/// Service layer for LedgerAccount business logic
/// Handles validation, domain rules, and repository coordination
/// Removes logic from controllers and centralizes business operations
/// </summary>
public interface ILedgerAccountService
{
    /// <summary>
    /// Create a new ledger account with validation
    /// </summary>
    Task<LedgerAccountDto> CreateAsync(CreateLedgerAccountDto request, Guid userId);

    /// <summary>
    /// Get all accounts (tenant-scoped, for dropdowns)
    /// </summary>
    Task<PaginatedResponse<LedgerAccountDto>> GetAllAsync(PaginationParams pagination);

    /// <summary>
    /// Get all accounts by ledger ID
    /// </summary>
    Task<PaginatedResponse<LedgerAccountDto>> GetByLedgerIdAsync(Guid ledgerId, PaginationParams pagination);

    /// <summary>
    /// Get account by ID
    /// </summary>
    Task<LedgerAccountDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Get account by account number
    /// </summary>
    Task<LedgerAccountDto?> GetByAccountNumberAsync(string accountNumber);

    /// <summary>
    /// Get accounts by category
    /// </summary>
    Task<PaginatedResponse<LedgerAccountDto>> GetByCategoryIdAsync(Guid categoryId, Guid ledgerId, PaginationParams pagination);

    /// <summary>
    /// Get subledger accounts by master account
    /// </summary>
    Task<PaginatedResponse<LedgerAccountDto>> GetSubledgerAccountsAsync(Guid ledgerId, Guid masterAccountId, PaginationParams pagination);

    /// <summary>
    /// Update ledger account
    /// </summary>
    Task<LedgerAccountDto> UpdateAsync(Guid id, UpdateLedgerAccountDto request, Guid userId);

    /// <summary>
    /// Soft delete ledger account
    /// </summary>
    Task DeleteAsync(Guid id, Guid userId);

    /// <summary>
    /// Activate ledger account
    /// </summary>
    Task<LedgerAccountDto> ActivateAsync(Guid id, Guid userId);

    /// <summary>
    /// Deactivate ledger account
    /// </summary>
    Task<LedgerAccountDto> DeactivateAsync(Guid id, Guid userId);
}
