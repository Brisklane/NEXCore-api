using Nexcore.SharedKernel.Api;
using Procurement.Application.DTOs;

namespace Procurement.Application.Services.Interfaces;

/// <summary>
/// Approved Vendor List (AVL) / Source List — which vendors may supply a given item or category.
/// </summary>
public interface IApprovedVendorListService
{
    Task<PaginatedResponse<ApprovedVendorListDto>> GetAllAsync(PaginationParams pagination);
    Task<List<ApprovedVendorListDto>> GetByVendorAsync(Guid vendorId);
    Task<List<ApprovedVendorListDto>> GetByItemAsync(Guid itemId);
    Task<List<ApprovedVendorListDto>> GetByCategoryAsync(Guid categoryId);
    Task<ApprovedVendorListDto?> GetByIdAsync(Guid id);
    Task<ApprovedVendorListDto> CreateAsync(CreateApprovedVendorListDto dto, Guid userId);
    Task<ApprovedVendorListDto> UpdateAsync(Guid id, UpdateApprovedVendorListDto dto);
    Task<ApprovedVendorListDto> BlockAsync(Guid id, BlockApprovedVendorDto dto);
    Task<ApprovedVendorListDto> UnblockAsync(Guid id);
    Task DeleteAsync(Guid id);
}
