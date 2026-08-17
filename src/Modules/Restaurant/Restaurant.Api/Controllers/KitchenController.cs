using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant.Application.DTOs;
using Restaurant.Application.Services.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;
using Restaurant.Infrastructure.Persistence;
using Restaurant.Infrastructure.Services;

namespace Restaurant.Api.Controllers;

/// <summary>The kitchen display, station configuration and ticket routing.</summary>
[Route("api/restaurant/kitchen")]
public class KitchenController(
    IKitchenService kitchen,
    RestaurantDbContext db,
    IRestaurantTenant tenant,
    ILogger<KitchenController> logger) : RestaurantControllerBase(logger)
{
    // ── Display ──────────────────────────────────────────────────────────────

    /// <summary>
    /// One poll returns everything a kitchen screen shows: its tickets, the all-day counts and
    /// the SLA state of each card. Passing no station id gives the expo view across every station.
    /// </summary>
    [HttpGet("display/{outletId:guid}")]
    public Task<IActionResult> GetDisplay(
        Guid outletId, [FromQuery] Guid? stationId, [FromQuery] bool includeBumped = false)
        => Run(() => kitchen.GetDisplayAsync(outletId, stationId, includeBumped));

    [HttpGet("tickets/order/{orderId:guid}")]
    public Task<IActionResult> GetTicketsForOrder(Guid orderId)
        => Run(() => kitchen.GetTicketsForOrderAsync(orderId));

    [HttpPost("tickets/{ticketId:guid}/acknowledge")]
    public Task<IActionResult> Acknowledge(Guid ticketId)
        => Run(() => kitchen.AcknowledgeAsync(ticketId, UserId));

    [HttpPost("tickets/{ticketId:guid}/start")]
    public Task<IActionResult> Start(Guid ticketId)
        => Run(() => kitchen.StartAsync(ticketId, UserId));

    [HttpPost("tickets/{ticketId:guid}/ready")]
    public Task<IActionResult> Ready(Guid ticketId, [FromQuery] Guid? lineId)
        => Run(() => kitchen.MarkReadyAsync(ticketId, lineId, UserId));

    [HttpPost("tickets/bump")]
    public Task<IActionResult> Bump([FromBody] BumpTicketDto request)
        => Run(() => kitchen.BumpAsync(request, UserId));

    [HttpPost("tickets/recall")]
    public Task<IActionResult> Recall([FromBody] RecallTicketDto request)
        => Run(() => kitchen.RecallAsync(request, UserId), "Ticket recalled.");

    [HttpPost("tickets/{ticketId:guid}/priority")]
    public Task<IActionResult> SetPriority(Guid ticketId, [FromQuery] bool isPriority = true)
        => Run(() => kitchen.SetPriorityAsync(ticketId, isPriority, UserId));

    // ── Stations ─────────────────────────────────────────────────────────────

    [HttpGet("stations/{outletId:guid}")]
    public Task<IActionResult> GetStations(Guid outletId) => Run(async () =>
    {
        var now = DateTime.UtcNow;

        var stations = await db.Stations.ForTenant(tenant)
            .Where(s => s.OutletId == outletId)
            .Include(s => s.RoutingRules.Where(r => !r.IsDeleted))
            .OrderBy(s => s.DisplayOrder)
            .ToListAsync();

        var openTickets = await db.KitchenTickets.ForTenant(tenant)
            .Where(t => t.OutletId == outletId
                     && t.Status != KitchenTicketStatus.Bumped
                     && t.Status != KitchenTicketStatus.Cancelled)
            .Select(t => new { t.StationId, t.FiredAt })
            .ToListAsync();

        var prep = await db.KitchenTickets.ForTenant(tenant)
            .Where(t => t.OutletId == outletId && t.PrepSeconds != null && t.FiredAt >= now.Date)
            .Select(t => new { t.StationId, Seconds = t.PrepSeconds!.Value })
            .ToListAsync();

        var categoryNames = await db.MenuCategories.ForTenant(tenant).ToDictionaryAsync(c => c.Id, c => c.Name);
        var itemNames = await db.MenuItems.ForTenant(tenant).ToDictionaryAsync(i => i.Id, i => i.Name);

        return stations.Select(s =>
        {
            var dto = RestaurantMapper.ToDto(s);
            var own = openTickets.Where(t => t.StationId == s.Id).ToList();
            dto.OpenTicketCount = own.Count;
            dto.OverdueTicketCount = own.Count(t => (now - t.FiredAt).TotalMinutes >= s.SlaMinutes);

            var times = prep.Where(p => p.StationId == s.Id).Select(p => p.Seconds).ToList();
            dto.AveragePrepSeconds = times.Count == 0 ? 0 : (int)times.Average();

            foreach (var rule in dto.RoutingRules)
            {
                dto.RoutingRules.First(r => r.Id == rule.Id).StationName = s.Name;
                if (rule.CategoryId.HasValue) rule.CategoryName = categoryNames.GetValueOrDefault(rule.CategoryId.Value);
                if (rule.MenuItemId.HasValue) rule.MenuItemName = itemNames.GetValueOrDefault(rule.MenuItemId.Value);
            }

            return dto;
        }).ToList();
    });

    [HttpPost("stations")]
    public Task<IActionResult> CreateStation([FromBody] SaveKitchenStationDto request) => Run(async () =>
    {
        var station = new KitchenStation().StampNew(tenant, UserId);
        Apply(station, request);
        await EnsureSingleExpoAsync(station);
        db.Stations.Add(station);
        await db.SaveChangesAsync();
        return RestaurantMapper.ToDto(station);
    }, "Station added.");

    [HttpPut("stations/{id:guid}")]
    public Task<IActionResult> UpdateStation(Guid id, [FromBody] SaveKitchenStationDto request) => Run(async () =>
    {
        var station = await db.Stations.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new InvalidOperationException("Station not found.");

        Apply(station, request);
        station.StampUpdated(UserId);
        await EnsureSingleExpoAsync(station);
        await db.SaveChangesAsync();
        return RestaurantMapper.ToDto(station);
    }, "Station saved.");

    [HttpDelete("stations/{id:guid}")]
    public Task<IActionResult> DeleteStation(Guid id) => Run(async () =>
    {
        var station = await db.Stations.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new InvalidOperationException("Station not found.");

        var open = await db.KitchenTickets.ForTenant(tenant)
            .CountAsync(t => t.StationId == id
                          && t.Status != KitchenTicketStatus.Bumped
                          && t.Status != KitchenTicketStatus.Cancelled);

        if (open > 0)
            throw new InvalidOperationException($"This station has {open} ticket(s) still open.");

        foreach (var rule in await db.RoutingRules.ForTenant(tenant).Where(r => r.StationId == id).ToListAsync())
            rule.StampDeleted(UserId);

        station.StampDeleted(UserId);
        await db.SaveChangesAsync();
    }, "Station removed.");

    /// <summary>
    /// Only one station per outlet can be the pass. Two expo screens would each believe they own
    /// the table and plates would leave twice.
    /// </summary>
    private async Task EnsureSingleExpoAsync(KitchenStation station)
    {
        if (!station.IsExpo) return;

        var others = await db.Stations.ForTenant(tenant)
            .Where(s => s.OutletId == station.OutletId && s.Id != station.Id && s.IsExpo)
            .ToListAsync();

        foreach (var other in others)
        {
            other.IsExpo = false;
            other.StampUpdated(UserId);
        }
    }

    private static void Apply(KitchenStation station, SaveKitchenStationDto request)
    {
        station.OutletId = request.OutletId;
        station.Name = request.Name;
        station.StationType = request.StationType;
        station.DisplayOrder = request.DisplayOrder;
        station.ColorHex = request.ColorHex;
        station.IsExpo = request.IsExpo;
        station.SlaMinutes = request.SlaMinutes <= 0 ? 15 : request.SlaMinutes;
        station.MaxConcurrentTickets = request.MaxConcurrentTickets <= 0 ? 12 : request.MaxConcurrentTickets;
        station.PrintsTickets = request.PrintsTickets;
        station.PrinterProfileId = request.PrinterProfileId;
        station.IsActive = request.IsActive;
        station.Description = request.Description;
    }

    // ── Routing rules ────────────────────────────────────────────────────────

    [HttpPost("routing-rules")]
    public Task<IActionResult> CreateRule([FromBody] StationRoutingRuleDto request) => Run(async () =>
    {
        var rule = new StationRoutingRule().StampNew(tenant, UserId);
        ApplyRule(rule, request);
        db.RoutingRules.Add(rule);
        await db.SaveChangesAsync();
        return RestaurantMapper.ToDto(rule);
    }, "Routing rule added.");

    [HttpPut("routing-rules/{id:guid}")]
    public Task<IActionResult> UpdateRule(Guid id, [FromBody] StationRoutingRuleDto request) => Run(async () =>
    {
        var rule = await db.RoutingRules.ForTenant(tenant).FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new InvalidOperationException("Routing rule not found.");

        ApplyRule(rule, request);
        rule.StampUpdated(UserId);
        await db.SaveChangesAsync();
        return RestaurantMapper.ToDto(rule);
    }, "Routing rule saved.");

    [HttpDelete("routing-rules/{id:guid}")]
    public Task<IActionResult> DeleteRule(Guid id) => Run(async () =>
    {
        var rule = await db.RoutingRules.ForTenant(tenant).FirstOrDefaultAsync(r => r.Id == id)
            ?? throw new InvalidOperationException("Routing rule not found.");

        rule.StampDeleted(UserId);
        await db.SaveChangesAsync();
    }, "Routing rule removed.");

    private static void ApplyRule(StationRoutingRule rule, StationRoutingRuleDto request)
    {
        rule.StationId = request.StationId;
        rule.OutletId = request.OutletId;
        rule.MatchType = request.MatchType;
        rule.CategoryId = request.CategoryId;
        rule.MenuItemId = request.MenuItemId;
        rule.OrderType = request.OrderType;
        rule.Priority = request.Priority;
        rule.IsAdditional = request.IsAdditional;
        rule.IsActive = request.IsActive;
    }

    // ── Printers ─────────────────────────────────────────────────────────────

    [HttpGet("printers/{outletId:guid}")]
    public Task<IActionResult> GetPrinters(Guid outletId) => Run(async () =>
        await db.PrinterProfiles.ForTenant(tenant)
            .Where(p => p.OutletId == outletId)
            .OrderBy(p => p.Name)
            .Select(p => RestaurantMapper.ToDto(p))
            .ToListAsync());

    [HttpPost("printers")]
    public Task<IActionResult> CreatePrinter([FromBody] PrinterProfileDto request) => Run(async () =>
    {
        var printer = new PrinterProfile().StampNew(tenant, UserId);
        ApplyPrinter(printer, request);
        db.PrinterProfiles.Add(printer);
        await db.SaveChangesAsync();
        return RestaurantMapper.ToDto(printer);
    }, "Printer added.");

    [HttpPut("printers/{id:guid}")]
    public Task<IActionResult> UpdatePrinter(Guid id, [FromBody] PrinterProfileDto request) => Run(async () =>
    {
        var printer = await db.PrinterProfiles.ForTenant(tenant).FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new InvalidOperationException("Printer not found.");

        ApplyPrinter(printer, request);
        printer.StampUpdated(UserId);
        await db.SaveChangesAsync();
        return RestaurantMapper.ToDto(printer);
    }, "Printer saved.");

    [HttpDelete("printers/{id:guid}")]
    public Task<IActionResult> DeletePrinter(Guid id) => Run(async () =>
    {
        var printer = await db.PrinterProfiles.ForTenant(tenant).FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new InvalidOperationException("Printer not found.");

        printer.StampDeleted(UserId);
        await db.SaveChangesAsync();
    }, "Printer removed.");

    private static void ApplyPrinter(PrinterProfile printer, PrinterProfileDto request)
    {
        printer.OutletId = request.OutletId;
        printer.Name = request.Name;
        printer.Target = request.Target;
        printer.PaperWidthMm = request.PaperWidthMm <= 0 ? 80 : request.PaperWidthMm;
        printer.IsReceiptPrinter = request.IsReceiptPrinter;
        printer.IsKitchenPrinter = request.IsKitchenPrinter;
        printer.IsLabelPrinter = request.IsLabelPrinter;
        printer.OpensCashDrawer = request.OpensCashDrawer;
        printer.CopiesPerTicket = request.CopiesPerTicket <= 0 ? 1 : request.CopiesPerTicket;
        printer.HeaderText = request.HeaderText;
        printer.FooterText = request.FooterText;
        printer.IsActive = request.IsActive;
    }
}
