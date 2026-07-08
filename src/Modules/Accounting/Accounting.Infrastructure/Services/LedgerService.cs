using Accounting.Application.DTOs;
using Accounting.Application.Services.Interfaces;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;

namespace Accounting.Infrastructure.Services;

/// <summary>
/// Implementation of ledger service
/// Handles all business logic for ledger operations
/// Coordinates between controllers and repositories
/// Applies domain rules and validations
/// </summary>
public class LedgerService : ILedgerService
{
    private readonly ILedgerRepository _repository;
    private readonly ILogger<LedgerService> _logger;

    public LedgerService(ILedgerRepository repository, ILogger<LedgerService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<LedgerDto> CreateAsync(CreateLedgerDto request, Guid userId)
    {
        try
        {
            var ledger = new Ledger
            {
                // CompanyId, BranchId, BusinessUnitId automatically set by repository.AddAsync()
                Name = request.Name,
                BaseCurrencyCode = request.BaseCurrencyCode,
                FiscalCalendarId = request.FiscalCalendarId,
                Description = request.Description,
                IsActive = true,
                IsDefault = request.IsDefault,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            await _repository.AddAsync(ledger);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Ledger created: {LedgerName} (ID: {LedgerId})", 
                ledger.Name, ledger.Id);

            return MapToDto(ledger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating ledger: {LedgerName}", request.Name);
            throw;
        }
    }

    public async Task<LedgerDto?> GetByIdAsync(Guid id)
    {
        try
        {
            var ledger = await _repository.GetByIdAsync(id);
            if (ledger == null || ledger.IsDeleted)
                return null;

            return MapToDto(ledger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ledger: {LedgerId}", id);
            throw;
        }
    }

    public async Task<PaginatedResponse<LedgerDto>> GetAllAsync(PaginationParams pagination)
    {
        try
        {
            var search = pagination.SearchTerm?.Trim().ToLower();
            var (items, total) = await _repository.GetPagedAsync(
                pagination.PageNumber, pagination.PageSize,
                predicate: l => string.IsNullOrEmpty(search) ||
                    l.Name.ToLower().Contains(search) ||
                    l.BaseCurrencyCode.ToLower().Contains(search) ||
                    (l.Description != null && l.Description.ToLower().Contains(search)),
                orderBy: q => q.ApplyOrderNewestFirst(pagination.SortBy, pagination.SortDirection, "Name"));
            return PaginatedResponse<LedgerDto>.Ok(items.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ledgers");
            throw;
        }
    }

    public async Task<LedgerDto?> GetDefaultAsync()
    {
        try
        {
            var ledger = await _repository.GetDefaultLedgerAsync();
            if (ledger == null || ledger.IsDeleted)
                return null;

            return MapToDto(ledger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving default ledger");
            throw;
        }
    }

    public async Task<LedgerDto> UpdateAsync(Guid id, UpdateLedgerDto request, Guid userId)
    {
        try
        {
            var ledger = await _repository.GetByIdAsync(id);
            if (ledger == null || ledger.IsDeleted)
                throw new InvalidOperationException("Ledger not found");

            // Update allowed fields only
            if (!string.IsNullOrWhiteSpace(request.Name))
                ledger.Name = request.Name;

            if (!string.IsNullOrWhiteSpace(request.BaseCurrencyCode))
                ledger.BaseCurrencyCode = request.BaseCurrencyCode;

            if (request.IsActive.HasValue)
                ledger.IsActive = request.IsActive.Value;

            if (request.IsDefault.HasValue)
                ledger.IsDefault = request.IsDefault.Value;

            if (request.FiscalCalendarId.HasValue)
                ledger.FiscalCalendarId = request.FiscalCalendarId.Value;

            if (request.Description != null)
                ledger.Description = request.Description;

            ledger.UpdatedAt = DateTime.UtcNow;
            ledger.UpdatedByUserId = userId;

            _repository.Update(ledger);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Ledger updated: {LedgerName} (ID: {LedgerId})", 
                ledger.Name, ledger.Id);

            return MapToDto(ledger);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating ledger: {LedgerId}", id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var ledger = await _repository.GetByIdAsync(id);
            if (ledger == null || ledger.IsDeleted)
                throw new InvalidOperationException("Ledger not found");

            ledger.IsDeleted = true;
            ledger.DeletedAt = DateTime.UtcNow;
            ledger.DeletedByUserId = userId;

            _repository.Update(ledger);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Ledger deleted: {LedgerName} (ID: {LedgerId})", 
                ledger.Name, ledger.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting ledger: {LedgerId}", id);
            throw;
        }
    }

    private static LedgerDto MapToDto(Ledger ledger)
    {
        return new LedgerDto
        {
            Id = ledger.Id,
            CompanyId = ledger.CompanyId,
            Name = ledger.Name,
            BaseCurrencyCode = ledger.BaseCurrencyCode,
            FiscalCalendarId = ledger.FiscalCalendarId,
            IsDefault = ledger.IsDefault,
            IsActive = ledger.IsActive,
            Description = ledger.Description,
            CreatedAt = ledger.CreatedAt,
            UpdatedAt = ledger.UpdatedAt
        };
    }
}
