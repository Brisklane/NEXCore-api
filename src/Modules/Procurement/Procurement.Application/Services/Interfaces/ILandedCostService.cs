using Nexcore.SharedKernel.Api;
using Procurement.Application.DTOs;

namespace Procurement.Application.Services.Interfaces;

/// <summary>Landed Costs — additional procurement costs allocated across received goods.</summary>
public interface ILandedCostService
{
    Task<PaginatedResponse<LandedCostDto>> GetAllAsync(PaginationParams pagination);
    Task<LandedCostDto?> GetByIdAsync(Guid id);
    Task<LandedCostDto> CreateAsync(CreateLandedCostDto dto);
    /// <summary>Computes allocations across the linked GRN lines and posts the landed cost.</summary>
    Task<LandedCostDto> PostAsync(Guid id, Guid userId);
    Task<LandedCostDto> CancelAsync(Guid id);
    Task DeleteAsync(Guid id);
}
