using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Repository;
using Sales.Domain.Entities;
using Sales.Domain.Enums;
using Sales.Infrastructure.Persistence;
using Sales.Infrastructure.Repositories.Interfaces;

namespace Sales.Infrastructure.Repositories.Implementations;

// ?? Sales Order ???????????????????????????????????????????????????????????????

public class SalesOrderRepository : TenantAwareRepository<SalesOrder>, ISalesOrderRepository
{
    public SalesOrderRepository(SalesDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<SalesOrder?> GetByNumberAsync(string orderNumber)
        => await FirstOrDefaultAsync(o => o.OrderNumber == orderNumber);

    public async Task<SalesOrder?> GetWithLinesAsync(Guid id)
    {
        var (company, branch, bu) = GetTenantContext();
        return await DbSet
            .Include(o => o.Lines).ThenInclude(l => l.Addons)
            .FirstOrDefaultAsync(o => o.Id == id && o.CompanyId == company &&
                                      o.BranchId == branch && o.BusinessUnitId == bu && !o.IsDeleted);
    }

    public async Task<SalesOrder?> GetWithFullDetailsAsync(Guid id)
    {
        var (company, branch, bu) = GetTenantContext();
        return await DbSet
            .Include(o => o.Lines).ThenInclude(l => l.Addons)
            .Include(o => o.Payments)
            .Include(o => o.StatusHistory.OrderByDescending(s => s.ChangedAt).Take(20))
            .FirstOrDefaultAsync(o => o.Id == id && o.CompanyId == company &&
                                      o.BranchId == branch && o.BusinessUnitId == bu && !o.IsDeleted);
    }

    public async Task<List<SalesOrder>> GetByContactAsync(Guid contactId)
    {
        var list = await FindAsync(o => o.ContactId == contactId && !o.IsDeleted);
        return list.OrderByDescending(o => o.OrderDate).ToList();
    }

    public async Task<List<SalesOrder>> GetByStatusAsync(SalesOrderStatus status)
    {
        var list = await FindAsync(o => o.Status == status && !o.IsDeleted);
        return list.OrderByDescending(o => o.OrderDate).ToList();
    }

    public async Task<List<SalesOrder>> GetByChannelAsync(SalesChannel channel)
    {
        var list = await FindAsync(o => o.SalesChannel == channel && !o.IsDeleted);
        return list.OrderByDescending(o => o.OrderDate).ToList();
    }

    public async Task<SalesOrder?> GetByOfflineNumberAsync(string offlineOrderNumber)
        => await FirstOrDefaultAsync(o => o.OfflineOrderNumber == offlineOrderNumber);

    public async Task<List<SalesOrder>> GetStoreOrderQueueAsync(Guid storeId)
    {
        var (company, branch, bu) = GetTenantContext();
        return await DbSet
            .Where(o => o.OriginBranchId == storeId &&
                        o.PlacedAt != null &&
                        o.Status != SalesOrderStatus.Draft &&
                        o.Status != SalesOrderStatus.PosParked &&
                        o.Status != SalesOrderStatus.PaymentPending &&
                        o.Status != SalesOrderStatus.Cancelled &&
                        o.Status != SalesOrderStatus.Rejected &&
                        o.CompanyId == company && o.BranchId == branch &&
                        o.BusinessUnitId == bu && !o.IsDeleted)
            .Include(o => o.Lines)
            .OrderBy(o => o.PlacedAt)
            .ToListAsync();
    }

    public async Task<int> GetPendingOnlineOrderCountAsync(Guid storeId)
    {
        var (company, branch, bu) = GetTenantContext();
        return await DbSet
            .Where(o => o.OriginBranchId == storeId &&
                        o.Status == SalesOrderStatus.Placed &&
                        (o.SalesChannel == SalesChannel.OnlineApp ||
                         o.SalesChannel == SalesChannel.OnlineStore ||
                         o.SalesChannel == SalesChannel.Marketplace) &&
                        o.CompanyId == company && o.BranchId == branch &&
                        o.BusinessUnitId == bu && !o.IsDeleted)
            .CountAsync();
    }

    public async Task<List<SalesOrder>> GetActiveDraftsByContactAsync(Guid contactId)
    {
        var list = await FindAsync(o => o.ContactId == contactId &&
                                        o.Status == SalesOrderStatus.Draft &&
                                        (o.ExpiresAt == null || o.ExpiresAt > DateTime.UtcNow) &&
                                        !o.IsDeleted);
        return list.OrderByDescending(o => o.OrderDate).ToList();
    }

    public async Task<List<SalesOrder>> GetOrdersToInvoiceAsync()
    {
        var (company, branch, bu) = GetTenantContext();
        return await DbSet
            .Include(o => o.Lines)
            .Where(o => o.CompanyId == company && o.BranchId == branch && o.BusinessUnitId == bu &&
                        !o.IsDeleted &&
                        (o.InvoiceStatus == OrderInvoiceStatus.ToInvoice ||
                         o.InvoiceStatus == OrderInvoiceStatus.PartiallyInvoiced))
            .OrderByDescending(o => o.PlacedAt)
            .ToListAsync();
    }

    public async Task SetStatusAsync(Guid id, SalesOrderStatus status)
        => await DbSet.Where(o => o.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.Status, status));

    public async Task AddStatusHistoryAsync(SalesOrderStatusHistory history)
    {
        await Context.Set<SalesOrderStatusHistory>().AddAsync(history);
        await Context.SaveChangesAsync();
    }
}

