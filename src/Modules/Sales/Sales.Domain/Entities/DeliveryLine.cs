using Nexcore.SharedKernel;

namespace Sales.Domain.Entities;

/// <summary>
/// One line of a delivery — which order line is being shipped and how much.
/// Product / UOM details are read from the linked SalesOrderLine; only
/// warehouse-execution data that is unknown at order time lives here.
/// </summary>
public class DeliveryLine : BaseEntity
{
    public Guid DeliveryId { get; set; }
    public Delivery Delivery { get; set; } = null!;

    public int LineNumber { get; set; }

    /// <summary>Source order line — product, UOM, ordered qty all come from here.</summary>
    public Guid SalesOrderLineId { get; set; }
    public SalesOrderLine SalesOrderLine { get; set; } = null!;

    /// <summary>Quantity actually shipped in this delivery (may be less than ordered).</summary>
    public decimal DeliveredQuantity { get; set; }

    // ── Warehouse execution (only known at pick / dispatch time) ─────────────
    public string? BinLocation { get; set; }
    public string? LotNumber { get; set; }
    public string? SerialNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }

    public string? Notes { get; set; }
}
