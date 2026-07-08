using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nexcore.Tests.Infrastructure.Auth;
using Sales.Domain.Entities;
using Sales.Infrastructure.Persistence;

namespace Sales.Tests.Integration.Fixtures;

/// <summary>
/// Inserts and removes Sales test data directly via the DbContext (bypasses HTTP layer).
/// All entities must have CompanyId/BranchId/BusinessUnitId set to the test-JWT defaults
/// so the TenantAwareRepository filters can find them when the HTTP endpoint reads back.
/// </summary>
public class SalesTestDataBuilder
{
    private readonly SalesCollectionFixture _fixture;

    // Thread-safe counters — produce deterministic, unique document numbers per test run.
    private static int _orderSeq   = 0;
    private static int _invoiceSeq = 0;
    private static int _paymentSeq = 0;
    private static int _deliverySeq = 0;
    private static int _sessionSeq  = 0;

    public SalesTestDataBuilder(SalesCollectionFixture fixture)
    {
        _fixture = fixture;
    }

    // ── Document number helpers ───────────────────────────────────────────────

    public static string NextOrderNumber()   => $"SO-TEST-{Interlocked.Increment(ref _orderSeq):D5}";
    public static string NextInvoiceNumber() => $"INV-TEST-{Interlocked.Increment(ref _invoiceSeq):D5}";
    public static string NextPaymentNumber() => $"PAY-TEST-{Interlocked.Increment(ref _paymentSeq):D5}";
    public static string NextDeliveryNumber()=> $"DLV-TEST-{Interlocked.Increment(ref _deliverySeq):D5}";
    public static string NextSessionNumber() => $"POSS-TEST-{Interlocked.Increment(ref _sessionSeq):D5}";

    // ── PosStore ─────────────────────────────────────────────────────────────

    public async Task<PosStore> CreatePosStoreAsync(
        string? tradingName = null,
        PosStoreType storeType = PosStoreType.Retail,
        bool isActive = true)
    {
        tradingName ??= $"Test Store {Guid.NewGuid():N}"[..30];

        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();

        var store = new PosStore
        {
            Code = $"TST-{Guid.NewGuid():N}"[..12],
            CodeInt = Random.Shared.Next(1, 99999),
            TradingName = tradingName,
            StoreType = storeType,
            StoreFormat = PosStoreFormat.Physical,
            CountryCode = "PK",
            IsActive = isActive,
            OnlineStatus = StoreOnlineStatus.Closed,
            CompanyId = TestJwtSettings.CompanyId,
            BranchId = TestJwtSettings.BranchId,
            BusinessUnitId = TestJwtSettings.BusinessUnitId,
        };

        db.PosStores.Add(store);
        await db.SaveChangesAsync();
        return store;
    }

    public async Task DeletePosStoreAsync(Guid id)
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();

        // StoreOffer.StoreId is Restrict — delete offers before the store
        var offers = await db.StoreOffers.Where(o => o.StoreId == id).ToListAsync();
        db.StoreOffers.RemoveRange(offers);

        var store = await db.PosStores.FindAsync(id);
        if (store is not null)
            db.PosStores.Remove(store);

