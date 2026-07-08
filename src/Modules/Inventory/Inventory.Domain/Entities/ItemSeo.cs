using Nexcore.SharedKernel;

namespace Inventory.Domain.Entities;

/// <summary>
/// Item SEO
/// Search engine optimization metadata for e-commerce portals.
/// Used in Daraz, AliExpress, and any web storefront.
/// </summary>
public class ItemSeo : BaseEntity
{
    public Guid ItemId { get; set; }

    /// <summary>SEO page title (max 70 chars recommended)</summary>
    public string? MetaTitle { get; set; }

    /// <summary>SEO meta description (max 160 chars recommended)</summary>
    public string? MetaDescription { get; set; }

    /// <summary>Comma-separated SEO keywords</summary>
    public string? MetaKeywords { get; set; }

    /// <summary>URL-friendly slug (e.g., "samsung-galaxy-s24-256gb-black")</summary>
    public string? Slug { get; set; }

    /// <summary>
    /// Canonical URL — prevents duplicate content penalties when item
    /// is listed on multiple channels
    /// </summary>
    public string? CanonicalUrl { get; set; }

    // Navigation property
    public Item? Item { get; set; }
}
