using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.DTOs;
using Restaurant.Application.Services.Interfaces;
using Restaurant.Domain.Enums;

namespace Restaurant.Api.Controllers;

/// <summary>Recipes, food cost and the wastage log.</summary>
[Route("api/restaurant/recipes")]
public class RecipeController(
    IRecipeCostingService recipes,
    ILogger<RecipeController> logger) : RestaurantControllerBase(logger)
{
    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] Guid? menuItemId, [FromQuery] bool includeSubRecipes = true)
        => Run(() => recipes.GetRecipesAsync(menuItemId, includeSubRecipes));

    [HttpGet("{id:guid}")]
    public Task<IActionResult> GetById(Guid id)
        => RunFound(() => recipes.GetRecipeAsync(id), "Recipe not found.");

    [HttpPost]
    public Task<IActionResult> Create([FromBody] SaveRecipeDto request)
        => Run(() => recipes.SaveRecipeAsync(null, request, UserId), "Recipe saved.");

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(Guid id, [FromBody] SaveRecipeDto request)
        => Run(() => recipes.SaveRecipeAsync(id, request, UserId), "Recipe saved.");

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(Guid id)
        => Run(() => recipes.DeleteRecipeAsync(id, UserId), "Recipe removed.");

    [HttpPost("{id:guid}/recalculate")]
    public Task<IActionResult> Recalculate(Guid id)
        => Run(() => recipes.RecalculateCostAsync(id, UserId), "Cost recalculated.");

    /// <summary>Re-costs every recipe — run this after an ingredient price change.</summary>
    [HttpPost("recalculate-all")]
    public Task<IActionResult> RecalculateAll()
        => Run(() => recipes.RecalculateAllAsync(UserId), "All recipes re-costed.");

    // ── Wastage ──────────────────────────────────────────────────────────────

    [HttpGet("wastage/{outletId:guid}")]
    public Task<IActionResult> GetWastage(
        Guid outletId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] WastageReason? reason)
        => Run(() => recipes.GetWastageAsync(
            outletId,
            from ?? DateTime.UtcNow.Date.AddDays(-30),
            to ?? DateTime.UtcNow,
            reason));

    [HttpPost("wastage")]
    public Task<IActionResult> LogWastage([FromBody] SaveWastageDto request)
        => Run(() => recipes.LogWastageAsync(request, UserId), "Wastage recorded.");

    [HttpDelete("wastage/{id:guid}")]
    public Task<IActionResult> DeleteWastage(Guid id)
        => Run(() => recipes.DeleteWastageAsync(id, UserId), "Entry removed.");
}
