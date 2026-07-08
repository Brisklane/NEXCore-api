using Nexcore.SharedKernel.Api;
using Procurement.Application.DTOs;
using Procurement.Domain.Enums;

namespace Procurement.Application.Services.Interfaces;

public interface IGoodsReceiptService
{
    Task<PaginatedResponse<GoodsReceiptDto>> GetAllAsync(PaginationParams pagination);
    Task<GoodsReceiptDto?> GetByIdAsync(Guid id);
    Task<GoodsReceiptDto?> GetByNumberAsync(string receiptNumber);
    Task<List<GoodsReceiptDto>> GetByStatusAsync(GoodsReceiptStatus status);
    Task<List<GoodsReceiptDto>> GetByPurchaseOrderAsync(Guid purchaseOrderId);
    Task<List<GoodsReceiptDto>> GetByVendorAsync(Guid vendorId);
    Task<GoodsReceiptDto> CreateAsync(CreateGoodsReceiptDto dto);

    /// <summary>
    /// Auto-create a Draft goods receipt pre-filled from a confirmed Purchase Order's outstanding
    /// quantities, so the user only reviews/confirms. Idempotent (skips if the PO already has a
    /// receipt). Returns null when nothing was created.
    /// </summary>
    Task<GoodsReceiptDto?> CreateDraftFromPurchaseOrderAsync(Guid purchaseOrderId, Guid userId);

    Task<GoodsReceiptDto> UpdateAsync(Guid id, UpdateGoodsReceiptDto dto);
    Task<GoodsReceiptDto> PostAsync(Guid id, Guid postedByUserId, Guid? fiscalPeriodId = null);
    Task<GoodsReceiptDto> CancelAsync(Guid id);
    Task<GoodsReceiptDto> InspectLinesAsync(Guid id, List<InspectGoodsReceiptLineDto> inspections, Guid inspectedByUserId);
    Task DeleteAsync(Guid id);
}
