using Inventory.Application.DTOs;
using Inventory.Application.Services.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Helpers;

namespace Inventory.Infrastructure.Services;

/// <summary>
/// The single place that turns a code into a sellable line. See
/// <see cref="ICatalogResolutionService"/> for why it exists.
///
/// <para><b>Scope.</b> Company-wide, deliberately: a barcode identifies the same product in
/// every store of the company. Per-store price and stock are resolved downstream by Sales
/// and Inventory respectively. Branch/business-unit are NOT part of the predicate — scoping
/// identity to a branch would force a duplicate item row per store for one physical product.</para>
/// </summary>
public class CatalogResolutionService : ICatalogResolutionService
{
    private readonly InventoryDbContext _db;
    private readonly IHttpContextAccessor _http;

    public CatalogResolutionService(InventoryDbContext db, IHttpContextAccessor http)
    {
        _db = db;
        _http = http;
    }

    public async Task<CatalogResolveResultDto> ResolveAsync(
        string code, bool fallbackToSearch = true, CancellationToken ct = default)
    {
        var result = new CatalogResolveResultDto();

        var needle = (code ?? string.Empty).Trim();
        if (needle.Length == 0) return result;

        var companyId = CompanyId();

        // Most specific identifier first. A barcode is an unambiguous scan; an item SKU
        // typed by hand is the weakest of the exact matches.
        var match =
            await MatchItemBarcodeAsync(companyId, needle, ct)
            ?? await MatchVariantAsync(companyId, needle, byBarcode: true, ct)
            ?? await MatchVariantAsync(companyId, needle, byBarcode: false, ct)
            ?? await MatchItemCodeAsync(companyId, needle, ct);

        if (match is not null)
        {
            result.IsExactMatch = true;
            result.Match = match;
            return result;
        }

        // Nothing matched an identifier. For the till, offer name candidates so the
        // cashier can choose; for machine callers, report the miss instead of guessing.
        if (fallbackToSearch)
            result.Candidates = await SearchAsync(needle, 25, ct);

        return result;
    }

    public async Task<List<CatalogResolutionDto>> SearchAsync(
        string query, int limit = 25, CancellationToken ct = default)
    {
        var needle = (query ?? string.Empty).Trim();
        if (needle.Length == 0) return [];

        limit = Math.Clamp(limit, 1, 100);
        var companyId = CompanyId();
        var lowered = needle.ToLower();

        // Lowered Contains rather than EF.Functions.ILike: both become a LIKE with a leading
        // wildcard on PostgreSQL (so neither uses a btree index — a trigram index is the
        // answer if the catalogue grows), but this one also translates on other providers,
        // which keeps the resolver testable. Plain Contains would be case-SENSITIVE on
        // PostgreSQL and would miss "Cola" for "cola".
        var items = await BaseItems(companyId)
            .Where(i => i.Name.ToLower().Contains(lowered)
                     || i.Code.ToLower().Contains(lowered))
            .OrderByDescending(i => i.Code.ToLower().StartsWith(lowered))
            .ThenByDescending(i => i.Name.ToLower().StartsWith(lowered))
            .ThenBy(i => i.Name)
            .Take(limit)
            .ToListAsync(ct);

        var results = new List<CatalogResolutionDto>(items.Count);
        foreach (var item in items)
            results.Add(await BuildAsync(item, variant: null, unitId: item.BaseUnitId,
                                         CatalogMatchType.Name, item.Name, ct));

        // Variants matching by name or SKU, so "red xl" finds the variant, not just the parent.
        if (results.Count < limit)
        {
            var variants = await _db.ItemVariants
                .AsNoTracking()
                .Where(v => !v.IsDeleted && v.CompanyId == companyId
                         && (v.VariantCode.ToLower().Contains(lowered)
                          || v.VariantName.ToLower().Contains(lowered)))
                .OrderBy(v => v.VariantName)
                .Take(limit - results.Count)
                .ToListAsync(ct);

            foreach (var variant in variants)
            {
                var item = await BaseItems(companyId).FirstOrDefaultAsync(i => i.Id == variant.ItemId, ct);
                if (item is null) continue;
                results.Add(await BuildAsync(item, variant, item.BaseUnitId,
                                             CatalogMatchType.Name, variant.VariantName, ct));
            }
        }

        return results;
    }

