using Distribution.Application.DTOs;
using Distribution.Application.Services.Interfaces;
using Distribution.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;

namespace Distribution.Api.Controllers;

/// <summary>
/// Secondary sales, distributor stock and the sell-in/sell-out reconciliation — the DMS layer.
/// </summary>
[Route("api/distribution/secondary")]
public class SecondarySalesController(ISecondarySalesService service, ILogger<SecondarySalesController> logger)
    : DistributionControllerBase(logger)
{
    [HttpGet]
    public Task<IActionResult> List(
        [FromQuery] Guid? partnerId, [FromQuery] Guid? outletId, [FromQuery] DateTime? from,
        [FromQuery] DateTime? to, [FromQuery] bool? unmappedOnly, [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListAsync(
            partnerId, outletId, from, to, unmappedOnly, pagination ?? new PaginationParams()));

    [HttpGet("{id:guid}")]
    public Task<IActionResult> Get(Guid id)
        => RunFound(() => service.GetAsync(id), "That record no longer exists.");

    /// <summary>The simple portal form, for partners who will never send a file.</summary>
    [HttpPost("declare")]
    public Task<IActionResult> Declare([FromBody] DeclareSecondarySalesDto request)
        => Run(() => service.DeclareAsync(request, UserId), "Secondary sales recorded.");

    /// <summary>Parses an uploaded file through the partner's mapping profile.</summary>
    [HttpPost("upload")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> Upload(
        [FromQuery] Guid partnerId, [FromQuery] DateTime periodStart, [FromQuery] DateTime periodEnd,
        [FromQuery] Guid? mappingProfileId, IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new ApiErrorResponse { Message = "No file was uploaded." });

        await using var stream = file.OpenReadStream();
        var content = stream;

        return await Run(() => service.UploadAsync(
            partnerId, periodStart, periodEnd, file.FileName, content, mappingProfileId, UserId),
            "File uploaded.");
    }

    [HttpGet("uploads/{id:guid}")]
    public Task<IActionResult> GetUpload(Guid id)
        => RunFound(() => service.GetUploadAsync(id), "That upload no longer exists.");

    [HttpGet("uploads")]
    public Task<IActionResult> ListUploads(
        [FromQuery] Guid? partnerId, [FromQuery] UploadBatchStatus? status, [FromQuery] DateTime? from,
        [FromQuery] DateTime? to, [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListUploadsAsync(
            partnerId, status, from, to, pagination ?? new PaginationParams()));

    [HttpPost("uploads/{id:guid}/post")]
    public Task<IActionResult> PostUpload(Guid id)
        => Run(() => service.PostUploadAsync(id, UserId), "Batch posted.");

    [HttpPost("uploads/{id:guid}/reject")]
    public Task<IActionResult> RejectUpload(Guid id, [FromQuery] string reason)
        => Run(() => service.RejectUploadAsync(id, reason, UserId), "Batch rejected.");

    /// <summary>Rows that would not map, with the best guesses at what they meant.</summary>
    [HttpGet("exceptions")]
    public Task<IActionResult> Exceptions([FromQuery] Guid? uploadId, [FromQuery] Guid? partnerId)
        => Run(() => service.GetMappingExceptionsAsync(uploadId, partnerId));

    /// <summary>Resolving an exception teaches the profile, so the same code maps itself next time.</summary>
    [HttpPost("exceptions/resolve")]
    public Task<IActionResult> ResolveMapping([FromBody] ResolveMappingDto request)
        => Run(() => service.ResolveMappingAsync(request, UserId), "Mapping resolved.");

    [HttpGet("mapping-profiles")]
    public Task<IActionResult> MappingProfiles([FromQuery] Guid? partnerId)
        => Run(() => service.ListMappingProfilesAsync(partnerId));

    [HttpPost("mapping-profiles")]
    public Task<IActionResult> CreateMappingProfile([FromBody] MappingProfileDto request)
        => Run(() => service.SaveMappingProfileAsync(null, request, UserId), "Profile created.");

    [HttpPut("mapping-profiles/{id:guid}")]
    public Task<IActionResult> UpdateMappingProfile(Guid id, [FromBody] MappingProfileDto request)
        => Run(() => service.SaveMappingProfileAsync(id, request, UserId), "Profile saved.");

    // ── Stock declarations & norms ───────────────────────────────────────────

    [HttpPost("stock")]
    public Task<IActionResult> SubmitStock([FromBody] SubmitStockDeclarationDto request)
        => Run(() => service.SubmitStockDeclarationAsync(request, UserId), "Stock declared.");

    [HttpGet("stock/{id:guid}")]
    public Task<IActionResult> GetStock(Guid id)
        => RunFound(() => service.GetStockDeclarationAsync(id), "That declaration no longer exists.");

    [HttpGet("stock")]
    public Task<IActionResult> ListStock(
        [FromQuery] Guid? partnerId, [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListStockDeclarationsAsync(
            partnerId, from, to, pagination ?? new PaginationParams()));

    [HttpPost("stock/{id:guid}/verify")]
    public Task<IActionResult> VerifyStock(Guid id)
        => Run(() => service.VerifyStockDeclarationAsync(id, UserId), "Declaration verified.");

    [HttpGet("norms")]
    public Task<IActionResult> Norms([FromQuery] Guid? partnerId, [FromQuery] bool? exceptionsOnly)
        => Run(() => service.ListNormsAsync(partnerId, exceptionsOnly));

    [HttpPost("norms")]
    public Task<IActionResult> CreateNorm([FromBody] StockNormDto request)
        => Run(() => service.SaveNormAsync(null, request, UserId), "Norm saved.");

    [HttpPut("norms/{id:guid}")]
    public Task<IActionResult> UpdateNorm(Guid id, [FromBody] StockNormDto request)
        => Run(() => service.SaveNormAsync(id, request, UserId), "Norm saved.");

    // ── Reconciliation ───────────────────────────────────────────────────────

    /// <summary>Opening + primary − secondary − returns = closing, per partner per SKU.</summary>
    [HttpPost("reconcile")]
    public Task<IActionResult> Reconcile(
        [FromQuery] Guid? partnerId, [FromQuery] DateTime periodStart, [FromQuery] DateTime periodEnd)
        => Run(() => service.ReconcileAsync(partnerId, periodStart, periodEnd, UserId), "Reconciliation run.");

    [HttpGet("reconciliations")]
    public Task<IActionResult> Reconciliations(
        [FromQuery] Guid? partnerId, [FromQuery] ReconciliationOutcome? outcome,
        [FromQuery] bool? unexplainedOnly, [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListReconciliationsAsync(
            partnerId, outcome, unexplainedOnly, from, to, pagination ?? new PaginationParams()));

    [HttpPost("reconciliations/explain")]
    public Task<IActionResult> Explain([FromBody] ExplainReconciliationDto request)
        => Run(() => service.ExplainAsync(request, UserId), "Variance explained.");

    /// <summary>What we sold in, what sold through, and what is still sitting in the trade.</summary>
    [HttpGet("channel-inventory")]
    public Task<IActionResult> ChannelInventory(
        [FromQuery] DateTime periodStart, [FromQuery] DateTime periodEnd, [FromQuery] Guid? territoryId)
        => Run(() => service.GetChannelInventoryAsync(periodStart, periodEnd, territoryId));

    /// <summary>How well each partner actually reports — a commercial fact, not an IT one.</summary>
    [HttpGet("data-quality")]
    public Task<IActionResult> DataQuality([FromQuery] DateTime periodStart, [FromQuery] DateTime periodEnd)
        => Run(() => service.GetDataQualityAsync(periodStart, periodEnd));
}

/// <summary>Targets, incentives and the field KPI set.</summary>
[Route("api/distribution/performance")]
public class PerformanceController(IPerformanceService service, ILogger<PerformanceController> logger)
    : DistributionControllerBase(logger)
{
    [HttpGet("targets")]
    public Task<IActionResult> Targets(
        [FromQuery] TargetScope? scope, [FromQuery] TargetMetric? metric, [FromQuery] Guid? fieldRepId,
        [FromQuery] Guid? territoryId, [FromQuery] DateTime? periodStart, [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListTargetsAsync(
            scope, metric, fieldRepId, territoryId, periodStart, pagination ?? new PaginationParams()));

    [HttpGet("targets/{id:guid}")]
    public Task<IActionResult> GetTarget(Guid id)
        => RunFound(() => service.GetTargetAsync(id), "That target no longer exists.");

    [HttpPost("targets")]
    public Task<IActionResult> CreateTarget([FromBody] SaveTargetDto request)
        => Run(() => service.SaveTargetAsync(null, request, UserId), "Target created.");

    [HttpPut("targets/{id:guid}")]
    public Task<IActionResult> UpdateTarget(Guid id, [FromBody] SaveTargetDto request)
        => Run(() => service.SaveTargetAsync(id, request, UserId), "Target saved.");

    [HttpPost("targets/{id:guid}/publish")]
    public Task<IActionResult> PublishTarget(Guid id)
        => Run(() => service.PublishTargetAsync(id, UserId), "Target published.");

    [HttpDelete("targets/{id:guid}")]
    public Task<IActionResult> DeleteTarget(Guid id)
        => Run(() => service.DeleteTargetAsync(id, UserId), "Target removed.");

    [HttpPost("targets/recompute")]
    public Task<IActionResult> RecomputeTargets([FromQuery] DateTime periodStart, [FromQuery] DateTime periodEnd)
        => Run(() => service.RecomputeTargetsAsync(periodStart, periodEnd), "Targets recomputed.");

    [HttpGet("incentives")]
    public Task<IActionResult> IncentiveSchemes([FromQuery] bool? activeOnly)
        => Run(() => service.ListIncentiveSchemesAsync(activeOnly));

    [HttpPost("incentives")]
    public Task<IActionResult> CreateIncentive([FromBody] IncentiveSchemeDto request)
        => Run(() => service.SaveIncentiveSchemeAsync(null, request, UserId), "Incentive scheme created.");

    [HttpPut("incentives/{id:guid}")]
    public Task<IActionResult> UpdateIncentive(Guid id, [FromBody] IncentiveSchemeDto request)
        => Run(() => service.SaveIncentiveSchemeAsync(id, request, UserId), "Incentive scheme saved.");

    [HttpPost("incentives/{id:guid}/compute")]
    public Task<IActionResult> ComputePayouts(
        Guid id, [FromQuery] DateTime periodStart, [FromQuery] DateTime periodEnd)
        => Run(() => service.ComputePayoutsAsync(id, periodStart, periodEnd, UserId), "Payouts computed.");

    [HttpPost("payouts/{id:guid}/approve")]
    public Task<IActionResult> ApprovePayout(
        Guid id, [FromQuery] bool isApproved = true, [FromQuery] string? note = null)
        => Run(() => service.ApprovePayoutAsync(id, isApproved, note, UserId), "Decision recorded.");

    [HttpGet("payouts")]
    public Task<IActionResult> Payouts(
        [FromQuery] Guid? schemeId, [FromQuery] Guid? fieldRepId,
        [FromQuery] DateTime? periodStart, [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListPayoutsAsync(
            schemeId, fieldRepId, periodStart, pagination ?? new PaginationParams()));

    /// <summary>Recomputes and stores the KPI snapshots for a date. Idempotent per date.</summary>
    [HttpPost("kpis/compute")]
    public Task<IActionResult> ComputeKpis([FromQuery] DateTime date)
        => Run(() => service.ComputeKpiSnapshotsAsync(date), "KPI snapshots computed.");

    [HttpGet("kpis")]
    public Task<IActionResult> Kpis(
        [FromQuery] TargetScope scope, [FromQuery] DateTime from, [FromQuery] DateTime to,
        [FromQuery] Guid? territoryId, [FromQuery] Guid? fieldRepId, [FromQuery] Guid? routeId)
        => Run(() => service.GetKpisAsync(scope, from, to, territoryId, fieldRepId, routeId));

    [HttpGet("leaderboard")]
    public Task<IActionResult> Leaderboard(
        [FromQuery] TargetScope scope, [FromQuery] TargetMetric metric,
        [FromQuery] DateTime from, [FromQuery] DateTime to)
        => Run(() => service.GetLeaderboardAsync(scope, metric, from, to));
}

/// <summary>Demand forecasting, replenishment and stock transfers.</summary>
[Route("api/distribution/planning")]
public class PlanningController(IPlanningService service, ILogger<PlanningController> logger)
    : DistributionControllerBase(logger)
{
    [HttpPost("forecasts")]
    public Task<IActionResult> Generate([FromBody] GenerateForecastDto request)
        => Run(() => service.GenerateForecastAsync(request, UserId), "Forecast generated.");

    [HttpGet("forecasts/{id:guid}")]
    public Task<IActionResult> GetForecast(Guid id)
        => RunFound(() => service.GetForecastAsync(id), "That forecast no longer exists.");

    [HttpGet("forecasts")]
    public Task<IActionResult> ListForecasts(
        [FromQuery] Guid? partnerId, [FromQuery] Guid? warehouseId,
        [FromQuery] DateTime? periodStart, [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListForecastsAsync(
            partnerId, warehouseId, periodStart, pagination ?? new PaginationParams()));

    [HttpPost("forecasts/override")]
    public Task<IActionResult> Override([FromBody] OverrideForecastLineDto request)
        => Run(() => service.OverrideLineAsync(request, UserId), "Override recorded.");

    [HttpPost("forecasts/{id:guid}/approve")]
    public Task<IActionResult> ApproveForecast(Guid id)
        => Run(() => service.ApproveForecastAsync(id, UserId), "Forecast approved.");

    [HttpPost("suggestions/generate")]
    public Task<IActionResult> GenerateSuggestions(
        [FromQuery] ReplenishmentTargetKind targetKind, [FromQuery] Guid? scopeId)
        => Run(() => service.GenerateSuggestionsAsync(targetKind, scopeId, UserId), "Suggestions rebuilt.");

    [HttpGet("suggestions")]
    public Task<IActionResult> Suggestions(
        [FromQuery] ReplenishmentTargetKind? targetKind, [FromQuery] Guid? partnerId,
        [FromQuery] Guid? warehouseId, [FromQuery] Guid? vanUnitId, [FromQuery] bool? openOnly,
        [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListSuggestionsAsync(
            targetKind, partnerId, warehouseId, vanUnitId, openOnly, pagination ?? new PaginationParams()));

    [HttpPost("suggestions/{id:guid}/dismiss")]
    public Task<IActionResult> Dismiss(Guid id, [FromQuery] string reason)
        => Run(() => service.DismissSuggestionAsync(id, reason, UserId), "Suggestion dismissed.");

    [HttpPost("transfers")]
    public Task<IActionResult> CreateTransfer([FromBody] List<Guid> suggestionIds)
        => Run(() => service.CreateTransferAsync(suggestionIds, UserId), "Transfer raised.");

    [HttpGet("transfers")]
    public Task<IActionResult> Transfers(
        [FromQuery] TransferRequestStatus? status, [FromQuery] Guid? partnerId,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListTransfersAsync(
            status, partnerId, from, to, pagination ?? new PaginationParams()));

    [HttpPost("transfers/{id:guid}/decide")]
    public Task<IActionResult> DecideTransfer(
        Guid id, [FromQuery] bool isApproved = true, [FromQuery] string? reason = null)
        => Run(() => service.DecideTransferAsync(id, isApproved, reason, UserId), "Decision recorded.");
}

/// <summary>Batch traceability, cold chain, expiry and recall.</summary>
[Route("api/distribution/traceability")]
public class TraceabilityController(ITraceabilityService service, ILogger<TraceabilityController> logger)
    : DistributionControllerBase(logger)
{
    [HttpGet("checkpoints")]
    public Task<IActionResult> Checkpoints([FromQuery] Guid? warehouseId, [FromQuery] bool? breachedOnly)
        => Run(() => service.ListCheckpointsAsync(warehouseId, breachedOnly));

    [HttpPost("checkpoints")]
    public Task<IActionResult> CreateCheckpoint([FromBody] ColdChainCheckpointDto request)
        => Run(() => service.SaveCheckpointAsync(null, request, UserId), "Checkpoint created.");

    [HttpPut("checkpoints/{id:guid}")]
    public Task<IActionResult> UpdateCheckpoint(Guid id, [FromBody] ColdChainCheckpointDto request)
        => Run(() => service.SaveCheckpointAsync(id, request, UserId), "Checkpoint saved.");

    /// <summary>An out-of-range reading cannot be filed without a corrective action.</summary>
    [HttpPost("readings")]
    public Task<IActionResult> RecordReading([FromBody] RecordColdChainReadingDto request)
        => Run(() => service.RecordReadingAsync(request, UserId), "Reading recorded.");

    [HttpGet("readings")]
    public Task<IActionResult> Readings(
        [FromQuery] Guid? checkpointId, [FromQuery] bool? excursionsOnly, [FromQuery] bool? unresolvedOnly,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListReadingsAsync(
            checkpointId, excursionsOnly, unresolvedOnly, from, to, pagination ?? new PaginationParams()));

    [HttpPost("readings/{id:guid}/resolve")]
    public Task<IActionResult> ResolveExcursion(
        Guid id, [FromQuery] string correctiveAction, [FromQuery] decimal affectedValue = 0)
        => Run(() => service.ResolveExcursionAsync(id, correctiveAction, affectedValue, UserId),
            "Excursion closed.");

    /// <summary>Forward and backward trace in one answer — what a recall actually needs.</summary>
    [HttpGet("trace")]
    public Task<IActionResult> Trace([FromQuery] Guid? itemId, [FromQuery] string batchNumber)
        => RunFound(() => service.TraceBatchAsync(itemId, batchNumber), "No movements found for that batch.");

    [HttpGet("trace/from-outlet")]
    public Task<IActionResult> TraceFromOutlet(
        [FromQuery] Guid outletId, [FromQuery] Guid itemId, [FromQuery] DateTime? asOf)
        => RunFound(() => service.TraceFromOutletAsync(outletId, itemId, asOf),
            "No movements found for that outlet and item.");

    [HttpPost("recalls")]
    public Task<IActionResult> InitiateRecall([FromBody] InitiateRecallDto request)
        => Run(() => service.InitiateRecallAsync(request, UserId), "Recall raised.");

    [HttpGet("recalls/{id:guid}")]
    public Task<IActionResult> GetRecall(Guid id)
        => RunFound(() => service.GetRecallAsync(id), "That recall no longer exists.");

    [HttpGet("recalls")]
    public Task<IActionResult> ListRecalls([FromQuery] RecallStatus? status, [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListRecallsAsync(status, pagination ?? new PaginationParams()));

    /// <summary>Builds the affected-outlet list from the trace links and announces the recall.</summary>
    [HttpPost("recalls/{id:guid}/announce")]
    public Task<IActionResult> AnnounceRecall(Guid id)
        => Run(() => service.AnnounceRecallAsync(id, UserId), "Recall announced.");

    [HttpPost("recalls/notices/{id:guid}")]
    public Task<IActionResult> UpdateNotice(
        Guid id, [FromQuery] decimal returnedQuantity, [FromQuery] bool isClosed,
        [FromQuery] string? note)
        => Run(() => service.UpdateNoticeAsync(id, returnedQuantity, isClosed, note, UserId), "Notice updated.");

    [HttpPost("recalls/{id:guid}/complete")]
    public Task<IActionResult> CompleteRecall(Guid id, [FromQuery] string closureReport)
        => Run(() => service.CompleteRecallAsync(id, closureReport, UserId), "Recall closed.");

    [HttpGet("near-expiry")]
    public Task<IActionResult> NearExpiry(
        [FromQuery] int? withinDays, [FromQuery] Guid? warehouseId, [FromQuery] Guid? vanUnitId,
        [FromQuery] Guid? partnerId, [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.GetNearExpiryAsync(
            withinDays, warehouseId, vanUnitId, partnerId, pagination ?? new PaginationParams()));
}

/// <summary>Dashboards, reports and the exception queue.</summary>
[Route("api/distribution/reports")]
public class DistributionReportController(
    IDistributionReportService service, ILogger<DistributionReportController> logger)
    : DistributionControllerBase(logger)
{
    [HttpGet("dashboard")]
    public Task<IActionResult> Dashboard([FromQuery] Guid? territoryId, [FromQuery] DateTime? asOf)
        => Run(() => service.GetDashboardAsync(territoryId, asOf));

    /// <summary>Everything that needs a human today, in one ranked list.</summary>
    [HttpGet("exceptions")]
    public Task<IActionResult> Exceptions([FromQuery] Guid? territoryId)
        => Run(() => service.GetExceptionsAsync(territoryId));

    [HttpPost("sales")]
    public Task<IActionResult> Sales([FromBody] DistributionReportFilter filter, [FromQuery] string groupBy = "Outlet")
        => Run(() => service.GetSalesReportAsync(filter, groupBy));

    [HttpPost("productivity")]
    public Task<IActionResult> Productivity([FromBody] DistributionReportFilter filter)
        => Run(() => service.GetProductivityReportAsync(filter));

    [HttpPost("outlets")]
    public Task<IActionResult> Outlets([FromBody] DistributionReportFilter filter)
        => Run(() => service.GetOutletAnalyticsAsync(filter));

    [HttpPost("logistics")]
    public Task<IActionResult> Logistics([FromBody] DistributionReportFilter filter)
        => Run(() => service.GetLogisticsReportAsync(filter));

    [HttpPost("receivables")]
    public Task<IActionResult> Receivables([FromBody] DistributionReportFilter filter)
        => Run(() => service.GetReceivablesReportAsync(filter));

    [HttpPost("returns")]
    public Task<IActionResult> Returns([FromBody] DistributionReportFilter filter)
        => Run(() => service.GetReturnsReportAsync(filter));

    [HttpPost("claims")]
    public Task<IActionResult> Claims([FromBody] DistributionReportFilter filter)
        => Run(() => service.GetClaimsReportAsync(filter));

    [HttpPost("stock")]
    public Task<IActionResult> Stock([FromBody] DistributionReportFilter filter)
        => Run(() => service.GetStockReportAsync(filter));
}

/// <summary>Reason codes, settings and notifications.</summary>
[Route("api/distribution/admin")]
public class DistributionAdminController(
    IDistributionAdminService service, ILogger<DistributionAdminController> logger)
    : DistributionControllerBase(logger)
{
    [HttpGet("reasons")]
    public Task<IActionResult> Reasons([FromQuery] ReasonSurface? surface, [FromQuery] bool? activeOnly)
        => Run(() => service.ListReasonCodesAsync(surface, activeOnly));

    [HttpPost("reasons")]
    public Task<IActionResult> CreateReason([FromBody] ReasonCodeDto request)
        => Run(() => service.SaveReasonCodeAsync(null, request, UserId), "Reason created.");

    [HttpPut("reasons/{id:guid}")]
    public Task<IActionResult> UpdateReason(Guid id, [FromBody] ReasonCodeDto request)
        => Run(() => service.SaveReasonCodeAsync(id, request, UserId), "Reason saved.");

    [HttpDelete("reasons/{id:guid}")]
    public Task<IActionResult> DeleteReason(Guid id)
        => Run(() => service.DeleteReasonCodeAsync(id, UserId), "Reason removed.");

    [HttpGet("settings")]
    public Task<IActionResult> Settings()
        => Run(() => service.GetSettingsAsync());

    [HttpPut("settings")]
    public Task<IActionResult> SaveSettings([FromBody] DistributionSettingsDto request)
        => Run(() => service.SaveSettingsAsync(request, UserId), "Settings saved.");

    [HttpGet("notifications")]
    public Task<IActionResult> Notifications([FromQuery] bool? unreadOnly, [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListNotificationsAsync(UserId, unreadOnly, pagination ?? new PaginationParams()));

    [HttpPost("notifications/{id:guid}/read")]
    public Task<IActionResult> MarkRead(Guid id)
        => Run(() => service.MarkReadAsync(id, UserId));

    [HttpPost("notifications/read-all")]
    public Task<IActionResult> MarkAllRead()
        => Run(() => service.MarkAllReadAsync(UserId), "All marked read.");

    [HttpGet("notifications/unread-count")]
    public Task<IActionResult> UnreadCount()
        => Run(() => service.GetUnreadCountAsync(UserId));
}
