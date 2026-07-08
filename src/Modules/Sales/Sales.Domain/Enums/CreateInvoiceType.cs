namespace Sales.Domain.Enums;

/// <summary>
/// Which type of invoice to generate from a Sales Order.
/// Mirrors Odoo's "Create Invoice" dialog options.
/// </summary>
public enum CreateInvoiceType
{
    /// <summary>
    /// Full invoice for all uninvoiced quantities on the order.
    /// Uses InvoicePolicy to decide whether to count ordered or delivered qty.
    /// </summary>
    Regular = 0,

    /// <summary>
    /// Advance invoice for a percentage of the order total (e.g. 30% deposit).
    /// Requires DownPaymentPercentage in the request.
    /// </summary>
    DownPaymentPercentage = 1,

    /// <summary>
    /// Advance invoice for a fixed monetary amount (e.g. 5 000 Rs. deposit).
    /// Requires DownPaymentAmount in the request.
    /// </summary>
    DownPaymentFixed = 2,
}
