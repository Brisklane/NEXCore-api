using Fitness.Application.DTOs;
using Fitness.Application.Services.Interfaces;
using Fitness.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;

namespace Fitness.Api.Controllers;

/// <summary>
/// Arrears and the dunning ladder.
///
/// A failed payment is usually an expired card, not a refusal to pay — so the ladder retries and
/// asks before it charges a fee, and suspends access only after two weeks. Locking someone out on
/// day two costs more in goodwill than it recovers.
/// </summary>
[Route("api/fitness/collections")]
public class CollectionsController(
    IDunningService dunning,
    ILogger<CollectionsController> logger) : FitnessControllerBase(logger)
{
    [HttpGet("cases")]
    public Task<IActionResult> ListCases(
        [FromQuery] Guid? clubId, [FromQuery] DunningCaseStatus? status,
        [FromQuery] Guid? assignedStaffId, [FromQuery] PaginationParams? pagination = null)
        => RunPaged(() => dunning.ListCasesAsync(clubId, status, assignedStaffId,
            pagination ?? new PaginationParams()));

    [HttpGet("cases/{id:guid}")]
    public Task<IActionResult> GetCase(Guid id)
        => RunFound(() => dunning.GetCaseAsync(id), "Case not found.");

    [HttpPost("cases")]
    public Task<IActionResult> OpenCase(
        [FromQuery] Guid invoiceId, [FromQuery] PaymentFailureReason reason)
        => Run(() => dunning.OpenCaseAsync(invoiceId, reason, UserId), "Case opened.");

    /// <summary>
    /// Retry, pause, resume, assign, log a promise to pay, or write off.
    ///
    /// A promise to pay pauses the ladder until the promised date — chasing someone who has
    /// already told you when they will pay is how a recoverable account becomes a cancellation.
    /// </summary>
    [HttpPost("cases/action")]
    public Task<IActionResult> ActionCase([FromBody] DunningActionDto request)
        => Run(() => dunning.ActionCaseAsync(request, UserId), "Done.");

    /// <summary>Nightly job: runs every ladder step that fell due today.</summary>
    [HttpPost("process-due")]
    public Task<IActionResult> ProcessDue()
        => Run(dunning.ProcessDueStepsAsync);

    // ── Policies ─────────────────────────────────────────────────────────────

    [HttpGet("policies")]
    public Task<IActionResult> GetPolicies([FromQuery] Guid? clubId)
        => Run(() => dunning.GetPoliciesAsync(clubId));

    [HttpPost("policies")]
    public Task<IActionResult> SavePolicy([FromBody] DunningPolicyDto request, [FromQuery] Guid? id = null)
        => Run(() => dunning.SavePolicyAsync(id, request, UserId), "Policy saved.");

    /// <summary>Ageing bands and the breakdown by failure reason — what is owed, and why it failed.</summary>
    [HttpGet("arrears")]
    public Task<IActionResult> GetArrears([FromQuery] Guid? clubId, [FromQuery] DateTime? asAt)
        => Run(() => dunning.GetArrearsAsync(clubId, asAt));
}
