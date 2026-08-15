using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Helpers;
using Sales.Application.DTOs;
using Sales.Domain.Entities;
using Sales.Domain.Enums;
using Sales.Infrastructure.Persistence;
using Sales.Infrastructure.Repositories.Interfaces;

namespace Sales.Infrastructure.Repositories.Implementations;

/// <summary>
/// Read-only aggregation for POS reporting.
///
/// This does not derive from <see cref="Nexcore.SharedKernel.Repository.TenantAwareRepository{T}"/>
/// because a report spans several tables at once — transactions, their lines, their payments and
/// the session's cash movements — and that base class is scoped to a single aggregate. The tenant
/// predicate is therefore applied explicitly here, on every query, with no exceptions.
///
/// Grouping happens in SQL. A busy store writes thousands of lines a day, so pulling rows into
/// memory to sum them would turn a shift report into a slow, memory-hungry query as the shop grows.
/// </summary>
public class PosReportRepository : IPosReportRepository
{
    private readonly SalesDbContext _ctx;
    private readonly IHttpContextAccessor _http;

    public PosReportRepository(SalesDbContext ctx, IHttpContextAccessor http)
    {
        _ctx = ctx;
        _http = http;
    }

    private (Guid CompanyId, Guid BranchId, Guid BusinessUnitId) Tenant()
    {
        var user = _http.HttpContext?.User
            ?? throw new InvalidOperationException("HTTP context or user not available");
        var (companyId, branchId, businessUnitId) = TenantContextHelper.ExtractTenantContext(user);
        if (businessUnitId is null)
            throw new InvalidOperationException("BusinessUnitId claim not found or invalid");
        return (companyId, branchId, businessUnitId.Value);
    }

    /// <summary>
    /// Completed transactions only, scoped to the tenant.
    ///
    /// Held, in-progress and voided transactions are excluded deliberately: a parked basket has
    /// not been paid for, and a void is the cancellation of a sale rather than a sale of its own.
    /// Counting either would overstate the day.
    /// </summary>
    private IQueryable<PosTransaction> CompletedTransactions()
    {
        var (companyId, branchId, businessUnitId) = Tenant();
        return _ctx.PosTransactions.AsNoTracking()
            .Where(t => t.CompanyId == companyId
                     && t.BranchId == branchId
                     && t.BusinessUnitId == businessUnitId
                     && !t.IsDeleted
                     && t.Status == PosTransactionStatus.Completed);
    }

    private static IQueryable<PosTransaction> InRange(
        IQueryable<PosTransaction> q, DateTime from, DateTime to, Guid? storeId) =>
        q.Where(t => t.TransactionDate >= from && t.TransactionDate <= to)
         .Where(t => storeId == null || t.PosStoreId == storeId);

    // ── Shift reads ───────────────────────────────────────────────────────────

    public async Task<PosSession?> GetSessionAsync(Guid sessionId)
    {
        var (companyId, branchId, businessUnitId) = Tenant();
        return await _ctx.PosSessions.AsNoTracking()
            .Include(s => s.PosCashier)
            .Include(s => s.PosTerminal)
            .FirstOrDefaultAsync(s => s.Id == sessionId
                                   && s.CompanyId == companyId
                                   && s.BranchId == branchId
                                   && s.BusinessUnitId == businessUnitId
                                   && !s.IsDeleted);
    }

    public async Task<PosTradingTotals> GetSessionTotalsAsync(Guid sessionId)
    {
        var q = CompletedTransactions().Where(t => t.PosSessionId == sessionId);
        return await AggregateAsync(q);
    }

    public async Task<List<PosTenderTotalDto>> GetSessionTendersAsync(Guid sessionId)
    {
        var sessionTxns = CompletedTransactions().Where(t => t.PosSessionId == sessionId).Select(t => t.Id);
        return await TendersForAsync(sessionTxns);
    }

    public async Task<decimal> GetSessionItemCountAsync(Guid sessionId)
    {
        var ids = CompletedTransactions().Where(t => t.PosSessionId == sessionId).Select(t => t.Id);
        return await _ctx.PosTransactionLines.AsNoTracking()
            .Where(l => !l.IsDeleted && ids.Contains(l.PosTransactionId))
            .SumAsync(l => (decimal?)l.Quantity) ?? 0m;
    }

    public async Task<List<PosCashMovement>> GetCashMovementsAsync(Guid sessionId)
    {
        var (companyId, branchId, businessUnitId) = Tenant();
        return await _ctx.PosCashMovements.AsNoTracking()
            .Where(m => m.PosSessionId == sessionId
                     && m.CompanyId == companyId
                     && m.BranchId == branchId
                     && m.BusinessUnitId == businessUnitId
                     && !m.IsDeleted)
            .OrderBy(m => m.MovementDate)
            .ToListAsync();
    }

