using Microsoft.AspNetCore.Mvc;
using Restaurant.Application.DTOs;
using Restaurant.Domain.Enums;
using Restaurant.Infrastructure.Services;

namespace Restaurant.Api.Controllers;

/// <summary>
/// Food safety: temperature logs, checklists, prep batches — and the delivery-zone engine.
///
/// Grouped together because both answer "may we do this?" before an action rather than reporting
/// on it afterwards: may this food be served, and may this address be delivered to.
/// </summary>
[Route("api/restaurant/compliance")]
public class ComplianceController(
    ComplianceService compliance,
    ILogger<ComplianceController> logger) : RestaurantControllerBase(logger)
{
    /// <summary>What is due, overdue or breached right now.</summary>
    [HttpGet("board/{outletId:guid}")]
    public Task<IActionResult> Board(Guid outletId) => Run(() => compliance.GetBoardAsync(outletId));

    // ── Temperature ──────────────────────────────────────────────────────────

    [HttpGet("checkpoints/{outletId:guid}")]
    public Task<IActionResult> GetCheckpoints(Guid outletId)
        => Run(() => compliance.GetCheckpointsAsync(outletId));

    [HttpPost("checkpoints")]
    public Task<IActionResult> CreateCheckpoint([FromBody] SaveTemperatureCheckpointDto request)
        => Run(() => compliance.SaveCheckpointAsync(null, request, UserId), "Checkpoint added.");

    [HttpPut("checkpoints/{id:guid}")]
    public Task<IActionResult> UpdateCheckpoint(Guid id, [FromBody] SaveTemperatureCheckpointDto request)
        => Run(() => compliance.SaveCheckpointAsync(id, request, UserId), "Checkpoint saved.");

    [HttpDelete("checkpoints/{id:guid}")]
    public Task<IActionResult> DeleteCheckpoint(Guid id)
        => Run(() => compliance.DeleteCheckpointAsync(id, UserId), "Checkpoint removed.");

    /// <summary>
    /// Files a reading. A value outside the safe range is refused unless a corrective action
    /// comes with it — an unresolved breach on the record is what an inspection looks for.
    /// </summary>
    [HttpPost("temperature")]
    public Task<IActionResult> RecordTemperature([FromBody] RecordTemperatureDto request)
        => Run(() => compliance.RecordTemperatureAsync(request, UserId), "Reading recorded.");

    [HttpPost("temperature/resolve")]
    public Task<IActionResult> ResolveBreach([FromBody] ResolveBreachDto request)
        => Run(() => compliance.ResolveBreachAsync(request, UserId), "Breach closed.");

    [HttpGet("temperature/{outletId:guid}")]
    public Task<IActionResult> GetLogs(
        Guid outletId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? checkpointId,
        [FromQuery] bool breachesOnly = false)
        => Run(() => compliance.GetLogsAsync(
            outletId,
            from ?? DateTime.UtcNow.Date.AddDays(-30),
            to ?? DateTime.UtcNow,
            checkpointId,
            breachesOnly));

    // ── Checklists ───────────────────────────────────────────────────────────

    [HttpGet("checklists/{outletId:guid}")]
    public Task<IActionResult> GetChecklists(Guid outletId)
        => Run(() => compliance.GetChecklistsAsync(outletId));

    [HttpPost("checklists")]
    public Task<IActionResult> CreateChecklist([FromBody] SaveChecklistDto request)
        => Run(() => compliance.SaveChecklistAsync(null, request, UserId), "Checklist created.");

    [HttpPut("checklists/{id:guid}")]
    public Task<IActionResult> UpdateChecklist(Guid id, [FromBody] SaveChecklistDto request)
        => Run(() => compliance.SaveChecklistAsync(id, request, UserId), "Checklist saved.");

    [HttpDelete("checklists/{id:guid}")]
    public Task<IActionResult> DeleteChecklist(Guid id)
        => Run(() => compliance.DeleteChecklistAsync(id, UserId), "Checklist removed.");

    /// <summary>Signs off a checklist. Refused while a failed critical item has no action against it.</summary>
    [HttpPost("checklists/submit")]
    public Task<IActionResult> SubmitRun([FromBody] SubmitChecklistRunDto request)
        => Run(() => compliance.SubmitRunAsync(request, UserId), "Checklist signed off.");

    [HttpGet("checklists/runs/{outletId:guid}")]
    public Task<IActionResult> GetRuns(Guid outletId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
        => Run(() => compliance.GetRunsAsync(
            outletId,
            from ?? DateTime.UtcNow.Date.AddDays(-30),
            to ?? DateTime.UtcNow));

    // ── Prep batches ─────────────────────────────────────────────────────────

    [HttpGet("batches/{outletId:guid}")]
    public Task<IActionResult> GetBatches(Guid outletId, [FromQuery] bool activeOnly = true)
        => Run(() => compliance.GetBatchesAsync(outletId, activeOnly));

    [HttpPost("batches")]
    public Task<IActionResult> CreateBatch([FromBody] SavePrepBatchDto request)
        => Run(() => compliance.CreateBatchAsync(request, UserId), "Batch labelled.");

    [HttpPost("batches/{id:guid}/discard")]
    public Task<IActionResult> DiscardBatch(Guid id, [FromQuery] string? reason, [FromQuery] Guid? staffId)
        => Run(() => compliance.DiscardBatchAsync(id, reason, staffId, UserId), "Batch discarded.");

    // ── Delivery zones ───────────────────────────────────────────────────────

    [HttpGet("delivery-zones/{outletId:guid}")]
    public Task<IActionResult> GetZones(Guid outletId) => Run(() => compliance.GetZonesAsync(outletId));

    [HttpPost("delivery-zones")]
    public Task<IActionResult> CreateZone([FromBody] DeliveryZoneDto request)
        => Run(() => compliance.SaveZoneAsync(null, request, UserId), "Zone added.");

    [HttpPut("delivery-zones/{id:guid}")]
    public Task<IActionResult> UpdateZone(Guid id, [FromBody] DeliveryZoneDto request)
        => Run(() => compliance.SaveZoneAsync(id, request, UserId), "Zone saved.");

    [HttpDelete("delivery-zones/{id:guid}")]
    public Task<IActionResult> DeleteZone(Guid id)
        => Run(() => compliance.DeleteZoneAsync(id, UserId), "Zone removed.");

    /// <summary>Can we deliver there, and what does it cost — asked before the order is taken.</summary>
    [HttpPost("delivery-quote")]
    public Task<IActionResult> QuoteDelivery([FromBody] DeliveryQuoteRequestDto request)
        => Run(() => compliance.QuoteDeliveryAsync(request));

    // ── Lookups ──────────────────────────────────────────────────────────────

    [HttpGet("lookups/checkpoint-kinds")]
    public IActionResult CheckpointKinds() => Ok(new Nexcore.SharedKernel.Api.ApiResponse<List<LookupItemDto>>
    {
        Success = true,
        Data = Enum.GetValues<TemperatureCheckpointKind>()
            .Select(v => new LookupItemDto { Value = (int)v, Name = v.ToString(), Label = Humanise(v.ToString()) })
            .ToList(),
    });

    [HttpGet("lookups/checklist-frequencies")]
    public IActionResult ChecklistFrequencies() => Ok(new Nexcore.SharedKernel.Api.ApiResponse<List<LookupItemDto>>
    {
        Success = true,
        Data = Enum.GetValues<ChecklistFrequency>()
            .Select(v => new LookupItemDto { Value = (int)v, Name = v.ToString(), Label = Humanise(v.ToString()) })
            .ToList(),
    });

    [HttpGet("lookups/answer-types")]
    public IActionResult AnswerTypes() => Ok(new Nexcore.SharedKernel.Api.ApiResponse<List<LookupItemDto>>
    {
        Success = true,
        Data = Enum.GetValues<ChecklistAnswerType>()
            .Select(v => new LookupItemDto { Value = (int)v, Name = v.ToString(), Label = Humanise(v.ToString()) })
            .ToList(),
    });

    private static string Humanise(string name)
    {
        var spaced = System.Text.RegularExpressions.Regex.Replace(name, "(?<!^)([A-Z])", " $1");
        return char.ToUpperInvariant(spaced[0]) + spaced[1..].ToLowerInvariant();
    }
}
