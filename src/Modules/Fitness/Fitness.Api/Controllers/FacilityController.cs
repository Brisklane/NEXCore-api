using Fitness.Application.DTOs;
using Fitness.Application.Services.Interfaces;
using Fitness.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Nexcore.SharedKernel.Api;

namespace Fitness.Api.Controllers;

/// <summary>
/// The building: lockers, courts and other bookable resources, equipment and maintenance.
///
/// Taking a machine out of service is not just a status change. It propagates — the spot on the
/// rig map goes dark, the resource stops being bookable, and anyone already booked onto it is
/// moved and told. A broken treadmill that is still showing as bookable is how a member's evening
/// gets wasted.
/// </summary>
[Route("api/fitness/facilities")]
public class FacilityController(
    IFacilityService facilities,
    ILogger<FacilityController> logger) : FitnessControllerBase(logger)
{
    // ── Lockers ──────────────────────────────────────────────────────────────

    [HttpGet("locker-banks/{clubId:guid}")]
    public Task<IActionResult> GetLockerBanks(Guid clubId)
        => Run(() => facilities.GetLockerBanksAsync(clubId));

    [HttpPost("locker-banks")]
    public Task<IActionResult> SaveLockerBank([FromBody] LockerBankDto request, [FromQuery] Guid? id = null)
        => Run(() => facilities.SaveLockerBankAsync(id, request, UserId), "Locker bank saved.");

    [HttpPost("lockers/assign")]
    public Task<IActionResult> AssignLocker([FromBody] AssignLockerDto request)
        => Run(() => facilities.AssignLockerAsync(request, UserId), "Locker assigned.");

    [HttpPost("lockers/release")]
    public Task<IActionResult> ReleaseLocker([FromBody] ReleaseLockerDto request)
        => Run(() => facilities.ReleaseLockerAsync(request, UserId), "Locker released.");

    [HttpGet("lockers/assignments")]
    public Task<IActionResult> GetLockerAssignments(
        [FromQuery] Guid? clubId, [FromQuery] Guid? memberId, [FromQuery] bool activeOnly = true,
        [FromQuery] PaginationParams? pagination = null)
        => RunPaged(() => facilities.GetLockerAssignmentsAsync(clubId, memberId, activeOnly,
            pagination ?? new PaginationParams()));

    /// <summary>Nightly job: clears day-use lockers and flags rentals that have expired.</summary>
    [HttpPost("lockers/sweep")]
    public Task<IActionResult> SweepLockers()
        => Run(facilities.SweepLockersAsync);

    // ── Courts and resources ─────────────────────────────────────────────────

    [HttpGet("resources")]
    public Task<IActionResult> GetResources(
        [FromQuery] Guid clubId, [FromQuery] ResourceKind? kind, [FromQuery] bool activeOnly = true)
        => Run(() => facilities.GetResourcesAsync(clubId, kind, activeOnly));

    [HttpPost("resources")]
    public Task<IActionResult> SaveResource([FromBody] BookableResourceDto request, [FromQuery] Guid? id = null)
        => Run(() => facilities.SaveResourceAsync(id, request, UserId), "Resource saved.");

    /// <summary>The court grid for a day — every resource down one axis, time along the other.</summary>
    [HttpGet("resources/grid")]
    public Task<IActionResult> GetGrid(
        [FromQuery] Guid clubId, [FromQuery] DateTime forDate, [FromQuery] ResourceKind? kind)
        => Run(() => facilities.GetGridAsync(clubId, forDate, kind));

    [HttpPost("resources/bookings")]
    public Task<IActionResult> BookResource([FromBody] CreateResourceBookingDto request)
        => Run(() => facilities.BookResourceAsync(request, UserId), "Booked.");

    [HttpPost("resources/bookings/{id:guid}/cancel")]
    public Task<IActionResult> CancelResourceBooking(
        Guid id, [FromQuery] string? reason, [FromQuery] bool waivePenalty = false)
        => Run(() => facilities.CancelResourceBookingAsync(id, reason, waivePenalty, UserId), "Cancelled.");

    [HttpPost("resources/bookings/{id:guid}/check-in")]
    public Task<IActionResult> CheckInResourceBooking(Guid id)
        => Run(() => facilities.CheckInResourceBookingAsync(id, UserId), "Checked in.");

    [HttpGet("resources/bookings")]
    public Task<IActionResult> ListResourceBookings(
        [FromQuery] Guid? clubId, [FromQuery] Guid? memberId, [FromQuery] Guid? resourceId,
        [FromQuery] DateTime? from, [FromQuery] DateTime? to,
        [FromQuery] PaginationParams? pagination = null)
        => RunPaged(() => facilities.ListResourceBookingsAsync(clubId, memberId, resourceId, from, to,
            pagination ?? new PaginationParams()));

    // ── Equipment ────────────────────────────────────────────────────────────

    [HttpGet("equipment")]
    public Task<IActionResult> GetEquipment(
        [FromQuery] Guid? clubId, [FromQuery] AssetStatus? status, [FromQuery] string? category,
        [FromQuery] string? search, [FromQuery] PaginationParams? pagination = null)
        => RunPaged(() => facilities.GetEquipmentAsync(clubId, status, category, search,
            pagination ?? new PaginationParams()));

    [HttpGet("equipment/{id:guid}")]
    public Task<IActionResult> GetAsset(Guid id)
        => RunFound(() => facilities.GetAssetAsync(id), "Asset not found.");

    /// <summary>
    /// Looks up a machine from the QR sticker on it.
    ///
    /// The point of the sticker is that a member who finds a broken machine can report it in ten
    /// seconds without finding a member of staff — which is the difference between a fault report
    /// and a bad review.
    /// </summary>
    [HttpGet("equipment/qr/{qrCode}")]
    public Task<IActionResult> GetAssetByQr(string qrCode)
        => RunFound(() => facilities.GetAssetByQrAsync(qrCode), "That code was not recognised.");

    [HttpPost("equipment")]
    public Task<IActionResult> SaveAsset([FromBody] SaveEquipmentDto request, [FromQuery] Guid? id = null)
        => Run(() => facilities.SaveAssetAsync(id, request, UserId), "Asset saved.");

    /// <summary>Changes a machine's status, and propagates it to spot maps, resources and bookings.</summary>
    [HttpPost("equipment/{id:guid}/status")]
    public Task<IActionResult> SetAssetStatus(
        Guid id, [FromQuery] AssetStatus status, [FromQuery] string? note)
        => Run(() => facilities.SetAssetStatusAsync(id, status, note, UserId), "Status updated.");

    [HttpPost("equipment/{id:guid}/usage")]
    public Task<IActionResult> RecordUsage(
        Guid id, [FromQuery] decimal cumulativeHours, [FromQuery] string? source)
        => Run(() => facilities.RecordUsageAsync(id, cumulativeHours, source, UserId), "Usage recorded.");

    // ── Maintenance ──────────────────────────────────────────────────────────

    [HttpGet("maintenance")]
    public Task<IActionResult> GetMaintenanceSchedules([FromQuery] Guid? clubId, [FromQuery] bool dueOnly = false)
        => Run(() => facilities.GetMaintenanceSchedulesAsync(clubId, dueOnly));

    [HttpPost("maintenance")]
    public Task<IActionResult> SaveMaintenanceSchedule(
        [FromBody] MaintenanceScheduleDto request, [FromQuery] Guid? id = null)
        => Run(() => facilities.SaveMaintenanceScheduleAsync(id, request, UserId), "Schedule saved.");

    /// <summary>Nightly job: raises work orders for servicing that has fallen due, by date or by hours run.</summary>
    [HttpPost("maintenance/generate-due")]
    public Task<IActionResult> GenerateDueWorkOrders()
        => Run(facilities.GenerateDueWorkOrdersAsync);

    [HttpGet("work-orders")]
    public Task<IActionResult> GetWorkOrders(
        [FromQuery] Guid? clubId, [FromQuery] WorkOrderStatus? status,
        [FromQuery] WorkOrderPriority? priority, [FromQuery] Guid? assignedStaffId,
        [FromQuery] PaginationParams? pagination = null)
        => RunPaged(() => facilities.GetWorkOrdersAsync(clubId, status, priority, assignedStaffId,
            pagination ?? new PaginationParams()));

    [HttpPost("work-orders")]
    public Task<IActionResult> SaveWorkOrder([FromBody] SaveWorkOrderDto request, [FromQuery] Guid? id = null)
        => Run(() => facilities.SaveWorkOrderAsync(id, request, UserId), "Work order saved.");

    [HttpPost("work-orders/complete")]
    public Task<IActionResult> CompleteWorkOrder([FromBody] CompleteWorkOrderDto request)
        => Run(() => facilities.CompleteWorkOrderAsync(request, UserId), "Work order completed.");

    [HttpPost("faults")]
    public Task<IActionResult> ReportFault([FromBody] ReportFaultDto request)
        => Run(() => facilities.ReportFaultAsync(request, UserId), "Thanks — we'll look at it.");

    [HttpGet("faults")]
    public Task<IActionResult> GetFaults([FromQuery] Guid? clubId, [FromQuery] bool openOnly = true)
        => Run(() => facilities.GetFaultsAsync(clubId, openOnly));
}
