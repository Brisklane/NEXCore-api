using Microsoft.AspNetCore.Mvc;
using RealEstate.Application.DTOs;
using RealEstate.Application.Services.Interfaces;
using RealEstate.Domain.Enums;

namespace RealEstate.Api.Controllers;

/// <summary>
/// People and the pipeline: parties, KYC, the caution list, enquiries, requirements and matching,
/// activities, tasks, the diary, viewings, site visits and keys.
///
/// The one number this part of the application lives or dies by is how long a lead waits for a
/// reply. Everything here is arranged around shortening it — routing, the SLA sweep, the day view
/// — because a lead answered in five minutes converts several times better than one answered in
/// an hour, and no amount of pipeline reporting recovers the difference.
/// </summary>
[Route("api/realestate/crm")]
public class CrmController(
    ICrmService crm,
    ILogger<CrmController> logger) : RealEstateControllerBase(logger)
{
    // ── Parties ──────────────────────────────────────────────────────────────

    [HttpPost("parties/search")]
    public Task<IActionResult> SearchParties([FromBody] PartySearchDto query)
        => RunPaged(() => crm.GetPartiesAsync(query));

    [HttpGet("parties/{id:guid}")]
    public Task<IActionResult> GetParty(Guid id)
        => RunFound(() => crm.GetPartyAsync(id), "That contact does not exist.");

    [HttpPost("parties")]
    public Task<IActionResult> SaveParty([FromBody] PartyUpsertDto request)
        => Run(() => crm.SavePartyAsync(request, UserId), "Contact saved.");

    [HttpGet("parties/lookup")]
    public Task<IActionResult> LookupParties(
        [FromQuery] string search, [FromQuery] PartyRoleKind? role, [FromQuery] int take = 20)
        => Run(() => crm.SearchPartiesAsync(search, role, take));

    [HttpPost("parties/duplicates")]
    public Task<IActionResult> FindDuplicates([FromBody] PartyUpsertDto request)
        => Run(() => crm.FindDuplicatePartiesAsync(request));

    /// <summary>
    /// Folds one contact into another. Everything the duplicate carried moves across, because a
    /// customer whose payments are split across two records cannot be shown a statement.
    /// </summary>
    [HttpPost("parties/merge")]
    public Task<IActionResult> MergeParties([FromQuery] Guid keepId, [FromQuery] Guid mergeId)
        => Run(() => crm.MergePartiesAsync(keepId, mergeId, UserId), "Contacts merged.");

    // ── KYC ──────────────────────────────────────────────────────────────────

    [HttpGet("parties/{partyId:guid}/kyc")]
    public Task<IActionResult> GetKyc(Guid partyId)
        => RunFound(() => crm.GetKycAsync(partyId), "No KYC case on file for that contact.");

    [HttpPost("kyc")]
    public Task<IActionResult> SaveKyc([FromBody] KycCaseDto request)
        => Run(() => crm.SaveKycAsync(request, UserId), "KYC saved.");

    [HttpPost("kyc/{id:guid}/decide")]
    public Task<IActionResult> DecideKyc(Guid id, [FromQuery] KycStatus status, [FromQuery] string? note)
        => Run(() => crm.DecideKycAsync(id, status, note, UserId), "Decision recorded.");

    [HttpGet("kyc/queue")]
    public Task<IActionResult> GetKycQueue([FromQuery] ListQueryDto query, [FromQuery] KycStatus? status)
        => RunPaged(() => crm.GetKycQueueAsync(query, status));

    // ── Caution list ─────────────────────────────────────────────────────────

    [HttpGet("caution-list")]
    public Task<IActionResult> GetCautionList([FromQuery] ListQueryDto query)
        => RunPaged(() => crm.GetCautionListAsync(query));

    [HttpPost("caution-list")]
    public Task<IActionResult> AddCaution([FromBody] CautionListEntryDto request)
        => Run(() => crm.AddCautionAsync(request, UserId), "Entry added.");

    [HttpPost("caution-list/{id:guid}/clear")]
    public Task<IActionResult> ClearCaution(Guid id, [FromQuery] string? note)
        => Run(() => crm.ClearCautionAsync(id, note, UserId), "Entry cleared.");

    // ── Enquiries ────────────────────────────────────────────────────────────

    [HttpPost("enquiries/search")]
    public Task<IActionResult> SearchEnquiries([FromBody] EnquirySearchDto query)
        => RunPaged(() => crm.GetEnquiriesAsync(query));

    [HttpPost("enquiries/board")]
    public Task<IActionResult> GetBoard([FromBody] EnquirySearchDto query)
        => Run(() => crm.GetBoardAsync(query));

    [HttpGet("enquiries/{id:guid}")]
    public Task<IActionResult> GetEnquiry(Guid id)
        => RunFound(() => crm.GetEnquiryAsync(id), "That enquiry does not exist.");

    [HttpPost("enquiries")]
    public Task<IActionResult> SaveEnquiry([FromBody] EnquiryUpsertDto request)
        => Run(() => crm.SaveEnquiryAsync(request, UserId), "Enquiry saved.");

    [HttpPost("enquiries/stage")]
    public Task<IActionResult> ChangeStage([FromBody] EnquiryStageChangeDto request)
        => Run(() => crm.ChangeStageAsync(request, UserId), "Stage updated.");

    [HttpPost("enquiries/{id:guid}/assign")]
    public Task<IActionResult> Assign(Guid id, [FromQuery] Guid agentId)
        => Run(() => crm.AssignAsync(id, agentId, UserId), "Enquiry assigned.");

    /// <summary>Hands out anything still sitting unassigned, by the office's own routing rules.</summary>
    [HttpPost("enquiries/route")]
    public Task<IActionResult> Route() => Run(crm.RouteUnassignedAsync, "Unassigned leads routed.");

    /// <summary>Marks leads that have passed the response promise, so they show as breached.</summary>
    [HttpPost("enquiries/flag-sla")]
    public Task<IActionResult> FlagSla() => Run(crm.FlagSlaBreachesAsync, "Breaches flagged.");

    // ── Requirements and matching ────────────────────────────────────────────

    [HttpPost("requirements")]
    public Task<IActionResult> SaveRequirement([FromBody] RequirementProfileUpsertDto request)
        => Run(() => crm.SaveRequirementAsync(request, UserId), "Requirement saved.");

    [HttpGet("parties/{partyId:guid}/requirements")]
    public Task<IActionResult> GetRequirements(Guid partyId)
        => Run(() => crm.GetRequirementsAsync(partyId));

    [HttpGet("requirements/{id:guid}/matches")]
    public Task<IActionResult> RunMatch(Guid id, [FromQuery] int take = 20)
        => Run(() => crm.RunMatchAsync(id, take));

    [HttpGet("listings/{listingId:guid}/matches")]
    public Task<IActionResult> MatchListing(Guid listingId, [FromQuery] int take = 20)
        => Run(() => crm.MatchListingToRequirementsAsync(listingId, take));

    [HttpPost("matches/send")]
    public Task<IActionResult> SendMatches([FromBody] SendMatchesDto request)
        => Run(() => crm.SendMatchesAsync(request, UserId), "Matches sent.");

    [HttpPost("matches/{matchId:guid}/dismiss")]
    public Task<IActionResult> DismissMatch(Guid matchId, [FromQuery] Guid? reasonCodeId)
        => Run(() => crm.DismissMatchAsync(matchId, reasonCodeId, UserId), "Match dismissed.");

    // ── Activity and tasks ───────────────────────────────────────────────────

    [HttpPost("activities")]
    public Task<IActionResult> LogActivity([FromBody] ActivityCreateDto request)
        => Run(() => crm.LogActivityAsync(request, UserId), "Logged.");

    [HttpGet("activities")]
    public Task<IActionResult> GetActivities(
        [FromQuery] Guid? partyId, [FromQuery] Guid? enquiryId, [FromQuery] ListQueryDto query)
        => RunPaged(() => crm.GetActivitiesAsync(partyId, enquiryId, query));

    [HttpPost("tasks")]
    public Task<IActionResult> SaveTask([FromBody] FollowUpTaskCreateDto request)
        => Run(() => crm.SaveTaskAsync(request, UserId), "Task saved.");

    [HttpPost("tasks/complete")]
    public Task<IActionResult> CompleteTask([FromBody] TaskCompletionDto request)
        => Run(() => crm.CompleteTaskAsync(request, UserId), "Task completed.");

    [HttpPost("tasks/{taskId:guid}/snooze")]
    public Task<IActionResult> SnoozeTask(Guid taskId, [FromQuery] DateTime until)
        => Run(() => crm.SnoozeTaskAsync(taskId, until, UserId), "Task snoozed.");

    /// <summary>Everything one person has to do today, in the order it has to happen.</summary>
    [HttpGet("my-day")]
    public Task<IActionResult> GetMyDay([FromQuery] DateOnly? date)
        => Run(() => crm.GetMyDayAsync(UserId, date ?? DateOnly.FromDateTime(DateTime.UtcNow)));

    [HttpGet("diary")]
    public Task<IActionResult> GetDiary([FromQuery] Guid? agentId, [FromQuery] DateOnly? date)
        => Run(() => crm.GetDiaryAsync(agentId, date ?? DateOnly.FromDateTime(DateTime.UtcNow), UserId));

    // ── Viewings ─────────────────────────────────────────────────────────────

    [HttpGet("viewings")]
    public Task<IActionResult> GetViewings(
        [FromQuery] ListQueryDto query, [FromQuery] Guid? agentId, [FromQuery] ViewingStatus? status)
        => RunPaged(() => crm.GetViewingsAsync(query, agentId, status));

    [HttpGet("viewings/{id:guid}")]
    public Task<IActionResult> GetViewing(Guid id)
        => RunFound(() => crm.GetViewingAsync(id), "That viewing does not exist.");

    [HttpPost("viewings")]
    public Task<IActionResult> SaveViewing([FromBody] ViewingUpsertDto request)
        => Run(() => crm.SaveViewingAsync(request, UserId), "Viewing saved.");

    [HttpPost("viewings/{id:guid}/status")]
    public Task<IActionResult> ChangeViewingStatus(
        Guid id, [FromQuery] ViewingStatus status, [FromQuery] Guid? reasonCodeId, [FromQuery] string? note)
        => Run(() => crm.ChangeViewingStatusAsync(id, status, reasonCodeId, note, UserId), "Status updated.");

    [HttpPost("viewings/feedback")]
    public Task<IActionResult> SaveFeedback([FromBody] ViewingFeedbackDto request)
        => Run(() => crm.SaveFeedbackAsync(request, UserId), "Feedback saved.");

    /// <summary>
    /// Sends the viewer's feedback to the vendor. Deliberately a separate step — some feedback
    /// needs a conversation before a seller reads it verbatim.
    /// </summary>
    [HttpPost("viewings/feedback/{feedbackId:guid}/share")]
    public Task<IActionResult> ShareFeedback(Guid feedbackId)
        => Run(() => crm.ShareFeedbackWithVendorAsync(feedbackId, UserId), "Feedback shared.");

    // ── Site visits ──────────────────────────────────────────────────────────

    [HttpGet("site-visits")]
    public Task<IActionResult> GetSiteVisits(
        [FromQuery] ListQueryDto query, [FromQuery] Guid? projectId, [FromQuery] ViewingStatus? status)
        => RunPaged(() => crm.GetSiteVisitsAsync(query, projectId, status));

    [HttpGet("site-visits/{id:guid}")]
    public Task<IActionResult> GetSiteVisit(Guid id)
        => RunFound(() => crm.GetSiteVisitAsync(id), "That site visit does not exist.");

    [HttpPost("site-visits")]
    public Task<IActionResult> SaveSiteVisit([FromBody] SiteVisitUpsertDto request)
        => Run(() => crm.SaveSiteVisitAsync(request, UserId), "Site visit saved.");

    [HttpPost("site-visits/{id:guid}/status")]
    public Task<IActionResult> ChangeVisitStatus(
        Guid id, [FromQuery] ViewingStatus status, [FromQuery] Guid? reasonCodeId)
        => Run(() => crm.ChangeVisitStatusAsync(id, status, reasonCodeId, UserId), "Status updated.");

    [HttpPost("site-visits/feedback")]
    public Task<IActionResult> SaveVisitFeedback([FromBody] SiteVisitFeedbackDto request)
        => Run(() => crm.SaveVisitFeedbackAsync(request, UserId), "Feedback saved.");

    // ── Keys ─────────────────────────────────────────────────────────────────

    [HttpGet("keys")]
    public Task<IActionResult> GetKeys([FromQuery] Guid? propertyId, [FromQuery] bool outOnly = false)
        => Run(() => crm.GetKeysAsync(propertyId, outOnly));

    [HttpPost("keys")]
    public Task<IActionResult> SaveKeySet([FromBody] KeySetDto request)
        => Run(() => crm.SaveKeySetAsync(request, UserId), "Key set saved.");

    /// <summary>Signs a set of keys in or out. The chain of custody is the whole point.</summary>
    [HttpPost("keys/{keySetId:guid}/move")]
    public Task<IActionResult> MoveKeys(
        Guid keySetId,
        [FromQuery] string movement,
        [FromQuery] Guid? holderUserId,
        [FromQuery] Guid? holderPartyId,
        [FromQuery] DateTime? dueBack,
        [FromQuery] string? note)
        => Run(() => crm.MoveKeysAsync(keySetId, movement, holderUserId, holderPartyId, dueBack, note, UserId),
            "Movement recorded.");
}