public class SalesOrderLineRepository : TenantAwareRepository<SalesOrderLine>, ISalesOrderLineRepository
{
    public SalesOrderLineRepository(SalesDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<List<SalesOrderLine>> GetByOrderAsync(Guid salesOrderId)
    {
        var (company, branch, bu) = GetTenantContext();
        return await DbSet
            .Where(l => l.SalesOrderId == salesOrderId && l.CompanyId == company &&
                        l.BranchId == branch && l.BusinessUnitId == bu && !l.IsDeleted)
            .Include(l => l.Addons)
            .OrderBy(l => l.LineNumber)
            .ToListAsync();
    }
}

// ?? Quotation ?????????????????????????????????????????????????????????????????

public class QuotationRepository : TenantAwareRepository<Quotation>, IQuotationRepository
{
    public QuotationRepository(SalesDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<Quotation?> GetByNumberAsync(string quotationNumber)
        => await FirstOrDefaultAsync(q => q.QuotationNumber == quotationNumber);

    public async Task<Quotation?> GetWithLinesAsync(Guid id)
    {
        var (company, branch, bu) = GetTenantContext();
        return await DbSet
            .Include(q => q.Lines)
            .FirstOrDefaultAsync(q => q.Id == id && q.CompanyId == company &&
                                      q.BranchId == branch && q.BusinessUnitId == bu && !q.IsDeleted);
    }

    public async Task<List<Quotation>> GetByContactAsync(Guid contactId)
    {
        var list = await FindAsync(q => q.ContactId == contactId && !q.IsDeleted);
        return list.OrderByDescending(q => q.QuotationDate).ToList();
    }

    public async Task<List<Quotation>> GetByStatusAsync(QuotationStatus status)
    {
        var list = await FindAsync(q => q.Status == status && !q.IsDeleted);
        return list.OrderByDescending(q => q.QuotationDate).ToList();
    }
}

// ?? Delivery ??????????????????????????????????????????????????????????????????

public class DeliveryRepository : TenantAwareRepository<Delivery>, IDeliveryRepository
{
    public DeliveryRepository(SalesDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<Delivery?> GetByNumberAsync(string deliveryNumber)
        => await FirstOrDefaultAsync(d => d.DeliveryNumber == deliveryNumber);

    public async Task<Delivery?> GetWithLinesAsync(Guid id)
    {
        var (company, branch, bu) = GetTenantContext();
        return await DbSet
            .Include(d => d.Lines)
                .ThenInclude(l => l.SalesOrderLine)
            .Include(d => d.SalesOrder)
            .FirstOrDefaultAsync(d => d.Id == id && d.CompanyId == company &&
                                      d.BranchId == branch && d.BusinessUnitId == bu && !d.IsDeleted);
    }

    public async Task<List<Delivery>> GetByOrderAsync(Guid salesOrderId)
    {
        var list = await FindAsync(d => d.SalesOrderId == salesOrderId && !d.IsDeleted);
        return list.OrderByDescending(d => d.PlannedDeliveryDate).ToList();
    }

    public async Task<List<Delivery>> GetByStatusAsync(DeliveryStatus status)
    {
        var list = await FindAsync(d => d.Status == status && !d.IsDeleted);
        return list.OrderBy(d => d.PlannedDeliveryDate).ToList();
    }
}

// ?? Sales Invoice ?????????????????????????????????????????????????????????????

public class SalesInvoiceRepository : TenantAwareRepository<SalesInvoice>, ISalesInvoiceRepository
{
    public SalesInvoiceRepository(SalesDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<SalesInvoice?> GetByNumberAsync(string invoiceNumber)
        => await FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber);

    public async Task<SalesInvoice?> GetWithLinesAsync(Guid id)
    {
        var (company, branch, bu) = GetTenantContext();
        return await DbSet
            .Include(i => i.Lines)
            .FirstOrDefaultAsync(i => i.Id == id && i.CompanyId == company &&
                                      i.BranchId == branch && i.BusinessUnitId == bu && !i.IsDeleted);
    }

    public async Task<List<SalesInvoice>> GetByContactAsync(Guid contactId)
    {
        var list = await FindAsync(i => i.ContactId == contactId && !i.IsDeleted);
        return list.OrderByDescending(i => i.InvoiceDate).ToList();
    }

    public async Task<List<SalesInvoice>> GetByOrderAsync(Guid salesOrderId)
    {
        var list = await FindAsync(i => i.SalesOrderId == salesOrderId && !i.IsDeleted);
        return list.OrderByDescending(i => i.InvoiceDate).ToList();
    }

    public async Task<List<SalesInvoice>> GetByOrderWithLinesAsync(Guid salesOrderId)
    {
        var (company, branch, bu) = GetTenantContext();
        return await DbSet
            .Include(i => i.Lines)
            .Where(i => i.SalesOrderId == salesOrderId && i.CompanyId == company &&
                        i.BranchId == branch && i.BusinessUnitId == bu && !i.IsDeleted)
            .OrderByDescending(i => i.InvoiceDate)
            .ToListAsync();
    }

    public async Task<List<SalesInvoice>> GetUnpaidByOrderAsync(Guid salesOrderId)
    {
        var list = await FindAsync(i => i.SalesOrderId == salesOrderId &&
                                        i.BalanceDue > 0 &&
                                        i.Status != InvoiceStatus.Cancelled &&
                                        !i.IsDeleted);
        return list.OrderBy(i => i.InvoiceDate).ToList(); // oldest first for FIFO allocation
    }

