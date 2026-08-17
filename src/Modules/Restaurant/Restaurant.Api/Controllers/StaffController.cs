using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.DTOs;
using Restaurant.Application.Services.Interfaces;
using Restaurant.Domain.Enums;

namespace Restaurant.Api.Controllers;

/// <summary>Staff records, PIN sign-in, the roster, the time clock and tip pooling.</summary>
[Route("api/restaurant/staff")]
public class StaffController(
    IRestaurantStaffService staff,
    ILogger<StaffController> logger) : RestaurantControllerBase(logger)
{
    // ── People ───────────────────────────────────────────────────────────────

    [HttpGet]
    public Task<IActionResult> GetStaff(
        [FromQuery] Guid? outletId, [FromQuery] StaffRole? role, [FromQuery] bool activeOnly = true)
        => Run(() => staff.GetStaffAsync(outletId, role, activeOnly));

    [HttpGet("{id:guid}")]
    public Task<IActionResult> GetById(Guid id)
        => RunFound(() => staff.GetStaffMemberAsync(id), "Staff member not found.");

    [HttpPost]
    public Task<IActionResult> Create([FromBody] SaveRestaurantStaffDto request)
        => Run(() => staff.SaveStaffAsync(null, request, UserId), "Staff member added.");

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(Guid id, [FromBody] SaveRestaurantStaffDto request)
        => Run(() => staff.SaveStaffAsync(id, request, UserId), "Staff member saved.");

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(Guid id)
        => Run(() => staff.DeleteStaffAsync(id, UserId), "Staff member removed.");

    // ── PIN ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Signs a waiter in on a shared till. Returns what they are allowed to do, so the screen can
    /// hide what they cannot — while the server still enforces it independently.
    /// </summary>
    [HttpPost("pin-login")]
    public Task<IActionResult> PinLogin([FromBody] StaffPinLoginDto request)
        => RunFound(() => staff.PinLoginAsync(request), "That PIN was not recognised.");

    [HttpPost("set-pin")]
    public Task<IActionResult> SetPin([FromBody] SetStaffPinDto request)
        => Run(() => staff.SetPinAsync(request, UserId), "PIN set.");

    /// <summary>Checks a supervisor PIN against one permission, for an over-the-shoulder approval.</summary>
    [HttpPost("verify-approval")]
    public Task<IActionResult> VerifyApproval(
        [FromQuery] Guid outletId, [FromQuery] string pin, [FromQuery] string permission)
        => RunFound(() => staff.VerifyApprovalAsync(outletId, pin, permission),
                    "That PIN is not authorised for this action.");

    // ── Shifts ───────────────────────────────────────────────────────────────

    [HttpGet("shifts/{outletId:guid}")]
    public Task<IActionResult> GetShifts(
        Guid outletId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] Guid? staffId)
        => Run(() => staff.GetShiftsAsync(
            outletId,
            from ?? DateTime.UtcNow.Date.AddDays(-7),
            to ?? DateTime.UtcNow.Date.AddDays(7),
            staffId));

    [HttpPost("shifts")]
    public Task<IActionResult> CreateShift([FromBody] SaveStaffShiftDto request)
        => Run(() => staff.SaveShiftAsync(null, request, UserId), "Shift added.");

    [HttpPut("shifts/{id:guid}")]
    public Task<IActionResult> UpdateShift(Guid id, [FromBody] SaveStaffShiftDto request)
        => Run(() => staff.SaveShiftAsync(id, request, UserId), "Shift saved.");

    [HttpDelete("shifts/{id:guid}")]
    public Task<IActionResult> DeleteShift(Guid id)
        => Run(() => staff.DeleteShiftAsync(id, UserId), "Shift removed.");

    // ── Time clock ───────────────────────────────────────────────────────────

    [HttpPost("clock-in")]
    public Task<IActionResult> ClockIn([FromBody] ClockDto request)
        => Run(() => staff.ClockInAsync(request, UserId), request.IsBreak ? "On break." : "Clocked in.");

    [HttpPost("clock-out/{staffId:guid}")]
    public Task<IActionResult> ClockOut(Guid staffId)
        => Run(() => staff.ClockOutAsync(staffId, UserId), "Clocked out.");

    [HttpGet("time-clock/{outletId:guid}")]
    public Task<IActionResult> GetTimeClock(
        Guid outletId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] Guid? staffId)
        => Run(() => staff.GetTimeClockAsync(
            outletId,
            from ?? DateTime.UtcNow.Date.AddDays(-7),
            to ?? DateTime.UtcNow,
            staffId));

    // ── Tips ─────────────────────────────────────────────────────────────────

    [HttpGet("tips/{outletId:guid}")]
    public Task<IActionResult> GetTips(
        Guid outletId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] Guid? waiterId)
        => Run(() => staff.GetTipsAsync(
            outletId,
            from ?? DateTime.UtcNow.Date,
            to ?? DateTime.UtcNow,
            waiterId));

    [HttpGet("tip-pools/{outletId:guid}")]
    public Task<IActionResult> GetTipPools(Guid outletId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Run(() => staff.GetTipPoolsAsync(
            outletId,
            from ?? DateTime.UtcNow.Date.AddDays(-30),
            to ?? DateTime.UtcNow));

    [HttpPost("tip-pools")]
    public Task<IActionResult> CreateTipPool([FromBody] CreateTipPoolDto request)
        => Run(() => staff.CreateTipPoolAsync(request, UserId), "Tip pool created.");

    [HttpPost("tip-pools/{id:guid}/calculate")]
    public Task<IActionResult> CalculateTipPool(Guid id)
        => Run(() => staff.CalculateTipPoolAsync(id, UserId), "Shares recalculated.");

    [HttpPost("tip-pools/{id:guid}/finalise")]
    public Task<IActionResult> FinaliseTipPool(Guid id)
        => Run(() => staff.FinaliseTipPoolAsync(id, UserId), "Tip pool finalised.");
}
