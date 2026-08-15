namespace Inventory.Application.DTOs;

/// <summary>
/// One catalogue entry as a till needs it: enough to search, scan and price offline,
/// and nothing more. Deliberately not <c>ItemDto</c> — a till syncing 20,000 products
/// should not be shipped images, accounting links and supplier records.
/// </summary>
public class CatalogSyncEntryDto
{
    public Guid ItemId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? ItemType { get; set; }
    public string? TrackingType { get; set; }

    public Guid BaseUnitId { get; set; }
    public string? BaseUnitName { get; set; }

    /// <summary>Catalogue list price in the base unit — indicative; Sales pricing rules at checkout.</summary>
    public decimal? ListPrice { get; set; }

    /// <summary>Primary image URL (API-relative), so the till's product grid can render
    /// from the index instead of a second full catalogue download.</summary>
    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; }

    /// <summary>
    /// True when the row should be dropped from the local index — deleted, deactivated, or
    /// no longer sellable. Tombstones are what stop a withdrawn product lingering on tills.
    /// </summary>
    public bool IsDeleted { get; set; }

    /// <summary>Watermark for this row; the client replays from the highest it has seen.</summary>
    public DateTime ChangedAt { get; set; }

    public List<CatalogSyncBarcodeDto> Barcodes { get; set; } = [];
    public List<CatalogSyncVariantDto> Variants { get; set; } = [];
}

public class CatalogSyncBarcodeDto
{
    public string Barcode { get; set; } = string.Empty;
    public Guid? UnitId { get; set; }
    public string? UnitName { get; set; }

    /// <summary>
    /// Base units this barcode represents — 24 for a case-of-24. Resolved server-side so an
    /// offline till gets pack quantities right without shipping the conversion table.
    /// </summary>
    public decimal QuantityInBaseUnits { get; set; } = 1m;
}

public class CatalogSyncVariantDto
{
    public Guid Id { get; set; }
    public string VariantCode { get; set; } = string.Empty;
    public string? VariantName { get; set; }
    public string? Barcode { get; set; }
}

/// <summary>
/// A page of catalogue changes. Paging is keyset-based on (ChangedAt, ItemId): ordering by
/// timestamp alone would silently skip rows when several share a timestamp and the page
/// boundary lands between them.
/// </summary>
public class CatalogSyncPageDto
{
    public List<CatalogSyncEntryDto> Entries { get; set; } = [];

    /// <summary>Pass back as <c>since</c> to continue. Null when fully caught up.</summary>
    public DateTime? NextSince { get; set; }

    /// <summary>Pass back as <c>sinceId</c> alongside <see cref="NextSince"/>.</summary>
    public Guid? NextSinceId { get; set; }

    /// <summary>True while more pages remain.</summary>
    public bool HasMore { get; set; }

    /// <summary>Server clock, so clients can reason about staleness without trusting their own.</summary>
    public DateTime ServerTime { get; set; }
}
