using Microsoft.AspNetCore.Mvc;
using RealEstate.Application.DTOs;
using RealEstate.Application.Services.Interfaces;
using RealEstate.Domain.Enums;

namespace RealEstate.Api.Controllers;

/// <summary>
/// Everything that happens on site: the work breakdown, bills of quantities, rates, programme and
/// progress, payment certificates, tenders and subcontracts, variations, delays, materials, labour,
/// plant, safety — and the turnkey contracts built on somebody else's land.
///
/// The interim payment certificate is the most consequential arithmetic in the application: it is
/// what a contractor gets paid, and every deduction on it is a conversation. It is built in the
/// order the contract reads — gross value to date, less previously certified, less retention to its
/// cap, less advance recovery, less contra-charges — and returns its own workings in words. A
/// certificate a quantity surveyor cannot follow line by line is one that gets disputed.
/// </summary>
[Route("api/realestate/construction")]
public class ConstructionController(
    IConstructionService construction,
    ILogger<ConstructionController> logger) : RealEstateControllerBase(logger)
{
    // ── Projects and work breakdown ──────────────────────────────────────────

    [HttpGet("projects")]
    public Task<IActionResult> GetProjects([FromQuery] ListQueryDto query, [FromQuery] ProjectStatus? status)
        => RunPaged(() => construction.GetProjectsAsync(query, status));

    [HttpGet("projects/{id:guid}")]
    public Task<IActionResult> GetProject(Guid id)
        => RunFound(() => construction.GetProjectAsync(id), "That project does not exist.");

    [HttpPost("projects")]
    public Task<IActionResult> SaveProject([FromBody] ConstructionProjectDetailDto request)
        => Run(() => construction.SaveProjectAsync(request, UserId), "Project saved.");

    [HttpGet("projects/{id:guid}/wbs")]
    public Task<IActionResult> GetWbs(Guid id) => Run(() => construction.GetWbsAsync(id));

    [HttpPost("wbs")]
    public Task<IActionResult> SaveWbsNode([FromBody] WbsNodeUpsertDto request)
        => Run(() => construction.SaveWbsNodeAsync(request, UserId), "Saved.");

    [HttpDelete("wbs/{id:guid}")]
    public Task<IActionResult> DeleteWbsNode(Guid id)
        => Run(() => construction.DeleteWbsNodeAsync(id, UserId), "Removed.");

    // ── Bills of quantities ──────────────────────────────────────────────────

    [HttpGet("projects/{id:guid}/boqs")]
    public Task<IActionResult> GetBoqs(Guid id) => Run(() => construction.GetBoqsAsync(id));

    [HttpGet("boqs/{id:guid}")]
    public Task<IActionResult> GetBoq(Guid id)
        => RunFound(() => construction.GetBoqAsync(id), "That bill of quantities does not exist.");

    [HttpPost("boqs")]
    public Task<IActionResult> SaveBoq([FromBody] BillOfQuantitiesDto request)
        => Run(() => construction.SaveBoqAsync(request, UserId), "Bill saved.");

    [HttpPost("boqs/lines")]
    public Task<IActionResult> SaveBoqLine([FromBody] BoqLineUpsertDto request)
        => Run(() => construction.SaveBoqLineAsync(request, UserId), "Line saved.");

    [HttpPost("boqs/{id:guid}/import")]
    public Task<IActionResult> ImportBoqLines(Guid id, [FromBody] List<BoqLineUpsertDto> lines)
        => Run(() => construction.ImportBoqLinesAsync(id, lines, UserId), "Lines imported.");

    /// <summary>Rate build-ups: labour, material, plant and overhead behind each unit rate.</summary>
    [HttpGet("rate-analyses")]
    public Task<IActionResult> GetRateAnalyses(
        [FromQuery] Guid? constructionProjectId, [FromQuery] bool libraryOnly = false)
        => Run(() => construction.GetRateAnalysesAsync(constructionProjectId, libraryOnly));

    [HttpPost("rate-analyses")]
    public Task<IActionResult> SaveRateAnalysis([FromBody] RateAnalysisDto request)
        => Run(() => construction.SaveRateAnalysisAsync(request, UserId), "Analysis saved.");

    [HttpGet("estimates")]
    public Task<IActionResult> GetEstimates([FromQuery] ListQueryDto query)
        => RunPaged(() => construction.GetEstimatesAsync(query));

    [HttpPost("estimates")]
    public Task<IActionResult> SaveEstimate([FromBody] EstimateDto request)
        => Run(() => construction.SaveEstimateAsync(request, UserId), "Estimate saved.");

    [HttpPost("estimates/{id:guid}/to-boq")]
    public Task<IActionResult> ConvertEstimate(Guid id, [FromQuery] Guid constructionProjectId)
        => Run(() => construction.ConvertEstimateToBoqAsync(id, constructionProjectId, UserId), "Converted.");

    // ── Specifications ───────────────────────────────────────────────────────

    [HttpGet("specifications")]
    public Task<IActionResult> GetSpecifications(
        [FromQuery] Guid? constructionProjectId, [FromQuery] Guid? clientBuildContractId)
        => Run(() => construction.GetSpecificationsAsync(constructionProjectId, clientBuildContractId));

    [HttpPost("specifications")]
    public Task<IActionResult> SaveSpecification([FromBody] SpecificationScheduleDto request)
        => Run(() => construction.SaveSpecificationAsync(request, UserId), "Specification saved.");

    /// <summary>
    /// Freezes a specification so it becomes contractual. After this, changing a make or a finish
    /// is a variation with a price, which is exactly the discipline a fixed-price build needs.
    /// </summary>
    [HttpPost("specifications/{id:guid}/freeze")]
    public Task<IActionResult> FreezeSpecification(Guid id)
        => Run(() => construction.FreezeSpecificationAsync(id, UserId), "Specification frozen.");

    // ── Programme and progress ───────────────────────────────────────────────

    [HttpGet("projects/{id:guid}/programme")]
    public Task<IActionResult> GetProgramme(Guid id) => Run(() => construction.GetProgrammeAsync(id));

    [HttpPost("projects/{id:guid}/programme")]
    public Task<IActionResult> SaveActivity(Guid id, [FromBody] ProgrammeActivityDto request)
        => Run(() => construction.SaveActivityAsync(id, request, UserId), "Activity saved.");

    /// <summary>Forward and backward pass, so the float and the critical path are current.</summary>
    [HttpPost("projects/{id:guid}/programme/critical-path")]
    public Task<IActionResult> RecalculateCriticalPath(Guid id)
        => Run(() => construction.RecalculateCriticalPathAsync(id, UserId), "Critical path recalculated.");

    [HttpPost("projects/{id:guid}/programme/baseline")]
    public Task<IActionResult> Baseline(Guid id)
        => Run(() => construction.BaselineAsync(id, UserId), "Programme baselined.");

    [HttpPost("progress")]
    public Task<IActionResult> SaveProgress([FromBody] ProgressMeasurementDto request)
        => Run(() => construction.SaveProgressAsync(request, UserId), "Progress saved.");

    /// <summary>Uploads measurements taken on site with no signal.</summary>
    [HttpPost("progress/sync")]
    public Task<IActionResult> SyncProgress([FromBody] ProgressSyncBatchDto batch)
        => Run(() => construction.SyncProgressAsync(batch, UserId), "Progress synchronised.");

    [HttpPost("progress/{id:guid}/certify")]
    public Task<IActionResult> CertifyProgress(Guid id)
        => Run(() => construction.CertifyProgressAsync(id, UserId), "Progress certified.");

    [HttpGet("progress")]
    public Task<IActionResult> GetProgress(
        [FromQuery] ListQueryDto query,
        [FromQuery] Guid? constructionProjectId,
        [FromQuery] Guid? subcontractId)
        => RunPaged(() => construction.GetProgressAsync(query, constructionProjectId, subcontractId));

    // ── Payment certificates ─────────────────────────────────────────────────

    [HttpPost("ipcs")]
    public Task<IActionResult> PrepareIpc([FromBody] IpcCreateDto request)
        => Run(() => construction.PrepareIpcAsync(request, UserId), "Certificate prepared.");

    [HttpPost("ipcs/{id:guid}/certify")]
    public Task<IActionResult> CertifyIpc(Guid id)
        => Run(() => construction.CertifyIpcAsync(id, UserId), "Certificate certified.");

    [HttpPost("ipcs/{id:guid}/approve")]
    public Task<IActionResult> ApproveIpc(
        Guid id, [FromQuery] ApprovalOutcome outcome, [FromQuery] string? comment)
        => Run(() => construction.ApproveIpcAsync(id, outcome, comment, UserId), "Decision recorded.");

    [HttpGet("ipcs")]
    public Task<IActionResult> GetIpcs(
        [FromQuery] ListQueryDto query,
        [FromQuery] Guid? constructionProjectId,
        [FromQuery] string? direction,
        [FromQuery] CertificateStatus? status)
        => RunPaged(() => construction.GetIpcsAsync(query, constructionProjectId, direction, status));

    [HttpGet("ipcs/{id:guid}")]
    public Task<IActionResult> GetIpc(Guid id)
        => RunFound(() => construction.GetIpcAsync(id), "That certificate does not exist.");

    // ── Retention and advances ───────────────────────────────────────────────

    [HttpGet("retention")]
    public Task<IActionResult> GetRetention(
        [FromQuery] ListQueryDto query,
        [FromQuery] Guid? subcontractId,
        [FromQuery] bool dueForReleaseOnly = false)
        => RunPaged(() => construction.GetRetentionAsync(query, subcontractId, dueForReleaseOnly));

    [HttpPost("retention/release")]
    public Task<IActionResult> ReleaseRetention(
        [FromQuery] Guid? subcontractId,
        [FromQuery] Guid? clientBuildContractId,
        [FromQuery] decimal amount,
        [FromQuery] RetentionMovement movement)
        => Run(() => construction.ReleaseRetentionAsync(subcontractId, clientBuildContractId, amount, movement, UserId),
            "Retention released.");

    [HttpPost("advances")]
    public Task<IActionResult> CreateAdvance([FromBody] AdvancePaymentDto request)
        => Run(() => construction.CreateAdvanceAsync(request, UserId), "Advance recorded.");

    [HttpPost("materials-on-site")]
    public Task<IActionResult> SaveMaterialsOnSite([FromBody] MaterialsOnSiteDto request)
        => Run(() => construction.SaveMaterialsOnSiteAsync(request, UserId), "Saved.");

    /// <summary>
    /// Rebuilds the cost to complete from what has been spent and what is left. This is the number
    /// that tells a builder whether the job is still making money while there is time to react.
    /// </summary>
    [HttpPost("projects/{id:guid}/cost-to-complete")]
    public Task<IActionResult> RecalculateCostToComplete(Guid id, [FromQuery] DateOnly? asOf)
        => Run(() => construction.RecalculateCostToCompleteAsync(
            id, asOf ?? DateOnly.FromDateTime(DateTime.UtcNow), UserId), "Recalculated.");

    // ── Tendering ────────────────────────────────────────────────────────────

    [HttpGet("tenders")]
    public Task<IActionResult> GetTenders([FromQuery] ListQueryDto query, [FromQuery] TenderStatus? status)
        => RunPaged(() => construction.GetTendersAsync(query, status));

    [HttpGet("tenders/{id:guid}")]
    public Task<IActionResult> GetTender(Guid id)
        => RunFound(() => construction.GetTenderAsync(id), "That tender does not exist.");

    [HttpPost("tenders")]
    public Task<IActionResult> SaveTender([FromBody] TenderDto request)
        => Run(() => construction.SaveTenderAsync(request, UserId), "Tender saved.");

    [HttpPost("tenders/{id:guid}/bids")]
    public Task<IActionResult> SubmitBid(Guid id, [FromBody] TenderBidDto bid)
        => Run(() => construction.SubmitBidAsync(id, bid, UserId), "Bid recorded.");

    /// <summary>
    /// Awards the tender. The justification is required even when the lowest bid wins, because the
    /// question always asked afterwards is why the other one did not.
    /// </summary>
    [HttpPost("tenders/{id:guid}/award")]
    public Task<IActionResult> AwardTender(
        Guid id, [FromQuery] Guid bidId, [FromQuery] string justification)
        => Run(() => construction.AwardTenderAsync(id, bidId, justification, UserId), "Tender awarded.");

    [HttpGet("tenders/{id:guid}/comparison")]
    public Task<IActionResult> GetBidComparison(Guid id)
        => Run(() => construction.GetBidComparisonAsync(id));

    // ── Subcontracts ─────────────────────────────────────────────────────────

    [HttpGet("subcontracts")]
    public Task<IActionResult> GetSubcontracts(
        [FromQuery] ListQueryDto query,
        [FromQuery] Guid? constructionProjectId,
        [FromQuery] SubcontractStatus? status)
        => RunPaged(() => construction.GetSubcontractsAsync(query, constructionProjectId, status));

    [HttpGet("subcontracts/{id:guid}")]
    public Task<IActionResult> GetSubcontract(Guid id)
        => RunFound(() => construction.GetSubcontractAsync(id), "That subcontract does not exist.");

    [HttpPost("subcontracts")]
    public Task<IActionResult> SaveSubcontract([FromBody] SubcontractCreateDto request)
        => Run(() => construction.SaveSubcontractAsync(request, UserId), "Subcontract saved.");

    [HttpPost("subcontracts/{id:guid}/status")]
    public Task<IActionResult> ChangeSubcontractStatus(
        Guid id, [FromQuery] SubcontractStatus status, [FromQuery] string? reason)
        => Run(() => construction.ChangeSubcontractStatusAsync(id, status, reason, UserId), "Status updated.");

    [HttpPost("claims")]
    public Task<IActionResult> SubmitClaim([FromBody] SubcontractorClaimDto request)
        => Run(() => construction.SubmitClaimAsync(request, UserId), "Claim submitted.");

    /// <summary>
    /// Certifies a claim line by line. What is disallowed is recorded with its reason — a
    /// subcontractor who is paid less than they claimed will ask, and "the system said so" is not
    /// an answer that survives a site meeting.
    /// </summary>
    [HttpPost("claims/{id:guid}/certify")]
    public Task<IActionResult> CertifyClaim(Guid id, [FromBody] List<ClaimCertificationDto> certifications)
        => Run(() => construction.CertifyClaimAsync(id, certifications, UserId), "Claim certified.");

    [HttpGet("claims")]
    public Task<IActionResult> GetClaims(
        [FromQuery] ListQueryDto query,
        [FromQuery] Guid? subcontractId,
        [FromQuery] CertificateStatus? status)
        => RunPaged(() => construction.GetClaimsAsync(query, subcontractId, status));

    [HttpPost("contra-charges")]
    public Task<IActionResult> SaveContraCharge([FromBody] ContraChargeDto request)
        => Run(() => construction.SaveContraChargeAsync(request, UserId), "Contra charge saved.");

    // ── Variations, instructions and delays ──────────────────────────────────

    [HttpGet("variations")]
    public Task<IActionResult> GetVariations(
        [FromQuery] ListQueryDto query,
        [FromQuery] Guid? constructionProjectId,
        [FromQuery] VariationStatus? status)
        => RunPaged(() => construction.GetVariationsAsync(query, constructionProjectId, status));

    [HttpGet("variations/{id:guid}")]
    public Task<IActionResult> GetVariation(Guid id)
        => RunFound(() => construction.GetVariationAsync(id), "That variation does not exist.");

    [HttpPost("variations")]
    public Task<IActionResult> SaveVariation([FromBody] VariationOrderUpsertDto request)
        => Run(() => construction.SaveVariationAsync(request, UserId), "Variation saved.");

    [HttpPost("variations/{id:guid}/decide")]
    public Task<IActionResult> DecideVariation(
        Guid id, [FromQuery] VariationStatus status, [FromQuery] string? reason)
        => Run(() => construction.DecideVariationAsync(id, status, reason, UserId), "Decision recorded.");

    [HttpPost("site-instructions")]
    public Task<IActionResult> IssueSiteInstruction([FromBody] SiteInstructionDto request)
        => Run(() => construction.IssueSiteInstructionAsync(request, UserId), "Instruction issued.");

    [HttpGet("site-instructions")]
    public Task<IActionResult> GetSiteInstructions(
        [FromQuery] ListQueryDto query, [FromQuery] Guid? constructionProjectId)
        => RunPaged(() => construction.GetSiteInstructionsAsync(query, constructionProjectId));

    [HttpPost("delays")]
    public Task<IActionResult> SaveDelay([FromBody] DelayEventDto request)
        => Run(() => construction.SaveDelayAsync(request, UserId), "Delay recorded.");

    [HttpGet("delays")]
    public Task<IActionResult> GetDelays([FromQuery] ListQueryDto query, [FromQuery] Guid? constructionProjectId)
        => RunPaged(() => construction.GetDelaysAsync(query, constructionProjectId));

    [HttpPost("extensions")]
    public Task<IActionResult> SaveExtension([FromBody] ExtensionOfTimeDto request)
        => Run(() => construction.SaveExtensionOfTimeAsync(request, UserId), "Claim saved.");

    [HttpPost("extensions/{id:guid}/decide")]
    public Task<IActionResult> DecideExtension(
        Guid id,
        [FromQuery] int daysGranted,
        [FromQuery] bool prolongationGranted,
        [FromQuery] string? note)
        => Run(() => construction.DecideExtensionAsync(id, daysGranted, prolongationGranted, note, UserId),
            "Decision recorded.");

    // ── Materials, labour and plant ──────────────────────────────────────────

    [HttpPost("requisitions")]
    public Task<IActionResult> SaveRequisition([FromBody] MaterialRequisitionDto request)
        => Run(() => construction.SaveRequisitionAsync(request, UserId), "Requisition saved.");

    [HttpPost("requisitions/{id:guid}/approve")]
    public Task<IActionResult> ApproveRequisition(
        Guid id, [FromQuery] ApprovalOutcome outcome, [FromQuery] string? comment)
        => Run(() => construction.ApproveRequisitionAsync(id, outcome, comment, UserId), "Decision recorded.");

    [HttpGet("requisitions")]
    public Task<IActionResult> GetRequisitions(
        [FromQuery] ListQueryDto query,
        [FromQuery] Guid? constructionProjectId,
        [FromQuery] string? status)
        => RunPaged(() => construction.GetRequisitionsAsync(query, constructionProjectId, status));

    [HttpPost("material-issues")]
    public Task<IActionResult> IssueMaterial([FromBody] MaterialIssueDto request)
        => Run(() => construction.IssueMaterialAsync(request, UserId), "Material issued.");

    [HttpGet("material-issues")]
    public Task<IActionResult> GetIssues([FromQuery] ListQueryDto query, [FromQuery] Guid? constructionProjectId)
        => RunPaged(() => construction.GetIssuesAsync(query, constructionProjectId));

    /// <summary>
    /// Compares what was consumed against what the norms say should have been. The difference is
    /// wastage, and on a large site it is a bigger number than most people expect.
    /// </summary>
    [HttpPost("projects/{id:guid}/wastage")]
    public Task<IActionResult> CalculateWastage(
        Guid id, [FromQuery] DateOnly from, [FromQuery] DateOnly to)
        => Run(() => construction.CalculateWastageAsync(id, from, to, UserId), "Wastage calculated.");

    [HttpPost("labour")]
    public Task<IActionResult> SaveLabour([FromBody] LabourRecordDto request)
        => Run(() => construction.SaveLabourAsync(request, UserId), "Labour recorded.");

    [HttpGet("labour")]
    public Task<IActionResult> GetLabour([FromQuery] ListQueryDto query, [FromQuery] Guid? constructionProjectId)
        => RunPaged(() => construction.GetLabourAsync(query, constructionProjectId));

    [HttpGet("plant")]
    public Task<IActionResult> GetPlant([FromQuery] string? status)
        => Run(() => construction.GetPlantAsync(status));

    [HttpPost("plant")]
    public Task<IActionResult> SavePlant([FromBody] PlantItemDto request)
        => Run(() => construction.SavePlantAsync(request, UserId), "Plant saved.");

    [HttpPost("plant/allocate")]
    public Task<IActionResult> AllocatePlant([FromBody] PlantAllocationDto request)
        => Run(() => construction.AllocatePlantAsync(request, UserId), "Plant allocated.");

    [HttpPost("site-gate")]
    public Task<IActionResult> RecordSiteGateEntry([FromBody] SiteGateEntryDto request)
        => Run(() => construction.RecordSiteGateEntryAsync(request, UserId), "Entry recorded.");

    // ── Safety ───────────────────────────────────────────────────────────────

    [HttpPost("safety-incidents")]
    public Task<IActionResult> SaveSafetyIncident([FromBody] SafetyIncidentDto request)
        => Run(() => construction.SaveSafetyIncidentAsync(request, UserId), "Incident recorded.");

    [HttpGet("safety-incidents")]
    public Task<IActionResult> GetSafetyIncidents(
        [FromQuery] ListQueryDto query, [FromQuery] Guid? constructionProjectId)
        => RunPaged(() => construction.GetSafetyIncidentsAsync(query, constructionProjectId));

    // ── Client builds ────────────────────────────────────────────────────────

    [HttpGet("client-builds")]
    public Task<IActionResult> GetClientBuilds([FromQuery] ListQueryDto query, [FromQuery] string? status)
        => RunPaged(() => construction.GetClientBuildsAsync(query, status));

    [HttpGet("client-builds/{id:guid}")]
    public Task<IActionResult> GetClientBuild(Guid id)
        => RunFound(() => construction.GetClientBuildAsync(id), "That contract does not exist.");

    [HttpPost("client-builds")]
    public Task<IActionResult> SaveClientBuild([FromBody] ClientBuildContractUpsertDto request)
        => Run(() => construction.SaveClientBuildAsync(request, UserId), "Contract saved.");

    [HttpPost("client-builds/{id:guid}/status")]
    public Task<IActionResult> ChangeClientBuildStatus(Guid id, [FromQuery] string status)
        => Run(() => construction.ChangeClientBuildStatusAsync(id, status, UserId), "Status updated.");

    [HttpPost("client-builds/variations")]
    public Task<IActionResult> SaveClientVariation([FromBody] ClientVariationUpsertDto request)
        => Run(() => construction.SaveClientVariationAsync(request, UserId), "Variation saved.");

    [HttpPost("client-builds/variations/{id:guid}/decide")]
    public Task<IActionResult> DecideClientVariation(
        Guid id,
        [FromQuery] bool approved,
        [FromQuery] string? reason,
        [FromQuery] string? evidenceUrl)
        => Run(() => construction.DecideClientVariationAsync(id, approved, reason, evidenceUrl, UserId),
            "Decision recorded.");

    [HttpGet("client-builds/variations")]
    public Task<IActionResult> GetClientVariations(
        [FromQuery] ListQueryDto query, [FromQuery] Guid? contractId, [FromQuery] VariationStatus? status)
        => RunPaged(() => construction.GetClientVariationsAsync(query, contractId, status));

    [HttpGet("client-builds/{id:guid}/cost-sheet")]
    public Task<IActionResult> GetCostSheet(Guid id, [FromQuery] DateOnly? asOf)
        => Run(() => construction.GetContractCostSheetAsync(id, asOf));

    /// <summary>
    /// Rebuilds the cost sheet and attributes margin erosion to what caused it — variations
    /// absorbed, wastage, rework, delay, rate increases — with whatever is left honestly labelled
    /// as unexplained rather than quietly folded into one of the others.
    /// </summary>
    [HttpPost("client-builds/{id:guid}/cost-sheet")]
    public Task<IActionResult> RecalculateCostSheet(Guid id)
        => Run(() => construction.RecalculateCostSheetAsync(id, UserId), "Cost sheet recalculated.");

    [HttpGet("drawings")]
    public Task<IActionResult> GetDrawings(
        [FromQuery] Guid? clientBuildContractId, [FromQuery] Guid? constructionProjectId)
        => Run(() => construction.GetDrawingsAsync(clientBuildContractId, constructionProjectId));

    [HttpPost("drawings")]
    public Task<IActionResult> SaveDrawing([FromBody] DrawingRegisterDto request)
        => Run(() => construction.SaveDrawingAsync(request, UserId), "Drawing saved.");

    [HttpPost("drawings/{id:guid}/revisions")]
    public Task<IActionResult> AddRevision(Guid id, [FromBody] DrawingRevisionDto request)
        => Run(() => construction.AddRevisionAsync(id, request, UserId), "Revision added.");

    [HttpGet("client-builds/portal/{partyId:guid}")]
    public Task<IActionResult> GetClientPortal(Guid partyId)
        => Run(() => construction.GetClientPortalHomeAsync(partyId));
}
