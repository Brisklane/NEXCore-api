using Nexcore.SharedKernel;

namespace Inventory.Domain.Entities;

/// <summary>
/// Item Barcode
/// Supports multiple barcodes per item - one per UOM or simply multiple identifiers.
/// Essential for POS scanning (EAN-13, QR, Code128, UPC-A, etc.)
/// Example: Item "Water Bottle" ? EAN13 on single unit, QR on a 6-pack
/// </summary>
public class ItemBarcode : BaseEntity
{
    /// <summary>
    /// Item this barcode belongs to
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Unit ID - which unit of measure this barcode scans as (e.g., PCS, BOX, PACK)
    /// Null = applies to base unit
    /// </summary>
    public Guid? UnitId { get; set; }

    /// <summary>
    /// Barcode value (e.g., 6281234567890)
    /// </summary>
    public string Barcode { get; set; } = null!;

    /// <summary>
    /// Barcode format: EAN13 | EAN8 | UPCA | UPCE | Code128 | Code39 | QR | DataMatrix
    /// </summary>
    public string BarcodeType { get; set; } = "EAN13";

    /// <summary>
    /// Whether this is the primary/default barcode for the item
    /// </summary>
    public bool IsPrimary { get; set; }

    // Navigation properties
    public Item? Item { get; set; }
    public Unit? Unit { get; set; }
}