    public async Task<List<SalesInvoice>> GetOverdueAsync()
    {
        var now = DateTime.UtcNow;
        var list = await FindAsync(i => i.DueDate < now &&
                                        i.Status != InvoiceStatus.Paid &&
                                        i.Status != InvoiceStatus.Cancelled &&
                                        !i.IsDeleted);
        return list.OrderBy(i => i.DueDate).ToList();
    }

}

// ?? Sales Team ????????????????????????????????????????????????????????????????

public class SalesTeamRepository : TenantAwareRepository<SalesTeam>, ISalesTeamRepository
{
    public SalesTeamRepository(SalesDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<List<SalesTeam>> GetActiveAsync()
    {
        var list = await FindAsync(t => t.IsActive && !t.IsDeleted);
        return list.OrderBy(t => t.Name).ToList();
    }

    public async Task<SalesTeam?> GetWithMembersAsync(Guid id)
    {
        var (company, branch, bu) = GetTenantContext();
        return await DbSet
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == id && t.CompanyId == company &&
                                      t.BranchId == branch && t.BusinessUnitId == bu && !t.IsDeleted);
    }
}

// ?? Payment Allocation ????????????????????????????????????????????????????????

public class PaymentAllocationRepository : TenantAwareRepository<PaymentAllocation>, IPaymentAllocationRepository
{
    public PaymentAllocationRepository(SalesDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<List<PaymentAllocation>> GetByPaymentAsync(Guid paymentId)
        => (await FindAsync(a => a.SalesPaymentId == paymentId && !a.IsDeleted)).ToList();

    public async Task<List<PaymentAllocation>> GetByInvoiceAsync(Guid invoiceId)
        => (await FindAsync(a => a.SalesInvoiceId == invoiceId && !a.IsDeleted)).ToList();
}

// ?? Sales Payment ?????????????????????????????????????????????????????????????

public class SalesPaymentRepository : TenantAwareRepository<SalesPayment>, ISalesPaymentRepository
{
    public SalesPaymentRepository(SalesDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<SalesPayment?> GetByNumberAsync(string paymentNumber)
        => await FirstOrDefaultAsync(p => p.PaymentNumber == paymentNumber);

    public async Task<List<SalesPayment>> GetByOrderAsync(Guid salesOrderId)
    {
        var list = await FindAsync(p => p.SalesOrderId == salesOrderId && !p.IsDeleted);
        return list.OrderByDescending(p => p.PaymentDate).ToList();
    }

    public async Task<List<SalesPayment>> GetByContactAsync(Guid contactId)
    {
        var list = await FindAsync(p => p.ContactId == contactId && !p.IsDeleted);
        return list.OrderByDescending(p => p.PaymentDate).ToList();
    }
}

// ?? POS ???????????????????????????????????????????????????????????????????????

public class PosTerminalRepository : TenantAwareRepository<PosTerminal>, IPosTerminalRepository
{
    public PosTerminalRepository(SalesDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<List<PosTerminal>> GetByBranchAsync(Guid storeId)
    {
        var list = await FindAsync(t => t.PosStoreId == storeId && !t.IsDeleted);
        return list.OrderBy(t => t.TerminalCode).ToList();
    }

    public async Task<PosTerminal?> GetByCodeAsync(string code, Guid storeId)
        => await FirstOrDefaultAsync(t => t.TerminalCode == code && t.PosStoreId == storeId);

    public async Task<List<PosTerminal>> GetActiveByBranchAsync(Guid storeId)
    {
        var list = await FindAsync(t => t.PosStoreId == storeId && t.IsActive && !t.IsDeleted);
        return list.OrderBy(t => t.TerminalCode).ToList();
    }

    public async Task<List<PosTerminal>> GetAllByCompanyAsync()
    {
        var (companyId, _, _) = GetTenantContext();
        return await DbSet.AsNoTracking()
            .Where(t => t.CompanyId == companyId && !t.IsDeleted)
            .OrderBy(t => t.TerminalCode)
            .ToListAsync();
    }

    public async Task<List<PosTerminal>> GetAllByBranchAsync()
    {
        var (companyId, branchId, _) = GetTenantContext();
        return await DbSet.AsNoTracking()
            .Where(t => t.CompanyId == companyId && t.BranchId == branchId && !t.IsDeleted)
            .OrderBy(t => t.TerminalCode)
            .ToListAsync();
    }
}

public class PosCashierRepository : TenantAwareRepository<PosCashier>, IPosCashierRepository
{
    public PosCashierRepository(SalesDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<List<PosCashier>> GetByBranchAsync(Guid storeId)
    {
        var list = await FindAsync(c => c.PosStoreId == storeId && c.IsActive && !c.IsDeleted);
        return list.OrderBy(c => c.DisplayName).ToList();
    }

    public async Task<PosCashier?> GetByPinAsync(string pin, Guid storeId)
    {
        var candidates = await FindAsync(c =>
            c.PosStoreId == storeId && c.IsActive && c.PinCode != null && !c.IsDeleted);
        return candidates.FirstOrDefault(c =>
            !string.IsNullOrEmpty(c.PinCode) && BCrypt.Net.BCrypt.Verify(pin, c.PinCode));
    }

    public async Task<PosCashier?> GetByEmployeeAsync(Guid employeeId, Guid storeId)
        => await FirstOrDefaultAsync(c => c.EmployeeId == employeeId &&
                                          c.PosStoreId == storeId && !c.IsDeleted);
}

public class PosSessionRepository : TenantAwareRepository<PosSession>, IPosSessionRepository
{
    public PosSessionRepository(SalesDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<PosSession?> GetOpenSessionAsync(Guid cashierId, Guid terminalId)
        => await FirstOrDefaultAsync(s => s.PosCashierId == cashierId &&
                                          s.PosTerminalId == terminalId &&
                                          s.Status == PosSessionStatus.Open &&
                                          !s.IsDeleted);

    public async Task<PosSession?> GetOpenSessionByTerminalAsync(Guid terminalId)
        => await FirstOrDefaultAsync(s => s.PosTerminalId == terminalId &&
                                          s.Status == PosSessionStatus.Open &&
                                          !s.IsDeleted);

    public async Task<PosSession?> GetOpenSessionByCashierAsync(Guid cashierId)
        => await FirstOrDefaultAsync(s => s.PosCashierId == cashierId &&
                                          s.Status == PosSessionStatus.Open &&
                                          !s.IsDeleted);

    public async Task<List<PosSession>> GetByTerminalAsync(Guid terminalId)
    {
        var list = await FindAsync(s => s.PosTerminalId == terminalId && !s.IsDeleted);
        return list.OrderByDescending(s => s.OpenedAt).ToList();
    }

    public async Task<List<PosSession>> GetByDateAsync(DateTime date, Guid storeId)
    {
        var (company, _, _) = GetTenantContext();
        return await DbSet
            .Include(s => s.PosTerminal)
            .Include(s => s.PosCashier)
            .Where(s => s.OpenedAt.Date == date.Date && s.CompanyId == company &&
                        s.PosTerminal.PosStoreId == storeId && !s.IsDeleted)
            .OrderByDescending(s => s.OpenedAt)
            .ToListAsync();
    }

    public async Task<List<PosSession>> GetOpenByBranchAsync(Guid storeId)
    {
        var (company, branch, bu) = GetTenantContext();
        return await DbSet
            .Include(s => s.PosTerminal)
            .Include(s => s.PosCashier)
            .Where(s => s.PosTerminal.PosStoreId == storeId &&
                        s.CompanyId == company && s.BranchId == branch && s.BusinessUnitId == bu &&
                        s.Status == PosSessionStatus.Open && !s.IsDeleted)
            .OrderByDescending(s => s.OpenedAt)
            .ToListAsync();
    }

    public async Task<List<PosSession>> GetByBranchAndDateAsync(Guid storeId, DateTime date)
    {
        var (company, branch, bu) = GetTenantContext();
        return await DbSet
            .Include(s => s.PosTerminal)
            .Include(s => s.PosCashier)
            .Where(s => s.PosTerminal.PosStoreId == storeId &&
                        s.CompanyId == company && s.BranchId == branch && s.BusinessUnitId == bu &&
                        s.OpenedAt.Date == date.Date && !s.IsDeleted)
            .OrderByDescending(s => s.OpenedAt)
            .ToListAsync();
    }

    public async Task<List<PosSession>> GetByBranchAndDateRangeAsync(Guid storeId, DateTime from, DateTime to)
    {
        var (company, branch, bu) = GetTenantContext();
        return await DbSet
            .Include(s => s.PosTerminal)
            .Where(s => s.PosTerminal.PosStoreId == storeId &&
                        s.CompanyId == company && s.BranchId == branch && s.BusinessUnitId == bu &&
                        s.OpenedAt >= from && s.OpenedAt <= to && !s.IsDeleted)
            .OrderByDescending(s => s.OpenedAt)
            .ToListAsync();
    }

    public async Task<PosSession?> GetByIdWithMovementsAsync(Guid id)
    {
        var (company, branch, bu) = GetTenantContext();
        return await DbSet
            .Include(s => s.CashMovements)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id && s.CompanyId == company &&
                                      s.BranchId == branch && s.BusinessUnitId == bu && !s.IsDeleted);
    }

