namespace Sales.Domain.Enums;

/// <summary>
/// Tracks the payment state of a SalesInvoice, independent of its lifecycle status.
/// Mirrors Odoo's account.move.payment_state field.
/// </summary>
public enum InvoicePaymentStatus
{
    /// <summary>No payment has been registered against this invoice.</summary>
    NotPaid = 0,

    /// <summary>
    /// Payment registered but not yet fully reconciled with a bank statement.
    /// Shown as the "IN PAYMENT" ribbon in Odoo.
    /// </summary>
    InPayment = 1,

    /// <summary>Invoice fully paid and reconciled.</summary>
    Paid = 2,

    /// <summary>A partial payment has been applied — balance remains.</summary>
    Partial = 3,

    /// <summary>Payment was reversed (refunded or credit note applied).</summary>
    Reversed = 4,
}
