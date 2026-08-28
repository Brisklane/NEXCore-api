using Microsoft.AspNetCore.Mvc;
using RealEstate.Application.DTOs;
using RealEstate.Application.Services.Interfaces;
using RealEstate.Domain.Enums;

namespace RealEstate.Api.Controllers;

/// <summary>
/// Joint ventures, investors, escrow, loans and guarantees, customer mortgages, revenue
/// recognition, cost allocation, tax, statutory compliance, documents, signatures, the record room
/// and litigation.
///
/// Escrow is the control everybody eventually tries to go round. What may be withdrawn is a
/// function of certified physical progress, never of what the bank balance happens to be — so an
/// over-draw is refused outright with the arithmetic shown, the entitlement is re-checked at the
/// moment of release as well as at request, and every withdrawal carries the certificates that
/// justified it. In two years an auditor will ask exactly that question about exactly that
/// transfer, and the answer has to already be on file.
/// </summary>
[Route("api/realestate/finance")]
public class FinanceController(
    IFinanceService finance,
    ILogger<FinanceController> logger) : RealEstateControllerBase(logger)
{
    // ── Joint ventures ───────────────────────────────────────────────────────

    [HttpGet("ventures")]
    public Task<IActionResult> GetVentures([FromQuery] Guid? projectId)
        => Run(() => finance.GetVenturesAsync(projectId));

    [HttpGet("ventures/{id:guid}")]
    public Task<IActionResult> GetVenture(Guid id)
        => RunFound(() => finance.GetVentureAsync(id), "That joint venture does not exist.");

    [HttpPost("ventures")]
    public Task<IActionResult> SaveVenture([FromBody] JointVentureDto request)
        => Run(() => finance.SaveVentureAsync(request, UserId), "Joint venture saved.");

    /// <summary>Takes a unit out of saleable inventory and gives it to the landowner in kind.</summary>
    [HttpPost("ventures/{id:guid}/allocate")]
    public Task<IActionResult> AllocateUnit(
        Guid id, [FromQuery] Guid unitId, [FromQuery] Guid? jvPartnerId)
        => Run(() => finance.AllocateUnitAsync(id, unitId, jvPartnerId, UserId), "Unit allocated.");

    [HttpPost("ventures/allocations/{id:guid}/release")]
    public Task<IActionResult> ReleaseAllocation(Guid id)
        => Run(() => finance.ReleaseAllocationAsync(id, UserId), "Allocation released.");

    [HttpGet("ventures/{id:guid}/ledger")]
    public Task<IActionResult> GetLandownerLedger(Guid id, [FromQuery] ListQueryDto query)
        => RunPaged(() => finance.GetLandownerLedgerAsync(id, query));

    /// <summary>
    /// Accrues the landowner's share of everything collected since the last accrual. Incremental,
    /// so running it twice in a day does nothing the second time.
    /// </summary>
    [HttpPost("ventures/{id:guid}/accrue")]
    public Task<IActionResult> AccrueLandownerShare(Guid id)
        => Run(() => finance.AccrueLandownerShareAsync(id, UserId), "Share accrued.");

    // ── Investors ────────────────────────────────────────────────────────────

    [HttpGet("investors")]
    public Task<IActionResult> GetInvestors([FromQuery] ListQueryDto query, [FromQuery] Guid? projectId)
        => RunPaged(() => finance.GetInvestorsAsync(query, projectId));

    [HttpGet("investors/{id:guid}")]
    public Task<IActionResult> GetInvestor(Guid id)
        => RunFound(() => finance.GetInvestorAsync(id), "That investor does not exist.");

    [HttpPost("investors")]
    public Task<IActionResult> SaveInvestor([FromBody] InvestorDto request)
        => Run(() => finance.SaveInvestorAsync(request, UserId), "Investor saved.");

    [HttpPost("capital-calls")]
    public Task<IActionResult> IssueCapitalCall([FromBody] CapitalCallDto request)
        => Run(() => finance.IssueCapitalCallAsync(request, UserId), "Capital call issued.");

    [HttpPost("contributions")]
    public Task<IActionResult> RecordContribution([FromBody] ContributionDto request)
        => Run(() => finance.RecordContributionAsync(request, UserId), "Contribution recorded.");

    [HttpPost("distributions")]
    public Task<IActionResult> RecordDistribution([FromBody] DistributionDto request)
        => Run(() => finance.RecordDistributionAsync(request, UserId), "Distribution recorded.");

    // ── Escrow ───────────────────────────────────────────────────────────────

    [HttpGet("projects/{projectId:guid}/accounts")]
    public Task<IActionResult> GetProjectAccounts(Guid projectId)
        => Run(() => finance.GetProjectAccountsAsync(projectId));

    [HttpPost("accounts")]
    public Task<IActionResult> SaveProjectAccount([FromBody] ProjectBankAccountDto request)
        => Run(() => finance.SaveProjectAccountAsync(request, UserId), "Account saved.");

    [HttpGet("accounts/{id:guid}/ledger")]
    public Task<IActionResult> GetEscrowLedger(Guid id, [FromQuery] ListQueryDto query)
        => RunPaged(() => finance.GetEscrowLedgerAsync(id, query));

    /// <summary>
    /// Requests a withdrawal. Pass <c>dryRun</c> on the body to see the entitlement arithmetic and
    /// the missing certificates without leaving a request behind.
    /// </summary>
    [HttpPost("escrow/withdrawals")]
    public Task<IActionResult> RequestWithdrawal([FromBody] EscrowWithdrawalRequestDto request)
        => Run(() => finance.RequestWithdrawalAsync(request, UserId));

    [HttpPost("escrow/withdrawals/{id:guid}/decide")]
    public Task<IActionResult> DecideWithdrawal(
        Guid id,
        [FromQuery] ApprovalOutcome outcome,
        [FromQuery] decimal? approvedAmount,
        [FromQuery] string? comment)
        => Run(() => finance.DecideWithdrawalAsync(id, outcome, approvedAmount, comment, UserId),
            "Decision recorded.");

    [HttpGet("escrow/withdrawals")]
    public Task<IActionResult> GetWithdrawals([FromQuery] ListQueryDto query, [FromQuery] Guid? projectId)
        => RunPaged(() => finance.GetWithdrawalsAsync(query, projectId));

    [HttpPost("accounts/{id:guid}/recompute-entitlement")]
    public Task<IActionResult> RecomputeEntitlement(Guid id)
        => Run(() => finance.RecomputeEntitlementAsync(id, UserId), "Entitlement recomputed.");

    // ── Loans and guarantees ─────────────────────────────────────────────────

    [HttpGet("loans")]
    public Task<IActionResult> GetLoans([FromQuery] ListQueryDto query, [FromQuery] Guid? projectId)
        => RunPaged(() => finance.GetLoansAsync(query, projectId));

    [HttpPost("loans")]
    public Task<IActionResult> SaveLoan([FromBody] ProjectLoanDto request)
        => Run(() => finance.SaveLoanAsync(request, UserId), "Loan saved.");

    [HttpPost("loans/{id:guid}/drawdowns")]
    public Task<IActionResult> RecordDrawdown(Guid id, [FromBody] LoanDrawdownDto request)
        => Run(() => finance.RecordDrawdownAsync(id, request, UserId), "Drawdown recorded.");

    [HttpGet("guarantees")]
    public Task<IActionResult> GetGuarantees([FromQuery] Guid? projectId, [FromQuery] bool expiringOnly = false)
        => Run(() => finance.GetGuaranteesAsync(projectId, expiringOnly));

    [HttpPost("guarantees")]
    public Task<IActionResult> SaveGuarantee([FromBody] BankGuaranteeDto request)
        => Run(() => finance.SaveGuaranteeAsync(request, UserId), "Guarantee saved.");

    // ── Customer mortgages ───────────────────────────────────────────────────

    [HttpGet("mortgages")]
    public Task<IActionResult> GetMortgages([FromQuery] ListQueryDto query, [FromQuery] string? status)
        => RunPaged(() => finance.GetMortgagesAsync(query, status));

    [HttpPost("mortgages")]
    public Task<IActionResult> SaveMortgage([FromBody] CustomerMortgageDto request)
        => Run(() => finance.SaveMortgageAsync(request, UserId), "Mortgage saved.");

    [HttpPost("mortgages/{id:guid}/disbursements")]
    public Task<IActionResult> RecordDisbursement(Guid id, [FromBody] MortgageDisbursementDto request)
        => Run(() => finance.RecordDisbursementAsync(id, request, UserId), "Disbursement recorded.");

    // ── Revenue recognition ──────────────────────────────────────────────────

    [HttpPost("recognition/policies")]
    public Task<IActionResult> SaveRecognitionPolicy([FromBody] RecognitionPolicyDto request)
        => Run(() => finance.SaveRecognitionPolicyAsync(request, UserId), "Policy saved.");

    /// <summary>
    /// Recognises revenue for a period. Runs as a dry run by default — a period close nobody can
    /// review before committing is a period close that gets committed wrong.
    /// </summary>
    [HttpPost("recognition/run")]
    public Task<IActionResult> RunRecognition(
        [FromQuery] Guid? projectId,
        [FromQuery] DateOnly periodFrom,
        [FromQuery] DateOnly periodTo,
        [FromQuery] bool dryRun = true)
        => Run(() => finance.RunRecognitionAsync(projectId, periodFrom, periodTo, dryRun, UserId));

    [HttpGet("recognition/runs")]
    public Task<IActionResult> GetRecognitionRuns([FromQuery] ListQueryDto query)
        => RunPaged(() => finance.GetRecognitionRunsAsync(query));

    [HttpGet("projects/{projectId:guid}/wip")]
    public Task<IActionResult> GetWip(Guid projectId, [FromQuery] ListQueryDto query)
        => RunPaged(() => finance.GetWipAsync(projectId, query));

    [HttpGet("projects/{projectId:guid}/profitability")]
    public Task<IActionResult> GetUnitProfitability(Guid projectId, [FromQuery] ListQueryDto query)
        => Run(() => finance.GetUnitProfitabilityAsync(projectId, query));

    /// <summary>
    /// Spreads incurred cost across the saleable units on whichever basis each cost category is
    /// configured for, and records which basis it used.
    /// </summary>
    [HttpPost("projects/{projectId:guid}/allocate-costs")]
    public Task<IActionResult> AllocateCosts(Guid projectId, [FromQuery] DateOnly? asOf)
        => Run(() => finance.AllocateCostsAsync(projectId, asOf ?? DateOnly.FromDateTime(DateTime.UtcNow), UserId),
            "Costs allocated.");

    [HttpGet("projects/{projectId:guid}/pnl")]
    public Task<IActionResult> GetProjectPnl(Guid projectId, [FromQuery] DateOnly? asOf)
        => Run(() => finance.GetProjectPnlAsync(projectId, asOf ?? DateOnly.FromDateTime(DateTime.UtcNow)));

    // ── Tax ──────────────────────────────────────────────────────────────────

    [HttpGet("tax-profiles")]
    public Task<IActionResult> GetTaxProfiles([FromQuery] Guid? projectId)
        => Run(() => finance.GetTaxProfilesAsync(projectId));

    [HttpPost("tax-profiles")]
    public Task<IActionResult> SaveTaxProfile([FromBody] TaxProfileDto request)
        => Run(() => finance.SaveTaxProfileAsync(request, UserId), "Profile saved.");

    [HttpGet("withholding")]
    public Task<IActionResult> GetWithholding(
        [FromQuery] ListQueryDto query,
        [FromQuery] WithholdingKind? kind,
        [FromQuery] bool? undepositedOnly)
        => RunPaged(() => finance.GetWithholdingAsync(query, kind, undepositedOnly));

    // ── Statutory compliance ─────────────────────────────────────────────────

    [HttpGet("approvals")]
    public Task<IActionResult> GetApprovalRecords(
        [FromQuery] ListQueryDto query, [FromQuery] Guid? projectId, [FromQuery] ApprovalState? state)
        => RunPaged(() => finance.GetApprovalRecordsAsync(query, projectId, state));

    [HttpPost("approvals")]
    public Task<IActionResult> SaveApprovalRecord([FromBody] ApprovalRecordDto request)
        => Run(() => finance.SaveApprovalRecordAsync(request, UserId), "Approval saved.");

    [HttpGet("licences")]
    public Task<IActionResult> GetLicences([FromQuery] bool expiringOnly = false)
        => Run(() => finance.GetLicencesAsync(expiringOnly));

    [HttpPost("licences")]
    public Task<IActionResult> SaveLicence([FromBody] LicenceRecordDto request)
        => Run(() => finance.SaveLicenceAsync(request, UserId), "Licence saved.");

    /// <summary>
    /// Every dated obligation on one calendar with one owner. Anything already overdue is included
    /// whatever window was asked for — a missed deadline does not stop mattering.
    /// </summary>
    [HttpGet("compliance-calendar")]
    public Task<IActionResult> GetCalendar(
        [FromQuery] DateOnly from, [FromQuery] DateOnly to, [FromQuery] string? category)
        => Run(() => finance.GetComplianceCalendarAsync(from, to, category));

    [HttpPost("compliance-calendar/{id:guid}/complete")]
    public Task<IActionResult> CompleteCalendarEntry(Guid id, [FromQuery] string? evidenceUrl)
        => Run(() => finance.CompleteCalendarEntryAsync(id, evidenceUrl, UserId), "Marked complete.");

    [HttpGet("filings")]
    public Task<IActionResult> GetFilings([FromQuery] ListQueryDto query, [FromQuery] string? status)
        => RunPaged(() => finance.GetFilingsAsync(query, status));

    /// <summary>
    /// Builds the quarterly return from the ledgers rather than from a spreadsheet, so the sales,
    /// collections, escrow movement and physical progress all tie to the same records.
    /// </summary>
    [HttpPost("qpr")]
    public Task<IActionResult> GenerateQpr(
        [FromQuery] Guid projectId, [FromQuery] int year, [FromQuery] int quarter)
        => Run(() => finance.GenerateQprAsync(projectId, year, quarter, UserId), "Report generated.");

    [HttpPost("qpr/{id:guid}/file")]
    public Task<IActionResult> FileQpr(Guid id, [FromQuery] string acknowledgementNumber)
        => Run(() => finance.FileQprAsync(id, acknowledgementNumber, UserId), "Report filed.");

    [HttpGet("qpr/{id:guid}")]
    public Task<IActionResult> GetQpr(Guid id)
        => RunFound(() => finance.GetQprAsync(id), "That report does not exist.");

    // ── Documents ────────────────────────────────────────────────────────────

    [HttpGet("templates")]
    public Task<IActionResult> GetTemplates([FromQuery] string? documentType, [FromQuery] Guid? projectId)
        => Run(() => finance.GetTemplatesAsync(documentType, projectId));

    [HttpPost("templates")]
    public Task<IActionResult> SaveTemplate([FromBody] DocumentTemplateDto request)
        => Run(() => finance.SaveTemplateAsync(request, UserId), "Template saved.");

    /// <summary>
    /// Publishes a new version and closes the previous one the day before. Every document ever
    /// generated can then be traced to exactly one version of exactly one template.
    /// </summary>
    [HttpPost("templates/{id:guid}/versions")]
    public Task<IActionResult> PublishVersion(Guid id, [FromBody] TemplateVersionDto request)
        => Run(() => finance.PublishTemplateVersionAsync(id, request, UserId), "Version published.");

    [HttpGet("clauses")]
    public Task<IActionResult> GetClauses([FromQuery] string? category, [FromQuery] Guid? projectId)
        => Run(() => finance.GetClausesAsync(category, projectId));

    [HttpPost("clauses")]
    public Task<IActionResult> SaveClause([FromBody] ClauseLibraryItemDto request)
        => Run(() => finance.SaveClauseAsync(request, UserId), "Clause saved.");

    [HttpPost("documents/generate")]
    public Task<IActionResult> GenerateDocument([FromBody] DocumentGenerationRequestDto request)
        => Run(() => finance.GenerateDocumentAsync(request, UserId), "Document generated.");

    [HttpGet("documents")]
    public Task<IActionResult> GetDocuments(
        [FromQuery] ListQueryDto query, [FromQuery] string? documentType, [FromQuery] Guid? entityId)
        => RunPaged(() => finance.GetDocumentsAsync(query, documentType, entityId));

    /// <summary>Checks a printed document against what was actually issued from this office.</summary>
    [HttpGet("documents/verify/{code}")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public Task<IActionResult> VerifyDocument(string code)
        => RunFound(() => finance.VerifyDocumentAsync(code), "No document matches that code.");

    // ── Signatures ───────────────────────────────────────────────────────────

    [HttpPost("documents/{id:guid}/sign")]
    public Task<IActionResult> StartSigning(
        Guid id,
        [FromQuery] SignatureMethod method,
        [FromQuery] bool sequential,
        [FromBody] List<SignaturePartyDto> parties)
        => Run(() => finance.StartSigningAsync(id, parties, method, sequential, UserId), "Signing started.");

    [HttpPost("signing/{sessionId:guid}/record")]
    public Task<IActionResult> RecordSignature(
        Guid sessionId,
        [FromQuery] Guid partyId,
        [FromQuery] string? signatureUrl,
        [FromQuery] string? thumbUrl,
        [FromQuery] string? photoUrl)
        => Run(() => finance.RecordSignatureAsync(sessionId, partyId, signatureUrl, thumbUrl, photoUrl, UserId),
            "Signature recorded.");

    [HttpGet("signing/{id:guid}")]
    public Task<IActionResult> GetSigning(Guid id)
        => RunFound(() => finance.GetSigningAsync(id), "That signing session does not exist.");

    // ── The record room ──────────────────────────────────────────────────────

    [HttpGet("files")]
    public Task<IActionResult> GetPhysicalFiles([FromQuery] ListQueryDto query, [FromQuery] PhysicalFileState? state)
        => RunPaged(() => finance.GetPhysicalFilesAsync(query, state));

    [HttpPost("files")]
    public Task<IActionResult> SavePhysicalFile([FromBody] PhysicalFileDto request)
        => Run(() => finance.SavePhysicalFileAsync(request, UserId), "File saved.");

    /// <summary>Signs an original title file in or out. The chain of custody is the whole point.</summary>
    [HttpPost("files/{id:guid}/move")]
    public Task<IActionResult> MovePhysicalFile(
        Guid id,
        [FromQuery] string movement,
        [FromQuery] Guid? toUserId,
        [FromQuery] string? toName,
        [FromQuery] string? purpose,
        [FromQuery] DateOnly? dueBack)
        => Run(() => finance.MovePhysicalFileAsync(id, movement, toUserId, toName, purpose, dueBack, UserId),
            "Movement recorded.");

    // ── Litigation ───────────────────────────────────────────────────────────

    [HttpGet("legal-cases")]
    public Task<IActionResult> GetLegalCases([FromQuery] ListQueryDto query, [FromQuery] LegalCaseStatus? status)
        => RunPaged(() => finance.GetLegalCasesAsync(query, status));

    [HttpGet("legal-cases/{id:guid}")]
    public Task<IActionResult> GetLegalCase(Guid id)
        => RunFound(() => finance.GetLegalCaseAsync(id), "That case does not exist.");

    [HttpPost("legal-cases")]
    public Task<IActionResult> SaveLegalCase([FromBody] LegalCaseDto request)
        => Run(() => finance.SaveLegalCaseAsync(request, UserId), "Case saved.");

    [HttpPost("legal-cases/{id:guid}/hearings")]
    public Task<IActionResult> RecordHearing(Guid id, [FromBody] LegalHearingDto request)
        => Run(() => finance.RecordHearingAsync(id, request, UserId), "Hearing recorded.");
}
