using Nexcore.SharedKernel;

namespace Manufacturing.Domain.Entities;

/// <summary>
/// Finished Goods Receipt - tracks completion and receipt of finished products into inventory
/// </summary>
public class FinishedGoodsReceipt : BaseEntity
{
    /// <summary>
    /// Production Order reference
    /// </summary>
    public Guid ProductionOrderId { get; set; }

    /// <summary>
    /// Finished product reference (External - from Inventory module)
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Quantity received
    /// </summary>
    public decimal QuantityReceived { get; set; }

    /// <summary>
    /// Destination warehouse reference (External - from Inventory module)
    /// </summary>
    public Guid WarehouseId { get; set; }

    /// <summary>
    /// Batch number for traceability
    /// </summary>
    public string? BatchNo { get; set; }

    /// <summary>
    /// Receipt timestamp
    /// </summary>
    public DateTime ReceivedAt { get; set; }

    /// <summary>
    /// Notes
    /// </summary>
    public string? Notes { get; set; }

    // Navigation properties
    public ProductionOrder? ProductionOrder { get; set; }
}