    public async Task AddCashMovementAsync(PosCashMovement movement)
        => await Context.Set<PosCashMovement>().AddAsync(movement);

    public async Task<List<PosSession>> GetByStoreAndDateForCompanyAsync(Guid storeId, DateTime date)
    {
        var (company, _, _) = GetTenantContext();
        return await DbSet
            .Include(s => s.PosTerminal)
            .Include(s => s.PosCashier)
            .Where(s => s.PosTerminal.PosStoreId == storeId &&
                        s.CompanyId == company &&
                        s.OpenedAt.Date == date.Date && !s.IsDeleted)
            .OrderByDescending(s => s.OpenedAt)
            .AsNoTracking()
            .ToListAsync();
    }
}

public class PosTransactionRepository : TenantAwareRepository<PosTransaction>, IPosTransactionRepository
{
    public PosTransactionRepository(SalesDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<PosTransaction?> GetByNumberAsync(string transactionNumber)
        => await FirstOrDefaultAsync(t => t.TransactionNumber == transactionNumber);

    public async Task<PosTransaction?> GetWithLinesAsync(Guid id)
    {
        var (company, branch, bu) = GetTenantContext();
        return await DbSet
            .Include(t => t.Lines)
            .Include(t => t.Payments)
            .FirstOrDefaultAsync(t => t.Id == id && t.CompanyId == company &&
                                      t.BranchId == branch && t.BusinessUnitId == bu && !t.IsDeleted);
    }

    public async Task<List<PosTransaction>> GetBySessionAsync(Guid sessionId)
    {
        var list = await FindAsync(t => t.PosSessionId == sessionId && !t.IsDeleted);
        return list.OrderByDescending(t => t.TransactionDate).ToList();
    }

    public async Task<List<PosTransaction>> GetByDateRangeAsync(DateTime from, DateTime to, Guid branchId)
    {
        var (company, branch, bu) = GetTenantContext();
        return await DbSet
            .Where(t => t.TransactionDate >= from && t.TransactionDate <= to &&
                        t.CompanyId == company &&
                        t.BranchId == branch && t.BusinessUnitId == bu && !t.IsDeleted)
            .OrderByDescending(t => t.TransactionDate)
            .ToListAsync();
    }
}

// ?? POS Receipt Template ??????????????????????????????????????????????????????

public class PosReceiptTemplateRepository : TenantAwareRepository<PosReceiptTemplate>, IPosReceiptTemplateRepository
{
    public PosReceiptTemplateRepository(SalesDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    // Receipt/invoice templates are a company asset (stores reference one by id), so reads are
    // company-scoped — not hidden when the caller's token branch differs from the seeded branch.
    public async Task<List<PosReceiptTemplate>> GetByBranchAsync(Guid branchId)
    {
        var (company, _, _) = GetTenantContext();
        return await DbSet
            .Where(t => t.CompanyId == company && !t.IsDeleted)
            .OrderBy(t => t.TemplateName)
            .ToListAsync();
    }

    public async Task<PosReceiptTemplate?> GetDefaultAsync(Guid branchId)
    {
        var (company, _, _) = GetTenantContext();
        return await DbSet
            .Where(t => t.IsDefault && t.CompanyId == company && !t.IsDeleted)
            .FirstOrDefaultAsync();
    }
}

public class PosBarcodeLabelTemplateRepository : TenantAwareRepository<PosBarcodeLabelTemplate>, IPosBarcodeLabelTemplateRepository
{
    public PosBarcodeLabelTemplateRepository(SalesDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<List<PosBarcodeLabelTemplate>> GetByBranchAsync(Guid branchId)
    {
        var (company, _, _) = GetTenantContext();
        return await DbSet
            .Where(t => t.CompanyId == company && !t.IsDeleted)
            .OrderBy(t => t.TemplateName)
            .ToListAsync();
    }

    public async Task<PosBarcodeLabelTemplate?> GetDefaultAsync(Guid branchId)
    {
        var (company, _, _) = GetTenantContext();
        return await DbSet
            .Where(t => t.IsDefault && t.CompanyId == company && !t.IsDeleted)
            .FirstOrDefaultAsync();
    }
}

// ?? POS Cash Drawer ???????????????????????????????????????????????????????????

public class PosCashDrawerRepository : TenantAwareRepository<PosCashDrawer>, IPosCashDrawerRepository
{
    public PosCashDrawerRepository(SalesDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<List<PosCashDrawer>> GetByBranchAsync(Guid branchId)
    {
        var list = await FindAsync(d => d.BranchId == branchId && !d.IsDeleted);
        return list.OrderBy(d => d.DrawerCode).ToList();
    }

    public async Task<PosCashDrawer?> GetByCodeAsync(string code, Guid branchId)
        => await FirstOrDefaultAsync(d => d.DrawerCode == code && d.BranchId == branchId);

    public async Task<List<PosCashDrawer>> GetActiveByBranchAsync(Guid branchId)
    {
        var list = await FindAsync(d => d.BranchId == branchId && d.IsActive && !d.IsDeleted);
        return list.OrderBy(d => d.DrawerCode).ToList();
    }
}

// ?? Rider ?????????????????????????????????????????????????????????????????????

public class RiderRepository : TenantAwareRepository<Rider>, IRiderRepository
{
    public RiderRepository(SalesDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<Rider?> GetByCodeAsync(string code)
        => await FirstOrDefaultAsync(r => r.RiderCode == code);

    public async Task<List<Rider>> GetAvailableAsync(Guid? branchId = null)
    {
        var list = branchId.HasValue
            ? await FindAsync(r => r.Status == RiderStatus.Available &&
                                   r.IsActive && r.HomeBranchId == branchId && !r.IsDeleted)
            : await FindAsync(r => r.Status == RiderStatus.Available && r.IsActive && !r.IsDeleted);
        return list.OrderBy(r => r.RiderCode).ToList();
    }

    public async Task<List<Rider>> GetByBranchAsync(Guid branchId)
    {
        var list = await FindAsync(r => r.HomeBranchId == branchId && r.IsActive && !r.IsDeleted);
        return list.OrderBy(r => r.RiderCode).ToList();
    }
}

public class RiderAssignmentRepository : TenantAwareRepository<RiderAssignment>, IRiderAssignmentRepository
{
    public RiderAssignmentRepository(SalesDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<List<RiderAssignment>> GetByOrderAsync(Guid salesOrderId)
    {
        var list = await FindAsync(a => a.SalesOrderId == salesOrderId && !a.IsDeleted);
        return list.OrderByDescending(a => a.AssignedAt).ToList();
    }

    public async Task<List<RiderAssignment>> GetByRiderAsync(Guid riderId)
    {
        var list = await FindAsync(a => a.RiderId == riderId && !a.IsDeleted);
        return list.OrderByDescending(a => a.AssignedAt).ToList();
    }

    public async Task<RiderAssignment?> GetActiveAssignmentAsync(Guid salesOrderId)
        => await FirstOrDefaultAsync(a => a.SalesOrderId == salesOrderId &&
                                          a.Status != RiderAssignmentStatus.Delivered &&
                                          a.Status != RiderAssignmentStatus.Failed &&
                                          !a.IsReassigned && !a.IsDeleted);
}

// ?? Loyalty ???????????????????????????????????????????????????????????????????

public class LoyaltyAccountRepository : TenantAwareRepository<LoyaltyAccount>, ILoyaltyAccountRepository
{
    public LoyaltyAccountRepository(SalesDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<LoyaltyAccount?> GetByContactAsync(Guid contactId)
        => await FirstOrDefaultAsync(a => a.ContactId == contactId && !a.IsDeleted);
}

// ?? Price List ????????????????????????????????????????????????????????????????

public class PriceListRepository : TenantAwareRepository<PriceList>, IPriceListRepository
{
    public PriceListRepository(SalesDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<PriceList?> GetByCodeAsync(string code)
        => await FirstOrDefaultAsync(p => p.Code == code);

    public async Task<List<PriceList>> GetActiveAsync()
    {
        var now = DateTime.UtcNow;
        var list = await FindAsync(p => p.IsActive && !p.IsDeleted &&
                                        p.ValidFrom <= now &&
                                        (p.ValidTo == null || p.ValidTo >= now));
        return list.OrderBy(p => p.Name).ToList();
    }

    public async Task<PriceList?> GetWithItemsAsync(Guid id)
    {
        var (company, branch, bu) = GetTenantContext();
        return await DbSet
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == company &&
                                      p.BranchId == branch && p.BusinessUnitId == bu && !p.IsDeleted);
    }
}

// ?? Coupon ????????????????????????????????????????????????????????????????????

public class CouponRepository : TenantAwareRepository<Coupon>, ICouponRepository
{
    public CouponRepository(SalesDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<Coupon?> GetByCodeAsync(string code)
        => await FirstOrDefaultAsync(c => c.Code == code);

    public async Task<Coupon?> GetByCodePublicAsync(string code)
        => await DbSet.AsNoTracking().FirstOrDefaultAsync(c => c.Code == code && !c.IsDeleted);

    public async Task<List<Coupon>> GetActiveAsync()
    {
        var now = DateTime.UtcNow;
        var list = await FindAsync(c => c.Status == CouponStatus.Active && !c.IsDeleted &&
                                        c.ValidFrom <= now &&
                                        (c.ValidTo == null || c.ValidTo >= now));
        return list.OrderBy(c => c.Code).ToList();
    }

    public async Task<bool> IsCodeUsedByContactAsync(string code, Guid contactId)
    {
        var (company, branch, bu) = GetTenantContext();
        return await DbSet
            .Where(c => c.Code == code && c.CompanyId == company &&
                        c.BranchId == branch && c.BusinessUnitId == bu)
            .SelectMany(c => c.Usages)
            .AnyAsync(u => u.ContactId == contactId);
    }
}

// ?? Vendor Profile ????????????????????????????????????????????????????????????

public class StoreVendorProfileRepository : TenantAwareRepository<StoreVendorProfile>, IStoreVendorProfileRepository
{
    public StoreVendorProfileRepository(SalesDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<StoreVendorProfile?> GetByStoreAsync(Guid storeId)
        => await FirstOrDefaultAsync(p => p.StoreId == storeId && !p.IsDeleted);

    public async Task<List<StoreVendorProfile>> GetByStatusAsync(VendorOnboardingStatus status)
    {
        var list = await FindAsync(p => p.OnboardingStatus == status && !p.IsDeleted);
        return list.OrderByDescending(p => p.SubmittedAt).ToList();
    }
}

// ?? Store Offer ???????????????????????????????????????????????????????????????

public class StoreOfferRepository : TenantAwareRepository<StoreOffer>, IStoreOfferRepository
{
    public StoreOfferRepository(SalesDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<List<StoreOffer>> GetByStoreAsync(Guid storeId)
    {
        var list = await FindAsync(o => o.StoreId == storeId && !o.IsDeleted);
        return list.OrderBy(o => o.DisplayOrder).ToList();
    }

    public async Task<List<StoreOffer>> GetActiveByStoreAsync(Guid storeId, DateOnly date, TimeOnly time)
    {
        var list = await FindAsync(o =>
            o.StoreId == storeId && o.IsActive && !o.IsDeleted &&
            (o.StartDate == null || o.StartDate <= date) &&
            (o.EndDate == null || o.EndDate >= date) &&
            (o.StartTime == null || o.StartTime <= time) &&
            (o.EndTime == null || o.EndTime >= time));
        return list.OrderBy(o => o.DisplayOrder).ToList();
    }

    public async Task<List<StoreOffer>> GetPublicActiveByStoreAsync(Guid storeId, DateOnly date, TimeOnly time)
    {
        // Intentionally bypasses tenant JWT filter — storeId is the scope
        return await DbSet
            .Where(o =>
                o.StoreId == storeId && o.IsActive && !o.IsDeleted &&
                (o.StartDate == null || o.StartDate <= date) &&
                (o.EndDate == null || o.EndDate >= date) &&
                (o.StartTime == null || o.StartTime <= time) &&
                (o.EndTime == null || o.EndTime >= time))
            .Include(o => o.Promotion)
            .OrderBy(o => o.DisplayOrder)
            .AsNoTracking()
            .ToListAsync();
    }
}

// ?? Store Menu ????????????????????????????????????????????????????????????????

public class StoreMenuRepository : TenantAwareRepository<StoreMenu>, IStoreMenuRepository
{
    public StoreMenuRepository(SalesDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<List<StoreMenu>> GetByBranchAsync(Guid branchId)
    {
        var list = await FindAsync(m => m.BranchId == branchId && m.IsActive && !m.IsDeleted);
        return list.OrderBy(m => m.DisplayOrder).ToList();
    }

    public async Task<StoreMenu?> GetActiveMenuAsync(Guid branchId)
    {
        var now = TimeOnly.FromDateTime(DateTime.UtcNow);
        var (company, branch, bu) = GetTenantContext();
        return await DbSet
            .Where(m => m.BranchId == branchId && m.IsActive && !m.IsDeleted &&
                        m.CompanyId == company && m.BranchId == branch && m.BusinessUnitId == bu &&
                        (m.AvailableFrom == null || m.AvailableFrom <= now) &&
                        (m.AvailableTo == null || m.AvailableTo >= now))
            .Include(m => m.Sections.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder))
            .OrderBy(m => m.DisplayOrder)
            .FirstOrDefaultAsync();
    }

