using Microsoft.AspNetCore.Mvc;
using RealEstate.Application.DTOs;
using RealEstate.Application.Services.Interfaces;

namespace RealEstate.Api.Controllers;

/// <summary>
/// The inventory board, holds, blocks, price lists and the cost sheet.
///
/// The board is the busiest screen in the application and the one where two people can sell the
/// same flat, so a hold is a real record with an owner and an expiry rather than a colour on a
/// grid. The cost sheet is the other half: it is what a customer is shown before they pay, and it
/// is built from the price list in force at that moment so it can be reproduced afterwards.
/// </summary>
[Route("api/realestate/inventory")]
public class InventoryController(
    IInventoryService inventory,
    ILogger<InventoryController> logger) : RealEstateControllerBase(logger)
{
    [HttpPost("board")]
    public Task<IActionResult> GetBoard([FromBody] InventoryQueryDto query)
        => Run(() => inventory.GetBoardAsync(query));

    [HttpGet("units/{unitId:guid}")]
    public Task<IActionResult> GetUnit(Guid unitId)
        => RunFound(() => inventory.GetUnitAsync(unitId), "That unit does not exist.");

    [HttpPost("units")]
    public Task<IActionResult> SaveUnit([FromBody] InventoryUnitDto request)
        => Run(() => inventory.SaveUnitAsync(request, UserId), "Unit saved.");

    // ── Holds ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Takes a unit off the board for a named person for a fixed time. Anything longer than the
    /// configured limit needs an approval, because an indefinite hold is inventory removed from
    /// sale by somebody with no authority to remove it.
    /// </summary>
    [HttpPost("holds")]
    public Task<IActionResult> Hold([FromBody] HoldRequestDto request)
        => Run(() => inventory.HoldAsync(request, UserId), "Unit held.");

    [HttpPost("holds/{holdId:guid}/release")]
    public Task<IActionResult> ReleaseHold(Guid holdId, [FromQuery] string? note)
        => Run(() => inventory.ReleaseHoldAsync(holdId, note, UserId), "Hold released.");

    [HttpGet("holds")]
    public Task<IActionResult> GetActiveHolds([FromQuery] Guid? projectId)
        => Run(() => inventory.GetActiveHoldsAsync(projectId));

    /// <summary>Sweeps up holds whose time has run out. Safe to run repeatedly.</summary>
    [HttpPost("holds/expire")]
    public Task<IActionResult> ExpireHolds()
        => Run(inventory.ExpireHoldsAsync, "Expired holds released.");

    // ── Blocks ───────────────────────────────────────────────────────────────

    [HttpPost("blocks")]
    public Task<IActionResult> Block([FromBody] BlockRequestDto request)
        => Run(() => inventory.BlockAsync(request, UserId), "Unit blocked.");

    [HttpPost("units/{unitId:guid}/unblock")]
    public Task<IActionResult> Unblock(Guid unitId, [FromQuery] string? note)
        => Run(() => inventory.UnblockAsync(unitId, note, UserId), "Unit unblocked.");

    // ── Pricing ──────────────────────────────────────────────────────────────

    [HttpGet("price-lists")]
    public Task<IActionResult> GetPriceLists([FromQuery] Guid projectId)
        => Run(() => inventory.GetPriceListsAsync(projectId));

    [HttpGet("price-lists/{id:guid}")]
    public Task<IActionResult> GetPriceList(Guid id)
        => RunFound(() => inventory.GetPriceListAsync(id), "That price list does not exist.");

    [HttpPost("price-lists")]
    public Task<IActionResult> SavePriceList([FromBody] PriceListDto request)
        => Run(() => inventory.SavePriceListAsync(request, UserId), "Price list saved.");

    /// <summary>Puts a price list into force. Published lists are never edited afterwards.</summary>
    [HttpPost("price-lists/{id:guid}/publish")]
    public Task<IActionResult> PublishPriceList(Guid id)
        => Run(() => inventory.PublishPriceListAsync(id, UserId), "Price list published.");

    /// <summary>
    /// Applies a published list to everything still available. Sold and booked units keep the price
    /// they were sold at — re-pricing must never rewrite a signed cost sheet.
    /// </summary>
    [HttpPost("price-lists/{id:guid}/reprice")]
    public Task<IActionResult> Reprice(Guid id)
        => Run(() => inventory.RepriceAvailableUnitsAsync(id, UserId), "Available units repriced.");

    [HttpGet("premiums")]
    public Task<IActionResult> GetPremiums([FromQuery] Guid projectId)
        => Run(() => inventory.GetPremiumsAsync(projectId));

    [HttpPost("premiums")]
    public Task<IActionResult> SavePremium([FromBody] PremiumChargeDto request)
        => Run(() => inventory.SavePremiumAsync(request, UserId), "Premium saved.");

    /// <summary>
    /// The cost sheet a customer is shown: base price, every premium, every charge, tax, and the
    /// instalment schedule that follows from it.
    /// </summary>
    [HttpGet("units/{unitId:guid}/cost-sheet")]
    public Task<IActionResult> GetCostSheet(
        Guid unitId,
        [FromQuery] Guid? paymentPlanTemplateId,
        [FromQuery] decimal discountAmount = 0,
        [FromQuery] decimal discountPercent = 0)
        => Run(() => inventory.GetCostSheetAsync(unitId, paymentPlanTemplateId, discountAmount, discountPercent));
}
