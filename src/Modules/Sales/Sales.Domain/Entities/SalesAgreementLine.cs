using Nexcore.SharedKernel;

namespace Sales.Domain.Entities;

/// <summary>Product line committed under a Sales Agreement.</summary>
public class SalesAgreementLine : BaseEntity
{
    public Guid SalesAgreementId { get; set; }
    public SalesAgreement SalesAgreement { get; set; } = null!;

    public int LineNumber { get; set; }

    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string UnitOfMeasure { get; set; } = string.Empty;

    public decimal CommittedQuantity { get; set; }
    public decimal ReleasedQuantity { get; set; }
    public decimal RemainingQuantity { get; set; }

    public decimal AgreedUnitPrice { get; set; }
    public decimal DiscountPercentage { get; set; }

    public string? Notes { get; set; }
}
