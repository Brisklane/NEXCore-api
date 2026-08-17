using Microsoft.EntityFrameworkCore;
using Restaurant.Application.DTOs;
using Restaurant.Application.Services.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Infrastructure.Services;

/// <summary>
/// Bookings and the walk-in queue.
///
/// The availability engine is the substance here. A reservation is not a point in time — it
/// occupies a table for a duration, so two bookings clash when their windows overlap, not when
/// their start times match. Getting that wrong double-books a table at 19:00 and 19:30 and the
/// host finds out when the second party arrives.
/// </summary>
public class ReservationService(
    RestaurantDbContext db,
    IRestaurantTenant tenant,
    RestaurantNumbering numbering) : IReservationService
{
    // ── Reservations ─────────────────────────────────────────────────────────

    public async Task<List<ReservationDto>> GetReservationsAsync(
        Guid outletId, DateTime from, DateTime to, ReservationStatus? status)
    {
        var now = DateTime.UtcNow;

        var reservations = await db.Reservations.ForTenant(tenant)
            .Where(r => r.OutletId == outletId && r.ReservedFor >= from && r.ReservedFor <= to)
            .WhereIf(status.HasValue, r => r.Status == status)
            .OrderBy(r => r.ReservedFor)
            .ToListAsync();

        return await DecorateAsync(reservations, now);
    }

    public async Task<ReservationDto?> GetReservationAsync(Guid reservationId)
    {
        var reservation = await db.Reservations.ForTenant(tenant)
            .FirstOrDefaultAsync(r => r.Id == reservationId);

        if (reservation is null) return null;
        return (await DecorateAsync([reservation], DateTime.UtcNow)).First();
    }

    private async Task<List<ReservationDto>> DecorateAsync(List<Reservation> reservations, DateTime now)
    {
        if (reservations.Count == 0) return [];

        var tableIds = reservations.Where(r => r.TableId.HasValue).Select(r => r.TableId!.Value).Distinct().ToList();
        var tables = await db.Tables.ForTenant(tenant)
            .Where(t => tableIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, t => new { t.TableNumber, t.SectionId });

        var sectionNames = await db.Sections.ForTenant(tenant).ToDictionaryAsync(s => s.Id, s => s.Name);

        var guestIds = reservations.Where(r => r.GuestProfileId.HasValue)
            .Select(r => r.GuestProfileId!.Value).Distinct().ToList();

        var vipGuests = await db.Guests.ForTenant(tenant)
            .Where(g => guestIds.Contains(g.Id) && g.IsVip)
            .Select(g => g.Id)
            .ToListAsync();

        return reservations.Select(r =>
        {
            var dto = RestaurantMapper.ToDto(r, now);

            if (r.TableId.HasValue && tables.TryGetValue(r.TableId.Value, out var table))
            {
                dto.TableNumber = table.TableNumber;
                dto.SectionId ??= table.SectionId;
            }

            if (dto.SectionId.HasValue) dto.SectionName = sectionNames.GetValueOrDefault(dto.SectionId.Value);
            dto.IsVipGuest = r.GuestProfileId.HasValue && vipGuests.Contains(r.GuestProfileId.Value);

            return dto;
        }).ToList();
    }

    public async Task<ReservationDto> SaveReservationAsync(Guid? id, SaveReservationDto request, Guid userId)
    {
        var now = DateTime.UtcNow;
        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync() ?? new RestaurantSettings();

        Reservation reservation;

        if (id.HasValue)
        {
            reservation = await db.Reservations.ForTenant(tenant).FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new InvalidOperationException("Reservation not found.");

            if (reservation.Status is ReservationStatus.Seated or ReservationStatus.Completed)
                throw new InvalidOperationException("This booking has already been seated and cannot be changed.");

            reservation.StampUpdated(userId);
        }
        else
        {
            reservation = new Reservation
            {
                ReservationNumber = await numbering.NextReservationNumberAsync(request.OutletId, now),
                Status = ReservationStatus.Requested,
            }.StampNew(tenant, userId);

            reservation.Code = reservation.ReservationNumber;
            db.Reservations.Add(reservation);
        }

        var duration = request.DurationMinutes > 0 ? request.DurationMinutes : settings.DefaultReservationDuration;

        // A table already promised to somebody else is the one thing a booking screen must never
        // allow, so the clash check runs on save rather than only on the availability lookup.
        if (request.TableId.HasValue)
        {
            var clash = await HasClashAsync(request.OutletId, request.TableId.Value,
                                            request.ReservedFor, duration, reservation.Id);

            if (clash)
                throw new InvalidOperationException(
                    "That table is already booked for an overlapping time. Pick another table or time.");
        }

        if (settings.RequireDepositForLargeParty
            && request.PartySize >= settings.LargePartyThreshold
            && request.DepositAmount <= 0)
            throw new InvalidOperationException(
                $"A deposit is required for parties of {settings.LargePartyThreshold} or more.");

        reservation.OutletId = request.OutletId;
        reservation.GuestProfileId = request.GuestProfileId;
        reservation.GuestName = request.GuestName;
        reservation.Phone = request.Phone;
        reservation.Email = request.Email;
        reservation.PartySize = Math.Max(1, request.PartySize);
        reservation.ReservedFor = request.ReservedFor;
        reservation.DurationMinutes = duration;
        reservation.TableId = request.TableId;
        reservation.SectionId = request.SectionId;
        reservation.FloorId = request.FloorId;
        reservation.Occasion = request.Occasion;
        reservation.SpecialRequests = request.SpecialRequests;
        reservation.AllergyNotes = request.AllergyNotes;
        reservation.IsHighChairNeeded = request.IsHighChairNeeded;
        reservation.IsWheelchairAccess = request.IsWheelchairAccess;
        reservation.DepositAmount = request.DepositAmount;
        reservation.IsDepositPaid = request.IsDepositPaid;
        if (request.IsDepositPaid) reservation.DepositPaidAt ??= now;
        reservation.Source = request.Source;
        reservation.Note = request.Note;

        await db.SaveChangesAsync();
        return (await GetReservationAsync(reservation.Id))!;
    }

    public async Task<ReservationDto> ChangeStatusAsync(
        Guid reservationId, ChangeReservationStatusDto request, Guid userId)
    {
        var reservation = await db.Reservations.ForTenant(tenant).FirstOrDefaultAsync(r => r.Id == reservationId)
            ?? throw new InvalidOperationException("Reservation not found.");

        var now = DateTime.UtcNow;

        if (request.TableId.HasValue && request.TableId != reservation.TableId)
        {
            var clash = await HasClashAsync(reservation.OutletId, request.TableId.Value,
                                            reservation.ReservedFor, reservation.DurationMinutes, reservation.Id);

            if (clash) throw new InvalidOperationException("That table is already booked for an overlapping time.");
            reservation.TableId = request.TableId;
        }

        reservation.Status = request.Status;

        switch (request.Status)
        {
            case ReservationStatus.Confirmed:
                reservation.ConfirmedAt ??= now;
                break;

            case ReservationStatus.Seated:
                reservation.SeatedAt ??= now;
                if (reservation.TableId.HasValue)
                {
                    var table = await db.Tables.ForTenant(tenant)
                        .FirstOrDefaultAsync(t => t.Id == reservation.TableId);

                    if (table is not null && table.State is TableState.Free or TableState.Reserved)
                    {
                        table.State = TableState.Seated;
                        table.CurrentGuestCount = reservation.PartySize;
                        table.SeatedAt = now;
                        table.StateChangedAt = now;
                        table.StampUpdated(userId);
                    }
                }
                break;

            case ReservationStatus.Completed:
                reservation.CompletedAt ??= now;
                break;

            case ReservationStatus.Cancelled:
                reservation.CancelledAt = now;
                reservation.CancelReason = request.Reason;
                await ReleaseTableAsync(reservation, userId, now);
                break;

            case ReservationStatus.NoShow:
                reservation.CancelledAt = now;
                reservation.CancelReason = request.Reason ?? "No show";
                await ReleaseTableAsync(reservation, userId, now);

                // A no-show history is what lets a host decide whether to take a deposit next
                // time, so it is recorded against the guest, not just the booking.
                if (reservation.GuestProfileId.HasValue)
                {
                    var guest = await db.Guests.ForTenant(tenant)
                        .FirstOrDefaultAsync(g => g.Id == reservation.GuestProfileId);

                    if (guest is not null)
                    {
                        guest.NoShowCount++;
                        guest.StampUpdated(userId);
                    }
                }
                break;
        }

        reservation.StampUpdated(userId);
        await db.SaveChangesAsync();
        return (await GetReservationAsync(reservation.Id))!;
    }

    private async Task ReleaseTableAsync(Reservation reservation, Guid userId, DateTime now)
    {
        if (reservation.TableId is null) return;

        var table = await db.Tables.ForTenant(tenant).FirstOrDefaultAsync(t => t.Id == reservation.TableId);
        if (table is null || table.State != TableState.Reserved) return;

        table.State = TableState.Free;
        table.StateChangedAt = now;
        table.StampUpdated(userId);
    }

    /// <summary>
    /// True when this table already has a booking whose window overlaps the requested one.
    /// Two intervals overlap when each starts before the other ends — the standard test, and the
    /// reason a booking has to carry a duration at all.
    /// </summary>
    private async Task<bool> HasClashAsync(
        Guid outletId, Guid tableId, DateTime start, int durationMinutes, Guid excludeId)
    {
        var end = start.AddMinutes(durationMinutes);

        var live = new[] { ReservationStatus.Requested, ReservationStatus.Confirmed, ReservationStatus.Seated };

        var candidates = await db.Reservations.ForTenant(tenant)
            .Where(r => r.OutletId == outletId
                     && r.TableId == tableId
                     && r.Id != excludeId
                     && live.Contains(r.Status)
                     && r.ReservedFor < end.AddHours(6)
                     && r.ReservedFor > start.AddHours(-6))
            .Select(r => new { r.ReservedFor, r.DurationMinutes })
            .ToListAsync();

        return candidates.Any(r => r.ReservedFor < end && r.ReservedFor.AddMinutes(r.DurationMinutes) > start);
    }

    public async Task<ReservationAvailabilityDto> CheckAvailabilityAsync(
        Guid outletId, DateTime forTime, int partySize, int durationMinutes)
    {
        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync() ?? new RestaurantSettings();
        var duration = durationMinutes > 0 ? durationMinutes : settings.DefaultReservationDuration;
        var end = forTime.AddMinutes(duration);

        var tables = await db.Tables.ForTenant(tenant)
            .Where(t => t.OutletId == outletId && t.IsActive && t.State != TableState.Blocked)
            .ToListAsync();

        var live = new[] { ReservationStatus.Requested, ReservationStatus.Confirmed, ReservationStatus.Seated };

        var sameDay = await db.Reservations.ForTenant(tenant)
            .Where(r => r.OutletId == outletId
                     && live.Contains(r.Status)
                     && r.ReservedFor >= forTime.Date
                     && r.ReservedFor < forTime.Date.AddDays(1))
            .ToListAsync();

        var conflicting = sameDay
            .Where(r => r.ReservedFor < end && r.ReservedFor.AddMinutes(r.DurationMinutes) > forTime)
            .ToList();

        var takenTableIds = conflicting.Where(r => r.TableId.HasValue).Select(r => r.TableId!.Value).ToHashSet();

        var sectionNames = await db.Sections.ForTenant(tenant).ToDictionaryAsync(s => s.Id, s => s.Name);
        var floorNames = await db.Floors.ForTenant(tenant).ToDictionaryAsync(f => f.Id, f => f.Name);

        var available = tables
            .Where(t => !takenTableIds.Contains(t.Id))
            .Where(t => t.Seats >= partySize)
            .Where(t => t.MaxPartySize is null || partySize <= t.MaxPartySize)
            .Where(t => t.MinPartySize is null || partySize >= t.MinPartySize)
            .Select(t => new AvailableTableDto
            {
                TableId = t.Id,
                TableNumber = t.TableNumber,
                Seats = t.Seats,
                SectionId = t.SectionId,
                SectionName = t.SectionId.HasValue ? sectionNames.GetValueOrDefault(t.SectionId.Value) : null,
                FloorName = floorNames.GetValueOrDefault(t.FloorId),
                CurrentState = t.State,
                // Prefer the tightest fit. Seating two people at a ten-top on a Saturday costs
                // the restaurant the eight covers it could have sold instead.
                FitScore = 100 - (t.Seats - partySize) * 10,
            })
            .OrderByDescending(t => t.FitScore)
            .ThenBy(t => t.TableNumber)
            .ToList();

        var alternatives = new List<DateTime>();
        if (available.Count == 0)
        {
            // Offer the nearest workable slots either side rather than a flat "no".
            foreach (var offset in new[] { -60, -30, 30, 60, 90, 120 })
            {
                var candidate = forTime.AddMinutes(offset);
                var candidateEnd = candidate.AddMinutes(duration);

                var busy = sameDay
                    .Where(r => r.ReservedFor < candidateEnd && r.ReservedFor.AddMinutes(r.DurationMinutes) > candidate)
                    .Where(r => r.TableId.HasValue)
                    .Select(r => r.TableId!.Value)
                    .ToHashSet();

                if (tables.Any(t => !busy.Contains(t.Id) && t.Seats >= partySize))
                    alternatives.Add(candidate);
            }
        }

        return new ReservationAvailabilityDto
        {
            OutletId = outletId,
            RequestedFor = forTime,
            PartySize = partySize,
            DurationMinutes = duration,
            AvailableTables = available,
            ConflictingReservations = await DecorateAsync(conflicting, DateTime.UtcNow),
            AlternativeTimes = alternatives,
        };
    }

    // ── Waitlist ─────────────────────────────────────────────────────────────

    public async Task<List<WaitlistEntryDto>> GetWaitlistAsync(Guid outletId, bool activeOnly = true)
    {
        var now = DateTime.UtcNow;
        var waiting = new[] { WaitlistStatus.Waiting, WaitlistStatus.Notified };

        var entries = await db.Waitlist.ForTenant(tenant)
            .Where(w => w.OutletId == outletId)
            .WhereIf(activeOnly, w => waiting.Contains(w.Status))
            .WhereIf(!activeOnly, w => w.JoinedAt >= now.Date)
            .OrderBy(w => w.JoinedAt)
            .ToListAsync();

        var tableNumbers = await db.Tables.ForTenant(tenant)
            .Where(t => t.OutletId == outletId)
            .ToDictionaryAsync(t => t.Id, t => t.TableNumber);

        var sectionNames = await db.Sections.ForTenant(tenant).ToDictionaryAsync(s => s.Id, s => s.Name);

        var position = 0;
        return entries.Select(e =>
        {
            var dto = RestaurantMapper.ToDto(e, now);
            if (e.Status is WaitlistStatus.Waiting or WaitlistStatus.Notified) dto.Position = ++position;
            if (e.TableId.HasValue) dto.TableNumber = tableNumbers.GetValueOrDefault(e.TableId.Value);
            if (e.PreferredSectionId.HasValue)
                dto.PreferredSectionName = sectionNames.GetValueOrDefault(e.PreferredSectionId.Value);
            return dto;
        }).ToList();
    }

    public async Task<WaitlistEntryDto> SaveWaitlistEntryAsync(Guid? id, SaveWaitlistEntryDto request, Guid userId)
    {
        WaitlistEntry entry;

        if (id.HasValue)
        {
            entry = await db.Waitlist.ForTenant(tenant).FirstOrDefaultAsync(w => w.Id == id)
                ?? throw new InvalidOperationException("Waitlist entry not found.");
            entry.StampUpdated(userId);
        }
        else
        {
            entry = new WaitlistEntry
            {
                Status = WaitlistStatus.Waiting,
                JoinedAt = DateTime.UtcNow,
            }.StampNew(tenant, userId);

            db.Waitlist.Add(entry);
        }

        entry.OutletId = request.OutletId;
        entry.GuestName = request.GuestName;
        entry.Phone = request.Phone;
        entry.PartySize = Math.Max(1, request.PartySize);
        entry.PreferredSectionId = request.PreferredSectionId;
        entry.GuestProfileId = request.GuestProfileId;
        entry.PagerNumber = request.PagerNumber;
        entry.Note = request.Note;

        // A quote of zero would report every wait as over-quote; estimate from the queue instead.
        entry.QuotedWaitMinutes = request.QuotedWaitMinutes > 0
            ? request.QuotedWaitMinutes
            : await EstimateWaitAsync(request.OutletId, request.PartySize);

        await db.SaveChangesAsync();
        return RestaurantMapper.ToDto(entry, DateTime.UtcNow);
    }

    /// <summary>
    /// Rough wait estimate: how many parties are ahead that need a comparable table, times the
    /// venue's average dining time, divided by how many such tables exist.
    /// </summary>
    private async Task<int> EstimateWaitAsync(Guid outletId, int partySize)
    {
        var outlet = await db.Outlets.ForTenant(tenant).FirstOrDefaultAsync(o => o.Id == outletId);
        var averageDining = outlet?.AverageDiningMinutes ?? 60;

        var suitableTables = await db.Tables.ForTenant(tenant)
            .CountAsync(t => t.OutletId == outletId && t.IsActive && t.Seats >= partySize);

        if (suitableTables == 0) return averageDining;

        var freeNow = await db.Tables.ForTenant(tenant)
            .CountAsync(t => t.OutletId == outletId && t.IsActive && t.Seats >= partySize && t.State == TableState.Free);

        if (freeNow > 0) return 0;

        var ahead = await db.Waitlist.ForTenant(tenant)
            .CountAsync(w => w.OutletId == outletId && w.Status == WaitlistStatus.Waiting && w.PartySize <= partySize + 2);

        return (int)Math.Ceiling((ahead + 1) / (double)suitableTables * averageDining);
    }

    public async Task<WaitlistEntryDto> ChangeWaitlistStatusAsync(
        Guid entryId, ChangeWaitlistStatusDto request, Guid userId)
    {
        var entry = await db.Waitlist.ForTenant(tenant).FirstOrDefaultAsync(w => w.Id == entryId)
            ?? throw new InvalidOperationException("Waitlist entry not found.");

        var now = DateTime.UtcNow;
        entry.Status = request.Status;
        if (request.Note is not null) entry.Note = request.Note;

        switch (request.Status)
        {
            case WaitlistStatus.Notified: entry.NotifiedAt ??= now; break;
            case WaitlistStatus.Seated:
                entry.SeatedAt ??= now;
                entry.TableId = request.TableId ?? entry.TableId;
                break;
            case WaitlistStatus.Left:
            case WaitlistStatus.Cancelled:
                entry.LeftAt ??= now;
                break;
        }

        entry.StampUpdated(userId);
        await db.SaveChangesAsync();
        return RestaurantMapper.ToDto(entry, now);
    }
}
