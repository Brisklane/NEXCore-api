using Nexcore.SharedKernel;

namespace Manufacturing.Domain.Entities;

/// <summary>
/// Material Issue - tracks raw material consumption from inventory against a production order
/// </summary>
public class MaterialIssue : BaseEntity
{
    /// <summary>
    /// Production Order reference
    /// </summary>
    public Guid ProductionOrderId { get; set; }

    /// <summary>
    /// Material/Component product reference (External - from Inventory module)
    /// </summary>
    public Guid MaterialId { get; set; }

    /// <summary>
    /// Quantity issued/consumed
    /// </summary>
    public decimal QuantityIssued { get; set; }

    /// <summary>
    /// Quantity returned to inventory
    /// </summary>
    public decimal QuantityReturned { get; set; } = 0;

    /// <summary>
    /// Timestamp when material was issued
    /// </summary>
    public DateTime IssuedAt { get; set; }

    /// <summary>
    /// User who issued the material
    /// </summary>
    public Guid IssuedById { get; set; }

    /// <summary>
    /// Notes
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public ProductionOrder? ProductionOrder { get; set; }
}