using Nexcore.SharedKernel;

namespace Manufacturing.Domain.Entities;

/// <summary>
/// BOM By-Product / Co-Product - secondary outputs produced alongside the main finished product.
/// Critical for process manufacturing industries: food, chemicals, pharmaceuticals, oil &amp; gas.
/// SAP Equivalent: BOM Item with negative quantity / co-product flag | Oracle Equivalent: Co-Product/By-Product Line
/// </summary>
public class BOMByProduct : BaseEntity
{
    /// <summary>
    /// Bill of Material reference
    /// </summary>
    public Guid BillOfMaterialId { get; set; }

    /// <summary>
    /// Product reference for the by-product or co-product (External - from Inventory module)
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Type of secondary output
    /// </summary>
    public required string Type { get; set; } // CoProduct, ByProduct, Scrap, Waste

    /// <summary>
    /// Quantity produced per unit of main finished product
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit of measure
    /// </summary>
    public required string UnitOfMeasure { get; set; }

    /// <summary>
    /// Cost allocation percentage for co-products (total of all co-products must equal 100%)
    /// </summary>
    public decimal? CostAllocationPercent { get; set; }

    /// <summary>
    /// Destination warehouse reference (External - from Inventory module)
    /// </summary>
    public Guid? WarehouseId { get; set; }

    /// <summary>
    /// Notes and remarks
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public BillOfMaterial? BillOfMaterial { get; set; }
}
