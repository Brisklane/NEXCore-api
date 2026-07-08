using Nexcore.SharedKernel;

namespace Inventory.Domain.Entities;

/// <summary>
/// UOM Conversion
/// Defines conversion factors between different units of measure for an item
/// Example: 1 Box = 12 PCS
/// </summary>
public class ItemUomConversion : BaseEntity
{
    /// <summary>
    /// Item ID
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// From Unit ID (source unit)
    /// </summary>
    public Guid FromUnitId { get; set; }

    /// <summary>
    /// To Unit ID (target unit)
    /// </summary>
    public Guid ToUnitId { get; set; }

    /// <summary>
    /// Conversion factor
    /// Example: 1 (From) = 12 (To)
    /// </summary>
    public decimal ConversionFactor { get; set; }

    // Navigation properties
    public Item? Item { get; set; }
    public Unit? FromUnit { get; set; }
    public Unit? ToUnit { get; set; }
}
