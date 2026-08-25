using Fitness.Application.DTOs;
using Fitness.Application.Services.Interfaces;
using Fitness.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;

namespace Fitness.Api.Controllers;

/// <summary>
/// The till, the cash drawer, corporate schemes and third-party payers.
///
/// Retail stock lives in Inventory. This records the sale and publishes the same depletion event
/// Point of Sale publishes, because a second item master is a second answer to "how many shakers
/// are in the cupboard" — and two answers is worse than none.
/// </summary>
[Route("api/fitness/commerce")]
public class CommerceController(
    ICommerceService commerce,
    ILogger<CommerceController> logger) : FitnessControllerBase(logger)
{
    // ── Pro shop ─────────────────────────────────────────────────────────────

    [HttpGet("products/{clubId:guid}")]
    public Task<IActionResult> GetProducts(Guid clubId, [FromQuery] string? search)
        => Run(() => commerce.GetProductsAsync(clubId, search));

    /// <summary>Rings up a sale. Send an idempotency key — tills get double-tapped.</summary>
    [HttpPost("sales")]
    public Task<IActionResult> CreateSale([FromBody] CreateSaleDto request)
        => Run(() => commerce.CreateSaleAsync(request, UserId), "Sale complete.");

    /// <summary>A return is its own negative sale, so the day's takings and the audit trail stay honest.</summary>
    [HttpPost("sales/{id:guid}/return")]
    public Task<IActionResult> ReturnSale(
        Guid id, [FromQuery] string reason, [FromBody] List<Guid>? lineIds = null)
        => Run(() => commerce.ReturnSaleAsync(id, reason, lineIds, UserId), "Return processed.");

    [HttpGet("sales")]
    public Task<IActionResult> ListSales(
        [FromQuery] Guid? clubId, [FromQuery] Guid? memberId,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] PaginationParams? pagination = null)
        => RunPaged(() => commerce.ListSalesAsync(clubId, memberId, from, to, pagination ?? new PaginationParams()));

    [HttpGet("house-account/{memberId:guid}")]
    public Task<IActionResult> GetHouseAccount(Guid memberId, [FromQuery] bool unsettledOnly = true)
        => Run(() => commerce.GetHouseAccountAsync(memberId, unsettledOnly));

    // ── Cash ─────────────────────────────────────────────────────────────────

    [HttpPost("cash-sessions")]
    public Task<IActionResult> OpenSession([FromBody] OpenCashSessionDto request)
        => Run(() => commerce.OpenSessionAsync(request, UserId), "Drawer open.");

    [HttpGet("cash-sessions/open")]
    public Task<IActionResult> GetOpenSession([FromQuery] Guid clubId, [FromQuery] Guid? staffId)
        => RunFound(() => commerce.GetOpenSessionAsync(clubId, staffId), "No session is open.");

    [HttpGet("cash-sessions/{id:guid}")]
    public Task<IActionResult> GetSession(Guid id)
        => RunFound(() => commerce.GetSessionAsync(id), "Session not found.");

    [HttpPost("cash-sessions/movements")]
    public Task<IActionResult> RecordMovement([FromBody] CashMovementRequestDto request)
        => Run(() => commerce.RecordMovementAsync(request, UserId), "Recorded.");

    /// <summary>
    /// Closes the drawer on a blind count.
    ///
    /// The expected figure is not shown before the count is entered. A visible target is a target,
    /// and a variance that can be reverse-engineered tells you nothing about the drawer.
    /// </summary>
    [HttpPost("cash-sessions/close")]
    public Task<IActionResult> CloseSession([FromBody] CloseCashSessionDto request)
        => Run(() => commerce.CloseSessionAsync(request, UserId), "Drawer closed.");

    [HttpGet("cash-sessions")]
    public Task<IActionResult> ListSessions(
        [FromQuery] Guid? clubId, [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] PaginationParams? pagination = null)
        => RunPaged(() => commerce.ListSessionsAsync(clubId, from, to, pagination ?? new PaginationParams()));

    /// <summary>The day-end read. X is a look; Z is the end of the day. Same numbers, deliberately.</summary>
    [HttpGet("day-end/{clubId:guid}")]
    public Task<IActionResult> GetDayEndRead(
        Guid clubId, [FromQuery] DateTime forDate, [FromQuery] bool isZRead = false)
        => Run(() => commerce.GetDayEndReadAsync(clubId, forDate, isZRead));

    // ── Gift cards ───────────────────────────────────────────────────────────

    [HttpPost("gift-cards")]
    public Task<IActionResult> IssueGiftCard([FromBody] IssueGiftCardDto request)
        => Run(() => commerce.IssueGiftCardAsync(request, UserId), "Gift card issued.");

    [HttpGet("gift-cards/{cardNumber}")]
    public Task<IActionResult> GetGiftCard(string cardNumber)
        => RunFound(() => commerce.GetGiftCardAsync(cardNumber), "That card was not recognised.");

    [HttpPost("gift-cards/{cardNumber}/redeem")]
    public Task<IActionResult> RedeemGiftCard(
        string cardNumber, [FromQuery] decimal amount, [FromQuery] Guid? saleId)
        => Run(() => commerce.RedeemGiftCardAsync(cardNumber, amount, saleId, UserId), "Redeemed.");

    // ── Corporate ────────────────────────────────────────────────────────────

    [HttpGet("corporate")]
    public Task<IActionResult> GetCorporateAccounts(
        [FromQuery] Guid? clubId, [FromQuery] bool activeOnly = true,
        [FromQuery] PaginationParams? pagination = null)
        => RunPaged(() => commerce.GetCorporateAccountsAsync(clubId, activeOnly, pagination ?? new PaginationParams()));

    [HttpGet("corporate/{id:guid}")]
    public Task<IActionResult> GetCorporateAccount(Guid id)
        => RunFound(() => commerce.GetCorporateAccountAsync(id), "Account not found.");

    [HttpPost("corporate")]
    public Task<IActionResult> SaveCorporateAccount([FromBody] CorporateAccountDto request, [FromQuery] Guid? id = null)
        => Run(() => commerce.SaveCorporateAccountAsync(id, request, UserId), "Account saved.");

    [HttpGet("corporate/{id:guid}/members")]
    public Task<IActionResult> GetCorporateMembers(Guid id, [FromQuery] bool activeOnly = true)
        => Run(() => commerce.GetCorporateMembersAsync(id, activeOnly));

    [HttpPost("corporate/{id:guid}/members")]
    public Task<IActionResult> AddCorporateMember(
        Guid id, [FromQuery] Guid memberId, [FromQuery] string? employeeReference)
        => Run(() => commerce.AddCorporateMemberAsync(id, memberId, employeeReference, UserId), "Added to scheme.");

    /// <summary>Checks an email domain, employee id or access code against the scheme's rules.</summary>
    [HttpGet("corporate/{id:guid}/eligibility")]
    public Task<IActionResult> CheckEligibility(
        Guid id, [FromQuery] string? email, [FromQuery] string? employeeReference, [FromQuery] string? code)
        => Run(() => commerce.CheckEligibilityAsync(id, email, employeeReference, code));

    /// <summary>
    /// The consolidated employer invoice, with the per-employee breakdown and their usage.
    ///
    /// Usage is on there because it is usually why the employer bought it — and an invoice with no
    /// evidence of use is an invoice that gets queried at renewal.
    /// </summary>
    [HttpPost("corporate/{id:guid}/invoices")]
    public Task<IActionResult> GenerateCorporateInvoice(
        Guid id, [FromQuery] DateTime periodStart, [FromQuery] DateTime periodEnd)
        => Run(() => commerce.GenerateCorporateInvoiceAsync(id, periodStart, periodEnd, UserId), "Invoice generated.");

    [HttpGet("corporate/invoices")]
    public Task<IActionResult> GetCorporateInvoices(
        [FromQuery] Guid? accountId, [FromQuery] InvoiceStatus? status,
        [FromQuery] PaginationParams? pagination = null)
        => RunPaged(() => commerce.GetCorporateInvoicesAsync(accountId, status, pagination ?? new PaginationParams()));

    // ── Third-party payers ───────────────────────────────────────────────────

    [HttpGet("payers")]
    public Task<IActionResult> GetPayers([FromQuery] Guid? clubId, [FromQuery] bool activeOnly = true)
        => Run(() => commerce.GetPayersAsync(clubId, activeOnly));

    [HttpPost("payers")]
    public Task<IActionResult> SavePayer([FromBody] ThirdPartyPayerDto request, [FromQuery] Guid? id = null)
        => Run(() => commerce.SavePayerAsync(id, request, UserId), "Payer saved.");

    [HttpPost("payers/authorisations")]
    public Task<IActionResult> SaveAuthorisation([FromBody] PayerAuthorisationDto request, [FromQuery] Guid? id = null)
        => Run(() => commerce.SaveAuthorisationAsync(id, request, UserId), "Authorisation saved.");

    [HttpGet("payers/authorisations")]
    public Task<IActionResult> GetAuthorisations(
        [FromQuery] Guid? payerId, [FromQuery] Guid? memberId, [FromQuery] bool activeOnly = true)
        => Run(() => commerce.GetAuthorisationsAsync(payerId, memberId, activeOnly));

    [HttpPost("vending")]
    public Task<IActionResult> RecordVendingRevenue([FromBody] VendingRevenueEntryDto request)
        => Run(() => commerce.RecordVendingRevenueAsync(request, UserId), "Recorded.");
}