        await db.SaveChangesAsync();
    }

    // ── PosTerminal ──────────────────────────────────────────────────────────

    public async Task<PosTerminal> CreatePosTerminalAsync(
        Guid storeId,
        string? terminalCode = null,
        string? terminalName = null)
    {
        terminalCode ??= $"T-{Guid.NewGuid():N}"[..10];
        terminalName ??= $"Terminal {terminalCode}";

        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();

        var terminal = new PosTerminal
        {
            TerminalCode = terminalCode,
            TerminalName = terminalName,
            BranchId = TestJwtSettings.BranchId,
            IsActive = true,
            IsOnline = false,
            CompanyId = TestJwtSettings.CompanyId,
            BusinessUnitId = TestJwtSettings.BusinessUnitId,
        };

        db.PosTerminals.Add(terminal);
        await db.SaveChangesAsync();
        return terminal;
    }

    public async Task DeletePosTerminalAsync(Guid id)
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();

        // Sessions are Restrict FK — delete them before the terminal
        var sessions = await db.PosSessions.Where(s => s.PosTerminalId == id).ToListAsync();
        db.PosSessions.RemoveRange(sessions);

        var terminal = await db.PosTerminals.FindAsync(id);
        if (terminal is not null)
            db.PosTerminals.Remove(terminal);

        await db.SaveChangesAsync();
    }

    // ── PosCashier ───────────────────────────────────────────────────────────

    public async Task<PosCashier> CreatePosCashierAsync(
        string? displayName = null,
        Guid? employeeId = null,
        Guid? branchId = null)
    {
        displayName ??= $"Cashier {Guid.NewGuid():N}"[..20];

        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();

        var cashier = new PosCashier
        {
            EmployeeId = employeeId ?? Guid.NewGuid(),
            DisplayName = displayName,
            PosStoreId = _fixture.SharedStore.Id,
            BranchId = branchId ?? TestJwtSettings.BranchId,
            IsActive = true,
            CompanyId = TestJwtSettings.CompanyId,
            BusinessUnitId = TestJwtSettings.BusinessUnitId,
        };

        db.PosCashiers.Add(cashier);
        await db.SaveChangesAsync();
        return cashier;
    }

    public async Task DeletePosCashierAsync(Guid id)
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();

        // Sessions are Restrict FK — delete them before the cashier
        var sessions = await db.PosSessions.Where(s => s.PosCashierId == id).ToListAsync();
        db.PosSessions.RemoveRange(sessions);

        var cashier = await db.PosCashiers.FindAsync(id);
        if (cashier is not null)
            db.PosCashiers.Remove(cashier);

        await db.SaveChangesAsync();
    }

    // ── SalesOrderLine ───────────────────────────────────────────────────────

    public async Task<SalesOrderLine> CreateSalesOrderLineAsync(Guid orderId, Guid? productId = null)
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();

        var line = new SalesOrderLine
        {
            SalesOrderId = orderId,
            LineNumber = 1,
            ProductId = productId ?? Guid.NewGuid(),
            ProductCode = "TEST-SKU-001",
            ProductName = "Test Product",
            OrderedQuantity = 1,
            UnitOfMeasure = "PCS",
            UnitPrice = 100m,
            LineAmount = 100m,
            TotalAmount = 100m,
            CompanyId = TestJwtSettings.CompanyId,
            BranchId = TestJwtSettings.BranchId,
            BusinessUnitId = TestJwtSettings.BusinessUnitId,
        };

        db.SalesOrderLines.Add(line);
        await db.SaveChangesAsync();
        return line;
    }

    // ── SalesOrder ───────────────────────────────────────────────────────────

    public async Task<SalesOrder> CreateSalesOrderAsync(
        SalesOrderStatus status = SalesOrderStatus.Draft,
        SalesChannel channel = SalesChannel.DirectSales)
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();

        var order = new SalesOrder
        {
            OrderNumber     = NextOrderNumber(),
            SalesChannel    = channel,
            FulfillmentType = FulfillmentType.Immediate,
            Status          = status,
            CurrencyCode    = "USD",
            ExchangeRate    = 1m,
            SubtotalAmount  = 100m,
            TotalAmount     = 100m,
            BalanceDue      = 100m,
            CompanyId       = TestJwtSettings.CompanyId,
            BranchId        = TestJwtSettings.BranchId,
            BusinessUnitId  = TestJwtSettings.BusinessUnitId,
        };

        db.SalesOrders.Add(order);
        await db.SaveChangesAsync();
        return order;
    }

    public async Task DeleteSalesOrderAsync(Guid id)
    {
        using var scope = _fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SalesDbContext>();

        // Delete FK-Restrict/NoAction children first so the parent order can be removed
        var payments = await db.SalesPayments.Where(p => p.SalesOrderId == id).ToListAsync();
        if (payments.Count > 0)
        {
            var paymentIds = payments.Select(p => p.Id).ToList();
            var allocations = await db.PaymentAllocations.Where(a => paymentIds.Contains(a.SalesPaymentId)).ToListAsync();
            db.PaymentAllocations.RemoveRange(allocations);
        }
        db.SalesPayments.RemoveRange(payments);
        await db.SaveChangesAsync();

        var invoices = await db.SalesInvoices.Where(i => i.SalesOrderId == id).ToListAsync();
        db.SalesInvoices.RemoveRange(invoices);

        var deliveries = await db.Deliveries.Where(d => d.SalesOrderId == id).ToListAsync();
        db.Deliveries.RemoveRange(deliveries);

        var assignments = await db.RiderAssignments.Where(a => a.SalesOrderId == id).ToListAsync();
        db.RiderAssignments.RemoveRange(assignments);

        var order = await db.SalesOrders.FindAsync(id);
        if (order is not null)
            db.SalesOrders.Remove(order);

        await db.SaveChangesAsync();
    }
}
