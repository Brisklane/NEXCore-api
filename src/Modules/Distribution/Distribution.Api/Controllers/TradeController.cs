using Distribution.Application.DTOs;
using Distribution.Application.Services.Interfaces;
using Distribution.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;

namespace Distribution.Api.Controllers;

/// <summary>Orders: quoting, capture, approval and allocation.</summary>
[Route("api/distribution/orders")]
public class OrderController(IOrderService service, ILogger<OrderController> logger)
    : DistributionControllerBase(logger)
{
    /// <summary>
    /// Prices a basket without saving it. The field terminal calls this on every quantity change,
    /// so the retailer sees price, scheme, free goods and credit live at the counter.
    /// </summary>
    [HttpPost("quote")]
    public Task<IActionResult> Quote([FromBody] QuoteOrderDto request)
        => Run(() => service.QuoteAsync(request));

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateOrderDto request)
        => Run(() => service.CreateAsync(request, UserId), "Order created.");

    [HttpGet("{id:guid}")]
    public Task<IActionResult> Get(Guid id)
        => RunFound(() => service.GetAsync(id), "That order no longer exists.");

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(Guid id, [FromBody] UpdateOrderDto request)
        => Run(() => service.UpdateAsync(id, request, UserId), "Order saved.");

    [HttpPost("{id:guid}/submit")]
    public Task<IActionResult> Submit(Guid id)
        => Run(() => service.SubmitAsync(id, UserId), "Order submitted.");

    [HttpPost("decide")]
    public Task<IActionResult> Decide([FromBody] OrderDecisionDto request)
        => Run(() => service.DecideAsync(request, UserId), "Decision recorded.");

    [HttpPost("{id:guid}/hold")]
    public Task<IActionResult> Hold(Guid id, [FromQuery] string reason)
        => Run(() => service.HoldAsync(id, reason, UserId), "Order held.");

    [HttpPost("{id:guid}/release")]
    public Task<IActionResult> Release(Guid id)
        => Run(() => service.ReleaseHoldAsync(id, UserId), "Hold released.");

    [HttpPost("{id:guid}/cancel")]
    public Task<IActionResult> Cancel(Guid id, [FromBody] CancelOrderDto request)
        => Run(() => service.CancelAsync(id, request, UserId), "Order cancelled.");

    [HttpGet]
    public Task<IActionResult> List(
        [FromQuery] string? search, [FromQuery] DistributionOrderStatus? status,
        [FromQuery] OrderSource? source, [FromQuery] Guid? outletId, [FromQuery] Guid? partnerId,
        [FromQuery] Guid? routeId, [FromQuery] Guid? fieldRepId, [FromQuery] Guid? warehouseId,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] bool? awaitingApproval,
        [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListAsync(
            search, status, source, outletId, partnerId, routeId, fieldRepId, warehouseId,
            from, to, awaitingApproval, pagination ?? new PaginationParams()));

    /// <summary>The catalogue the terminal renders, priced and stock-checked for this outlet.</summary>
    [HttpGet("catalogue")]
    public Task<IActionResult> Catalogue(
        [FromQuery] Guid? outletId, [FromQuery] Guid? partnerId, [FromQuery] Guid? warehouseId,
        [FromQuery] Guid? vanUnitId, [FromQuery] string? search,
        [FromQuery] Guid? categoryId, [FromQuery] Guid? brandId)
        => Run(() => service.GetCatalogueAsync(
            outletId, partnerId, warehouseId, vanUnitId, search, categoryId, brandId));

    [HttpPost("allocate")]
    public Task<IActionResult> Allocate([FromBody] AllocateOrderDto request)
        => Run(() => service.AllocateAsync(request, UserId), "Stock allocated.");

    [HttpPost("{id:guid}/release-allocation")]
    public Task<IActionResult> ReleaseAllocation(Guid id)
        => Run(() => service.ReleaseAllocationAsync(id, UserId), "Allocation released.");

    [HttpPost("consolidate")]
    public Task<IActionResult> Consolidate([FromBody] List<Guid> orderIds)
        => Run(() => service.ConsolidateAsync(orderIds, UserId), "Orders consolidated.");
}

