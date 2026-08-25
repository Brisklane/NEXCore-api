using Fitness.Application.DTOs;
using Fitness.Application.Services.Interfaces;
using Fitness.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;

namespace Fitness.Api.Controllers;

/// <summary>
/// One-to-one and small-group appointments: personal training, inductions, assessments, massage.
///
/// Session credits are a ledger, not a counter. A trainer is paid when a session is **signed
/// off**, not when the pack is sold — which is the difference between a trainer who sells ten
/// sessions and disappears and one who makes sure the member uses all ten.
/// </summary>
[Route("api/fitness/appointments")]
public class AppointmentController(
    IAppointmentService appointments,
    ILogger<AppointmentController> logger) : FitnessControllerBase(logger)
{
    // ── Who is bookable ──────────────────────────────────────────────────────

    [HttpGet("staff")]
    public Task<IActionResult> GetBookableStaff(
        [FromQuery] Guid? clubId, [FromQuery] Guid? serviceId, [FromQuery] bool activeOnly = true)
        => Run(() => appointments.GetBookableStaffAsync(clubId, serviceId, activeOnly));

    [HttpPost("staff")]
    public Task<IActionResult> SaveBookableStaff([FromBody] BookableStaffDto request, [FromQuery] Guid? id = null)
        => Run(() => appointments.SaveBookableStaffAsync(id, request, UserId), "Saved.");

    [HttpPut("staff/{id:guid}/availability")]
    public Task<IActionResult> SaveAvailability(Guid id, [FromBody] List<StaffAvailabilityDto> availability)
        => Run(() => appointments.SaveAvailabilityAsync(id, availability, UserId), "Availability saved.");

    [HttpPost("staff/time-off")]
    public Task<IActionResult> AddTimeOff([FromBody] StaffTimeOffDto request)
        => Run(() => appointments.AddTimeOffAsync(request, UserId), "Time off recorded.");

    /// <summary>
    /// Free slots matching a service, a date range and — if the member has one — their own coach.
    ///
    /// Their coach is offered first. Continuity is most of what a member is paying for.
    /// </summary>
    [HttpPost("availability/search")]
    public Task<IActionResult> FindAvailability([FromBody] AvailabilitySearchDto request)
        => Run(() => appointments.FindAvailabilityAsync(request));

    // ── The diary ────────────────────────────────────────────────────────────

    [HttpGet]
    public Task<IActionResult> List(
        [FromQuery] Guid? clubId, [FromQuery] Guid? staffId, [FromQuery] Guid? memberId,
        [FromQuery] AppointmentStatus? status, [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] PaginationParams? pagination = null)
        => RunPaged(() => appointments.ListAsync(clubId, staffId, memberId, status, from, to,
            pagination ?? new PaginationParams()));

    [HttpGet("{id:guid}")]
    public Task<IActionResult> Get(Guid id)
        => RunFound(() => appointments.GetAsync(id), "Appointment not found.");

    [HttpGet("diary")]
    public Task<IActionResult> GetDiary(
        [FromQuery] Guid clubId, [FromQuery] DateTime forDate, [FromQuery] Guid? staffId)
        => Run(() => appointments.GetDiaryAsync(clubId, forDate, staffId));

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateAppointmentDto request)
        => Run(() => appointments.CreateAsync(request, UserId), "Booked.");

    /// <summary>Moves an appointment by cancelling and re-creating it, linked — so the history survives.</summary>
    [HttpPost("{id:guid}/reschedule")]
    public Task<IActionResult> Reschedule(
        Guid id, [FromQuery] DateTime newStart, [FromQuery] Guid? newStaffId)
        => Run(() => appointments.RescheduleAsync(id, newStart, newStaffId, UserId), "Rescheduled.");

    [HttpPost("{id:guid}/cancel")]
    public Task<IActionResult> Cancel(
        Guid id, [FromQuery] string? reason, [FromQuery] bool waivePenalty = false)
        => Run(() => appointments.CancelAsync(id, reason, waivePenalty, UserId), "Cancelled.");

    [HttpPost("{id:guid}/check-in")]
    public Task<IActionResult> CheckIn(Guid id, [FromQuery] Guid? memberId)
        => Run(() => appointments.CheckInAsync(id, memberId, UserId), "Checked in.");

    /// <summary>
    /// Closes a session: consumes the credit, accrues the trainer's commission and releases the
    /// deferred revenue. One action, because doing any of the three without the others leaves the
    /// books wrong.
    /// </summary>
    [HttpPost("sign-off")]
    public Task<IActionResult> SignOff([FromBody] SignOffSessionDto request)
        => Run(() => appointments.SignOffAsync(request, UserId), "Session signed off.");

    [HttpPost("{id:guid}/no-show")]
    public Task<IActionResult> MarkNoShow(
        Guid id, [FromQuery] Guid? memberId, [FromQuery] bool waivePenalty = false)
        => Run(() => appointments.MarkNoShowAsync(id, memberId, waivePenalty, UserId), "Recorded.");

    // ── Credits ──────────────────────────────────────────────────────────────

    [HttpPost("packages")]
    public Task<IActionResult> SellPackage([FromBody] SellPackageDto request)
        => Run(() => appointments.SellPackageAsync(request, UserId), "Package sold.");

    [HttpGet("packages")]
    public Task<IActionResult> GetPackages(
        [FromQuery] Guid? clubId, [FromQuery] Guid? memberId, [FromQuery] bool activeOnly = true)
        => Run(() => appointments.GetPackagesAsync(clubId, memberId, activeOnly));

    [HttpGet("credits/{memberId:guid}")]
    public Task<IActionResult> GetCredits(Guid memberId)
        => Run(() => appointments.GetCreditsAsync(memberId));

    [HttpPost("credits/adjust")]
    public Task<IActionResult> AdjustCredits([FromBody] AdjustCreditsDto request)
        => Run(() => appointments.AdjustCreditsAsync(request, UserId), "Credits adjusted.");

    /// <summary>Nightly job: expires credits whose validity ran out.</summary>
    [HttpPost("credits/expire-due")]
    public Task<IActionResult> ExpireDue()
        => Run(appointments.ExpireDueCreditsAsync);

    // ── Coaching ─────────────────────────────────────────────────────────────

    [HttpPost("coaches/assign")]
    public Task<IActionResult> AssignCoach(
        [FromQuery] Guid memberId, [FromQuery] Guid staffId, [FromQuery] bool isPrimary = true)
        => Run(() => appointments.AssignCoachAsync(memberId, staffId, isPrimary, UserId), "Coach assigned.");

    [HttpGet("coaches/{staffId:guid}/clients")]
    public Task<IActionResult> GetCoachClients(Guid staffId, [FromQuery] bool activeOnly = true)
        => Run(() => appointments.GetCoachClientsAsync(staffId, activeOnly));

    /// <summary>A trainer's whole day in one call: sessions, classes, gaps and who needs chasing.</summary>
    [HttpGet("trainer-day/{staffId:guid}")]
    public Task<IActionResult> GetTrainerDay(Guid staffId, [FromQuery] DateTime forDate)
        => Run(() => appointments.GetTrainerDayAsync(staffId, forDate));
}
