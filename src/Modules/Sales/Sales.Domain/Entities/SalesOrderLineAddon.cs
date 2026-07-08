using Nexcore.SharedKernel;

namespace Sales.Domain.Entities;

/// <summary>
/// Add-on / modifier on a SalesOrderLine.
/// Shared by all channels — food service POS, customer app, online orders.
/// Examples: extra sauce, gift wrapping, extended warranty, installation service.
/// </summary>
public class SalesOrderLineAddon : BaseEntity
{
    public Guid SalesOrderLineId { get; set; }
    public SalesOrderLine SalesOrderLine { get; set; } = null!;

    /// <summary>FK to Inventory Item if the add-on is a stockable product.</summary>
    public Guid? AddonProductId { get; set; }

    /// <summary>Display name — copied from product catalog or entered manually.</summary>
    public string AddonName { get; set; } = string.Empty;

    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
}
