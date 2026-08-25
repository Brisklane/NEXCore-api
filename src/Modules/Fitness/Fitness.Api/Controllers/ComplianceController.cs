using Fitness.Application.DTOs;
using Fitness.Application.Services.Interfaces;
using Fitness.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;

namespace Fitness.Api.Controllers;

/// <summary>
/// Waivers, health screening, incidents, complaints and the daily safety checks.
///
/// This is the part of the app that exists because something went wrong once. A waiver edit
/// creates a new version rather than rewriting the old one — what a member signed in March is
/// what they signed, and an incident report that can be quietly amended afterwards is worth
/// nothing to anybody.
/// </summary>
[Route("api/fitness/compliance")]
public class ComplianceController(
    IComplianceService compliance,
    ILogger<ComplianceController> logger) : FitnessControllerBase(logger)
{
    // ── Waivers ──────────────────────────────────────────────────────────────

    [HttpGet("waivers/templates")]
    public Task<IActionResult> GetWaiverTemplates([FromQuery] Guid? clubId, [FromQuery] bool publishedOnly = false)
        => Run(() => compliance.GetWaiverTemplatesAsync(clubId, publishedOnly));

    /// <summary>Editing a published waiver creates the next version and supersedes existing signatures.</summary>
    [HttpPost("waivers/templates")]
    public Task<IActionResult> SaveWaiverTemplate([FromBody] WaiverTemplateDto request, [FromQuery] Guid? id = null)
        => Run(() => compliance.SaveWaiverTemplateAsync(id, request, UserId), "Waiver saved.");

    [HttpPost("waivers/templates/{id:guid}/publish")]
    public Task<IActionResult> PublishWaiver(Guid id)
        => Run(() => compliance.PublishWaiverAsync(id, UserId), "Waiver published.");

    /// <summary>Captures a signature. A guardian's is required below the club's age threshold.</summary>
    [HttpPost("waivers/sign")]
    public Task<IActionResult> SignWaiver([FromBody] SignWaiverDto request)
        => Run(() => compliance.SignWaiverAsync(request, UserId), "Signed.");

    [HttpGet("waivers/signatures")]
    public Task<IActionResult> GetSignatures(
        [FromQuery] Guid? memberId, [FromQuery] Guid? clubId, [FromQuery] SignatureStatus? status)
        => Run(() => compliance.GetSignaturesAsync(memberId, clubId, status));

    /// <summary>Members who cannot get through the door until they sign. The desk works this list.</summary>
    [HttpGet("waivers/outstanding")]
    public Task<IActionResult> GetOutstandingWaivers(
        [FromQuery] Guid? clubId, [FromQuery] PaginationParams? pagination = null)
        => RunPaged(() => compliance.GetOutstandingWaiversAsync(clubId, pagination ?? new PaginationParams()));

    // ── Health screening ─────────────────────────────────────────────────────

    /// <summary>The PAR-Q+ questions. Seven of the ten gate access until a clinician signs off.</summary>
    [HttpGet("screening/form")]
    public Task<IActionResult> GetScreeningForm([FromQuery] Guid? clubId)
        => Run(() => compliance.GetScreeningFormAsync(clubId));

    /// <summary>
    /// Submits a screening.
    ///
    /// A "yes" on a gating question raises a medical clearance, marks the member accordingly and
    /// records the flag on their record — because a trainer needs to know before the session, not
    /// after the ambulance.
    /// </summary>
    [HttpPost("screening")]
    public Task<IActionResult> SubmitScreening([FromBody] HealthScreeningDto request)
        => Run(() => compliance.SubmitScreeningAsync(request, UserId), "Screening recorded.");

    [HttpGet("screening/{memberId:guid}")]
    public Task<IActionResult> GetScreening(Guid memberId)
        => RunFound(() => compliance.GetScreeningAsync(memberId, UserId), "No screening on file.");

    [HttpPost("clearances")]
    public Task<IActionResult> SubmitClearance([FromBody] SubmitClearanceDto request)
        => Run(() => compliance.SubmitClearanceAsync(request, UserId), "Clearance submitted.");

    [HttpPost("clearances/review")]
    public Task<IActionResult> ReviewClearance([FromBody] ReviewClearanceDto request)
        => Run(() => compliance.ReviewClearanceAsync(request, UserId), "Clearance reviewed.");

    [HttpGet("clearances")]
    public Task<IActionResult> GetClearances([FromQuery] Guid? clubId, [FromQuery] ClearanceStatus? status)
        => Run(() => compliance.GetClearancesAsync(clubId, status));

    // ── Incidents ────────────────────────────────────────────────────────────

    [HttpGet("incidents")]
    public Task<IActionResult> GetIncidents(
        [FromQuery] Guid? clubId, [FromQuery] IncidentKind? kind, [FromQuery] IncidentStatus? status,
        [FromQuery] IncidentSeverity? severity, [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] PaginationParams? pagination = null)
        => RunPaged(() => compliance.GetIncidentsAsync(clubId, kind, status, severity, from, to,
            pagination ?? new PaginationParams()));

    [HttpGet("incidents/{id:guid}")]
    public Task<IActionResult> GetIncident(Guid id)
        => RunFound(() => compliance.GetIncidentAsync(id), "Incident not found.");

    /// <summary>Records an incident. Serious ones raise their own follow-up actions automatically.</summary>
    [HttpPost("incidents")]
    public Task<IActionResult> SaveIncident([FromBody] SaveIncidentDto request, [FromQuery] Guid? id = null)
        => Run(() => compliance.SaveIncidentAsync(id, request, UserId), "Incident recorded.");

    [HttpPost("incidents/{id:guid}/actions")]
    public Task<IActionResult> AddIncidentAction(Guid id, [FromBody] IncidentActionDto request)
        => Run(() => compliance.AddIncidentActionAsync(id, request, UserId), "Action added.");

    /// <summary>Closes an incident. Refused while actions are outstanding or the root cause is blank.</summary>
    [HttpPost("incidents/{id:guid}/close")]
    public Task<IActionResult> CloseIncident(
        Guid id, [FromQuery] string rootCause, [FromQuery] string preventiveAction)
        => Run(() => compliance.CloseIncidentAsync(id, rootCause, preventiveAction, UserId), "Incident closed.");

    // ── Complaints ───────────────────────────────────────────────────────────

    [HttpGet("complaints")]
    public Task<IActionResult> GetComplaints(
        [FromQuery] Guid? clubId, [FromQuery] ComplaintStatus? status,
        [FromQuery] PaginationParams? pagination = null)
        => RunPaged(() => compliance.GetComplaintsAsync(clubId, status, pagination ?? new PaginationParams()));

    [HttpPost("complaints")]
    public Task<IActionResult> SaveComplaint([FromBody] SaveComplaintDto request, [FromQuery] Guid? id = null)
        => Run(() => compliance.SaveComplaintAsync(id, request, UserId), "Complaint logged.");

    [HttpPost("complaints/resolve")]
    public Task<IActionResult> ResolveComplaint([FromBody] ResolveComplaintDto request)
        => Run(() => compliance.ResolveComplaintAsync(request, UserId), "Complaint resolved.");

    // ── Lost property ────────────────────────────────────────────────────────

    [HttpGet("lost-property")]
    public Task<IActionResult> GetLostProperty(
        [FromQuery] Guid? clubId, [FromQuery] LostPropertyStatus? status,
        [FromQuery] PaginationParams? pagination = null)
        => RunPaged(() => compliance.GetLostPropertyAsync(clubId, status, pagination ?? new PaginationParams()));

    [HttpPost("lost-property")]
    public Task<IActionResult> SaveLostProperty([FromBody] SaveLostPropertyDto request, [FromQuery] Guid? id = null)
        => Run(() => compliance.SaveLostPropertyAsync(id, request, UserId), "Item logged.");

    [HttpPost("lost-property/claim")]
    public Task<IActionResult> ClaimLostProperty([FromBody] ClaimLostPropertyDto request)
        => Run(() => compliance.ClaimLostPropertyAsync(request, UserId), "Returned to owner.");

    // ── Daily checks ─────────────────────────────────────────────────────────

    [HttpGet("checks")]
    public Task<IActionResult> GetChecks([FromQuery] Guid clubId, [FromQuery] bool dueTodayOnly = false)
        => Run(() => compliance.GetChecksAsync(clubId, dueTodayOnly));

    [HttpPost("checks")]
    public Task<IActionResult> SaveCheck([FromBody] FacilityCheckDto request, [FromQuery] Guid? id = null)
        => Run(() => compliance.SaveCheckAsync(id, request, UserId), "Check saved.");

    /// <summary>Submits a completed check. A failed critical item raises an incident, not a note.</summary>
    [HttpPost("checks/submit")]
    public Task<IActionResult> SubmitCheck([FromBody] SubmitFacilityCheckDto request)
        => Run(() => compliance.SubmitCheckAsync(request, UserId), "Check submitted.");

    // ── Handover ─────────────────────────────────────────────────────────────

    [HttpPost("handovers")]
    public Task<IActionResult> SaveHandover([FromBody] ShiftHandoverDto request)
        => Run(() => compliance.SaveHandoverAsync(request, UserId), "Handover saved.");

    [HttpGet("handovers")]
    public Task<IActionResult> GetHandovers([FromQuery] Guid clubId, [FromQuery] int limit = 20)
        => Run(() => compliance.GetHandoversAsync(clubId, limit));

    // ── Audit ────────────────────────────────────────────────────────────────

    /// <summary>
    /// The audit trail, including every read of a member's medical data.
    ///
    /// <c>sensitiveOnly</c> narrows it to exactly that — which is the query a data protection
    /// officer actually asks for.
    /// </summary>
    [HttpGet("audit")]
    public Task<IActionResult> GetAudit(
        [FromQuery] Guid? clubId, [FromQuery] Guid? memberId, [FromQuery] Guid? actorUserId,
        [FromQuery] string? entityType, [FromQuery] bool sensitiveOnly = false,
        [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null,
        [FromQuery] PaginationParams? pagination = null)
        => RunPaged(() => compliance.GetAuditAsync(clubId, memberId, actorUserId, entityType,
            sensitiveOnly, from, to, pagination ?? new PaginationParams()));
}
