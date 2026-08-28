using Microsoft.AspNetCore.Mvc;
using RealEstate.Application.DTOs;
using RealEstate.Application.Services.Interfaces;
using RealEstate.Domain.Enums;

namespace RealEstate.Api.Controllers;

/// <summary>
/// Societies and gated communities: residents, maintenance billing, the gate, amenities,
/// complaints, notices, polls and building control.
///
/// The gate is the part that has to work when nothing else does. Entries can be recorded offline
/// and replayed later against a client reference, so a guard whose tablet lost signal for two
/// hours does not create two hundred duplicate entries when it comes back.
/// </summary>
[Route("api/realestate/societies")]
public class SocietyController(
    ISocietyService societies,
    ILogger<SocietyController> logger) : RealEstateControllerBase(logger)
{
    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] ListQueryDto query)
        => RunPaged(() => societies.GetSocietiesAsync(query));

    [HttpGet("{id:guid}")]
    public Task<IActionResult> Get(Guid id)
        => RunFound(() => societies.GetSocietyAsync(id), "That society does not exist.");

    [HttpPost]
    public Task<IActionResult> Save([FromBody] SocietyUpsertDto request)
        => Run(() => societies.SaveSocietyAsync(request, UserId), "Society saved.");

    [HttpGet("{id:guid}/dashboard")]
    public Task<IActionResult> GetDashboard(Guid id)
        => Run(() => societies.GetDashboardAsync(id));

    /// <summary>
    /// Hands the society from the developer to its own committee, transferring the corpus. A
    /// one-way step, and the one every resident association eventually asks about.
    /// </summary>
    [HttpPost("{id:guid}/handover")]
    public Task<IActionResult> HandOver(
        Guid id, [FromQuery] DateOnly handoverDate, [FromQuery] decimal corpusTransferred)
        => Run(() => societies.HandOverFromDeveloperAsync(id, handoverDate, corpusTransferred, UserId),
            "Society handed over.");

    // ── Committee and residents ──────────────────────────────────────────────

    [HttpGet("{id:guid}/committee")]
    public Task<IActionResult> GetCommittee(Guid id) => Run(() => societies.GetCommitteeAsync(id));

    [HttpPost("{id:guid}/committee")]
    public Task<IActionResult> SaveCommitteeMember(Guid id, [FromBody] CommitteeMemberDto request)
        => Run(() => societies.SaveCommitteeMemberAsync(id, request, UserId), "Member saved.");

    [HttpGet("{id:guid}/residents")]
    public Task<IActionResult> GetResidents(
        Guid id,
        [FromQuery] ListQueryDto query,
        [FromQuery] ResidentKind? kind,
        [FromQuery] bool? defaultersOnly)
        => RunPaged(() => societies.GetResidentsAsync(query, id, kind, defaultersOnly));

    [HttpGet("residents/{id:guid}")]
    public Task<IActionResult> GetResident(Guid id)
        => RunFound(() => societies.GetResidentAsync(id), "That resident does not exist.");

    [HttpPost("residents")]
    public Task<IActionResult> SaveResident([FromBody] ResidentUpsertDto request)
        => Run(() => societies.SaveResidentAsync(request, UserId), "Resident saved.");

    [HttpPost("residents/{id:guid}/move-out")]
    public Task<IActionResult> MoveOut(Guid id, [FromQuery] DateOnly movedOutOn)
        => Run(() => societies.MoveOutAsync(id, movedOutOn, UserId), "Move-out recorded.");

    [HttpPost("residents/{id:guid}/vehicles")]
    public Task<IActionResult> SaveVehicle(Guid id, [FromBody] ResidentVehicleDto request)
        => Run(() => societies.SaveVehicleAsync(id, request, UserId), "Vehicle saved.");

    [HttpGet("{id:guid}/staff")]
    public Task<IActionResult> GetStaff(Guid id, [FromQuery] ListQueryDto query)
        => RunPaged(() => societies.GetStaffAsync(query, id));

    [HttpPost("staff")]
    public Task<IActionResult> SaveStaff([FromBody] DomesticStaffDto request)
        => Run(() => societies.SaveStaffAsync(request, UserId), "Staff saved.");

    // ── Billing ──────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/charge-schemes")]
    public Task<IActionResult> GetChargeSchemes(Guid id) => Run(() => societies.GetChargeSchemesAsync(id));

    [HttpPost("charge-schemes")]
    public Task<IActionResult> SaveChargeScheme([FromBody] MaintenanceChargeSchemeDto request)
        => Run(() => societies.SaveChargeSchemeAsync(request, UserId), "Scheme saved.");

    /// <summary>
    /// Raises the month's bills. Maintenance, utilities, one-off charges, penalties, arrears and
    /// late fee land on one document, because a resident who gets five separate bills pays none.
    /// </summary>
    [HttpPost("billing/run")]
    public Task<IActionResult> RunBilling([FromBody] MaintenanceBillRunDto request)
        => Run(() => societies.RunBillingAsync(request, UserId), "Billing run complete.");

    [HttpGet("{id:guid}/bills")]
    public Task<IActionResult> GetBills(
        Guid id, [FromQuery] ListQueryDto query, [FromQuery] InstalmentStatus? status)
        => RunPaged(() => societies.GetBillsAsync(query, id, status));

    [HttpGet("bills/{id:guid}")]
    public Task<IActionResult> GetBill(Guid id)
        => RunFound(() => societies.GetBillAsync(id), "That bill does not exist.");

    [HttpGet("{id:guid}/charges")]
    public Task<IActionResult> GetCharges(Guid id, [FromQuery] Guid? unitId)
        => Run(() => societies.GetSocietyChargesAsync(id, unitId));

    [HttpPost("charges")]
    public Task<IActionResult> SaveCharge([FromBody] SocietyChargeDto request)
        => Run(() => societies.SaveSocietyChargeAsync(request, UserId), "Charge saved.");

    [HttpPost("penalties")]
    public Task<IActionResult> ImposePenalty([FromBody] SocietyPenaltyDto request)
        => Run(() => societies.ImposePenaltyAsync(request, UserId), "Penalty imposed.");

    [HttpPost("penalties/{id:guid}/waive")]
    public Task<IActionResult> WaivePenalty(Guid id, [FromQuery] string reason)
        => Run(() => societies.WaivePenaltyAsync(id, reason, UserId), "Penalty waived.");

    [HttpGet("{id:guid}/penalties")]
    public Task<IActionResult> GetPenalties(Guid id, [FromQuery] ListQueryDto query)
        => RunPaged(() => societies.GetPenaltiesAsync(query, id));

    // ── The gate ─────────────────────────────────────────────────────────────

    [HttpPost("visitor-passes")]
    public Task<IActionResult> CreateVisitorPass([FromBody] VisitorPassCreateDto request)
        => Run(() => societies.CreateVisitorPassAsync(request, UserId), "Pass created.");

    [HttpGet("{id:guid}/visitor-passes")]
    public Task<IActionResult> GetVisitorPasses(
        Guid id, [FromQuery] ListQueryDto query, [FromQuery] Guid? unitId)
        => RunPaged(() => societies.GetVisitorPassesAsync(query, id, unitId));

    [HttpPost("gate/entries")]
    public Task<IActionResult> RecordEntry([FromBody] GateEntryCreateDto request)
        => Run(() => societies.RecordEntryAsync(request, UserId), "Entry recorded.");

    [HttpPost("gate/approve")]
    public Task<IActionResult> ApproveEntry([FromBody] GateApprovalDto request)
        => Run(() => societies.ApproveEntryAsync(request, UserId), "Decision recorded.");

    [HttpPost("gate/entries/{id:guid}/check-out")]
    public Task<IActionResult> CheckOut(Guid id)
        => Run(() => societies.CheckOutAsync(id, UserId), "Checked out.");

    [HttpGet("{id:guid}/gate/log")]
    public Task<IActionResult> GetGateLog(
        Guid id, [FromQuery] ListQueryDto query, [FromQuery] GateEntryStatus? status)
        => RunPaged(() => societies.GetGateLogAsync(query, id, status));

    /// <summary>Who is inside the community right now — the first question after any incident.</summary>
    [HttpGet("{id:guid}/gate/inside-now")]
    public Task<IActionResult> GetInsideNow(Guid id) => Run(() => societies.GetInsideNowAsync(id));

    /// <summary>Replays entries a guard recorded while the tablet had no signal.</summary>
    [HttpPost("gate/sync")]
    public Task<IActionResult> SyncGate([FromBody] GateSyncBatchDto batch)
        => Run(() => societies.SyncGateBatchAsync(batch, UserId), "Entries synchronised.");

    [HttpPost("gate/passes")]
    public Task<IActionResult> CreateGatePass([FromBody] GatePassDto request)
        => Run(() => societies.CreateGatePassAsync(request, UserId), "Gate pass created.");

    [HttpPost("moves")]
    public Task<IActionResult> CreateMoveRequest([FromBody] MoveRequestDto request)
        => Run(() => societies.CreateMoveRequestAsync(request, UserId), "Move request created.");

    [HttpPost("moves/{id:guid}/decide")]
    public Task<IActionResult> DecideMoveRequest(Guid id, [FromQuery] string status)
        => Run(() => societies.DecideMoveRequestAsync(id, status, UserId), "Decision recorded.");

    // ── Amenities ────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/amenities")]
    public Task<IActionResult> GetAmenities(Guid id) => Run(() => societies.GetAmenitiesAsync(id));

    [HttpPost("amenities")]
    public Task<IActionResult> SaveAmenity([FromBody] AmenityDto request)
        => Run(() => societies.SaveAmenityAsync(request, UserId), "Amenity saved.");

    [HttpGet("amenities/{id:guid}/availability")]
    public Task<IActionResult> GetAvailability(Guid id, [FromQuery] DateOnly date)
        => Run(() => societies.GetAvailabilityAsync(id, date));

    [HttpPost("amenities/bookings")]
    public Task<IActionResult> BookAmenity([FromBody] AmenityBookingCreateDto request)
        => Run(() => societies.BookAmenityAsync(request, UserId), "Booking created.");

    [HttpPost("amenities/bookings/{id:guid}/decide")]
    public Task<IActionResult> DecideBooking(Guid id, [FromQuery] bool approved, [FromQuery] string? reason)
        => Run(() => societies.DecideAmenityBookingAsync(id, approved, reason, UserId), "Decision recorded.");

    [HttpPost("amenities/bookings/{id:guid}/cancel")]
    public Task<IActionResult> CancelBooking(Guid id, [FromQuery] string? reason)
        => Run(() => societies.CancelAmenityBookingAsync(id, reason, UserId), "Booking cancelled.");

    [HttpGet("{id:guid}/amenities/bookings")]
    public Task<IActionResult> GetBookings(
        Guid id, [FromQuery] ListQueryDto query, [FromQuery] AmenityBookingStatus? status)
        => RunPaged(() => societies.GetAmenityBookingsAsync(query, id, status));

    // ── Complaints ───────────────────────────────────────────────────────────

    [HttpPost("complaints")]
    public Task<IActionResult> CreateComplaint([FromBody] ComplaintCreateDto request)
        => Run(() => societies.CreateComplaintAsync(request, UserId), "Complaint logged.");

    [HttpGet("complaints/{id:guid}")]
    public Task<IActionResult> GetComplaint(Guid id)
        => RunFound(() => societies.GetComplaintAsync(id), "That complaint does not exist.");

    [HttpPost("complaints/search")]
    public Task<IActionResult> SearchComplaints([FromBody] ComplaintSearchDto query)
        => RunPaged(() => societies.GetComplaintsAsync(query));

    [HttpPost("complaints/{id:guid}/update")]
    public Task<IActionResult> UpdateComplaint(
        Guid id,
        [FromQuery] TicketStatus? status,
        [FromQuery] string note,
        [FromQuery] bool visibleToResident = true,
        [FromBody] List<string>? photoUrls = null)
        => Run(() => societies.UpdateComplaintAsync(id, status, note, photoUrls, visibleToResident, UserId),
            "Update posted.");

    [HttpPost("complaints/{id:guid}/assign")]
    public Task<IActionResult> AssignComplaint(
        Guid id, [FromQuery] Guid? assignToUserId, [FromQuery] Guid? contractorId)
        => Run(() => societies.AssignComplaintAsync(id, assignToUserId, contractorId, UserId), "Assigned.");

    [HttpPost("complaints/{id:guid}/rate")]
    public Task<IActionResult> RateComplaint(Guid id, [FromQuery] int rating, [FromQuery] string? note)
        => Run(() => societies.RateComplaintAsync(id, rating, note, UserId), "Thank you.");

    [HttpPost("complaints/escalate")]
    public Task<IActionResult> EscalateBreached()
        => Run(societies.EscalateBreachedComplaintsAsync, "Breached complaints escalated.");

    // ── Notices and polls ────────────────────────────────────────────────────

    [HttpGet("{id:guid}/notices")]
    public Task<IActionResult> GetNotices(Guid id, [FromQuery] ListQueryDto query)
        => RunPaged(() => societies.GetNoticesAsync(query, id));

    [HttpPost("notices")]
    public Task<IActionResult> SaveNotice([FromBody] SocietyNoticeDto request)
        => Run(() => societies.SaveNoticeAsync(request, UserId), "Notice published.");

    [HttpGet("{id:guid}/polls")]
    public Task<IActionResult> GetPolls(Guid id, [FromQuery] bool openOnly = true)
        => Run(() => societies.GetPollsAsync(id, openOnly));

    [HttpPost("polls")]
    public Task<IActionResult> SavePoll([FromBody] SocietyPollDto request)
        => Run(() => societies.SavePollAsync(request, UserId), "Poll saved.");

    /// <summary>One vote per unit, not per person — which is how a society's constitution reads.</summary>
    [HttpPost("polls/{id:guid}/vote")]
    public Task<IActionResult> Vote(
        Guid id, [FromQuery] Guid? unitId, [FromQuery] string choice, [FromQuery] string? comment)
        => Run(() => societies.VoteAsync(id, unitId, choice, comment, UserId), "Vote recorded.");

    // ── Building control ─────────────────────────────────────────────────────

    [HttpGet("{id:guid}/building-applications")]
    public Task<IActionResult> GetBuildingApplications(
        Guid id, [FromQuery] ListQueryDto query, [FromQuery] BuildingApplicationStatus? status)
        => RunPaged(() => societies.GetBuildingApplicationsAsync(query, id, status));

    [HttpGet("building-applications/{id:guid}")]
    public Task<IActionResult> GetBuildingApplication(Guid id)
        => RunFound(() => societies.GetBuildingApplicationAsync(id), "That application does not exist.");

    [HttpPost("building-applications")]
    public Task<IActionResult> SaveBuildingApplication([FromBody] BuildingPlanApplicationDto request)
        => Run(() => societies.SaveBuildingApplicationAsync(request, UserId), "Application saved.");

    [HttpPost("building-applications/{id:guid}/decide")]
    public Task<IActionResult> DecideBuildingApplication(
        Guid id,
        [FromQuery] BuildingApplicationStatus status,
        [FromQuery] string? conditions,
        [FromQuery] string? rejectionReason)
        => Run(() => societies.DecideBuildingApplicationAsync(id, status, conditions, rejectionReason, UserId),
            "Decision recorded.");

    [HttpPost("building-inspections")]
    public Task<IActionResult> RecordBuildingInspection([FromBody] BuildingInspectionDto request)
        => Run(() => societies.RecordBuildingInspectionAsync(request, UserId), "Inspection recorded.");

    [HttpPost("violations")]
    public Task<IActionResult> IssueViolation([FromBody] ViolationNoticeDto request)
        => Run(() => societies.IssueViolationAsync(request, UserId), "Notice issued.");

    [HttpPost("violations/{id:guid}/close")]
    public Task<IActionResult> CloseViolation(Guid id, [FromQuery] DateOnly compliedOn)
        => Run(() => societies.CloseViolationAsync(id, compliedOn, UserId), "Violation closed.");

    [HttpGet("{id:guid}/violations")]
    public Task<IActionResult> GetViolations(
        Guid id, [FromQuery] ListQueryDto query, [FromQuery] bool openOnly = true)
        => RunPaged(() => societies.GetViolationsAsync(query, id, openOnly));
}
