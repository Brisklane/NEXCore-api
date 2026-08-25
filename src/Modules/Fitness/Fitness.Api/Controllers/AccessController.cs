using Fitness.Application.DTOs;
using Fitness.Application.Services.Interfaces;
using Fitness.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;

namespace Fitness.Api.Controllers;

/// <summary>
/// The door, the desk and the hardware behind them.
///
/// <c>decide</c> is the hot path: a turnstile calls it, waits, and either opens or does not. It is
/// budgeted at a few hundred milliseconds, which is why blocking conditions are materialised onto
/// the member's record rather than computed live — and why every refusal comes back as a sentence
/// a member can be told, not a code.
/// </summary>
[Route("api/fitness/access")]
public class AccessController(
    IAccessService access,
    ILogger<AccessController> logger) : FitnessControllerBase(logger)
{
    /// <summary>Should this door open? Eleven checks, in the order that fails cheapest first.</summary>
    [HttpPost("decide")]
    public Task<IActionResult> Decide([FromBody] AccessRequestDto request)
        => Run(() => access.DecideAsync(request));

    /// <summary>A staff member letting someone in by hand. Always logged as an override.</summary>
    [HttpPost("check-in/manual")]
    public Task<IActionResult> ManualCheckIn([FromBody] ManualCheckInDto request)
        => Run(() => access.ManualCheckInAsync(request, UserId), "Checked in.");

    [HttpPost("check-out/{checkInId:guid}")]
    public Task<IActionResult> CheckOut(Guid checkInId)
        => Run(() => access.CheckOutAsync(checkInId, UserId), "Checked out.");

    /// <summary>
    /// Nightly job: closes visits nobody tapped out of.
    ///
    /// Duration is estimated from the club's own median visit rather than left null, and the row
    /// is flagged as auto-closed so the attendance report is honest about which is which.
    /// </summary>
    [HttpPost("check-ins/sweep")]
    public Task<IActionResult> SweepOpenCheckIns()
        => Run(access.SweepOpenCheckInsAsync);

    [HttpGet("occupancy/{clubId:guid}")]
    public Task<IActionResult> GetOccupancy(Guid clubId)
        => Run(() => access.GetOccupancyAsync(clubId));

    [HttpGet("occupancy/{clubId:guid}/trend")]
    public Task<IActionResult> GetOccupancyTrend(
        Guid clubId, [FromQuery] DateTime from, [FromQuery] DateTime to)
        => Run(() => access.GetOccupancyTrendAsync(clubId, from, to));

    [HttpGet("events")]
    public Task<IActionResult> GetEvents(
        [FromQuery] Guid? clubId, [FromQuery] Guid? doorId, [FromQuery] Guid? memberId,
        [FromQuery] AccessDecision? decision, [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] PaginationParams? pagination = null)
        => RunPaged(() => access.GetEventsAsync(clubId, doorId, memberId, decision, from, to,
            pagination ?? new PaginationParams()));

    [HttpGet("check-ins")]
    public Task<IActionResult> GetCheckIns(
        [FromQuery] Guid? clubId, [FromQuery] Guid? memberId,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] PaginationParams? pagination = null)
        => RunPaged(() => access.GetCheckInsAsync(clubId, memberId, from, to,
            pagination ?? new PaginationParams()));

    // ── Hardware ─────────────────────────────────────────────────────────────

    [HttpGet("doors")]
    public Task<IActionResult> GetDoors([FromQuery] Guid? clubId)
        => Run(() => access.GetDoorsAsync(clubId));

    [HttpPost("doors")]
    public Task<IActionResult> SaveDoor([FromBody] SaveDoorDto request, [FromQuery] Guid? id = null)
        => Run(() => access.SaveDoorAsync(id, request, UserId), "Door saved.");

    [HttpDelete("doors/{id:guid}")]
    public Task<IActionResult> DeleteDoor(Guid id)
        => Run(() => access.DeleteDoorAsync(id, UserId), "Door removed.");

    /// <summary>Holds a door open — for a delivery, a fire drill, or an open evening. Always logged.</summary>
    [HttpPost("doors/{id:guid}/release")]
    public Task<IActionResult> ReleaseDoor(Guid id, [FromQuery] string reason)
        => Run(() => access.ReleaseDoorAsync(id, reason, UserId), "Door released.");

    [HttpGet("controllers")]
    public Task<IActionResult> GetControllers([FromQuery] Guid? clubId)
        => Run(() => access.GetControllersAsync(clubId));

    [HttpPost("controllers")]
    public Task<IActionResult> SaveController([FromBody] SaveControllerDto request, [FromQuery] Guid? id = null)
        => Run(() => access.SaveControllerAsync(id, request, UserId), "Controller saved.");

    /// <summary>
    /// The offline entitlement list a controller caches.
    ///
    /// A barrier that stops working when the network does is a barrier that gets propped open with
    /// a fire extinguisher. The controller holds enough to decide by itself, and replays what it
    /// did when the link comes back.
    /// </summary>
    [HttpGet("controllers/{id:guid}/cache")]
    public Task<IActionResult> GetControllerCache(Guid id)
        => Run(() => access.GetControllerCacheAsync(id));

    [HttpPost("controllers/{id:guid}/heartbeat")]
    public Task<IActionResult> Heartbeat(Guid id, [FromQuery] int pendingEvents = 0)
        => Run(() => access.RecordHeartbeatAsync(id, pendingEvents));

    [HttpPost("controllers/{id:guid}/replay")]
    public Task<IActionResult> ReplayOffline(Guid id, [FromBody] List<AccessRequestDto> events)
        => Run(() => access.ReplayOfflineEventsAsync(id, events), "Offline events replayed.");

    // ── Rules ────────────────────────────────────────────────────────────────

    [HttpGet("rules")]
    public Task<IActionResult> GetRules([FromQuery] Guid? clubId)
        => Run(() => access.GetRulesAsync(clubId));

    [HttpPost("rules")]
    public Task<IActionResult> SaveRule([FromBody] AccessRuleDto request, [FromQuery] Guid? id = null)
        => Run(() => access.SaveRuleAsync(id, request, UserId), "Access rule saved.");

    // ── Visitors ─────────────────────────────────────────────────────────────

    [HttpPost("guests")]
    public Task<IActionResult> RegisterGuest([FromBody] RegisterGuestDto request)
        => Run(() => access.RegisterGuestAsync(request, UserId), "Guest registered.");

    [HttpPost("day-passes")]
    public Task<IActionResult> IssueDayPass([FromBody] IssueDayPassDto request)
        => Run(() => access.IssueDayPassAsync(request, UserId), "Day pass issued.");

    [HttpGet("day-passes")]
    public Task<IActionResult> ListDayPasses(
        [FromQuery] Guid? clubId, [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] PaginationParams? pagination = null)
        => RunPaged(() => access.ListDayPassesAsync(clubId, from, to, pagination ?? new PaginationParams()));

    /// <summary>
    /// The entire front desk screen in one call: who is in, what is on, who is due, what needs doing.
    ///
    /// One call because the desk is used standing up, between conversations, and a screen that
    /// assembles itself from nine requests is a screen that is always half-loaded.
    /// </summary>
    [HttpGet("front-desk/{clubId:guid}")]
    public Task<IActionResult> GetFrontDesk(Guid clubId, [FromQuery] Guid? staffId)
        => Run(() => access.GetFrontDeskAsync(clubId, staffId));
}
