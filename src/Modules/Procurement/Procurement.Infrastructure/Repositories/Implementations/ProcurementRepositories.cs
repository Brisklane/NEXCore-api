using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Repository;
using Procurement.Domain.Entities;
using Procurement.Domain.Enums;
using Procurement.Infrastructure.Persistence;
using Procurement.Infrastructure.Repositories.Interfaces;

namespace Procurement.Infrastructure.Repositories.Implementations;

// ── Document Sequence ─────────────────────────────────────────────────────────

public class DocumentSequenceRepository : TenantAwareRepository<DocumentSequence>, IDocumentSequenceRepository
{
    public DocumentSequenceRepository(ProcurementDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<DocumentSequence?> GetByDocumentTypeAsync(ProcurementDocumentType documentType)
        => await FirstOrDefaultAsync(s => s.DocumentType == documentType && s.IsActive);

    public async Task<List<DocumentSequence>> GetAllActiveAsync()
    {
        var list = await FindAsync(s => s.IsActive);
        return list.OrderBy(s => s.DocumentType.ToString()).ToList();
    }
}

// ── Vendor ────────────────────────────────────────────────────────────────────

public class VendorRepository : TenantAwareRepository<Vendor>, IVendorRepository
{
    public VendorRepository(ProcurementDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<Vendor?> GetByNumberAsync(string vendorNumber)
        => await FirstOrDefaultAsync(v => v.VendorNumber == vendorNumber && !v.IsDeleted);

    public async Task<Vendor?> GetWithFullDetailsAsync(Guid id)
    {
        var (company, branch, bu) = GetTenantContext();
        return await DbSet
            .Include(v => v.Contacts)
            .Include(v => v.Addresses)
            .Include(v => v.BankAccounts)
            .Include(v => v.VendorCategory)
            .FirstOrDefaultAsync(v => v.Id == id && v.CompanyId == company &&
                                      v.BranchId == branch && v.BusinessUnitId == bu && !v.IsDeleted);
    }

    public async Task<List<Vendor>> GetByStatusAsync(VendorStatus status)
    {
        var list = await FindAsync(v => v.Status == status && !v.IsDeleted);
        return list.OrderBy(v => v.Name).ToList();
    }

    public async Task<List<Vendor>> GetByOnboardingStatusAsync(VendorOnboardingStatus status)
    {
        var list = await FindAsync(v => v.OnboardingStatus == status && !v.IsDeleted);
        return list.OrderBy(v => v.Name).ToList();
    }

    public async Task<List<Vendor>> GetPreferredVendorsAsync()
    {
        var list = await FindAsync(v => v.IsPreferredVendor && v.Status == VendorStatus.Active && !v.IsDeleted);
        return list.OrderBy(v => v.Name).ToList();
    }

    public async Task<List<Vendor>> GetByCategoryAsync(Guid categoryId)
    {
        var list = await FindAsync(v => v.VendorCategoryId == categoryId && !v.IsDeleted);
        return list.OrderBy(v => v.Name).ToList();
    }
}

// ── Purchase Requisition ──────────────────────────────────────────────────────

public class PurchaseRequisitionRepository : TenantAwareRepository<PurchaseRequisition>, IPurchaseRequisitionRepository
{
    public PurchaseRequisitionRepository(ProcurementDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<PurchaseRequisition?> GetByNumberAsync(string requisitionNumber)
        => await FirstOrDefaultAsync(r => r.RequisitionNumber == requisitionNumber && !r.IsDeleted);

    public async Task<PurchaseRequisition?> GetWithLinesAsync(Guid id)
    {
        var (company, branch, bu) = GetTenantContext();
        return await DbSet
            .Include(r => r.Lines)
            .Include(r => r.SuggestedVendor)
            .FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == company &&
                                       r.BranchId == branch && r.BusinessUnitId == bu && !r.IsDeleted);
    }

