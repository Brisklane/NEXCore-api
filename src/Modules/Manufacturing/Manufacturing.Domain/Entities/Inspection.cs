using Nexcore.SharedKernel;

namespace Manufacturing.Domain.Entities;

/// <summary>
/// Inspection - quality checkpoint record during or after production
/// </summary>
public class Inspection : BaseEntity
{
    /// <summary>
    /// Production Order reference
    /// </summary>
    public Guid ProductionOrderId { get; set; }

    /// <summary>
    /// Quantity inspected
    /// </summary>
    public decimal InspectedQty { get; set; }

    /// <summary>
    /// Quantity passed inspection
    /// </summary>
    public decimal PassedQty { get; set; }

    /// <summary>
    /// Quantity rejected
    /// </summary>
    public decimal RejectedQty { get; set; }

    /// <summary>
    /// Inspection status
    /// </summary>
    public required string Status { get; set; } = "Pending"; // Pending, Passed, Rejected, Rework

    /// <summary>
    /// User who performed the inspection
    /// </summary>
    public Guid InspectedById { get; set; }

    /// <summary>
    /// Inspection timestamp
    /// </summary>
    public DateTime InspectedAt { get; set; }

    /// <summary>
    /// Remarks and findings
    /// </summary>
    public string? Remarks { get; set; }

    // Navigation properties
    public ProductionOrder? ProductionOrder { get; set; }
    public ICollection<InspectionCharacteristic>? Characteristics { get; set; }
    public ICollection<ProductionBatch>? Batches { get; set; }
}