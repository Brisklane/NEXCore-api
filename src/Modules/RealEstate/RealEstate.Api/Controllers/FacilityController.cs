using Microsoft.AspNetCore.Mvc;
using RealEstate.Application.DTOs;
using RealEstate.Application.Services.Interfaces;
using RealEstate.Domain.Enums;

namespace RealEstate.Api.Controllers;

/// <summary>
/// Work orders, contractors, planned maintenance, assets, inspections, meters, utilities, fuel and
/// parking.
///
/// Meter reading is the quiet source of most utility disputes, so a reading that looks impossible
/// is flagged and — deliberately — does not advance the meter. A bad reading accepted silently
/// corrupts the next twelve bills, and by the time somebody notices, the money has been collected.
/// </summary>
[Route("api/realestate/facility")]
public class FacilityController(
    IFacilityService facility,
    ILogger<FacilityController> logger) : RealEstateControllerBase(logger)
{
    // ── Work orders ──────────────────────────────────────────────────────────

    [HttpPost("work-orders/search")]
    public Task<IActionResult> SearchWorkOrders([FromBody] WorkOrderSearchDto query)
        => RunPaged(() => facility.GetWorkOrdersAsync(query));

    [HttpGet("work-orders/{id:guid}")]
    public Task<IActionResult> GetWorkOrder(Guid id)
        => RunFound(() => facility.GetWorkOrderAsync(id), "That work order does not exist.");

    [HttpPost("work-orders")]
    public Task<IActionResult> CreateWorkOrder([FromBody] WorkOrderCreateDto request)
        => Run(() => facility.CreateWorkOrderAsync(request, UserId), "Work order raised.");

    [HttpPost("work-orders/{id:guid}/assign")]
    public Task<IActionResult> AssignWorkOrder(
        Guid id,
        [FromQuery] Guid? assignToUserId,
        [FromQuery] Guid? contractorId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
        => Run(() => facility.AssignWorkOrderAsync(id, assignToUserId, contractorId, from, to, UserId),
            "Assigned.");

    /// <summary>
    /// Authorises spend above the landlord's own repair limit. Below it, no approval is needed —
    /// asking a landlord to sign off a washer costs more than the washer.
    /// </summary>
    [HttpPost("work-orders/{id:guid}/authorise")]
    public Task<IActionResult> AuthoriseWorkOrder(
        Guid id, [FromQuery] ApprovalOutcome outcome, [FromQuery] string? comment)
        => Run(() => facility.AuthoriseWorkOrderAsync(id, outcome, comment, UserId), "Decision recorded.");

    [HttpPost("work-orders/complete")]
    public Task<IActionResult> CompleteWorkOrder([FromBody] WorkOrderCompletionDto request)
        => Run(() => facility.CompleteWorkOrderAsync(request, UserId), "Work order completed.");

    [HttpPost("work-orders/{id:guid}/cancel")]
    public Task<IActionResult> CancelWorkOrder(Guid id, [FromQuery] Guid reasonCodeId)
        => Run(() => facility.CancelWorkOrderAsync(id, reasonCodeId, UserId), "Work order cancelled.");

    [HttpPost("work-orders/flag-sla")]
    public Task<IActionResult> FlagSla() => Run(facility.FlagSlaBreachesAsync, "Breaches flagged.");

    // ── Contractors ──────────────────────────────────────────────────────────

    [HttpGet("contractors")]
    public Task<IActionResult> GetContractors(
        [FromQuery] ListQueryDto query, [FromQuery] string? trade, [FromQuery] bool? approvedOnly)
        => RunPaged(() => facility.GetContractorsAsync(query, trade, approvedOnly));

    [HttpGet("contractors/{id:guid}")]
    public Task<IActionResult> GetContractor(Guid id)
        => RunFound(() => facility.GetContractorAsync(id), "That contractor does not exist.");

    [HttpPost("contractors")]
    public Task<IActionResult> SaveContractor([FromBody] ContractorDetailDto request)
        => Run(() => facility.SaveContractorAsync(request, UserId), "Contractor saved.");

    /// <summary>
    /// Insurance, licences and certifications. A contractor whose public liability has lapsed
    /// cannot be sent to a job, and this is where that is known.
    /// </summary>
    [HttpPost("contractors/{id:guid}/compliance")]
    public Task<IActionResult> SaveCompliance(Guid id, [FromBody] ContractorComplianceDto request)
        => Run(() => facility.SaveContractorComplianceAsync(id, request, UserId), "Compliance saved.");

    // ── Planned maintenance ──────────────────────────────────────────────────

    [HttpGet("ppm/schedules")]
    public Task<IActionResult> GetPpmSchedules(
        [FromQuery] ListQueryDto query, [FromQuery] Guid? propertyId, [FromQuery] bool? overdueOnly)
        => RunPaged(() => facility.GetPpmSchedulesAsync(query, propertyId, overdueOnly));

    [HttpPost("ppm/schedules")]
    public Task<IActionResult> SavePpmSchedule([FromBody] PpmScheduleDto request)
        => Run(() => facility.SavePpmScheduleAsync(request, UserId), "Schedule saved.");

    [HttpGet("ppm/tasks")]
    public Task<IActionResult> GetPpmTasks([FromQuery] ListQueryDto query, [FromQuery] string? status)
        => RunPaged(() => facility.GetPpmTasksAsync(query, status));

    [HttpPost("ppm/generate")]
    public Task<IActionResult> GeneratePpm([FromQuery] DateOnly? asOf)
        => Run(() => facility.GeneratePpmTasksAsync(asOf ?? DateOnly.FromDateTime(DateTime.UtcNow)),
            "Tasks generated.");

    [HttpPost("ppm/tasks/{id:guid}/complete")]
    public Task<IActionResult> CompletePpmTask(
        Guid id,
        [FromQuery] DateOnly completedOn,
        [FromQuery] decimal? cost,
        [FromQuery] string? note,
        [FromQuery] string? certificateUrl)
        => Run(() => facility.CompletePpmTaskAsync(id, completedOn, cost, note, certificateUrl, UserId),
            "Task completed.");

    // ── Assets ───────────────────────────────────────────────────────────────

    [HttpGet("assets")]
    public Task<IActionResult> GetAssets(
        [FromQuery] ListQueryDto query, [FromQuery] Guid? propertyId, [FromQuery] AssetKind? kind)
        => RunPaged(() => facility.GetAssetsAsync(query, propertyId, kind));

    [HttpGet("assets/{id:guid}")]
    public Task<IActionResult> GetAsset(Guid id)
        => RunFound(() => facility.GetAssetAsync(id), "That asset does not exist.");

    [HttpPost("assets")]
    public Task<IActionResult> SaveAsset([FromBody] FacilityAssetDto request)
        => Run(() => facility.SaveAssetAsync(request, UserId), "Asset saved.");

    [HttpPost("assets/{id:guid}/service")]
    public Task<IActionResult> RecordService(Guid id, [FromBody] AssetServiceRecordDto request)
        => Run(() => facility.RecordServiceAsync(id, request, UserId), "Service recorded.");

    [HttpGet("service-contracts")]
    public Task<IActionResult> GetServiceContracts([FromQuery] Guid? propertyId, [FromQuery] bool expiringOnly = false)
        => Run(() => facility.GetServiceContractsAsync(propertyId, expiringOnly));

    [HttpPost("service-contracts")]
    public Task<IActionResult> SaveServiceContract([FromBody] ServiceContractDto request)
        => Run(() => facility.SaveServiceContractAsync(request, UserId), "Contract saved.");

    [HttpPost("inspection-rounds")]
    public Task<IActionResult> SaveInspectionRound([FromBody] InspectionRoundDto request)
        => Run(() => facility.SaveInspectionRoundAsync(request, UserId), "Round saved.");

    [HttpGet("inspection-rounds")]
    public Task<IActionResult> GetInspectionRounds([FromQuery] ListQueryDto query, [FromQuery] Guid? propertyId)
        => RunPaged(() => facility.GetInspectionRoundsAsync(query, propertyId));

    // ── Meters ───────────────────────────────────────────────────────────────

    [HttpGet("meters")]
    public Task<IActionResult> GetMeters(
        [FromQuery] ListQueryDto query,
        [FromQuery] Guid? propertyId,
        [FromQuery] Guid? societyId,
        [FromQuery] MeterKind? kind)
        => RunPaged(() => facility.GetMetersAsync(query, propertyId, societyId, kind));

    [HttpPost("meters")]
    public Task<IActionResult> SaveMeter([FromBody] MeterDto request)
        => Run(() => facility.SaveMeterAsync(request, UserId), "Meter saved.");

    /// <summary>The round a meter reader walks, in order, with the last reading to compare against.</summary>
    [HttpGet("meters/round")]
    public Task<IActionResult> GetReadingRound(
        [FromQuery] Guid? societyId, [FromQuery] Guid? propertyId, [FromQuery] MeterKind? kind)
        => Run(() => facility.GetReadingRoundAsync(societyId, propertyId, kind));

    [HttpPost("meters/readings")]
    public Task<IActionResult> SubmitReadings([FromBody] MeterReadingBatchDto batch)
        => Run(() => facility.SubmitReadingsAsync(batch, UserId), "Readings submitted.");

    [HttpGet("meters/readings")]
    public Task<IActionResult> GetReadings(
        [FromQuery] ListQueryDto query, [FromQuery] Guid? meterId, [FromQuery] bool? implausibleOnly)
        => RunPaged(() => facility.GetReadingsAsync(query, meterId, implausibleOnly));

    [HttpPost("meters/readings/{id:guid}/verify")]
    public Task<IActionResult> VerifyReading(Guid id, [FromQuery] decimal? correctedValue)
        => Run(() => facility.VerifyReadingAsync(id, correctedValue, UserId), "Reading verified.");

    // ── Utilities ────────────────────────────────────────────────────────────

    [HttpGet("tariffs")]
    public Task<IActionResult> GetTariffs([FromQuery] Guid? societyId, [FromQuery] MeterKind? kind)
        => Run(() => facility.GetTariffsAsync(societyId, kind));

    [HttpPost("tariffs")]
    public Task<IActionResult> SaveTariff([FromBody] UtilityTariffDto request)
        => Run(() => facility.SaveTariffAsync(request, UserId), "Tariff saved.");

    [HttpPost("utility-bills/generate")]
    public Task<IActionResult> GenerateUtilityBills(
        [FromQuery] Guid? societyId,
        [FromQuery] DateOnly periodFrom,
        [FromQuery] DateOnly periodTo,
        [FromQuery] bool dryRun = true)
        => Run(() => facility.GenerateUtilityBillsAsync(societyId, periodFrom, periodTo, dryRun, UserId));

    [HttpGet("utility-bills")]
    public Task<IActionResult> GetUtilityBills([FromQuery] ListQueryDto query, [FromQuery] Guid? unitId)
        => RunPaged(() => facility.GetUtilityBillsAsync(query, unitId));

    /// <summary>
    /// Bulk supply against the sum of the sub-meters, separating common-area use from unexplained
    /// loss. The second number is theft or leakage, and it is the only one worth chasing.
    /// </summary>
    [HttpGet("utility-bills/reconcile")]
    public Task<IActionResult> ReconcileUtility(
        [FromQuery] Guid? societyId,
        [FromQuery] Guid? propertyId,
        [FromQuery] MeterKind kind,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to)
        => Run(() => facility.ReconcileUtilityAsync(societyId, propertyId, kind, from, to));

    [HttpPost("fuel-logs")]
    public Task<IActionResult> SaveFuelLog([FromBody] FuelLogDto request)
        => Run(() => facility.SaveFuelLogAsync(request, UserId), "Log saved.");

    [HttpGet("fuel-logs")]
    public Task<IActionResult> GetFuelLogs([FromQuery] ListQueryDto query, [FromQuery] Guid? societyId)
        => RunPaged(() => facility.GetFuelLogsAsync(query, societyId));

    // ── Parking ──────────────────────────────────────────────────────────────

    [HttpGet("parking")]
    public Task<IActionResult> GetParking(
        [FromQuery] ListQueryDto query, [FromQuery] Guid? societyId, [FromQuery] bool? unallottedOnly)
        => RunPaged(() => facility.GetParkingAsync(query, societyId, unallottedOnly));

    [HttpPost("parking")]
    public Task<IActionResult> SaveParkingSlot([FromBody] ParkingSlotDto request)
        => Run(() => facility.SaveParkingSlotAsync(request, UserId), "Slot saved.");

    [HttpPost("parking/allot")]
    public Task<IActionResult> AllotParking([FromBody] ParkingAllotmentDto request)
        => Run(() => facility.AllotParkingAsync(request, UserId), "Slot allotted.");
}
