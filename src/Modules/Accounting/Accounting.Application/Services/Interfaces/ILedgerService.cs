using Accounting.Application.DTOs;
using Nexcore.SharedKernel.Api;

namespace Accounting.Application.Services.Interfaces;

/// <summary>
/// Service layer for Ledger business logic
/// Handles validation, domain rules, and repository coordination
/// Removes logic from controllers and centralizes business operations
/// </summary>
public interface ILedgerService
{
    /// <summary>
    /// Create a new ledger with validation
    /// </summary>
    Task<LedgerDto> CreateAsync(CreateLedgerDto request, Guid userId);

    /// <summary>
    /// Get ledger by ID
    /// </summary>
    Task<LedgerDto?> GetByIdAsync(Guid id);

    /// <summary>
    /// Get all ledgers for current tenant
    /// </summary>
    Task<PaginatedResponse<LedgerDto>> GetAllAsync(PaginationParams pagination);

    /// <summary>
    /// Get default ledger for current tenant
    /// </summary>
    Task<LedgerDto?> GetDefaultAsync();

    /// <summary>
    /// Update ledger
    /// </summary>
    Task<LedgerDto> UpdateAsync(Guid id, UpdateLedgerDto request, Guid userId);

    /// <summary>
    /// Soft delete ledger
    /// </summary>
    Task DeleteAsync(Guid id, Guid userId);
}
