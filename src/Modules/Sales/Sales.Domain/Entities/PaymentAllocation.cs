using Nexcore.SharedKernel;

namespace Sales.Domain.Entities;

/// <summary>
/// Links a SalesPayment to one or more SalesInvoices, recording how much
/// of the payment is applied to each invoice.
///
/// This is the join entity that implements the Invoice *----* Payment relationship.
///
/// Examples:
///   • Full payment of one invoice:   1 allocation, AllocatedAmount == Invoice.TotalAmount
///   • Partial payment of one invoice: 1 allocation, AllocatedAmount &lt; Invoice.TotalAmount
///   • One payment across 2 invoices: 2 allocations that sum to Payment.Amount
///
/// POS walk-in sales with no invoice have zero allocations.
/// </summary>
public class PaymentAllocation : BaseEntity
{
    public Guid SalesPaymentId { get; set; }
    public SalesPayment Payment { get; set; } = null!;

    public Guid SalesInvoiceId { get; set; }
    public SalesInvoice Invoice { get; set; } = null!;

    /// <summary>Portion of the payment credited to this invoice.</summary>
    public decimal AllocatedAmount { get; set; }

    public DateTime AllocatedAt { get; set; } = DateTime.UtcNow;

    public string? Notes { get; set; }
}
