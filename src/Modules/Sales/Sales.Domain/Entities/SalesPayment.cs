using Nexcore.SharedKernel;

namespace Sales.Domain.Entities;

/// <summary>
/// A payment received. One payment can be allocated across multiple invoices
/// via the PaymentAllocation join entity.
///
/// POS walk-in: SalesOrderId set, zero allocations (no invoice generated).
/// B2B / App:   One or more PaymentAllocations link this payment to invoices.
/// Partial pay: Allocations sum to Amount; invoices remain PartiallyPaid.
/// </summary>
public class SalesPayment : BaseEntity
{
    // ── Identity ──────────────────────────────────────────────────────────────
    public string PaymentNumber { get; set; } = string.Empty;

    // ── Links ─────────────────────────────────────────────────────────────────
    /// <summary>Always set — every payment traces back to an order.</summary>
    public Guid SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = null!;

    /// <summary>POS transaction that generated this payment. Null for bank/online payments.</summary>
    public Guid? PosTransactionId { get; set; }
    public PosTransaction? PosTransaction { get; set; }

    /// <summary>Contact who paid. Cross-module Guid. Null for fully anonymous POS sales.</summary>
    public Guid? ContactId { get; set; }

    // ── Payment Details ───────────────────────────────────────────────────────
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public decimal Amount { get; set; }

    public string CurrencyCode { get; set; } = "USD";
    public decimal ExchangeRate { get; set; } = 1m;

    /// <summary>Cash, Card, BankTransfer, Cheque, Wallet, GiftCard, LoyaltyPoints, COD.</summary>
    public string PaymentMethod { get; set; } = string.Empty;

    public string? ReferenceNumber { get; set; }
    public string? BankName { get; set; }

    // ── Online / Gateway ──────────────────────────────────────────────────────
    public string? GatewayTransactionId { get; set; }
    public string? GatewayResponse { get; set; }
    public string? FailureReason { get; set; }
    public DateTime? ConfirmedAt { get; set; }

    // ── Refund ────────────────────────────────────────────────────────────────
    public bool IsRefunded { get; set; }
    public decimal RefundedAmount { get; set; }
    public DateTime? RefundedAt { get; set; }

    public string? Notes { get; set; }

    /// <summary>Reference to AR receipt in Accounting module.</summary>
    public Guid? AccountingReceiptId { get; set; }

    // ── Navigation ────────────────────────────────────────────────────────────
    /// <summary>Which invoices this payment is allocated to and how much.</summary>
    public ICollection<PaymentAllocation> Allocations { get; set; } = new List<PaymentAllocation>();
}