    /// <summary>
    /// Cash actually taken on this session, net of cash refunds.
    ///
    /// Read from the payments rather than the session counter: refunds are recorded as negative
    /// amounts and no code path decrements the counter, so trusting it would report a drawer as
    /// over by the value of every refund given in cash.
    /// </summary>
    public async Task<(decimal Sales, decimal Refunds)> GetSessionCashAsync(Guid sessionId)
    {
        var ids = CompletedTransactions().Where(t => t.PosSessionId == sessionId).Select(t => t.Id);

        var cash = _ctx.PosPayments.AsNoTracking()
            .Where(p => !p.IsDeleted && p.IsApproved
                     && p.TenderType == PosTenderType.Cash
                     && ids.Contains(p.PosTransactionId));

        var taken = await cash.Where(p => p.Amount >= 0).SumAsync(p => (decimal?)p.Amount) ?? 0m;
        var given = await cash.Where(p => p.Amount < 0).SumAsync(p => (decimal?)p.Amount) ?? 0m;
        return (taken, Math.Abs(given));
    }

    // ── Period reports ────────────────────────────────────────────────────────

    public async Task<PosTradingTotals> GetSummaryAsync(DateTime from, DateTime to, Guid? storeId)
        => await AggregateAsync(InRange(CompletedTransactions(), from, to, storeId));

    public async Task<decimal> GetItemsSoldAsync(DateTime from, DateTime to, Guid? storeId)
    {
        var ids = InRange(CompletedTransactions(), from, to, storeId).Select(t => t.Id);
        return await _ctx.PosTransactionLines.AsNoTracking()
            .Where(l => !l.IsDeleted && ids.Contains(l.PosTransactionId))
            .SumAsync(l => (decimal?)l.Quantity) ?? 0m;
    }

    public async Task<int> GetSessionCountAsync(DateTime from, DateTime to, Guid? storeId)
        => await InRange(CompletedTransactions(), from, to, storeId)
            .Select(t => t.PosSessionId).Distinct().CountAsync();

    public async Task<List<PosProductSalesDto>> GetByProductAsync(
        DateTime from, DateTime to, Guid? storeId, int top)
    {
        var ids = InRange(CompletedTransactions(), from, to, storeId).Select(t => t.Id);

        // Variants are grouped separately: "Tee, Large" and "Tee, Small" sell at different rates
        // and collapsing them hides exactly the detail a buyer is looking for.
        var rows = await _ctx.PosTransactionLines.AsNoTracking()
            .Where(l => !l.IsDeleted && ids.Contains(l.PosTransactionId))
            .GroupBy(l => new { l.ProductId, l.ProductCode, l.ProductName, l.VariantId, l.VariantName })
            .Select(g => new PosProductSalesDto
            {
                ProductId = g.Key.ProductId,
                ProductCode = g.Key.ProductCode,
                ProductName = g.Key.ProductName,
                VariantId = g.Key.VariantId,
                VariantName = g.Key.VariantName,
                Quantity = g.Sum(l => l.Quantity),
                NetSales = g.Sum(l => l.TotalAmount),
                Discounts = g.Sum(l => l.DiscountAmount),
                LineCount = g.Count(),
            })
            .OrderByDescending(r => r.NetSales)
            .Take(top)
            .ToListAsync();

        return rows;
    }

    public async Task<List<PosCashierSalesDto>> GetByCashierAsync(DateTime from, DateTime to, Guid? storeId)
    {
        var q = InRange(CompletedTransactions(), from, to, storeId);

        var rows = await q
            .GroupBy(t => t.PosCashierId)
            .Select(g => new
            {
                CashierId = g.Key,
                Gross = g.Where(t => t.TransactionType == PosTransactionType.Sale)
                         .Sum(t => (decimal?)t.TotalAmount) ?? 0m,
                Refunds = g.Where(t => t.TransactionType == PosTransactionType.Refund)
                           .Sum(t => (decimal?)t.TotalAmount) ?? 0m,
                Count = g.Count(t => t.TransactionType == PosTransactionType.Sale),
            })
            .ToListAsync();

        var names = await _ctx.PosCashiers.AsNoTracking()
            .Where(c => rows.Select(r => r.CashierId).Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.DisplayName);

        return rows
            .Select(r => new PosCashierSalesDto
            {
                CashierId = r.CashierId,
                CashierName = names.TryGetValue(r.CashierId, out var n) ? n : null,
                GrossSales = r.Gross,
                Refunds = Math.Abs(r.Refunds),
                NetSales = r.Gross + r.Refunds,
                TransactionCount = r.Count,
                AverageBasket = r.Count > 0 ? r.Gross / r.Count : 0m,
            })
            .OrderByDescending(r => r.NetSales)
            .ToList();
    }

