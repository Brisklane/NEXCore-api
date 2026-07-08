using Nexcore.SharedKernel;

namespace Manufacturing.Domain.Entities;

/// <summary>
/// BOM Item - individual component required for manufacturing a product
/// </summary>
public class BOMItem : BaseEntity
{
    /// <summary>
    /// Bill of Material reference
    /// </summary>
    public Guid BillOfMaterialId { get; set; }

    /// <summary>
    /// Material/Component product reference (External - from Inventory module)
    /// </summary>
    public Guid MaterialId { get; set; }

    /// <summary>
    /// Quantity required per unit of finished product
    /// </summary>
    public decimal QuantityRequired { get; set; }

    /// <summary>
    /// Scrap percentage expected during manufacturing
    /// </summary>
    public decimal ScrapPercentage { get; set; } = 0;

    /// <summary>
    /// Unit of measure for the material
    /// </summary>
    public required string UnitOfMeasure { get; set; }

    /// <summary>
    /// Notes for this BOM item
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public BillOfMaterial? BillOfMaterial { get; set; }
}