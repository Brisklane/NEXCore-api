using Nexcore.SharedKernel;

namespace Sales.Domain.Entities;

/// <summary>
/// Individual product price within a Price List.
/// Supports unit-of-measure specific pricing and quantity breaks (volume pricing).
/// </summary>
public class PriceListItem : BaseEntity
{
    public Guid PriceListId { get; set; }
    public PriceList PriceList { get; set; } = null!;

    /// <summary>FK to Inventory Item / Product catalog.</summary>
    public Guid ProductId { get; set; }

    public string? UnitOfMeasure { get; set; }

    // ??? Pricing ??????????????????????????????????????????????????????????????
    public decimal UnitPrice { get; set; }

    /// <summary>Minimum quantity for this price to apply (volume pricing).</summary>
    public decimal? MinQuantity { get; set; }
    public decimal? MaxQuantity { get; set; }

    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }
}
