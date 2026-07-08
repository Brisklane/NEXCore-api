using Accounting.Application.DTOs;
using Nexcore.SharedKernel.Api;

namespace Accounting.Application.Services.Interfaces;

/// <summary>
/// Service layer for Dimension business logic
/// </summary>
public interface IDimensionService
{
    Task<PaginatedResponse<DimensionDto>> GetAllAsync(PaginationParams pagination);
    Task<DimensionDto?> GetByIdAsync(Guid id);
    Task<DimensionDto?> GetByCodeAsync(string code);
    Task<DimensionDto> CreateAsync(CreateDimensionDto request, Guid userId);
    Task<DimensionDto> UpdateAsync(Guid id, UpdateDimensionDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
    Task<PaginatedResponse<DimensionValueDto>> GetDimensionValuesAsync(Guid dimensionId, PaginationParams pagination);
}