    public async Task<StoreMenu?> GetWithSectionsAsync(Guid id)
    {
        var (company, branch, bu) = GetTenantContext();
        return await DbSet
            .Include(m => m.Sections.OrderBy(s => s.DisplayOrder))
            .FirstOrDefaultAsync(m => m.Id == id && m.CompanyId == company &&
                                      m.BranchId == branch && m.BusinessUnitId == bu && !m.IsDeleted);
    }
}

// ?? Tax Engine ????????????????????????????????????????????????????????????????
// TaxDefinition (rate master) lives in Inventory.
// Sales owns TaxGroup (bundles rates), TaxGroupRate (cross-module Guid to TaxDefinition),
// and TaxRule (determines which TaxGroup to apply per order line).

public class TaxGroupRepository : TenantAwareRepository<TaxGroup>, ITaxGroupRepository
{
    public TaxGroupRepository(SalesDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<TaxGroup?> GetByCodeAsync(string code)
        => await FirstOrDefaultAsync(g => g.Code == code && g.IsActive);

    public async Task<TaxGroup?> GetWithRatesAsync(Guid id)
    {
        var (company, branch, bu) = GetTenantContext();
        return await DbSet
            .Include(g => g.Rates)   // TaxGroupRate rows — TaxDefinition resolved via Inventory module
            .FirstOrDefaultAsync(g => g.Id == id && g.CompanyId == company &&
                                      g.BranchId == branch && g.BusinessUnitId == bu && !g.IsDeleted);
    }
}

public class TaxRuleRepository : TenantAwareRepository<TaxRule>, ITaxRuleRepository
{
    public TaxRuleRepository(SalesDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<List<TaxRule>> GetActiveRulesAsync()
    {
        var now = DateTime.UtcNow;
        var (company, branch, bu) = GetTenantContext();
        return await DbSet
            .Where(r => r.IsActive && !r.IsDeleted &&
                        r.CompanyId == company && r.BranchId == branch && r.BusinessUnitId == bu &&
                        r.ValidFrom <= now && (r.ValidTo == null || r.ValidTo >= now))
            .Include(r => r.TaxGroup).ThenInclude(g => g.Rates)  // no further ThenInclude — TaxDefinition is cross-module
            .OrderBy(r => r.Priority)
            .ToListAsync();
    }

    public async Task<TaxGroup?> ResolveAsync(TaxCategory productCategory, string? customerCountryCode,
        string? customerType, SalesChannel? channel)
    {
        var rules = await GetActiveRulesAsync();

        // First-match wins — rules already ordered by Priority asc
        var match = rules.FirstOrDefault(r =>
            (r.ProductTaxCategory == null || r.ProductTaxCategory == productCategory) &&
            (r.CustomerCountryCode == null || r.CustomerCountryCode == customerCountryCode) &&
            (r.CustomerType == null || r.CustomerType == customerType) &&
            (r.SalesChannel == null || r.SalesChannel == channel));

        return match?.TaxGroup;
    }
}

// ?? Approval Engine ???????????????????????????????????????????????????????????

public class ApprovalPolicyRepository : TenantAwareRepository<ApprovalPolicy>, IApprovalPolicyRepository
{
    public ApprovalPolicyRepository(SalesDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<List<ApprovalPolicy>> GetActivePoliciesAsync()
    {
        var (company, branch, bu) = GetTenantContext();
        return await DbSet
            .Where(p => p.IsActive && !p.IsDeleted &&
                        p.CompanyId == company && p.BranchId == branch && p.BusinessUnitId == bu)
            .Include(p => p.Conditions)
            .Include(p => p.Steps.OrderBy(s => s.StepOrder))
            .OrderBy(p => p.Priority)
            .ToListAsync();
    }

    public async Task<ApprovalPolicy?> GetWithStepsAsync(Guid id)
    {
        var (company, branch, bu) = GetTenantContext();
        return await DbSet
            .Include(p => p.Conditions)
            .Include(p => p.Steps.OrderBy(s => s.StepOrder))
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == company &&
                                      p.BranchId == branch && p.BusinessUnitId == bu && !p.IsDeleted);
    }
}

public class ApprovalRequestRepository : TenantAwareRepository<ApprovalRequest>, IApprovalRequestRepository
{
    public ApprovalRequestRepository(SalesDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<List<ApprovalRequest>> GetByOrderAsync(Guid salesOrderId)
    {
        var list = await FindAsync(r => r.SalesOrderId == salesOrderId && !r.IsDeleted);
        return list.OrderBy(r => r.StepOrder).ToList();
    }

    public async Task<List<ApprovalRequest>> GetPendingByUserAsync(Guid userId)
    {
        var list = await FindAsync(r => r.AssignedUserId == userId &&
                                        r.Decision == ApprovalDecision.Pending && !r.IsDeleted);
        return list.OrderBy(r => r.RequestedAt).ToList();
    }

    public async Task<List<ApprovalRequest>> GetPendingByRoleAsync(string role)
    {
        var list = await FindAsync(r => r.AssignedRole == role &&
                                        r.Decision == ApprovalDecision.Pending && !r.IsDeleted);
        return list.OrderBy(r => r.RequestedAt).ToList();
    }

    public async Task<ApprovalRequest?> GetPendingStepAsync(Guid salesOrderId, int stepOrder)
        => await FirstOrDefaultAsync(r => r.SalesOrderId == salesOrderId &&
                                          r.StepOrder == stepOrder &&
                                          r.Decision == ApprovalDecision.Pending &&
                                          !r.IsDeleted);
}

// ?? Commission ????????????????????????????????????????????????????????????????

public class CommissionRuleRepository : TenantAwareRepository<CommissionRule>, ICommissionRuleRepository
{
    public CommissionRuleRepository(SalesDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<List<CommissionRule>> GetActiveRulesAsync()
    {
        var now = DateTime.UtcNow;
        var list = await FindAsync(r => r.IsActive && !r.IsDeleted &&
                                        r.ValidFrom <= now &&
                                        (r.ValidTo == null || r.ValidTo >= now));
        return list.OrderBy(r => r.Priority).ToList();
    }
}

public class CommissionEntryRepository : TenantAwareRepository<CommissionEntry>, ICommissionEntryRepository
{
    public CommissionEntryRepository(SalesDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<List<CommissionEntry>> GetByOrderAsync(Guid salesOrderId)
    {
        var list = await FindAsync(e => e.SalesOrderId == salesOrderId && !e.IsDeleted);
        return list.OrderBy(e => e.EarnedDate).ToList();
    }

    public async Task<List<CommissionEntry>> GetByRepAsync(Guid salesRepId, DateTime? from = null, DateTime? to = null)
    {
        var list = await FindAsync(e => e.SalesRepId == salesRepId && !e.IsDeleted &&
                                        (from == null || e.EarnedDate >= from) &&
                                        (to == null || e.EarnedDate <= to));
        return list.OrderByDescending(e => e.EarnedDate).ToList();
    }

    public async Task<List<CommissionEntry>> GetPendingByRepAsync(Guid salesRepId)
    {
        var list = await FindAsync(e => e.SalesRepId == salesRepId &&
                                        e.Status == CommissionStatus.Pending && !e.IsDeleted);
        return list.OrderBy(e => e.EarnedDate).ToList();
    }

    public async Task<decimal> GetTotalEarnedAsync(Guid salesRepId, DateTime from, DateTime to)
    {
        var (company, branch, bu) = GetTenantContext();
        return await DbSet
            .Where(e => e.SalesRepId == salesRepId &&
                        e.EarnedDate >= from && e.EarnedDate <= to &&
                        e.Status != CommissionStatus.Reversed &&
                        e.CompanyId == company && e.BranchId == branch &&
                        e.BusinessUnitId == bu && !e.IsDeleted)
            .SumAsync(e => e.CommissionAmount);
    }
}

public class SalesTargetRepository : TenantAwareRepository<SalesTarget>, ISalesTargetRepository
{
    public SalesTargetRepository(SalesDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<SalesTarget?> GetCurrentTargetAsync(Guid salesRepId, DateTime date)
        => await FirstOrDefaultAsync(t => t.SalesRepId == salesRepId &&
                                          t.PeriodStart <= date && t.PeriodEnd >= date &&
                                          !t.IsDeleted);

    public async Task<List<SalesTarget>> GetByRepAsync(Guid salesRepId)
    {
        var list = await FindAsync(t => t.SalesRepId == salesRepId && !t.IsDeleted);
        return list.OrderByDescending(t => t.PeriodStart).ToList();
    }

    public async Task<List<SalesTarget>> GetByTerritoryAsync(Guid territoryId)
    {
        var list = await FindAsync(t => t.SalesTerritoryId == territoryId && !t.IsDeleted);
        return list.OrderByDescending(t => t.PeriodStart).ToList();
    }
}

// ?? Promotion ?????????????????????????????????????????????????????????????????

public class PromotionRepository : TenantAwareRepository<Promotion>, IPromotionRepository
{
    public PromotionRepository(SalesDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<Promotion?> GetByCodeAsync(string promotionCode)
        => await FirstOrDefaultAsync(p => p.PromotionCode == promotionCode && !p.IsDeleted);

    public async Task<HashSet<string>> GetUsedCodesAsync()
    {
        var (company, branch, _) = GetTenantContext();
        // No !IsDeleted filter — the unique index covers soft-deleted rows too.
        var codes = await DbSet
            .Where(p => p.CompanyId == company && p.BranchId == branch && p.PromotionCode != null)
            .Select(p => p.PromotionCode!)
            .ToListAsync();
        return codes.Select(c => c.ToUpperInvariant()).ToHashSet();
    }

    public async Task<Promotion?> GetWithItemsAsync(Guid id)
    {
        var (company, branch, bu) = GetTenantContext();
        return await DbSet
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == id && p.CompanyId == company &&
                                      p.BranchId == branch && p.BusinessUnitId == bu && !p.IsDeleted);
    }

