using Nexcore.SharedKernel.Api;
using Procurement.Application.DTOs;
using Procurement.Domain.Enums;

namespace Procurement.Application.Services.Interfaces;

public interface IPurchaseInvoiceService
{
    Task<PaginatedResponse<PurchaseInvoiceDto>> GetAllAsync(PaginationParams pagination);
    Task<PurchaseInvoiceDto?> GetByIdAsync(Guid id);
    Task<PurchaseInvoiceDto?> GetByNumberAsync(string invoiceNumber);
    Task<List<PurchaseInvoiceDto>> GetByStatusAsync(PurchaseInvoiceStatus status);
    Task<List<PurchaseInvoiceDto>> GetByVendorAsync(Guid vendorId);
    Task<List<PurchaseInvoiceDto>> GetByPurchaseOrderAsync(Guid purchaseOrderId);
    Task<PaginatedResponse<PurchaseInvoiceDto>> GetOverdueAsync(PaginationParams pagination);
    Task<PaginatedResponse<PurchaseInvoiceDto>> GetPendingPaymentAsync(PaginationParams pagination);
    Task<PurchaseInvoiceDto> CreateAsync(CreatePurchaseInvoiceDto dto);

    /// <summary>
    /// Auto-create a Draft vendor invoice pre-filled from a posted Goods Receipt (accepted quantities
    /// priced from the PO), so the user only enters the vendor's invoice number and confirms.
    /// Returns null when nothing was created.
    /// </summary>
    Task<PurchaseInvoiceDto?> CreateDraftFromGoodsReceiptAsync(Guid goodsReceiptId, Guid userId);

    Task<PurchaseInvoiceDto> UpdateAsync(Guid id, UpdatePurchaseInvoiceDto dto);
    Task<PurchaseInvoiceDto> ApproveAsync(Guid id, Guid approvedByUserId);
    Task<PurchaseInvoiceDto> PostAsync(Guid id, Guid postedByUserId);
    Task<PurchaseInvoiceDto> HoldAsync(Guid id, HoldInvoiceDto dto);
    Task<PurchaseInvoiceDto> ReleaseHoldAsync(Guid id);
    Task<PurchaseInvoiceDto> DisputeAsync(Guid id, DisputeInvoiceDto dto);
    Task<PurchaseInvoiceDto> ResolveDisputeAsync(Guid id);
    Task<PurchaseInvoiceDto> CancelAsync(Guid id);
    Task DeleteAsync(Guid id);

    /// <summary>Run three-way match (PO vs GR vs Invoice) and update MatchingStatus.</summary>
    Task<PurchaseInvoiceDto> RunThreeWayMatchAsync(Guid id);
}
