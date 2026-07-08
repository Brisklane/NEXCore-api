namespace Inventory.Domain.Entities;

/// <summary>
/// A binary file (e.g. an inventory item image) stored directly in SQL Server
/// (table <c>inv.StoredFiles</c>) instead of an external object store.
///
/// Used for local/self-hosted runs that don't have blob storage. The bytes are
/// served back over HTTP by <c>InventoryImageController</c> at
/// <c>/api/inventory/images/{id}</c>, which is the URL persisted on the owning
/// <see cref="ItemImage"/> record.
/// </summary>
public class StoredFile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Original file name (used only for reference / download naming).</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>MIME type, returned as the Content-Type when the file is served.</summary>
    public string ContentType { get; set; } = "application/octet-stream";

    /// <summary>Raw file bytes (varbinary(max)).</summary>
    public byte[] Content { get; set; } = Array.Empty<byte>();

    public long SizeBytes { get; set; }

    // ── Tenant scope ─────────────────────────────────────────────────────────
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public Guid? BusinessUnitId { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
