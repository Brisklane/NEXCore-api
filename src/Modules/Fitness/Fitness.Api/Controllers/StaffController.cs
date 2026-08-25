using Fitness.Application.DTOs;
using Fitness.Application.Services.Interfaces;
using Fitness.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;

namespace Fitness.Api.Controllers;

/// <summary>
/// Staff, the rota, the time clock and commission.
///
/// PINs are BCrypt-hashed and never returned in the clear, including to a manager. A manager
/// override is a PIN check that is recorded — who approved what, when, and for whom — because an
/// override with no name against it is the hole every till fraud goes through.
/// </summary>
[Route("api/fitness/staff")]
public class StaffController(
    IStaffService staff,
    ILogger<StaffController> logger) : FitnessControllerBase(logger)
{
    [HttpGet]
    public Task<IActionResult> List(
        [FromQuery] Guid? clubId, [FromQuery] StaffRoleKind? role, [FromQuery] bool activeOnly = true,
        [FromQuery] string? search = null, [FromQuery] PaginationParams? pagination = null)
        => RunPaged(() => staff.ListAsync(clubId, role, activeOnly, search, pagination ?? new PaginationParams()));

    [HttpGet("{id:guid}")]
    public Task<IActionResult> Get(Guid id)
        => RunFound(() => staff.GetAsync(id), "Staff member not found.");

    [HttpPost]
    public Task<IActionResult> Create([FromBody] SaveStaffDto request)
        => Run(() => staff.SaveAsync(null, request, UserId), "Staff member added.");

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(Guid id, [FromBody] SaveStaffDto request)
        => Run(() => staff.SaveAsync(id, request, UserId), "Saved.");

    [HttpPost("{id:guid}/deactivate")]
    public Task<IActionResult> Deactivate(Guid id, [FromQuery] DateTime leftOn)
        => Run(() => staff.DeactivateAsync(id, leftOn, UserId), "Staff member deactivated.");

    /// <summary>PIN sign-in at the desk. Returns what this person is allowed to do, not their PIN.</summary>
    [HttpPost("pin/verify")]
    public Task<IActionResult> VerifyPin([FromBody] StaffPinLoginDto request)
        => Run(() => staff.VerifyPinAsync(request));

    /// <summary>A manager approving something above someone else's limit. Always recorded.</summary>
    [HttpPost("override/verify")]
    public Task<IActionResult> VerifyOverride([FromBody] ManagerOverrideDto request)
        => Run(() => staff.VerifyOverrideAsync(request, UserId));

    // ── Roles and tickets ────────────────────────────────────────────────────

    [HttpGet("roles")]
    public Task<IActionResult> GetRoles([FromQuery] Guid? clubId)
        => Run(() => staff.GetRolesAsync(clubId));

    [HttpPost("roles")]
    public Task<IActionResult> SaveRole([FromBody] StaffRoleDto request, [FromQuery] Guid? id = null)
        => Run(() => staff.SaveRoleAsync(id, request, UserId), "Role saved.");

    /// <summary>
    /// Qualifications and their expiry dates.
    ///
    /// An instructor teaching on a lapsed first-aid certificate is an insurance problem, so the
    /// expiring list is surfaced on the dashboard rather than buried in a personnel file.
    /// </summary>
    [HttpGet("certifications")]
    public Task<IActionResult> GetCertifications(
        [FromQuery] Guid? clubId, [FromQuery] Guid? staffId, [FromQuery] bool expiringOnly = false)
        => Run(() => staff.GetCertificationsAsync(clubId, staffId, expiringOnly));

    [HttpPost("certifications")]
    public Task<IActionResult> SaveCertification([FromBody] StaffCertificationDto request, [FromQuery] Guid? id = null)
        => Run(() => staff.SaveCertificationAsync(id, request, UserId), "Certification saved.");

    // ── Rota ─────────────────────────────────────────────────────────────────

    /// <summary>The week's rota, with the coverage gaps computed against opening hours.</summary>
    [HttpGet("rota")]
    public Task<IActionResult> GetRota(
        [FromQuery] Guid clubId, [FromQuery] DateTime from, [FromQuery] DateTime to)
        => Run(() => staff.GetRotaAsync(clubId, from, to));

    [HttpPost("shifts")]
    public Task<IActionResult> SaveShift([FromBody] SaveShiftDto request, [FromQuery] Guid? id = null)
        => Run(() => staff.SaveShiftAsync(id, request, UserId), "Shift saved.");

    [HttpDelete("shifts/{id:guid}")]
    public Task<IActionResult> DeleteShift(Guid id)
        => Run(() => staff.DeleteShiftAsync(id, UserId), "Shift removed.");

    [HttpPost("rota/publish")]
    public Task<IActionResult> PublishRota(
        [FromQuery] Guid clubId, [FromQuery] DateTime from, [FromQuery] DateTime to)
        => Run(() => staff.PublishRotaAsync(clubId, from, to, UserId), "Rota published.");

    [HttpPost("shifts/{assignmentId:guid}/swap")]
    public Task<IActionResult> RequestSwap(
        Guid assignmentId, [FromQuery] Guid? offerToStaffId, [FromQuery] string? reason)
        => Run(() => staff.RequestSwapAsync(assignmentId, offerToStaffId, reason, UserId), "Swap requested.");

    [HttpPost("swaps/{id:guid}/respond")]
    public Task<IActionResult> RespondToSwap(Guid id, [FromQuery] bool accept, [FromQuery] Guid staffId)
        => Run(() => staff.RespondToSwapAsync(id, accept, staffId, UserId), accept ? "Swap accepted." : "Swap declined.");

    [HttpGet("swaps")]
    public Task<IActionResult> GetSwapRequests([FromQuery] Guid clubId, [FromQuery] bool openOnly = true)
        => Run(() => staff.GetSwapRequestsAsync(clubId, openOnly));

    // ── Time clock ───────────────────────────────────────────────────────────

    [HttpPost("clock/in")]
    public Task<IActionResult> ClockIn([FromBody] ClockDto request)
        => Run(() => staff.ClockInAsync(request, UserId), "Clocked in.");

    [HttpPost("clock/out")]
    public Task<IActionResult> ClockOut([FromBody] ClockDto request)
        => Run(() => staff.ClockOutAsync(request, UserId), "Clocked out.");

    [HttpGet("timesheets/{staffId:guid}")]
    public Task<IActionResult> GetTimesheet(
        Guid staffId, [FromQuery] DateTime from, [FromQuery] DateTime to)
        => Run(() => staff.GetTimesheetAsync(staffId, from, to));

    /// <summary>Corrects a clock entry. The note is required — payroll adjustments need a reason.</summary>
    [HttpPost("timesheets/entries/{id:guid}/adjust")]
    public Task<IActionResult> AdjustEntry(
        Guid id, [FromQuery] DateTime? inAt, [FromQuery] DateTime? outAt,
        [FromQuery] int? breakMinutes, [FromQuery] string note = "")
        => Run(() => staff.AdjustEntryAsync(id, inAt, outAt, breakMinutes, note, UserId), "Entry adjusted.");

    [HttpPost("timesheets/{staffId:guid}/approve")]
    public Task<IActionResult> ApproveTimesheet(
        Guid staffId, [FromQuery] DateTime from, [FromQuery] DateTime to)
        => Run(() => staff.ApproveTimesheetAsync(staffId, from, to, UserId), "Timesheet approved.");

    // ── Commission ───────────────────────────────────────────────────────────

    [HttpGet("commission/rules")]
    public Task<IActionResult> GetCommissionRules([FromQuery] Guid? clubId, [FromQuery] Guid? staffId)
        => Run(() => staff.GetCommissionRulesAsync(clubId, staffId));

    [HttpPost("commission/rules")]
    public Task<IActionResult> SaveCommissionRule([FromBody] CommissionRuleDto request, [FromQuery] Guid? id = null)
        => Run(() => staff.SaveCommissionRuleAsync(id, request, UserId), "Rule saved.");

    [HttpPost("commission/statements/generate")]
    public Task<IActionResult> GenerateStatements([FromBody] GenerateCommissionDto request)
        => Run(() => staff.GenerateStatementsAsync(request, UserId), "Statements generated.");

    [HttpGet("commission/statements/{id:guid}")]
    public Task<IActionResult> GetStatement(Guid id)
        => RunFound(() => staff.GetStatementAsync(id), "Statement not found.");

    [HttpGet("commission/statements")]
    public Task<IActionResult> ListStatements(
        [FromQuery] Guid? clubId, [FromQuery] Guid? staffId,
        [FromQuery] CommissionStatementStatus? status, [FromQuery] PaginationParams? pagination = null)
        => RunPaged(() => staff.ListStatementsAsync(clubId, staffId, status, pagination ?? new PaginationParams()));

    [HttpPost("commission/statements/approve")]
    public Task<IActionResult> ApproveStatement([FromBody] ApproveStatementDto request)
        => Run(() => staff.ApproveStatementAsync(request, UserId), "Statement approved.");

    /// <summary>Marks a statement exported to payroll, so it cannot be paid twice.</summary>
    [HttpPost("commission/statements/{id:guid}/export")]
    public Task<IActionResult> ExportStatement(Guid id)
        => Run(() => staff.ExportStatementAsync(id, UserId), "Exported to payroll.");
}
