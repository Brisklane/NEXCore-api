using Nexcore.SharedKernel;

namespace Manufacturing.Domain.Entities;

/// <summary>
/// Production Order Component - order-specific copy of BOM items created at order release.
/// Allows quantity changes, material substitutions, and actual vs planned tracking
/// without modifying the master BOM.
/// SAP Equivalent: PP Order Component (CO03 Components) | Oracle Equivalent: Work Order Material Requirement
/// </summary>
public class ProductionOrderComponent : BaseEntity
{
    /// <summary>
    /// Production Order reference
    /// </summary>
    public Guid ProductionOrderId { get; set; }

    /// <summary>
    /// Source BOM item template reference (nullable for manually added components)
    /// </summary>
    public Guid? BOMItemId { get; set; }

    /// <summary>
    /// Material/Component product reference (External - from Inventory module)
    /// </summary>
    public Guid MaterialId { get; set; }

    /// <summary>
    /// Planned quantity required based on BOM for the production order quantity
    /// </summary>
    public decimal PlannedQty { get; set; }

    /// <summary>
    /// Total quantity actually issued against this component
    /// </summary>
    public decimal IssuedQty { get; set; } = 0;

    /// <summary>
    /// Quantity returned back to inventory
    /// </summary>
    public decimal ReturnedQty { get; set; } = 0;

    /// <summary>
    /// Unit of measure
    /// </summary>
    public required string UnitOfMeasure { get; set; }

    /// <summary>
    /// Scrap percentage for this component in this order
    /// </summary>
    public decimal ScrapPercentage { get; set; } = 0;

    /// <summary>
    /// Indicates if the original BOM material was substituted with a different material
    /// </summary>
    public bool IsSubstituted { get; set; } = false;

    /// <summary>
    /// Original material reference before substitution (if substituted)
    /// </summary>
    public Guid? OriginalMaterialId { get; set; }

    /// <summary>
    /// Storage location / bin reference (External - from Inventory module)
    /// </summary>
    public Guid? StorageLocationId { get; set; }

    /// <summary>
    /// Indicates if this component was manually added (not from BOM)
    /// </summary>
    public bool IsManuallyAdded { get; set; } = false;

    /// <summary>
    /// Notes and remarks
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public ProductionOrder? ProductionOrder { get; set; }
    public BOMItem? BOMItem { get; set; }
}
