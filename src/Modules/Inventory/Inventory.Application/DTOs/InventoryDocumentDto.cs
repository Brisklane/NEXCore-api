namespace Inventory.Application.DTOs;

/// <summary>
/// Inventory Document DTO
/// </summary>
public class InventoryDocumentDto
{
    public Guid Id { get; set; }
    public string DocumentNumber { get; set; } = null!;
    public string DocumentType { get; set; } = null!;
    public DateTime DocumentDate { get; set; }
    public string Status { get; set; } = null!;
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? Description { get; set; }
    public Guid? FromWarehouseId { get; set; }
    public Guid? ToWarehouseId { get; set; }
    public DateTime? PostingDate { get; set; }
    public Guid? PostedByUserId { get; set; }
    public decimal TotalQuantity { get; set; }
    public decimal TotalCost { get; set; }
    public List<InventoryDocumentLineDto> Lines { get; set; } = new();
}

/// <summary>
/// Inventory Document Line DTO
/// </summary>
public class InventoryDocumentLineDto
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public Guid ItemId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? BinId { get; set; }
    public Guid? VariantId { get; set; }
    public decimal Quantity { get; set; }
    public Guid UnitId { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public int LineNumber { get; set; }
    public string? Description { get; set; }
    public Guid? ReferenceLineId { get; set; }

    // Lot / serial capture
    public string? BatchNumber { get; set; }
    public DateTime? ManufactureDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public List<DocumentLineSerialDto> Serials { get; set; } = new();
}

/// <summary>A serialized unit captured on a document line (serial-tracked items).</summary>
public class DocumentLineSerialDto
{
    public Guid Id { get; set; }
    public string SerialNumber { get; set; } = null!;
    public string? Imei { get; set; }
    public string? Imei2 { get; set; }
    public string? MacAddress { get; set; }
}

/// <summary>
/// Create Inventory Document DTO
/// </summary>
public class CreateInventoryDocumentDto
{
    public string DocumentType { get; set; } = null!;
    public DateTime DocumentDate { get; set; }
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? Description { get; set; }
    public Guid? FromWarehouseId { get; set; }
    public Guid? ToWarehouseId { get; set; }
    public List<CreateInventoryDocumentLineDto> Lines { get; set; } = new();
}

/// <summary>
/// Create Inventory Document Line DTO
/// </summary>
public class CreateInventoryDocumentLineDto
{
    public Guid ItemId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? BinId { get; set; }
    public Guid? VariantId { get; set; }
    public decimal Quantity { get; set; }
    public Guid UnitId { get; set; }
    public decimal UnitCost { get; set; }
    public int LineNumber { get; set; }
    public string? Description { get; set; }
    public Guid? ReferenceLineId { get; set; }

    // Lot / serial capture (serial-tracked → provide Serials; lot-tracked → provide BatchNumber + dates)
    public string? BatchNumber { get; set; }
    public DateTime? ManufactureDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public List<DocumentLineSerialDto> Serials { get; set; } = new();
}

/// <summary>
/// Post Inventory Document DTO (to finalize document)
/// </summary>
public class PostInventoryDocumentDto
{
    public Guid DocumentId { get; set; }
    public DateTime PostingDate { get; set; }
    public Guid? PostedByUserId { get; set; }
}

/// <summary>
/// Quick adjust DTO — sets an item's quantity in a warehouse to a target value.
/// The backend calculates the delta, creates an Adjustment document, and posts it atomically.
/// </summary>
public class QuickAdjustDto
{
    public Guid ItemId { get; set; }
    public Guid WarehouseId { get; set; }
    /// <summary>Variant (Color/Size SKU) to adjust. Null = item-level stock.</summary>
    public Guid? VariantId { get; set; }
    public decimal NewQuantity { get; set; }
    public decimal? UnitCost { get; set; }
    public string? Reason { get; set; }
}
