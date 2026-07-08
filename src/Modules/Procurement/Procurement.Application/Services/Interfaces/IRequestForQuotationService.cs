using Nexcore.SharedKernel.Api;
using Procurement.Application.DTOs;
using Procurement.Domain.Enums;

namespace Procurement.Application.Services.Interfaces;

public interface IRequestForQuotationService
{
    Task<PaginatedResponse<RequestForQuotationDto>> GetAllAsync(PaginationParams pagination);
    Task<RequestForQuotationDto?> GetByIdAsync(Guid id);
    Task<RequestForQuotationDto?> GetByNumberAsync(string rfqNumber);
    Task<List<RequestForQuotationDto>> GetByStatusAsync(RFQStatus status);
    Task<List<RequestForQuotationDto>> GetByRequisitionAsync(Guid requisitionId);
    Task<RequestForQuotationDto> CreateAsync(CreateRFQDto dto);
    Task<RequestForQuotationDto> UpdateAsync(Guid id, UpdateRFQDto dto);
    Task<RequestForQuotationDto> SendToVendorsAsync(Guid id);
    Task<RequestForQuotationDto> CloseAsync(Guid id);
    Task<RequestForQuotationDto> CancelAsync(Guid id);
    Task DeleteAsync(Guid id);

    // ── Vendor Quotations ─────────────────────────────────────────────────────
    Task<VendorQuotationDto> SubmitQuotationAsync(Guid rfqId, SubmitVendorQuotationDto dto);
    Task<RequestForQuotationDto> EvaluateQuotationsAsync(Guid rfqId, List<EvaluateQuotationDto> evaluations);

    /// <summary>Awards the RFQ to the recommended quotation and creates a PO draft.</summary>
    Task<PurchaseOrderDto> AwardAndCreatePurchaseOrderAsync(Guid rfqId, Guid quotationId);
}
