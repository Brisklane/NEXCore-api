using Fitness.Application.DTOs;
using Fitness.Application.Services.Interfaces;
using Fitness.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;

namespace Fitness.Api.Controllers;

/// <summary>Clubs, their opening hours, the rooms inside them, and company-wide settings.</summary>
[Route("api/fitness/clubs")]
public class ClubController(
    IClubService clubs,
    IFitnessTenant tenant,
    ILogger<ClubController> logger) : FitnessControllerBase(logger)
{
    /// <summary>
    /// Brings this company's Fitness app up to a working state.
    ///
    /// Needed because installing an app is not the same event as creating a company: a business
    /// that adds Fitness today never saw <c>CompanyCreatedEvent</c>, so it has no club, no access
    /// rule and no waiver — and every screen would silently do nothing. Safe to call repeatedly;
    /// it only fills in what is missing.
    /// </summary>
    [HttpPost("provision")]
    public Task<IActionResult> Provision([FromQuery] bool includeSampleData = false) => Run(async () =>
    {
        await clubs.EnsureProvisionedAsync(
            tenant.CompanyId, tenant.BranchId, tenant.BusinessUnitId, UserId, includeSampleData);

        return await clubs.GetClubsAsync(false);
    }, "Fitness is ready.");

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] bool activeOnly = false)
        => Run(() => clubs.GetClubsAsync(activeOnly));

    [HttpGet("{id:guid}")]
    public Task<IActionResult> Get(Guid id)
        => RunFound(() => clubs.GetClubAsync(id), "Club not found.");

    [HttpPost]
    public Task<IActionResult> Create([FromBody] SaveClubDto request)
        => Run(() => clubs.SaveClubAsync(null, request, UserId), "Club created.");

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(Guid id, [FromBody] SaveClubDto request)
        => Run(() => clubs.SaveClubAsync(id, request, UserId), "Club saved.");

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(Guid id)
        => Run(() => clubs.DeleteClubAsync(id, UserId), "Club removed.");

    // ── Hours ────────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/schedules")]
    public Task<IActionResult> GetSchedules(Guid id)
        => Run(() => clubs.GetSchedulesAsync(id));

    [HttpPut("{id:guid}/schedules")]
    public Task<IActionResult> SaveSchedules(Guid id, [FromBody] List<ClubScheduleDto> schedules)
        => Run(() => clubs.SaveSchedulesAsync(id, schedules, UserId), "Opening hours saved.");

    [HttpGet("{id:guid}/closures")]
    public Task<IActionResult> GetClosures(Guid id, [FromQuery] bool upcomingOnly = true)
        => Run(() => clubs.GetClosuresAsync(id, upcomingOnly));

    /// <summary>Records a closure, cancelling any classes that fall inside it.</summary>
    [HttpPost("closures")]
    public Task<IActionResult> SaveClosure([FromBody] ClubClosureDto request, [FromQuery] Guid? id = null)
        => Run(() => clubs.SaveClosureAsync(id, request, UserId), "Closure saved.");

    // ── Spaces ───────────────────────────────────────────────────────────────

    [HttpGet("{id:guid}/areas")]
    public Task<IActionResult> GetAreas(Guid id) => Run(() => clubs.GetAreasAsync(id));

    [HttpPost("areas")]
    public Task<IActionResult> SaveArea([FromBody] ClubAreaDto request, [FromQuery] Guid? id = null)
        => Run(() => clubs.SaveAreaAsync(id, request, UserId), "Area saved.");

    [HttpGet("{id:guid}/rooms")]
    public Task<IActionResult> GetRooms(Guid id) => Run(() => clubs.GetRoomsAsync(id));

    /// <summary>Saves a room and its spot map — bike numbers, mat positions, rig stations.</summary>
    [HttpPost("rooms/layout")]
    public Task<IActionResult> SaveRoomLayout([FromBody] SaveRoomLayoutDto request)
        => Run(() => clubs.SaveRoomLayoutAsync(request, UserId), "Room layout saved.");

    // ── Settings ─────────────────────────────────────────────────────────────

    [HttpGet("settings")]
    public Task<IActionResult> GetSettings() => Run(clubs.GetSettingsAsync);

    [HttpPut("settings")]
    public Task<IActionResult> SaveSettings([FromBody] FitnessSettingsDto request)
        => Run(() => clubs.SaveSettingsAsync(request, UserId), "Settings saved.");
}
