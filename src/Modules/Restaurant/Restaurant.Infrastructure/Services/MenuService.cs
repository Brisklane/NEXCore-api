using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using Restaurant.Application.DTOs;
using Restaurant.Application.Services.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Infrastructure.Services;

/// <summary>
/// Menu maintenance, and — more importantly — menu *resolution*: which menus are live right now
/// and what a guest is actually charged.
///
/// Pricing is server-side and only server-side. The order pad shows what
/// <see cref="ResolvePriceAsync"/> returns and the order service prices lines with the same call,
/// so a stale tablet cannot undercharge and a happy-hour rule cannot apply on one till and not
/// another.
/// </summary>
public class MenuService(RestaurantDbContext db, IRestaurantTenant tenant) : IMenuService
{
    // ── Menus ────────────────────────────────────────────────────────────────

    public async Task<List<MenuCardDto>> GetMenusAsync(Guid? outletId, bool activeNowOnly)
    {
        var now = DateTime.UtcNow;

        var menus = await db.Menus.ForTenant(tenant)
            .WhereIf(outletId.HasValue, m => m.OutletId == null || m.OutletId == outletId)
            .OrderBy(m => m.DisplayOrder).ThenBy(m => m.Name)
            .ToListAsync();

        var categoryCounts = await db.MenuCategories.ForTenant(tenant)
            .GroupBy(c => c.MenuId)
            .Select(g => new { MenuId = g.Key, Count = g.Count() })
            .ToListAsync();

        var itemCounts = await db.MenuItems.ForTenant(tenant)
            .Join(db.MenuCategories.ForTenant(tenant), i => i.CategoryId, c => c.Id, (i, c) => c.MenuId)
            .GroupBy(menuId => menuId)
            .Select(g => new { MenuId = g.Key, Count = g.Count() })
            .ToListAsync();

        var result = menus.Select(m =>
        {
            var dto = RestaurantMapper.ToDto(m);
            dto.CategoryCount = categoryCounts.FirstOrDefault(c => c.MenuId == m.Id)?.Count ?? 0;
            dto.ItemCount = itemCounts.FirstOrDefault(c => c.MenuId == m.Id)?.Count ?? 0;
            dto.IsCurrentlyActive = IsMenuActive(m, now);
            return dto;
        }).ToList();

        return activeNowOnly ? result.Where(m => m.IsCurrentlyActive).ToList() : result;
    }

    /// <summary>
    /// A menu is live when it is enabled, inside its effective dates, on an active weekday and
    /// inside its time window. A window that wraps midnight (22:00–02:00, the late-night menu)
    /// is handled explicitly — the naive "from &lt;= now &lt;= to" test says a late-night menu is
    /// never on, which is exactly when a venue most needs it.
    /// </summary>
    internal static bool IsMenuActive(MenuCard m, DateTime now)
    {
        if (!m.IsActive) return false;
        if (m.EffectiveFrom.HasValue && now < m.EffectiveFrom.Value) return false;
        if (m.EffectiveTo.HasValue && now > m.EffectiveTo.Value) return false;

        if (!string.IsNullOrWhiteSpace(m.ActiveDays))
        {
            var days = m.ActiveDays.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (days.Length > 0 && !days.Contains(((int)now.DayOfWeek).ToString()))
                return false;
        }

        if (m.AvailableFrom is null || m.AvailableTo is null) return true;

        var t = now.TimeOfDay;
        return m.AvailableFrom <= m.AvailableTo
            ? t >= m.AvailableFrom && t <= m.AvailableTo
            : t >= m.AvailableFrom || t <= m.AvailableTo;   // window wraps midnight
    }

    // ── Items ────────────────────────────────────────────────────────────────

    public async Task<MenuItemDto?> GetItemAsync(Guid itemId)
    {
        var item = await db.MenuItems.ForTenant(tenant)
            .Include(i => i.Variants)
            .Include(i => i.Prices)
            .FirstOrDefaultAsync(i => i.Id == itemId);

        if (item is null) return null;

        var dto = RestaurantMapper.ToDto(item);
        dto.CategoryName = await db.MenuCategories.ForTenant(tenant)
            .Where(c => c.Id == item.CategoryId).Select(c => c.Name).FirstOrDefaultAsync();
        dto.MenuId = await db.MenuCategories.ForTenant(tenant)
            .Where(c => c.Id == item.CategoryId).Select(c => (Guid?)c.MenuId).FirstOrDefaultAsync();
        dto.ModifierGroups = await LoadItemModifierGroupsAsync([itemId]) is var map && map.TryGetValue(itemId, out var groups)
            ? groups : [];
        dto.HasRecipe = await db.Recipes.ForTenant(tenant).AnyAsync(r => r.MenuItemId == itemId);
        return dto;
    }

