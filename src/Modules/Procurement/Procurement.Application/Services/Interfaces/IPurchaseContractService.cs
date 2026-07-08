using Nexcore.SharedKernel.Api;
using Procurement.Application.DTOs;
using Procurement.Domain.Enums;

namespace Procurement.Application.Services.Interfaces;

public interface IPurchaseContractService
{
    Task<PaginatedResponse<PurchaseContractDto>> GetAllAsync(PaginationParams pagination);
    Task<PurchaseContractDto?> GetByIdAsync(Guid id);
    Task<PurchaseContractDto?> GetByNumberAsync(string contractNumber);
    Task<List<PurchaseContractDto>> GetByStatusAsync(PurchaseContractStatus status);
    Task<List<PurchaseContractDto>> GetByVendorAsync(Guid vendorId);
    Task<List<PurchaseContractDto>> GetActiveAsync();
    Task<List<PurchaseContractDto>> GetExpiringAsync(int withinDays);
    Task<PurchaseContractDto> CreateAsync(CreatePurchaseContractDto dto);
    Task<PurchaseContractDto> UpdateAsync(Guid id, UpdatePurchaseContractDto dto);
    Task<PurchaseContractDto> ActivateAsync(Guid id, Guid approvedByUserId);
    Task<PurchaseContractDto> SuspendAsync(Guid id);
    Task<PurchaseContractDto> TerminateAsync(Guid id, TerminateContractDto dto);
    Task<PurchaseContractDto> RenewAsync(Guid id);
    Task DeleteAsync(Guid id);
}
