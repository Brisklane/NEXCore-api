using Nexcore.SharedKernel;

namespace Procurement.Domain.Entities;

/// <summary>
/// Allocation of a vendor payment against a specific invoice.
/// Supports partial payments across multiple invoices from a single payment run.
/// </summary>
public class VendorPaymentLine : BaseEntity
{
    public Guid PaymentId { get; set; }
    public VendorPayment Payment { get; set; } = null!;

    public Guid InvoiceId { get; set; }
    public PurchaseInvoice Invoice { get; set; } = null!;

    public decimal InvoiceTotalAmount { get; set; }
    public decimal InvoiceOutstandingAmount { get; set; }
    public decimal AllocatedAmount { get; set; }
    public decimal DiscountTaken { get; set; }
    public decimal WriteOffAmount { get; set; }

    /// <summary>
    /// Realized FX gain (positive) or loss (negative) arising when this foreign-currency
    /// invoice is settled at a different rate than when it was posted.
    /// Drives a separate Dr/Cr to the FX Gain/Loss account in Accounting.
    /// </summary>
    public decimal FXGainLossAmount { get; set; }

    /// <summary>
    /// Cross-module reference to the FX Gain/Loss GL account in Accounting. ID only.
    /// Falls back to the company default FX account if null.
    /// </summary>
    public Guid? FXGainLossLedgerAccountId { get; set; }

    public string? Notes { get; set; }
}
