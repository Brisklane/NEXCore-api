namespace Sales.Domain.Entities;

/// <summary>
/// A binary file (e.g. a POS receipt-template logo) stored directly in SQL Server
/// (table <c>sales.StoredFiles</c>) instead of an external object store.
///
/// Used for local/self-hosted runs that don't have blob storage. The bytes are
/// served back over HTTP by <c>SalesImageController</c> at
/// <c>/api/sales/images/{id}</c>, which is the URL persisted on the receipt
/// template's <c>LogoUrl</c> field.
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
