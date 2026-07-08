using Nexcore.SharedKernel;

namespace Sales.Domain.Entities;

/// <summary>Line item on a Customer Return.</summary>
public class SalesReturnLine : BaseEntity
{
    public Guid SalesReturnId { get; set; }
    public SalesReturn SalesReturn { get; set; } = null!;

    public int LineNumber { get; set; }

    public Guid SalesOrderLineId { get; set; }
    public SalesOrderLine SalesOrderLine { get; set; } = null!;

    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;

    public decimal ReturnedQuantity { get; set; }
    public string UnitOfMeasure { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }
    public decimal RefundAmount { get; set; }

    /// <summary>Good, Damaged, Defective, Expired.</summary>
    public string? ConditionOnReturn { get; set; }

    public string? Reason { get; set; }
    public string? Notes { get; set; }
}
