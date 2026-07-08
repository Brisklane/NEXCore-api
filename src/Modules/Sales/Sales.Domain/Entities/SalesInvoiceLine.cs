using Nexcore.SharedKernel;
using Sales.Domain.Enums;

namespace Sales.Domain.Entities;

/// <summary>
/// Individual line item on a Sales Invoice.
/// SalesOrderLineId is null for down-payment lines (no specific order line referenced).
/// </summary>
public class SalesInvoiceLine : BaseEntity
{
    public Guid SalesInvoiceId { get; set; }
    public SalesInvoice SalesInvoice { get; set; } = null!;

    public int LineNumber { get; set; }

    /// <summary>Null for down-payment lines where no specific order line is invoiced.</summary>
    public Guid? SalesOrderLineId { get; set; }
    public SalesOrderLine? SalesOrderLine { get; set; }

    public Guid? ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;

    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineAmount { get; set; }

    public TaxCategory TaxCategory { get; set; } = TaxCategory.Standard;
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }

    /// <summary>
    /// True for down-payment lines generated from a percentage or fixed-amount advance.
    /// These are deducted when the final regular invoice is created.
    /// </summary>
    public bool IsDownPayment { get; set; }
}