    public async Task<List<Promotion>> GetActiveAsync(DateOnly date, TimeOnly time)
    {
        var (company, branch, bu) = GetTenantContext();
        var dayFlag = (int)(1 << ((int)date.DayOfWeek == 0 ? 6 : (int)date.DayOfWeek - 1));
        return await DbSet
            .Where(p => p.Status == PromotionStatus.Active && !p.IsDeleted &&
                        p.CompanyId == company && p.BranchId == branch && p.BusinessUnitId == bu &&
                        p.StartDate <= date && p.EndDate >= date &&
                        (p.ScheduledDays & (ScheduledDays)dayFlag) != 0 &&
                        (p.StartTime == null || p.StartTime <= time) &&
                        (p.EndTime == null || p.EndTime >= time))
            .Include(p => p.Items)
            .OrderByDescending(p => p.Priority)
            .ToListAsync();
    }

    public async Task<List<Promotion>> GetActiveForItemAsync(Guid itemId, DateOnly date, TimeOnly time)
    {
        var active = await GetActiveAsync(date, time);
        return active
            .Where(p => p.IsAutoApplied &&
                        p.Items.Any(i => (i.ItemId == itemId || i.ItemId == null) && !i.IsDeleted))
            .ToList();
    }
}

public class PromotionItemRepository : TenantAwareRepository<PromotionItem>, IPromotionItemRepository
{
    public PromotionItemRepository(SalesDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<List<PromotionItem>> GetByPromotionAsync(Guid promotionId)
    {
        var list = await FindAsync(i => i.PromotionId == promotionId && !i.IsDeleted);
        return list.ToList();
    }
}

// ?? POS Store ?????????????????????????????????????????????????????????????????

public class PosStoreRepository : TenantAwareRepository<PosStore>, IPosStoreRepository
{
    public PosStoreRepository(SalesDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<PosStore?> GetByCodeAsync(string storeCode)
        => await FirstOrDefaultAsync(s => s.Code == storeCode && !s.IsDeleted);

    public async Task<List<PosStore>> GetActiveByBranchAsync(Guid branchId)
    {
        var list = await FindAsync(s => s.BranchId == branchId && s.IsActive && !s.IsDeleted);
        return list.OrderBy(s => s.CodeInt).ToList();
    }

    public async Task<List<PosStore>> GetOnlineEnabledAsync()
    {
        var list = await FindAsync(s => s.IsOnlineOrderingEnabled && s.IsActive && !s.IsDeleted);
        return list.OrderBy(s => s.CodeInt).ToList();
    }

    // Bypasses TenantAwareRepository so [AllowAnonymous] endpoints can call it without a JWT.
    public async Task<List<PosStore>> GetAllActiveWithLocationAsync()
        => await DbSet.AsNoTracking()
            .Where(s => s.IsActive && s.Location != null && !s.IsDeleted)
            .ToListAsync();

    // Direct MAX query — bypasses FindAsync in-memory aggregation for atomic code generation.
    public async Task<int> GetMaxCodeIntAsync()
        => await DbSet.Where(s => !s.IsDeleted).MaxAsync(s => (int?)s.CodeInt) ?? 0;

    public async Task<bool> TradingNameExistsAsync(string tradingName, Guid? excludeStoreId = null)
    {
        var (company, _, _) = GetTenantContext();
        var name = (tradingName ?? string.Empty).Trim().ToLower();
        return await DbSet.AnyAsync(s =>
            s.CompanyId == company && !s.IsDeleted &&
            s.TradingName != null && s.TradingName.Trim().ToLower() == name &&
            (excludeStoreId == null || s.Id != excludeStoreId));
    }

    public async Task<List<PosStore>> GetAllByCompanyAsync()
    {
        var (companyId, _, _) = GetTenantContext();
        return await DbSet.AsNoTracking()
            .Where(s => s.CompanyId == companyId && !s.IsDeleted)
            .OrderBy(s => s.CodeInt)
            .ToListAsync();
    }

    public async Task<List<PosStore>> GetAllByBranchAsync()
    {
        var (companyId, branchId, _) = GetTenantContext();
        return await DbSet.AsNoTracking()
            .Where(s => s.CompanyId == companyId && s.BranchId == branchId && !s.IsDeleted)
            .OrderBy(s => s.CodeInt)
            .ToListAsync();
    }
}

// ?? POS Settings ???????????????????????????????????????????????????????????????

public class PosSettingsRepository : TenantAwareRepository<PosSettings>, IPosSettingsRepository
{
    public PosSettingsRepository(SalesDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<PosSettings?> GetCurrentAsync()
        => await FirstOrDefaultAsync(s => !s.IsDeleted);
}

// ?? Document Sequence ?????????????????????????????????????????????????????????????

public class DocumentSequenceRepository : TenantAwareRepository<DocumentSequence>, IDocumentSequenceRepository
{
    public DocumentSequenceRepository(SalesDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<DocumentSequence?> GetByDocumentTypeAsync(DocumentType documentType)
        => await FirstOrDefaultAsync(s => s.DocumentType == documentType && !s.IsDeleted);

    public async Task<List<DocumentSequence>> GetAllActiveAsync()
    {
        var list = await FindAsync(s => s.IsActive && !s.IsDeleted);
        return list.OrderBy(s => s.DocumentType).ToList();
    }
}

// ?? Currency ??????????????????????????????????????????????????????????????????

public class CurrencyRepository : TenantAwareRepository<Currency>, ICurrencyRepository
{
    public CurrencyRepository(SalesDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<Currency?> GetByCodeAsync(string code)
        => await FirstOrDefaultAsync(c => c.Code == code && !c.IsDeleted);

    public async Task<Currency?> GetBaseCurrencyAsync()
        => await FirstOrDefaultAsync(c => c.IsBaseCurrency && c.IsActive && !c.IsDeleted);

    public async Task<List<Currency>> GetActiveAsync()
    {
        var list = await FindAsync(c => c.IsActive && !c.IsDeleted);
        return list.OrderBy(c => c.Code).ToList();
    }

    public async Task<Currency?> GetWithRatesAsync(Guid id)
    {
        var (company, branch, bu) = GetTenantContext();
        return await DbSet
            .Include(c => c.Rates.Where(r => !r.IsDeleted).OrderByDescending(r => r.EffectiveDate))
            .FirstOrDefaultAsync(c => c.Id == id && c.CompanyId == company &&
                                      c.BranchId == branch && c.BusinessUnitId == bu && !c.IsDeleted);
    }
}

public class CurrencyRateRepository : TenantAwareRepository<CurrencyRate>, ICurrencyRateRepository
{
    public CurrencyRateRepository(SalesDbContext ctx, IHttpContextAccessor http) : base(ctx, http) { }

