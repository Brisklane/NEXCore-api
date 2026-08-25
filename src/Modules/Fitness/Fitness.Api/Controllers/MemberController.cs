using Fitness.Application.DTOs;
using Fitness.Application.Services.Interfaces;
using Fitness.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;

namespace Fitness.Api.Controllers;

/// <summary>
/// Members — the record everything else in this app hangs off.
///
/// Two endpoints here are deliberately different from ordinary CRUD. <c>export</c> and
/// <c>anonymise</c> exist because a member has a legal right to ask for their data and to ask for
/// it to be erased, and both are audited. Erasure keeps the financial and incident records the
/// club is required to retain, and says so in the response.
/// </summary>
[Route("api/fitness/members")]
public class MemberController(
    IMemberService members,
    ILogger<MemberController> logger) : FitnessControllerBase(logger)
{
    [HttpGet]
    public Task<IActionResult> List(
        [FromQuery] Guid? clubId, [FromQuery] MemberStatus? status, [FromQuery] ChurnRiskBand? riskBand,
        [FromQuery] string? search, [FromQuery] Guid? planId, [FromQuery] bool? hasBalance,
        [FromQuery] PaginationParams? pagination = null)
        => RunPaged(() => members.ListAsync(clubId, status, riskBand, search, planId, hasBalance,
            pagination ?? new PaginationParams()));

    /// <summary>The whole 360 view in one payload — the desk should never wait on six round trips.</summary>
    [HttpGet("{id:guid}")]
    public Task<IActionResult> Get(Guid id)
        => RunFound(() => members.GetAsync(id), "Member not found.");

    /// <summary>Ranked search for the desk: exact member number first, then phone, then active members.</summary>
    [HttpPost("search")]
    public Task<IActionResult> Search([FromBody] MemberSearchDto request)
        => Run(() => members.SearchAsync(request));

    [HttpPost]
    public Task<IActionResult> Create([FromBody] SaveMemberDto request)
        => Run(() => members.CreateAsync(request, UserId), "Member created.");

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(Guid id, [FromBody] SaveMemberDto request)
        => Run(() => members.UpdateAsync(id, request, UserId), "Member saved.");

    /// <summary>
    /// The join wizard, in one transaction: member, consents, waiver, screening, agreement,
    /// payment method, first payment, access credential and loyalty account.
    ///
    /// One call because a half-joined member — signed but not billed, or billed with no fob — is
    /// the single most common mess in gym software.
    /// </summary>
    [HttpPost("join")]
    public Task<IActionResult> Join([FromBody] JoinMemberDto request)
        => Run(() => members.JoinAsync(request, UserId), "Welcome aboard.");

    [HttpPost("status")]
    public Task<IActionResult> ChangeStatus([FromBody] ChangeMemberStatusDto request)
        => Run(() => members.ChangeStatusAsync(request, UserId), "Status updated.");

    [HttpPost("ban")]
    public Task<IActionResult> SetBan([FromBody] BanMemberDto request)
        => Run(() => members.SetBanAsync(request, UserId), "Saved.");

    // ── Record ───────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/timeline")]
    public Task<IActionResult> Timeline(Guid id, [FromQuery] int limit = 100)
        => Run(() => members.GetTimelineAsync(id, limit));

    [HttpPost("notes")]
    public Task<IActionResult> AddNote([FromBody] MemberNoteDto request)
        => Run(() => members.AddNoteAsync(request, UserId), "Note added.");

    [HttpGet("{id:guid}/alerts")]
    public Task<IActionResult> Alerts(Guid id) => Run(() => members.GetAlertsAsync(id));

    /// <summary>Recomputes the blocking conditions the turnstile reads. Cheap, and idempotent.</summary>
    [HttpPost("{id:guid}/alerts/refresh")]
    public Task<IActionResult> RefreshAlerts(Guid id)
        => Run(() => members.RefreshAlertsAsync(id), "Alerts refreshed.");

    [HttpGet("{id:guid}/visits")]
    public Task<IActionResult> Visits(Guid id, [FromQuery] int limit = 50)
        => Run(() => members.GetVisitHistoryAsync(id, limit));

    [HttpGet("{id:guid}/ledger")]
    public Task<IActionResult> Ledger(Guid id, [FromQuery] int limit = 100)
        => Run(() => members.GetLedgerAsync(id, limit));

    [HttpGet("{id:guid}/upcoming")]
    public Task<IActionResult> Upcoming(Guid id) => Run(() => members.GetUpcomingAsync(id));

    // ── Credentials ──────────────────────────────────────────────────────────

    /// <summary>
    /// Issues a fob, key tag or mobile credential.
    ///
    /// For a biometric, the identifier stored is a *reference* to a template held on the reader.
    /// The template itself never reaches this server.
    /// </summary>
    [HttpPost("credentials")]
    public Task<IActionResult> IssueCredential([FromBody] IssueCredentialDto request)
        => Run(() => members.IssueCredentialAsync(request, UserId), "Credential issued.");

    [HttpDelete("credentials/{id:guid}")]
    public Task<IActionResult> DeactivateCredential(Guid id, [FromQuery] string reason = "Lost")
        => Run(() => members.DeactivateCredentialAsync(id, reason, UserId), "Credential deactivated.");

    // ── Households ───────────────────────────────────────────────────────────

    [HttpGet("households")]
    public Task<IActionResult> ListHouseholds(
        [FromQuery] Guid? clubId, [FromQuery] string? search,
        [FromQuery] PaginationParams? pagination = null)
        => RunPaged(() => members.ListHouseholdsAsync(clubId, search, pagination ?? new PaginationParams()));

    [HttpGet("households/{id:guid}")]
    public Task<IActionResult> GetHousehold(Guid id)
        => RunFound(() => members.GetHouseholdAsync(id), "Household not found.");

    [HttpPost("households")]
    public Task<IActionResult> SaveHousehold([FromBody] SaveHouseholdDto request)
        => Run(() => members.SaveHouseholdAsync(request, UserId), "Household saved.");

    // ── Duplicates ───────────────────────────────────────────────────────────

    /// <summary>Shows exactly what a merge would move before anything is moved.</summary>
    [HttpPost("merge/preview")]
    public Task<IActionResult> PreviewMerge([FromBody] MergeMembersDto request)
        => Run(() => members.PreviewMergeAsync(request));

    [HttpPost("merge")]
    public Task<IActionResult> Merge([FromBody] MergeMembersDto request)
        => Run(() => members.MergeAsync(request, UserId), "Records merged.");

    // ── Data rights ──────────────────────────────────────────────────────────

    /// <summary>Everything held about this member, in one file. The request itself is audited.</summary>
    [HttpGet("{id:guid}/export")]
    public Task<IActionResult> Export(Guid id)
        => Run(() => members.ExportAsync(id, UserId));

    /// <summary>
    /// Erases the person, keeps what the law requires the club to keep.
    ///
    /// Identity is tombstoned and special-category data is deleted outright; financial and
    /// incident records survive, because a club cannot lawfully destroy them. Irreversible, and
    /// the request has to say so explicitly.
    /// </summary>
    [HttpPost("anonymise")]
    public Task<IActionResult> Anonymise([FromBody] AnonymiseMemberDto request)
        => Run(() => members.AnonymiseAsync(request, UserId), "Member record anonymised.");
}
