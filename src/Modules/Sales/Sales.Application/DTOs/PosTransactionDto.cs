using Sales.Domain.Enums;

namespace Sales.Application.DTOs;

// ── Checkout request ──────────────────────────────────────────────────────────

/// <summary>
/// Request to settle (pay) a POS sale.
///
/// Flow:
///   1. Cashier scans items  → a Draft <c>SalesOrder</c> already exists (created via SalesOrder/Create).
///   2. Cashier taps "Pay"   → this DTO is posted: the order is invoiced, the tender(s) recorded,
///                             and a <c>PosTransaction</c> is written. Receipt no = invoice no.
///
/// Partial / credit sale: when <see cref="AllowCredit"/> is true and the tendered amount is less than
/// the balance due, the remaining balance stays on the customer's account (invoice + order go
/// PartiallyPaid). Otherwise the tendered amount must cover the balance.
/// </summary>
public class PosCheckoutDto
{
    /// <summary>The Draft / Parked sales order created while scanning items.</summary>
    public Guid SalesOrderId { get; set; }

    // POS context
    public Guid PosSessionId { get; set; }
    public Guid PosTerminalId { get; set; }
    public Guid PosStoreId { get; set; }
    public Guid PosCashierId { get; set; }

    /// <summary>One entry per tender (cash, card, …). Split payment = multiple entries.</summary>
    public List<PosTenderDto> Tenders { get; set; } = [];

    /// <summary>
    /// When true, a tender that does not cover the full balance is accepted as a credit sale —
    /// the remaining balance stays receivable on the customer's account.
    /// When false (default), the tender must cover the balance due.
    /// </summary>
    public bool AllowCredit { get; set; }

    /// <summary>
    /// Skip the pre-payment stock-availability check. Set by the offline-sync replay: those sales
    /// already completed on the device, so they must never be blocked on re-settlement even if the
    /// store enforces stock. Online checkout leaves this false.
    /// </summary>
    public bool SkipStockCheck { get; set; }

    public string? Notes { get; set; }
}

/// <summary>A single payment tender within a POS checkout.</summary>
public class PosTenderDto
{
    public PosTenderType TenderType { get; set; }
    public decimal Amount { get; set; }

    /// <summary>Card last 4, wallet txn id, cheque no., etc.</summary>
    public string? ReferenceNumber { get; set; }
    public string? AuthorizationCode { get; set; }
    public string? CardScheme { get; set; }
    public string? CardLast4 { get; set; }
}

// ── Read models ───────────────────────────────────────────────────────────────

public class PosTransactionDto
{
    public Guid Id { get; set; }
    public string TransactionNumber { get; set; } = string.Empty;
    public string? ReceiptNumber { get; set; }

    public Guid PosSessionId { get; set; }
    public Guid PosTerminalId { get; set; }
    public Guid PosStoreId { get; set; }
    public Guid PosCashierId { get; set; }
    public Guid? ContactId { get; set; }
    public Guid? SalesOrderId { get; set; }

    public PosTransactionType TransactionType { get; set; }
    public PosTransactionStatus Status { get; set; }
    public DateTime TransactionDate { get; set; }

    public decimal SubtotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal RoundingAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TenderedAmount { get; set; }
    public decimal ChangeAmount { get; set; }

    public string? Notes { get; set; }

    public List<PosTransactionLineDto> Lines { get; set; } = [];
    public List<PosPaymentDto> Payments { get; set; } = [];
}

public class PosTransactionLineDto
{
    public Guid Id { get; set; }
    public int LineNumber { get; set; }
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public Guid? VariantId { get; set; }
    public string? VariantName { get; set; }
    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetUnitPrice { get; set; }
    public decimal LineAmount { get; set; }
    public TaxCategory TaxCategory { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
}

public class PosPaymentDto
{
    public Guid Id { get; set; }
    public PosTenderType TenderType { get; set; }
    public decimal Amount { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? AuthorizationCode { get; set; }
    public string? CardScheme { get; set; }
    public string? CardLast4 { get; set; }
    public bool IsApproved { get; set; }
}

/// <summary>Result returned to the POS device after a successful checkout — drives the printed receipt.</summary>
public class PosCheckoutResultDto
{
    public PosTransactionDto Transaction { get; set; } = null!;

    public Guid SalesOrderId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;

    public Guid InvoiceId { get; set; }
    /// <summary>Same value as <see cref="ReceiptNumber"/> — POS receipt no = invoice no.</summary>
    public string InvoiceNumber { get; set; } = string.Empty;
    public string ReceiptNumber { get; set; } = string.Empty;

    public decimal TotalAmount { get; set; }
    public decimal TenderedAmount { get; set; }
    public decimal ChangeAmount { get; set; }

    /// <summary>Remaining balance on the customer's account after this tender (0 for a fully-paid sale).</summary>
    public decimal BalanceDue { get; set; }
    public bool IsFullyPaid { get; set; }
}
