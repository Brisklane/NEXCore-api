using Nexcore.SharedKernel.Api;
using Procurement.Application.DTOs;
using Procurement.Domain.Enums;

namespace Procurement.Application.Services.Interfaces;

public interface IPurchaseOrderService
{
    Task<PaginatedResponse<PurchaseOrderDto>> GetAllAsync(PaginationParams pagination);
    Task<PurchaseOrderDto?> GetByIdAsync(Guid id);
    Task<PurchaseOrderDto?> GetByNumberAsync(string orderNumber);
    Task<List<PurchaseOrderDto>> GetByStatusAsync(PurchaseOrderStatus status);
    Task<List<PurchaseOrderDto>> GetByVendorAsync(Guid vendorId);
    Task<PaginatedResponse<PurchaseOrderDto>> GetPendingReceiptAsync(PaginationParams pagination);
    Task<PaginatedResponse<PurchaseOrderDto>> GetToInvoiceAsync(PaginationParams pagination);
    Task<PurchaseOrderDto> CreateAsync(CreatePurchaseOrderDto dto);
    Task<PurchaseOrderDto> UpdateAsync(Guid id, UpdatePurchaseOrderDto dto);
    Task<PurchaseOrderDto> ConfirmAsync(Guid id, Guid confirmedByUserId);
    Task<PurchaseOrderDto> SendToVendorAsync(Guid id);
    Task<PurchaseOrderDto> AcknowledgeAsync(Guid id);
    Task<PurchaseOrderDto> CloseAsync(Guid id);
    Task<PurchaseOrderDto> CancelAsync(Guid id, CancelPurchaseOrderDto dto);
    Task DeleteAsync(Guid id);
}
