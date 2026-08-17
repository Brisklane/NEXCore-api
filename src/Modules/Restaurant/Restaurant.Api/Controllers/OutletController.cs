using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant.Application.DTOs;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;
using Restaurant.Infrastructure.Persistence;
using Restaurant.Infrastructure.Services;

namespace Restaurant.Api.Controllers;

/// <summary>Venues, their trading hours, and the company-wide settings the app runs on.</summary>
[Route("api/restaurant/outlets")]
public class OutletController(
    RestaurantDbContext db,
    IRestaurantTenant tenant,
    ILogger<OutletController> logger) : RestaurantControllerBase(logger)
{
    /// <summary>
    /// Brings this company's Restaurant app up to a working state.
    ///
    /// Needed because installing an app is not the same event as creating a company: a business
    /// that adds Restaurant today never saw <c>CompanyCreatedEvent</c>, so it has no outlet, no
    /// stations and no reason codes — and every screen would silently do nothing. Safe to call
    /// repeatedly; it only fills in what is missing.
    /// </summary>
    [HttpPost("provision")]
    public Task<IActionResult> Provision(
        [FromServices] Restaurant.Infrastructure.Services.RestaurantInitializationService initializer,
        [FromQuery] bool includeSampleData = false)
        => Run(async () =>
        {
            await initializer.EnsureProvisionedAsync(
                tenant.CompanyId, tenant.BranchId, tenant.BusinessUnitId, UserId, includeSampleData);

            var outlets = await db.Outlets.ForTenant(tenant)
                .Include(o => o.Schedules.Where(s => !s.IsDeleted))
                .OrderBy(o => o.Name)
                .ToListAsync();

            return outlets.Select(RestaurantMapper.ToDto).ToList();
        }, "Restaurant is ready.");

    [HttpGet]
    public Task<IActionResult> GetAll([FromQuery] bool activeOnly = false) => Run(async () =>
    {
        var outlets = await db.Outlets.ForTenant(tenant)
            .WhereIf(activeOnly, o => o.IsActive)
            .Include(o => o.Schedules.Where(s => !s.IsDeleted))
            .OrderBy(o => o.Name)
            .ToListAsync();

        var ids = outlets.Select(o => o.Id).ToList();

        var tableStats = await db.Tables.ForTenant(tenant)
            .Where(t => ids.Contains(t.OutletId) && t.IsActive)
            .GroupBy(t => t.OutletId)
            .Select(g => new
            {
                OutletId = g.Key,
                Total = g.Count(),
                Occupied = g.Count(t => t.CurrentOrderId != null),
            })
            .ToListAsync();

        var openOrders = await db.Orders.ForTenant(tenant)
            .Where(o => ids.Contains(o.OutletId)
                     && o.Status != RestaurantOrderStatus.Closed
                     && o.Status != RestaurantOrderStatus.Cancelled)
            .GroupBy(o => o.OutletId)
            .Select(g => new { OutletId = g.Key, Count = g.Count() })
            .ToListAsync();

        return outlets.Select(o =>
        {
            var dto = RestaurantMapper.ToDto(o);
            var stats = tableStats.FirstOrDefault(s => s.OutletId == o.Id);
            dto.TableCount = stats?.Total ?? 0;
            dto.OccupiedTableCount = stats?.Occupied ?? 0;
            dto.OpenOrderCount = openOrders.FirstOrDefault(s => s.OutletId == o.Id)?.Count ?? 0;
            return dto;
        }).ToList();
    });

    [HttpGet("{id:guid}")]
    public Task<IActionResult> GetById(Guid id) => RunFound(async () =>
    {
        var outlet = await db.Outlets.ForTenant(tenant)
            .Include(o => o.Schedules.Where(s => !s.IsDeleted))
            .FirstOrDefaultAsync(o => o.Id == id);

        return outlet is null ? null : RestaurantMapper.ToDto(outlet);
    }, "Outlet not found.");

    [HttpPost]
    public Task<IActionResult> Create([FromBody] SaveOutletDto request) => Run(async () =>
    {
        var outlet = new RestaurantOutlet().StampNew(tenant, UserId);
        Apply(outlet, request);
        db.Outlets.Add(outlet);
        await db.SaveChangesAsync();
        return RestaurantMapper.ToDto(outlet);
    }, "Outlet created.");

    [HttpPut("{id:guid}")]
    public Task<IActionResult> Update(Guid id, [FromBody] SaveOutletDto request) => Run(async () =>
    {
        var outlet = await db.Outlets.ForTenant(tenant).FirstOrDefaultAsync(o => o.Id == id)
            ?? throw new InvalidOperationException("Outlet not found.");

        Apply(outlet, request);
        outlet.StampUpdated(UserId);
        await db.SaveChangesAsync();
        return RestaurantMapper.ToDto(outlet);
    }, "Outlet saved.");

    [HttpDelete("{id:guid}")]
    public Task<IActionResult> Delete(Guid id) => Run(async () =>
    {
        var outlet = await db.Outlets.ForTenant(tenant).FirstOrDefaultAsync(o => o.Id == id)
            ?? throw new InvalidOperationException("Outlet not found.");

        var openOrders = await db.Orders.ForTenant(tenant)
            .CountAsync(o => o.OutletId == id
                          && o.Status != RestaurantOrderStatus.Closed
                          && o.Status != RestaurantOrderStatus.Cancelled);

        if (openOrders > 0)
            throw new InvalidOperationException(
                $"This outlet still has {openOrders} open order(s). Close them before removing it.");

        outlet.StampDeleted(UserId);
        await db.SaveChangesAsync();
    }, "Outlet removed.");

    private static void Apply(RestaurantOutlet outlet, SaveOutletDto request)
    {
        outlet.Code = request.Code;
        outlet.Name = request.Name;
        outlet.ServiceStyle = request.ServiceStyle;
        outlet.CuisineType = request.CuisineType;
        outlet.Phone = request.Phone;
        outlet.Email = request.Email;
        outlet.AddressLine = request.AddressLine;
        outlet.City = request.City;
        outlet.CountryCode = request.CountryCode;
        outlet.TimeZoneId = request.TimeZoneId;
        outlet.CurrencyCode = string.IsNullOrWhiteSpace(request.CurrencyCode) ? "USD" : request.CurrencyCode;
        outlet.WarehouseId = request.WarehouseId;
        outlet.PosStoreId = request.PosStoreId;
        outlet.DefaultMenuId = request.DefaultMenuId;
        outlet.DefaultTaxGroupId = request.DefaultTaxGroupId;
        outlet.DefaultTaxPercent = request.DefaultTaxPercent;
        outlet.TakeawayTaxPercent = request.TakeawayTaxPercent;
        outlet.ServiceChargeRuleId = request.ServiceChargeRuleId;
        outlet.AverageDiningMinutes = request.AverageDiningMinutes <= 0 ? 60 : request.AverageDiningMinutes;
        outlet.AcceptsReservations = request.AcceptsReservations;
        outlet.AcceptsDelivery = request.AcceptsDelivery;
        outlet.AcceptsTakeaway = request.AcceptsTakeaway;
        outlet.HasDriveThru = request.HasDriveThru;
        outlet.QrOrderingEnabled = request.QrOrderingEnabled;
        outlet.IsTemporarilyClosed = request.IsTemporarilyClosed;
        outlet.ClosureNote = request.ClosureNote;
        outlet.LogoUrl = request.LogoUrl;
        outlet.ReceiptFooter = request.ReceiptFooter;
        outlet.IsActive = request.IsActive;
        outlet.Description = request.Description;
    }

    // ── Trading hours ────────────────────────────────────────────────────────

    [HttpGet("{outletId:guid}/schedules")]
    public Task<IActionResult> GetSchedules(Guid outletId) => Run(async () =>
        await db.OutletSchedules.ForTenant(tenant)
            .Where(s => s.OutletId == outletId)
            .OrderBy(s => s.OverrideDate ?? DateTime.MinValue).ThenBy(s => s.DayOfWeek)
            .Select(s => RestaurantMapper.ToDto(s))
            .ToListAsync());

    [HttpPut("{outletId:guid}/schedules")]
    public Task<IActionResult> SaveSchedules(Guid outletId, [FromBody] List<OutletScheduleDto> request) => Run(async () =>
    {
        var existing = await db.OutletSchedules.ForTenant(tenant)
            .Where(s => s.OutletId == outletId).ToListAsync();

        foreach (var gone in existing.Where(s => request.All(r => r.Id != s.Id)))
            gone.StampDeleted(UserId);

        foreach (var dto in request)
        {
            var schedule = existing.FirstOrDefault(s => s.Id == dto.Id);
            if (schedule is null)
            {
                schedule = new OutletSchedule { OutletId = outletId }.StampNew(tenant, UserId);
                db.OutletSchedules.Add(schedule);
            }
            else schedule.StampUpdated(UserId);

            schedule.DayOfWeek = dto.DayOfWeek;
            schedule.OverrideDate = dto.OverrideDate;
            schedule.OpensAt = dto.OpensAt;
            schedule.ClosesAt = dto.ClosesAt;
            schedule.IsClosed = dto.IsClosed;
            schedule.Note = dto.Note;
        }

        await db.SaveChangesAsync();

        return await db.OutletSchedules.ForTenant(tenant)
            .Where(s => s.OutletId == outletId)
            .OrderBy(s => s.DayOfWeek)
            .Select(s => RestaurantMapper.ToDto(s))
            .ToListAsync();
    }, "Trading hours saved.");

    // ── Settings ─────────────────────────────────────────────────────────────

    [HttpGet("settings")]
    public Task<IActionResult> GetSettings() => Run(async () =>
    {
        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();

        // A company that installed the app after creation has no settings row yet; create the
        // defaults on first read rather than making every screen handle a null.
        if (settings is null)
        {
            settings = new RestaurantSettings().StampNew(tenant, UserId);
            db.Settings.Add(settings);
            await db.SaveChangesAsync();
        }

        return RestaurantMapper.ToDto(settings);
    });

    [HttpPut("settings")]
    public Task<IActionResult> UpdateSettings([FromBody] RestaurantSettingsDto request) => Run(async () =>
    {
        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();

        if (settings is null)
        {
            settings = new RestaurantSettings().StampNew(tenant, UserId);
            db.Settings.Add(settings);
        }
        else settings.StampUpdated(UserId);

        settings.RequireWaiterPin = request.RequireWaiterPin;
        settings.RequireGuestCountOnSeat = request.RequireGuestCountOnSeat;
        settings.RequireSeatNumbers = request.RequireSeatNumbers;
        settings.AutoFireOnSend = request.AutoFireOnSend;
        settings.SeatedAttentionMinutes = request.SeatedAttentionMinutes;
        settings.ServedAttentionMinutes = request.ServedAttentionMinutes;
        settings.AllowTableMerge = request.AllowTableMerge;
        settings.AllowTableTransfer = request.AllowTableTransfer;
        settings.AllowSplitBill = request.AllowSplitBill;
        settings.PricesIncludeTax = request.PricesIncludeTax;
        settings.TipsEnabled = request.TipsEnabled;
        settings.TipPresetPercents = request.TipPresetPercents;
        settings.TipPoolingEnabled = request.TipPoolingEnabled;
        settings.TipDistributionBasis = request.TipDistributionBasis;
        settings.KitchenTipSharePercent = request.KitchenTipSharePercent;
        settings.CashRoundingIncrement = request.CashRoundingIncrement;
        settings.PackagingChargePerOrder = request.PackagingChargePerOrder;
        settings.VoidRequiresReason = request.VoidRequiresReason;
        settings.DiscountRequiresReason = request.DiscountRequiresReason;
        settings.DiscountApprovalThreshold = request.DiscountApprovalThreshold;
        settings.KitchenDisplayEnabled = request.KitchenDisplayEnabled;
        settings.PrintKitchenTickets = request.PrintKitchenTickets;
        settings.ExpoScreenEnabled = request.ExpoScreenEnabled;
        settings.KdsWarningMinutes = request.KdsWarningMinutes;
        settings.AutoBumpOnServe = request.AutoBumpOnServe;
        settings.ShowAllergenWarnings = request.ShowAllergenWarnings;
        settings.DepleteStockOnCheckClose = request.DepleteStockOnCheckClose;
        settings.Auto86OnZeroStock = request.Auto86OnZeroStock;
        settings.TrackWastage = request.TrackWastage;
        settings.ReservationsEnabled = request.ReservationsEnabled;
        settings.WaitlistEnabled = request.WaitlistEnabled;
        settings.ReservationHoldMinutes = request.ReservationHoldMinutes;
        settings.DefaultReservationDuration = request.DefaultReservationDuration;
        settings.RequireDepositForLargeParty = request.RequireDepositForLargeParty;
        settings.LargePartyThreshold = request.LargePartyThreshold;
        settings.PrintReceiptAutomatically = request.PrintReceiptAutomatically;
        settings.EmailReceiptEnabled = request.EmailReceiptEnabled;
        settings.ReceiptHeader = request.ReceiptHeader;
        settings.ReceiptFooter = request.ReceiptFooter;
        settings.ShowCalories = request.ShowCalories;
        settings.FeedbackPromptEnabled = request.FeedbackPromptEnabled;

        await db.SaveChangesAsync();
        return RestaurantMapper.ToDto(settings);
    }, "Settings saved.");
}