    public async Task<CurrencyRate?> GetRateAsync(
        string currencyCode,
        string baseCurrencyCode,
        ExchangeRateType rateType,
        DateOnly asOfDate,
        string? rateName = null)
    {
        var (company, branch, bu) = GetTenantContext();

        var baseQuery = DbSet
            .Where(r => r.CurrencyCode     == currencyCode     &&
                        r.BaseCurrencyCode == baseCurrencyCode &&
                        r.RateType         == rateType         &&
                        r.EffectiveDate    <= asOfDate         &&
                        (r.ValidUntil == null || r.ValidUntil >= asOfDate) &&
                        r.CompanyId == company && r.BranchId == branch &&
                        r.BusinessUnitId == bu && !r.IsDeleted);

        // When a specific name is requested, filter to that name only
        if (!string.IsNullOrWhiteSpace(rateName))
        {
            return await baseQuery
                .Where(r => r.RateName == rateName)
                .OrderByDescending(r => r.EffectiveDate)
                .FirstOrDefaultAsync();
        }

        // No name requested: prefer unnamed (default) rates; fall back to any named rate
        var unnamed = await baseQuery
            .Where(r => r.RateName == null)
            .OrderByDescending(r => r.EffectiveDate)
            .FirstOrDefaultAsync();

        return unnamed ?? await baseQuery
            .OrderByDescending(r => r.EffectiveDate)
            .FirstOrDefaultAsync();
    }