    public async Task<PaginatedResponse<MenuItemDto>> ListItemsAsync(
        Guid? menuId, Guid? categoryId, string? search, PaginationParams pagination)
    {
        var categories = db.MenuCategories.ForTenant(tenant);

        var query = db.MenuItems.ForTenant(tenant)
            .WhereIf(categoryId.HasValue, i => i.CategoryId == categoryId)
            .WhereIf(!string.IsNullOrWhiteSpace(search),
                i => i.Name.ToLower().Contains(search!.ToLower()) ||
                     (i.Code != null && i.Code.ToLower().Contains(search!.ToLower())));

        if (menuId.HasValue)
        {
            var menuCategoryIds = categories.Where(c => c.MenuId == menuId).Select(c => c.Id);
            query = query.Where(i => menuCategoryIds.Contains(i.CategoryId));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderBy(i => i.DisplayOrder).ThenBy(i => i.Name)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Include(i => i.Variants)
            .Include(i => i.Prices)
            .ToListAsync();

        var categoryNames = await categories
            .Where(c => items.Select(i => i.CategoryId).Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name);

        var stationNames = await db.Stations.ForTenant(tenant)
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        var withRecipes = await db.Recipes.ForTenant(tenant)
            .Where(r => r.MenuItemId != null)
            .Select(r => r.MenuItemId!.Value).Distinct().ToListAsync();

        var dtos = items.Select(i =>
        {
            var dto = RestaurantMapper.ToDto(i);
            dto.CategoryName = categoryNames.GetValueOrDefault(i.CategoryId);
            if (i.StationId.HasValue) dto.StationName = stationNames.GetValueOrDefault(i.StationId.Value);
            dto.HasRecipe = withRecipes.Contains(i.Id);
            return dto;
        }).ToList();

        return PaginatedResponse<MenuItemDto>.Ok(dtos, total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<MenuItemDto> SaveItemAsync(Guid? id, SaveMenuItemDto request, Guid userId)
    {
        MenuItem item;

        if (id.HasValue)
        {
            item = await db.MenuItems.ForTenant(tenant)
                .Include(i => i.Variants).Include(i => i.Prices).Include(i => i.ModifierGroups)
                .FirstOrDefaultAsync(i => i.Id == id)
                ?? throw new InvalidOperationException("Menu item not found.");
            item.StampUpdated(userId);
        }
        else
        {
            item = new MenuItem().StampNew(tenant, userId);
            db.MenuItems.Add(item);
        }

        item.Code = request.Code;
        item.CategoryId = request.CategoryId;
        item.Name = request.Name;
        item.ShortName = request.ShortName;
        item.ImageUrl = request.ImageUrl;
        item.Description = request.Description;
        item.DisplayOrder = request.DisplayOrder;
        item.BasePrice = request.BasePrice;
        item.StandardCost = request.StandardCost;
        item.TaxGroupId = request.TaxGroupId;
        item.InventoryItemId = request.InventoryItemId;
        item.StationId = request.StationId;
        item.DefaultCourse = request.DefaultCourse;
        item.PrepTimeMinutes = request.PrepTimeMinutes;
        item.IsVegetarian = request.IsVegetarian;
        item.IsVegan = request.IsVegan;
        item.IsHalal = request.IsHalal;
        item.IsGlutenFree = request.IsGlutenFree;
        item.ContainsNuts = request.ContainsNuts;
        item.ContainsDairy = request.ContainsDairy;
        item.ContainsShellfish = request.ContainsShellfish;
        item.SpiceLevel = request.SpiceLevel;
        item.Calories = request.Calories;
        item.Allergens = request.Allergens;
        item.IsAlcohol = request.IsAlcohol;
        item.IsSoldByWeight = request.IsSoldByWeight;
        item.IsOpenPrice = request.IsOpenPrice;
        item.IsFeatured = request.IsFeatured;
        item.IsActive = request.IsActive;
        item.KitchenNote = request.KitchenNote;
        item.Barcode = request.Barcode;

        SyncVariants(item, request.Variants, userId);
        SyncPrices(item, request.Prices, userId);
        await SyncModifierGroupsAsync(item, request.ModifierGroupIds, userId);

        await db.SaveChangesAsync();
        return await GetItemAsync(item.Id) ?? RestaurantMapper.ToDto(item);
    }

    private void SyncVariants(MenuItem item, List<MenuItemVariantDto> requested, Guid userId)
    {
        var existing = item.Variants.Where(v => !v.IsDeleted).ToList();

        foreach (var gone in existing.Where(v => requested.All(r => r.Id != v.Id)))
            gone.StampDeleted(userId);

        foreach (var dto in requested)
        {
            var variant = existing.FirstOrDefault(v => v.Id == dto.Id);
            if (variant is null)
            {
                variant = new MenuItemVariant { MenuItemId = item.Id }.StampNew(tenant, userId);
                item.Variants.Add(variant);
            }
            else variant.StampUpdated(userId);

            variant.Name = dto.Name;
            variant.DisplayOrder = dto.DisplayOrder;
            variant.Price = dto.Price;
            variant.StandardCost = dto.StandardCost;
            variant.IsDefault = dto.IsDefault;
            variant.Barcode = dto.Barcode;
            variant.InventoryItemId = dto.InventoryItemId;
            variant.IsAvailable = dto.IsAvailable;
            variant.IsActive = dto.IsActive;
        }
    }

    private void SyncPrices(MenuItem item, List<MenuItemPriceDto> requested, Guid userId)
    {
        var existing = item.Prices.Where(p => !p.IsDeleted).ToList();

        foreach (var gone in existing.Where(p => requested.All(r => r.Id != p.Id)))
            gone.StampDeleted(userId);

        foreach (var dto in requested)
        {
            var price = existing.FirstOrDefault(p => p.Id == dto.Id);
            if (price is null)
            {
                price = new MenuItemPrice { MenuItemId = item.Id }.StampNew(tenant, userId);
                item.Prices.Add(price);
            }
            else price.StampUpdated(userId);

            price.VariantId = dto.VariantId;
            price.OutletId = dto.OutletId;
            price.Scope = dto.Scope;
            price.Price = dto.Price;
        }
    }

    private async Task SyncModifierGroupsAsync(MenuItem item, List<Guid> groupIds, Guid userId)
    {
        var existing = await db.MenuItemModifierGroups.ForTenant(tenant)
            .Where(m => m.MenuItemId == item.Id).ToListAsync();

        foreach (var gone in existing.Where(m => !groupIds.Contains(m.ModifierGroupId)))
            gone.StampDeleted(userId);

        for (var i = 0; i < groupIds.Count; i++)
        {
            var link = existing.FirstOrDefault(m => m.ModifierGroupId == groupIds[i] && !m.IsDeleted);
            if (link is null)
            {
                db.MenuItemModifierGroups.Add(new MenuItemModifierGroup
                {
                    MenuItemId = item.Id,
                    ModifierGroupId = groupIds[i],
                    DisplayOrder = i,
                }.StampNew(tenant, userId));
            }
            else
            {
                link.DisplayOrder = i;
                link.StampUpdated(userId);
            }
        }
    }

    public async Task DeleteItemAsync(Guid itemId, Guid userId)
    {
        var item = await db.MenuItems.ForTenant(tenant).FirstOrDefaultAsync(i => i.Id == itemId)
            ?? throw new InvalidOperationException("Menu item not found.");

        // An item that has been sold is history: soft-delete keeps every past order line readable.
        item.StampDeleted(userId);
        await db.SaveChangesAsync();
    }

    // ── Modifier groups ──────────────────────────────────────────────────────

    public async Task<List<ModifierGroupDto>> GetModifierGroupsAsync()
    {
        var groups = await db.ModifierGroups.ForTenant(tenant)
            .Include(g => g.Modifiers)
            .OrderBy(g => g.DisplayOrder).ThenBy(g => g.Name)
            .ToListAsync();

        var usage = await db.MenuItemModifierGroups.ForTenant(tenant)
            .GroupBy(m => m.ModifierGroupId)
            .Select(g => new { GroupId = g.Key, Count = g.Count() })
            .ToListAsync();

        return groups.Select(g =>
        {
            var dto = RestaurantMapper.ToDto(g);
            dto.UsedByItemCount = usage.FirstOrDefault(u => u.GroupId == g.Id)?.Count ?? 0;
            return dto;
        }).ToList();
    }

    public async Task<ModifierGroupDto> SaveModifierGroupAsync(Guid? id, SaveModifierGroupDto request, Guid userId)
    {
        ModifierGroup group;

        if (id.HasValue)
        {
            group = await db.ModifierGroups.ForTenant(tenant)
                .Include(g => g.Modifiers).FirstOrDefaultAsync(g => g.Id == id)
                ?? throw new InvalidOperationException("Modifier group not found.");
            group.StampUpdated(userId);
        }
        else
        {
            group = new ModifierGroup().StampNew(tenant, userId);
            db.ModifierGroups.Add(group);
        }

        group.Name = request.Name;
        group.PromptText = request.PromptText;
        group.SelectionMode = request.SelectionMode;
        group.IsRequired = request.IsRequired;
        group.MinSelections = request.MinSelections;
        group.MaxSelections = request.SelectionMode == ModifierSelectionMode.Single ? 1 : request.MaxSelections;
        group.FreeSelections = request.FreeSelections;
        group.DisplayOrder = request.DisplayOrder;
        group.IsActive = request.IsActive;
        group.Description = request.Description;

        var existing = group.Modifiers.Where(m => !m.IsDeleted).ToList();
        foreach (var gone in existing.Where(m => request.Modifiers.All(r => r.Id != m.Id)))
            gone.StampDeleted(userId);

        foreach (var dto in request.Modifiers)
        {
            var modifier = existing.FirstOrDefault(m => m.Id == dto.Id);
            if (modifier is null)
            {
                modifier = new Modifier { ModifierGroupId = group.Id }.StampNew(tenant, userId);
                group.Modifiers.Add(modifier);
            }
            else modifier.StampUpdated(userId);

            modifier.Name = dto.Name;
            modifier.PriceDelta = dto.PriceDelta;
            modifier.CostDelta = dto.CostDelta;
            modifier.DisplayOrder = dto.DisplayOrder;
            modifier.IsDefault = dto.IsDefault;
            modifier.IsAvailable = dto.IsAvailable;
            modifier.IsRemoval = dto.IsRemoval;
            modifier.InventoryItemId = dto.InventoryItemId;
            modifier.ConsumptionQuantity = dto.ConsumptionQuantity;
            modifier.ConsumptionUom = dto.ConsumptionUom;
            modifier.IsActive = dto.IsActive;
        }

        await db.SaveChangesAsync();
        return RestaurantMapper.ToDto(group);
    }

    /// <summary>Modifier groups per item, with the item's own overrides folded in.</summary>
    private async Task<Dictionary<Guid, List<ItemModifierGroupDto>>> LoadItemModifierGroupsAsync(List<Guid> itemIds)
    {
        var links = await db.MenuItemModifierGroups.ForTenant(tenant)
            .Where(l => itemIds.Contains(l.MenuItemId))
            .ToListAsync();

        if (links.Count == 0) return [];

        var groupIds = links.Select(l => l.ModifierGroupId).Distinct().ToList();
        var groups = await db.ModifierGroups.ForTenant(tenant)
            .Where(g => groupIds.Contains(g.Id))
            .Include(g => g.Modifiers)
            .ToListAsync();

        var byId = groups.ToDictionary(g => g.Id);

        return links
            .Where(l => byId.ContainsKey(l.ModifierGroupId))
            .GroupBy(l => l.MenuItemId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(l => l.DisplayOrder).Select(l =>
                {
                    var group = byId[l.ModifierGroupId];
                    return new ItemModifierGroupDto
                    {
                        Id = l.Id,
                        ModifierGroupId = group.Id,
                        Name = group.Name,
                        PromptText = group.PromptText,
                        SelectionMode = group.SelectionMode,
                        IsRequired = l.IsRequiredOverride ?? group.IsRequired,
                        MinSelections = l.MinSelectionsOverride ?? group.MinSelections,
                        MaxSelections = l.MaxSelectionsOverride ?? group.MaxSelections,
                        FreeSelections = group.FreeSelections,
                        DisplayOrder = l.DisplayOrder,
                        Modifiers = group.Modifiers.Where(m => !m.IsDeleted && m.IsActive)
                            .OrderBy(m => m.DisplayOrder).Select(RestaurantMapper.ToDto).ToList(),
                    };
                }).ToList());
    }

    // ── Combos ───────────────────────────────────────────────────────────────

    public async Task<List<ComboMealDto>> GetCombosAsync(Guid? outletId)
    {
        var combos = await db.Combos.ForTenant(tenant)
            .WhereIf(outletId.HasValue, c => c.OutletId == null || c.OutletId == outletId)
            .Include(c => c.Components).ThenInclude(c => c.Options)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .ToListAsync();

        var itemIds = combos.SelectMany(c => c.Components).SelectMany(c => c.Options)
            .Select(o => o.MenuItemId).Distinct().ToList();

        var items = await db.MenuItems.ForTenant(tenant)
            .Where(i => itemIds.Contains(i.Id))
            .Select(i => new { i.Id, i.Name, i.BasePrice })
            .ToListAsync();

        return combos.Select(c =>
        {
            var dto = RestaurantMapper.ToDto(c);
            foreach (var component in dto.Components)
                foreach (var option in component.Options)
                    option.MenuItemName = items.FirstOrDefault(i => i.Id == option.MenuItemId)?.Name;

            // What the same food would cost bought separately — the saving is the selling point.
            dto.ALaCarteTotal = dto.Components.Sum(component =>
            {
                var chosen = component.Options.FirstOrDefault(o => o.IsDefault) ?? component.Options.FirstOrDefault();
                var price = chosen is null ? 0m : items.FirstOrDefault(i => i.Id == chosen.MenuItemId)?.BasePrice ?? 0m;
                return price * component.Quantity;
            });
            dto.SavingAmount = Math.Max(0, dto.ALaCarteTotal - dto.Price);
            return dto;
        }).ToList();
    }

    public async Task<ComboMealDto> SaveComboAsync(Guid? id, ComboMealDto request, Guid userId)
    {
        ComboMeal combo;

        if (id.HasValue)
        {
            combo = await db.Combos.ForTenant(tenant)
                .Include(c => c.Components).ThenInclude(c => c.Options)
                .FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new InvalidOperationException("Combo not found.");
            combo.StampUpdated(userId);
        }
        else
        {
            combo = new ComboMeal().StampNew(tenant, userId);
            db.Combos.Add(combo);
        }

        combo.Code = request.Code;
        combo.OutletId = request.OutletId;
        combo.MenuItemId = request.MenuItemId;
        combo.Name = request.Name;
        combo.ImageUrl = request.ImageUrl;
        combo.Description = request.Description;
        combo.Price = request.Price;
        combo.StandardCost = request.StandardCost;
        combo.TaxGroupId = request.TaxGroupId;
        combo.DisplayOrder = request.DisplayOrder;
        combo.IsAvailable = request.IsAvailable;
        combo.IsActive = request.IsActive;

        var existingComponents = combo.Components.Where(c => !c.IsDeleted).ToList();
        foreach (var gone in existingComponents.Where(c => request.Components.All(r => r.Id != c.Id)))
            gone.StampDeleted(userId);

        foreach (var dto in request.Components)
        {
            var component = existingComponents.FirstOrDefault(c => c.Id == dto.Id);
            if (component is null)
            {
                component = new ComboComponent { ComboMealId = combo.Id }.StampNew(tenant, userId);
                combo.Components.Add(component);
            }
            else component.StampUpdated(userId);

            component.Name = dto.Name;
            component.Mode = dto.Mode;
            component.Quantity = dto.Quantity;
            component.MinChoices = dto.MinChoices;
            component.MaxChoices = dto.MaxChoices;
            component.DisplayOrder = dto.DisplayOrder;

            var existingOptions = component.Options.Where(o => !o.IsDeleted).ToList();
            foreach (var gone in existingOptions.Where(o => dto.Options.All(r => r.Id != o.Id)))
                gone.StampDeleted(userId);

            foreach (var optionDto in dto.Options)
            {
                var option = existingOptions.FirstOrDefault(o => o.Id == optionDto.Id);
                if (option is null)
                {
                    option = new ComboComponentOption { ComboComponentId = component.Id }.StampNew(tenant, userId);
                    component.Options.Add(option);
                }
                else option.StampUpdated(userId);

                option.MenuItemId = optionDto.MenuItemId;
                option.VariantId = optionDto.VariantId;
                option.UpchargeAmount = optionDto.UpchargeAmount;
                option.IsDefault = optionDto.IsDefault;
                option.DisplayOrder = optionDto.DisplayOrder;
            }
        }

        await db.SaveChangesAsync();
        return RestaurantMapper.ToDto(combo);
    }

    // ── 86 list ──────────────────────────────────────────────────────────────

    public async Task<List<AvailabilityDto>> Get86ListAsync(Guid outletId, bool unavailableOnly)
    {
        var rows = await db.MenuItemAvailabilities.ForTenant(tenant)
            .Where(a => a.OutletId == outletId)
            .WhereIf(unavailableOnly, a => !a.IsAvailable)
            .OrderByDescending(a => a.MarkedAt)
            .ToListAsync();

        var itemIds = rows.Select(r => r.MenuItemId).Distinct().ToList();
        var items = await db.MenuItems.ForTenant(tenant)
            .Where(i => itemIds.Contains(i.Id))
            .Select(i => new { i.Id, i.Name, i.CategoryId })
            .ToListAsync();

        var categoryNames = await db.MenuCategories.ForTenant(tenant)
            .ToDictionaryAsync(c => c.Id, c => c.Name);

        var staffNames = await db.Staff.ForTenant(tenant)
            .ToDictionaryAsync(s => s.Id, s => s.FullName);

        var variantNames = await db.MenuItemVariants.ForTenant(tenant)
            .Where(v => itemIds.Contains(v.MenuItemId))
            .ToDictionaryAsync(v => v.Id, v => v.Name);

        return rows.Select(r =>
        {
            var dto = RestaurantMapper.ToDto(r);
            var item = items.FirstOrDefault(i => i.Id == r.MenuItemId);
            dto.MenuItemName = item?.Name;
            if (item is not null) dto.CategoryName = categoryNames.GetValueOrDefault(item.CategoryId);
            if (r.VariantId.HasValue) dto.VariantName = variantNames.GetValueOrDefault(r.VariantId.Value);
            if (r.MarkedByStaffId.HasValue) dto.MarkedByStaffName = staffNames.GetValueOrDefault(r.MarkedByStaffId.Value);
            return dto;
        }).ToList();
    }

    public async Task<AvailabilityDto> Set86Async(Set86Dto request, Guid userId)
    {
        var row = await db.MenuItemAvailabilities.ForTenant(tenant)
            .FirstOrDefaultAsync(a => a.OutletId == request.OutletId
                                   && a.MenuItemId == request.MenuItemId
                                   && a.VariantId == request.VariantId);

        if (row is null)
        {
            row = new MenuItemAvailability
            {
                OutletId = request.OutletId,
                MenuItemId = request.MenuItemId,
                VariantId = request.VariantId,
            }.StampNew(tenant, userId);
            db.MenuItemAvailabilities.Add(row);
        }
        else row.StampUpdated(userId);

        row.IsAvailable = request.IsAvailable;
        row.Reason = request.Reason;
        row.AvailableAgainAt = request.AvailableAgainAt;
        row.MarkedAt = DateTime.UtcNow;
        row.MarkedByStaffId = request.StaffId;
        row.IsAutomatic = false;

        // The denormalised flag on the item is what the order pad reads on every keystroke;
        // keeping it in step here means the pad never has to join to this table.
        if (request.VariantId is null)
        {
            var item = await db.MenuItems.ForTenant(tenant).FirstOrDefaultAsync(i => i.Id == request.MenuItemId);
            if (item is not null)
            {
                item.IsAvailable = request.IsAvailable;
                item.StampUpdated(userId);
            }
        }
        else
        {
            var variant = await db.MenuItemVariants.ForTenant(tenant).FirstOrDefaultAsync(v => v.Id == request.VariantId);
            if (variant is not null)
            {
                variant.IsAvailable = request.IsAvailable;
                variant.StampUpdated(userId);
            }
        }

        await db.SaveChangesAsync();
        return RestaurantMapper.ToDto(row);
    }

    // ── Pricing ──────────────────────────────────────────────────────────────

    public async Task<decimal> ResolvePriceAsync(
        Guid menuItemId, Guid? variantId, Guid outletId, OrderType orderType, DateTime at)
    {
        var item = await db.MenuItems.ForTenant(tenant).FirstOrDefaultAsync(i => i.Id == menuItemId)
            ?? throw new InvalidOperationException("Menu item not found.");

        var prices = await db.MenuItemPrices.ForTenant(tenant)
            .Where(p => p.MenuItemId == menuItemId)
            .ToListAsync();

        var basePrice = await ResolveBasePriceAsync(item, variantId, outletId, orderType, prices);

        var rules = await db.HappyHourRules.ForTenant(tenant)
            .Where(r => r.IsActive && (r.OutletId == null || r.OutletId == outletId))
            .OrderBy(r => r.Priority)
            .ToListAsync();

        var rule = rules.FirstOrDefault(r => IsHappyHourApplicable(r, item, orderType, at));
        if (rule is null) return Math.Round(basePrice, 2);

        var discounted = rule.DiscountKind == DiscountKind.Percentage
            ? basePrice * (1 - rule.DiscountValue / 100m)
            : basePrice - rule.DiscountValue;

        return Math.Round(Math.Max(0, discounted), 2);
    }

    private async Task<decimal> ResolveBasePriceAsync(
        MenuItem item, Guid? variantId, Guid outletId, OrderType orderType, List<MenuItemPrice> prices)
    {
        var scope = ScopeFor(orderType);

        // Most specific wins: this outlet + this scope, then any outlet + this scope, then the
        // base scope, then the variant's own price, then the item's base price.
        var match = prices.FirstOrDefault(p => p.VariantId == variantId && p.OutletId == outletId && p.Scope == scope)
                 ?? prices.FirstOrDefault(p => p.VariantId == variantId && p.OutletId == null && p.Scope == scope)
                 ?? prices.FirstOrDefault(p => p.VariantId == variantId && p.OutletId == outletId && p.Scope == PriceScope.Base)
                 ?? prices.FirstOrDefault(p => p.VariantId == variantId && p.OutletId == null && p.Scope == PriceScope.Base);

        if (match is not null) return match.Price;

        if (variantId.HasValue)
        {
            var variant = await db.MenuItemVariants.ForTenant(tenant).FirstOrDefaultAsync(v => v.Id == variantId);
            if (variant is not null && variant.Price > 0) return variant.Price;
        }

        return item.BasePrice;
    }

    internal static PriceScope ScopeFor(OrderType orderType) => orderType switch
    {
        OrderType.DineIn or OrderType.BarTab or OrderType.RoomService => PriceScope.DineIn,
        OrderType.Takeaway or OrderType.Counter or OrderType.Curbside => PriceScope.Takeaway,
        OrderType.Delivery => PriceScope.Delivery,
        OrderType.DriveThru => PriceScope.DriveThru,
        _ => PriceScope.Base,
    };

    internal static bool IsHappyHourApplicable(HappyHourRule rule, MenuItem item, OrderType orderType, DateTime at)
    {
        if (rule.EffectiveFrom.HasValue && at < rule.EffectiveFrom.Value) return false;
        if (rule.EffectiveTo.HasValue && at > rule.EffectiveTo.Value) return false;
        if (rule.MenuItemId.HasValue && rule.MenuItemId != item.Id) return false;
        if (rule.CategoryId.HasValue && rule.CategoryId != item.CategoryId) return false;

        if (!string.IsNullOrWhiteSpace(rule.ActiveDays))
        {
            var days = rule.ActiveDays.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (days.Length > 0 && !days.Contains(((int)at.DayOfWeek).ToString())) return false;
        }

        if (!string.IsNullOrWhiteSpace(rule.ApplicableOrderTypes))
        {
            var types = rule.ApplicableOrderTypes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (types.Length > 0 && !types.Contains(((int)orderType).ToString())) return false;
        }

        var t = at.TimeOfDay;
        return rule.StartTime <= rule.EndTime
            ? t >= rule.StartTime && t <= rule.EndTime
            : t >= rule.StartTime || t <= rule.EndTime;   // window wraps midnight
    }

    // ── Order-pad catalogue ──────────────────────────────────────────────────

    /// <summary>
    /// Everything the order pad needs, priced and filtered, in one payload. Assembled here rather
    /// than in the order service because it is a menu concern, and because the pad and the
    /// pricing path must share one implementation.
    /// </summary>
    public async Task<OrderPadCatalogDto> BuildCatalogAsync(Guid outletId, OrderType orderType)
    {
        var now = DateTime.UtcNow;

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();
        var outlet = await db.Outlets.ForTenant(tenant).FirstOrDefaultAsync(o => o.Id == outletId);

        var allMenus = await db.Menus.ForTenant(tenant)
            .Where(m => m.OutletId == null || m.OutletId == outletId)
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync();

        var liveMenus = allMenus.Where(m => IsMenuActive(m, now)).ToList();
        // A venue with no time-boxed menu configured still has to be able to sell: fall back to
        // every enabled menu rather than showing an empty pad.
        if (liveMenus.Count == 0) liveMenus = allMenus.Where(m => m.IsActive).ToList();

        var menuIds = liveMenus.Select(m => m.Id).ToList();

        var categories = await db.MenuCategories.ForTenant(tenant)
            .Where(c => menuIds.Contains(c.MenuId) && c.IsActive)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .ToListAsync();

        var categoryIds = categories.Select(c => c.Id).ToList();

        var items = await db.MenuItems.ForTenant(tenant)
            .Where(i => categoryIds.Contains(i.CategoryId) && i.IsActive)
            .Include(i => i.Variants)
            .Include(i => i.Prices)
            .OrderBy(i => i.DisplayOrder).ThenBy(i => i.Name)
            .ToListAsync();

        var itemIds = items.Select(i => i.Id).ToList();
        var modifierMap = await LoadItemModifierGroupsAsync(itemIds);

        var unavailable = await db.MenuItemAvailabilities.ForTenant(tenant)
            .Where(a => a.OutletId == outletId && !a.IsAvailable && a.VariantId == null)
            .ToListAsync();

        var happyHours = await db.HappyHourRules.ForTenant(tenant)
            .Where(r => r.IsActive && (r.OutletId == null || r.OutletId == outletId))
            .OrderBy(r => r.Priority)
            .ToListAsync();

        var allPrices = await db.MenuItemPrices.ForTenant(tenant)
            .Where(p => itemIds.Contains(p.MenuItemId))
            .ToListAsync();

        var stationNames = await db.Stations.ForTenant(tenant)
            .Where(s => s.OutletId == outletId)
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        var categoryNames = categories.ToDictionary(c => c.Id, c => c.Name);

        var itemDtos = new List<MenuItemDto>(items.Count);
        foreach (var item in items)
        {
            var dto = RestaurantMapper.ToDto(item);
            dto.CategoryName = categoryNames.GetValueOrDefault(item.CategoryId);
            if (item.StationId.HasValue) dto.StationName = stationNames.GetValueOrDefault(item.StationId.Value);
            dto.ModifierGroups = modifierMap.GetValueOrDefault(item.Id) ?? [];

            var prices = allPrices.Where(p => p.MenuItemId == item.Id).ToList();
            dto.BasePrice = await ApplyPricingAsync(item, null, outletId, orderType, now, prices, happyHours);

            foreach (var variant in dto.Variants)
                variant.Price = await ApplyPricingAsync(item, variant.Id, outletId, orderType, now, prices, happyHours);

            var block = unavailable.FirstOrDefault(a => a.MenuItemId == item.Id);
            if (block is not null)
            {
                dto.IsAvailable = false;
                dto.UnavailableReason = block.Reason;
            }

            itemDtos.Add(dto);
        }

        var combos = await GetCombosAsync(outletId);

        return new OrderPadCatalogDto
        {
            OutletId = outletId,
            ResolvedAt = now,
            OrderType = orderType,
            CurrencyCode = outlet?.CurrencyCode ?? "USD",
            PricesIncludeTax = settings?.PricesIncludeTax ?? false,
            Menus = liveMenus.Select(m => { var d = RestaurantMapper.ToDto(m); d.IsCurrentlyActive = true; return d; }).ToList(),
            Categories = categories.Select(RestaurantMapper.ToDto).ToList(),
            Items = itemDtos,
            ModifierGroups = modifierMap.Values.SelectMany(g => g)
                .GroupBy(g => g.ModifierGroupId)
                .Select(g => new ModifierGroupDto
                {
                    Id = g.First().ModifierGroupId,
                    Name = g.First().Name,
                    PromptText = g.First().PromptText,
                    SelectionMode = g.First().SelectionMode,
                    IsRequired = g.First().IsRequired,
                    MinSelections = g.First().MinSelections,
                    MaxSelections = g.First().MaxSelections,
                    FreeSelections = g.First().FreeSelections,
                    DisplayOrder = g.First().DisplayOrder,
                    Modifiers = g.First().Modifiers,
                }).ToList(),
            Combos = combos,
            UnavailableItemIds = unavailable.Select(a => a.MenuItemId).Distinct().ToList(),
        };
    }

    private Task<decimal> ApplyPricingAsync(
        MenuItem item, Guid? variantId, Guid outletId, OrderType orderType, DateTime at,
        List<MenuItemPrice> prices, List<HappyHourRule> happyHours)
    {
        var scope = ScopeFor(orderType);

        var match = prices.FirstOrDefault(p => p.VariantId == variantId && p.OutletId == outletId && p.Scope == scope)
                 ?? prices.FirstOrDefault(p => p.VariantId == variantId && p.OutletId == null && p.Scope == scope)
                 ?? prices.FirstOrDefault(p => p.VariantId == variantId && p.OutletId == outletId && p.Scope == PriceScope.Base)
                 ?? prices.FirstOrDefault(p => p.VariantId == variantId && p.OutletId == null && p.Scope == PriceScope.Base);

        decimal basePrice;
        if (match is not null) basePrice = match.Price;
        else if (variantId.HasValue)
        {
            var variant = item.Variants.FirstOrDefault(v => v.Id == variantId);
            basePrice = variant is { Price: > 0 } ? variant.Price : item.BasePrice;
        }
        else basePrice = item.BasePrice;

        var rule = happyHours.FirstOrDefault(r => IsHappyHourApplicable(r, item, orderType, at));
        if (rule is null) return Task.FromResult(Math.Round(basePrice, 2));

        var discounted = rule.DiscountKind == DiscountKind.Percentage
            ? basePrice * (1 - rule.DiscountValue / 100m)
            : basePrice - rule.DiscountValue;

        return Task.FromResult(Math.Round(Math.Max(0, discounted), 2));
    }
}
