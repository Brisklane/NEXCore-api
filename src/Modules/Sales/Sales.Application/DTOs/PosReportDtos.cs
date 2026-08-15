using Sales.Domain.Enums;

namespace Sales.Application.DTOs;

// ─── Shift reads (X / Z) ──────────────────────────────────────────────────────

/// <summary>
/// A shift read for one cash session.
///
/// An **X read** is taken mid-shift and changes nothing. A **Z read** is the end-of-shift
/// report taken once the drawer has been counted. Both return this same shape — the
/// difference is <see cref="IsProvisional"/>, which is true whenever the session is still
/// open and the figures can still move.
///
/// Every money figure is computed from the transactions and payments themselves rather
/// than read off the session's running counters. The counters are reported separately in
/// <see cref="RecordedCashCollected"/> and friends so a drift between the two is visible
/// instead of silently deciding the drawer is short.
/// </summary>
public class PosShiftReportDto
{
    public Guid SessionId { get; set; }
    public string SessionNumber { get; set; } = string.Empty;
    public PosSessionStatus SessionStatus { get; set; }

    /// <summary>True while the session is still open — the figures are a snapshot, not a close.</summary>
    public bool IsProvisional { get; set; }

    public Guid CashierId { get; set; }
    public string? CashierName { get; set; }
    public Guid TerminalId { get; set; }
    public string? TerminalName { get; set; }
    public Guid? StoreId { get; set; }
    public string? StoreName { get; set; }

    public DateTime OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    /// <summary>When this read was taken.</summary>
    public DateTime GeneratedAt { get; set; }

    // ── Trading ──
    public decimal GrossSales { get; set; }
    /// <summary>Positive figure. Refunds are stored negative and reported here as a magnitude.</summary>
    public decimal Refunds { get; set; }
    public decimal Discounts { get; set; }
    public decimal Tax { get; set; }
    /// <summary>Gross less refunds.</summary>
    public decimal NetSales { get; set; }

    public int SaleCount { get; set; }
    public int RefundCount { get; set; }
    public int VoidCount { get; set; }
    public decimal AverageBasket { get; set; }
    public decimal ItemsSold { get; set; }

    // ── Tenders ──
    public List<PosTenderTotalDto> Tenders { get; set; } = [];

    // ── Drawer ──
    public decimal OpeningFloat { get; set; }
    public decimal CashSales { get; set; }
    public decimal CashRefunds { get; set; }
    public decimal CashIn { get; set; }
    /// <summary>Cash-out, safe drops and petty cash combined — money that left the drawer.</summary>
    public decimal CashOut { get; set; }
    /// <summary>Opening float + net cash taken + cash in − cash out. What should be in the drawer.</summary>
    public decimal ExpectedCash { get; set; }
    /// <summary>What was counted at close. Null while the session is open.</summary>
    public decimal? CountedCash { get; set; }
    /// <summary>Counted − expected. Null while the session is open.</summary>
    public decimal? CashVariance { get; set; }
    public List<PosCashMovementSummaryDto> CashMovements { get; set; } = [];

    // ── The session's own running counters, for comparison ──
    public decimal RecordedSalesTotal { get; set; }
    public decimal RecordedCashCollected { get; set; }
    public int RecordedTransactionCount { get; set; }
    /// <summary>True when a counter disagrees with the transactions — worth investigating, not fatal.</summary>
    public bool CountersDisagree { get; set; }
}

public class PosTenderTotalDto
{
    public PosTenderType TenderType { get; set; }
    public string TenderName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int Count { get; set; }
    /// <summary>Share of the total taken, 0–100.</summary>
    public decimal SharePercent { get; set; }
}

public class PosCashMovementSummaryDto
{
    public PosCashMovementType MovementType { get; set; }
    public string MovementName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    public DateTime MovementDate { get; set; }
}

// ─── Period reports ───────────────────────────────────────────────────────────

/// <summary>Headline trading figures for a date range.</summary>
public class PosSalesSummaryDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public Guid? StoreId { get; set; }

    public decimal GrossSales { get; set; }
    public decimal Refunds { get; set; }
    public decimal Discounts { get; set; }
    public decimal Tax { get; set; }
    public decimal NetSales { get; set; }

    public int SaleCount { get; set; }
    public int RefundCount { get; set; }
    public decimal ItemsSold { get; set; }
    public decimal AverageBasket { get; set; }
    public int SessionCount { get; set; }
}

public class PosProductSalesDto
{
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public Guid? VariantId { get; set; }
    public string? VariantName { get; set; }
    public decimal Quantity { get; set; }
    public decimal NetSales { get; set; }
    public decimal Discounts { get; set; }
    public int LineCount { get; set; }
}

public class PosCashierSalesDto
{
    public Guid CashierId { get; set; }
    public string? CashierName { get; set; }
    public decimal GrossSales { get; set; }
    public decimal Refunds { get; set; }
    public decimal NetSales { get; set; }
    public int TransactionCount { get; set; }
    public decimal AverageBasket { get; set; }
}

/// <summary>One hour of the trading day, aggregated across the whole range.</summary>
public class PosHourlySalesDto
{
    /// <summary>0–23, in the hour the transaction was recorded.</summary>
    public int Hour { get; set; }
    public decimal NetSales { get; set; }
    public int TransactionCount { get; set; }
}
