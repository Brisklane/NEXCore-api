using Fitness.Application.DTOs;
using Fitness.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Fitness.Api.Controllers;

/// <summary>
/// Class types, the timetable behind them, and every booking made against it.
///
/// The timetable is a recurring schedule that generates dated occurrences ahead of time, rather
/// than a list of individual classes — so a club edits "Spin, Mondays at 6:30" once, not fifty-two
/// times. Bookings, waitlists and attendance all hang off the occurrence.
/// </summary>
[Route("api/fitness/classes")]
public class ScheduleController(
    IScheduleService schedule,
    ILogger<ScheduleController> logger) : FitnessControllerBase(logger)
{
    // ── Class types ──────────────────────────────────────────────────────────

    [HttpGet("types")]
    public Task<IActionResult> GetClassTypes([FromQuery] Guid? clubId, [FromQuery] bool activeOnly = true)
        => Run(() => schedule.GetClassTypesAsync(clubId, activeOnly));

    [HttpPost("types")]
    public Task<IActionResult> SaveClassType([FromBody] SaveClassTypeDto request, [FromQuery] Guid? id = null)
        => Run(() => schedule.SaveClassTypeAsync(id, request, UserId), "Class type saved.");

    [HttpDelete("types/{id:guid}")]
    public Task<IActionResult> DeleteClassType(Guid id)
        => Run(() => schedule.DeleteClassTypeAsync(id, UserId), "Class type removed.");

    // ── The timetable ────────────────────────────────────────────────────────

    [HttpGet("schedules")]
    public Task<IActionResult> GetSchedules(
        [FromQuery] Guid clubId, [FromQuery] string? seasonCode, [FromQuery] bool publishedOnly = false)
        => Run(() => schedule.GetSchedulesAsync(clubId, seasonCode, publishedOnly));

    [HttpPost("schedules")]
    public Task<IActionResult> SaveSchedule([FromBody] SaveClassScheduleDto request, [FromQuery] Guid? id = null)
        => Run(() => schedule.SaveScheduleAsync(id, request, UserId), "Schedule saved.");

    [HttpDelete("schedules/{id:guid}")]
    public Task<IActionResult> DeleteSchedule(Guid id, [FromQuery] bool cancelFutureOccurrences = true)
        => Run(() => schedule.DeleteScheduleAsync(id, cancelFutureOccurrences, UserId), "Schedule removed.");

    /// <summary>
    /// Double-booked rooms, double-booked instructors, over-capacity rooms — before publishing.
    ///
    /// A timetable that puts the same instructor in two studios at 18:30 is a discovery best made
    /// on a Tuesday afternoon, not at 18:29 on a Monday.
    /// </summary>
    [HttpGet("schedules/conflicts")]
    public Task<IActionResult> CheckConflicts([FromQuery] Guid clubId, [FromQuery] string? seasonCode)
        => Run(() => schedule.CheckConflictsAsync(clubId, seasonCode));

    [HttpPost("schedules/{id:guid}/publish")]
    public Task<IActionResult> PublishSchedule(Guid id)
        => Run(() => schedule.PublishScheduleAsync(id, UserId), "Published — classes are now bookable.");

    /// <summary>Nightly job: rolls the generated horizon forward.</summary>
    [HttpPost("occurrences/generate")]
    public Task<IActionResult> GenerateOccurrences([FromQuery] Guid? clubId)
        => Run(() => schedule.GenerateOccurrencesAsync(clubId));

    [HttpGet("timetable")]
    public Task<IActionResult> GetTimetable(
        [FromQuery] Guid clubId, [FromQuery] DateTime from, [FromQuery] DateTime to,
        [FromQuery] Guid? classTypeId, [FromQuery] Guid? instructorStaffId,
        [FromQuery] Guid? roomId, [FromQuery] Guid? viewerMemberId)
        => Run(() => schedule.GetTimetableAsync(clubId, from, to, classTypeId, instructorStaffId, roomId, viewerMemberId));

    /// <summary>One class: the roster, the spot map, and the medical flags the instructor needs.</summary>
    [HttpGet("occurrences/{id:guid}")]
    public Task<IActionResult> GetOccurrence(Guid id, [FromQuery] Guid? viewerMemberId)
        => RunFound(() => schedule.GetOccurrenceAsync(id, viewerMemberId), "Class not found.");

    [HttpPut("occurrences")]
    public Task<IActionResult> UpdateOccurrence([FromBody] UpdateOccurrenceDto request)
        => Run(() => schedule.UpdateOccurrenceAsync(request, UserId), "Class updated.");

    /// <summary>Cancels one class, returns everyone's credit and notifies them.</summary>
    [HttpPost("occurrences/cancel")]
    public Task<IActionResult> CancelOccurrence([FromBody] CancelOccurrenceDto request)
        => Run(() => schedule.CancelOccurrenceAsync(request, UserId), "Class cancelled and members notified.");

    // ── Bookings ─────────────────────────────────────────────────────────────

    /// <summary>Can this member book this class, and if not, why not — in one sentence.</summary>
    [HttpGet("occurrences/{id:guid}/eligibility")]
    public Task<IActionResult> CheckEligibility(Guid id, [FromQuery] Guid memberId)
        => Run(() => schedule.CheckEligibilityAsync(id, memberId));

    [HttpPost("bookings")]
    public Task<IActionResult> Book([FromBody] CreateBookingDto request)
        => Run(() => schedule.BookAsync(request, UserId), "Booked.");

    /// <summary>What cancelling costs right now — credit back, credit lost, or a fee.</summary>
    [HttpGet("bookings/{id:guid}/cancel/preview")]
    public Task<IActionResult> PreviewCancel(Guid id)
        => Run(() => schedule.PreviewCancelAsync(id));

    [HttpPost("bookings/cancel")]
    public Task<IActionResult> CancelBooking([FromBody] CancelBookingDto request)
        => Run(() => schedule.CancelBookingAsync(request, UserId), "Booking cancelled.");

    [HttpPost("bookings/{id:guid}/check-in")]
    public Task<IActionResult> CheckInToClass(Guid id)
        => Run(() => schedule.CheckInToClassAsync(id, UserId), "Checked in.");

    [HttpGet("bookings/member/{memberId:guid}")]
    public Task<IActionResult> GetMemberBookings(
        Guid memberId, [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] bool upcomingOnly = true)
        => Run(() => schedule.GetMemberBookingsAsync(memberId, from, to, upcomingOnly));

    /// <summary>The instructor marking the register, from the studio screen.</summary>
    [HttpPost("attendance")]
    public Task<IActionResult> MarkAttendance([FromBody] MarkAttendanceDto request)
        => Run(() => schedule.MarkAttendanceAsync(request, UserId), "Register saved.");

    /// <summary>Frequent job: promotes waitlisted members into spots that have come free.</summary>
    [HttpPost("waitlists/process")]
    public Task<IActionResult> ProcessWaitlists()
        => Run(schedule.ProcessWaitlistsAsync);

    /// <summary>Frequent job: closes finished classes, marks no-shows and issues strikes.</summary>
    [HttpPost("occurrences/process-finished")]
    public Task<IActionResult> ProcessFinished()
        => Run(schedule.ProcessFinishedClassesAsync);

    // ── Policies ─────────────────────────────────────────────────────────────

    [HttpGet("policies/booking")]
    public Task<IActionResult> GetBookingPolicies([FromQuery] Guid? clubId)
        => Run(() => schedule.GetBookingPoliciesAsync(clubId));

    [HttpPost("policies/booking")]
    public Task<IActionResult> SaveBookingPolicy([FromBody] BookingPolicyDto request, [FromQuery] Guid? id = null)
        => Run(() => schedule.SaveBookingPolicyAsync(id, request, UserId), "Policy saved.");

    [HttpGet("policies/cancellation")]
    public Task<IActionResult> GetCancellationPolicies([FromQuery] Guid? clubId)
        => Run(() => schedule.GetCancellationPoliciesAsync(clubId));

    [HttpPost("policies/cancellation")]
    public Task<IActionResult> SaveCancellationPolicy(
        [FromBody] CancellationPolicyDto request, [FromQuery] Guid? id = null)
        => Run(() => schedule.SaveCancellationPolicyAsync(id, request, UserId), "Policy saved.");

    [HttpGet("strikes/{memberId:guid}")]
    public Task<IActionResult> GetStrikes(Guid memberId, [FromQuery] bool activeOnly = true)
        => Run(() => schedule.GetStrikesAsync(memberId, activeOnly));

    [HttpPost("strikes/{id:guid}/waive")]
    public Task<IActionResult> WaiveStrike(Guid id, [FromQuery] string reason)
        => Run(() => schedule.WaiveStrikeAsync(id, reason, UserId), "Strike waived.");
}
