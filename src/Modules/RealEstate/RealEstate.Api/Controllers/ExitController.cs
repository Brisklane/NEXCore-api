using Microsoft.AspNetCore.Mvc;
using RealEstate.Application.DTOs;
using RealEstate.Application.Services.Interfaces;
using RealEstate.Domain.Enums;

namespace RealEstate.Api.Controllers;

/// <summary>
/// How a customer leaves, or arrives at the end: cancellations and refunds, transfers of a file to
/// a new owner, possession and handover, snagging, defect liability and no-objection certificates.
///
/// Two things on this controller decide whether a company gets sued. A cancellation's arithmetic
/// is built once and used by both the preview and the commit, so what a customer was shown is what
/// they get. And a transfer is gated four ways — dues, litigation, documents, identity — because a
/// file transferred to the wrong person is not something that can be put back.
/// </summary>
[Route("api/realestate/exit")]
public class ExitController(
    IExitService exit,
    ILogger<ExitController> logger) : RealEstateControllerBase(logger)
{
    // ── Cancellation ─────────────────────────────────────────────────────────

    /// <summary>What would be deducted and what would come back, without cancelling anything.</summary>
    [HttpPost("cancellations/preview")]
    public Task<IActionResult> PreviewCancellation([FromBody] CancellationRequestDto request)
        => Run(() => exit.PreviewCancellationAsync(request));

    [HttpPost("cancellations")]
    public Task<IActionResult> RequestCancellation([FromBody] CancellationRequestDto request)
        => Run(() => exit.RequestCancellationAsync(request, UserId), "Cancellation requested.");

    [HttpPost("cancellations/{id:guid}/decide")]
    public Task<IActionResult> DecideCancellation(
        Guid id, [FromQuery] ApprovalOutcome outcome, [FromQuery] string? comment)
        => Run(() => exit.DecideCancellationAsync(id, outcome, comment, UserId), "Decision recorded.");

    [HttpGet("cancellations")]
    public Task<IActionResult> GetCancellations([FromQuery] ListQueryDto query)
        => RunPaged(() => exit.GetCancellationsAsync(query));

    [HttpGet("deduction-policies")]
    public Task<IActionResult> GetDeductionPolicies([FromQuery] Guid? projectId)
        => Run(() => exit.GetDeductionPoliciesAsync(projectId));

    [HttpPost("deduction-policies")]
    public Task<IActionResult> SaveDeductionPolicy([FromBody] DeductionPolicyDto request)
        => Run(() => exit.SaveDeductionPolicyAsync(request, UserId), "Policy saved.");

    // ── Refunds ──────────────────────────────────────────────────────────────

    [HttpPost("refunds")]
    public Task<IActionResult> CreateRefund([FromBody] RefundRequestDto request)
        => Run(() => exit.CreateRefundAsync(request, UserId), "Refund raised.");

    [HttpPost("refunds/{id:guid}/decide")]
    public Task<IActionResult> DecideRefund(
        Guid id,
        [FromQuery] ApprovalOutcome outcome,
        [FromQuery] decimal? approvedAmount,
        [FromQuery] string? comment)
        => Run(() => exit.DecideRefundAsync(id, outcome, approvedAmount, comment, UserId), "Decision recorded.");

    [HttpPost("refunds/schedule/{scheduleId:guid}/pay")]
    public Task<IActionResult> RecordRefundPayment(
        Guid scheduleId,
        [FromQuery] DateOnly paidOn,
        [FromQuery] PaymentInstrument instrument,
        [FromQuery] string? reference)
        => Run(() => exit.RecordRefundPaymentAsync(scheduleId, paidOn, instrument, reference, UserId),
            "Payment recorded.");

    [HttpGet("refunds")]
    public Task<IActionResult> GetRefunds([FromQuery] ListQueryDto query, [FromQuery] RefundStatus? status)
        => RunPaged(() => exit.GetRefundsAsync(query, status));

    /// <summary>
    /// Puts the unit back on the market and pays the customer out of the resale rather than out of
    /// cash flow. Common where a scheme's terms say refunds wait for a resale.
    /// </summary>
    [HttpPost("resales")]
    public Task<IActionResult> CreateResale([FromBody] ResaleRequestDto request)
        => Run(() => exit.CreateResaleAsync(request, UserId), "Resale registered.");

    // ── Transfers ────────────────────────────────────────────────────────────

    [HttpGet("transfers")]
    public Task<IActionResult> GetTransfers(
        [FromQuery] ListQueryDto query, [FromQuery] TransferStatus? status, [FromQuery] Guid? projectId)
        => RunPaged(() => exit.GetTransfersAsync(query, status, projectId));

    [HttpGet("transfers/{id:guid}")]
    public Task<IActionResult> GetTransfer(Guid id)
        => RunFound(() => exit.GetTransferAsync(id), "That transfer does not exist.");

    [HttpPost("transfers")]
    public Task<IActionResult> CreateTransfer([FromBody] TransferRequestCreateDto request)
        => Run(() => exit.CreateTransferAsync(request, UserId), "Transfer started.");

    [HttpPost("dues-clearance")]
    public Task<IActionResult> IssueDuesClearance(
        [FromQuery] Guid? bookingId,
        [FromQuery] Guid? unitId,
        [FromQuery] Guid? propertyId,
        [FromQuery] Guid partyId)
        => Run(() => exit.IssueDuesClearanceAsync(bookingId, unitId, propertyId, partyId, UserId),
            "Clearance issued.");

    /// <summary>
    /// Lets a transfer proceed with dues outstanding. Needs a reason and an approval, and is
    /// recorded as a high-risk override — this is the control everybody tries to go round.
    /// </summary>
    [HttpPost("transfers/{id:guid}/override-dues")]
    public Task<IActionResult> OverrideDues(Guid id, [FromQuery] string reason)
        => Run(() => exit.OverrideDuesAsync(id, reason, UserId), "Override recorded.");

    [HttpPost("transfers/{id:guid}/fees")]
    public Task<IActionResult> ComputeFees(Guid id)
        => Run(() => exit.ComputeFeesAsync(id, UserId), "Fees computed.");

    [HttpPost("transfers/{id:guid}/session")]
    public Task<IActionResult> ScheduleSession(
        Guid id, [FromQuery] DateTime scheduledAt, [FromQuery] string? venue)
        => Run(() => exit.ScheduleSessionAsync(id, scheduledAt, venue, UserId), "Session scheduled.");

    [HttpPut("transfers/session")]
    public Task<IActionResult> SaveSession([FromBody] TransferSessionDto request)
        => Run(() => exit.SaveSessionAsync(request, UserId), "Session saved.");

    [HttpPost("transfers/{id:guid}/complete")]
    public Task<IActionResult> CompleteTransfer(Guid id)
        => Run(() => exit.CompleteTransferAsync(id, UserId), "Transfer completed.");

    [HttpPost("transfers/{id:guid}/reject")]
    public Task<IActionResult> RejectTransfer(
        Guid id, [FromQuery] Guid reasonCodeId, [FromQuery] string? note)
        => Run(() => exit.RejectTransferAsync(id, reasonCodeId, note, UserId), "Transfer rejected.");

    /// <summary>Every owner this asset has ever had, in order, append-only.</summary>
    [HttpGet("ownership-chain")]
    public Task<IActionResult> GetOwnershipChain(
        [FromQuery] Guid? unitId, [FromQuery] Guid? propertyId, [FromQuery] Guid? plotFileId)
        => Run(() => exit.GetOwnershipChainAsync(unitId, propertyId, plotFileId));

    [HttpPost("duplicate-files")]
    public Task<IActionResult> CreateDuplicateFileRequest([FromBody] DuplicateFileRequestDto request)
        => Run(() => exit.CreateDuplicateFileRequestAsync(request, UserId), "Request recorded.");

    [HttpPost("duplicate-files/{id:guid}/issue")]
    public Task<IActionResult> IssueDuplicateFile(Guid id)
        => Run(() => exit.IssueDuplicateFileAsync(id, UserId), "Duplicate issued.");

    // ── Possession and handover ──────────────────────────────────────────────

    /// <summary>Whether this booking is ready for possession, and what is stopping it if not.</summary>
    [HttpGet("possession/{bookingId:guid}/evaluate")]
    public Task<IActionResult> EvaluatePossession(Guid bookingId)
        => Run(() => exit.EvaluatePossessionAsync(bookingId));

    [HttpPost("possession/{bookingId:guid}/offer")]
    public Task<IActionResult> OfferPossession(
        Guid bookingId,
        [FromQuery] DateOnly windowFrom,
        [FromQuery] DateOnly windowTo,
        [FromQuery] Guid? templateId)
        => Run(() => exit.OfferPossessionAsync(bookingId, windowFrom, windowTo, templateId, UserId),
            "Possession offered.");

    [HttpPost("possession/{id:guid}/appointment")]
    public Task<IActionResult> SetAppointment(Guid id, [FromQuery] DateOnly on)
        => Run(() => exit.SetAppointmentAsync(id, on, UserId), "Appointment set.");

    [HttpPost("possession/checklist/{checklistItemId:guid}/override")]
    public Task<IActionResult> OverrideChecklist(Guid checklistItemId, [FromQuery] string reason)
        => Run(() => exit.OverrideChecklistAsync(checklistItemId, reason, UserId), "Override recorded.");

    [HttpGet("possession")]
    public Task<IActionResult> GetPossessions(
        [FromQuery] ListQueryDto query, [FromQuery] PossessionStatus? status, [FromQuery] Guid? projectId)
        => RunPaged(() => exit.GetPossessionsAsync(query, status, projectId));

    [HttpPost("handovers")]
    public Task<IActionResult> CompleteHandover([FromBody] HandoverDto request)
        => Run(() => exit.CompleteHandoverAsync(request, UserId), "Handover completed.");

    [HttpGet("handovers/{id:guid}")]
    public Task<IActionResult> GetHandover(Guid id)
        => RunFound(() => exit.GetHandoverAsync(id), "That handover does not exist.");

    // ── Snagging ─────────────────────────────────────────────────────────────

    [HttpPost("inspections")]
    public Task<IActionResult> CreateInspection([FromBody] SnagInspectionDto request)
        => Run(() => exit.CreateInspectionAsync(request, UserId), "Inspection created.");

    [HttpGet("inspections/{id:guid}")]
    public Task<IActionResult> GetInspection(Guid id)
        => RunFound(() => exit.GetInspectionAsync(id), "That inspection does not exist.");

    [HttpGet("inspections")]
    public Task<IActionResult> GetInspections(
        [FromQuery] ListQueryDto query, [FromQuery] Guid? projectId, [FromQuery] bool openOnly = true)
        => RunPaged(() => exit.GetInspectionsAsync(query, projectId, openOnly));

    [HttpPost("snags")]
    public Task<IActionResult> SaveSnag([FromBody] SnagUpsertDto request)
        => Run(() => exit.SaveSnagAsync(request, UserId), "Snag saved.");

    /// <summary>
    /// Uploads a batch captured on a tablet with no signal. Idempotent on the client reference, so
    /// a surveyor who walks back into range and syncs twice does not create every snag twice.
    /// </summary>
    [HttpPost("snags/sync")]
    public Task<IActionResult> SyncSnags([FromBody] SnagSyncBatchDto batch)
        => Run(() => exit.SyncSnagsAsync(batch, UserId), "Snags synchronised.");

    [HttpPost("snags/{id:guid}/status")]
    public Task<IActionResult> ChangeSnagStatus(
        Guid id,
        [FromQuery] SnagStatus status,
        [FromQuery] string? note,
        [FromBody] List<SnagPhotoDto>? photos = null)
        => Run(() => exit.ChangeSnagStatusAsync(id, status, note, photos, UserId), "Status updated.");

    [HttpPost("inspections/{id:guid}/punch-list")]
    public Task<IActionResult> IssuePunchList(Guid id, [FromQuery] DateOnly? agreedClosureDate)
        => Run(() => exit.IssuePunchListAsync(id, agreedClosureDate, UserId), "Punch list issued.");

    // ── Defect liability ─────────────────────────────────────────────────────

    [HttpGet("defect-liabilities")]
    public Task<IActionResult> GetLiabilities(
        [FromQuery] Guid? unitId, [FromQuery] Guid? projectId, [FromQuery] bool activeOnly = true)
        => Run(() => exit.GetLiabilitiesAsync(unitId, projectId, activeOnly));

    [HttpPost("defect-claims")]
    public Task<IActionResult> CreateDefectClaim([FromBody] DefectClaimDto request)
        => Run(() => exit.CreateDefectClaimAsync(request, UserId), "Claim raised.");

    [HttpPost("defect-claims/{id:guid}/decide")]
    public Task<IActionResult> DecideDefectClaim(
        Guid id, [FromQuery] bool accepted, [FromQuery] string? reason)
        => Run(() => exit.DecideDefectClaimAsync(id, accepted, reason, UserId), "Decision recorded.");

    [HttpGet("defect-claims")]
    public Task<IActionResult> GetDefectClaims([FromQuery] ListQueryDto query, [FromQuery] TicketStatus? status)
        => RunPaged(() => exit.GetDefectClaimsAsync(query, status));

    // ── No-objection certificates ────────────────────────────────────────────

    [HttpPost("nocs")]
    public Task<IActionResult> RequestNoc([FromBody] NocRequestDto request)
        => Run(() => exit.RequestNocAsync(request, UserId), "NOC requested.");

    [HttpPost("nocs/{id:guid}/issue")]
    public Task<IActionResult> IssueNoc(Guid id)
        => Run(() => exit.IssueNocAsync(id, UserId), "NOC issued.");

    [HttpPost("nocs/{id:guid}/revoke")]
    public Task<IActionResult> RevokeNoc(Guid id, [FromQuery] string reason)
        => Run(() => exit.RevokeNocAsync(id, reason, UserId), "NOC revoked.");

    [HttpGet("nocs")]
    public Task<IActionResult> GetNocs(
        [FromQuery] ListQueryDto query, [FromQuery] NocKind? kind, [FromQuery] NocStatus? status)
        => RunPaged(() => exit.GetNocsAsync(query, kind, status));

    /// <summary>
    /// Checks a certificate somebody is holding against what was actually issued. Open to anybody
    /// with the code, because that is the only way a verification feature is worth anything.
    /// </summary>
    [HttpGet("nocs/verify/{code}")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public Task<IActionResult> VerifyNoc(string code)
        => RunFound(() => exit.VerifyNocAsync(code), "No certificate matches that code.");
}
