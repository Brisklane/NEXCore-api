using Fitness.Application.DTOs;
using Fitness.Application.Services.Interfaces;
using Fitness.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Fitness.Api.Controllers;

/// <summary>
/// What the club sells: membership plans, session packs, passes, promotions and services.
///
/// Editing a plan's terms bumps its version rather than rewriting history — members on the old
/// terms keep them, because the terms they signed are the terms they signed. Pushing a price
/// change onto existing members is a separate, deliberate action.
/// </summary>
[Route("api/fitness/catalogue")]
public class CatalogueController(
    ICatalogueService catalogue,
    ILogger<CatalogueController> logger) : FitnessControllerBase(logger)
{
    [HttpGet("plans")]
    public Task<IActionResult> GetPlans(
        [FromQuery] Guid? clubId, [FromQuery] PlanKind? kind, [FromQuery] bool sellableOnly = false)
        => Run(() => catalogue.GetPlansAsync(clubId, kind, sellableOnly));

    [HttpGet("plans/{id:guid}")]
    public Task<IActionResult> GetPlan(Guid id)
        => RunFound(() => catalogue.GetPlanAsync(id), "Plan not found.");

    [HttpPost("plans")]
    public Task<IActionResult> CreatePlan([FromBody] SavePlanDto request)
        => Run(() => catalogue.SavePlanAsync(null, request, UserId), "Plan created.");

    [HttpPut("plans/{id:guid}")]
    public Task<IActionResult> UpdatePlan(Guid id, [FromBody] SavePlanDto request)
        => Run(() => catalogue.SavePlanAsync(id, request, UserId), "Plan saved.");

    [HttpDelete("plans/{id:guid}")]
    public Task<IActionResult> DeletePlan(Guid id)
        => Run(() => catalogue.DeletePlanAsync(id, UserId), "Plan withdrawn from sale.");

    /// <summary>What the join wizard and the kiosk show: sellable plans with live prices.</summary>
    [HttpGet("sales/{clubId:guid}")]
    public Task<IActionResult> GetSalesCatalogue(Guid clubId)
        => Run(() => catalogue.GetSalesCatalogueAsync(clubId));

    // ── Promotions ───────────────────────────────────────────────────────────

    [HttpGet("promotions")]
    public Task<IActionResult> GetPromotions([FromQuery] Guid? clubId, [FromQuery] bool activeOnly = true)
        => Run(() => catalogue.GetPromotionsAsync(clubId, activeOnly));

    [HttpPost("promotions")]
    public Task<IActionResult> SavePromotion([FromBody] PromotionRuleDto request, [FromQuery] Guid? id = null)
        => Run(() => catalogue.SavePromotionAsync(id, request, UserId), "Promotion saved.");

    /// <summary>
    /// Checks a promo code before it is applied.
    ///
    /// A refusal comes back as a sentence the person at the desk can read out — "that code is for
    /// new members only" beats a red cross with no explanation.
    /// </summary>
    [HttpPost("promo-codes/check")]
    public Task<IActionResult> CheckPromoCode([FromBody] PromoCodeCheckDto request)
        => Run(() => catalogue.CheckPromoCodeAsync(request));

    // ── Services ─────────────────────────────────────────────────────────────

    [HttpGet("services")]
    public Task<IActionResult> GetServices([FromQuery] Guid? clubId, [FromQuery] bool activeOnly = true)
        => Run(() => catalogue.GetServicesAsync(clubId, activeOnly));

    [HttpPost("services")]
    public Task<IActionResult> SaveService([FromBody] AppointmentServiceDto request, [FromQuery] Guid? id = null)
        => Run(() => catalogue.SaveServiceAsync(id, request, UserId), "Service saved.");

    // ── Upgrades and downgrades ──────────────────────────────────────────────

    [HttpGet("plans/{id:guid}/change-paths")]
    public Task<IActionResult> GetChangePaths(Guid id)
        => Run(() => catalogue.GetChangePathsAsync(id));
}
