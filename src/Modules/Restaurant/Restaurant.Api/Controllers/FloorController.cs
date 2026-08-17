using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant.Application.DTOs;
using Restaurant.Application.Services.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Infrastructure.Persistence;
using Restaurant.Infrastructure.Services;

namespace Restaurant.Api.Controllers;

/// <summary>The floor plan: layout maintenance, the live view, and moving parties around it.</summary>
[Route("api/restaurant/floor")]
public class FloorController(
    IFloorService floors,
    RestaurantDbContext db,
    IRestaurantTenant tenant,
    ILogger<FloorController> logger) : RestaurantControllerBase(logger)
{
    // ── Live view ────────────────────────────────────────────────────────────

    /// <summary>Everything the floor screen renders for one outlet, in one call.</summary>
    [HttpGet("plan/{outletId:guid}")]
    public Task<IActionResult> GetPlan(Guid outletId) => Run(() => floors.GetFloorPlanAsync(outletId));

    [HttpGet("tables/{outletId:guid}")]
    public Task<IActionResult> GetTables(Guid outletId, [FromQuery] Guid? floorId)
        => Run(() => floors.GetTablesAsync(outletId, floorId));

    [HttpGet("table/{tableId:guid}")]
    public Task<IActionResult> GetTable(Guid tableId)
        => RunFound(() => floors.GetTableAsync(tableId), "Table not found.");

    // ── Table operations ─────────────────────────────────────────────────────

    [HttpPost("seat")]
    public Task<IActionResult> Seat([FromBody] SeatGuestsDto request)
        => Run(() => floors.SeatGuestsAsync(request, UserId), "Guests seated.");

    [HttpPost("state")]
    public Task<IActionResult> ChangeState([FromBody] ChangeTableStateDto request)
        => Run(() => floors.ChangeStateAsync(request, UserId), "Table updated.");

    [HttpPost("assign-waiter")]
    public Task<IActionResult> AssignWaiter([FromBody] AssignWaiterDto request)
        => Run(() => floors.AssignWaiterAsync(request, UserId), "Waiter assigned.");

    [HttpPost("transfer")]
    public Task<IActionResult> Transfer([FromBody] TransferTableDto request)
        => Run(() => floors.TransferTableAsync(request, UserId), "Table transferred.");

    [HttpPost("merge")]
    public Task<IActionResult> Merge([FromBody] MergeTablesDto request)
        => Run(() => floors.MergeTablesAsync(request, UserId), "Tables merged.");

    [HttpPost("unmerge/{primaryTableId:guid}")]
    public Task<IActionResult> Unmerge(Guid primaryTableId)
        => Run(() => floors.UnmergeTableAsync(primaryTableId, UserId), "Tables separated.");

    [HttpPost("clear/{tableId:guid}")]
    public Task<IActionResult> Clear(Guid tableId)
        => Run(() => floors.ClearTableAsync(tableId, UserId), "Table cleared.");

    // ── Designer ─────────────────────────────────────────────────────────────

    /// <summary>Saves an entire floor layout atomically — see <see cref="SaveLayoutDto"/>.</summary>
    [HttpPost("layout")]
    public Task<IActionResult> SaveLayout([FromBody] SaveLayoutDto request)
        => Run(() => floors.SaveLayoutAsync(request, UserId), "Layout saved.");

    [HttpGet("floors/{outletId:guid}")]
    public Task<IActionResult> GetFloors(Guid outletId) => Run(async () =>
        await db.Floors.ForTenant(tenant)
            .Where(f => f.OutletId == outletId)
            .OrderBy(f => f.DisplayOrder)
            .Select(f => new FloorDto
            {
                Id = f.Id,
                OutletId = f.OutletId,
                Name = f.Name,
                DisplayOrder = f.DisplayOrder,
                CanvasWidth = f.CanvasWidth,
                CanvasHeight = f.CanvasHeight,
                BackgroundImageUrl = f.BackgroundImageUrl,
                IsActive = f.IsActive,
            })
            .ToListAsync());

    [HttpPost("floors")]
    public Task<IActionResult> CreateFloor([FromBody] SaveFloorDto request) => Run(async () =>
    {
        var floor = new Floor
        {
            OutletId = request.OutletId,
            Name = request.Name,
            DisplayOrder = request.DisplayOrder,
            CanvasWidth = request.CanvasWidth,
            CanvasHeight = request.CanvasHeight,
            BackgroundImageUrl = request.BackgroundImageUrl,
            IsActive = request.IsActive,
        }.StampNew(tenant, UserId);

        db.Floors.Add(floor);
        await db.SaveChangesAsync();

        return new FloorDto
        {
            Id = floor.Id,
            OutletId = floor.OutletId,
            Name = floor.Name,
            DisplayOrder = floor.DisplayOrder,
            CanvasWidth = floor.CanvasWidth,
            CanvasHeight = floor.CanvasHeight,
            IsActive = floor.IsActive,
        };
    }, "Floor added.");

    [HttpPut("floors/{id:guid}")]
    public Task<IActionResult> UpdateFloor(Guid id, [FromBody] SaveFloorDto request) => Run(async () =>
    {
        var floor = await db.Floors.ForTenant(tenant).FirstOrDefaultAsync(f => f.Id == id)
            ?? throw new InvalidOperationException("Floor not found.");

        floor.Name = request.Name;
        floor.DisplayOrder = request.DisplayOrder;
        floor.CanvasWidth = request.CanvasWidth;
        floor.CanvasHeight = request.CanvasHeight;
        floor.BackgroundImageUrl = request.BackgroundImageUrl;
        floor.IsActive = request.IsActive;
        floor.StampUpdated(UserId);

        await db.SaveChangesAsync();

        return new FloorDto
        {
            Id = floor.Id,
            OutletId = floor.OutletId,
            Name = floor.Name,
            DisplayOrder = floor.DisplayOrder,
            CanvasWidth = floor.CanvasWidth,
            CanvasHeight = floor.CanvasHeight,
            BackgroundImageUrl = floor.BackgroundImageUrl,
            IsActive = floor.IsActive,
        };
    }, "Floor saved.");

    [HttpDelete("floors/{id:guid}")]
    public Task<IActionResult> DeleteFloor(Guid id) => Run(async () =>
    {
        var floor = await db.Floors.ForTenant(tenant).FirstOrDefaultAsync(f => f.Id == id)
            ?? throw new InvalidOperationException("Floor not found.");

        var busy = await db.Tables.ForTenant(tenant)
            .CountAsync(t => t.FloorId == id && t.CurrentOrderId != null);

        if (busy > 0)
            throw new InvalidOperationException($"{busy} table(s) on this floor are in service.");

        foreach (var table in await db.Tables.ForTenant(tenant).Where(t => t.FloorId == id).ToListAsync())
            table.StampDeleted(UserId);

        floor.StampDeleted(UserId);
        await db.SaveChangesAsync();
    }, "Floor removed.");

    // ── Sections ─────────────────────────────────────────────────────────────

    [HttpGet("sections/{outletId:guid}")]
    public Task<IActionResult> GetSections(Guid outletId) => Run(async () =>
    {
        var floorIds = await db.Floors.ForTenant(tenant)
            .Where(f => f.OutletId == outletId).Select(f => f.Id).ToListAsync();

        var sections = await db.Sections.ForTenant(tenant)
            .Where(s => floorIds.Contains(s.FloorId))
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync();

        var tables = await db.Tables.ForTenant(tenant)
            .Where(t => t.OutletId == outletId && t.IsActive)
            .Select(t => new { t.SectionId, t.Seats })
            .ToListAsync();

        return sections.Select(s =>
        {
            var dto = RestaurantMapper.ToDto(s);
            var own = tables.Where(t => t.SectionId == s.Id).ToList();
            dto.TableCount = own.Count;
            dto.SeatCount = own.Sum(t => t.Seats);
            return dto;
        }).ToList();
    });

    [HttpPost("sections")]
    public Task<IActionResult> CreateSection([FromBody] SaveSectionDto request) => Run(async () =>
    {
        var section = new TableSection().StampNew(tenant, UserId);
        ApplySection(section, request);
        db.Sections.Add(section);
        await db.SaveChangesAsync();
        return RestaurantMapper.ToDto(section);
    }, "Section added.");

    [HttpPut("sections/{id:guid}")]
    public Task<IActionResult> UpdateSection(Guid id, [FromBody] SaveSectionDto request) => Run(async () =>
    {
        var section = await db.Sections.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new InvalidOperationException("Section not found.");

        ApplySection(section, request);
        section.StampUpdated(UserId);
        await db.SaveChangesAsync();
        return RestaurantMapper.ToDto(section);
    }, "Section saved.");

    [HttpDelete("sections/{id:guid}")]
    public Task<IActionResult> DeleteSection(Guid id) => Run(async () =>
    {
        var section = await db.Sections.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new InvalidOperationException("Section not found.");

        // Tables survive; they simply become unassigned. Deleting them with the section would
        // destroy a layout because somebody renamed a zone.
        foreach (var table in await db.Tables.ForTenant(tenant).Where(t => t.SectionId == id).ToListAsync())
        {
            table.SectionId = null;
            table.StampUpdated(UserId);
        }

        section.StampDeleted(UserId);
        await db.SaveChangesAsync();
    }, "Section removed.");

    private static void ApplySection(TableSection section, SaveSectionDto request)
    {
        section.FloorId = request.FloorId;
        section.Name = request.Name;
        section.DisplayOrder = request.DisplayOrder;
        section.ColorHex = request.ColorHex;
        section.IsSmoking = request.IsSmoking;
        section.IsOutdoor = request.IsOutdoor;
        section.IsPrivate = request.IsPrivate;
        section.MinimumSpend = request.MinimumSpend;
        section.IsActive = request.IsActive;
    }
}
