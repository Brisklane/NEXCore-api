using Nexcore.SharedKernel.Api;
using Procurement.Application.DTOs;

namespace Procurement.Application.Services.Interfaces;

/// <summary>Return-to-Vendor (RTV) — returns received goods to the vendor.</summary>
public interface IPurchaseReturnService
{
    Task<PaginatedResponse<PurchaseReturnDto>> GetAllAsync(PaginationParams pagination);
    Task<PurchaseReturnDto?> GetByIdAsync(Guid id);
    Task<List<PurchaseReturnDto>> GetByVendorAsync(Guid vendorId);
    /// <summary>Posted returns not yet linked to a debit note (eligible to raise one against). Unpaginated — used as a lookup.</summary>
    Task<List<PurchaseReturnDto>> GetEligibleForDebitNoteAsync();
    Task<PurchaseReturnDto> CreateAsync(CreatePurchaseReturnDto dto);
    Task<PurchaseReturnDto> ApproveAsync(Guid id, Guid userId);
    Task<PurchaseReturnDto> PostAsync(Guid id, Guid userId);
    Task<PurchaseReturnDto> CancelAsync(Guid id);
    Task DeleteAsync(Guid id);
}
