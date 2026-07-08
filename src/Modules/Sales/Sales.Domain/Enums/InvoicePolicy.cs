namespace Sales.Domain.Enums;

/// <summary>
/// Controls which quantity is used when auto-generating invoice lines from an order.
/// Mirrors Odoo's product.template.invoice_policy field applied at order level.
/// </summary>
public enum InvoicePolicy
{
    /// <summary>Invoice the full ordered quantity regardless of what was delivered (default for products/services).</summary>
    OnOrder = 0,

    /// <summary>Invoice only the quantity that has been delivered. Common for physical goods shipped in batches.</summary>
    OnDelivery = 1,
}
