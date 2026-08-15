using Sales.Application.DTOs;
using Sales.Domain.Entities;

namespace Sales.Infrastructure.Repositories.Interfaces;

/// <summary>
/// Raw trading figures, straight out of one grouped query. Kept separate from the DTO so the
/// controller can decide how to present a refund (stored negative, reported as a magnitude).
/// </summary>
public class PosTradingTotals
{
    public decimal GrossSales { get; set; }
    /// <summary>Negative, as stored. Net sales is <c>GrossSales + RefundsSigned</c>.</summary>
    public decimal RefundsSigned { get; set; }
    public decimal Discounts { get; set; }
    public decimal Tax { get; set; }
    public int SaleCount { get; set; }
    public int RefundCount { get; set; }
    public int VoidCount { get; set; }

    public decimal Refunds => Math.Abs(RefundsSigned);
    public decimal NetSales => GrossSales + RefundsSigned;
}

/// <summary>
/// Aggregation for POS reporting. Every method groups in SQL and returns totals, never rows —
/// a shift report must not get slower as the store gets busier.
/// </summary>
public interface IPosReportRepository
{
    // Shift reads
    Task<PosSession?> GetSessionAsync(Guid sessionId);
    Task<PosTradingTotals> GetSessionTotalsAsync(Guid sessionId);
    Task<List<PosTenderTotalDto>> GetSessionTendersAsync(Guid sessionId);
    Task<decimal> GetSessionItemCountAsync(Guid sessionId);
    Task<List<PosCashMovement>> GetCashMovementsAsync(Guid sessionId);
    /// <summary>Cash taken and cash given back on this session, both as positive figures.</summary>
    Task<(decimal Sales, decimal Refunds)> GetSessionCashAsync(Guid sessionId);

    // Period reports
    Task<PosTradingTotals> GetSummaryAsync(DateTime from, DateTime to, Guid? storeId);
    Task<decimal> GetItemsSoldAsync(DateTime from, DateTime to, Guid? storeId);
    Task<int> GetSessionCountAsync(DateTime from, DateTime to, Guid? storeId);
    Task<List<PosProductSalesDto>> GetByProductAsync(DateTime from, DateTime to, Guid? storeId, int top);
    Task<List<PosCashierSalesDto>> GetByCashierAsync(DateTime from, DateTime to, Guid? storeId);
    Task<List<PosHourlySalesDto>> GetByHourAsync(DateTime from, DateTime to, Guid? storeId);
    Task<List<PosTenderTotalDto>> GetTenderMixAsync(DateTime from, DateTime to, Guid? storeId);
}
