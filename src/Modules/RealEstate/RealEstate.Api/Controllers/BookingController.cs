using Microsoft.AspNetCore.Mvc;
using RealEstate.Application.DTOs;
using RealEstate.Application.Services.Interfaces;
using RealEstate.Domain.Enums;

namespace RealEstate.Api.Controllers;

/// <summary>
/// Offers, expressions of interest, tokens, bookings, allotments, agreements and balloting.
///
/// A booking is the moment a unit stops being inventory and becomes somebody's home, so it is
/// preview-then-commit: the preview returns the exact arithmetic and every gate that would block
/// it, and the commit re-checks all of it. The two share one code path, because a preview that can
/// disagree with the commit is worse than no preview at all.
/// </summary>
[Route("api/realestate/bookings")]
public class BookingController(
    IBookingService bookings,
    ILogger<BookingController> logger) : RealEstateControllerBase(logger)
{
    // ── Offers ───────────────────────────────────────────────────────────────

    [HttpGet("offers")]
    public Task<IActionResult> GetOffers(
        [FromQuery] ListQueryDto query, [FromQuery] Guid? propertyId, [FromQuery] OfferStatus? status)
        => RunPaged(() => bookings.GetOffersAsync(query, propertyId, status));

    [HttpGet("offers/{id:guid}")]
    public Task<IActionResult> GetOffer(Guid id)
        => RunFound(() => bookings.GetOfferAsync(id), "That offer does not exist.");

    [HttpPost("offers")]
    public Task<IActionResult> SaveOffer([FromBody] OfferUpsertDto request)
        => Run(() => bookings.SaveOfferAsync(request, UserId), "Offer recorded.");

    [HttpPost("offers/decide")]
    public Task<IActionResult> DecideOffer([FromBody] OfferDecisionDto request)
        => Run(() => bookings.DecideOfferAsync(request, UserId), "Decision recorded.");

    // ── Expressions of interest ──────────────────────────────────────────────

    [HttpGet("eois")]
    public Task<IActionResult> GetEois([FromQuery] ListQueryDto query, [FromQuery] Guid? projectId)
        => RunPaged(() => bookings.GetEoisAsync(query, projectId));

    [HttpPost("eois")]
    public Task<IActionResult> SaveEoi(
        [FromBody] ExpressionOfInterestDto request, [FromQuery] bool withPayment = false)
        => Run(() => bookings.SaveEoiAsync(request, withPayment ? new ReceiptCreateDto() : null, UserId),
            "Expression of interest recorded.");

    [HttpPost("eois/{id:guid}/refund")]
    public Task<IActionResult> RefundEoi(Guid id)
        => Run(() => bookings.RefundEoiAsync(id, UserId), "Refund raised.");

    // ── Tokens ───────────────────────────────────────────────────────────────

    [HttpGet("tokens")]
    public Task<IActionResult> GetTokens(
        [FromQuery] ListQueryDto query, [FromQuery] Guid? projectId, [FromQuery] ReservationStatus? status)
        => RunPaged(() => bookings.GetTokensAsync(query, projectId, status));

    [HttpPost("tokens")]
    public Task<IActionResult> CreateToken([FromBody] TokenReservationCreateDto request)
        => Run(() => bookings.CreateTokenAsync(request, UserId), "Token taken.");

    [HttpPost("tokens/{id:guid}/cancel")]
    public Task<IActionResult> CancelToken(
        Guid id, [FromQuery] Guid? reasonCodeId, [FromQuery] bool forfeit = false)
        => Run(() => bookings.CancelTokenAsync(id, reasonCodeId, forfeit, UserId), "Token cancelled.");

    [HttpPost("tokens/expire")]
    public Task<IActionResult> ExpireTokens()
        => Run(bookings.ExpireTokensAsync, "Expired tokens released.");

    // ── Bookings ─────────────────────────────────────────────────────────────

    [HttpPost("search")]
    public Task<IActionResult> Search([FromBody] BookingSearchDto query)
        => RunPaged(() => bookings.GetBookingsAsync(query));

    [HttpGet("{id:guid}")]
    public Task<IActionResult> Get(Guid id)
        => RunFound(() => bookings.GetBookingAsync(id), "That booking does not exist.");

    /// <summary>
    /// The cost sheet, the schedule and every gate this booking would have to clear — without
    /// writing anything. What the customer is shown before they sign.
    /// </summary>
    [HttpPost("preview")]
    public Task<IActionResult> Preview([FromBody] BookingCreateDto request)
        => Run(() => bookings.PreviewBookingAsync(request));

    [HttpPost]
    public Task<IActionResult> Create([FromBody] BookingCreateDto request)
        => Run(() => bookings.CreateBookingAsync(request, UserId), "Booking created.");

    [HttpPost("approve")]
    public Task<IActionResult> Approve([FromBody] BookingApprovalDto request)
        => Run(() => bookings.ApproveBookingAsync(request, UserId), "Booking approved.");

    [HttpPost("{id:guid}/confirm")]
    public Task<IActionResult> Confirm(Guid id)
        => Run(() => bookings.ConfirmBookingAsync(id, UserId), "Booking confirmed.");

    // ── Amendments ───────────────────────────────────────────────────────────

    [HttpPost("amendments")]
    public Task<IActionResult> RequestAmendment([FromBody] BookingAmendmentDto request)
        => Run(() => bookings.RequestAmendmentAsync(request, UserId), "Amendment requested.");

    [HttpPost("amendments/{id:guid}/decide")]
    public Task<IActionResult> DecideAmendment(
        Guid id, [FromQuery] ApprovalOutcome outcome, [FromQuery] string? comment)
        => Run(() => bookings.DecideAmendmentAsync(id, outcome, comment, UserId), "Decision recorded.");

    // ── Allotment and agreement ──────────────────────────────────────────────

    [HttpPost("{id:guid}/allotment")]
    public Task<IActionResult> IssueAllotment(Guid id, [FromQuery] Guid? templateId)
        => Run(() => bookings.IssueAllotmentAsync(id, templateId, UserId), "Allotment issued.");

    /// <summary>Reissues an allotment letter, superseding the old one rather than replacing it.</summary>
    [HttpPost("allotments/{id:guid}/reissue")]
    public Task<IActionResult> ReissueAllotment(Guid id, [FromQuery] string reason)
        => Run(() => bookings.ReissueAllotmentAsync(id, reason, UserId), "Allotment reissued.");

    [HttpGet("allotments")]
    public Task<IActionResult> GetAllotments([FromQuery] ListQueryDto query, [FromQuery] Guid? projectId)
        => RunPaged(() => bookings.GetAllotmentsAsync(query, projectId));

    [HttpPost("{id:guid}/agreement")]
    public Task<IActionResult> GenerateAgreement(
        Guid id, [FromQuery] Guid? templateId, [FromQuery] string languageCode = "en")
        => Run(() => bookings.GenerateAgreementAsync(id, templateId, languageCode, UserId), "Agreement generated.");

    [HttpPost("agreements/{id:guid}/execution")]
    public Task<IActionResult> RecordExecution(
        Guid id, [FromQuery] DateOnly executedOn, [FromQuery] string? registrationNumber)
        => Run(() => bookings.RecordExecutionAsync(id, executedOn, registrationNumber, UserId), "Execution recorded.");

    // ── Balloting ────────────────────────────────────────────────────────────

    [HttpGet("ballots")]
    public Task<IActionResult> GetBallots([FromQuery] ListQueryDto query, [FromQuery] Guid? projectId)
        => RunPaged(() => bookings.GetBallotsAsync(query, projectId));

    [HttpGet("ballots/{id:guid}")]
    public Task<IActionResult> GetBallot(Guid id)
        => RunFound(() => bookings.GetBallotAsync(id), "That ballot does not exist.");

    [HttpPost("ballots")]
    public Task<IActionResult> SaveBallot([FromBody] BallotCreateDto request)
        => Run(() => bookings.SaveBallotAsync(request, UserId), "Ballot saved.");

    /// <summary>
    /// Freezes the pool of entries and the pool of units. Nothing may join or leave after this,
    /// which is the only thing that makes the draw afterwards defensible.
    /// </summary>
    [HttpPost("ballots/{id:guid}/lock")]
    public Task<IActionResult> LockPool(Guid id)
        => Run(() => bookings.LockPoolAsync(id, UserId), "Pool locked.");

    /// <summary>Draws the ballot from a recorded seed, so the same draw can be reproduced.</summary>
    [HttpPost("ballots/draw")]
    public Task<IActionResult> Draw([FromBody] BallotDrawDto request)
        => Run(() => bookings.DrawAsync(request, UserId), "Ballot drawn.");

    [HttpPost("ballots/{id:guid}/publish")]
    public Task<IActionResult> PublishBallot(Guid id)
        => Run(() => bookings.PublishBallotAsync(id, UserId), "Result published.");

    [HttpGet("ballots/{id:guid}/entries")]
    public Task<IActionResult> GetBallotEntries(Guid id, [FromQuery] Guid? categoryId)
        => Run(() => bookings.GetBallotEntriesAsync(id, categoryId));

    /// <summary>
    /// Overrides one allocation after a draw. Recorded loudly with its reason, because this is
    /// exactly what everybody suspects happens in a ballot and the record is the answer.
    /// </summary>
    [HttpPost("ballots/entries/{entryId:guid}/override")]
    public Task<IActionResult> OverrideAllocation(
        Guid entryId, [FromQuery] Guid unitId, [FromQuery] string reason)
        => Run(() => bookings.OverrideAllocationAsync(entryId, unitId, reason, UserId), "Allocation overridden.");
}
