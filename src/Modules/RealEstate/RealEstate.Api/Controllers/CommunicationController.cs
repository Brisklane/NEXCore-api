using Microsoft.AspNetCore.Mvc;
using RealEstate.Application.DTOs;
using RealEstate.Application.Services.Interfaces;
using RealEstate.Domain.Enums;

namespace RealEstate.Api.Controllers;

/// <summary>
/// The shared inbox, message templates, broadcasts, notification rules and portal accounts.
///
/// The governing idea is restraint. A property company can message every customer it has in one
/// click, and the moment it does that twice in a week those customers stop reading anything it
/// sends — including the demand notice that matters. So consent is checked, quiet hours are
/// honoured, a per-person daily cap applies, and a broadcast runs as a dry run first with every
/// suppression shown before anybody commits to sending.
/// </summary>
[Route("api/realestate/communication")]
public class CommunicationController(
    ICommunicationService communication,
    ILogger<CommunicationController> logger) : RealEstateControllerBase(logger)
{
    // ── Inbox ────────────────────────────────────────────────────────────────

    [HttpGet("conversations")]
    public Task<IActionResult> GetConversations(
        [FromQuery] ListQueryDto query,
        [FromQuery] NotificationChannel? channel,
        [FromQuery] string? status,
        [FromQuery] Guid? assignedToUserId)
        => RunPaged(() => communication.GetConversationsAsync(query, channel, status, assignedToUserId));

    [HttpGet("conversations/{id:guid}")]
    public Task<IActionResult> GetConversation(Guid id)
        => RunFound(() => communication.GetConversationAsync(id), "That conversation does not exist.");

    [HttpPost("send")]
    public Task<IActionResult> Send([FromBody] SendMessageDto request)
        => Run(() => communication.SendAsync(request, UserId), "Message sent.");

    [HttpPost("conversations/{id:guid}/assign")]
    public Task<IActionResult> Assign(Guid id, [FromQuery] Guid assignToUserId)
        => Run(() => communication.AssignConversationAsync(id, assignToUserId, UserId), "Conversation assigned.");

    /// <summary>
    /// Closes a thread. Refused while the customer's last message is unanswered — closing one of
    /// those is how a complaint becomes a review.
    /// </summary>
    [HttpPost("conversations/{id:guid}/close")]
    public Task<IActionResult> Close(Guid id)
        => Run(() => communication.CloseConversationAsync(id, UserId), "Conversation closed.");

    // ── Templates ────────────────────────────────────────────────────────────

    [HttpGet("templates")]
    public Task<IActionResult> GetTemplates(
        [FromQuery] NotificationChannel? channel, [FromQuery] string? category)
        => Run(() => communication.GetMessageTemplatesAsync(channel, category));

    [HttpPost("templates")]
    public Task<IActionResult> SaveTemplate([FromBody] MessageTemplateDto request)
        => Run(() => communication.SaveMessageTemplateAsync(request, UserId), "Template saved.");

    // ── Broadcasts ───────────────────────────────────────────────────────────

    /// <summary>
    /// Runs a broadcast. Defaults to a dry run, which returns the audience, the suppressions and
    /// why each one was suppressed, without writing or sending anything.
    /// </summary>
    [HttpPost("broadcasts")]
    public Task<IActionResult> RunBroadcast([FromBody] BroadcastRequestDto request)
        => Run(() => communication.RunBroadcastAsync(request, UserId));

    [HttpGet("broadcasts")]
    public Task<IActionResult> GetBroadcasts([FromQuery] ListQueryDto query)
        => RunPaged(() => communication.GetBroadcastsAsync(query));

    // ── Notifications ────────────────────────────────────────────────────────

    [HttpGet("rules")]
    public Task<IActionResult> GetRules() => Run(communication.GetNotificationRulesAsync);

    [HttpPost("rules")]
    public Task<IActionResult> SaveRule([FromBody] NotificationRuleDto request)
        => Run(() => communication.SaveNotificationRuleAsync(request, UserId), "Rule saved.");

    [HttpGet("notifications")]
    public Task<IActionResult> GetNotifications([FromQuery] ListQueryDto query, [FromQuery] bool unreadOnly = false)
        => RunPaged(() => communication.GetNotificationsAsync(query, UserId, unreadOnly));

    [HttpPost("notifications/read")]
    public Task<IActionResult> MarkRead([FromBody] List<Guid> ids)
        => Run(() => communication.MarkNotificationsReadAsync(ids, UserId), "Marked as read.");

    /// <summary>
    /// The nightly sweep that turns dated obligations into notifications. Idempotent — each source
    /// carries its own alert flag, so running it twice in a day does not message anybody twice.
    /// </summary>
    [HttpPost("alerts/sweep")]
    public Task<IActionResult> RunAlertSweep()
        => Run(communication.RunAlertSweepAsync, "Alert sweep complete.");

    // ── Portal accounts ──────────────────────────────────────────────────────

    [HttpGet("portal-users")]
    public Task<IActionResult> GetPortalUsers([FromQuery] ListQueryDto query, [FromQuery] PortalAudience? audience)
        => RunPaged(() => communication.GetPortalUsersAsync(query, audience));

    [HttpPost("portal-users")]
    public Task<IActionResult> SavePortalUser([FromBody] PortalUserDto request)
        => Run(() => communication.SavePortalUserAsync(request, UserId), "Account saved.");

    [HttpPost("portal-users/{id:guid}/invite")]
    public Task<IActionResult> InvitePortalUser(Guid id)
        => Run(() => communication.InvitePortalUserAsync(id, UserId), "Invitation sent.");

    /// <summary>
    /// What a buyer sees when they log in: what they owe, when it is due, and how far along the
    /// building is — from the same figures the office sees, because a portal that disagrees with
    /// the accounts department generates more calls than it saves.
    /// </summary>
    [HttpGet("portal/customer/{partyId:guid}")]
    public Task<IActionResult> GetCustomerPortal(Guid partyId)
        => Run(() => communication.GetCustomerPortalHomeAsync(partyId));
}
