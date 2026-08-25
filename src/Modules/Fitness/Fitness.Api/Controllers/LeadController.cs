using Fitness.Application.DTOs;
using Fitness.Application.Services.Interfaces;
using Fitness.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;

namespace Fitness.Api.Controllers;

/// <summary>
/// The sales pipeline: enquiries, tours, trials, referrals and conversion.
///
/// The response-time clock is the point of this module. Conversion falls off a cliff after the
/// first few minutes, so an enquiry that has not been contacted inside the SLA is flagged, counted
/// and surfaced on the dashboard — a breach is a countable event, not a feeling the sales manager
/// has on a Friday.
/// </summary>
[Route("api/fitness/leads")]
public class LeadController(
    ILeadService leads,
    ILogger<LeadController> logger) : FitnessControllerBase(logger)
{
    /// <summary>The pipeline board — columns, counts, values and the SLA metrics above it.</summary>
    [HttpGet("board")]
    public Task<IActionResult> GetBoard([FromQuery] Guid? clubId, [FromQuery] Guid? assignedStaffId)
        => Run(() => leads.GetBoardAsync(clubId, assignedStaffId));

    [HttpGet]
    public Task<IActionResult> List(
        [FromQuery] Guid? clubId, [FromQuery] LeadStatus? status, [FromQuery] Guid? sourceId,
        [FromQuery] Guid? assignedStaffId, [FromQuery] bool? slaBreached,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? search,
        [FromQuery] PaginationParams? pagination = null)
        => RunPaged(() => leads.ListAsync(clubId, status, sourceId, assignedStaffId, slaBreached,
            from, to, search, pagination ?? new PaginationParams()));

    [HttpGet("{id:guid}")]
    public Task<IActionResult> Get(Guid id)
        => RunFound(() => leads.GetAsync(id), "Lead not found.");

    /// <summary>Creates an enquiry and assigns it — round robin, to whoever has fewest open leads.</summary>
    [HttpPost]
    public Task<IActionResult> Create([FromBody] SaveLeadDto request)
        => Run(() => leads.CreateAsync(request, UserId), "Lead created.");

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(Guid id, [FromBody] SaveLeadDto request)
        => Run(() => leads.UpdateAsync(id, request, UserId), "Lead saved.");

    [HttpPost("{id:guid}/assign")]
    public Task<IActionResult> Assign(Guid id, [FromQuery] Guid staffId)
        => Run(() => leads.AssignAsync(id, staffId, UserId), "Assigned.");

    /// <summary>Logs a call, text or email. The first successful contact stops the SLA clock.</summary>
    [HttpPost("activities")]
    public Task<IActionResult> LogActivity([FromBody] LogLeadActivityDto request)
        => Run(() => leads.LogActivityAsync(request, UserId), "Logged.");

    [HttpPost("{id:guid}/stage")]
    public Task<IActionResult> MoveStage(Guid id, [FromQuery] LeadStatus status)
        => Run(() => leads.MoveStageAsync(id, status, UserId), "Moved.");

    [HttpPost("close")]
    public Task<IActionResult> Close([FromBody] CloseLeadDto request)
        => Run(() => leads.CloseAsync(request, UserId), "Closed.");

    /// <summary>
    /// Converts an enquiry into a member, carrying the attribution across.
    ///
    /// Also closes the loops behind it — the referral that produced them, the trial they were on,
    /// the tour they took — so the source report tells the truth about what actually works.
    /// </summary>
    [HttpPost("{id:guid}/convert")]
    public Task<IActionResult> Convert(Guid id, [FromBody] JoinMemberDto request)
        => Run(() => leads.ConvertAsync(id, request, UserId), "Joined.");

    // ── Tours and trials ─────────────────────────────────────────────────────

    [HttpPost("tours")]
    public Task<IActionResult> BookTour([FromBody] BookTourDto request)
        => Run(() => leads.BookTourAsync(request, UserId), "Tour booked.");

    [HttpPut("tours/{id:guid}")]
    public Task<IActionResult> UpdateTour(Guid id, [FromBody] TourDto request)
        => Run(() => leads.UpdateTourAsync(id, request, UserId), "Tour saved.");

    [HttpGet("tours")]
    public Task<IActionResult> GetTours(
        [FromQuery] Guid clubId, [FromQuery] DateTime from, [FromQuery] DateTime to,
        [FromQuery] Guid? staffId)
        => Run(() => leads.GetToursAsync(clubId, from, to, staffId));

    /// <summary>Issues a trial, backed by a real day pass so the barrier actually lets them in.</summary>
    [HttpPost("trials")]
    public Task<IActionResult> IssueTrial([FromBody] IssueTrialDto request)
        => Run(() => leads.IssueTrialAsync(request, UserId), "Trial issued.");

    [HttpGet("trials")]
    public Task<IActionResult> GetTrials([FromQuery] Guid? clubId, [FromQuery] bool activeOnly = true)
        => Run(() => leads.GetTrialsAsync(clubId, activeOnly));

    // ── Referrals ────────────────────────────────────────────────────────────

    [HttpPost("referrals")]
    public Task<IActionResult> CreateReferral([FromBody] CreateReferralDto request)
        => Run(() => leads.CreateReferralAsync(request, UserId), "Referral logged.");

    [HttpGet("referrals")]
    public Task<IActionResult> GetReferrals(
        [FromQuery] Guid? clubId, [FromQuery] Guid? memberId, [FromQuery] bool? converted)
        => Run(() => leads.GetReferralsAsync(clubId, memberId, converted));

    // ── Configuration ────────────────────────────────────────────────────────

    [HttpGet("sources")]
    public Task<IActionResult> GetSources([FromQuery] Guid? clubId, [FromQuery] bool activeOnly = true)
        => Run(() => leads.GetSourcesAsync(clubId, activeOnly));

    [HttpPost("sources")]
    public Task<IActionResult> SaveSource([FromBody] LeadSourceDto request, [FromQuery] Guid? id = null)
        => Run(() => leads.SaveSourceAsync(id, request, UserId), "Source saved.");

    [HttpGet("loss-reasons")]
    public Task<IActionResult> GetLossReasons() => Run(leads.GetLossReasonsAsync);

    [HttpGet("targets")]
    public Task<IActionResult> GetTargets([FromQuery] Guid? clubId, [FromQuery] DateTime? periodStart)
        => Run(() => leads.GetTargetsAsync(clubId, periodStart));

    [HttpPost("targets")]
    public Task<IActionResult> SaveTarget([FromBody] SalesTargetDto request, [FromQuery] Guid? id = null)
        => Run(() => leads.SaveTargetAsync(id, request, UserId), "Target saved.");

    /// <summary>Frequent job: flags enquiries that have gone past the response SLA.</summary>
    [HttpPost("flag-sla-breaches")]
    public Task<IActionResult> FlagSlaBreaches()
        => Run(leads.FlagSlaBreachesAsync);
}
