namespace Sales.Domain.Enums;

/// <summary>
/// Tracks how much of a SalesOrder has been invoiced.
/// Drives the "Invoice Status" column on the order list and the "To Invoice" filter.
/// Mirrors Odoo's sale.order.invoice_status field.
/// </summary>
public enum OrderInvoiceStatus
{
    /// <summary>Order is Draft/Cancelled or has no invoiceable lines.</summary>
    NothingToInvoice = 0,

    /// <summary>Order is confirmed and has uninvoiced quantities — "Create Invoice" button is active.</summary>
    ToInvoice = 1,

    /// <summary>Some lines invoiced, some still pending.</summary>
    PartiallyInvoiced = 2,

    /// <summary>All ordered (or delivered) quantities have been invoiced.</summary>
    FullyInvoiced = 3,
}
