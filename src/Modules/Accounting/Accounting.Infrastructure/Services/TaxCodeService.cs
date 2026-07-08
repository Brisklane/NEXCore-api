using Accounting.Application.DTOs;
using Accounting.Application.Services.Interfaces;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;

namespace Accounting.Infrastructure.Services;

/// <summary>
/// Implementation of tax code service
/// Handles all business logic for tax code operations
/// Coordinates between controllers and repositories
/// Applies domain rules and validations
/// </summary>
public class TaxCodeService : ITaxCodeService
{
    private readonly ITaxCodeRepository _repository;
    private readonly ILogger<TaxCodeService> _logger;

    public TaxCodeService(ITaxCodeRepository repository, ILogger<TaxCodeService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<PaginatedResponse<TaxCodeDto>> GetAllAsync(PaginationParams pagination)
    {
        try
        {
            var search = pagination.SearchTerm?.Trim().ToLower();
            var (items, total) = await _repository.GetPagedAsync(
                pagination.PageNumber, pagination.PageSize,
                predicate: t => string.IsNullOrEmpty(search) ||
                    t.Code.ToLower().Contains(search) ||
                    t.Name.ToLower().Contains(search) ||
                    (t.Description != null && t.Description.ToLower().Contains(search)),
                orderBy: q => q.ApplyOrderNewestFirst(pagination.SortBy, pagination.SortDirection, "Code"));
            return PaginatedResponse<TaxCodeDto>.Ok(items.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tax codes");
            throw;
        }
    }

    public async Task<TaxCodeDto?> GetByIdAsync(Guid id)
    {
        try
        {
            var taxCode = await _repository.GetByIdAsync(id);
            if (taxCode == null || taxCode.IsDeleted)
                return null;

            return MapToDto(taxCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tax code: {TaxCodeId}", id);
            throw;
        }
    }

    public async Task<TaxCodeDto?> GetByCodeAsync(string code)
    {
        try
        {
            var taxCode = await _repository.GetByCodeAsync(code);
            if (taxCode == null || taxCode.IsDeleted)
                return null;

            return MapToDto(taxCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving tax code by code: {Code}", code);
            throw;
        }
    }

    public async Task<TaxCodeDto> CreateAsync(CreateTaxCodeDto request, Guid userId)
    {
        try
        {
            var taxCode = new TaxCode
            {
                Code = request.Code,
                Name = request.Name,
                Percentage = (decimal)request.TaxRate,
                IsRecoverable = false,
                IsActive = true,
                Description = request.Description,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            await _repository.AddAsync(taxCode);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Tax code created: {TaxCodeName} (Code: {Code})", 
                taxCode.Name, taxCode.Code);

            return MapToDto(taxCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating tax code: {Code}", request.Code);
            throw;
        }
    }

    public async Task<TaxCodeDto> UpdateAsync(Guid id, UpdateTaxCodeDto request, Guid userId)
    {
        try
        {
            var taxCode = await _repository.GetByIdAsync(id);
            if (taxCode == null || taxCode.IsDeleted)
                throw new InvalidOperationException("Tax code not found");

            if (!string.IsNullOrWhiteSpace(request.Name))
                taxCode.Name = request.Name;

            if (request.TaxRate.HasValue)
                taxCode.Percentage = (decimal)request.TaxRate.Value;

            if (request.Description != null)
                taxCode.Description = request.Description;

            taxCode.UpdatedAt = DateTime.UtcNow;
            taxCode.UpdatedByUserId = userId;

            _repository.Update(taxCode);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Tax code updated: {TaxCodeName} (Code: {Code})", 
                taxCode.Name, taxCode.Code);

            return MapToDto(taxCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tax code: {TaxCodeId}", id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var taxCode = await _repository.GetByIdAsync(id);
            if (taxCode == null || taxCode.IsDeleted)
                throw new InvalidOperationException("Tax code not found");

            taxCode.IsDeleted = true;
            taxCode.DeletedAt = DateTime.UtcNow;
            taxCode.DeletedByUserId = userId;

            _repository.Update(taxCode);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Tax code deleted: {TaxCodeName} (Code: {Code})", 
                taxCode.Name, taxCode.Code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tax code: {TaxCodeId}", id);
            throw;
        }
    }

    private static TaxCodeDto MapToDto(TaxCode taxCode)
    {
        return new TaxCodeDto
        {
            Id = taxCode.Id,
            CompanyId = taxCode.CompanyId,
            Code = taxCode.Code,
            Name = taxCode.Name,
            Percentage = taxCode.Percentage,
            IsRecoverable = taxCode.IsRecoverable,
            IsActive = taxCode.IsActive,
            Description = taxCode.Description
        };
    }
}