    public async Task<List<CurrencyRate>> GetByCurrencyAsync(Guid currencyId)
    {
        var list = await FindAsync(r => r.CurrencyId == currencyId && !r.IsDeleted);
        return list.OrderByDescending(r => r.EffectiveDate).ThenBy(r => r.RateType).ToList();
    }

    public async Task<List<CurrencyRate>> GetHistoryAsync(
        string currencyCode,
        string baseCurrencyCode,
        DateOnly fromDate,
        DateOnly toDate)
    {
        var (company, branch, bu) = GetTenantContext();
        return await DbSet
            .Where(r => r.CurrencyCode     == currencyCode     &&
                        r.BaseCurrencyCode == baseCurrencyCode &&
                        r.EffectiveDate    >= fromDate         &&
                        r.EffectiveDate    <= toDate           &&
                        r.CompanyId == company && r.BranchId == branch &&
                        r.BusinessUnitId == bu && !r.IsDeleted)
            .OrderByDescending(r => r.EffectiveDate)
            .ThenBy(r => r.RateType)
            .ToListAsync();
    }

    public async Task<List<CurrencyRate>> GetLatestAllAsync(DateOnly asOfDate)
    {
        var (company, branch, bu) = GetTenantContext();

        // For each (CurrencyCode, RateType) pair, get the most recent rate <= asOfDate
        var all = await DbSet
            .Where(r => r.EffectiveDate <= asOfDate &&
                        (r.ValidUntil == null || r.ValidUntil >= asOfDate) &&
                        r.CompanyId == company && r.BranchId == branch &&
                        r.BusinessUnitId == bu && !r.IsDeleted)
            .OrderByDescending(r => r.EffectiveDate)
            .ToListAsync();

        // Deduplicate: keep only the freshest row per (CurrencyCode, RateType, RateName)
        // This returns ALL named variants so the caller sees every available rate
        return all
            .GroupBy(r => (r.CurrencyCode, r.RateType, r.RateName))
            .Select(g => g.First())
            .OrderBy(r => r.CurrencyCode)
            .ThenBy(r => r.RateType)
            .ThenBy(r => r.RateName)
            .ToList();
    }
}
