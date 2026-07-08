using Accounting.Application.DTOs;
using Accounting.Application.Services.Interfaces;
using Accounting.Domain.Entities;
using Accounting.Infrastructure.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;

namespace Accounting.Infrastructure.Services;

/// <summary>
/// Implementation of dimension service
/// Handles all business logic for dimension operations
/// Coordinates between controllers and repositories
/// Applies domain rules and validations
/// </summary>
public class DimensionService : IDimensionService
{
    private readonly IDimensionRepository _repository;
    private readonly IDimensionValueRepository _valueRepository;
    private readonly ILogger<DimensionService> _logger;

    public DimensionService(
        IDimensionRepository repository,
        IDimensionValueRepository valueRepository,
        ILogger<DimensionService> logger)
    {
        _repository = repository;
        _valueRepository = valueRepository;
        _logger = logger;
    }

    public async Task<DimensionDto?> GetByIdAsync(Guid id)
    {
        try
        {
            var dimension = await _repository.GetByIdAsync(id);
            if (dimension == null || dimension.IsDeleted)
                return null;

            return MapToDto(dimension);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dimension: {DimensionId}", id);
            throw;
        }
    }

    public async Task<DimensionDto?> GetByCodeAsync(string code)
    {
        try
        {
            var dimension = await _repository.GetByCodeAsync(code);
            if (dimension == null || dimension.IsDeleted)
                return null;

            return MapToDto(dimension);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dimension by code: {Code}", code);
            throw;
        }
    }

    public async Task<PaginatedResponse<DimensionDto>> GetAllAsync(PaginationParams pagination)
    {
        try
        {
            var search = pagination.SearchTerm?.Trim().ToLower();
            var (items, total) = await _repository.GetPagedAsync(
                pagination.PageNumber, pagination.PageSize,
                predicate: d => string.IsNullOrEmpty(search) ||
                    d.Code.ToLower().Contains(search) ||
                    d.Name.ToLower().Contains(search) ||
                    (d.Description != null && d.Description.ToLower().Contains(search)),
                orderBy: q => q.ApplyOrderNewestFirst(pagination.SortBy, pagination.SortDirection, "Code"));
            return PaginatedResponse<DimensionDto>.Ok(items.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dimensions");
            throw;
        }
    }

    public async Task<DimensionDto> CreateAsync(CreateDimensionDto request, Guid userId)
    {
        try
        {
            // Check for duplicate code (tenant filtering automatic)
            var existing = await _repository.GetByCodeAsync(request.Code);
            if (existing != null && !existing.IsDeleted)
                throw new InvalidOperationException("Dimension code already exists");

            var dimension = new Dimension
            {
                Code = request.Code,
                Name = request.Name,
                Description = request.Description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = userId
            };

            await _repository.AddAsync(dimension);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Dimension created: {DimensionName} (Code: {Code})", 
                dimension.Name, dimension.Code);

            return MapToDto(dimension);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating dimension: {Code}", request.Code);
            throw;
        }
    }

    public async Task<DimensionDto> UpdateAsync(Guid id, UpdateDimensionDto request, Guid userId)
    {
        try
        {
            var dimension = await _repository.GetByIdAsync(id);
            if (dimension == null || dimension.IsDeleted)
                throw new InvalidOperationException("Dimension not found");

            if (!string.IsNullOrWhiteSpace(request.Name))
                dimension.Name = request.Name;

            if (request.IsActive.HasValue)
                dimension.IsActive = request.IsActive.Value;

            if (request.Description != null)
                dimension.Description = request.Description;

            dimension.UpdatedAt = DateTime.UtcNow;
            dimension.UpdatedByUserId = userId;

            _repository.Update(dimension);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Dimension updated: {DimensionName} (Code: {Code})", 
                dimension.Name, dimension.Code);

            return MapToDto(dimension);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating dimension: {DimensionId}", id);
            throw;
        }
    }

    public async Task DeleteAsync(Guid id, Guid userId)
    {
        try
        {
            var dimension = await _repository.GetByIdAsync(id);
            if (dimension == null || dimension.IsDeleted)
                throw new InvalidOperationException("Dimension not found");

            dimension.IsDeleted = true;
            dimension.DeletedAt = DateTime.UtcNow;
            dimension.DeletedByUserId = userId;

            _repository.Update(dimension);
            await _repository.SaveChangesAsync();

            _logger.LogInformation("Dimension deleted: {DimensionName} (Code: {Code})", 
                dimension.Name, dimension.Code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting dimension: {DimensionId}", id);
            throw;
        }
    }

    public async Task<PaginatedResponse<DimensionValueDto>> GetDimensionValuesAsync(Guid dimensionId, PaginationParams pagination)
    {
        try
        {
            var dimension = await _repository.GetByIdAsync(dimensionId);
            if (dimension == null || dimension.IsDeleted)
                throw new InvalidOperationException("Dimension not found");

            var (items, total) = await _valueRepository.GetPagedAsync(
                pagination.PageNumber, pagination.PageSize,
                predicate: v => v.DimensionId == dimensionId,
                orderBy: q => q.OrderBy(v => v.ValueCode));
            return PaginatedResponse<DimensionValueDto>.Ok(items.Select(MapValueToDto), total, pagination.PageNumber, pagination.PageSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dimension values: {DimensionId}", dimensionId);
            throw;
        }
    }

    private static DimensionDto MapToDto(Dimension dimension)
    {
        return new DimensionDto
        {
            Id = dimension.Id,
            CompanyId = dimension.CompanyId,
            Code = dimension.Code,
            Name = dimension.Name,
            IsActive = dimension.IsActive,
            Description = dimension.Description
        };
    }

    private static DimensionValueDto MapValueToDto(DimensionValue value)
    {
        return new DimensionValueDto
        {
            Id = value.Id,
            CompanyId = value.CompanyId,
            DimensionId = value.DimensionId,
            ValueCode = value.ValueCode,
            ValueName = value.ValueName,
            IsActive = value.IsActive,
            Description = value.Description
        };
    }
}