    public async Task<List<PurchaseRequisition>> GetByStatusAsync(RequisitionStatus status)
    {
        var list = await FindAsync(r => r.Status == status && !r.IsDeleted);
        return list.OrderByDescending(r => r.RequestDate).ToList();
    }

    public async Task<List<PurchaseRequisition>> GetByRequesterAsync(Guid requestedByUserId)
    {
        var list = await FindAsync(r => r.RequestedByUserId == requestedByUserId && !r.IsDeleted);
        return list.OrderByDescending(r => r.RequestDate).ToList();
    }

    public async Task<List<PurchaseRequisition>> GetByDepartmentAsync(Guid departmentId)
    {
        var list = await FindAsync(r => r.DepartmentId == departmentId && !r.IsDeleted);
        return list.OrderByDescending(r => r.RequestDate).ToList();
    }

    public async Task<List<PurchaseRequisition>> GetPendingApprovalAsync()
    {
        var list = await FindAsync(r => r.Status == RequisitionStatus.UnderApproval && !r.IsDeleted);
        return list.OrderBy(r => r.RequestDate).ToList();
    }
}

// ── Request For Quotation ─────────────────────────────────────────────────────

public class RequestForQuotationRepository : TenantAwareRepository<RequestForQuotation>, IRequestForQuotationRepository
{
    public RequestForQuotationRepository(ProcurementDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<RequestForQuotation?> GetByNumberAsync(string rfqNumber)
        => await FirstOrDefaultAsync(r => r.RFQNumber == rfqNumber && !r.IsDeleted);

    public async Task<RequestForQuotation?> GetWithFullDetailsAsync(Guid id)
    {
        var (company, branch, bu) = GetTenantContext();
        return await DbSet
            .Include(r => r.Lines)
            .Include(r => r.InvitedVendors)
            .Include(r => r.VendorQuotations).ThenInclude(q => q.Lines)
            .FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == company &&
                                       r.BranchId == branch && r.BusinessUnitId == bu && !r.IsDeleted);
    }

    public async Task<List<RequestForQuotation>> GetByStatusAsync(RFQStatus status)
    {
        var list = await FindAsync(r => r.Status == status && !r.IsDeleted);
        return list.OrderByDescending(r => r.IssueDate).ToList();
    }

    public async Task<List<RequestForQuotation>> GetByRequisitionAsync(Guid requisitionId)
    {
        var list = await FindAsync(r => r.RequisitionId == requisitionId && !r.IsDeleted);
        return list.OrderByDescending(r => r.IssueDate).ToList();
    }
}

public class VendorQuotationRepository : TenantAwareRepository<VendorQuotation>, IVendorQuotationRepository
{
    public VendorQuotationRepository(ProcurementDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<VendorQuotation?> GetByNumberAsync(string quotationNumber)
        => await FirstOrDefaultAsync(q => q.QuotationNumber == quotationNumber && !q.IsDeleted);

    public async Task<VendorQuotation?> GetWithLinesAsync(Guid id)
    {
        var (company, branch, bu) = GetTenantContext();
        return await DbSet
            .Include(q => q.Lines)
            .FirstOrDefaultAsync(q => q.Id == id && q.CompanyId == company &&
                                      q.BranchId == branch && q.BusinessUnitId == bu && !q.IsDeleted);
    }

    public async Task<List<VendorQuotation>> GetByRFQAsync(Guid rfqId)
    {
        var list = await FindAsync(q => q.RFQId == rfqId && !q.IsDeleted);
        return list.OrderByDescending(q => q.TotalAmount).ToList();
    }

    public async Task<List<VendorQuotation>> GetByVendorAsync(Guid vendorId)
    {
        var list = await FindAsync(q => q.VendorId == vendorId && !q.IsDeleted);
        return list.OrderByDescending(q => q.SubmissionDate).ToList();
    }
}

// ── Purchase Contract ─────────────────────────────────────────────────────────

public class PurchaseContractRepository : TenantAwareRepository<PurchaseContract>, IPurchaseContractRepository
{
    public PurchaseContractRepository(ProcurementDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<PurchaseContract?> GetByNumberAsync(string contractNumber)
        => await FirstOrDefaultAsync(c => c.ContractNumber == contractNumber && !c.IsDeleted);

    public async Task<PurchaseContract?> GetWithLinesAsync(Guid id)
    {
        var (company, branch, bu) = GetTenantContext();
        return await DbSet
            .Include(c => c.Lines)
            .Include(c => c.Vendor)
            .FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == company &&
                                      c.BranchId == branch && c.BusinessUnitId == bu && !c.IsDeleted);
    }

