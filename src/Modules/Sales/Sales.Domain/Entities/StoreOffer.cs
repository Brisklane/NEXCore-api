using Nexcore.SharedKernel;
using Sales.Domain.Enums;

namespace Sales.Domain.Entities;

/// <summary>
/// A customer-facing marketing card shown on the store's home page in the customer app.
///
/// This is a display/marketing concept, separate from <see cref="Promotion"/> which is the
/// pricing engine. An offer can optionally link to a Promotion (to surface the discount details)
/// and/or to a specific item or category (to deep-link into the menu).
/// </summary>
public class StoreOffer : BaseEntity
{
    // ─── Scope ───────────────────────────────────────────────────────────────
    /// <summary>FK to PosStore — the store this offer belongs to.</summary>
    public Guid StoreId { get; set; }

    // ─── Display ─────────────────────────────────────────────────────────────
    public StoreOfferType OfferType { get; set; } = StoreOfferType.Featured;
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    /// <summary>Square/portrait card image shown in the offer grid.</summary>
    public string? ImageUrl { get; set; }
    /// <summary>Wide banner image used for hero/carousel slots.</summary>
    public string? BannerUrl { get; set; }

    /// <summary>Short badge overlaid on the card, e.g. "20% OFF", "NEW", "HOT".</summary>
    public string? BadgeText { get; set; }
    /// <summary>Hex colour for the badge background, e.g. "#E53935".</summary>
    public string? BadgeColor { get; set; }

    /// <summary>Button label shown on the card, e.g. "Order Now", "View Menu".</summary>
    public string? CallToAction { get; set; }

    /// <summary>In-app deep-link or external URL the card navigates to when tapped.</summary>
    public string? DeepLinkUrl { get; set; }

    /// <summary>Lower number = shown first.</summary>
    public int DisplayOrder { get; set; } = 0;

    // ─── Visibility Window ───────────────────────────────────────────────────
    /// <summary>Date the offer starts appearing. Null = show immediately.</summary>
    public DateOnly? StartDate { get; set; }
    /// <summary>Date the offer stops appearing. Null = no expiry.</summary>
    public DateOnly? EndDate { get; set; }
    /// <summary>Daily start time (e.g. HappyHour starts at 17:00). Null = all day.</summary>
    public TimeOnly? StartTime { get; set; }
    /// <summary>Daily end time. Null = all day.</summary>
    public TimeOnly? EndTime { get; set; }

    // ─── Links ───────────────────────────────────────────────────────────────
    /// <summary>Optional link to a Promotion — surfaces the discount details on the card.</summary>
    public Guid? PromotionId { get; set; }
    /// <summary>Optional cross-module FK to Inventory.Item — deep-links into a specific item.</summary>
    public Guid? ItemId { get; set; }
    /// <summary>Optional cross-module FK to Inventory.ItemCategory — deep-links into a category.</summary>
    public Guid? ItemCategoryId { get; set; }

    // ─── Navigation ──────────────────────────────────────────────────────────
    public PosStore? Store { get; set; }
    public Promotion? Promotion { get; set; }
}
