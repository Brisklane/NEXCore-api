using Nexcore.SharedKernel;

namespace Inventory.Domain.Entities;

/// <summary>
/// One entry in a serialized unit's audit trail — every status change / movement is logged here,
/// giving full per-unit traceability (received → sold → returned → RMA → scrapped, etc.).
/// </summary>
public class ItemSerialHistory : BaseEntity
{
    /// <summary>The serialized unit this event belongs to.</summary>
    public Guid ItemSerialId { get; set; }

    /// <summary>Event type — see <see cref="Constants.SerialEventType"/>.</summary>
    public string EventType { get; set; } = null!;

    /// <summary>Status before the event (null for the initial Received event).</summary>
    public string? FromStatus { get; set; }

    /// <summary>Status after the event.</summary>
    public string? ToStatus { get; set; }

    /// <summary>Warehouse involved in the event (optional).</summary>
    public Guid? WarehouseId { get; set; }

    /// <summary>Document that triggered the event (GRN, Issue, Transfer, etc.).</summary>
    public Guid? DocumentId { get; set; }

    /// <summary>When the event occurred.</summary>
    public DateTime EventDate { get; set; } = DateTime.UtcNow;

    /// <summary>Free-text note / reason.</summary>
    public string? Notes { get; set; }

    // ── Navigation ───────────────────────────────────────────────────────────
    public ItemSerial? ItemSerial { get; set; }
}