    public async Task<List<PurchaseContract>> GetByStatusAsync(PurchaseContractStatus status)
    {
        var list = await FindAsync(c => c.Status == status && !c.IsDeleted);
        return list.OrderBy(c => c.EndDate).ToList();
    }

    public async Task<List<PurchaseContract>> GetByVendorAsync(Guid vendorId)
    {
        var list = await FindAsync(c => c.VendorId == vendorId && !c.IsDeleted);
        return list.OrderByDescending(c => c.StartDate).ToList();
    }

    public async Task<List<PurchaseContract>> GetActiveAsync()
    {
        var list = await FindAsync(c => c.Status == PurchaseContractStatus.Active &&
                                        c.EndDate >= DateTime.UtcNow && !c.IsDeleted);
        return list.OrderBy(c => c.EndDate).ToList();
    }

    public async Task<List<PurchaseContract>> GetExpiringAsync(int withinDays)
    {
        var threshold = DateTime.UtcNow.AddDays(withinDays);
        var list = await FindAsync(c => c.Status == PurchaseContractStatus.Active &&
                                        c.EndDate >= DateTime.UtcNow &&
                                        c.EndDate <= threshold && !c.IsDeleted);
        return list.OrderBy(c => c.EndDate).ToList();
    }
}

// ── Purchase Order ────────────────────────────────────────────────────────────

public class PurchaseOrderRepository : TenantAwareRepository<PurchaseOrder>, IPurchaseOrderRepository
{
    public PurchaseOrderRepository(ProcurementDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<PurchaseOrder?> GetByNumberAsync(string orderNumber)
        => await FirstOrDefaultAsync(o => o.OrderNumber == orderNumber && !o.IsDeleted);

    public async Task<PurchaseOrder?> GetWithLinesAsync(Guid id)
    {
        var (company, branch, bu) = GetTenantContext();
        return await DbSet
            .Include(o => o.Lines)
            .Include(o => o.Vendor)
            .FirstOrDefaultAsync(o => o.Id == id && o.CompanyId == company &&
                                      o.BranchId == branch && o.BusinessUnitId == bu && !o.IsDeleted);
    }

    public async Task<PurchaseOrder?> GetWithFullDetailsAsync(Guid id)
    {
        var (company, branch, bu) = GetTenantContext();
        return await DbSet
            .Include(o => o.Lines)
            .Include(o => o.Vendor)
            .Include(o => o.GoodsReceipts)
            .Include(o => o.PurchaseInvoices)
            .Include(o => o.Approvals)
            .FirstOrDefaultAsync(o => o.Id == id && o.CompanyId == company &&
                                      o.BranchId == branch && o.BusinessUnitId == bu && !o.IsDeleted);
    }

    public async Task<List<PurchaseOrder>> GetByStatusAsync(PurchaseOrderStatus status)
    {
        var list = await FindAsync(o => o.Status == status && !o.IsDeleted);
        return list.OrderByDescending(o => o.OrderDate).ToList();
    }

    public async Task<List<PurchaseOrder>> GetByVendorAsync(Guid vendorId)
    {
        var list = await FindAsync(o => o.VendorId == vendorId && !o.IsDeleted);
        return list.OrderByDescending(o => o.OrderDate).ToList();
    }

    public async Task<List<PurchaseOrder>> GetPendingReceiptAsync()
    {
        var list = await FindAsync(o => !o.IsDeleted &&
            (o.Status == PurchaseOrderStatus.Confirmed ||
             o.Status == PurchaseOrderStatus.SentToVendor ||
             o.Status == PurchaseOrderStatus.Acknowledged ||
             o.Status == PurchaseOrderStatus.PartiallyReceived));
        return list.OrderBy(o => o.ExpectedDeliveryDate).ToList();
    }

    public async Task<List<PurchaseOrder>> GetToInvoiceAsync()
    {
        var (company, branch, bu) = GetTenantContext();
        return await DbSet
            .Include(o => o.Lines)
            .Where(o => o.CompanyId == company && o.BranchId == branch && o.BusinessUnitId == bu &&
                        !o.IsDeleted &&
                        (o.Status == PurchaseOrderStatus.PartiallyReceived ||
                         o.Status == PurchaseOrderStatus.FullyReceived ||
                         o.Status == PurchaseOrderStatus.PartiallyInvoiced) &&
                        o.Lines.Any(l => l.QuantityAccepted > l.QuantityInvoiced))
            .OrderBy(o => o.OrderDate)
            .ToListAsync();
    }
}

// ── Goods Receipt ─────────────────────────────────────────────────────────────

public class GoodsReceiptRepository : TenantAwareRepository<GoodsReceipt>, IGoodsReceiptRepository
{
    public GoodsReceiptRepository(ProcurementDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<GoodsReceipt?> GetByNumberAsync(string receiptNumber)
        => await FirstOrDefaultAsync(r => r.ReceiptNumber == receiptNumber && !r.IsDeleted);

    public async Task<GoodsReceipt?> GetWithLinesAsync(Guid id)
    {
        var (company, branch, bu) = GetTenantContext();
        return await DbSet
            .Include(r => r.Lines)
            .Include(r => r.Vendor)
            .FirstOrDefaultAsync(r => r.Id == id && r.CompanyId == company &&
                                      r.BranchId == branch && r.BusinessUnitId == bu && !r.IsDeleted);
    }

    public async Task<List<GoodsReceipt>> GetByStatusAsync(GoodsReceiptStatus status)
    {
        var list = await FindAsync(r => r.Status == status && !r.IsDeleted);
        return list.OrderByDescending(r => r.ReceiptDate).ToList();
    }

    public async Task<List<GoodsReceipt>> GetByPurchaseOrderAsync(Guid purchaseOrderId)
    {
        var list = await FindAsync(r => r.PurchaseOrderId == purchaseOrderId && !r.IsDeleted);
        return list.OrderByDescending(r => r.ReceiptDate).ToList();
    }

    public async Task<List<GoodsReceipt>> GetByVendorAsync(Guid vendorId)
    {
        var list = await FindAsync(r => r.VendorId == vendorId && !r.IsDeleted);
        return list.OrderByDescending(r => r.ReceiptDate).ToList();
    }
}

// ── Purchase Invoice ──────────────────────────────────────────────────────────

public class PurchaseInvoiceRepository : TenantAwareRepository<PurchaseInvoice>, IPurchaseInvoiceRepository
{
    public PurchaseInvoiceRepository(ProcurementDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<PurchaseInvoice?> GetByNumberAsync(string invoiceNumber)
        => await FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber && !i.IsDeleted);

