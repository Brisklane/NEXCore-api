using Nexcore.SharedKernel;
using Restaurant.Domain.Enums;

namespace Restaurant.Domain.Entities;

/// <summary>
/// A menu that is live for part of the day. A venue routinely runs several at once — an all-day
/// menu plus a breakfast menu that expires at 11:00 — so the order pad resolves the active set
/// by time rather than showing one fixed catalogue.
/// </summary>
public class MenuCard : BaseEntity
{
    public Guid? OutletId { get; set; }

    public string Name { get; set; } = string.Empty;
    public MenuDaypart Daypart { get; set; } = MenuDaypart.AllDay;

    public TimeSpan? AvailableFrom { get; set; }
    public TimeSpan? AvailableTo { get; set; }

    /// <summary>Comma-separated weekday numbers (0=Sun). Empty means every day.</summary>
    public string? ActiveDays { get; set; }

    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    public int DisplayOrder { get; set; }
    public bool IsDefault { get; set; }

    public ICollection<MenuCategory> Categories { get; set; } = [];
}

/// <summary>A tab on the order pad. Ordering and colour matter — this is a touch UI.</summary>
public class MenuCategory : BaseEntity
{
    public Guid MenuId { get; set; }
    public MenuCard? Menu { get; set; }

    /// <summary>Set for sub-categories (Pizza → Thin crust). Null for a top-level tab.</summary>
    public Guid? ParentCategoryId { get; set; }

    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string? ColorHex { get; set; }
    public string? IconName { get; set; }
    public string? ImageUrl { get; set; }

    /// <summary>Station every item in this category routes to unless the item overrides it.</summary>
    public Guid? DefaultStationId { get; set; }

    public ICollection<MenuItem> Items { get; set; } = [];
}

/// <summary>A sellable dish. Prices live on <see cref="MenuItemPrice"/>, not here — see <see cref="PriceScope"/>.</summary>
public class MenuItem : BaseEntity
{
    public Guid CategoryId { get; set; }
    public MenuCategory? Category { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? ShortName { get; set; }
    public string? ImageUrl { get; set; }
    public int DisplayOrder { get; set; }

    /// <summary>Base price used when no scoped price exists. Always the dine-in reference price.</summary>
    public decimal BasePrice { get; set; }

    /// <summary>Recipe cost snapshot, refreshed when the recipe changes. Drives food-cost %.</summary>
    public decimal StandardCost { get; set; }

    /// <summary>
    /// Tax group in the Accounting tax engine, when the company runs one. Left as a reference
    /// only — the rate actually charged is <see cref="TaxPercent"/>.
    /// </summary>
    public Guid? TaxGroupId { get; set; }

    /// <summary>
    /// Rate charged on this dish, as a percentage. Held on the item rather than resolved from
    /// another module because food service taxes by dish class (hot food, cold food, alcohol,
    /// packaged goods) and a till must be able to compute a total without a cross-module call
    /// on every keystroke. Zero means the outlet's default applies.
    /// </summary>
    public decimal TaxPercent { get; set; }

    /// <summary>Inventory item this dish maps to for stock-tracked (pre-packaged) goods.</summary>
    public Guid? InventoryItemId { get; set; }

    /// <summary>Kitchen station override; falls back to the category, then to routing rules.</summary>
    public Guid? StationId { get; set; }

    public CourseType DefaultCourse { get; set; } = CourseType.Main;

    /// <summary>Minutes the kitchen needs. Drives the KDS colour SLA and the quoted wait.</summary>
    public int PrepTimeMinutes { get; set; } = 10;

    // ── Guest-facing attributes ──────────────────────────────────────────────
    public bool IsVegetarian { get; set; }
    public bool IsVegan { get; set; }
    public bool IsHalal { get; set; }
    public bool IsGlutenFree { get; set; }
    public bool ContainsNuts { get; set; }
    public bool ContainsDairy { get; set; }
    public bool ContainsShellfish { get; set; }
    public SpiceLevel SpiceLevel { get; set; } = SpiceLevel.None;
    public int? Calories { get; set; }
    public string? Allergens { get; set; }

    /// <summary>Licensed item: some venues restrict who may sell it and when.</summary>
    public bool IsAlcohol { get; set; }

    /// <summary>Sold by weight — the till asks for a scale reading instead of a quantity.</summary>
    public bool IsSoldByWeight { get; set; }

    public bool IsOpenPrice { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsCombo { get; set; }

    /// <summary>Denormalised 86 flag so the order pad does not join on every render.</summary>
    public bool IsAvailable { get; set; } = true;

    public string? KitchenNote { get; set; }
    public string? Barcode { get; set; }

    public ICollection<MenuItemVariant> Variants { get; set; } = [];
    public ICollection<MenuItemPrice> Prices { get; set; } = [];
    public ICollection<MenuItemModifierGroup> ModifierGroups { get; set; } = [];
    public ICollection<Recipe> Recipes { get; set; } = [];
}

/// <summary>A size or preparation of an item, each with its own price and recipe.</summary>
public class MenuItemVariant : BaseEntity
{
    public Guid MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }

    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    /// <summary>Absolute price for this variant. Zero means "use the item's base price".</summary>
    public decimal Price { get; set; }

    public decimal StandardCost { get; set; }
    public bool IsDefault { get; set; }
    public string? Barcode { get; set; }
    public Guid? InventoryItemId { get; set; }
    public bool IsAvailable { get; set; } = true;
}

/// <summary>
/// Price of an item for one scope. Delivery carries marketplace commission and takeaway carries
/// packaging, so a single price per item is wrong the moment a venue sells more than one way.
/// </summary>
public class MenuItemPrice : BaseEntity
{
    public Guid MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }

