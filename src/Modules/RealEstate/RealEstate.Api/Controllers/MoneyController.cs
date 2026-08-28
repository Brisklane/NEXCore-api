using Microsoft.AspNetCore.Mvc;
using RealEstate.Application.DTOs;
using RealEstate.Application.Services.Interfaces;
using RealEstate.Domain.Enums;

namespace RealEstate.Api.Controllers;

/// <summary>
/// Payment plans, surcharge, demands, receipts, cheques, the ledger, collections and dunning.
///
/// Allocation is the part people argue about. A receipt is spread across what is owed in a
/// declared order — surcharge first, then oldest — and the preview shows exactly where each unit
/// of currency lands before anything is posted. Reallocating afterwards is possible but needs a
/// reason, because it moves money between a customer's own buckets and somebody will ask why.
/// </summary>
[Route("api/realestate/money")]
public class MoneyController(
    IMoneyService money,
    ILogger<MoneyController> logger) : RealEstateControllerBase(logger)
{
    // ── Payment plans ────────────────────────────────────────────────────────

    [HttpGet("plan-templates")]
    public Task<IActionResult> GetTemplates([FromQuery] Guid? projectId, [FromQuery] bool activeOnly = true)
        => Run(() => money.GetTemplatesAsync(projectId, activeOnly));

    [HttpGet("plan-templates/{id:guid}")]
    public Task<IActionResult> GetTemplate(Guid id)
        => RunFound(() => money.GetTemplateAsync(id), "That template does not exist.");

    [HttpPost("plan-templates")]
    public Task<IActionResult> SaveTemplate([FromBody] PaymentPlanTemplateDto request)
        => Run(() => money.SaveTemplateAsync(request, UserId), "Template saved.");

    /// <summary>The schedule a template would produce, dated and totalled, before anybody commits.</summary>
    [HttpPost("plans/preview")]
    public Task<IActionResult> PreviewPlan(
        [FromQuery] Guid? templateId,
        [FromQuery] decimal totalConsideration,
        [FromQuery] DateOnly startDate,
        [FromQuery] Guid? projectId,
        [FromBody] PaymentPlanCustomDto? custom = null)
        => Run(() => money.PreviewPlanAsync(templateId, custom, totalConsideration, startDate, projectId));

    [HttpGet("plans/{bookingId:guid}")]
    public Task<IActionResult> GetPlan(Guid bookingId)
        => RunFound(() => money.GetPlanAsync(bookingId), "That booking has no payment plan.");

    /// <summary>
    /// Rebuilds the remaining schedule. What has already been paid is never touched — a
    /// restructure changes the future, not the history.
    /// </summary>
    [HttpPost("plans/restructure")]
    public Task<IActionResult> Restructure([FromBody] PlanRestructureDto request)
        => Run(() => money.RestructureAsync(request, UserId), "Plan restructured.");

    // ── Surcharge ────────────────────────────────────────────────────────────

    [HttpGet("surcharge-policies")]
    public Task<IActionResult> GetSurchargePolicies([FromQuery] Guid? projectId)
        => Run(() => money.GetSurchargePoliciesAsync(projectId));

    [HttpPost("surcharge-policies")]
    public Task<IActionResult> SaveSurchargePolicy([FromBody] SurchargePolicyDto request)
        => Run(() => money.SaveSurchargePolicyAsync(request, UserId), "Policy saved.");

    [HttpPost("surcharge/accrue")]
    public Task<IActionResult> Accrue([FromQuery] DateOnly? asOf)
        => Run(() => money.AccrueSurchargeAsync(asOf ?? DateOnly.FromDateTime(DateTime.UtcNow)),
            "Surcharge accrued.");

    [HttpPost("surcharge/waivers")]
    public Task<IActionResult> RequestWaiver([FromBody] SurchargeWaiverRequestDto request)
        => Run(() => money.RequestWaiverAsync(request, UserId), "Waiver requested.");

    [HttpPost("surcharge/waivers/{id:guid}/decide")]
    public Task<IActionResult> DecideWaiver(
        Guid id,
        [FromQuery] ApprovalOutcome outcome,
        [FromQuery] decimal? approvedAmount,
        [FromQuery] string? comment)
        => Run(() => money.DecideWaiverAsync(id, outcome, approvedAmount, comment, UserId), "Decision recorded.");

    [HttpGet("surcharge/waivers")]
    public Task<IActionResult> GetWaivers([FromQuery] ListQueryDto query, [FromQuery] ApprovalOutcome? outcome)
        => RunPaged(() => money.GetWaiversAsync(query, outcome));

    // ── Demands ──────────────────────────────────────────────────────────────

    [HttpPost("demands/run")]
    public Task<IActionResult> RunDemands([FromBody] DemandRunRequestDto request)
        => Run(() => money.RunDemandsAsync(request, UserId), "Demands raised.");

    [HttpGet("demand-batches")]
    public Task<IActionResult> GetBatches([FromQuery] ListQueryDto query)
        => RunPaged(() => money.GetDemandBatchesAsync(query));

    [HttpGet("demands")]
    public Task<IActionResult> GetDemands(
        [FromQuery] ListQueryDto query, [FromQuery] Guid? bookingId, [FromQuery] DemandStatus? status)
        => RunPaged(() => money.GetDemandsAsync(query, bookingId, status));

    [HttpGet("demands/{id:guid}")]
    public Task<IActionResult> GetDemand(Guid id)
        => RunFound(() => money.GetDemandAsync(id), "That demand does not exist.");

    [HttpPost("demands/{id:guid}/send")]
    public Task<IActionResult> SendDemand(Guid id, [FromBody] List<NotificationChannel> channels)
        => Run(() => money.SendDemandAsync(id, channels, UserId), "Demand sent.");

    [HttpPost("demands/{id:guid}/cancel")]
    public Task<IActionResult> CancelDemand(Guid id, [FromQuery] Guid reasonCodeId)
        => Run(() => money.CancelDemandAsync(id, reasonCodeId, UserId), "Demand cancelled.");

    // ── Receipts ─────────────────────────────────────────────────────────────

    /// <summary>Where this money would land, line by line, before it is posted.</summary>
    [HttpPost("receipts/preview")]
    public Task<IActionResult> PreviewAllocation([FromBody] ReceiptCreateDto request)
        => Run(() => money.PreviewAllocationAsync(request));

    [HttpPost("receipts")]
    public Task<IActionResult> CreateReceipt([FromBody] ReceiptCreateDto request)
        => Run(() => money.CreateReceiptAsync(request, UserId), "Receipt posted.");

    [HttpPost("receipts/search")]
    public Task<IActionResult> SearchReceipts([FromBody] ReceiptSearchDto query)
        => RunPaged(() => money.GetReceiptsAsync(query));

    [HttpGet("receipts/{id:guid}")]
    public Task<IActionResult> GetReceipt(Guid id)
        => RunFound(() => money.GetReceiptAsync(id), "That receipt does not exist.");

    [HttpPost("receipts/{id:guid}/reverse")]
    public Task<IActionResult> ReverseReceipt(
        Guid id, [FromQuery] Guid reasonCodeId, [FromQuery] string? note)
        => Run(() => money.ReverseReceiptAsync(id, reasonCodeId, note, UserId), "Receipt reversed.");

    [HttpPost("receipts/{id:guid}/reallocate")]
    public Task<IActionResult> Reallocate(
        Guid id, [FromQuery] string reason, [FromBody] List<ManualAllocationDto> allocations)
        => Run(() => money.ReallocateAsync(id, allocations, reason, UserId), "Reallocated.");

    // ── Cheques ──────────────────────────────────────────────────────────────

    [HttpGet("cheques")]
    public Task<IActionResult> GetCheques([FromQuery] ListQueryDto query, [FromQuery] ChequeState? state)
        => RunPaged(() => money.GetChequesAsync(query, state));

    [HttpPost("cheques/state")]
    public Task<IActionResult> ChangeChequeState([FromBody] ChequeStateChangeDto request)
        => Run(() => money.ChangeChequeStateAsync(request, UserId), "Cheque updated.");

    /// <summary>Post-dated cheques by the day they fall due, so none is banked late or early.</summary>
    [HttpGet("cheques/maturity")]
    public Task<IActionResult> GetMaturity([FromQuery] DateOnly from, [FromQuery] DateOnly to)
        => Run(() => money.GetMaturityCalendarAsync(from, to));

    // ── Ledger and statements ────────────────────────────────────────────────

    [HttpGet("ledger")]
    public Task<IActionResult> GetLedger(
        [FromQuery] Guid? bookingId, [FromQuery] Guid? partyId, [FromQuery] ListQueryDto query)
        => RunPaged(() => money.GetLedgerAsync(bookingId, partyId, query));

    [HttpGet("statements/{bookingId:guid}")]
    public Task<IActionResult> GetStatement(
        Guid bookingId, [FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
        => Run(() => money.GetStatementAsync(bookingId, from, to));

    // ── Collections ──────────────────────────────────────────────────────────

    [HttpPost("worklist")]
    public Task<IActionResult> GetWorklist([FromBody] CollectionQueryDto query)
        => Run(() => money.GetWorklistAsync(query));

    [HttpPost("promises")]
    public Task<IActionResult> RecordPromise([FromBody] PromiseToPayCreateDto request)
        => Run(() => money.RecordPromiseAsync(request, UserId), "Promise recorded.");

    [HttpGet("promises")]
    public Task<IActionResult> GetPromises([FromQuery] DateOnly? dueOn, [FromQuery] PromiseState? state)
        => Run(() => money.GetPromisesAsync(dueOn, state));

    /// <summary>Marks promises kept or broken by what actually arrived.</summary>
    [HttpPost("promises/evaluate")]
    public Task<IActionResult> EvaluatePromises()
        => Run(money.EvaluatePromisesAsync, "Promises evaluated.");

    // ── Dunning ──────────────────────────────────────────────────────────────

    [HttpGet("dunning-policies")]
    public Task<IActionResult> GetDunningPolicies([FromQuery] string? appliesTo)
        => Run(() => money.GetDunningPoliciesAsync(appliesTo));

    [HttpPost("dunning-policies")]
    public Task<IActionResult> SaveDunningPolicy([FromBody] DunningPolicyDto request)
        => Run(() => money.SaveDunningPolicyAsync(request, UserId), "Policy saved.");

    [HttpGet("dunning")]
    public Task<IActionResult> GetDunningCases([FromQuery] ListQueryDto query, [FromQuery] bool openOnly = true)
        => RunPaged(() => money.GetDunningCasesAsync(query, openOnly));

    [HttpGet("dunning/{id:guid}")]
    public Task<IActionResult> GetDunningCase(Guid id)
        => RunFound(() => money.GetDunningCaseAsync(id), "That case does not exist.");

    /// <summary>
    /// Pauses the escalation ladder — usually because a promise has been made, or the customer is
    /// in hospital. It needs a reason, because a suspended case is a case nobody is chasing.
    /// </summary>
    [HttpPost("dunning/{id:guid}/suspend")]
    public Task<IActionResult> SuspendDunning(
        Guid id, [FromQuery] DateOnly? until, [FromQuery] string reason)
        => Run(() => money.SuspendDunningAsync(id, until, reason, UserId), "Case suspended.");

    [HttpPost("dunning/run")]
    public Task<IActionResult> RunDunning([FromQuery] DateOnly? asOf)
        => Run(() => money.RunDunningAsync(asOf ?? DateOnly.FromDateTime(DateTime.UtcNow)), "Dunning run.");

    // ── Notices and write-offs ───────────────────────────────────────────────

    [HttpPost("notices")]
    public Task<IActionResult> IssueNotice(
        [FromQuery] Guid? bookingId,
        [FromQuery] Guid? tenancyId,
        [FromQuery] string noticeType,
        [FromQuery] DateOnly? complyBy,
        [FromQuery] Guid? templateId)
        => Run(() => money.IssueNoticeAsync(bookingId, tenancyId, noticeType, complyBy, templateId, UserId),
            "Notice issued.");

    /// <summary>
    /// Records how and when a notice was served. Statutory periods run from service, not from
    /// issue, so this date is the one that decides whether anything downstream is valid.
    /// </summary>
    [HttpPost("notices/{id:guid}/service")]
    public Task<IActionResult> RecordService(
        Guid id,
        [FromQuery] string method,
        [FromQuery] string? reference,
        [FromQuery] string? evidenceUrl,
        [FromQuery] DateOnly servedOn)
        => Run(() => money.RecordServiceAsync(id, method, reference, evidenceUrl, servedOn, UserId),
            "Service recorded.");

    [HttpGet("notices")]
    public Task<IActionResult> GetNotices([FromQuery] ListQueryDto query)
        => RunPaged(() => money.GetNoticesAsync(query));

    [HttpPost("write-offs")]
    public Task<IActionResult> RequestWriteOff([FromBody] WriteOffDto request)
        => Run(() => money.RequestWriteOffAsync(request, UserId), "Write-off requested.");

    [HttpPost("write-offs/{id:guid}/decide")]
    public Task<IActionResult> DecideWriteOff(
        Guid id, [FromQuery] ApprovalOutcome outcome, [FromQuery] string? comment)
        => Run(() => money.DecideWriteOffAsync(id, outcome, comment, UserId), "Decision recorded.");
}
