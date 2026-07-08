using Nexcore.SharedKernel;

namespace Manufacturing.Domain.Entities;

/// <summary>
/// Work In Progress (WIP) - tracks partially completed goods during manufacturing
/// </summary>
public class WorkInProgress : BaseEntity
{
    /// <summary>
    /// Production Order reference
    /// </summary>
    public Guid ProductionOrderId { get; set; }

    /// <summary>
    /// Quantity currently in progress
    /// </summary>
    public decimal QuantityInProgress { get; set; }

    /// <summary>
    /// Quantity completed so far
    /// </summary>
    public decimal QuantityCompleted { get; set; } = 0;

    /// <summary>
    /// Quantity rejected during progress
    /// </summary>
    public decimal QuantityRejected { get; set; } = 0;

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime LastUpdatedAt { get; set; }

    /// <summary>
    /// Notes
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public ProductionOrder? ProductionOrder { get; set; }
}