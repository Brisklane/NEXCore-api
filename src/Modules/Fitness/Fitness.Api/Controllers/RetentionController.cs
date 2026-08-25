using Fitness.Application.DTOs;
using Fitness.Application.Services.Interfaces;
using Fitness.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;

namespace Fitness.Api.Controllers;

/// <summary>
/// Keeping members: risk scoring, journeys, campaigns, loyalty, challenges and feedback.
///
/// The churn score is explainable by design. "At risk" on its own is useless to the person who
/// has to make the call — every score comes back with the factors that produced it, weighted and
/// in plain English, so the conversation can start with "we noticed you've not been in for three
/// weeks" rather than a number nobody trusts.
/// </summary>
[Route("api/fitness/retention")]
public class RetentionController(
    IRetentionService retention,
    ILogger<RetentionController> logger) : FitnessControllerBase(logger)
{
    [HttpGet("board")]
    public Task<IActionResult> GetBoard(
        [FromQuery] Guid? clubId, [FromQuery] ChurnRiskBand? band, [FromQuery] Guid? ownerStaffId)
        => Run(() => retention.GetBoardAsync(clubId, band, ownerStaffId));

    [HttpGet("scores/{memberId:guid}")]
    public Task<IActionResult> GetScore(Guid memberId)
        => RunFound(() => retention.GetScoreAsync(memberId), "No score for that member yet.");

    /// <summary>Nightly job: rescores everyone and raises tasks for anyone who has got worse.</summary>
    [HttpPost("scores/run")]
    public Task<IActionResult> ScoreAll([FromQuery] Guid? clubId)
        => Run(() => retention.ScoreAllAsync(clubId));

    // ── Tasks ────────────────────────────────────────────────────────────────

    [HttpGet("tasks")]
    public Task<IActionResult> GetTasks(
        [FromQuery] Guid? clubId, [FromQuery] Guid? staffId, [FromQuery] bool openOnly = true)
        => Run(() => retention.GetTasksAsync(clubId, staffId, openOnly));

    [HttpPost("tasks")]
    public Task<IActionResult> CreateTask([FromBody] RetentionTaskDto request)
        => Run(() => retention.CreateTaskAsync(request, UserId), "Task created.");

    [HttpPost("tasks/complete")]
    public Task<IActionResult> CompleteTask([FromBody] CompleteTaskDto request)
        => Run(() => retention.CompleteTaskAsync(request, UserId), "Task completed.");

    // ── Journeys ─────────────────────────────────────────────────────────────

    [HttpGet("journeys")]
    public Task<IActionResult> GetJourneys([FromQuery] Guid? clubId)
        => Run(() => retention.GetJourneysAsync(clubId));

    /// <summary>
    /// Saves a journey. New ones are created switched off.
    ///
    /// Deliberate: an automation that starts messaging two thousand members the moment somebody
    /// hits save is an automation that gets the club reported.
    /// </summary>
    [HttpPost("journeys")]
    public Task<IActionResult> SaveJourney([FromBody] EngagementJourneyDto request, [FromQuery] Guid? id = null)
        => Run(() => retention.SaveJourneyAsync(id, request, UserId), "Journey saved.");

    [HttpPost("journeys/{id:guid}/active")]
    public Task<IActionResult> SetJourneyActive(Guid id, [FromQuery] bool active)
        => Run(() => retention.SetJourneyActiveAsync(id, active, UserId), active ? "Journey switched on." : "Journey paused.");

    [HttpGet("journeys/{id:guid}/enrolments")]
    public Task<IActionResult> GetEnrolments(Guid id, [FromQuery] bool activeOnly = true)
        => Run(() => retention.GetEnrolmentsAsync(id, activeOnly));

    /// <summary>Frequent job: advances every enrolment whose next step is due.</summary>
    [HttpPost("journeys/process")]
    public Task<IActionResult> ProcessJourneys()
        => Run(retention.ProcessJourneysAsync);

    // ── Campaigns ────────────────────────────────────────────────────────────

    [HttpGet("campaigns")]
    public Task<IActionResult> ListCampaigns([FromQuery] Guid? clubId, [FromQuery] PaginationParams? pagination = null)
        => RunPaged(() => retention.ListCampaignsAsync(clubId, pagination ?? new PaginationParams()));

    [HttpPost("campaigns")]
    public Task<IActionResult> SaveCampaign([FromBody] CampaignDto request, [FromQuery] Guid? id = null)
        => Run(() => retention.SaveCampaignAsync(id, request, UserId), "Campaign saved.");

    /// <summary>
    /// Sends a campaign.
    ///
    /// Consent is enforced here, not in the UI: anyone who has not opted in is skipped, and
    /// anything that would land inside quiet hours is deferred to the morning rather than
    /// dropped. A gym that texts people at 11pm loses more members than the offer wins.
    /// </summary>
    [HttpPost("campaigns/send")]
    public Task<IActionResult> SendCampaign([FromBody] SendCampaignDto request)
        => Run(() => retention.SendCampaignAsync(request, UserId), "Campaign sent.");

    [HttpGet("templates")]
    public Task<IActionResult> GetTemplates([FromQuery] Guid? clubId, [FromQuery] MessageChannel? channel)
        => Run(() => retention.GetTemplatesAsync(clubId, channel));

    [HttpPost("templates")]
    public Task<IActionResult> SaveTemplate([FromBody] MessageTemplateDto request, [FromQuery] Guid? id = null)
        => Run(() => retention.SaveTemplateAsync(id, request, UserId), "Template saved.");

    // ── Segments ─────────────────────────────────────────────────────────────

    [HttpGet("segments")]
    public Task<IActionResult> GetSegments([FromQuery] Guid? clubId)
        => Run(() => retention.GetSegmentsAsync(clubId));

    [HttpPost("segments")]
    public Task<IActionResult> SaveSegment([FromBody] SaveSegmentDto request, [FromQuery] Guid? id = null)
        => Run(() => retention.SaveSegmentAsync(id, request, UserId), "Segment saved.");

    /// <summary>Who is actually in this segment, before anything is sent to them.</summary>
    [HttpGet("segments/{id:guid}/preview")]
    public Task<IActionResult> PreviewSegment(Guid id, [FromQuery] PaginationParams? pagination = null)
        => RunPaged(() => retention.PreviewSegmentAsync(id, pagination ?? new PaginationParams()));

    [HttpGet("messages")]
    public Task<IActionResult> GetMessageLog(
        [FromQuery] Guid? clubId, [FromQuery] Guid? memberId, [FromQuery] MessageChannel? channel,
        [FromQuery] MessageStatus? status, [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] PaginationParams? pagination = null)
        => RunPaged(() => retention.GetMessageLogAsync(clubId, memberId, channel, status, from, to,
            pagination ?? new PaginationParams()));

    // ── Loyalty ──────────────────────────────────────────────────────────────

    [HttpGet("loyalty/{memberId:guid}")]
    public Task<IActionResult> GetLoyalty(Guid memberId)
        => RunFound(() => retention.GetLoyaltyAsync(memberId), "No loyalty account.");

    [HttpPost("loyalty/award")]
    public Task<IActionResult> AwardPoints([FromBody] AwardPointsDto request)
        => Run(() => retention.AwardPointsAsync(request, UserId), "Points awarded.");

    [HttpPost("loyalty/redeem")]
    public Task<IActionResult> RedeemPoints([FromBody] RedeemPointsDto request)
        => Run(() => retention.RedeemPointsAsync(request, UserId), "Points redeemed.");

    [HttpGet("loyalty/tiers")]
    public Task<IActionResult> GetTiers([FromQuery] Guid? clubId)
        => Run(() => retention.GetTiersAsync(clubId));

    [HttpPost("loyalty/tiers")]
    public Task<IActionResult> SaveTier([FromBody] LoyaltyTierDto request, [FromQuery] Guid? id = null)
        => Run(() => retention.SaveTierAsync(id, request, UserId), "Tier saved.");

    // ── Challenges ───────────────────────────────────────────────────────────

    [HttpGet("challenges")]
    public Task<IActionResult> GetChallenges(
        [FromQuery] Guid? clubId, [FromQuery] bool activeOnly = true, [FromQuery] Guid? viewerMemberId = null)
        => Run(() => retention.GetChallengesAsync(clubId, activeOnly, viewerMemberId));

    [HttpPost("challenges")]
    public Task<IActionResult> SaveChallenge([FromBody] ChallengeDto request, [FromQuery] Guid? id = null)
        => Run(() => retention.SaveChallengeAsync(id, request, UserId), "Challenge saved.");

    [HttpPost("challenges/{id:guid}/join")]
    public Task<IActionResult> JoinChallenge(
        Guid id, [FromQuery] Guid memberId, [FromQuery] string? teamName = null)
        => Run(() => retention.JoinChallengeAsync(id, memberId, teamName, UserId), "Joined.");

    /// <summary>Nightly job: recomputes challenge standings and awards any badges earned.</summary>
    [HttpPost("challenges/process")]
    public Task<IActionResult> ProcessChallenges()
        => Run(retention.ProcessChallengesAndBadgesAsync);

    [HttpGet("badges")]
    public Task<IActionResult> GetBadges([FromQuery] Guid? clubId)
        => Run(() => retention.GetBadgesAsync(clubId));

    [HttpGet("badges/{memberId:guid}")]
    public Task<IActionResult> GetMemberBadges(Guid memberId)
        => Run(() => retention.GetMemberBadgesAsync(memberId));

    // ── Voice of the member ──────────────────────────────────────────────────

    /// <summary>Records an NPS score. A detractor raises a follow-up task automatically.</summary>
    [HttpPost("nps")]
    public Task<IActionResult> RecordNps([FromBody] NpsResponseDto request)
        => Run(() => retention.RecordNpsAsync(request, UserId), "Thank you.");

    [HttpGet("nps/summary")]
    public Task<IActionResult> GetNpsSummary(
        [FromQuery] Guid? clubId, [FromQuery] DateTime from, [FromQuery] DateTime to)
        => Run(() => retention.GetNpsSummaryAsync(clubId, from, to));

    [HttpGet("nps")]
    public Task<IActionResult> GetNpsResponses(
        [FromQuery] Guid? clubId, [FromQuery] string? band, [FromQuery] bool needsFollowUpOnly = false,
        [FromQuery] PaginationParams? pagination = null)
        => RunPaged(() => retention.GetNpsResponsesAsync(clubId, band, needsFollowUpOnly,
            pagination ?? new PaginationParams()));

    [HttpPost("nps/{id:guid}/follow-up")]
    public Task<IActionResult> FollowUpNps(Guid id, [FromQuery] string note)
        => Run(() => retention.FollowUpNpsAsync(id, note, UserId), "Follow-up recorded.");

    [HttpPost("feedback")]
    public Task<IActionResult> RecordFeedback([FromBody] FeedbackDto request)
        => Run(() => retention.RecordFeedbackAsync(request, UserId), "Feedback recorded.");

    [HttpGet("feedback")]
    public Task<IActionResult> GetFeedback(
        [FromQuery] Guid? clubId, [FromQuery] bool openOnly = false,
        [FromQuery] PaginationParams? pagination = null)
        => RunPaged(() => retention.GetFeedbackAsync(clubId, openOnly, pagination ?? new PaginationParams()));

    [HttpGet("announcements")]
    public Task<IActionResult> GetAnnouncements([FromQuery] Guid? clubId, [FromQuery] bool liveOnly = true)
        => Run(() => retention.GetAnnouncementsAsync(clubId, liveOnly));

    [HttpPost("announcements")]
    public Task<IActionResult> SaveAnnouncement([FromBody] AnnouncementDto request, [FromQuery] Guid? id = null)
        => Run(() => retention.SaveAnnouncementAsync(id, request, UserId), "Announcement saved.");
}