    // ── Delta sync ────────────────────────────────────────────────────────

    public async Task<CatalogSyncPageDto> GetSyncPageAsync(
        DateTime? since, Guid? sinceId, int pageSize = 500, CancellationToken ct = default)
    {
        pageSize = Math.Clamp(pageSize, 1, 1000);
        var companyId = CompanyId();

        // Soft-deleted rows are INCLUDED here: the till needs the tombstone to drop them.
        var query = _db.Items.AsNoTracking().Where(i => i.CompanyId == companyId);

        if (since is { } watermark)
        {
            // Keyset on (ChangedAt, Id). Ordering on the timestamp alone would skip rows
            // when several share it and a page boundary falls between them.
            query = sinceId is { } lastId
                ? query.Where(i => (i.UpdatedAt ?? i.CreatedAt) > watermark
                                || ((i.UpdatedAt ?? i.CreatedAt) == watermark && i.Id.CompareTo(lastId) > 0))
                : query.Where(i => (i.UpdatedAt ?? i.CreatedAt) > watermark);
        }

        var items = await query
            .OrderBy(i => i.UpdatedAt ?? i.CreatedAt)
            .ThenBy(i => i.Id)
            .Take(pageSize + 1)          // one extra row tells us whether more remain
            .ToListAsync(ct);

        var hasMore = items.Count > pageSize;
        if (hasMore) items.RemoveAt(items.Count - 1);

        var page = new CatalogSyncPageDto
        {
            HasMore = hasMore,
            ServerTime = DateTime.UtcNow,
        };

        if (items.Count == 0) return page;

        var itemIds = items.Select(i => i.Id).ToList();

        var barcodes = await _db.ItemBarcodes.AsNoTracking()
            .Where(b => itemIds.Contains(b.ItemId) && b.IsActive && !b.IsDeleted)
            .ToListAsync(ct);

        var variants = await _db.ItemVariants.AsNoTracking()
            .Where(v => itemIds.Contains(v.ItemId) && !v.IsDeleted)
            .ToListAsync(ct);

        var prices = await _db.ItemPrices.AsNoTracking()
            .Where(p => itemIds.Contains(p.ItemId) && !p.IsDeleted)
            .ToListAsync(ct);

        // One image per item — primary if flagged, else the first by display order.
        var images = await _db.ItemImages.AsNoTracking()
            .Where(img => itemIds.Contains(img.ItemId) && !img.IsDeleted)
            .OrderByDescending(img => img.IsPrimary)
            .ThenBy(img => img.DisplayOrder)
            .ToListAsync(ct);

        // Unit and category names in two queries rather than per row.
        var unitIds = items.Select(i => i.BaseUnitId)
            .Concat(barcodes.Where(b => b.UnitId.HasValue).Select(b => b.UnitId!.Value))
            .Distinct().ToList();
        var unitNames = await _db.Units.AsNoTracking()
            .Where(u => unitIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Name, ct);

        var categoryIds = items.Where(i => i.CategoryId.HasValue)
            .Select(i => i.CategoryId!.Value).Distinct().ToList();
        var categoryNames = await _db.ItemCategories.AsNoTracking()
            .Where(c => categoryIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        foreach (var item in items)
        {
            var changedAt = item.UpdatedAt ?? item.CreatedAt;

            var entry = new CatalogSyncEntryDto
            {
                ItemId = item.Id,
                Code = item.Code,
                Name = item.Name,
                CategoryId = item.CategoryId,
                CategoryName = item.CategoryId is { } cid && categoryNames.TryGetValue(cid, out var cn) ? cn : null,
                ItemType = item.ItemType,
                TrackingType = item.TrackingType,
                BaseUnitId = item.BaseUnitId,
                BaseUnitName = unitNames.TryGetValue(item.BaseUnitId, out var bn) ? bn : null,
                IsActive = item.IsActive,
                // Anything deleted or deactivated is a tombstone — the till drops it.
                IsDeleted = item.IsDeleted || !item.IsActive,
                ChangedAt = changedAt,
                ListPrice = prices.FirstOrDefault(p => p.ItemId == item.Id && p.UnitId == item.BaseUnitId)?.SalePrice,
                ImageUrl = images.FirstOrDefault(img => img.ItemId == item.Id)?.Url,
            };

            // Tombstones carry identity only — no point shipping barcodes for a dead row.
            if (!entry.IsDeleted)
            {
                foreach (var b in barcodes.Where(b => b.ItemId == item.Id))
                {
                    var unitId = b.UnitId ?? item.BaseUnitId;
                    entry.Barcodes.Add(new CatalogSyncBarcodeDto
                    {
                        Barcode = b.Barcode,
                        UnitId = unitId,
                        UnitName = unitNames.TryGetValue(unitId, out var un) ? un : null,
                        // Resolved here so an offline till gets pack quantities right
                        // without shipping the conversion table to every device.
                        QuantityInBaseUnits =
                            await QuantityInBaseUnitsAsync(item.Id, unitId, item.BaseUnitId, ct),
                    });
                }

                foreach (var v in variants.Where(v => v.ItemId == item.Id))
                {
                    entry.Variants.Add(new CatalogSyncVariantDto
                    {
                        Id = v.Id,
                        VariantCode = v.VariantCode,
                        VariantName = v.VariantName,
                        Barcode = v.Barcode,
                    });
                }
            }

            page.Entries.Add(entry);
        }

        var last = page.Entries[^1];
        page.NextSince = last.ChangedAt;
        page.NextSinceId = last.ItemId;

        return page;
    }

    // ── Exact matchers ────────────────────────────────────────────────────

    /// <summary>
    /// Barcode registered against the item. The barcode may carry its own unit — a case of
    /// six has its own EAN — so the resolved unit comes from the barcode, not the item.
    /// </summary>
    private async Task<CatalogResolutionDto?> MatchItemBarcodeAsync(
        Guid companyId, string code, CancellationToken ct)
    {
        var barcode = await _db.ItemBarcodes
            .AsNoTracking()
            .Where(b => b.IsActive && !b.IsDeleted && b.CompanyId == companyId
                     && b.Barcode.ToLower() == code.ToLower())
            .FirstOrDefaultAsync(ct);

        if (barcode is null) return null;

        var item = await BaseItems(companyId).FirstOrDefaultAsync(i => i.Id == barcode.ItemId, ct);
        if (item is null) return null;

        return await BuildAsync(item, variant: null,
                                unitId: barcode.UnitId ?? item.BaseUnitId,
                                CatalogMatchType.ItemBarcode, barcode.Barcode, ct);
    }

    private async Task<CatalogResolutionDto?> MatchVariantAsync(
        Guid companyId, string code, bool byBarcode, CancellationToken ct)
    {
        var query = _db.ItemVariants
            .AsNoTracking()
            .Where(v => !v.IsDeleted && v.CompanyId == companyId);

        query = byBarcode
            ? query.Where(v => v.Barcode != null && v.Barcode.ToLower() == code.ToLower())
            : query.Where(v => v.VariantCode.ToLower() == code.ToLower());

        var variant = await query.FirstOrDefaultAsync(ct);
        if (variant is null) return null;

        var item = await BaseItems(companyId).FirstOrDefaultAsync(i => i.Id == variant.ItemId, ct);
        if (item is null) return null;

        // Variant identifiers always denote a single base unit — packs are modelled as
        // item barcodes, not variants.
        return await BuildAsync(item, variant, item.BaseUnitId,
                                byBarcode ? CatalogMatchType.VariantBarcode : CatalogMatchType.VariantCode,
                                byBarcode ? variant.Barcode : variant.VariantCode, ct);
    }

    private async Task<CatalogResolutionDto?> MatchItemCodeAsync(
        Guid companyId, string code, CancellationToken ct)
    {
        var item = await BaseItems(companyId)
            .FirstOrDefaultAsync(i => i.Code.ToLower() == code.ToLower(), ct);

        return item is null
            ? null
            : await BuildAsync(item, variant: null, item.BaseUnitId,
                               CatalogMatchType.ItemCode, item.Code, ct);
    }

    // ── Assembly ──────────────────────────────────────────────────────────

    private IQueryable<Item> BaseItems(Guid companyId) =>
        _db.Items.AsNoTracking().Where(i => !i.IsDeleted && i.CompanyId == companyId);

    private async Task<CatalogResolutionDto> BuildAsync(
        Item item, ItemVariant? variant, Guid unitId,
        CatalogMatchType matchType, string? matchedValue, CancellationToken ct)
    {
        var quantityInBase = await QuantityInBaseUnitsAsync(item.Id, unitId, item.BaseUnitId, ct);

        var dto = new CatalogResolutionDto
        {
            MatchType = matchType,
            MatchedValue = matchedValue,

            ItemId = item.Id,
            ItemCode = item.Code,
            ItemName = item.Name,
            ItemType = item.ItemType,
            TrackingType = item.TrackingType,
            CategoryId = item.CategoryId,
            IsActive = item.IsActive,

            VariantId = variant?.Id,
            VariantCode = variant?.VariantCode,
            VariantName = variant?.VariantName,

            UnitId = unitId,
            BaseUnitId = item.BaseUnitId,
            QuantityInBaseUnits = quantityInBase,
        };

        dto.UnitName = await UnitNameAsync(unitId, ct);
        dto.BaseUnitName = unitId == item.BaseUnitId ? dto.UnitName : await UnitNameAsync(item.BaseUnitId, ct);

        if (item.CategoryId is { } categoryId)
        {
            dto.CategoryName = await _db.ItemCategories.AsNoTracking()
                .Where(c => c.Id == categoryId).Select(c => c.Name).FirstOrDefaultAsync(ct);
        }

        // Indicative only — Sales pricing stays authoritative at checkout.
        dto.ListPrice = await _db.ItemPrices.AsNoTracking()
            .Where(p => p.ItemId == item.Id && p.UnitId == unitId && !p.IsDeleted)
            .Select(p => (decimal?)p.SalePrice)
            .FirstOrDefaultAsync(ct);

        return dto;
    }

    /// <summary>
    /// How many base units one <paramref name="unitId"/> represents. Conversions are stored
    /// directionally ("1 From = factor To"), so this checks both directions before giving up
    /// and assuming 1:1 — a missing conversion must never silently inflate a sale.
    /// </summary>
    private async Task<decimal> QuantityInBaseUnitsAsync(
        Guid itemId, Guid unitId, Guid baseUnitId, CancellationToken ct)
    {
        if (unitId == baseUnitId) return 1m;

        var conversions = await _db.ItemUomConversions.AsNoTracking()
            .Where(c => c.ItemId == itemId && !c.IsDeleted
                     && ((c.FromUnitId == unitId && c.ToUnitId == baseUnitId)
                      || (c.FromUnitId == baseUnitId && c.ToUnitId == unitId)))
            .ToListAsync(ct);

        var forward = conversions.FirstOrDefault(c => c.FromUnitId == unitId && c.ToUnitId == baseUnitId);
        if (forward is { ConversionFactor: > 0 }) return forward.ConversionFactor;

        var reverse = conversions.FirstOrDefault(c => c.FromUnitId == baseUnitId && c.ToUnitId == unitId);
        if (reverse is { ConversionFactor: > 0 }) return 1m / reverse.ConversionFactor;

        return 1m;
    }

    private Task<string?> UnitNameAsync(Guid unitId, CancellationToken ct) =>
        _db.Units.AsNoTracking().Where(u => u.Id == unitId).Select(u => u.Name).FirstOrDefaultAsync(ct);

    private Guid CompanyId()
    {
        var user = _http.HttpContext?.User
            ?? throw new InvalidOperationException("HTTP context or user not available");

        var (companyId, _, _) = TenantContextHelper.ExtractTenantContext(user);
        return companyId;
    }
}
