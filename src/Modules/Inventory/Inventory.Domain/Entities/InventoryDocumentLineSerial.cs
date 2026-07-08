using Nexcore.SharedKernel;

namespace Inventory.Domain.Entities;

/// <summary>
/// A specific serialized unit captured on an <see cref="InventoryDocumentLine"/> for a serial-tracked item.
/// On a receipt (GRN) these are the serials/IMEIs being brought in; on an issue/delivery these are the
/// exact units going out. Posting turns each of these into an <see cref="ItemSerial"/> registry change
/// plus a quantity-1 <see cref="InventoryTransaction"/>.
/// </summary>
public class InventoryDocumentLineSerial : BaseEntity
{
    /// <summary>Owning document line.</summary>
    public Guid DocumentLineId { get; set; }

    /// <summary>Serial number of the unit.</summary>
    public string SerialNumber { get; set; } = null!;

    public string? Imei { get; set; }
    public string? Imei2 { get; set; }
    public string? MacAddress { get; set; }

    // ── Navigation ───────────────────────────────────────────────────────────
    public InventoryDocumentLine? DocumentLine { get; set; }
}
