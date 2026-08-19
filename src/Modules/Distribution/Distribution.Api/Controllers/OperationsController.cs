using Distribution.Application.DTOs;
using Distribution.Application.Services.Interfaces;
using Distribution.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;
using DomainReturnStatus = Distribution.Domain.Enums.ReturnStatus;

namespace Distribution.Api.Controllers;

/// <summary>Warehouse outbound: waves, picking, packing and dispatch.</summary>
[Route("api/distribution/fulfilment")]
public class FulfilmentController(IFulfilmentService service, ILogger<FulfilmentController> logger)
    : DistributionControllerBase(logger)
{
    /// <summary>The dispatch desk's live board: what is waiting, picking, staged and gone.</summary>
    [HttpGet("board")]
    public Task<IActionResult> Board([FromQuery] Guid? warehouseId, [FromQuery] DateTime? date)
        => Run(() => service.GetBoardAsync(warehouseId, date));

    [HttpPost("waves")]
    public Task<IActionResult> CreateWave([FromBody] CreatePickWaveDto request)
        => Run(() => service.CreateWaveAsync(request, UserId), "Wave released.");

    [HttpGet("waves/{id:guid}")]
    public Task<IActionResult> GetWave(Guid id)
        => RunFound(() => service.GetWaveAsync(id), "That wave no longer exists.");

    [HttpGet("waves")]
    public Task<IActionResult> ListWaves(
        [FromQuery] Guid? warehouseId, [FromQuery] bool? openOnly, [FromQuery] DateTime? from,
        [FromQuery] DateTime? to, [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListWavesAsync(
            warehouseId, openOnly, from, to, pagination ?? new PaginationParams()));

    [HttpPost("waves/{id:guid}/close")]
    public Task<IActionResult> CloseWave(Guid id)
        => Run(() => service.CloseWaveAsync(id, UserId), "Wave closed.");

    [HttpGet("tasks/{id:guid}")]
    public Task<IActionResult> GetTask(Guid id)
        => RunFound(() => service.GetTaskAsync(id), "That pick task no longer exists.");

    [HttpPost("tasks/{id:guid}/assign")]
    public Task<IActionResult> AssignTask(Guid id, [FromQuery] Guid userId, [FromQuery] string? name)
        => Run(() => service.AssignTaskAsync(id, userId, name, UserId), "Task assigned.");

    [HttpPost("tasks/{id:guid}/start")]
    public Task<IActionResult> StartTask(Guid id)
        => Run(() => service.StartTaskAsync(id, UserId), "Picking started.");

    /// <summary>Confirms one pick. A lot other than the FEFO nomination requires a reason.</summary>
    [HttpPost("tasks/confirm")]
    public Task<IActionResult> ConfirmPick([FromBody] ConfirmPickDto request)
        => Run(() => service.ConfirmPickAsync(request, UserId), "Pick confirmed.");

    [HttpPost("tasks/{id:guid}/complete")]
    public Task<IActionResult> CompleteTask(Guid id)
        => Run(() => service.CompleteTaskAsync(id, UserId), "Task completed.");

    [HttpPost("packages")]
    public Task<IActionResult> CreatePackage([FromBody] CreatePackageDto request)
        => Run(() => service.CreatePackageAsync(request, UserId), "Package created.");

    [HttpGet("packages")]
    public Task<IActionResult> Packages(
        [FromQuery] Guid? orderId, [FromQuery] Guid? tripId, [FromQuery] Guid? dispatchId)
        => Run(() => service.ListPackagesAsync(orderId, tripId, dispatchId));

    [HttpPost("packages/{id:guid}/stage")]
    public Task<IActionResult> Stage(Guid id, [FromQuery] string stagingLocation)
        => Run(() => service.StagePackageAsync(id, stagingLocation, UserId), "Package staged.");

    /// <summary>Scans a carton onto a vehicle. Refuses one that belongs to another trip.</summary>
    [HttpPost("packages/{id:guid}/load")]
    public Task<IActionResult> Load(Guid id, [FromQuery] Guid tripId)
        => Run(() => service.LoadPackageAsync(id, tripId, UserId), "Package loaded.");

    [HttpPost("dispatches")]
    public Task<IActionResult> CreateDispatch([FromBody] CreateDispatchDto request)
        => Run(() => service.CreateDispatchAsync(request, UserId), "Dispatched.");

    [HttpGet("dispatches/{id:guid}")]
    public Task<IActionResult> GetDispatch(Guid id)
        => RunFound(() => service.GetDispatchAsync(id), "That dispatch no longer exists.");

    [HttpGet("dispatches")]
    public Task<IActionResult> ListDispatches(
        [FromQuery] Guid? warehouseId, [FromQuery] Guid? tripId, [FromQuery] DateTime? from,
        [FromQuery] DateTime? to, [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListDispatchesAsync(
            warehouseId, tripId, from, to, pagination ?? new PaginationParams()));
}

/// <summary>Fleet, trips and proof of delivery.</summary>
[Route("api/distribution/logistics")]
public class LogisticsController(ILogisticsService service, ILogger<LogisticsController> logger)
    : DistributionControllerBase(logger)
{
    // ── Fleet ────────────────────────────────────────────────────────────────

    [HttpGet("vehicles")]
    public Task<IActionResult> Vehicles(
        [FromQuery] string? search, [FromQuery] VehicleKind? kind,
        [FromQuery] bool? expiringComplianceOnly, [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListVehiclesAsync(
            search, kind, expiringComplianceOnly, pagination ?? new PaginationParams()));

    [HttpGet("vehicles/{id:guid}")]
    public Task<IActionResult> GetVehicle(Guid id)
        => RunFound(() => service.GetVehicleAsync(id), "That vehicle no longer exists.");

    [HttpPost("vehicles")]
    public Task<IActionResult> CreateVehicle([FromBody] SaveVehicleDto request)
        => Run(() => service.SaveVehicleAsync(null, request, UserId), "Vehicle created.");

    [HttpPut("vehicles/{id:guid}")]
    public Task<IActionResult> UpdateVehicle(Guid id, [FromBody] SaveVehicleDto request)
        => Run(() => service.SaveVehicleAsync(id, request, UserId), "Vehicle saved.");

    [HttpDelete("vehicles/{id:guid}")]
    public Task<IActionResult> DeleteVehicle(Guid id)
        => Run(() => service.DeleteVehicleAsync(id, UserId), "Vehicle removed.");

    [HttpGet("compliance/expiring")]
    public Task<IActionResult> ExpiringCompliance([FromQuery] int withinDays = 30)
        => Run(() => service.GetExpiringComplianceAsync(withinDays));

    [HttpGet("drivers")]
    public Task<IActionResult> Drivers([FromQuery] string? search, [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListDriversAsync(search, pagination ?? new PaginationParams()));

    [HttpGet("drivers/{id:guid}")]
    public Task<IActionResult> GetDriver(Guid id)
        => RunFound(() => service.GetDriverAsync(id), "That driver no longer exists.");

    [HttpPost("drivers")]
    public Task<IActionResult> CreateDriver([FromBody] SaveDriverDto request)
        => Run(() => service.SaveDriverAsync(null, request, UserId), "Driver created.");

    [HttpPut("drivers/{id:guid}")]
    public Task<IActionResult> UpdateDriver(Guid id, [FromBody] SaveDriverDto request)
        => Run(() => service.SaveDriverAsync(id, request, UserId), "Driver saved.");

    [HttpDelete("drivers/{id:guid}")]
    public Task<IActionResult> DeleteDriver(Guid id)
        => Run(() => service.DeleteDriverAsync(id, UserId), "Driver removed.");

    // ── Trips ────────────────────────────────────────────────────────────────

    [HttpPost("trips")]
    public Task<IActionResult> CreateTrip([FromBody] CreateTripDto request)
        => Run(() => service.CreateTripAsync(request, UserId), "Trip planned.");

    [HttpGet("trips/{id:guid}")]
    public Task<IActionResult> GetTrip(Guid id)
        => RunFound(() => service.GetTripAsync(id), "That trip no longer exists.");

    [HttpGet("trips")]
    public Task<IActionResult> ListTrips(
        [FromQuery] Guid? vehicleId, [FromQuery] Guid? driverId, [FromQuery] Guid? routeId,
        [FromQuery] TripStatus? status, [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListTripsAsync(
            vehicleId, driverId, routeId, status, from, to, pagination ?? new PaginationParams()));

    [HttpPost("trips/resequence")]
    public Task<IActionResult> ResequenceTrip([FromBody] ResequenceTripDto request)
        => Run(() => service.ResequenceTripAsync(request, UserId), "Stops resequenced.");

    [HttpPost("trips/start")]
    public Task<IActionResult> StartTrip([FromBody] StartTripDto request)
        => Run(() => service.StartTripAsync(request, UserId), "Trip started.");

    [HttpPost("trips/end")]
    public Task<IActionResult> EndTrip([FromBody] EndTripDto request)
        => Run(() => service.EndTripAsync(request, UserId), "Trip closed.");

    [HttpPost("trips/{id:guid}/cancel")]
    public Task<IActionResult> CancelTrip(Guid id, [FromQuery] string reason)
        => Run(() => service.CancelTripAsync(id, reason, UserId), "Trip cancelled.");

    [HttpPost("stops/arrive")]
    public Task<IActionResult> Arrive([FromBody] ArriveAtStopDto request)
        => Run(() => service.ArriveAsync(request, UserId), "Arrival recorded.");

    [HttpPost("stops/fail")]
    public Task<IActionResult> FailStop([FromBody] FailStopDto request)
        => Run(() => service.FailStopAsync(request, UserId), "Outcome recorded.");

    [HttpPost("expenses")]
    public Task<IActionResult> AddExpense([FromBody] SaveTripExpenseDto request)
        => Run(() => service.AddExpenseAsync(request, UserId), "Expense recorded.");

    [HttpPost("expenses/{id:guid}/decide")]
    public Task<IActionResult> DecideExpense(
        Guid id, [FromQuery] bool isApproved = true, [FromQuery] string? reason = null)
        => Run(() => service.DecideExpenseAsync(id, isApproved, reason, UserId), "Decision recorded.");

    // ── Proof of delivery ────────────────────────────────────────────────────

    /// <summary>The doorstep record, and — when lines fall short — the credit note with it.</summary>
    [HttpPost("pod")]
    public Task<IActionResult> CapturePod([FromBody] CapturePodDto request)
        => Run(() => service.CapturePodAsync(request, UserId), "Delivery recorded.");

    [HttpGet("pod/{id:guid}")]
    public Task<IActionResult> GetPod(Guid id)
        => RunFound(() => service.GetPodAsync(id), "That delivery record no longer exists.");

    [HttpGet("pod")]
    public Task<IActionResult> ListPods(
        [FromQuery] Guid? tripId, [FromQuery] Guid? outletId, [FromQuery] bool? exceptionsOnly,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListPodsAsync(
            tripId, outletId, exceptionsOnly, from, to, pagination ?? new PaginationParams()));

    [HttpPost("pod/{id:guid}/resolve")]
    public Task<IActionResult> ResolvePod(Guid id, [FromQuery] string note)
        => Run(() => service.ResolvePodExceptionAsync(id, note, UserId), "Exception closed.");
}

/// <summary>Returns: authorisation, receipt, inspection, disposition and credit.</summary>
[Route("api/distribution/returns")]
public class ReturnController(IReturnService service, ILogger<ReturnController> logger)
    : DistributionControllerBase(logger)
{
    [HttpPost]
    public Task<IActionResult> Request([FromBody] RequestReturnDto request)
        => Run(() => service.RequestAsync(request, UserId), "Return raised.");

    [HttpGet("{id:guid}")]
    public Task<IActionResult> Get(Guid id)
        => RunFound(() => service.GetAsync(id), "That return no longer exists.");

    [HttpGet]
    public Task<IActionResult> List(
        [FromQuery] string? search, [FromQuery] ReturnKind? kind, [FromQuery] DomainReturnStatus? status,
        [FromQuery] Guid? outletId, [FromQuery] Guid? partnerId, [FromQuery] DateTime? from,
        [FromQuery] DateTime? to, [FromQuery] PaginationParams? pagination)
        => RunPaged(() => service.ListAsync(
            search, kind, status, outletId, partnerId, from, to, pagination ?? new PaginationParams()));

    [HttpPost("{id:guid}/decide")]
    public Task<IActionResult> Decide(Guid id, [FromBody] DecideReturnDto request)
        => Run(() => service.DecideAsync(id, request, UserId), "Decision recorded.");

    [HttpPost("{id:guid}/cancel")]
    public Task<IActionResult> Cancel(Guid id, [FromQuery] string reason)
        => Run(() => service.CancelAsync(id, reason, UserId), "Return cancelled.");

    [HttpPost("receive")]
    public Task<IActionResult> Receive([FromBody] ReceiveReturnDto request)
        => Run(() => service.ReceiveAsync(request, UserId), "Goods received.");

    [HttpPost("disposition")]
    public Task<IActionResult> Disposition([FromBody] DispositionReturnDto request)
        => Run(() => service.DispositionAsync(request, UserId), "Disposition recorded.");

    /// <summary>Records destruction of expired goods, which regulated categories require on paper.</summary>
    [HttpPost("receipts/{id:guid}/destruction")]
    public Task<IActionResult> RecordDestruction(
        Guid id, [FromQuery] string certificateNumber,
        [FromQuery] string? certificateUrl, [FromQuery] DateTime destroyedOn)
        => Run(() => service.RecordDestructionAsync(id, certificateNumber, certificateUrl, destroyedOn, UserId),
            "Destruction recorded.");

    [HttpPost("{id:guid}/credit")]
    public Task<IActionResult> Credit(Guid id, [FromQuery] bool raiseClaim = true)
        => Run(() => service.CreditAsync(id, raiseClaim, UserId), "Credit raised.");
}
