using Fitness.Application.DTOs;
using Fitness.Application.Services.Interfaces;
using Fitness.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;

namespace Fitness.Api.Controllers;

/// <summary>
/// Membership agreements, and everything that happens to one after signing: plan changes,
/// freezes, suspensions and cancellation.
///
/// Every costly action has a <c>preview</c> beside it that returns the arithmetic *and* a
/// plain-English explanation. A member asking "what will it cost me to freeze for six weeks"
/// deserves a number and a sentence, not a shrug — and a receptionist who cannot answer that is
/// how a freeze turns into a cancellation.
/// </summary>
[Route("api/fitness/agreements")]
public class AgreementController(
    IAgreementService agreements,
    ILogger<AgreementController> logger) : FitnessControllerBase(logger)
{
    [HttpGet]
    public Task<IActionResult> List(
        [FromQuery] Guid? clubId, [FromQuery] Guid? memberId, [FromQuery] AgreementStatus? status,
        [FromQuery] Guid? planId, [FromQuery] DateTime? endingBefore,
        [FromQuery] PaginationParams? pagination = null)
        => RunPaged(() => agreements.ListAsync(clubId, memberId, status, planId, endingBefore,
            pagination ?? new PaginationParams()));

    [HttpGet("{id:guid}")]
    public Task<IActionResult> Get(Guid id)
        => RunFound(() => agreements.GetAsync(id), "Agreement not found.");

    /// <summary>Creates an agreement, freezing the plan's terms onto it and building the billing schedule.</summary>
    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateAgreementDto request)
        => Run(() => agreements.CreateAsync(request, UserId), "Agreement created.");

    [HttpPost("sign")]
    public Task<IActionResult> Sign([FromBody] SignAgreementDto request)
        => Run(() => agreements.SignAsync(request, UserId), "Agreement signed.");

    /// <summary>Emails a signing link, for a member who joined over the phone.</summary>
    [HttpPost("sign/remote")]
    public Task<IActionResult> RequestRemoteSignature([FromBody] RequestRemoteSignatureDto request)
        => Run(() => agreements.RequestRemoteSignatureAsync(request, UserId), "Signing link sent.");

    // ── Plan changes ─────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/plan-change/preview")]
    public Task<IActionResult> PreviewPlanChange(
        Guid id, [FromQuery] Guid newPlanId, [FromQuery] DateTime? effectiveOn)
        => Run(() => agreements.PreviewPlanChangeAsync(id, newPlanId, effectiveOn));

    [HttpPost("plan-change")]
    public Task<IActionResult> ChangePlan([FromBody] ChangePlanDto request)
        => Run(() => agreements.ChangePlanAsync(request, UserId), "Plan changed.");

    // ── Freezes ──────────────────────────────────────────────────────────────

    /// <summary>The cost, the new end date and the days used, before anything is committed.</summary>
    [HttpPost("freezes/preview")]
    public Task<IActionResult> PreviewFreeze([FromBody] RequestFreezeDto request)
        => Run(() => agreements.PreviewFreezeAsync(request));

    [HttpPost("freezes")]
    public Task<IActionResult> Freeze([FromBody] RequestFreezeDto request)
        => Run(() => agreements.FreezeAsync(request, UserId), "Membership frozen.");

    [HttpPost("freezes/end")]
    public Task<IActionResult> EndFreeze([FromBody] EndFreezeDto request)
        => Run(() => agreements.EndFreezeAsync(request, UserId), "Membership resumed.");

    [HttpGet("freezes")]
    public Task<IActionResult> GetFreezes(
        [FromQuery] Guid? clubId, [FromQuery] Guid? memberId, [FromQuery] bool activeOnly = true)
        => Run(() => agreements.GetFreezesAsync(clubId, memberId, activeOnly));

    /// <summary>Nightly job: brings back everyone whose freeze ended overnight.</summary>
    [HttpPost("freezes/release-due")]
    public Task<IActionResult> ReleaseDueFreezes()
        => Run(agreements.ReleaseDueFreezesAsync);

    // ── Suspensions ──────────────────────────────────────────────────────────

    /// <summary>A suspension is the club's decision; a freeze is the member's. They are not the same thing.</summary>
    [HttpPost("suspensions")]
    public Task<IActionResult> Suspend([FromBody] SuspendMemberDto request)
        => Run(() => agreements.SuspendAsync(request, UserId), "Membership suspended.");

    [HttpPost("suspensions/{id:guid}/lift")]
    public Task<IActionResult> LiftSuspension(Guid id, [FromQuery] string? reason = null)
        => Run(() => agreements.LiftSuspensionAsync(id, reason, UserId), "Suspension lifted.");

    [HttpPost("suspensions/lift-resolved")]
    public Task<IActionResult> LiftResolved()
        => Run(agreements.LiftResolvedSuspensionsAsync);

    // ── Leaving ──────────────────────────────────────────────────────────────

    /// <summary>
    /// What cancelling actually means for this member: last day, notice, any termination fee,
    /// and the save offers most likely to work on someone in their position.
    /// </summary>
    [HttpGet("{id:guid}/cancellation/preview")]
    public Task<IActionResult> PreviewCancellation(Guid id, [FromQuery] DateTime? requestedEffectiveOn)
        => Run(() => agreements.PreviewCancellationAsync(id, requestedEffectiveOn));

    [HttpPost("cancellation")]
    public Task<IActionResult> RequestCancellation([FromBody] RequestCancellationDto request)
        => Run(() => agreements.RequestCancellationAsync(request, UserId), "Cancellation logged.");

    [HttpPost("cancellation/offer")]
    public Task<IActionResult> MakeSaveOffer([FromBody] MakeSaveOfferDto request)
        => Run(() => agreements.MakeSaveOfferAsync(request, UserId), "Offer made.");

    [HttpPost("cancellation/offer/respond")]
    public Task<IActionResult> RespondToOffer([FromBody] RespondToSaveOfferDto request)
        => Run(() => agreements.RespondToOfferAsync(request, UserId), "Response recorded.");

    [HttpPost("cancellation/{id:guid}/process")]
    public Task<IActionResult> ProcessCancellation(Guid id)
        => Run(() => agreements.ProcessCancellationAsync(id, UserId), "Cancellation processed.");

    [HttpGet("cancellation/pending")]
    public Task<IActionResult> GetPendingCancellations([FromQuery] Guid? clubId)
        => Run(() => agreements.GetPendingCancellationsAsync(clubId));

    /// <summary>Nightly job: ends the agreements whose notice period ran out today.</summary>
    [HttpPost("cancellation/process-due")]
    public Task<IActionResult> ProcessDueCancellations()
        => Run(agreements.ProcessDueCancellationsAsync);
}
