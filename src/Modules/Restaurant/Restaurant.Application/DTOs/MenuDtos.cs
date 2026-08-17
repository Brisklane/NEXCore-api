using Restaurant.Domain.Enums;

namespace Restaurant.Application.DTOs;

// ── Menu card ────────────────────────────────────────────────────────────────

public class MenuCardDto
{
    public Guid Id { get; set; }
    public Guid? OutletId { get; set; }
    public string Name { get; set; } = string.Empty;
    public MenuDaypart Daypart { get; set; }
    public TimeSpan? AvailableFrom { get; set; }
    public TimeSpan? AvailableTo { get; set; }
    public string? ActiveDays { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }

    public int CategoryCount { get; set; }
    public int ItemCount { get; set; }

    /// <summary>True when this menu is live right now, evaluated server-side against the venue clock.</summary>
    public bool IsCurrentlyActive { get; set; }
}

public class SaveMenuCardDto
{
    public Guid? OutletId { get; set; }
    public string Name { get; set; } = string.Empty;
    public MenuDaypart Daypart { get; set; } = MenuDaypart.AllDay;
    public TimeSpan? AvailableFrom { get; set; }
    public TimeSpan? AvailableTo { get; set; }
    public string? ActiveDays { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

// ── Category ─────────────────────────────────────────────────────────────────

public class MenuCategoryDto
{
    public Guid Id { get; set; }
    public Guid MenuId { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string? ColorHex { get; set; }
    public string? IconName { get; set; }
    public string? ImageUrl { get; set; }
    public Guid? DefaultStationId { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public int ItemCount { get; set; }
}

public class SaveMenuCategoryDto
{
    public Guid MenuId { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string? ColorHex { get; set; }
    public string? IconName { get; set; }
    public string? ImageUrl { get; set; }
    public Guid? DefaultStationId { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
}

// ── Item ─────────────────────────────────────────────────────────────────────

public class MenuItemDto
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public Guid CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public Guid? MenuId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ShortName { get; set; }
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }

    public decimal BasePrice { get; set; }
    public decimal StandardCost { get; set; }
    public Guid? TaxGroupId { get; set; }
    public decimal TaxPercent { get; set; }
    public Guid? InventoryItemId { get; set; }
    public Guid? StationId { get; set; }
    public string? StationName { get; set; }
    public CourseType DefaultCourse { get; set; }
    public int PrepTimeMinutes { get; set; }

    public bool IsVegetarian { get; set; }
    public bool IsVegan { get; set; }
    public bool IsHalal { get; set; }
    public bool IsGlutenFree { get; set; }
    public bool ContainsNuts { get; set; }
    public bool ContainsDairy { get; set; }
    public bool ContainsShellfish { get; set; }
    public SpiceLevel SpiceLevel { get; set; }
    public int? Calories { get; set; }
    public string? Allergens { get; set; }
    public bool IsAlcohol { get; set; }
    public bool IsSoldByWeight { get; set; }
    public bool IsOpenPrice { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsCombo { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsActive { get; set; }
    public string? KitchenNote { get; set; }
    public string? Barcode { get; set; }

    /// <summary>Cost as a share of price. The number a restaurant owner actually manages by.</summary>
    public decimal FoodCostPercent { get; set; }
    public decimal ContributionMargin { get; set; }

    /// <summary>Reason the item is 86'd, when it is.</summary>
    public string? UnavailableReason { get; set; }

    public List<MenuItemVariantDto> Variants { get; set; } = [];
    public List<MenuItemPriceDto> Prices { get; set; } = [];
    public List<ItemModifierGroupDto> ModifierGroups { get; set; } = [];
    public bool HasRecipe { get; set; }
}

public class SaveMenuItemDto
{
    public string? Code { get; set; }
    public Guid CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ShortName { get; set; }
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }
    public decimal BasePrice { get; set; }
    public decimal StandardCost { get; set; }
    public Guid? TaxGroupId { get; set; }
    public decimal TaxPercent { get; set; }
    public Guid? InventoryItemId { get; set; }
    public Guid? StationId { get; set; }
    public CourseType DefaultCourse { get; set; } = CourseType.Main;
    public int PrepTimeMinutes { get; set; } = 10;
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
    public bool IsAlcohol { get; set; }
    public bool IsSoldByWeight { get; set; }
    public bool IsOpenPrice { get; set; }
    public bool IsFeatured { get; set; }
    public bool IsActive { get; set; } = true;
    public string? KitchenNote { get; set; }
    public string? Barcode { get; set; }

    public List<MenuItemVariantDto> Variants { get; set; } = [];
    public List<MenuItemPriceDto> Prices { get; set; } = [];

    /// <summary>Modifier group ids attached to this item, in display order.</summary>
    public List<Guid> ModifierGroupIds { get; set; } = [];
}

public class MenuItemVariantDto
{
    public Guid Id { get; set; }
    public Guid MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public decimal Price { get; set; }
    public decimal StandardCost { get; set; }
    public bool IsDefault { get; set; }
    public string? Barcode { get; set; }
    public Guid? InventoryItemId { get; set; }
    public bool IsAvailable { get; set; } = true;
    public bool IsActive { get; set; } = true;
}

public class MenuItemPriceDto
{
    public Guid Id { get; set; }
    public Guid MenuItemId { get; set; }
    public Guid? VariantId { get; set; }
    public Guid? OutletId { get; set; }
    public PriceScope Scope { get; set; }
    public decimal Price { get; set; }
}

// ── Modifiers ────────────────────────────────────────────────────────────────

public class ModifierGroupDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? PromptText { get; set; }
    public ModifierSelectionMode SelectionMode { get; set; }
    public bool IsRequired { get; set; }
    public int MinSelections { get; set; }
    public int MaxSelections { get; set; }
    public int FreeSelections { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public List<ModifierDto> Modifiers { get; set; } = [];

    /// <summary>How many menu items use this group — a delete guard and a usefulness signal.</summary>
    public int UsedByItemCount { get; set; }
}

public class SaveModifierGroupDto
{
    public string Name { get; set; } = string.Empty;
    public string? PromptText { get; set; }
    public ModifierSelectionMode SelectionMode { get; set; } = ModifierSelectionMode.Single;
    public bool IsRequired { get; set; }
    public int MinSelections { get; set; }
    public int MaxSelections { get; set; } = 1;
    public int FreeSelections { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Description { get; set; }
    public List<ModifierDto> Modifiers { get; set; } = [];
}

public class ModifierDto
{
    public Guid Id { get; set; }
    public Guid ModifierGroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal PriceDelta { get; set; }
    public decimal CostDelta { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsDefault { get; set; }
    public bool IsAvailable { get; set; } = true;
    public bool IsRemoval { get; set; }
    public Guid? InventoryItemId { get; set; }
    public decimal ConsumptionQuantity { get; set; }
    public string? ConsumptionUom { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>A modifier group as it applies to one item, with that item's overrides folded in.</summary>
public class ItemModifierGroupDto
{
    public Guid Id { get; set; }
    public Guid ModifierGroupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? PromptText { get; set; }
    public ModifierSelectionMode SelectionMode { get; set; }
    public bool IsRequired { get; set; }
    public int MinSelections { get; set; }
    public int MaxSelections { get; set; }
    public int FreeSelections { get; set; }
    public int DisplayOrder { get; set; }
    public List<ModifierDto> Modifiers { get; set; } = [];
}

// ── Combos ───────────────────────────────────────────────────────────────────

public class ComboMealDto
{
    public Guid Id { get; set; }
    public string? Code { get; set; }
    public Guid? OutletId { get; set; }
    public Guid? MenuItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public decimal StandardCost { get; set; }
    public Guid? TaxGroupId { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsActive { get; set; }
    public List<ComboComponentDto> Components { get; set; } = [];

    /// <summary>Sum of the default components' à-la-carte prices, so the saving is visible.</summary>
    public decimal ALaCarteTotal { get; set; }
    public decimal SavingAmount { get; set; }
}

public class ComboComponentDto
{
    public Guid Id { get; set; }
    public Guid ComboMealId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ComboComponentMode Mode { get; set; }
    public int Quantity { get; set; } = 1;
    public int MinChoices { get; set; } = 1;
    public int MaxChoices { get; set; } = 1;
    public int DisplayOrder { get; set; }
    public List<ComboOptionDto> Options { get; set; } = [];
}

public class ComboOptionDto
{
    public Guid Id { get; set; }
    public Guid ComboComponentId { get; set; }
    public Guid MenuItemId { get; set; }
    public string? MenuItemName { get; set; }
    public Guid? VariantId { get; set; }
    public string? VariantName { get; set; }
    public decimal UpchargeAmount { get; set; }
    public bool IsDefault { get; set; }
    public int DisplayOrder { get; set; }
}

// ── 86 list ──────────────────────────────────────────────────────────────────

public class AvailabilityDto
{
    public Guid Id { get; set; }
    public Guid OutletId { get; set; }
    public Guid MenuItemId { get; set; }
    public string? MenuItemName { get; set; }
    public string? CategoryName { get; set; }
    public Guid? VariantId { get; set; }
    public string? VariantName { get; set; }
    public bool IsAvailable { get; set; }
    public string? Reason { get; set; }
    public bool IsAutomatic { get; set; }
    public DateTime? AvailableAgainAt { get; set; }
    public DateTime MarkedAt { get; set; }
    public Guid? MarkedByStaffId { get; set; }
    public string? MarkedByStaffName { get; set; }
}

public class Set86Dto
{
    public Guid OutletId { get; set; }
    public Guid MenuItemId { get; set; }
    public Guid? VariantId { get; set; }
    public bool IsAvailable { get; set; }
    public string? Reason { get; set; }
    public DateTime? AvailableAgainAt { get; set; }
    public Guid? StaffId { get; set; }
}

// ── Happy hour ───────────────────────────────────────────────────────────────

public class HappyHourRuleDto
{
    public Guid Id { get; set; }
    public Guid? OutletId { get; set; }
    public string Name { get; set; } = string.Empty;
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? ActiveDays { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public DiscountKind DiscountKind { get; set; }
    public decimal DiscountValue { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public Guid? MenuItemId { get; set; }
    public string? MenuItemName { get; set; }
    public string? ApplicableOrderTypes { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
    public bool IsCurrentlyActive { get; set; }
}

// ── Order pad payload ────────────────────────────────────────────────────────

/// <summary>
/// The whole orderable catalogue for one outlet at one moment: active menus, their categories,
/// items with resolved prices, and modifier groups. One call, because a waiter opening the pad
/// cannot wait on a waterfall of requests — and because the price a waiter sees must be the
/// price the server would charge.
/// </summary>
public class OrderPadCatalogDto
{
    public Guid OutletId { get; set; }
    public DateTime ResolvedAt { get; set; }
    public OrderType OrderType { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public bool PricesIncludeTax { get; set; }

    public List<MenuCardDto> Menus { get; set; } = [];
    public List<MenuCategoryDto> Categories { get; set; } = [];
    public List<MenuItemDto> Items { get; set; } = [];
    public List<ModifierGroupDto> ModifierGroups { get; set; } = [];
    public List<ComboMealDto> Combos { get; set; } = [];

    /// <summary>Items unavailable right now, so the pad can grey them without a second call.</summary>
    public List<Guid> UnavailableItemIds { get; set; } = [];
}