    public async Task<PurchaseInvoice?> GetWithLinesAsync(Guid id)
    {
        var (company, branch, bu) = GetTenantContext();
        return await DbSet
            .Include(i => i.Lines)
            .Include(i => i.Vendor)
            .FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == company &&
                                      i.BranchId == branch && i.BusinessUnitId == bu && !i.IsDeleted);
    }

    public async Task<PurchaseInvoice?> GetWithFullDetailsAsync(Guid id)
    {
        var (company, branch, bu) = GetTenantContext();
        return await DbSet
            .Include(i => i.Lines)
            .Include(i => i.Vendor)
            .Include(i => i.MatchRecords)
            .Include(i => i.PaymentAllocations)
            .FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == company &&
                                      i.BranchId == branch && i.BusinessUnitId == bu && !i.IsDeleted);
    }

    public async Task<List<PurchaseInvoice>> GetByStatusAsync(PurchaseInvoiceStatus status)
    {
        var list = await FindAsync(i => i.Status == status && !i.IsDeleted);
        return list.OrderByDescending(i => i.InvoiceDate).ToList();
    }

    public async Task<List<PurchaseInvoice>> GetByVendorAsync(Guid vendorId)
    {
        var list = await FindAsync(i => i.VendorId == vendorId && !i.IsDeleted);
        return list.OrderByDescending(i => i.InvoiceDate).ToList();
    }

