using Microsoft.AspNetCore.Mvc;
using RealEstate.Application.DTOs;
using RealEstate.Application.Services.Interfaces;
using RealEstate.Domain.Enums;

namespace RealEstate.Api.Controllers;

/// <summary>
/// Deals through to completion, commission, and the channel partner network.
///
/// Commission is where an agency's staff check the software's arithmetic every single month, so
/// every calculation carries its own trace: which plan, which tier, which cap, and what each
/// deduction was for. A figure somebody cannot follow is a figure they will dispute, and they will
/// be right to.
/// </summary>
[Route("api/realestate/brokerage")]
public class BrokerageController(
    IBrokerageService brokerage,
    ILogger<BrokerageController> logger) : RealEstateControllerBase(logger)
{
    // ── Deals ────────────────────────────────────────────────────────────────

    [HttpGet("deals")]
    public Task<IActionResult> GetDeals(
        [FromQuery] ListQueryDto query, [FromQuery] DealStatus? status, [FromQuery] Guid? agentId)
        => RunPaged(() => brokerage.GetDealsAsync(query, status, agentId));

    [HttpGet("deals/board")]
    public Task<IActionResult> GetBoard([FromQuery] ListQueryDto query, [FromQuery] Guid? agentId)
        => Run(() => brokerage.GetDealBoardAsync(query, agentId));

    [HttpGet("deals/{id:guid}")]
    public Task<IActionResult> GetDeal(Guid id)
        => RunFound(() => brokerage.GetDealAsync(id), "That deal does not exist.");

    [HttpPost("deals")]
    public Task<IActionResult> CreateDeal([FromBody] DealCreateDto request)
        => Run(() => brokerage.CreateDealAsync(request, UserId), "Deal created.");

    [HttpPost("deals/{dealId:guid}/checklist/{itemId:guid}")]
    public Task<IActionResult> UpdateChecklist(
        Guid dealId,
        Guid itemId,
        [FromQuery] bool completed,
        [FromQuery] DateOnly? completedOn,
        [FromQuery] string? note)
        => Run(() => brokerage.UpdateChecklistAsync(dealId, itemId, completed, completedOn, note, UserId),
            "Checklist updated.");

    [HttpPost("deals/{id:guid}/status")]
    public Task<IActionResult> ChangeDealStatus(Guid id, [FromQuery] DealStatus status)
        => Run(() => brokerage.ChangeDealStatusAsync(id, status, UserId), "Status updated.");

    /// <summary>
    /// Records a deal collapsing, with the cause and the fee lost. Painful to fill in and the only
    /// way anybody ever finds out that four in ten deals die at survey.
    /// </summary>
    [HttpPost("deals/fall-through")]
    public Task<IActionResult> RecordFallThrough([FromBody] FallThroughRecordDto request)
        => Run(() => brokerage.RecordFallThroughAsync(request, UserId), "Recorded.");

    [HttpPost("deals/{dealId:guid}/parties")]
    public Task<IActionResult> SaveDealParty(Guid dealId, [FromBody] DealPartyDto request)
        => Run(() => brokerage.SaveDealPartyAsync(dealId, request, UserId), "Party saved.");

    // ── Chains ───────────────────────────────────────────────────────────────

    [HttpPost("chains")]
    public Task<IActionResult> SaveChain([FromBody] SalesChainDto request)
        => Run(() => brokerage.SaveChainAsync(request, UserId), "Chain saved.");

    /// <summary>Every chain, or just the ones with a weak link somebody should be ringing about.</summary>
    [HttpGet("chains")]
    public Task<IActionResult> GetChains([FromQuery] bool atRiskOnly = false)
        => Run(() => brokerage.GetChainsAsync(atRiskOnly));

    [HttpPost("conveyancing")]
    public Task<IActionResult> SaveConveyancing([FromBody] ConveyancingDto request)
        => Run(() => brokerage.SaveConveyancingAsync(request, UserId), "Saved.");

    // ── Commission ───────────────────────────────────────────────────────────

    [HttpGet("commission-plans")]
    public Task<IActionResult> GetPlans([FromQuery] string? appliesTo, [FromQuery] Guid? projectId)
        => Run(() => brokerage.GetCommissionPlansAsync(appliesTo, projectId));

    [HttpPost("commission-plans")]
    public Task<IActionResult> SavePlan([FromBody] CommissionPlanDto request)
        => Run(() => brokerage.SaveCommissionPlanAsync(request, UserId), "Plan saved.");

    /// <summary>
    /// Works out the fee and every split. Pass <c>commit=false</c> to see the arithmetic without
    /// accruing anything — which is what a negotiator wants before agreeing a deal.
    /// </summary>
    [HttpPost("commission/calculate")]
    public Task<IActionResult> Calculate(
        [FromQuery] Guid? dealId,
        [FromQuery] Guid? bookingId,
        [FromQuery] Guid? tenancyId,
        [FromQuery] bool commit = false)
        => Run(() => brokerage.CalculateAsync(dealId, bookingId, tenancyId, commit, UserId));

    [HttpGet("commission/calculations")]
    public Task<IActionResult> GetCalculations(
        [FromQuery] ListQueryDto query, [FromQuery] CommissionStatus? status)
        => RunPaged(() => brokerage.GetCalculationsAsync(query, status));

    [HttpPost("commission/{calculationId:guid}/disburse")]
    public Task<IActionResult> CreateDisbursement(Guid calculationId)
        => Run(() => brokerage.CreateDisbursementAsync(calculationId, UserId), "Disbursement raised.");

    [HttpPost("commission/disbursements/{id:guid}/decide")]
    public Task<IActionResult> DecideDisbursement(
        Guid id, [FromQuery] ApprovalOutcome outcome, [FromQuery] string? comment)
        => Run(() => brokerage.DecideDisbursementAsync(id, outcome, comment, UserId), "Decision recorded.");

    [HttpPost("commission/payouts")]
    public Task<IActionResult> CreatePayout(
        [FromQuery] Guid? agentId,
        [FromQuery] Guid? partnerId,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to)
        => Run(() => brokerage.CreatePayoutAsync(agentId, partnerId, from, to, UserId), "Payout created.");

    [HttpGet("commission/payouts")]
    public Task<IActionResult> GetPayouts([FromQuery] ListQueryDto query)
        => RunPaged(() => brokerage.GetPayoutsAsync(query));

    /// <summary>Where a negotiator stands against their annual cap, and what happens after it.</summary>
    [HttpGet("commission/cap/{agentId:guid}")]
    public Task<IActionResult> GetCapPosition(Guid agentId, [FromQuery] int? year)
        => RunFound(() => brokerage.GetCapPositionAsync(agentId, year), "No cap ledger for that agent.");

    /// <summary>Reverses commission already paid on a booking that has since been cancelled.</summary>
    [HttpPost("commission/clawback/{bookingId:guid}")]
    public Task<IActionResult> ClawBack(Guid bookingId)
        => Run(() => brokerage.ClawBackAsync(bookingId, UserId), "Clawback raised.");

    // ── Channel partners ─────────────────────────────────────────────────────

    [HttpGet("partners")]
    public Task<IActionResult> GetPartners([FromQuery] ListQueryDto query, [FromQuery] PartnerStatus? status)
        => RunPaged(() => brokerage.GetPartnersAsync(query, status));

    [HttpGet("partners/{id:guid}")]
    public Task<IActionResult> GetPartner(Guid id)
        => RunFound(() => brokerage.GetPartnerAsync(id), "That partner does not exist.");

    [HttpPost("partners")]
    public Task<IActionResult> SavePartner([FromBody] ChannelPartnerUpsertDto request)
        => Run(() => brokerage.SavePartnerAsync(request, UserId), "Partner saved.");

    [HttpPost("partners/{id:guid}/status")]
    public Task<IActionResult> ChangePartnerStatus(
        Guid id, [FromQuery] PartnerStatus status, [FromQuery] string? reason)
        => Run(() => brokerage.ChangePartnerStatusAsync(id, status, reason, UserId), "Status updated.");

    [HttpGet("partners/tiers")]
    public Task<IActionResult> GetTiers() => Run(brokerage.GetTiersAsync);

    [HttpPost("partners/tiers")]
    public Task<IActionResult> SaveTier([FromBody] PartnerTierDto request)
        => Run(() => brokerage.SaveTierAsync(request, UserId), "Tier saved.");

    [HttpGet("partners/rates")]
    public Task<IActionResult> GetPartnerRates([FromQuery] Guid? partnerId, [FromQuery] Guid? projectId)
        => Run(() => brokerage.GetPartnerRatesAsync(partnerId, projectId));

    [HttpPost("partners/rates")]
    public Task<IActionResult> SavePartnerRate([FromBody] PartnerCommissionRateDto request)
        => Run(() => brokerage.SavePartnerRateAsync(request, UserId), "Rate saved.");

    // ── Lead registration ────────────────────────────────────────────────────

    /// <summary>
    /// Registers a lead to a partner for a fixed window. This is what stops two agencies claiming
    /// the same buyer, and it is the single most argued-about record in the whole channel.
    /// </summary>
    [HttpPost("partners/registrations")]
    public Task<IActionResult> RegisterLead([FromBody] LeadRegistrationCreateDto request)
        => Run(() => brokerage.RegisterLeadAsync(request, UserId), "Lead registered.");

    [HttpGet("partners/registrations")]
    public Task<IActionResult> GetRegistrations(
        [FromQuery] ListQueryDto query,
        [FromQuery] Guid? partnerId,
        [FromQuery] LeadRegistrationStatus? status)
        => RunPaged(() => brokerage.GetRegistrationsAsync(query, partnerId, status));

    [HttpPost("partners/registrations/{id:guid}/extend")]
    public Task<IActionResult> ExtendRegistration(Guid id, [FromQuery] int days)
        => Run(() => brokerage.ExtendRegistrationAsync(id, days, UserId), "Registration extended.");

    [HttpPost("partners/registrations/expire")]
    public Task<IActionResult> ExpireRegistrations()
        => Run(brokerage.ExpireRegistrationsAsync, "Expired registrations closed.");

    // ── Partner money ────────────────────────────────────────────────────────

    [HttpGet("partners/commission")]
    public Task<IActionResult> GetPartnerCommission(
        [FromQuery] ListQueryDto query, [FromQuery] Guid? partnerId, [FromQuery] CommissionStatus? status)
        => RunPaged(() => brokerage.GetPartnerCommissionAsync(query, partnerId, status));

    [HttpPost("partners/{id:guid}/statement")]
    public Task<IActionResult> GenerateStatement(Guid id, [FromQuery] DateOnly from, [FromQuery] DateOnly to)
        => Run(() => brokerage.GeneratePartnerStatementAsync(id, from, to, UserId), "Statement generated.");

    [HttpPost("partners/advances")]
    public Task<IActionResult> CreateAdvance([FromBody] PartnerAdvanceDto request)
        => Run(() => brokerage.CreateAdvanceAsync(request, UserId), "Advance recorded.");

    [HttpGet("partners/contests")]
    public Task<IActionResult> GetContests([FromQuery] bool activeOnly = true)
        => Run(() => brokerage.GetContestsAsync(activeOnly));

    [HttpPost("partners/contests")]
    public Task<IActionResult> SaveContest([FromBody] PartnerContestDto request)
        => Run(() => brokerage.SaveContestAsync(request, UserId), "Contest saved.");

    /// <summary>What a partner sees when they log in: their leads, bookings and what they are owed.</summary>
    [HttpGet("partners/{id:guid}/portal")]
    public Task<IActionResult> GetPartnerPortal(Guid id)
        => Run(() => brokerage.GetPartnerPortalHomeAsync(id));
}
