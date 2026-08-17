using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Restaurant.Application.DTOs;
using Restaurant.Application.Services.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;
using Restaurant.Infrastructure.Persistence;
using Restaurant.Infrastructure.Services;

namespace Restaurant.Api.Controllers;

/// <summary>Reservations, the walk-in waitlist, guest records and feedback.</summary>
[Route("api/restaurant/front-of-house")]
public class FrontOfHouseController(
    IReservationService reservations,
    RestaurantDbContext db,
    IRestaurantTenant tenant,
    ILogger<FrontOfHouseController> logger) : RestaurantControllerBase(logger)
{
    // ── Reservations ─────────────────────────────────────────────────────────

    [HttpGet("reservations/{outletId:guid}")]
    public Task<IActionResult> GetReservations(
        Guid outletId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] ReservationStatus? status)
        => Run(() => reservations.GetReservationsAsync(
            outletId,
            from ?? DateTime.UtcNow.Date,
            to ?? DateTime.UtcNow.Date.AddDays(1).AddSeconds(-1),
            status));

    [HttpGet("reservations/detail/{id:guid}")]
    public Task<IActionResult> GetReservation(Guid id)
        => RunFound(() => reservations.GetReservationAsync(id), "Reservation not found.");

    [HttpPost("reservations")]
    public Task<IActionResult> CreateReservation([FromBody] SaveReservationDto request)
        => Run(() => reservations.SaveReservationAsync(null, request, UserId), "Booking created.");

    [HttpPut("reservations/{id:guid}")]
    public Task<IActionResult> UpdateReservation(Guid id, [FromBody] SaveReservationDto request)
        => Run(() => reservations.SaveReservationAsync(id, request, UserId), "Booking saved.");

    [HttpPost("reservations/{id:guid}/status")]
    public Task<IActionResult> ChangeReservationStatus(Guid id, [FromBody] ChangeReservationStatusDto request)
        => Run(() => reservations.ChangeStatusAsync(id, request, UserId), "Booking updated.");

    /// <summary>Which tables can take this party at this time, and what else to offer if none can.</summary>
    [HttpGet("availability/{outletId:guid}")]
    public Task<IActionResult> CheckAvailability(
        Guid outletId,
        [FromQuery] DateTime forTime,
        [FromQuery] int partySize = 2,
        [FromQuery] int durationMinutes = 0)
        => Run(() => reservations.CheckAvailabilityAsync(outletId, forTime, partySize, durationMinutes));

    // ── Waitlist ─────────────────────────────────────────────────────────────

    [HttpGet("waitlist/{outletId:guid}")]
    public Task<IActionResult> GetWaitlist(Guid outletId, [FromQuery] bool activeOnly = true)
        => Run(() => reservations.GetWaitlistAsync(outletId, activeOnly));

    [HttpPost("waitlist")]
    public Task<IActionResult> AddToWaitlist([FromBody] SaveWaitlistEntryDto request)
        => Run(() => reservations.SaveWaitlistEntryAsync(null, request, UserId), "Added to the waitlist.");

    [HttpPut("waitlist/{id:guid}")]
    public Task<IActionResult> UpdateWaitlist(Guid id, [FromBody] SaveWaitlistEntryDto request)
        => Run(() => reservations.SaveWaitlistEntryAsync(id, request, UserId), "Waitlist entry saved.");

    [HttpPost("waitlist/{id:guid}/status")]
    public Task<IActionResult> ChangeWaitlistStatus(Guid id, [FromBody] ChangeWaitlistStatusDto request)
        => Run(() => reservations.ChangeWaitlistStatusAsync(id, request, UserId), "Waitlist updated.");

    // ── Guests ───────────────────────────────────────────────────────────────

    [HttpGet("guests")]
    public Task<IActionResult> GetGuests([FromQuery] string? search, [FromQuery] bool vipOnly = false)
        => Run(async () =>
            await db.Guests.ForTenant(tenant)
                .WhereIf(vipOnly, g => g.IsVip)
                .WhereIf(!string.IsNullOrWhiteSpace(search),
                    g => g.FullName.ToLower().Contains(search!.ToLower())
                      || (g.Phone != null && g.Phone.Contains(search!)))
                .OrderByDescending(g => g.LastVisitAt)
                .Take(200)
                .Select(g => RestaurantMapper.ToDto(g))
                .ToListAsync());

    [HttpGet("guests/{id:guid}")]
    public Task<IActionResult> GetGuest(Guid id) => RunFound(async () =>
    {
        var guest = await db.Guests.ForTenant(tenant).FirstOrDefaultAsync(g => g.Id == id);
        return guest is null ? null : RestaurantMapper.ToDto(guest);
    }, "Guest not found.");

    [HttpPost("guests")]
    public Task<IActionResult> CreateGuest([FromBody] SaveGuestProfileDto request) => Run(async () =>
    {
        var guest = new GuestProfile().StampNew(tenant, UserId);
        ApplyGuest(guest, request);
        db.Guests.Add(guest);
        await db.SaveChangesAsync();
        return RestaurantMapper.ToDto(guest);
    }, "Guest added.");

    [HttpPut("guests/{id:guid}")]
    public Task<IActionResult> UpdateGuest(Guid id, [FromBody] SaveGuestProfileDto request) => Run(async () =>
    {
        var guest = await db.Guests.ForTenant(tenant).FirstOrDefaultAsync(g => g.Id == id)
            ?? throw new InvalidOperationException("Guest not found.");

        ApplyGuest(guest, request);
        guest.StampUpdated(UserId);
        await db.SaveChangesAsync();
        return RestaurantMapper.ToDto(guest);
    }, "Guest saved.");

    [HttpDelete("guests/{id:guid}")]
    public Task<IActionResult> DeleteGuest(Guid id) => Run(async () =>
    {
        var guest = await db.Guests.ForTenant(tenant).FirstOrDefaultAsync(g => g.Id == id)
            ?? throw new InvalidOperationException("Guest not found.");

        guest.StampDeleted(UserId);
        await db.SaveChangesAsync();
    }, "Guest removed.");

    private static void ApplyGuest(GuestProfile guest, SaveGuestProfileDto request)
    {
        guest.FullName = request.FullName;
        guest.Phone = request.Phone;
        guest.Email = request.Email;
        guest.CustomerId = request.CustomerId;
        guest.Birthday = request.Birthday;
        guest.Anniversary = request.Anniversary;
        guest.DietaryPreferences = request.DietaryPreferences;
        guest.Allergies = request.Allergies;
        guest.FavouriteItems = request.FavouriteItems;
        guest.PreferredSeating = request.PreferredSeating;
        guest.IsVip = request.IsVip;
        guest.IsBlacklisted = request.IsBlacklisted;
        guest.BlacklistReason = request.BlacklistReason;
        guest.LoyaltyTier = request.LoyaltyTier;
        guest.Notes = request.Notes;
        guest.IsActive = request.IsActive;
    }

    // ── Feedback ─────────────────────────────────────────────────────────────

    [HttpGet("feedback/{outletId:guid}")]
    public Task<IActionResult> GetFeedback(
        Guid outletId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] bool unresolvedOnly = false)
        => Run(async () =>
        {
            var start = from ?? DateTime.UtcNow.Date.AddDays(-30);
            var end = to ?? DateTime.UtcNow;

            var rows = await db.Feedback.ForTenant(tenant)
                .Where(f => f.OutletId == outletId && f.SubmittedAt >= start && f.SubmittedAt <= end)
                .WhereIf(unresolvedOnly, f => !f.IsResolved)
                .OrderByDescending(f => f.SubmittedAt)
                .ToListAsync();

            var waiterNames = await db.Staff.ForTenant(tenant)
                .ToDictionaryAsync(s => s.Id, s => s.DisplayName ?? s.FullName);

            var tableNumbers = await db.Tables.ForTenant(tenant)
                .Where(t => t.OutletId == outletId)
                .ToDictionaryAsync(t => t.Id, t => t.TableNumber);

            var orderNumbers = await db.Orders.ForTenant(tenant)
                .Where(o => rows.Select(f => f.OrderId).Contains(o.Id))
                .ToDictionaryAsync(o => o.Id, o => o.OrderNumber);

            return rows.Select(f =>
            {
                var dto = RestaurantMapper.ToDto(f);
                if (f.WaiterId.HasValue) dto.WaiterName = waiterNames.GetValueOrDefault(f.WaiterId.Value);
                if (f.TableId.HasValue) dto.TableNumber = tableNumbers.GetValueOrDefault(f.TableId.Value);
                if (f.OrderId.HasValue) dto.OrderNumber = orderNumbers.GetValueOrDefault(f.OrderId.Value);
                return dto;
            }).ToList();
        });

    [HttpPost("feedback")]
    public Task<IActionResult> SubmitFeedback([FromBody] SaveFeedbackDto request) => Run(async () =>
    {
        if (request.OverallRating is < 1 or > 5)
            throw new InvalidOperationException("An overall rating between 1 and 5 is required.");

        var feedback = new CustomerFeedback
        {
            OutletId = request.OutletId,
            OrderId = request.OrderId,
            CheckId = request.CheckId,
            GuestProfileId = request.GuestProfileId,
            WaiterId = request.WaiterId,
            TableId = request.TableId,
            OverallRating = request.OverallRating,
            FoodRating = request.FoodRating,
            ServiceRating = request.ServiceRating,
            AmbienceRating = request.AmbienceRating,
            ValueRating = request.ValueRating,
            Comment = request.Comment,
            GuestName = request.GuestName,
            Phone = request.Phone,
            SubmittedAt = DateTime.UtcNow,
        }.StampNew(tenant, UserId);

        db.Feedback.Add(feedback);
        await db.SaveChangesAsync();
        return RestaurantMapper.ToDto(feedback);
    }, "Thank you for the feedback.");

    [HttpPost("feedback/{id:guid}/resolve")]
    public Task<IActionResult> ResolveFeedback(Guid id, [FromBody] string? note) => Run(async () =>
    {
        var feedback = await db.Feedback.ForTenant(tenant).FirstOrDefaultAsync(f => f.Id == id)
            ?? throw new InvalidOperationException("Feedback not found.");

        feedback.IsResolved = true;
        feedback.ResolutionNote = note;
        feedback.ResolvedAt = DateTime.UtcNow;
        feedback.StampUpdated(UserId);

        await db.SaveChangesAsync();
        return RestaurantMapper.ToDto(feedback);
    }, "Marked as resolved.");
}
