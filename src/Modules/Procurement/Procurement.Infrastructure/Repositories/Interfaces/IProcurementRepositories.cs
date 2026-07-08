using Nexcore.SharedKernel.Repository;
using Procurement.Domain.Entities;
using Procurement.Domain.Enums;

namespace Procurement.Infrastructure.Repositories.Interfaces;

// ── Document Sequence ─────────────────────────────────────────────────────────

public interface IDocumentSequenceRepository : IRepository<DocumentSequence>
{
    Task<DocumentSequence?> GetByDocumentTypeAsync(ProcurementDocumentType documentType);
    Task<List<DocumentSequence>> GetAllActiveAsync();
}

// ── Vendor ────────────────────────────────────────────────────────────────────

public interface IVendorRepository : IRepository<Vendor>
{
    Task<Vendor?> GetByNumberAsync(string vendorNumber);
    Task<Vendor?> GetWithFullDetailsAsync(Guid id);
    Task<List<Vendor>> GetByStatusAsync(VendorStatus status);
    Task<List<Vendor>> GetByOnboardingStatusAsync(VendorOnboardingStatus status);
    Task<List<Vendor>> GetPreferredVendorsAsync();
    Task<List<Vendor>> GetByCategoryAsync(Guid categoryId);
}

// ── Purchase Requisition ──────────────────────────────────────────────────────

public interface IPurchaseRequisitionRepository : IRepository<PurchaseRequisition>
{
    Task<PurchaseRequisition?> GetByNumberAsync(string requisitionNumber);
    Task<PurchaseRequisition?> GetWithLinesAsync(Guid id);
    Task<List<PurchaseRequisition>> GetByStatusAsync(RequisitionStatus status);
    Task<List<PurchaseRequisition>> GetByRequesterAsync(Guid requestedByUserId);
    Task<List<PurchaseRequisition>> GetByDepartmentAsync(Guid departmentId);
    Task<List<PurchaseRequisition>> GetPendingApprovalAsync();
}

// ── Request For Quotation ─────────────────────────────────────────────────────

public interface IRequestForQuotationRepository : IRepository<RequestForQuotation>
{
    Task<RequestForQuotation?> GetByNumberAsync(string rfqNumber);
    Task<RequestForQuotation?> GetWithFullDetailsAsync(Guid id);
    Task<List<RequestForQuotation>> GetByStatusAsync(RFQStatus status);
    Task<List<RequestForQuotation>> GetByRequisitionAsync(Guid requisitionId);
}

public interface IVendorQuotationRepository : IRepository<VendorQuotation>
{
    Task<VendorQuotation?> GetByNumberAsync(string quotationNumber);
    Task<VendorQuotation?> GetWithLinesAsync(Guid id);
    Task<List<VendorQuotation>> GetByRFQAsync(Guid rfqId);
    Task<List<VendorQuotation>> GetByVendorAsync(Guid vendorId);
}

// ── Purchase Contract ─────────────────────────────────────────────────────────

public interface IPurchaseContractRepository : IRepository<PurchaseContract>
{
    Task<PurchaseContract?> GetByNumberAsync(string contractNumber);
    Task<PurchaseContract?> GetWithLinesAsync(Guid id);
    Task<List<PurchaseContract>> GetByStatusAsync(PurchaseContractStatus status);
    Task<List<PurchaseContract>> GetByVendorAsync(Guid vendorId);
    Task<List<PurchaseContract>> GetActiveAsync();
    Task<List<PurchaseContract>> GetExpiringAsync(int withinDays);
}

// ── Purchase Order ────────────────────────────────────────────────────────────

public interface IPurchaseOrderRepository : IRepository<PurchaseOrder>
{
    Task<PurchaseOrder?> GetByNumberAsync(string orderNumber);
    Task<PurchaseOrder?> GetWithLinesAsync(Guid id);
    Task<PurchaseOrder?> GetWithFullDetailsAsync(Guid id);
    Task<List<PurchaseOrder>> GetByStatusAsync(PurchaseOrderStatus status);
    Task<List<PurchaseOrder>> GetByVendorAsync(Guid vendorId);
    Task<List<PurchaseOrder>> GetPendingReceiptAsync();
    Task<List<PurchaseOrder>> GetToInvoiceAsync();
}

// ── Goods Receipt ─────────────────────────────────────────────────────────────

public interface IGoodsReceiptRepository : IRepository<GoodsReceipt>
{
    Task<GoodsReceipt?> GetByNumberAsync(string receiptNumber);
    Task<GoodsReceipt?> GetWithLinesAsync(Guid id);
    Task<List<GoodsReceipt>> GetByStatusAsync(GoodsReceiptStatus status);
    Task<List<GoodsReceipt>> GetByPurchaseOrderAsync(Guid purchaseOrderId);
    Task<List<GoodsReceipt>> GetByVendorAsync(Guid vendorId);
}

// ── Purchase Invoice ──────────────────────────────────────────────────────────

public interface IPurchaseInvoiceRepository : IRepository<PurchaseInvoice>
{
    Task<PurchaseInvoice?> GetByNumberAsync(string invoiceNumber);
    Task<PurchaseInvoice?> GetWithLinesAsync(Guid id);
    Task<PurchaseInvoice?> GetWithFullDetailsAsync(Guid id);
    Task<List<PurchaseInvoice>> GetByStatusAsync(PurchaseInvoiceStatus status);
    Task<List<PurchaseInvoice>> GetByVendorAsync(Guid vendorId);
    Task<List<PurchaseInvoice>> GetByPurchaseOrderAsync(Guid purchaseOrderId);
    Task<List<PurchaseInvoice>> GetOverdueAsync();
    Task<List<PurchaseInvoice>> GetPendingPaymentAsync();
    Task<bool> VendorInvoiceNumberExistsAsync(Guid vendorId, string vendorInvoiceNumber, Guid? excludeId = null);
}

// ── Vendor Payment ────────────────────────────────────────────────────────────

public interface IVendorPaymentRepository : IRepository<VendorPayment>
{
    Task<VendorPayment?> GetByNumberAsync(string paymentNumber);
    Task<VendorPayment?> GetWithAllocationsAsync(Guid id);
    Task<List<VendorPayment>> GetByStatusAsync(VendorPaymentStatus status);
    Task<List<VendorPayment>> GetByVendorAsync(Guid vendorId);
}