    public Guid? VariantId { get; set; }
    public Guid? OutletId { get; set; }

    public PriceScope Scope { get; set; } = PriceScope.Base;
    public decimal Price { get; set; }
}

/// <summary>
/// A time-boxed price override — happy hour, early bird, weekend surcharge. Evaluated after the
/// scoped price and before any check-level discount.
/// </summary>
public class HappyHourRule : BaseEntity
{
    public Guid? OutletId { get; set; }
    public string Name { get; set; } = string.Empty;

    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    /// <summary>Comma-separated weekday numbers (0=Sun). Empty means every day.</summary>
    public string? ActiveDays { get; set; }

    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }

    public DiscountKind DiscountKind { get; set; } = DiscountKind.Percentage;
    public decimal DiscountValue { get; set; }

    /// <summary>Null applies the rule to everything; otherwise it is limited to one category.</summary>
    public Guid? CategoryId { get; set; }
    public Guid? MenuItemId { get; set; }

    /// <summary>Order types this rule applies to, comma-separated enum values. Empty = all.</summary>
    public string? ApplicableOrderTypes { get; set; }

    public int Priority { get; set; }
}

/// <summary>
/// A reusable set of choices ("Choose your crust", "Add toppings"). Shared across items so a
/// change to the topping list does not have to be repeated on twelve pizzas.
/// </summary>
public class ModifierGroup : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>What the guest is asked, verbatim, on the order pad and QR menu.</summary>
    public string? PromptText { get; set; }

    public ModifierSelectionMode SelectionMode { get; set; } = ModifierSelectionMode.Single;
    public bool IsRequired { get; set; }
    public int MinSelections { get; set; }
    public int MaxSelections { get; set; } = 1;

    /// <summary>How many picks are free before <see cref="Modifier.PriceDelta"/> starts charging.</summary>
    public int FreeSelections { get; set; }

    public int DisplayOrder { get; set; }

    public ICollection<Modifier> Modifiers { get; set; } = [];
}

