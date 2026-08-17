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

/// <summary>Bills, splitting, tenders, discounts and the reason codes behind them.</summary>
[Route("api/restaurant/checks")]
public class CheckController(
    ICheckService checks,
    RestaurantDbContext db,
    IRestaurantTenant tenant,
    ILogger<CheckController> logger) : RestaurantControllerBase(logger)
{
    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] Guid? outletId,
        [FromQuery] CheckStatus? status,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] PaginationParams pagination)
    {
        try
        {
            return Ok(await checks.ListChecksAsync(outletId, status, from, to, pagination));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error listing checks");
            return StatusCode(500, new ApiErrorResponse { Message = "Could not load checks." });
        }
    }

    [HttpGet("{id:guid}")]
    public Task<IActionResult> GetById(Guid id)
        => RunFound(() => checks.GetCheckAsync(id), "Check not found.");

    [HttpGet("order/{orderId:guid}")]
    public Task<IActionResult> GetForOrder(Guid orderId)
        => Run(() => checks.GetChecksForOrderAsync(orderId));

    /// <summary>
    /// Carves an order into one or more checks. The split is computed server-side so the parts
    /// always add up to the order exactly.
    /// </summary>
    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateChecksDto request)
        => Run(() => checks.CreateChecksAsync(request, UserId), "Bill ready.");

    [HttpPost("payments")]
    public Task<IActionResult> TakePayment([FromBody] TakePaymentDto request)
        => Run(() => checks.TakePaymentAsync(request, UserId), "Payment taken.");

    [HttpPost("discounts")]
    public Task<IActionResult> ApplyDiscount([FromBody] ApplyDiscountDto request)
        => Run(() => checks.ApplyDiscountAsync(request, UserId), "Discount applied.");

    [HttpDelete("discounts/{discountId:guid}")]
    public Task<IActionResult> RemoveDiscount(Guid discountId)
        => Run(() => checks.RemoveDiscountAsync(discountId, UserId), "Discount removed.");

    [HttpPost("tips")]
    public Task<IActionResult> AddTip([FromBody] AddTipDto request)
        => Run(() => checks.AddTipAsync(request, UserId), "Tip recorded.");

    [HttpPost("waive-service-charge")]
    public Task<IActionResult> WaiveServiceCharge([FromBody] WaiveServiceChargeDto request)
        => Run(() => checks.WaiveServiceChargeAsync(request, UserId), "Service charge waived.");

    [HttpPost("void")]
    public Task<IActionResult> Void([FromBody] VoidCheckDto request)
        => Run(() => checks.VoidCheckAsync(request, UserId), "Check voided.");

    [HttpPost("refund")]
    public Task<IActionResult> Refund([FromBody] RefundPaymentDto request)
        => Run(() => checks.RefundPaymentAsync(request, UserId), "Refund issued.");

    [HttpPost("{id:guid}/printed")]
    public Task<IActionResult> MarkPrinted(Guid id)
        => Run(() => checks.MarkPrintedAsync(id, UserId));

    // ── Reason codes ─────────────────────────────────────────────────────────

    [HttpGet("void-reasons")]
    public Task<IActionResult> GetVoidReasons() => Run(async () =>
        await db.VoidReasons.ForTenant(tenant)
            .OrderBy(r => r.DisplayOrder)
            .Select(r => RestaurantMapper.ToDto(r))
            .ToListAsync());

    [HttpPost("void-reasons")]
    public Task<IActionResult> SaveVoidReason([FromBody] VoidReasonDto request) => Run(async () =>
    {
        var reason = request.Id == Guid.Empty
            ? null
            : await db.VoidReasons.ForTenant(tenant).FirstOrDefaultAsync(r => r.Id == request.Id);

        if (reason is null)
        {
            reason = new VoidReason().StampNew(tenant, UserId);
            db.VoidReasons.Add(reason);
        }
        else reason.StampUpdated(UserId);

        reason.Name = request.Name;
        reason.DisplayOrder = request.DisplayOrder;
        reason.RequiresApproval = request.RequiresApproval;
        reason.CountsAsWastage = request.CountsAsWastage;
        reason.IsActive = request.IsActive;

        await db.SaveChangesAsync();
        return RestaurantMapper.ToDto(reason);
    }, "Reason saved.");

    [HttpDelete("void-reasons/{id:guid}")]
    public Task<IActionResult> DeleteVoidReason(Guid id) => Run(async () =>
    {
        var reason = await db.VoidReasons.ForTenant(tenant).FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new InvalidOperationException("Reason not found.");

        reason.StampDeleted(UserId);
        await db.SaveChangesAsync();
    }, "Reason removed.");

    [HttpGet("discount-reasons")]
    public Task<IActionResult> GetDiscountReasons() => Run(async () =>
        await db.DiscountReasons.ForTenant(tenant)
            .OrderBy(r => r.DisplayOrder)
            .Select(r => RestaurantMapper.ToDto(r))
            .ToListAsync());

    [HttpPost("discount-reasons")]
    public Task<IActionResult> SaveDiscountReason([FromBody] DiscountReasonDto request) => Run(async () =>
    {
        var reason = request.Id == Guid.Empty
            ? null
            : await db.DiscountReasons.ForTenant(tenant).FirstOrDefaultAsync(r => r.Id == request.Id);

        if (reason is null)
        {
            reason = new DiscountReason().StampNew(tenant, UserId);
            db.DiscountReasons.Add(reason);
        }
        else reason.StampUpdated(UserId);

        reason.Name = request.Name;
        reason.DisplayOrder = request.DisplayOrder;
        reason.RequiresApproval = request.RequiresApproval;
        reason.MaxAmountWithoutApproval = request.MaxAmountWithoutApproval;
        reason.IsComp = request.IsComp;
        reason.IsActive = request.IsActive;

        await db.SaveChangesAsync();
        return RestaurantMapper.ToDto(reason);
    }, "Reason saved.");

    [HttpDelete("discount-reasons/{id:guid}")]
    public Task<IActionResult> DeleteDiscountReason(Guid id) => Run(async () =>
    {
        var reason = await db.DiscountReasons.ForTenant(tenant).FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new InvalidOperationException("Reason not found.");

        reason.StampDeleted(UserId);
        await db.SaveChangesAsync();
    }, "Reason removed.");

    // ── Service charge rules ─────────────────────────────────────────────────

    [HttpGet("service-charges")]
    public Task<IActionResult> GetServiceCharges([FromQuery] Guid? outletId) => Run(async () =>
        await db.ServiceChargeRules.ForTenant(tenant)
            .WhereIf(outletId.HasValue, r => r.OutletId == null || r.OutletId == outletId)
            .OrderBy(r => r.Priority)
            .Select(r => RestaurantMapper.ToDto(r))
            .ToListAsync());

    [HttpPost("service-charges")]
    public Task<IActionResult> SaveServiceCharge([FromBody] ServiceChargeRuleDto request) => Run(async () =>
    {
        var rule = request.Id == Guid.Empty
            ? null
            : await db.ServiceChargeRules.ForTenant(tenant).FirstOrDefaultAsync(r => r.Id == request.Id);

        if (rule is null)
        {
            rule = new ServiceChargeRule().StampNew(tenant, UserId);
            db.ServiceChargeRules.Add(rule);
        }
        else rule.StampUpdated(UserId);

        rule.OutletId = request.OutletId;
        rule.Name = request.Name;
        rule.Basis = request.Basis;
        rule.Value = request.Value;
        rule.MinPartySize = request.MinPartySize;
        rule.ApplicableOrderTypes = request.ApplicableOrderTypes;
        rule.IsTaxable = request.IsTaxable;
        rule.TaxGroupId = request.TaxGroupId;
        rule.IsWaivable = request.IsWaivable;
        rule.RequiresApprovalToWaive = request.RequiresApprovalToWaive;
        rule.Priority = request.Priority;
        rule.IsActive = request.IsActive;

        await db.SaveChangesAsync();
        return RestaurantMapper.ToDto(rule);
    }, "Service charge saved.");

    [HttpDelete("service-charges/{id:guid}")]
    public Task<IActionResult> DeleteServiceCharge(Guid id) => Run(async () =>
    {
        var rule = await db.ServiceChargeRules.ForTenant(tenant).FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new InvalidOperationException("Service charge rule not found.");

        rule.StampDeleted(UserId);
        await db.SaveChangesAsync();
    }, "Service charge removed.");
}
