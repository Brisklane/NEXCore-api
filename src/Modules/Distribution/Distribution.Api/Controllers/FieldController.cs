using Distribution.Application.DTOs;
using Distribution.Application.Services.Interfaces;
using Distribution.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;

namespace Distribution.Api.Controllers;

/// <summary>
/// The field terminal: the day, the beat, the visit and everything captured at the counter.
///
/// The write endpoints all accept a client idempotency key. A rep on a bad connection will retry,
/// and a double-submitted check-in that becomes two visits quietly corrupts coverage and strike
/// rate for the whole month.
/// </summary>
[Route("api/distribution/field")]
public class FieldController(IFieldService service, ILogger<FieldController> logger)
    : DistributionControllerBase(logger)
{
    // ── Reps ─────────────────────────────────────────────────────────────────

    [HttpGet("reps")]
    public Task<IActionResult> ListReps(
        [FromQuery] string? search, [FromQuery] FieldRole? role, [FromQuery] Guid? territoryId,
        [FromQuery] Guid? partnerId, [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListRepsAsync(
            search, role, territoryId, partnerId, pagination ?? new PaginationParams()));

    [HttpGet("reps/{id:guid}")]
    public Task<IActionResult> GetRep(Guid id)
        => RunFound(() => service.GetRepAsync(id), "That field rep no longer exists.");

    [HttpPost("reps")]
    public Task<IActionResult> CreateRep([FromBody] SaveFieldRepDto request)
        => Run(() => service.SaveRepAsync(null, request, UserId), "Field rep created.");

    [HttpPut("reps/{id:guid}")]
    public Task<IActionResult> UpdateRep(Guid id, [FromBody] SaveFieldRepDto request)
        => Run(() => service.SaveRepAsync(id, request, UserId), "Field rep saved.");

    [HttpDelete("reps/{id:guid}")]
    public Task<IActionResult> DeleteRep(Guid id)
        => Run(() => service.DeleteRepAsync(id, UserId), "Field rep removed.");

    /// <summary>PIN login on a shared device. Returns 404 rather than leaking which half was wrong.</summary>
    [HttpPost("reps/authenticate")]
    public Task<IActionResult> Authenticate([FromQuery] string code, [FromQuery] string pin)
        => RunFound(() => service.AuthenticatePinAsync(code, pin), "That code and PIN do not match.");

    // ── Devices ──────────────────────────────────────────────────────────────

    [HttpGet("devices")]
    public Task<IActionResult> Devices([FromQuery] Guid? fieldRepId, [FromQuery] bool? staleOnly)
        => Run(() => service.ListDevicesAsync(fieldRepId, staleOnly));

    [HttpPost("devices")]
    public Task<IActionResult> RegisterDevice([FromBody] FieldDeviceDto request)
        => Run(() => service.RegisterDeviceAsync(request, UserId), "Device registered.");

    [HttpPost("devices/{id:guid}/block")]
    public Task<IActionResult> BlockDevice(
        Guid id, [FromQuery] bool isBlocked = true, [FromQuery] string? reason = null)
        => Run(() => service.BlockDeviceAsync(id, isBlocked, reason, UserId),
            isBlocked ? "Device blocked." : "Device unblocked.");

    [HttpPost("devices/{id:guid}/wipe")]
    public Task<IActionResult> WipeDevice(Guid id)
        => Run(() => service.RequestWipeAsync(id, UserId), "Wipe requested.");

    // ── The day ──────────────────────────────────────────────────────────────

    [HttpPost("days/start")]
    public Task<IActionResult> StartDay([FromBody] StartDayDto request)
        => Run(() => service.StartDayAsync(request, UserId), "Day started.");

    [HttpGet("days/{id:guid}")]
    public Task<IActionResult> GetDay(Guid id)
        => RunFound(() => service.GetDayAsync(id), "That day no longer exists.");

    [HttpGet("days/today/{fieldRepId:guid}")]
    public Task<IActionResult> Today(Guid fieldRepId, [FromQuery] DateTime? workDate)
        => RunFound(() => service.GetTodayAsync(fieldRepId, workDate), "No day has been started.");

    /// <summary>The field terminal's boot call: the whole day in one round trip.</summary>
    [HttpGet("days/{id:guid}/board")]
    public Task<IActionResult> Board(Guid id)
        => RunFound(() => service.GetDayBoardAsync(id), "That day no longer exists.");

    [HttpPost("days/close")]
    public Task<IActionResult> CloseDay([FromBody] CloseDayDto request)
        => Run(() => service.CloseDayAsync(request, UserId), "Day closed.");

    [HttpGet("days")]
    public Task<IActionResult> ListDays(
        [FromQuery] Guid? fieldRepId, [FromQuery] Guid? routeId, [FromQuery] DateTime? from,
        [FromQuery] DateTime? to, [FromQuery] FieldDayStatus? status, [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListDaysAsync(
            fieldRepId, routeId, from, to, status, pagination ?? new PaginationParams()));

    // ── Visits ───────────────────────────────────────────────────────────────

    [HttpPost("visits/check-in")]
    public Task<IActionResult> CheckIn([FromBody] CheckInDto request)
        => Run(() => service.CheckInAsync(request, UserId), "Checked in.");

    [HttpPost("visits/check-out")]
    public Task<IActionResult> CheckOut([FromBody] CheckOutDto request)
        => Run(() => service.CheckOutAsync(request, UserId), "Checked out.");

    [HttpGet("visits/{id:guid}")]
    public Task<IActionResult> GetVisit(Guid id)
        => RunFound(() => service.GetVisitAsync(id), "That visit no longer exists.");

    [HttpGet("visits")]
    public Task<IActionResult> ListVisits(
        [FromQuery] Guid? fieldRepId, [FromQuery] Guid? outletId, [FromQuery] Guid? routeId,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] VisitStatus? status,
        [FromQuery] bool? outOfFenceOnly, [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListVisitsAsync(
            fieldRepId, outletId, routeId, from, to, status, outOfFenceOnly,
            pagination ?? new PaginationParams()));

    // ── Tasks ────────────────────────────────────────────────────────────────

    [HttpGet("tasks")]
    public Task<IActionResult> Tasks(
        [FromQuery] Guid? fieldRepId, [FromQuery] Guid? outletId,
        [FromQuery] Guid? routeId, [FromQuery] bool? openOnly)
        => Run(() => service.ListTasksAsync(fieldRepId, outletId, routeId, openOnly));

    [HttpPost("tasks")]
    public Task<IActionResult> CreateTask([FromBody] SaveVisitTaskDto request)
        => Run(() => service.SaveTaskAsync(null, request, UserId), "Task created.");

    [HttpPut("tasks/{id:guid}")]
    public Task<IActionResult> UpdateTask(Guid id, [FromBody] SaveVisitTaskDto request)
        => Run(() => service.SaveTaskAsync(id, request, UserId), "Task saved.");

    [HttpPost("tasks/complete")]
    public Task<IActionResult> CompleteTask([FromBody] CompleteVisitTaskDto request)
        => Run(() => service.CompleteTaskAsync(request, UserId), "Task completed.");

    [HttpDelete("tasks/{id:guid}")]
    public Task<IActionResult> DeleteTask(Guid id)
        => Run(() => service.DeleteTaskAsync(id, UserId), "Task removed.");

    // ── Surveys ──────────────────────────────────────────────────────────────

    [HttpGet("surveys")]
    public Task<IActionResult> Surveys([FromQuery] bool? activeOnly)
        => Run(() => service.ListSurveyFormsAsync(activeOnly));

    [HttpGet("surveys/{id:guid}")]
    public Task<IActionResult> Survey(Guid id)
        => RunFound(() => service.GetSurveyFormAsync(id), "That survey no longer exists.");

    [HttpPost("surveys")]
    public Task<IActionResult> CreateSurvey([FromBody] SurveyFormDto request)
        => Run(() => service.SaveSurveyFormAsync(null, request, UserId), "Survey created.");

    [HttpPut("surveys/{id:guid}")]
    public Task<IActionResult> UpdateSurvey(Guid id, [FromBody] SurveyFormDto request)
        => Run(() => service.SaveSurveyFormAsync(id, request, UserId), "Survey saved.");

    [HttpDelete("surveys/{id:guid}")]
    public Task<IActionResult> DeleteSurvey(Guid id)
        => Run(() => service.DeleteSurveyFormAsync(id, UserId), "Survey removed.");

    [HttpPost("surveys/submit")]
    public Task<IActionResult> SubmitSurvey([FromBody] SubmitSurveyDto request)
        => Run(() => service.SubmitSurveyAsync(request, UserId), "Survey submitted.");

    [HttpGet("surveys/responses")]
    public Task<IActionResult> SurveyResponses(
        [FromQuery] Guid? formId, [FromQuery] Guid? outletId, [FromQuery] DateTime? from,
        [FromQuery] DateTime? to, [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListSurveyResponsesAsync(
            formId, outletId, from, to, pagination ?? new PaginationParams()));

    // ── Merchandising ────────────────────────────────────────────────────────

    [HttpPost("audits")]
    public Task<IActionResult> SubmitAudit([FromBody] SubmitAuditDto request)
        => Run(() => service.SubmitAuditAsync(request, UserId), "Audit recorded.");

    [HttpGet("audits/{id:guid}")]
    public Task<IActionResult> GetAudit(Guid id)
        => RunFound(() => service.GetAuditAsync(id), "That audit no longer exists.");

    [HttpGet("audits")]
    public Task<IActionResult> ListAudits(
        [FromQuery] Guid? outletId, [FromQuery] Guid? fieldRepId, [FromQuery] AuditKind? kind,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListAuditsAsync(
            outletId, fieldRepId, kind, from, to, pagination ?? new PaginationParams()));

    [HttpPost("competitors")]
    public Task<IActionResult> RecordCompetitor([FromBody] CompetitorObservationDto request)
        => Run(() => service.RecordCompetitorAsync(request, UserId), "Observation recorded.");

    [HttpGet("competitors")]
    public Task<IActionResult> Competitors(
        [FromQuery] Guid? outletId, [FromQuery] string? competitorName, [FromQuery] DateTime? from,
        [FromQuery] DateTime? to, [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListCompetitorObservationsAsync(
            outletId, competitorName, from, to, pagination ?? new PaginationParams()));

    [HttpPost("posm")]
    public Task<IActionResult> RecordPosm([FromBody] PosmPlacementDto request)
        => Run(() => service.RecordPosmAsync(request, UserId), "Placement recorded.");

    [HttpGet("posm")]
    public Task<IActionResult> Posm(
        [FromQuery] Guid? outletId, [FromQuery] Guid? schemeId, [FromQuery] bool? activeOnly)
        => Run(() => service.ListPosmAsync(outletId, schemeId, activeOnly));
}