/// <summary>Price lists, the margin ladder and MRP history.</summary>
[Route("api/distribution/pricing")]
public class PricingController(IPricingService service, ILogger<PricingController> logger)
    : DistributionControllerBase(logger)
{
    /// <summary>The answer to "why this price" — the winning rule and everything considered.</summary>
    [HttpGet("resolve")]
    public Task<IActionResult> Resolve(
        [FromQuery] Guid itemId, [FromQuery] string uom, [FromQuery] decimal quantity,
        [FromQuery] Guid? outletId, [FromQuery] Guid? partnerId, [FromQuery] DateTime? asOf)
        => Run(() => service.ResolvePriceAsync(itemId, uom, quantity, outletId, partnerId, asOf));

    [HttpGet]
    public Task<IActionResult> List(
        [FromQuery] string? search, [FromQuery] PriceScope? scope, [FromQuery] Guid? partnerId,
        [FromQuery] bool? activeOnly, [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListPriceListsAsync(
            search, scope, partnerId, activeOnly, pagination ?? new PaginationParams()));

    [HttpGet("{id:guid}")]
    public Task<IActionResult> Get(Guid id)
        => RunFound(() => service.GetPriceListAsync(id), "That price list no longer exists.");

    [HttpPost]
    public Task<IActionResult> Create([FromBody] SavePriceListDto request)
        => Run(() => service.SavePriceListAsync(null, request, UserId), "Price list created.");

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(Guid id, [FromBody] SavePriceListDto request)
        => Run(() => service.SavePriceListAsync(id, request, UserId), "Price list saved.");

    [HttpPost("{id:guid}/approve")]
    public Task<IActionResult> Approve(Guid id)
        => Run(() => service.ApprovePriceListAsync(id, UserId), "Price list approved.");

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(Guid id)
        => Run(() => service.DeletePriceListAsync(id, UserId), "Price list removed.");

    [HttpGet("margins")]
    public Task<IActionResult> Margins(
        [FromQuery] Guid? itemId, [FromQuery] Guid? partnerId, [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListMarginLaddersAsync(itemId, partnerId, pagination ?? new PaginationParams()));

    [HttpPost("margins")]
    public Task<IActionResult> CreateMargin([FromBody] MarginLadderDto request)
        => Run(() => service.SaveMarginLadderAsync(null, request, UserId), "Margin ladder saved.");

    [HttpPut("margins/{id:guid}")]
    public Task<IActionResult> UpdateMargin(Guid id, [FromBody] MarginLadderDto request)
        => Run(() => service.SaveMarginLadderAsync(id, request, UserId), "Margin ladder saved.");

    [HttpGet("mrp")]
    public Task<IActionResult> MrpRevisions([FromQuery] Guid? itemId, [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListMrpRevisionsAsync(itemId, pagination ?? new PaginationParams()));

    [HttpPost("mrp")]
    public Task<IActionResult> CreateMrp([FromBody] MrpRevisionDto request)
        => Run(() => service.SaveMrpRevisionAsync(null, request, UserId), "MRP revision saved.");
}

/// <summary>Trade schemes: definition, evaluation, budget and performance.</summary>
[Route("api/distribution/schemes")]
public class SchemeController(ISchemeService service, ILogger<SchemeController> logger)
    : DistributionControllerBase(logger)
{
    [HttpGet]
    public Task<IActionResult> List(
        [FromQuery] string? search, [FromQuery] TradeSchemeKind? kind, [FromQuery] SchemeStatus? status,
        [FromQuery] Guid? territoryId, [FromQuery] bool? activeOnly, [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListSchemesAsync(
            search, kind, status, territoryId, activeOnly, pagination ?? new PaginationParams()));

    [HttpGet("{id:guid}")]
    public Task<IActionResult> Get(Guid id)
        => RunFound(() => service.GetSchemeAsync(id), "That scheme no longer exists.");

    [HttpPost]
    public Task<IActionResult> Create([FromBody] SaveTradeSchemeDto request)
        => Run(() => service.SaveSchemeAsync(null, request, UserId), "Scheme created.");

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(Guid id, [FromBody] SaveTradeSchemeDto request)
        => Run(() => service.SaveSchemeAsync(id, request, UserId), "Scheme saved.");

    [HttpPost("{id:guid}/decide")]
    public Task<IActionResult> Decide(Guid id, [FromBody] SchemeDecisionDto request)
        => Run(() => service.DecideSchemeAsync(id, request, UserId), "Decision recorded.");

    [HttpPost("{id:guid}/status")]
    public Task<IActionResult> ChangeStatus(
        Guid id, [FromQuery] SchemeStatus status, [FromQuery] string? reason)
        => Run(() => service.ChangeStatusAsync(id, status, reason, UserId), "Status updated.");

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(Guid id)
        => Run(() => service.DeleteSchemeAsync(id, UserId), "Scheme removed.");

    /// <summary>Schemes live for this outlet right now — the shelf-talker list on the terminal.</summary>
    [HttpGet("applicable")]
    public Task<IActionResult> Applicable(
        [FromQuery] Guid? outletId, [FromQuery] Guid? partnerId, [FromQuery] DateTime? asOf)
        => Run(() => service.GetApplicableSchemesAsync(outletId, partnerId, asOf ?? DateTime.UtcNow));

    [HttpPost("simulate")]
    public Task<IActionResult> Simulate(
        [FromQuery] Guid? schemeId, [FromQuery] DateTime from, [FromQuery] DateTime to,
        [FromBody] SaveTradeSchemeDto? draft)
        => Run(() => service.SimulateAsync(schemeId, draft, from, to));

    [HttpGet("{id:guid}/performance")]
    public Task<IActionResult> Performance(Guid id)
        => Run(() => service.GetPerformanceAsync(id));

    [HttpGet("{id:guid}/budget")]
    public Task<IActionResult> Budget(Guid id)
        => Run(() => service.GetBudgetLedgerAsync(id));

    [HttpPost("{id:guid}/budget")]
    public Task<IActionResult> AdjustBudget(Guid id, [FromQuery] decimal amount, [FromQuery] string reason)
        => Run(() => service.AdjustBudgetAsync(id, amount, reason, UserId), "Budget adjusted.");

    [HttpGet("applications")]
    public Task<IActionResult> Applications(
        [FromQuery] Guid? schemeId, [FromQuery] Guid? outletId, [FromQuery] Guid? partnerId,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListApplicationsAsync(
            schemeId, outletId, partnerId, from, to, pagination ?? new PaginationParams()));

    /// <summary>Raises the claims a deferred scheme has earned. The system already knows the answer.</summary>
    [HttpPost("generate-claims")]
    public Task<IActionResult> GenerateClaims([FromQuery] DateTime periodStart, [FromQuery] DateTime periodEnd)
        => Run(() => service.GenerateDeferredClaimsAsync(periodStart, periodEnd, UserId), "Claims generated.");
}

/// <summary>Van sales: load out, stock, transfers, counting and unloading.</summary>
[Route("api/distribution/vans")]
public class VanController(IVanService service, ILogger<VanController> logger)
    : DistributionControllerBase(logger)
{
    [HttpGet]
    public Task<IActionResult> List(
        [FromQuery] string? search, [FromQuery] Guid? fieldRepId, [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListVansAsync(search, fieldRepId, pagination ?? new PaginationParams()));

    [HttpGet("{id:guid}")]
    public Task<IActionResult> Get(Guid id)
        => RunFound(() => service.GetVanAsync(id), "That van no longer exists.");

    [HttpPost]
    public Task<IActionResult> Create([FromBody] SaveVanUnitDto request)
        => Run(() => service.SaveVanAsync(null, request, UserId), "Van created.");

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(Guid id, [FromBody] SaveVanUnitDto request)
        => Run(() => service.SaveVanAsync(id, request, UserId), "Van saved.");

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(Guid id)
        => Run(() => service.DeleteVanAsync(id, UserId), "Van removed.");

    [HttpGet("{id:guid}/stock")]
    public Task<IActionResult> Stock(Guid id, [FromQuery] VanCompartment? compartment)
        => Run(() => service.GetStockAsync(id, compartment));

    [HttpGet("{id:guid}/movements")]
    public Task<IActionResult> Movements(
        Guid id, [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] VanMovementKind? kind, [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.GetMovementsAsync(id, from, to, kind, pagination ?? new PaginationParams()));

    [HttpPost("loads")]
    public Task<IActionResult> CreateLoad([FromBody] CreateVanLoadDto request)
        => Run(() => service.CreateLoadAsync(request, UserId), "Load sheet created.");

    [HttpGet("loads/{id:guid}")]
    public Task<IActionResult> GetLoad(Guid id)
        => RunFound(() => service.GetLoadAsync(id), "That load sheet no longer exists.");

    [HttpGet("loads")]
    public Task<IActionResult> ListLoads(
        [FromQuery] Guid? vanUnitId, [FromQuery] VanLoadStatus? status, [FromQuery] DateTime? from,
        [FromQuery] DateTime? to, [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListLoadsAsync(
            vanUnitId, status, from, to, pagination ?? new PaginationParams()));

    [HttpPost("loads/{id:guid}/approve")]
    public Task<IActionResult> ApproveLoad(
        Guid id, [FromQuery] bool isApproved = true, [FromQuery] string? reason = null)
        => Run(() => service.ApproveLoadAsync(id, isApproved, reason, UserId),
            isApproved ? "Load approved." : "Load rejected.");

    /// <summary>Moves stock warehouse → van. Any gap between picked and loaded needs a reason.</summary>
    [HttpPost("loads/confirm")]
    public Task<IActionResult> ConfirmLoad([FromBody] ConfirmVanLoadDto request)
        => Run(() => service.ConfirmLoadAsync(request, UserId), "Van loaded.");

    [HttpPost("transfer")]
    public Task<IActionResult> Transfer([FromBody] VanTransferDto request)
        => Run(() => service.TransferAsync(request, UserId), "Stock transferred.");

    [HttpPost("counts/start")]
    public Task<IActionResult> StartCount([FromBody] StartVanCountDto request)
        => Run(() => service.StartCountAsync(request, UserId), "Count started.");

    [HttpPost("counts/submit")]
    public Task<IActionResult> SubmitCount([FromBody] SubmitVanCountDto request)
        => Run(() => service.SubmitCountAsync(request, UserId), "Count submitted.");

    [HttpPost("counts/{id:guid}/approve")]
    public Task<IActionResult> ApproveCount(Guid id)
        => Run(() => service.ApproveCountAsync(id, UserId), "Count approved.");

    [HttpGet("counts/{id:guid}")]
    public Task<IActionResult> GetCount(Guid id)
        => RunFound(() => service.GetCountAsync(id), "That count no longer exists.");

    /// <summary>End-of-day unload: expected against counted, with every variance reasoned.</summary>
    [HttpPost("{id:guid}/unload")]
    public Task<IActionResult> Unload(
        Guid id, [FromQuery] Guid? fieldDayId, [FromBody] SubmitVanCountDto request)
        => Run(() => service.UnloadAsync(id, fieldDayId, request, UserId), "Van unloaded.");
}
