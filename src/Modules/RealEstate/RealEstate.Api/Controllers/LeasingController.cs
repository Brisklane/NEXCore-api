using Microsoft.AspNetCore.Mvc;
using RealEstate.Application.DTOs;
using RealEstate.Application.Services.Interfaces;
using RealEstate.Domain.Enums;

namespace RealEstate.Api.Controllers;

/// <summary>
/// Tenancies, deposits, inspections, renewals, the rent roll, arrears, service charge, turnover
/// rent, landlords and client money.
///
/// Client money is the part that closes agencies down. It is reconciled three ways — the bank, the
/// ledger control account, and the sum of what every client is owed — and the sign-off refuses
/// both an unbalanced reconciliation and one signed by the person who prepared it. Those two rules
/// are not conveniences; they are the whole reason a regulator lets an agency hold the money.
/// </summary>
[Route("api/realestate/leasing")]
public class LeasingController(
    ILeasingService leasing,
    ILogger<LeasingController> logger) : RealEstateControllerBase(logger)
{
    // ── Tenancies ────────────────────────────────────────────────────────────

    [HttpPost("tenancies/search")]
    public Task<IActionResult> SearchTenancies([FromBody] TenancySearchDto query)
        => RunPaged(() => leasing.GetTenanciesAsync(query));

    [HttpGet("tenancies/{id:guid}")]
    public Task<IActionResult> GetTenancy(Guid id)
        => RunFound(() => leasing.GetTenancyAsync(id), "That tenancy does not exist.");

    [HttpPost("tenancies")]
    public Task<IActionResult> CreateTenancy([FromBody] TenancyCreateDto request)
        => Run(() => leasing.CreateTenancyAsync(request, UserId), "Tenancy created.");

    [HttpPut("tenancies/{id:guid}")]
    public Task<IActionResult> UpdateTenancy(Guid id, [FromBody] TenancyCreateDto request)
        => Run(() => leasing.UpdateTenancyAsync(id, request, UserId), "Tenancy saved.");

    [HttpPost("tenancies/{id:guid}/status")]
    public Task<IActionResult> ChangeStatus(Guid id, [FromQuery] TenancyStatus status)
        => Run(() => leasing.ChangeStatusAsync(id, status, UserId), "Status updated.");

    /// <summary>Rebuilds the rent schedule from the tenancy's terms. Charges already raised stand.</summary>
    [HttpPost("tenancies/{id:guid}/schedule")]
    public Task<IActionResult> RegenerateSchedule(Guid id)
        => Run(() => leasing.RegenerateScheduleAsync(id, UserId), "Schedule regenerated.");

    // ── Referencing ──────────────────────────────────────────────────────────

    [HttpPost("referencing")]
    public Task<IActionResult> SaveReferencing([FromBody] ReferencingCaseDto request)
        => Run(() => leasing.SaveReferencingAsync(request, UserId), "Referencing saved.");

    [HttpPost("referencing/{id:guid}/decide")]
    public Task<IActionResult> DecideReferencing(
        Guid id,
        [FromQuery] ReferencingOutcome outcome,
        [FromQuery] string? conditions,
        [FromQuery] string? failureReason)
        => Run(() => leasing.DecideReferencingAsync(id, outcome, conditions, failureReason, UserId),
            "Decision recorded.");

    // ── Deposits ─────────────────────────────────────────────────────────────

    [HttpPost("deposits")]
    public Task<IActionResult> SaveDeposit([FromBody] SecurityDepositDto request)
        => Run(() => leasing.SaveDepositAsync(request, UserId), "Deposit saved.");

    /// <summary>
    /// Records protection in a statutory scheme. There is usually a hard deadline from the day the
    /// money was taken, and missing it costs a multiple of the deposit.
    /// </summary>
    [HttpPost("deposits/{id:guid}/register")]
    public Task<IActionResult> RegisterDeposit(
        Guid id,
        [FromQuery] string schemeName,
        [FromQuery] string reference,
        [FromQuery] DateOnly registeredOn)
        => Run(() => leasing.RegisterDepositAsync(id, schemeName, reference, registeredOn, UserId),
            "Deposit registered.");

    [HttpPost("deposits/{id:guid}/deductions")]
    public Task<IActionResult> ProposeDeductions(Guid id, [FromBody] List<DepositDeductionDto> deductions)
        => Run(() => leasing.ProposeDeductionsAsync(id, deductions, UserId), "Deductions proposed.");

    [HttpPost("deposits/{id:guid}/release")]
    public Task<IActionResult> ReleaseDeposit(
        Guid id, [FromQuery] decimal toTenant, [FromQuery] decimal toLandlord)
        => Run(() => leasing.ReleaseDepositAsync(id, toTenant, toLandlord, UserId), "Deposit released.");

    [HttpGet("deposits")]
    public Task<IActionResult> GetDeposits([FromQuery] ListQueryDto query, [FromQuery] bool unregisteredOnly = false)
        => RunPaged(() => leasing.GetDepositsAsync(query, unregisteredOnly));

    // ── Inspections ──────────────────────────────────────────────────────────

    [HttpPost("inspections")]
    public Task<IActionResult> SaveInspection([FromBody] MoveInspectionDto request)
        => Run(() => leasing.SaveInspectionAsync(request, UserId), "Inspection saved.");

    [HttpGet("inspections/{id:guid}")]
    public Task<IActionResult> GetInspection(Guid id)
        => RunFound(() => leasing.GetInspectionAsync(id), "That inspection does not exist.");

    [HttpGet("inspections")]
    public Task<IActionResult> GetInspections([FromQuery] ListQueryDto query, [FromQuery] InspectionKind? kind)
        => RunPaged(() => leasing.GetInspectionsAsync(query, kind));

    // ── Notices and renewals ─────────────────────────────────────────────────

    [HttpPost("notices")]
    public Task<IActionResult> ServeNotice([FromBody] TenancyNoticeDto request)
        => Run(() => leasing.ServeNoticeAsync(request, UserId), "Notice served.");

    [HttpPost("renewals")]
    public Task<IActionResult> OfferRenewal([FromBody] TenancyRenewalDto request)
        => Run(() => leasing.OfferRenewalAsync(request, UserId), "Renewal offered.");

    [HttpPost("renewals/{id:guid}/decide")]
    public Task<IActionResult> DecideRenewal(
        Guid id,
        [FromQuery] string status,
        [FromQuery] decimal? agreedRent,
        [FromQuery] Guid? declineReasonCodeId)
        => Run(() => leasing.DecideRenewalAsync(id, status, agreedRent, declineReasonCodeId, UserId),
            "Decision recorded.");

    [HttpGet("renewals")]
    public Task<IActionResult> GetRenewalPipeline([FromQuery] ListQueryDto query, [FromQuery] int withinDays = 120)
        => RunPaged(() => leasing.GetRenewalPipelineAsync(query, withinDays));

    [HttpPost("rent-reviews")]
    public Task<IActionResult> SaveRentReview([FromBody] RentReviewDto request)
        => Run(() => leasing.SaveRentReviewAsync(request, UserId), "Review saved.");

    /// <summary>
    /// Lease dates that cannot be missed: break notices, option windows, review triggers. Missing
    /// one of these does not produce a warning, it produces a lease that runs another five years.
    /// </summary>
    [HttpGet("critical-dates")]
    public Task<IActionResult> GetCriticalDates([FromQuery] Guid? propertyId, [FromQuery] int withinDays = 180)
        => Run(() => leasing.GetCriticalDatesAsync(withinDays, propertyId));

    [HttpPost("critical-dates/{id:guid}/action")]
    public Task<IActionResult> ActionCriticalDate(Guid id, [FromQuery] string? note)
        => Run(() => leasing.ActionCriticalDateAsync(id, note, UserId), "Recorded.");

    // ── Compliance ───────────────────────────────────────────────────────────

    [HttpGet("certificates")]
    public Task<IActionResult> GetCertificates([FromQuery] ListQueryDto query, [FromQuery] bool expiringOnly = false)
        => RunPaged(() => leasing.GetCertificatesAsync(query, expiringOnly));

    [HttpPost("certificates")]
    public Task<IActionResult> SaveCertificate([FromBody] ComplianceCertificateDto request)
        => Run(() => leasing.SaveCertificateAsync(request, UserId), "Certificate saved.");

    // ── Rent ─────────────────────────────────────────────────────────────────

    [HttpGet("rent-roll")]
    public Task<IActionResult> GetRentRoll(
        [FromQuery] Guid? propertyId, [FromQuery] Guid? projectId, [FromQuery] DateOnly? asOf)
        => Run(() => leasing.GetRentRollAsync(propertyId, projectId,
            asOf ?? DateOnly.FromDateTime(DateTime.UtcNow)));

    [HttpPost("rent-runs")]
    public Task<IActionResult> RunRent([FromBody] RentRunRequestDto request)
        => Run(() => leasing.RunRentAsync(request, UserId), "Rent run complete.");

    [HttpGet("rent-runs")]
    public Task<IActionResult> GetRentRuns([FromQuery] ListQueryDto query)
        => RunPaged(() => leasing.GetRentRunsAsync(query));

    [HttpGet("arrears")]
    public Task<IActionResult> GetArrears([FromQuery] ListQueryDto query, [FromQuery] int? minDays)
        => RunPaged(() => leasing.GetArrearsAsync(query, minDays));

    [HttpGet("voids")]
    public Task<IActionResult> GetVoids([FromQuery] Guid? propertyId, [FromQuery] bool openOnly = true)
        => Run(() => leasing.GetVoidsAsync(propertyId, openOnly));

    /// <summary>The trade mix in a centre, which is what a retail asset manager actually manages.</summary>
    [HttpGet("tenant-mix/{propertyId:guid}")]
    public Task<IActionResult> GetTenantMix(Guid propertyId)
        => Run(() => leasing.GetTenantMixAsync(propertyId));

    // ── Service charge ───────────────────────────────────────────────────────

    [HttpGet("service-charge/budgets")]
    public Task<IActionResult> GetBudgets([FromQuery] ListQueryDto query, [FromQuery] Guid? propertyId)
        => RunPaged(() => leasing.GetBudgetsAsync(query, propertyId));

    [HttpGet("service-charge/budgets/{id:guid}")]
    public Task<IActionResult> GetBudget(Guid id)
        => RunFound(() => leasing.GetBudgetAsync(id), "That budget does not exist.");

    [HttpPost("service-charge/budgets")]
    public Task<IActionResult> SaveBudget([FromBody] ServiceChargeBudgetDto request)
        => Run(() => leasing.SaveBudgetAsync(request, UserId), "Budget saved.");

    [HttpPost("service-charge/budgets/{id:guid}/approve")]
    public Task<IActionResult> ApproveBudget(Guid id)
        => Run(() => leasing.ApproveBudgetAsync(id, UserId), "Budget approved.");

    [HttpPost("service-charge/budgets/{id:guid}/on-account")]
    public Task<IActionResult> RaiseOnAccount(
        Guid id,
        [FromQuery] DateOnly periodFrom,
        [FromQuery] DateOnly periodTo,
        [FromQuery] bool dryRun = true)
        => Run(() => leasing.RaiseOnAccountAsync(id, periodFrom, periodTo, dryRun, UserId));

    /// <summary>
    /// The year-end reconciliation, applying exclusions, then gross-up, then caps — in that order,
    /// because that is the order the lease reads and any other order gives a different answer.
    /// </summary>
    [HttpPost("service-charge/budgets/{id:guid}/reconcile")]
    public Task<IActionResult> Reconcile(Guid id, [FromQuery] bool dryRun = true)
        => Run(() => leasing.ReconcileAsync(id, dryRun, UserId));

    [HttpPost("service-charge/reconciliations/{id:guid}/finalise")]
    public Task<IActionResult> FinaliseReconciliation(Guid id)
        => Run(() => leasing.FinaliseReconciliationAsync(id, UserId), "Reconciliation finalised.");

    [HttpGet("service-charge/apportionments/{propertyId:guid}")]
    public Task<IActionResult> GetApportionments(Guid propertyId)
        => Run(() => leasing.GetApportionmentsAsync(propertyId));

    [HttpPost("service-charge/apportionments")]
    public Task<IActionResult> SaveApportionment([FromBody] ApportionmentScheduleDto request)
        => Run(() => leasing.SaveApportionmentAsync(request, UserId), "Apportionment saved.");

    // ── Turnover rent ────────────────────────────────────────────────────────

    [HttpPost("turnover/terms")]
    public Task<IActionResult> SaveTurnoverTerm([FromBody] TurnoverRentTermDto request)
        => Run(() => leasing.SaveTurnoverTermAsync(request, UserId), "Term saved.");

    [HttpPost("turnover/declarations")]
    public Task<IActionResult> SaveSalesDeclaration([FromBody] TenantSalesDeclarationDto request)
        => Run(() => leasing.SaveSalesDeclarationAsync(request, UserId), "Declaration saved.");

    [HttpGet("turnover/declarations")]
    public Task<IActionResult> GetSalesDeclarations(
        [FromQuery] ListQueryDto query, [FromQuery] Guid? propertyId, [FromQuery] bool overdueOnly = false)
        => RunPaged(() => leasing.GetSalesDeclarationsAsync(query, propertyId, overdueOnly));

    [HttpPost("turnover/overage")]
    public Task<IActionResult> CalculateOverage(
        [FromQuery] Guid? propertyId,
        [FromQuery] DateOnly periodFrom,
        [FromQuery] DateOnly periodTo,
        [FromQuery] bool dryRun = true)
        => Run(() => leasing.CalculateOverageAsync(propertyId, periodFrom, periodTo, dryRun, UserId));

    // ── Landlords ────────────────────────────────────────────────────────────

    [HttpGet("landlords")]
    public Task<IActionResult> GetLandlords([FromQuery] ListQueryDto query)
        => RunPaged(() => leasing.GetLandlordsAsync(query));

    [HttpGet("landlords/{id:guid}")]
    public Task<IActionResult> GetLandlord(Guid id)
        => RunFound(() => leasing.GetLandlordAsync(id), "That landlord does not exist.");

    [HttpPost("landlords")]
    public Task<IActionResult> SaveLandlord([FromBody] LandlordDetailDto request)
        => Run(() => leasing.SaveLandlordAsync(request, UserId), "Landlord saved.");

    [HttpPost("landlords/{id:guid}/agreement")]
    public Task<IActionResult> SaveAgreement(Guid id, [FromBody] ManagementAgreementDto request)
        => Run(() => leasing.SaveManagementAgreementAsync(id, request, UserId), "Agreement saved.");

    [HttpPost("landlords/{id:guid}/statement")]
    public Task<IActionResult> GenerateStatement(
        Guid id, [FromQuery] DateOnly from, [FromQuery] DateOnly to, [FromQuery] Guid? propertyId)
        => Run(() => leasing.GenerateOwnerStatementAsync(id, from, to, propertyId, UserId), "Statement generated.");

    [HttpGet("landlords/statements")]
    public Task<IActionResult> GetStatements([FromQuery] ListQueryDto query, [FromQuery] Guid? landlordId)
        => RunPaged(() => leasing.GetOwnerStatementsAsync(query, landlordId));

    [HttpPost("landlords/payouts")]
    public Task<IActionResult> CreatePayoutRun(
        [FromQuery] DateOnly payoutDate,
        [FromQuery] Guid? officeId,
        [FromQuery] Guid clientAccountId,
        [FromQuery] bool dryRun = true)
        => Run(() => leasing.CreatePayoutRunAsync(payoutDate, officeId, clientAccountId, dryRun, UserId));

    [HttpPost("landlords/payouts/{id:guid}/submit")]
    public Task<IActionResult> SubmitPayoutRun(Guid id)
        => Run(() => leasing.SubmitPayoutRunAsync(id, UserId), "Payout submitted.");

    [HttpGet("landlords/payouts")]
    public Task<IActionResult> GetPayoutRuns([FromQuery] ListQueryDto query)
        => RunPaged(() => leasing.GetPayoutRunsAsync(query));

    // ── Client money ─────────────────────────────────────────────────────────

    [HttpGet("client-accounts")]
    public Task<IActionResult> GetClientAccounts() => Run(leasing.GetClientAccountsAsync);

    [HttpPost("client-accounts")]
    public Task<IActionResult> SaveClientAccount([FromBody] ClientAccountDto request)
        => Run(() => leasing.SaveClientAccountAsync(request, UserId), "Account saved.");

    [HttpGet("client-accounts/{id:guid}/ledger")]
    public Task<IActionResult> GetClientLedger(Guid id, [FromQuery] ListQueryDto query)
        => RunPaged(() => leasing.GetClientLedgerAsync(id, query));

    /// <summary>Bank, ledger control and the sum of client balances, reconciled three ways.</summary>
    [HttpPost("client-accounts/{id:guid}/reconcile")]
    public Task<IActionResult> ReconcileClientMoney(
        Guid id, [FromQuery] DateOnly asOf, [FromQuery] decimal bankBalance)
        => Run(() => leasing.ReconcileClientMoneyAsync(id, asOf, bankBalance, UserId));

    [HttpPost("client-money/reconciliations/{id:guid}/sign-off")]
    public Task<IActionResult> SignOff(Guid id)
        => Run(() => leasing.SignOffReconciliationAsync(id, UserId), "Reconciliation signed off.");

    [HttpGet("client-money/exceptions")]
    public Task<IActionResult> GetExceptions([FromQuery] ListQueryDto query, [FromQuery] bool openOnly = true)
        => RunPaged(() => leasing.GetClientMoneyExceptionsAsync(query, openOnly));

    // ── Portals ──────────────────────────────────────────────────────────────

    [HttpGet("portal/owner/{landlordId:guid}")]
    public Task<IActionResult> GetOwnerPortal(Guid landlordId)
        => Run(() => leasing.GetOwnerPortalHomeAsync(landlordId));

    [HttpGet("portal/tenant/{partyId:guid}")]
    public Task<IActionResult> GetTenantPortal(Guid partyId)
        => Run(() => leasing.GetTenantPortalHomeAsync(partyId));
}