    public async Task<List<PosHourlySalesDto>> GetByHourAsync(DateTime from, DateTime to, Guid? storeId)
    {
        var rows = await InRange(CompletedTransactions(), from, to, storeId)
            .GroupBy(t => t.TransactionDate.Hour)
            .Select(g => new PosHourlySalesDto
            {
                Hour = g.Key,
                NetSales = g.Sum(t => t.TotalAmount),
                TransactionCount = g.Count(t => t.TransactionType == PosTransactionType.Sale),
            })
            .ToListAsync();

        // Every hour of the day is returned, including the quiet ones — a gap in a chart should
        // mean "no sales", not "no data", and the caller cannot tell those apart from sparse rows.
        return Enumerable.Range(0, 24)
            .Select(h => rows.FirstOrDefault(r => r.Hour == h) ?? new PosHourlySalesDto { Hour = h })
            .ToList();
    }

    public async Task<List<PosTenderTotalDto>> GetTenderMixAsync(DateTime from, DateTime to, Guid? storeId)
        => await TendersForAsync(InRange(CompletedTransactions(), from, to, storeId).Select(t => t.Id));

    // ── Shared ────────────────────────────────────────────────────────────────

    private static async Task<PosTradingTotals> AggregateAsync(IQueryable<PosTransaction> q)
    {
        // One round trip. Refund amounts are stored negative, so gross + refunds is already net.
        var agg = await q
            .GroupBy(_ => 1)
            .Select(g => new PosTradingTotals
            {
                GrossSales = g.Where(t => t.TransactionType == PosTransactionType.Sale)
                              .Sum(t => (decimal?)t.TotalAmount) ?? 0m,
                RefundsSigned = g.Where(t => t.TransactionType == PosTransactionType.Refund)
                                 .Sum(t => (decimal?)t.TotalAmount) ?? 0m,
                Discounts = g.Sum(t => (decimal?)t.DiscountAmount) ?? 0m,
                Tax = g.Sum(t => (decimal?)t.TaxAmount) ?? 0m,
                SaleCount = g.Count(t => t.TransactionType == PosTransactionType.Sale),
                RefundCount = g.Count(t => t.TransactionType == PosTransactionType.Refund),
                VoidCount = g.Count(t => t.TransactionType == PosTransactionType.Void),
            })
            .FirstOrDefaultAsync();

        return agg ?? new PosTradingTotals();
    }

    private async Task<List<PosTenderTotalDto>> TendersForAsync(IQueryable<Guid> transactionIds)
    {
        var rows = await _ctx.PosPayments.AsNoTracking()
            .Where(p => !p.IsDeleted && p.IsApproved && transactionIds.Contains(p.PosTransactionId))
            .GroupBy(p => p.TenderType)
            .Select(g => new PosTenderTotalDto
            {
                TenderType = g.Key,
                Amount = g.Sum(p => p.Amount),
                Count = g.Count(),
            })
            .ToListAsync();

        // Share is over the magnitude of what moved, so a refund-heavy day still adds to 100%
        // instead of dividing by a near-zero net and producing nonsense percentages.
        var basis = rows.Sum(r => Math.Abs(r.Amount));
        foreach (var r in rows)
        {
            r.TenderName = TenderName(r.TenderType);
            r.SharePercent = basis == 0 ? 0m : Math.Round(Math.Abs(r.Amount) / basis * 100m, 2);
        }

        return rows.OrderByDescending(r => Math.Abs(r.Amount)).ToList();
    }

    public static string TenderName(PosTenderType t) => t switch
    {
        PosTenderType.Cash => "Cash",
        PosTenderType.CreditCard => "Credit card",
        PosTenderType.DebitCard => "Debit card",
        PosTenderType.MobileWallet => "Mobile wallet",
        PosTenderType.QrCode => "QR code",
        PosTenderType.GiftCard => "Gift card",
        PosTenderType.LoyaltyPoints => "Loyalty points",
        PosTenderType.StoreCredit => "Store credit",
        PosTenderType.SplitPayment => "Split payment",
        _ => t.ToString(),
    };

    public static string MovementName(PosCashMovementType t) => t switch
    {
        PosCashMovementType.CashIn => "Cash in",
        PosCashMovementType.CashOut => "Cash out",
        PosCashMovementType.SafeDrop => "Safe drop",
        PosCashMovementType.PettyCash => "Petty cash",
        PosCashMovementType.OpeningFloat => "Opening float",
        PosCashMovementType.ClosingFloat => "Closing float",
        _ => t.ToString(),
    };
}
