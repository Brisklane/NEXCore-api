using Accounting.Application.DTOs;
using Nexcore.SharedKernel.Api;

namespace Accounting.Application.Services.Interfaces;

/// <summary>
/// Service layer for TaxCode business logic
/// </summary>
public interface ITaxCodeService
{
    Task<PaginatedResponse<TaxCodeDto>> GetAllAsync(PaginationParams pagination);
    Task<TaxCodeDto?> GetByIdAsync(Guid id);
    Task<TaxCodeDto?> GetByCodeAsync(string code);
    Task<TaxCodeDto> CreateAsync(CreateTaxCodeDto request, Guid userId);
    Task<TaxCodeDto> UpdateAsync(Guid id, UpdateTaxCodeDto request, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}
