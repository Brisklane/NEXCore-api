using Procurement.Application.DTOs;

namespace Procurement.Application.Services.Interfaces;

/// <summary>
/// Vendor performance scorecards — periodic KPI evaluations per vendor.
/// </summary>
public interface IVendorPerformanceService
{
    Task<List<VendorPerformanceDto>> GetAllAsync();
    Task<List<VendorPerformanceDto>> GetByVendorAsync(Guid vendorId);
    Task<VendorPerformanceDto?> GetByIdAsync(Guid id);
    Task<VendorPerformanceDto> CreateAsync(CreateVendorPerformanceDto dto, Guid userId);
    Task<VendorPerformanceDto> UpdateAsync(Guid id, UpdateVendorPerformanceDto dto);
    Task DeleteAsync(Guid id);
}
