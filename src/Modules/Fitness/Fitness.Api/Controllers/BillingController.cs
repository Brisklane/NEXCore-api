using Fitness.Application.DTOs;
using Fitness.Application.Services.Interfaces;
using Fitness.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;

namespace Fitness.Api.Controllers;

/// <summary>
/// Billing runs, invoices, payments and revenue recognition.
///
/// **No card data passes through here.** A payment method is stored as a provider token plus the
/// brand, last four and expiry the desk needs to recognise it. If something that looks like a
/// real card number arrives, the service rejects it outright rather than storing it.
/// </summary>
[Route("api/fitness/billing")]
public class BillingController(
    IBillingService billing,
    ILogger<BillingController> logger) : FitnessControllerBase(logger)
{
    // ── Schedules ────────────────────────────────────────────────────────────

    [HttpGet("schedules/{agreementId:guid}")]
    public Task<IActionResult> GetSchedule(Guid agreementId)
        => Run(() => billing.GetScheduleAsync(agreementId));

    /// <summary>Rebuilds the forward schedule after a change. Billed periods are never touched.</summary>
    [HttpPost("schedules/{agreementId:guid}/rebuild")]
    public Task<IActionResult> RebuildSchedule(Guid agreementId)
        => Run(() => billing.RebuildScheduleAsync(agreementId, UserId), "Schedule rebuilt.");

    // ── Runs ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Starts a billing run, in preview or for real.
    ///
    /// Resumable and idempotent: a run that dies halfway can be restarted without double-billing,
    /// because a schedule row is marked billed in the same save as the invoice that billed it.
    /// </summary>
    [HttpPost("runs")]
    public Task<IActionResult> StartRun([FromBody] StartBillingRunDto request)
        => Run(() => billing.StartRunAsync(request, UserId),
            request.PreviewOnly ? "Preview complete." : "Billing run complete.");

    [HttpGet("runs/{id:guid}")]
    public Task<IActionResult> GetRun(Guid id)
        => RunFound(() => billing.GetRunAsync(id), "Billing run not found.");

    [HttpGet("runs/{id:guid}/lines")]
    public Task<IActionResult> GetRunLines(
        Guid id, [FromQuery] string? outcome, [FromQuery] PaginationParams? pagination = null)
        => RunPaged(() => billing.GetRunLinesAsync(id, outcome, pagination ?? new PaginationParams()));

    [HttpGet("runs")]
    public Task<IActionResult> ListRuns(
        [FromQuery] Guid? clubId, [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] PaginationParams? pagination = null)
        => RunPaged(() => billing.ListRunsAsync(clubId, from, to, pagination ?? new PaginationParams()));

    /// <summary>The scheduled overnight run. Does nothing if auto-billing is switched off.</summary>
    [HttpPost("runs/scheduled")]
    public Task<IActionResult> RunScheduled()
        => Run(billing.RunScheduledBillingAsync);

    // ── Invoices ─────────────────────────────────────────────────────────────

    [HttpGet("invoices")]
    public Task<IActionResult> ListInvoices(
        [FromQuery] Guid? clubId, [FromQuery] Guid? memberId, [FromQuery] InvoiceStatus? status,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] bool overdueOnly = false,
        [FromQuery] PaginationParams? pagination = null)
        => RunPaged(() => billing.ListInvoicesAsync(clubId, memberId, status, from, to, overdueOnly,
            pagination ?? new PaginationParams()));

    [HttpGet("invoices/{id:guid}")]
    public Task<IActionResult> GetInvoice(Guid id)
        => RunFound(() => billing.GetInvoiceAsync(id), "Invoice not found.");

    [HttpPost("invoices")]
    public Task<IActionResult> CreateAdHocInvoice(
        [FromQuery] Guid memberId, [FromQuery] Guid clubId, [FromBody] List<InvoiceLineDto> lines)
        => Run(() => billing.CreateAdHocInvoiceAsync(memberId, clubId, lines, UserId), "Invoice raised.");

    [HttpPost("invoices/{id:guid}/cancel")]
    public Task<IActionResult> CancelInvoice(Guid id, [FromQuery] string reason)
        => Run(() => billing.CancelInvoiceAsync(id, reason, UserId), "Invoice cancelled.");

    // ── Payments ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Takes a payment and applies it oldest invoice first.
    ///
    /// Send an idempotency key. Tills get double-tapped, and a member charged twice at the desk
    /// is a complaint, a refund and a bad review.
    /// </summary>
    [HttpPost("payments")]
    public Task<IActionResult> TakePayment([FromBody] TakePaymentDto request)
        => Run(() => billing.TakePaymentAsync(request, UserId), "Payment taken.");

    [HttpGet("payments")]
    public Task<IActionResult> ListPayments(
        [FromQuery] Guid? clubId, [FromQuery] Guid? memberId, [FromQuery] PaymentStatus? status,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] PaginationParams? pagination = null)
        => RunPaged(() => billing.ListPaymentsAsync(clubId, memberId, status, from, to,
            pagination ?? new PaginationParams()));

    [HttpGet("payment-methods/{memberId:guid}")]
    public Task<IActionResult> GetPaymentMethods(Guid memberId)
        => Run(() => billing.GetPaymentMethodsAsync(memberId));

    /// <summary>Stores a provider token. Sending a card number here is rejected, deliberately.</summary>
    [HttpPost("payment-methods")]
    public Task<IActionResult> SavePaymentMethod([FromBody] SavePaymentMethodDto request)
        => Run(() => billing.SavePaymentMethodAsync(request, UserId), "Payment method saved.");

    [HttpDelete("payment-methods/{id:guid}")]
    public Task<IActionResult> DeletePaymentMethod(Guid id)
        => Run(() => billing.DeletePaymentMethodAsync(id, UserId), "Payment method removed.");

    // ── Adjustments ──────────────────────────────────────────────────────────

    [HttpPost("credit-notes")]
    public Task<IActionResult> IssueCreditNote([FromBody] IssueCreditNoteDto request)
        => Run(() => billing.IssueCreditNoteAsync(request, UserId), "Credit note issued.");

    [HttpPost("refunds")]
    public Task<IActionResult> IssueRefund([FromBody] IssueRefundDto request)
        => Run(() => billing.IssueRefundAsync(request, UserId), "Refund issued.");

    [HttpPost("write-offs")]
    public Task<IActionResult> WriteOff(
        [FromQuery] Guid memberId, [FromQuery] Guid? invoiceId,
        [FromQuery] decimal amount, [FromQuery] string reason)
        => Run(() => billing.WriteOffAsync(memberId, invoiceId, amount, reason, UserId), "Written off.");

    // ── Revenue ──────────────────────────────────────────────────────────────

    /// <summary>
    /// The deferred revenue roll-forward.
    ///
    /// An annual membership paid in January is not January's revenue. This is the schedule that
    /// says how much of it belongs to each month, and how much is still owed to the future.
    /// </summary>
    [HttpGet("deferred-revenue")]
    public Task<IActionResult> GetDeferredRevenue(
        [FromQuery] Guid? clubId, [FromQuery] DateTime from, [FromQuery] DateTime to)
        => Run(() => billing.GetDeferredRevenueAsync(clubId, from, to));

    [HttpPost("deferred-revenue/recognise-due")]
    public Task<IActionResult> RecogniseDue()
        => Run(billing.RecogniseDueRevenueAsync);
}
