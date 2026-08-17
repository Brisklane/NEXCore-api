using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using Restaurant.Application.DTOs;
using Restaurant.Application.Services.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;
using Restaurant.Infrastructure.Persistence;
using Restaurant.Infrastructure.Services;

namespace Restaurant.Api.Controllers;

/// <summary>Menus, categories, dishes, modifiers, combos, the 86 list and happy hour.</summary>
[Route("api/restaurant/menu")]
public class MenuController(
    IMenuService menu,
    RestaurantDbContext db,
    IRestaurantTenant tenant,
    ILogger<MenuController> logger) : RestaurantControllerBase(logger)
{
    // ── Menus ────────────────────────────────────────────────────────────────

    [HttpGet("menus")]
    public Task<IActionResult> GetMenus([FromQuery] Guid? outletId, [FromQuery] bool activeNowOnly = false)
        => Run(() => menu.GetMenusAsync(outletId, activeNowOnly));

    [HttpPost("menus")]
    public Task<IActionResult> CreateMenu([FromBody] SaveMenuCardDto request) => Run(async () =>
    {
        var card = new MenuCard().StampNew(tenant, UserId);
        ApplyMenu(card, request);
        db.Menus.Add(card);
        await EnsureSingleDefaultAsync(card);
        await db.SaveChangesAsync();
        return RestaurantMapper.ToDto(card);
    }, "Menu created.");

    [HttpPut("menus/{id:guid}")]
    public Task<IActionResult> UpdateMenu(Guid id, [FromBody] SaveMenuCardDto request) => Run(async () =>
    {
        var card = await db.Menus.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == id)
            ?? throw new InvalidOperationException("Menu not found.");

        ApplyMenu(card, request);
        card.StampUpdated(UserId);
        await EnsureSingleDefaultAsync(card);
        await db.SaveChangesAsync();
        return RestaurantMapper.ToDto(card);
    }, "Menu saved.");

    [HttpDelete("menus/{id:guid}")]
    public Task<IActionResult> DeleteMenu(Guid id) => Run(async () =>
    {
        var card = await db.Menus.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == id)
            ?? throw new InvalidOperationException("Menu not found.");

        var categories = await db.MenuCategories.ForTenant(tenant).Where(c => c.MenuId == id).ToListAsync();
        var categoryIds = categories.Select(c => c.Id).ToList();

        var itemCount = await db.MenuItems.ForTenant(tenant).CountAsync(i => categoryIds.Contains(i.CategoryId));
        if (itemCount > 0)
            throw new InvalidOperationException(
                $"This menu still has {itemCount} dish(es). Move or remove them first.");

        foreach (var category in categories) category.StampDeleted(UserId);
        card.StampDeleted(UserId);
        await db.SaveChangesAsync();
    }, "Menu removed.");

    /// <summary>Only one menu can be the default; setting a new one clears the old.</summary>
    private async Task EnsureSingleDefaultAsync(MenuCard card)
    {
        if (!card.IsDefault) return;

        var others = await db.Menus.ForTenant(tenant)
            .Where(m => m.Id != card.Id && m.IsDefault && m.OutletId == card.OutletId)
            .ToListAsync();

        foreach (var other in others)
        {
            other.IsDefault = false;
            other.StampUpdated(UserId);
        }
    }

    private static void ApplyMenu(MenuCard card, SaveMenuCardDto request)
    {
        card.OutletId = request.OutletId;
        card.Name = request.Name;
        card.Daypart = request.Daypart;
        card.AvailableFrom = request.AvailableFrom;
        card.AvailableTo = request.AvailableTo;
        card.ActiveDays = request.ActiveDays;
        card.EffectiveFrom = request.EffectiveFrom;
        card.EffectiveTo = request.EffectiveTo;
        card.DisplayOrder = request.DisplayOrder;
        card.IsDefault = request.IsDefault;
        card.IsActive = request.IsActive;
        card.Description = request.Description;
    }

    // ── Categories ───────────────────────────────────────────────────────────

    [HttpGet("categories")]
    public Task<IActionResult> GetCategories([FromQuery] Guid? menuId) => Run(async () =>
    {
        var categories = await db.MenuCategories.ForTenant(tenant)
            .WhereIf(menuId.HasValue, c => c.MenuId == menuId)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .ToListAsync();

        var counts = await db.MenuItems.ForTenant(tenant)
            .GroupBy(i => i.CategoryId)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToListAsync();

        return categories.Select(c =>
        {
            var dto = RestaurantMapper.ToDto(c);
            dto.ItemCount = counts.FirstOrDefault(x => x.CategoryId == c.Id)?.Count ?? 0;
            return dto;
        }).ToList();
    });

    [HttpPost("categories")]
    public Task<IActionResult> CreateCategory([FromBody] SaveMenuCategoryDto request) => Run(async () =>
    {
        var category = new MenuCategory().StampNew(tenant, UserId);
        ApplyCategory(category, request);
        db.MenuCategories.Add(category);
        await db.SaveChangesAsync();
        return RestaurantMapper.ToDto(category);
    }, "Category added.");

    [HttpPut("categories/{id:guid}")]
    public Task<IActionResult> UpdateCategory(Guid id, [FromBody] SaveMenuCategoryDto request) => Run(async () =>
    {
        var category = await db.MenuCategories.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new InvalidOperationException("Category not found.");

        ApplyCategory(category, request);
        category.StampUpdated(UserId);
        await db.SaveChangesAsync();
        return RestaurantMapper.ToDto(category);
    }, "Category saved.");

    [HttpDelete("categories/{id:guid}")]
    public Task<IActionResult> DeleteCategory(Guid id) => Run(async () =>
    {
        var category = await db.MenuCategories.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new InvalidOperationException("Category not found.");

        var itemCount = await db.MenuItems.ForTenant(tenant).CountAsync(i => i.CategoryId == id);
        if (itemCount > 0)
            throw new InvalidOperationException($"This category still has {itemCount} dish(es).");

        category.StampDeleted(UserId);
        await db.SaveChangesAsync();
    }, "Category removed.");

    private static void ApplyCategory(MenuCategory category, SaveMenuCategoryDto request)
    {
        category.MenuId = request.MenuId;
        category.ParentCategoryId = request.ParentCategoryId;
        category.Name = request.Name;
        category.DisplayOrder = request.DisplayOrder;
        category.ColorHex = request.ColorHex;
        category.IconName = request.IconName;
        category.ImageUrl = request.ImageUrl;
        category.DefaultStationId = request.DefaultStationId;
        category.IsActive = request.IsActive;
        category.Description = request.Description;
    }

    // ── Items ────────────────────────────────────────────────────────────────

    [HttpGet("items")]
    public async Task<IActionResult> GetItems(
        [FromQuery] Guid? menuId, [FromQuery] Guid? categoryId,
        [FromQuery] string? search, [FromQuery] PaginationParams pagination)
    {
        try
        {
            return Ok(await menu.ListItemsAsync(menuId, categoryId, search, pagination));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing menu items");
            return StatusCode(500, new ApiErrorResponse { Message = "Could not load the menu." });
        }
    }

    [HttpGet("items/{id:guid}")]
    public Task<IActionResult> GetItem(Guid id)
        => RunFound(() => menu.GetItemAsync(id), "Dish not found.");

    [HttpPost("items")]
    public Task<IActionResult> CreateItem([FromBody] SaveMenuItemDto request)
        => Run(() => menu.SaveItemAsync(null, request, UserId), "Dish added.");

    [HttpPut("items/{id:guid}")]
    public Task<IActionResult> UpdateItem(Guid id, [FromBody] SaveMenuItemDto request)
        => Run(() => menu.SaveItemAsync(id, request, UserId), "Dish saved.");

    [HttpDelete("items/{id:guid}")]
    public Task<IActionResult> DeleteItem(Guid id)
        => Run(() => menu.DeleteItemAsync(id, UserId), "Dish removed.");

    // ── Modifier groups ──────────────────────────────────────────────────────

    [HttpGet("modifier-groups")]
    public Task<IActionResult> GetModifierGroups() => Run(() => menu.GetModifierGroupsAsync());

    [HttpPost("modifier-groups")]
    public Task<IActionResult> CreateModifierGroup([FromBody] SaveModifierGroupDto request)
        => Run(() => menu.SaveModifierGroupAsync(null, request, UserId), "Modifier group added.");

    [HttpPut("modifier-groups/{id:guid}")]
    public Task<IActionResult> UpdateModifierGroup(Guid id, [FromBody] SaveModifierGroupDto request)
        => Run(() => menu.SaveModifierGroupAsync(id, request, UserId), "Modifier group saved.");

    [HttpDelete("modifier-groups/{id:guid}")]
    public Task<IActionResult> DeleteModifierGroup(Guid id) => Run(async () =>
    {
        var group = await db.ModifierGroups.ForTenant(tenant).FirstOrDefaultAsync(g => g.Id == id)
            ?? throw new InvalidOperationException("Modifier group not found.");

        var usage = await db.MenuItemModifierGroups.ForTenant(tenant).CountAsync(m => m.ModifierGroupId == id);
        if (usage > 0)
            throw new InvalidOperationException(
                $"This group is attached to {usage} dish(es). Detach it before removing it.");

        group.StampDeleted(UserId);
        await db.SaveChangesAsync();
    }, "Modifier group removed.");

    // ── Combos ───────────────────────────────────────────────────────────────

    [HttpGet("combos")]
    public Task<IActionResult> GetCombos([FromQuery] Guid? outletId) => Run(() => menu.GetCombosAsync(outletId));

    [HttpPost("combos")]
    public Task<IActionResult> CreateCombo([FromBody] ComboMealDto request)
        => Run(() => menu.SaveComboAsync(null, request, UserId), "Combo created.");

    [HttpPut("combos/{id:guid}")]
    public Task<IActionResult> UpdateCombo(Guid id, [FromBody] ComboMealDto request)
        => Run(() => menu.SaveComboAsync(id, request, UserId), "Combo saved.");

    [HttpDelete("combos/{id:guid}")]
    public Task<IActionResult> DeleteCombo(Guid id) => Run(async () =>
    {
        var combo = await db.Combos.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new InvalidOperationException("Combo not found.");

        combo.StampDeleted(UserId);
        await db.SaveChangesAsync();
    }, "Combo removed.");

    // ── 86 list ──────────────────────────────────────────────────────────────

    [HttpGet("availability/{outletId:guid}")]
    public Task<IActionResult> Get86(Guid outletId, [FromQuery] bool unavailableOnly = true)
        => Run(() => menu.Get86ListAsync(outletId, unavailableOnly));

    [HttpPost("availability")]
    public Task<IActionResult> Set86([FromBody] Set86Dto request)
        => Run(() => menu.Set86Async(request, UserId),
               request.IsAvailable ? "Back on the menu." : "Taken off the menu.");

    // ── Happy hour ───────────────────────────────────────────────────────────

    [HttpGet("happy-hours")]
    public Task<IActionResult> GetHappyHours([FromQuery] Guid? outletId) => Run(async () =>
    {
        var now = DateTime.UtcNow;

        var rules = await db.HappyHourRules.ForTenant(tenant)
            .WhereIf(outletId.HasValue, r => r.OutletId == null || r.OutletId == outletId)
            .OrderBy(r => r.Priority).ThenBy(r => r.Name)
            .ToListAsync();

        var categoryNames = await db.MenuCategories.ForTenant(tenant).ToDictionaryAsync(c => c.Id, c => c.Name);
        var itemNames = await db.MenuItems.ForTenant(tenant).ToDictionaryAsync(i => i.Id, i => i.Name);

        return rules.Select(r =>
        {
            var dto = RestaurantMapper.ToDto(r);
            if (r.CategoryId.HasValue) dto.CategoryName = categoryNames.GetValueOrDefault(r.CategoryId.Value);
            if (r.MenuItemId.HasValue) dto.MenuItemName = itemNames.GetValueOrDefault(r.MenuItemId.Value);

            var t = now.TimeOfDay;
            var inWindow = r.StartTime <= r.EndTime
                ? t >= r.StartTime && t <= r.EndTime
                : t >= r.StartTime || t <= r.EndTime;

            var onDay = string.IsNullOrWhiteSpace(r.ActiveDays)
                || r.ActiveDays.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Contains(((int)now.DayOfWeek).ToString());

            dto.IsCurrentlyActive = r.IsActive && inWindow && onDay;
            return dto;
        }).ToList();
    });

    [HttpPost("happy-hours")]
    public Task<IActionResult> CreateHappyHour([FromBody] HappyHourRuleDto request) => Run(async () =>
    {
        var rule = new HappyHourRule().StampNew(tenant, UserId);
        ApplyHappyHour(rule, request);
        db.HappyHourRules.Add(rule);
        await db.SaveChangesAsync();
        return RestaurantMapper.ToDto(rule);
    }, "Happy hour created.");

    [HttpPut("happy-hours/{id:guid}")]
    public Task<IActionResult> UpdateHappyHour(Guid id, [FromBody] HappyHourRuleDto request) => Run(async () =>
    {
        var rule = await db.HappyHourRules.ForTenant(tenant).FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new InvalidOperationException("Happy hour rule not found.");

        ApplyHappyHour(rule, request);
        rule.StampUpdated(UserId);
        await db.SaveChangesAsync();
        return RestaurantMapper.ToDto(rule);
    }, "Happy hour saved.");

    [HttpDelete("happy-hours/{id:guid}")]
    public Task<IActionResult> DeleteHappyHour(Guid id) => Run(async () =>
    {
        var rule = await db.HappyHourRules.ForTenant(tenant).FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new InvalidOperationException("Happy hour rule not found.");

        rule.StampDeleted(UserId);
        await db.SaveChangesAsync();
    }, "Happy hour removed.");

    private static void ApplyHappyHour(HappyHourRule rule, HappyHourRuleDto request)
    {
        rule.OutletId = request.OutletId;
        rule.Name = request.Name;
        rule.StartTime = request.StartTime;
        rule.EndTime = request.EndTime;
        rule.ActiveDays = request.ActiveDays;
        rule.EffectiveFrom = request.EffectiveFrom;
        rule.EffectiveTo = request.EffectiveTo;
        rule.DiscountKind = request.DiscountKind;
        rule.DiscountValue = request.DiscountValue;
        rule.CategoryId = request.CategoryId;
        rule.MenuItemId = request.MenuItemId;
        rule.ApplicableOrderTypes = request.ApplicableOrderTypes;
        rule.Priority = request.Priority;
        rule.IsActive = request.IsActive;
    }

    // ── Order pad ────────────────────────────────────────────────────────────

    /// <summary>
    /// The whole orderable catalogue for one outlet, priced for one order type. This is what the
    /// waiter's screen loads; nothing else on the order path needs a second menu call.
    /// </summary>
    [HttpGet("catalog/{outletId:guid}")]
    public Task<IActionResult> GetCatalog(Guid outletId, [FromQuery] OrderType orderType = OrderType.DineIn)
        => Run(() => ((MenuService)menu).BuildCatalogAsync(outletId, orderType));
}
