using Nexcore.SharedKernel;

namespace Inventory.Domain.Entities;

/// <summary>
/// Unit of Measure (UOM)
/// Base units for item quantities (PCS, KG, LTR, MTR, etc.)
/// </summary>
public class Unit : BaseEntity
{
    /// <summary>
    /// Unit code (e.g., PCS, KG, LTR)
    /// </summary>
    public new string Code { get; set; } = null!;

    /// <summary>
    /// Unit name (e.g., Pieces, Kilogram, Liter)
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Display order
    /// </summary>
    public int DisplayOrder { get; set; }

    // Navigation properties
    public ICollection<ItemUomConversion> ConversionsFrom { get; set; } = new List<ItemUomConversion>();
    public ICollection<ItemUomConversion> ConversionsTo { get; set; } = new List<ItemUomConversion>();
    public ICollection<Item> BaseUnitItems { get; set; } = new List<Item>();
}
