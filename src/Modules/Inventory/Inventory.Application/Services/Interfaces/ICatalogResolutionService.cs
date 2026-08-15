using Inventory.Application.DTOs;

namespace Inventory.Application.Services.Interfaces;

/// <summary>
/// Turns a scanned or typed code into a specific sellable line — item, variant, unit and
/// the quantity that unit represents.
///
/// <para>Exists because product identity is spread across four surfaces
/// (<c>Item.Code</c>, <c>ItemBarcode.Barcode</c>, <c>ItemVariant.VariantCode</c>,
/// <c>ItemVariant.Barcode</c>) and every caller that resolves them independently gets it
/// subtly wrong. Callers ask this service instead.</para>
/// </summary>
public interface ICatalogResolutionService
{
    /// <summary>
    /// Resolve a scanned/typed code. Exact identifier matches win, most specific first;
    /// only when nothing matches exactly does it fall back to name candidates.
    /// </summary>
    /// <param name="code">Raw scanner or keyboard input. Trimmed and matched case-insensitively.</param>
    /// <param name="fallbackToSearch">
    /// When true (the till), a miss returns name candidates so the cashier can pick.
    /// When false (automation/imports), a miss returns nothing rather than a guess.
    /// </param>
    Task<CatalogResolveResultDto> ResolveAsync(
        string code, bool fallbackToSearch = true, CancellationToken ct = default);

    /// <summary>
    /// Free-text catalogue search across item names, item SKUs, variant SKUs and barcodes.
    /// Exact identifier hits rank above prefix hits, which rank above contains-hits.
    /// </summary>
    Task<List<CatalogResolutionDto>> SearchAsync(
        string query, int limit = 25, CancellationToken ct = default);

    /// <summary>
    /// A page of catalogue changes since the caller's watermark, for tills that keep a local
    /// index instead of downloading the whole catalogue on every start.
    ///
    /// <para>Returns tombstones as well as upserts, so withdrawn products actually disappear
    /// from tills. Pass <paramref name="since"/> null for a first full sync.</para>
    /// </summary>
    /// <param name="since">Highest <c>ChangedAt</c> the caller already holds.</param>
    /// <param name="sinceId">Item id at that timestamp — disambiguates rows sharing it.</param>
    /// <param name="pageSize">Rows per page (1–1000).</param>
    Task<CatalogSyncPageDto> GetSyncPageAsync(
        DateTime? since, Guid? sinceId, int pageSize = 500, CancellationToken ct = default);
}
