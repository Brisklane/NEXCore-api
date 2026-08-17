using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Restaurant.Application.DTOs;
using Restaurant.Application.Services.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Infrastructure.Services;

/// <summary>
/// Recipes, food cost and wastage.
///
/// Cost roll-up is not simply "add up the ingredients". Two adjustments matter and both are
/// routinely got wrong:
///
/// **Yield** — 1kg of onion is not 1kg of usable onion. A 90% yield means you have to buy
/// 1/0.9 of what the recipe calls for, so cost divides by the yield factor.
///
/// **Waste** — a portion of what you prepped gets dropped, burnt or over-portioned. That is a
/// multiplier on top, not a second division.
///
/// Get either backwards and every food-cost percentage in the business is wrong in the
/// optimistic direction, which is the direction that closes restaurants.
/// </summary>
public class RecipeCostingService(
    RestaurantDbContext db,
    IRestaurantTenant tenant,
    ILogger<RecipeCostingService> logger) : IRecipeCostingService
{
    // ── Recipes ──────────────────────────────────────────────────────────────

    public async Task<List<RecipeDto>> GetRecipesAsync(Guid? menuItemId, bool includeSubRecipes)
    {
        var recipes = await db.Recipes.ForTenant(tenant)
            .WhereIf(menuItemId.HasValue, r => r.MenuItemId == menuItemId)
            .WhereIf(!includeSubRecipes, r => !r.IsSubRecipe)
            .Include(r => r.Ingredients.Where(i => !i.IsDeleted))
            .OrderBy(r => r.Name)
            .ToListAsync();

        return await DecorateAsync(recipes);
    }

    public async Task<RecipeDto?> GetRecipeAsync(Guid recipeId)
    {
        var recipe = await db.Recipes.ForTenant(tenant)
            .Include(r => r.Ingredients.Where(i => !i.IsDeleted))
            .FirstOrDefaultAsync(r => r.Id == recipeId);

        if (recipe is null) return null;
        return (await DecorateAsync([recipe])).First();
    }

    private async Task<List<RecipeDto>> DecorateAsync(List<Recipe> recipes)
    {
        if (recipes.Count == 0) return [];

        var itemIds = recipes.Where(r => r.MenuItemId.HasValue).Select(r => r.MenuItemId!.Value).Distinct().ToList();
        var items = await db.MenuItems.ForTenant(tenant)
            .Where(i => itemIds.Contains(i.Id))
            .Select(i => new { i.Id, i.Name, i.BasePrice })
            .ToListAsync();

        var variantIds = recipes.Where(r => r.VariantId.HasValue).Select(r => r.VariantId!.Value).Distinct().ToList();
        var variants = await db.MenuItemVariants.ForTenant(tenant)
            .Where(v => variantIds.Contains(v.Id))
            .Select(v => new { v.Id, v.Name, v.Price })
            .ToListAsync();

        return recipes.Select(r =>
        {
            var dto = RestaurantMapper.ToDto(r);
            var item = items.FirstOrDefault(i => i.Id == r.MenuItemId);
            var variant = variants.FirstOrDefault(v => v.Id == r.VariantId);

            dto.MenuItemName = item?.Name;
            dto.VariantName = variant?.Name;
            dto.SellingPrice = variant?.Price > 0 ? variant.Price : item?.BasePrice ?? 0m;

            dto.FoodCostPercent = dto.SellingPrice > 0
                ? Math.Round(dto.CostPerPortion / dto.SellingPrice * 100m, 2)
                : 0m;

            dto.ContributionMargin = Math.Round(dto.SellingPrice - dto.CostPerPortion, 2);
            return dto;
        }).ToList();
    }

    public async Task<RecipeDto> SaveRecipeAsync(Guid? id, SaveRecipeDto request, Guid userId)
    {
        Recipe recipe;

        if (id.HasValue)
        {
            recipe = await db.Recipes.ForTenant(tenant)
                .Include(r => r.Ingredients)
                .FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new InvalidOperationException("Recipe not found.");

            // Bumping the version keeps historical cost snapshots explicable: a dish that got
            // 20% dearer last Tuesday should show a version change, not an unexplained jump.
            recipe.Version++;
            recipe.StampUpdated(userId);
        }
        else
        {
            recipe = new Recipe().StampNew(tenant, userId);
            db.Recipes.Add(recipe);
        }

        recipe.Code = request.Code;
        recipe.MenuItemId = request.MenuItemId;
        recipe.VariantId = request.VariantId;
        recipe.Name = request.Name;
        recipe.YieldQuantity = request.YieldQuantity <= 0 ? 1m : request.YieldQuantity;
        recipe.YieldUom = request.YieldUom;
        recipe.IsSubRecipe = request.IsSubRecipe;
        recipe.OutputInventoryItemId = request.OutputInventoryItemId;
        recipe.PrepTimeMinutes = request.PrepTimeMinutes;
        recipe.CookTimeMinutes = request.CookTimeMinutes;
        recipe.Instructions = request.Instructions;
        recipe.PlatingNotes = request.PlatingNotes;
        recipe.IsActive = request.IsActive;
        recipe.Description = request.Description;

        var existing = recipe.Ingredients.Where(i => !i.IsDeleted).ToList();

        foreach (var gone in existing.Where(i => request.Ingredients.All(r => r.Id != i.Id)))
            gone.StampDeleted(userId);

        foreach (var dto in request.Ingredients)
        {
            if (dto.SubRecipeId == recipe.Id)
                throw new InvalidOperationException("A recipe cannot contain itself.");

            var ingredient = existing.FirstOrDefault(i => i.Id == dto.Id);
            if (ingredient is null)
            {
                ingredient = new RecipeIngredient { RecipeId = recipe.Id }.StampNew(tenant, userId);
                recipe.Ingredients.Add(ingredient);
            }
            else ingredient.StampUpdated(userId);

            ingredient.InventoryItemId = dto.InventoryItemId;
            ingredient.SubRecipeId = dto.SubRecipeId;
            ingredient.IngredientName = dto.IngredientName;
            ingredient.Quantity = dto.Quantity;
            ingredient.Uom = dto.Uom;
            ingredient.YieldPercent = dto.YieldPercent <= 0 ? 100m : dto.YieldPercent;
            ingredient.WastePercent = Math.Max(0, dto.WastePercent);
            ingredient.UnitCost = dto.UnitCost;
            ingredient.IsOptional = dto.IsOptional;
            ingredient.DisplayOrder = dto.DisplayOrder;
            ingredient.Note = dto.Note;
        }

        await db.SaveChangesAsync();
        return await RecalculateCostAsync(recipe.Id, userId);
    }

    public async Task DeleteRecipeAsync(Guid recipeId, Guid userId)
    {
        var recipe = await db.Recipes.ForTenant(tenant).FirstOrDefaultAsync(r => r.Id == recipeId)
            ?? throw new InvalidOperationException("Recipe not found.");

        var usedBy = await db.RecipeIngredients.ForTenant(tenant)
            .CountAsync(i => i.SubRecipeId == recipeId);

        if (usedBy > 0)
            throw new InvalidOperationException(
                $"This sub-recipe is used by {usedBy} other recipe(s). Remove it from them first.");

        recipe.StampDeleted(userId);
        await db.SaveChangesAsync();
    }

    public async Task<RecipeDto> RecalculateCostAsync(Guid recipeId, Guid userId)
    {
        var recipe = await db.Recipes.ForTenant(tenant)
            .Include(r => r.Ingredients.Where(i => !i.IsDeleted))
            .FirstOrDefaultAsync(r => r.Id == recipeId)
            ?? throw new InvalidOperationException("Recipe not found.");

        await RollUpAsync(recipe, userId, []);
        await db.SaveChangesAsync();

        return (await GetRecipeAsync(recipe.Id))!;
    }

    public async Task<int> RecalculateAllAsync(Guid userId)
    {
        var recipes = await db.Recipes.ForTenant(tenant)
            .Include(r => r.Ingredients.Where(i => !i.IsDeleted))
            .ToListAsync();

        // Sub-recipes first: a sauce has to be costed before the dish that uses it can be.
        foreach (var recipe in recipes.OrderByDescending(r => r.IsSubRecipe))
            await RollUpAsync(recipe, userId, []);

        await db.SaveChangesAsync();
        logger.LogInformation("Recalculated cost for {Count} recipe(s)", recipes.Count);
        return recipes.Count;
    }

    /// <summary>
    /// Costs one recipe and pushes the per-portion figure onto the menu item it belongs to.
    ///
    /// <paramref name="visiting"/> guards against a cycle — a sauce that (through a chain of
    /// sub-recipes) contains itself would otherwise recurse until the stack gives out.
    /// </summary>
    private async Task<decimal> RollUpAsync(Recipe recipe, Guid userId, HashSet<Guid> visiting)
    {
        if (!visiting.Add(recipe.Id))
            throw new InvalidOperationException($"Recipe '{recipe.Name}' contains itself through a sub-recipe.");

        var total = 0m;

        foreach (var ingredient in recipe.Ingredients.Where(i => !i.IsDeleted))
        {
            var unitCost = ingredient.UnitCost;

            if (ingredient.SubRecipeId.HasValue)
            {
                var sub = await db.Recipes.ForTenant(tenant)
                    .Include(r => r.Ingredients.Where(i => !i.IsDeleted))
                    .FirstOrDefaultAsync(r => r.Id == ingredient.SubRecipeId);

                if (sub is not null)
                {
                    var subTotal = await RollUpAsync(sub, userId, visiting);
                    unitCost = sub.YieldQuantity > 0 ? subTotal / sub.YieldQuantity : subTotal;
                }
            }

            // Yield divides (you buy more than you use); waste multiplies (you lose some of what
            // you prepped). Both push the true cost up.
            var yieldFactor = ingredient.YieldPercent > 0 ? ingredient.YieldPercent / 100m : 1m;
            var wasteFactor = 1m + ingredient.WastePercent / 100m;

            ingredient.UnitCost = unitCost;
            ingredient.LineCost = Math.Round(ingredient.Quantity / yieldFactor * wasteFactor * unitCost, 6);
            ingredient.StampUpdated(userId);

            total += ingredient.LineCost;
        }

        recipe.TotalCost = Math.Round(total, 4);
        recipe.StampUpdated(userId);

        visiting.Remove(recipe.Id);

        // The item carries a denormalised cost so the order pad and the margin reports never
        // have to explode a recipe on the hot path.
        var perPortion = recipe.YieldQuantity > 0 ? recipe.TotalCost / recipe.YieldQuantity : recipe.TotalCost;

        if (recipe.VariantId.HasValue)
        {
            var variant = await db.MenuItemVariants.ForTenant(tenant)
                .FirstOrDefaultAsync(v => v.Id == recipe.VariantId);

            if (variant is not null)
            {
                variant.StandardCost = Math.Round(perPortion, 4);
                variant.StampUpdated(userId);
            }
        }
        else if (recipe.MenuItemId.HasValue)
        {
            var item = await db.MenuItems.ForTenant(tenant)
                .FirstOrDefaultAsync(i => i.Id == recipe.MenuItemId);

            if (item is not null)
            {
                item.StandardCost = Math.Round(perPortion, 4);
                item.StampUpdated(userId);
            }
        }

        return recipe.TotalCost;
    }

    // ── Wastage ──────────────────────────────────────────────────────────────

    public async Task<List<WastageLogDto>> GetWastageAsync(
        Guid outletId, DateTime from, DateTime to, WastageReason? reason)
    {
        var rows = await db.WastageLogs.ForTenant(tenant)
            .Where(w => w.OutletId == outletId && w.OccurredAt >= from && w.OccurredAt <= to)
            .WhereIf(reason.HasValue, w => w.Reason == reason)
            .OrderByDescending(w => w.OccurredAt)
            .ToListAsync();

        var outletNames = await db.Outlets.ForTenant(tenant).ToDictionaryAsync(o => o.Id, o => o.Name);
        var stationNames = await db.Stations.ForTenant(tenant).ToDictionaryAsync(s => s.Id, s => s.Name);
        var staffNames = await db.Staff.ForTenant(tenant).ToDictionaryAsync(s => s.Id, s => s.DisplayName ?? s.FullName);

        return rows.Select(w =>
        {
            var dto = RestaurantMapper.ToDto(w);
            dto.OutletName = outletNames.GetValueOrDefault(w.OutletId);
            if (w.StationId.HasValue) dto.StationName = stationNames.GetValueOrDefault(w.StationId.Value);
            if (w.StaffId.HasValue) dto.StaffName ??= staffNames.GetValueOrDefault(w.StaffId.Value);
            return dto;
        }).ToList();
    }

    public async Task<WastageLogDto> LogWastageAsync(SaveWastageDto request, Guid userId)
    {
        if (request.Quantity <= 0) throw new InvalidOperationException("Quantity must be greater than zero.");

        var unitCost = request.UnitCost;

        // If the user did not supply a cost, take it from the dish's standard cost — a wastage
        // report priced at zero tells a manager nothing.
        if (unitCost <= 0 && request.MenuItemId.HasValue)
        {
            unitCost = await db.MenuItems.ForTenant(tenant)
                .Where(i => i.Id == request.MenuItemId)
                .Select(i => i.StandardCost)
                .FirstOrDefaultAsync();
        }

        var member = request.StaffId.HasValue
            ? await db.Staff.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == request.StaffId)
            : null;

        var entry = new WastageLog
        {
            OutletId = request.OutletId,
            OccurredAt = request.OccurredAt ?? DateTime.UtcNow,
            Reason = request.Reason,
            MenuItemId = request.MenuItemId,
            InventoryItemId = request.InventoryItemId,
            ItemName = request.ItemName,
            Quantity = request.Quantity,
            Uom = request.Uom,
            UnitCost = unitCost,
            TotalCost = Math.Round(unitCost * request.Quantity, 2),
            StaffId = request.StaffId,
            StaffName = member?.DisplayName ?? member?.FullName,
            StationId = request.StationId,
            Note = request.Note,
        }.StampNew(tenant, userId);

        db.WastageLogs.Add(entry);
        await db.SaveChangesAsync();

        return RestaurantMapper.ToDto(entry);
    }

    public async Task DeleteWastageAsync(Guid wastageId, Guid userId)
    {
        var entry = await db.WastageLogs.ForTenant(tenant).FirstOrDefaultAsync(w => w.Id == wastageId)
            ?? throw new InvalidOperationException("Wastage entry not found.");

        if (entry.OrderLineId.HasValue)
            throw new InvalidOperationException(
                "This entry came from a voided order line and cannot be deleted on its own.");

        entry.StampDeleted(userId);
        await db.SaveChangesAsync();
    }
}