/// <summary>One option inside a modifier group, with its price delta and its stock impact.</summary>
public class Modifier : BaseEntity
{
    public Guid ModifierGroupId { get; set; }
    public ModifierGroup? ModifierGroup { get; set; }

    public string Name { get; set; } = string.Empty;
    public decimal PriceDelta { get; set; }
    public decimal CostDelta { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsDefault { get; set; }
    public bool IsAvailable { get; set; } = true;

    /// <summary>Ingredient consumed when this modifier is chosen ("extra cheese" burns cheese).</summary>
    public Guid? InventoryItemId { get; set; }
    public decimal ConsumptionQuantity { get; set; }
    public string? ConsumptionUom { get; set; }

    /// <summary>Negative modifiers ("no onions") remove an ingredient instead of adding one.</summary>
    public bool IsRemoval { get; set; }
}

/// <summary>Join row attaching a modifier group to an item, with per-item overrides.</summary>
public class MenuItemModifierGroup : BaseEntity
{
    public Guid MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }

    public Guid ModifierGroupId { get; set; }
    public ModifierGroup? ModifierGroup { get; set; }

    public int DisplayOrder { get; set; }

    /// <summary>Overrides the group's own flag for this item only. Null keeps the group default.</summary>
    public bool? IsRequiredOverride { get; set; }
    public int? MinSelectionsOverride { get; set; }
    public int? MaxSelectionsOverride { get; set; }
}

/// <summary>A meal deal: a bundle sold at one price, some parts fixed and some chosen.</summary>
public class ComboMeal : BaseEntity
{
    public Guid? OutletId { get; set; }
    public Guid? MenuItemId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal Price { get; set; }
    public decimal StandardCost { get; set; }
    public Guid? TaxGroupId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsAvailable { get; set; } = true;

    public ICollection<ComboComponent> Components { get; set; } = [];
}

/// <summary>One slot in a combo — "the burger", "the drink", "choose two sides".</summary>
public class ComboComponent : BaseEntity
{
    public Guid ComboMealId { get; set; }
    public ComboMeal? ComboMeal { get; set; }

    public string Name { get; set; } = string.Empty;
    public ComboComponentMode Mode { get; set; } = ComboComponentMode.Fixed;
    public int Quantity { get; set; } = 1;
    public int MinChoices { get; set; } = 1;
    public int MaxChoices { get; set; } = 1;
    public int DisplayOrder { get; set; }

    public ICollection<ComboComponentOption> Options { get; set; } = [];
}

/// <summary>A dish that can fill a combo slot, and what upgrading to it costs.</summary>
public class ComboComponentOption : BaseEntity
{
    public Guid ComboComponentId { get; set; }
    public ComboComponent? ComboComponent { get; set; }

    public Guid MenuItemId { get; set; }
    public Guid? VariantId { get; set; }

    /// <summary>Extra charged when the guest picks this instead of the default. Usually zero.</summary>
    public decimal UpchargeAmount { get; set; }

    public bool IsDefault { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>
/// The 86 list: an item or variant taken off sale at one outlet, with why and by whom.
/// Kept as its own row rather than a flag on the item so it can be per-outlet and audited.
/// </summary>
public class MenuItemAvailability : BaseEntity
{
    public Guid OutletId { get; set; }
    public Guid MenuItemId { get; set; }
    public Guid? VariantId { get; set; }

    public bool IsAvailable { get; set; }

    /// <summary>Free text ("out of salmon"), shown to waiters so they can tell the guest.</summary>
    public string? Reason { get; set; }

    /// <summary>Set when the item was 86'd automatically because an ingredient hit zero.</summary>
    public bool IsAutomatic { get; set; }

    /// <summary>When it comes back by itself — end of service, or a delivery due tomorrow.</summary>
    public DateTime? AvailableAgainAt { get; set; }

    public DateTime MarkedAt { get; set; } = DateTime.UtcNow;
    public Guid? MarkedByStaffId { get; set; }
}
