using Distribution.Application.DTOs;
using Distribution.Application.Services.Interfaces;
using Distribution.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;

namespace Distribution.Api.Controllers;

/// <summary>Credit control, collections and cheques.</summary>
[Route("api/distribution/credit")]
public class CreditController(ICreditService service, ILogger<CreditController> logger)
    : DistributionControllerBase(logger)
{
    /// <summary>The credit position, small enough to sync to a phone for offline enforcement.</summary>
    [HttpGet("snapshot")]
    public Task<IActionResult> Snapshot([FromQuery] Guid? outletId, [FromQuery] Guid? partnerId)
        => Run(() => service.GetSnapshotAsync(outletId, partnerId));

    [HttpGet("check")]
    public Task<IActionResult> Check(
        [FromQuery] Guid? outletId, [FromQuery] Guid? partnerId,
        [FromQuery] decimal orderValue, [FromQuery] CreditEnforcement? enforcement)
        => Run(() => service.CheckAsync(outletId, partnerId, orderValue, enforcement));

    [HttpGet("profiles")]
    public Task<IActionResult> Profiles(
        [FromQuery] string? search, [FromQuery] Guid? territoryId, [FromQuery] Guid? routeId,
        [FromQuery] Guid? partnerId, [FromQuery] bool? overdueOnly, [FromQuery] bool? blockedOnly,
        [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListProfilesAsync(
            search, territoryId, routeId, partnerId, overdueOnly, blockedOnly,
            pagination ?? new PaginationParams()));

    [HttpPost("limit")]
    public Task<IActionResult> SetLimit([FromBody] SetCreditLimitDto request)
        => Run(() => service.SetLimitAsync(request, UserId), "Credit limit saved.");

    [HttpPost("recalculate")]
    public Task<IActionResult> Recalculate([FromQuery] Guid? outletId, [FromQuery] Guid? partnerId)
        => Run(() => service.RecalculateAsync(outletId, partnerId), "Position recalculated.");

    [HttpPost("block")]
    public Task<IActionResult> Block(
        [FromQuery] Guid? outletId, [FromQuery] Guid? partnerId,
        [FromQuery] bool isBlocked = true, [FromQuery] string? reason = null)
        => Run(() => service.BlockAsync(outletId, partnerId, isBlocked, reason, UserId),
            isBlocked ? "Account blocked." : "Account unblocked.");

    [HttpPost("overrides")]
    public Task<IActionResult> RequestOverride([FromBody] RequestCreditOverrideDto request)
        => Run(() => service.RequestOverrideAsync(request, UserId), "Override requested.");

    [HttpPost("overrides/{id:guid}/decide")]
    public Task<IActionResult> DecideOverride(Guid id, [FromBody] DecideCreditOverrideDto request)
        => Run(() => service.DecideOverrideAsync(id, request, UserId), "Decision recorded.");

    [HttpGet("overrides")]
    public Task<IActionResult> Overrides(
        [FromQuery] Guid? outletId, [FromQuery] Guid? partnerId, [FromQuery] bool? pendingOnly)
        => Run(() => service.ListOverridesAsync(outletId, partnerId, pendingOnly));

    // ── Collections ──────────────────────────────────────────────────────────

    [HttpPost("collections")]
    public Task<IActionResult> RecordCollection([FromBody] RecordCollectionDto request)
        => Run(() => service.RecordCollectionAsync(request, UserId), "Collection recorded.");

    [HttpGet("collections/{id:guid}")]
    public Task<IActionResult> GetCollection(Guid id)
        => RunFound(() => service.GetCollectionAsync(id), "That receipt no longer exists.");

    [HttpGet("collections")]
    public Task<IActionResult> ListCollections(
        [FromQuery] Guid? outletId, [FromQuery] Guid? partnerId, [FromQuery] Guid? fieldRepId,
        [FromQuery] Guid? fieldDayId, [FromQuery] PaymentTender? tender, [FromQuery] DateTime? from,
        [FromQuery] DateTime? to, [FromQuery] bool? undepositedOnly, [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListCollectionsAsync(
            outletId, partnerId, fieldRepId, fieldDayId, tender, from, to, undepositedOnly,
            pagination ?? new PaginationParams()));

    [HttpPost("collections/{id:guid}/reverse")]
    public Task<IActionResult> ReverseCollection(Guid id, [FromQuery] string reason)
        => Run(() => service.ReverseCollectionAsync(id, reason, UserId), "Receipt reversed.");

    // ── Cheques ──────────────────────────────────────────────────────────────

    [HttpGet("cheques")]
    public Task<IActionResult> Cheques(
        [FromQuery] ChequeStatus? status, [FromQuery] Guid? outletId, [FromQuery] DateTime? from,
        [FromQuery] DateTime? to, [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListChequesAsync(status, outletId, from, to, pagination ?? new PaginationParams()));

    /// <summary>A bounce reverses the allocation, charges the fee and blocks the outlet.</summary>
    [HttpPost("cheques/{id:guid}/status")]
    public Task<IActionResult> UpdateCheque(Guid id, [FromBody] UpdateChequeStatusDto request)
        => Run(() => service.UpdateChequeStatusAsync(id, request, UserId), "Cheque updated.");
}

/// <summary>Claims, supplier rebates and chargebacks.</summary>
[Route("api/distribution/claims")]
public class ClaimController(IClaimService service, ILogger<ClaimController> logger)
    : DistributionControllerBase(logger)
{
    [HttpPost]
    public Task<IActionResult> Submit([FromBody] SubmitClaimDto request)
        => Run(() => service.SubmitAsync(request, UserId), "Claim submitted.");

    [HttpGet("{id:guid}")]
    public Task<IActionResult> Get(Guid id)
        => RunFound(() => service.GetAsync(id), "That claim no longer exists.");

    [HttpGet]
    public Task<IActionResult> List(
        [FromQuery] string? search, [FromQuery] ClaimKind? kind, [FromQuery] ClaimStatus? status,
        [FromQuery] Guid? partnerId, [FromQuery] Guid? schemeId, [FromQuery] DateTime? from,
        [FromQuery] DateTime? to, [FromQuery] bool? breachingSlaOnly, [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListAsync(
            search, kind, status, partnerId, schemeId, from, to, breachingSlaOnly,
            pagination ?? new PaginationParams()));

    [HttpPost("{id:guid}/review")]
    public Task<IActionResult> StartReview(Guid id)
        => Run(() => service.StartReviewAsync(id, UserId), "Review started.");

    [HttpPost("{id:guid}/query")]
    public Task<IActionResult> Query(Guid id, [FromBody] QueryClaimDto request)
        => Run(() => service.QueryAsync(id, request, UserId), "Query raised.");

    [HttpPost("{id:guid}/resubmit")]
    public Task<IActionResult> Resubmit(Guid id, [FromBody] SubmitClaimDto request)
        => Run(() => service.ResubmitAsync(id, request, UserId), "Claim resubmitted.");

    [HttpPost("{id:guid}/decide")]
    public Task<IActionResult> Decide(Guid id, [FromBody] DecideClaimDto request)
        => Run(() => service.DecideAsync(id, request, UserId), "Decision recorded.");

    [HttpPost("{id:guid}/settle")]
    public Task<IActionResult> Settle(Guid id, [FromBody] SettleClaimDto request)
        => Run(() => service.SettleAsync(id, request, UserId), "Claim settled.");

    [HttpPost("{id:guid}/cancel")]
    public Task<IActionResult> Cancel(Guid id, [FromQuery] string reason)
        => Run(() => service.CancelAsync(id, reason, UserId), "Claim cancelled.");

    [HttpPost("{id:guid}/documents")]
    public Task<IActionResult> AddDocument(Guid id, [FromBody] ClaimDocumentDto request)
        => Run(() => service.AddDocumentAsync(id, request, UserId), "Document attached.");

    // ── Supplier rebates ─────────────────────────────────────────────────────

    [HttpGet("rebates")]
    public Task<IActionResult> Rebates(
        [FromQuery] string? search, [FromQuery] Guid? supplierId,
        [FromQuery] bool? activeOnly, [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListRebatesAsync(
            search, supplierId, activeOnly, pagination ?? new PaginationParams()));

    [HttpGet("rebates/{id:guid}")]
    public Task<IActionResult> GetRebate(Guid id)
        => RunFound(() => service.GetRebateAsync(id), "That agreement no longer exists.");

    [HttpPost("rebates")]
    public Task<IActionResult> CreateRebate([FromBody] RebateAgreementDto request)
        => Run(() => service.SaveRebateAsync(null, request, UserId), "Agreement created.");

    [HttpPut("rebates/{id:guid}")]
    public Task<IActionResult> UpdateRebate(Guid id, [FromBody] RebateAgreementDto request)
        => Run(() => service.SaveRebateAsync(id, request, UserId), "Agreement saved.");

    [HttpPost("rebates/accrue")]
    public Task<IActionResult> Accrue([FromQuery] DateTime periodStart, [FromQuery] DateTime periodEnd)
        => Run(() => service.AccrueRebatesAsync(periodStart, periodEnd, UserId), "Rebates accrued.");

    [HttpPost("rebates/accruals/{id:guid}/reconcile")]
    public Task<IActionResult> ReconcileAccrual(
        Guid id, [FromQuery] decimal receivedAmount, [FromQuery] string? reference)
        => Run(() => service.ReconcileAccrualAsync(id, receivedAmount, reference, UserId), "Accrual reconciled.");

    // ── Chargebacks ──────────────────────────────────────────────────────────

    [HttpGet("chargebacks")]
    public Task<IActionResult> Chargebacks(
        [FromQuery] ClaimStatus? status, [FromQuery] Guid? supplierId, [FromQuery] DateTime? from,
        [FromQuery] DateTime? to, [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListChargebacksAsync(
            status, supplierId, from, to, pagination ?? new PaginationParams()));

    [HttpPost("chargebacks")]
    public Task<IActionResult> CreateChargeback([FromBody] ChargebackDto request)
        => Run(() => service.SaveChargebackAsync(null, request, UserId), "Chargeback created.");

    [HttpPut("chargebacks/{id:guid}")]
    public Task<IActionResult> UpdateChargeback(Guid id, [FromBody] ChargebackDto request)
        => Run(() => service.SaveChargebackAsync(id, request, UserId), "Chargeback saved.");

    [HttpPost("chargebacks/{id:guid}/settle")]
    public Task<IActionResult> SettleChargeback(
        Guid id, [FromQuery] decimal settledAmount, [FromQuery] string? reference)
        => Run(() => service.SettleChargebackAsync(id, settledAmount, reference, UserId), "Chargeback settled.");
}

/// <summary>Route settlement and cash deposits.</summary>
[Route("api/distribution/settlement")]
public class SettlementController(ISettlementService service, ILogger<SettlementController> logger)
    : DistributionControllerBase(logger)
{
    /// <summary>The settlement clerk's queue: which routes are still open, and how badly.</summary>
    [HttpGet("board")]
    public Task<IActionResult> Board([FromQuery] DateTime? date, [FromQuery] Guid? territoryId)
        => Run(() => service.GetBoardAsync(date, territoryId));

    [HttpPost("open")]
    public Task<IActionResult> Open([FromBody] OpenSettlementDto request)
        => Run(() => service.OpenAsync(request, UserId), "Settlement opened.");

    [HttpGet("{id:guid}")]
    public Task<IActionResult> Get(Guid id)
        => RunFound(() => service.GetAsync(id), "That settlement no longer exists.");

    [HttpPost("{id:guid}/recompute")]
    public Task<IActionResult> Recompute(Guid id)
        => Run(() => service.RecomputeAsync(id, UserId), "Settlement recomputed.");

    [HttpPost("submit")]
    public Task<IActionResult> Submit([FromBody] SubmitSettlementDto request)
        => Run(() => service.SubmitAsync(request, UserId), "Settlement submitted.");

    /// <summary>The gate: a route cannot close until every variance carries a reason.</summary>
    [HttpPost("variance")]
    public Task<IActionResult> ExplainVariance([FromBody] ExplainVarianceDto request)
        => Run(() => service.ExplainVarianceAsync(request, UserId), "Variance explained.");

    [HttpPost("approve")]
    public Task<IActionResult> Approve([FromBody] ApproveSettlementDto request)
        => Run(() => service.ApproveAsync(request, UserId), "Decision recorded.");

    [HttpPost("{id:guid}/close")]
    public Task<IActionResult> Close(Guid id)
        => Run(() => service.CloseAsync(id, UserId), "Settlement closed.");

    [HttpPost("{id:guid}/reverse")]
    public Task<IActionResult> Reverse(Guid id, [FromBody] ReverseSettlementDto request)
        => Run(() => service.ReverseAsync(id, request, UserId), "Settlement reversed.");

    [HttpGet]
    public Task<IActionResult> List(
        [FromQuery] Guid? fieldRepId, [FromQuery] Guid? routeId, [FromQuery] SettlementStatus? status,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListAsync(
            fieldRepId, routeId, status, from, to, pagination ?? new PaginationParams()));

    [HttpPost("deposits")]
    public Task<IActionResult> RecordDeposit([FromBody] RecordDepositDto request)
        => Run(() => service.RecordDepositAsync(request, UserId), "Deposit recorded.");

    [HttpPost("deposits/{id:guid}/reconcile")]
    public Task<IActionResult> ReconcileDeposit(Guid id)
        => Run(() => service.ReconcileDepositAsync(id, UserId), "Deposit reconciled.");

    [HttpGet("deposits")]
    public Task<IActionResult> Deposits(
        [FromQuery] Guid? fieldRepId, [FromQuery] bool? unreconciledOnly, [FromQuery] DateTime? from,
        [FromQuery] DateTime? to, [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListDepositsAsync(
            fieldRepId, unreconciledOnly, from, to, pagination ?? new PaginationParams()));
}