    public async Task<List<PurchaseInvoice>> GetByPurchaseOrderAsync(Guid purchaseOrderId)
    {
        var list = await FindAsync(i => i.PurchaseOrderId == purchaseOrderId && !i.IsDeleted);
        return list.OrderByDescending(i => i.InvoiceDate).ToList();
    }

    public async Task<List<PurchaseInvoice>> GetOverdueAsync()
    {
        var today = DateTime.UtcNow;
        var list = await FindAsync(i => !i.IsDeleted &&
            i.DueDate < today &&
            (i.PaymentStatus == InvoicePaymentStatus.NotPaid ||
             i.PaymentStatus == InvoicePaymentStatus.PartiallyPaid));
        return list.OrderBy(i => i.DueDate).ToList();
    }

    public async Task<List<PurchaseInvoice>> GetPendingPaymentAsync()
    {
        var list = await FindAsync(i => !i.IsDeleted &&
            i.Status == PurchaseInvoiceStatus.Posted &&
            (i.PaymentStatus == InvoicePaymentStatus.NotPaid ||
             i.PaymentStatus == InvoicePaymentStatus.PartiallyPaid));
        return list.OrderBy(i => i.DueDate).ToList();
    }

    public async Task<bool> VendorInvoiceNumberExistsAsync(Guid vendorId, string vendorInvoiceNumber, Guid? excludeId = null)
    {
        var (company, branch, bu) = GetTenantContext();
        return await DbSet.AnyAsync(i =>
            i.CompanyId == company && i.VendorId == vendorId &&
            i.VendorInvoiceNumber == vendorInvoiceNumber &&
            !i.IsDeleted && (excludeId == null || i.Id != excludeId));
    }
}

// ── Vendor Payment ────────────────────────────────────────────────────────────

public class VendorPaymentRepository : TenantAwareRepository<VendorPayment>, IVendorPaymentRepository
{
    public VendorPaymentRepository(ProcurementDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<VendorPayment?> GetByNumberAsync(string paymentNumber)
        => await FirstOrDefaultAsync(p => p.PaymentNumber == paymentNumber && !p.IsDeleted);

    public async Task<VendorPayment?> GetWithAllocationsAsync(Guid id)
    {
        var (company, branch, bu) = GetTenantContext();
        return await DbSet
            .Include(p => p.Allocations).ThenInclude(a => a.Invoice)
            .Include(p => p.Vendor)
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == company &&
                                      p.BranchId == branch && p.BusinessUnitId == bu && !p.IsDeleted);
    }

    public async Task<List<VendorPayment>> GetByStatusAsync(VendorPaymentStatus status)
    {
        var list = await FindAsync(p => p.Status == status && !p.IsDeleted);
        return list.OrderByDescending(p => p.PaymentDate).ToList();
    }

    public async Task<List<VendorPayment>> GetByVendorAsync(Guid vendorId)
    {
        var list = await FindAsync(p => p.VendorId == vendorId && !p.IsDeleted);
        return list.OrderByDescending(p => p.PaymentDate).ToList();
    }
}
