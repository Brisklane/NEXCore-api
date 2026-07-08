namespace Inventory.Application.DTOs;

// ═══════════════════════════════════════════════════════════════════════════
//  Serial / Lot tracking DTOs
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>A single serialized unit (ItemSerial) as returned to the UI.</summary>
public class ItemSerialDto
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public string? ItemCode { get; set; }
    public string? ItemName { get; set; }
    public Guid? VariantId { get; set; }
    public string SerialNumber { get; set; } = null!;
    public string? Imei { get; set; }
    public string? Imei2 { get; set; }
    public string? MacAddress { get; set; }
    public string Status { get; set; } = null!;
    public Guid? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public Guid? BinId { get; set; }
    public decimal UnitCost { get; set; }
    public Guid? ReceiptDocumentId { get; set; }
    public DateTime? ReceiptDate { get; set; }
    public Guid? SupplierId { get; set; }
    public DateTime? WarrantyStartDate { get; set; }
    public DateTime? WarrantyEndDate { get; set; }
    public Guid? SoldDocumentId { get; set; }
    public DateTime? SoldDate { get; set; }
    public string? SalesReference { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>One audit-trail entry for a serialized unit.</summary>
public class ItemSerialHistoryDto
{
    public Guid Id { get; set; }
    public Guid ItemSerialId { get; set; }
    public string EventType { get; set; } = null!;
    public string? FromStatus { get; set; }
    public string? ToStatus { get; set; }
    public Guid? WarehouseId { get; set; }
    public string? WarehouseName { get; set; }
    public Guid? DocumentId { get; set; }
    public DateTime EventDate { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Manually register a single serialized unit (outside a receipt flow).</summary>
public class CreateItemSerialDto
{
    public Guid ItemId { get; set; }
    public Guid? VariantId { get; set; }
    public string SerialNumber { get; set; } = null!;
    public string? Imei { get; set; }
    public string? Imei2 { get; set; }
    public string? MacAddress { get; set; }
    public Guid? WarehouseId { get; set; }
    public Guid? BinId { get; set; }
    public decimal UnitCost { get; set; }
    public Guid? SupplierId { get; set; }
    public DateTime? WarrantyStartDate { get; set; }
    public DateTime? WarrantyEndDate { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Change a serialized unit's lifecycle status (e.g. mark Defective / Scrapped / Returned).</summary>
public class UpdateSerialStatusDto
{
    public string Status { get; set; } = null!;
    public string? Notes { get; set; }
}

/// <summary>
/// Bulk-generate N serials for an item from a prefix + starting number
/// (e.g. prefix "SN-", start 1, count 20, padding 5 → SN-00001 … SN-00020).
/// </summary>
public class BulkGenerateSerialsDto
{
    public Guid ItemId { get; set; }
    public Guid? VariantId { get; set; }
    public string Prefix { get; set; } = "";
    public int StartNumber { get; set; } = 1;
    public int Count { get; set; } = 1;
    public int Padding { get; set; } = 5;
    public Guid? WarehouseId { get; set; }
    public decimal UnitCost { get; set; }
}

/// <summary>Result of a global serial/IMEI scan lookup.</summary>
public class SerialLookupResultDto
{
    public bool Found { get; set; }
    public ItemSerialDto? Serial { get; set; }
    public List<ItemSerialHistoryDto> History { get; set; } = new();
}

/// <summary>A batch / lot (ItemBatch) as returned to the UI.</summary>
public class ItemBatchDto
{
    public Guid Id { get; set; }
    public Guid ItemId { get; set; }
    public string? ItemCode { get; set; }
    public string? ItemName { get; set; }
    public Guid? VariantId { get; set; }
    public string BatchNumber { get; set; } = null!;
    public DateTime? ManufactureDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public Guid? SupplierId { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal RemainingQuantity { get; set; }
    public decimal ReservedQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public string Status { get; set; } = null!;
    /// <summary>Days until expiry (negative = already expired). Null when no expiry date.</summary>
    public int? DaysToExpiry { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Change a batch's status (e.g. Quarantine → Active, or Recalled).</summary>
public class UpdateBatchStatusDto
{
    public string Status { get; set; } = null!;
}
