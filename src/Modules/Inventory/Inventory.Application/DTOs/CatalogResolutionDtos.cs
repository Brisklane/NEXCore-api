namespace Inventory.Application.DTOs;

/// <summary>
/// Which identifier the scanned/typed code matched. Ordered by how specific the match
/// is — a barcode is an unambiguous scan, a name is a guess.
/// </summary>
public enum CatalogMatchType
{
    /// <summary>Matched <c>ItemBarcode.Barcode</c> — may carry its own unit (a case/pack barcode).</summary>
    ItemBarcode = 0,

    /// <summary>Matched <c>ItemVariant.Barcode</c>.</summary>
    VariantBarcode = 1,

    /// <summary>Matched <c>ItemVariant.VariantCode</c> (the variant SKU).</summary>
    VariantCode = 2,

    /// <summary>Matched <c>Item.Code</c> (the item SKU).</summary>
    ItemCode = 3,

    /// <summary>Fuzzy hit on the name — a suggestion, never a scan result.</summary>
    Name = 4,
}

/// <summary>
/// One resolved sellable line: which item, which variant, in which unit, and how many
/// base units that unit represents.
///
/// <para><b>Why the unit matters.</b> A barcode may be registered against a non-base unit —
/// the EAN on a case of 6 bottles. Selling it must add one case (6 base units), not one
/// bottle. <see cref="QuantityInBaseUnits"/> carries that factor so the caller never has
/// to re-derive it.</para>
/// </summary>
public class CatalogResolutionDto
{
    /// <summary>How the code matched — lets the caller distinguish a scan from a suggestion.</summary>
    public CatalogMatchType MatchType { get; set; }

    /// <summary>The identifier that actually matched, echoed back for receipts/audit.</summary>
    public string? MatchedValue { get; set; }

    // ── Item ──────────────────────────────────────────────────────────────
    public Guid ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string? ItemType { get; set; }
    public string? TrackingType { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }

    // ── Variant (null when the item has none) ─────────────────────────────
    public Guid? VariantId { get; set; }
    public string? VariantCode { get; set; }
    public string? VariantName { get; set; }

    // ── Unit of measure ───────────────────────────────────────────────────
    /// <summary>Unit this code sells in — the barcode's unit when it has one, else the base unit.</summary>
    public Guid UnitId { get; set; }
    public string? UnitName { get; set; }

    /// <summary>The item's base (stock-keeping) unit.</summary>
    public Guid BaseUnitId { get; set; }
    public string? BaseUnitName { get; set; }

    /// <summary>
    /// How many base units one of <see cref="UnitId"/> equals. 1 when the code sells in the
    /// base unit; 6 for a case-of-6 barcode. Multiply the scanned quantity by this before
    /// touching stock.
    /// </summary>
    public decimal QuantityInBaseUnits { get; set; } = 1m;

    // ── Indicative price ──────────────────────────────────────────────────
    /// <summary>
    /// Catalogue list price for <see cref="UnitId"/>, for display only. The Sales pricing
    /// service (price lists, promotions, coupons, tax) remains authoritative at checkout.
    /// </summary>
    public decimal? ListPrice { get; set; }
}

/// <summary>Result of a resolve attempt: either exactly one hit, or candidates to choose from.</summary>
public class CatalogResolveResultDto
{
    /// <summary>True when the code matched an identifier exactly — safe to add to the cart directly.</summary>
    public bool IsExactMatch { get; set; }

    /// <summary>The single exact match, when there is one.</summary>
    public CatalogResolutionDto? Match { get; set; }

    /// <summary>
    /// Populated when the code was ambiguous or only matched loosely — the caller should
    /// present these rather than guessing.
    /// </summary>
    public List<CatalogResolutionDto> Candidates { get; set; } = [];
}
