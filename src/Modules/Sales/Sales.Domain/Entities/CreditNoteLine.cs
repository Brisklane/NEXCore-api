using Nexcore.SharedKernel;
using Sales.Domain.Enums;

namespace Sales.Domain.Entities;

/// <summary>Line item on a Credit Note.</summary>
public class CreditNoteLine : BaseEntity
{
    public Guid CreditNoteId { get; set; }
    public CreditNote CreditNote { get; set; } = null!;

    public int LineNumber { get; set; }

    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;

    public decimal Quantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal LineAmount { get; set; }

    public TaxCategory TaxCategory { get; set; } = TaxCategory.Standard;
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public string? Reason { get; set; }
}
