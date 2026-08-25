using Fitness.Application.DTOs;
using Fitness.Application.Services.Interfaces;
using Fitness.Domain.Entities;
using Fitness.Domain.Enums;
using Fitness.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fitness.Infrastructure.Services;

/// <summary>
/// Clubs, their opening hours, the areas and rooms inside them, and the tenant-wide settings.
///
/// Everything here is configuration rather than operation, which is why it is one service rather
/// than five: an admin sets a club up once and then never opens these screens again.
/// </summary>
public class ClubService(
    FitnessDbContext db,
    IFitnessTenant tenant,
    FitnessInitializationService initializer) : IClubService
{
    public async Task<List<ClubDto>> GetClubsAsync(bool activeOnly)
    {
        var clubs = await db.Clubs.ForTenant(tenant)
            .WhereIf(activeOnly, c => c.IsActive)
            .Include(c => c.Schedules.Where(s => !s.IsDeleted))
            .OrderBy(c => c.Name)
            .ToListAsync();

        var ids = clubs.Select(c => c.Id).ToList();
        var now = DateTime.UtcNow;
        var today = now.Date;

        // Three grouped counts rather than three-per-club, so a twelve-site brand is four queries
        // rather than thirty-seven.
        var memberCounts = await db.Members.ForTenant(tenant)
            .Where(m => ids.Contains(m.HomeClubId) && m.Status == MemberStatus.Active)
            .GroupBy(m => m.HomeClubId)
            .Select(g => new { ClubId = g.Key, Count = g.Count() })
            .ToListAsync();

        var inClub = await db.CheckIns.ForTenant(tenant)
            .Where(c => ids.Contains(c.ClubId) && c.CheckedOutAt == null && c.CheckedInAt >= today)
            .GroupBy(c => c.ClubId)
            .Select(g => new { ClubId = g.Key, Count = g.Count() })
            .ToListAsync();

        var classesToday = await db.ClassOccurrences.ForTenant(tenant)
            .Where(o => ids.Contains(o.ClubId)
                     && o.StartsAt >= today && o.StartsAt < today.AddDays(1)
                     && o.Status != ClassOccurrenceStatus.Cancelled)
            .GroupBy(o => o.ClubId)
            .Select(g => new { ClubId = g.Key, Count = g.Count() })
            .ToListAsync();

        return [.. clubs.Select(c =>
        {
            var dto = FitnessMapper.ToDto(c);
            dto.ActiveMemberCount = memberCounts.FirstOrDefault(x => x.ClubId == c.Id)?.Count ?? 0;
            dto.InClubNow = inClub.FirstOrDefault(x => x.ClubId == c.Id)?.Count ?? 0;
            dto.ClassesToday = classesToday.FirstOrDefault(x => x.ClubId == c.Id)?.Count ?? 0;
            dto.IsOpenNow = IsOpenAt(c, now);
            return dto;
        })];
    }

    public async Task<ClubDto?> GetClubAsync(Guid clubId)
    {
        var club = await db.Clubs.ForTenant(tenant)
            .Include(c => c.Schedules.Where(s => !s.IsDeleted))
            .FirstOrDefaultAsync(c => c.Id == clubId);

        if (club is null) return null;

        var dto = FitnessMapper.ToDto(club);
        dto.IsOpenNow = IsOpenAt(club, DateTime.UtcNow);
        return dto;
    }

    public async Task<ClubDto> SaveClubAsync(Guid? id, SaveClubDto request, Guid userId)
    {
        FitnessClub club;

        if (id is null)
        {
            club = new FitnessClub().StampNew(tenant, userId);
            db.Clubs.Add(club);
        }
        else
        {
            club = await db.Clubs.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new InvalidOperationException("Club not found.");
            club.StampUpdated(userId);
        }

        Apply(club, request);
        await db.SaveChangesAsync();

        // A brand-new club needs its default hours, or the door refuses everyone on day one.
        if (id is null) await SeedDefaultScheduleAsync(club, userId);

        return (await GetClubAsync(club.Id))!;
    }

    public async Task DeleteClubAsync(Guid clubId, Guid userId)
    {
        var club = await db.Clubs.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == clubId)
            ?? throw new InvalidOperationException("Club not found.");

        var activeMembers = await db.Members.ForTenant(tenant)
            .CountAsync(m => m.HomeClubId == clubId && m.Status == MemberStatus.Active);

        if (activeMembers > 0)
            throw new InvalidOperationException(
                $"{activeMembers} active members still belong to this club. Move them to another club first, " +
                "or mark this one inactive instead of deleting it.");

        club.StampDeleted(userId);
        await db.SaveChangesAsync();
    }

    // ── Schedules & closures ─────────────────────────────────────────────────

    public async Task<List<ClubScheduleDto>> GetSchedulesAsync(Guid clubId)
    {
        var schedules = await db.ClubSchedules.ForTenant(tenant)
            .Where(s => s.ClubId == clubId)
            .OrderBy(s => s.OverrideDate ?? DateTime.MinValue)
            .ThenBy(s => s.DayOfWeek)
            .ToListAsync();

        return [.. schedules.Select(FitnessMapper.ToDto)];
    }

    public async Task<List<ClubScheduleDto>> SaveSchedulesAsync(Guid clubId, List<ClubScheduleDto> schedules, Guid userId)
    {
        var existing = await db.ClubSchedules.ForTenant(tenant)
            .Where(s => s.ClubId == clubId)
            .ToListAsync();

        var keptIds = schedules.Where(s => s.Id != Guid.Empty).Select(s => s.Id).ToHashSet();
        foreach (var gone in existing.Where(e => !keptIds.Contains(e.Id)))
            gone.StampDeleted(userId);

        foreach (var dto in schedules)
        {
            var row = existing.FirstOrDefault(e => e.Id == dto.Id);
            if (row is null)
            {
                row = new ClubSchedule { ClubId = clubId }.StampNew(tenant, userId);
                db.ClubSchedules.Add(row);
            }
            else
            {
                row.StampUpdated(userId);
            }

            row.DayOfWeek = dto.DayOfWeek;
            row.OverrideDate = dto.OverrideDate;
            row.OpensAt = dto.OpensAt;
            row.ClosesAt = dto.ClosesAt;
            row.StaffedFrom = dto.StaffedFrom;
            row.StaffedTo = dto.StaffedTo;
            row.IsClosed = dto.IsClosed;
            row.Note = dto.Note;
        }

        await db.SaveChangesAsync();
        return await GetSchedulesAsync(clubId);
    }

    public async Task<List<ClubClosureDto>> GetClosuresAsync(Guid clubId, bool upcomingOnly)
    {
        var today = DateTime.UtcNow.Date;

        var closures = await db.ClubClosures.ForTenant(tenant)
            .Where(c => c.ClubId == clubId)
            .WhereIf(upcomingOnly, c => c.EndsOn >= today)
            .OrderBy(c => c.StartsOn)
            .ToListAsync();

        var dtos = closures.Select(FitnessMapper.ToDto).ToList();

        // Show the blast radius, so a manager knows what publishing this will cancel.
        foreach (var dto in dtos.Where(d => d.CancelsClasses))
        {
            dto.ClassesAffected = await db.ClassOccurrences.ForTenant(tenant)
                .CountAsync(o => o.ClubId == clubId
                              && o.StartsAt >= dto.StartsOn && o.StartsAt <= dto.EndsOn
                              && o.Status != ClassOccurrenceStatus.Cancelled);

            dto.BookingsAffected = await db.ClassBookings.ForTenant(tenant)
                .CountAsync(k => k.ClassOccurrence!.ClubId == clubId
                              && k.ClassOccurrence.StartsAt >= dto.StartsOn
                              && k.ClassOccurrence.StartsAt <= dto.EndsOn
                              && k.Status == BookingStatus.Booked);
        }

        return dtos;
    }

    public async Task<ClubClosureDto> SaveClosureAsync(Guid? id, ClubClosureDto request, Guid userId)
    {
        if (request.EndsOn < request.StartsOn)
            throw new InvalidOperationException("A closure cannot end before it starts.");

        ClubClosure closure;
        if (id is null)
        {
            closure = new ClubClosure { ClubId = request.ClubId }.StampNew(tenant, userId);
            db.ClubClosures.Add(closure);
        }
        else
        {
            closure = await db.ClubClosures.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new InvalidOperationException("Closure not found.");
            closure.StampUpdated(userId);
        }

        closure.StartsOn = request.StartsOn.Date;
        closure.EndsOn = request.EndsOn.Date;
        closure.Reason = request.Reason;
        closure.MemberNotice = request.MemberNotice;
        closure.CancelsClasses = request.CancelsClasses;
        closure.ExtendsAgreements = request.ExtendsAgreements;
        closure.BlocksAccess = request.BlocksAccess;

        await db.SaveChangesAsync();

        if (closure.CancelsClasses) await CancelClassesInClosureAsync(closure, userId);

        return FitnessMapper.ToDto(closure);
    }

    // ── Areas & rooms ────────────────────────────────────────────────────────

    public async Task<List<ClubAreaDto>> GetAreasAsync(Guid clubId)
    {
        var areas = await db.Areas.ForTenant(tenant)
            .Where(a => a.ClubId == clubId)
            .OrderBy(a => a.DisplayOrder).ThenBy(a => a.Name)
            .ToListAsync();

        var doorCounts = await db.Doors.ForTenant(tenant)
            .Where(d => d.ClubId == clubId && d.AreaId != null)
            .GroupBy(d => d.AreaId!.Value)
            .Select(g => new { AreaId = g.Key, Count = g.Count() })
            .ToListAsync();

        return [.. areas.Select(a =>
        {
            var dto = FitnessMapper.ToDto(a);
            dto.DoorCount = doorCounts.FirstOrDefault(d => d.AreaId == a.Id)?.Count ?? 0;
            return dto;
        })];
    }

    public async Task<ClubAreaDto> SaveAreaAsync(Guid? id, ClubAreaDto request, Guid userId)
    {
        ClubArea area;
        if (id is null)
        {
            area = new ClubArea { ClubId = request.ClubId }.StampNew(tenant, userId);
            db.Areas.Add(area);
        }
        else
        {
            area = await db.Areas.ForTenant(tenant).FirstOrDefaultAsync(a => a.Id == id)
                ?? throw new InvalidOperationException("Area not found.");
            area.StampUpdated(userId);
        }

        area.Name = request.Name;
        area.Kind = request.Kind;
        area.DisplayOrder = request.DisplayOrder;
        area.Capacity = request.Capacity;
        area.RequiresEntitlement = request.RequiresEntitlement;
        area.MinimumAge = request.MinimumAge;
        area.MaxParticipantsPerStaff = request.MaxParticipantsPerStaff;
        area.IsOutOfService = request.IsOutOfService;
        area.OutOfServiceNote = request.OutOfServiceNote;
        area.IsActive = request.IsActive;

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(area);
    }

    public async Task<List<RoomDto>> GetRoomsAsync(Guid clubId)
    {
        var rooms = await db.Rooms.ForTenant(tenant)
            .Where(r => r.ClubId == clubId)
            .Include(r => r.Area)
            .Include(r => r.Spots.Where(s => !s.IsDeleted))
            .OrderBy(r => r.DisplayOrder).ThenBy(r => r.Name)
            .ToListAsync();

        return [.. rooms.Select(FitnessMapper.ToDto)];
    }

    /// <summary>
    /// Saves a room and its whole spot map together.
    ///
    /// One call because that is how the designer edits it — drag twenty-four bikes into a grid and
    /// press save. Twenty-five round trips would be both slower and capable of leaving the room
    /// half-mapped if one of them failed.
    /// </summary>
    public async Task<RoomDto> SaveRoomLayoutAsync(SaveRoomLayoutDto request, Guid userId)
    {
        Room room;
        if (request.RoomId is null)
        {
            room = new Room { ClubId = request.ClubId }.StampNew(tenant, userId);
            db.Rooms.Add(room);
        }
        else
        {
            room = await db.Rooms.ForTenant(tenant)
                .Include(r => r.Spots.Where(s => !s.IsDeleted))
                .FirstOrDefaultAsync(r => r.Id == request.RoomId)
                ?? throw new InvalidOperationException("Room not found.");
            room.StampUpdated(userId);
        }

        room.AreaId = request.AreaId;
        room.Name = request.Name;
        room.Capacity = request.Capacity;
        room.HasSpotMap = request.HasSpotMap;
        room.GridColumns = request.GridColumns;
        room.GridRows = request.GridRows;
        room.EquipmentNote = request.EquipmentNote;

        var existing = room.Spots.Where(s => !s.IsDeleted).ToList();
        var keptIds = request.Spots.Where(s => s.Id != Guid.Empty).Select(s => s.Id).ToHashSet();

        foreach (var gone in existing.Where(s => !keptIds.Contains(s.Id)))
        {
            // A spot with live bookings cannot simply vanish — somebody is on bike 14 tomorrow.
            var hasBookings = await db.ClassBookings.ForTenant(tenant)
                .AnyAsync(k => k.SpotId == gone.Id
                            && k.Status == BookingStatus.Booked
                            && k.ClassOccurrence!.StartsAt > DateTime.UtcNow);

            if (hasBookings)
                throw new InvalidOperationException(
                    $"Spot {gone.Label} has upcoming bookings. Move or cancel them before removing it.");

            gone.StampDeleted(userId);
        }

        foreach (var dto in request.Spots)
        {
            var spot = existing.FirstOrDefault(s => s.Id == dto.Id);
            if (spot is null)
            {
                spot = new RoomSpot { RoomId = room.Id }.StampNew(tenant, userId);
                db.RoomSpots.Add(spot);
            }
            else
            {
                spot.StampUpdated(userId);
            }

            spot.Label = dto.Label;
            spot.GridColumn = dto.GridColumn;
            spot.GridRow = dto.GridRow;
            spot.EquipmentAssetId = dto.EquipmentAssetId;
            spot.IsReserved = dto.IsReserved;
            spot.ReservedNote = dto.ReservedNote;
            spot.IsOutOfService = dto.IsOutOfService;
        }

        await db.SaveChangesAsync();

        var saved = await db.Rooms.ForTenant(tenant)
            .Include(r => r.Area)
            .Include(r => r.Spots.Where(s => !s.IsDeleted))
            .FirstAsync(r => r.Id == room.Id);

        return FitnessMapper.ToDto(saved);
    }

    // ── Settings ─────────────────────────────────────────────────────────────

    public async Task<FitnessSettingsDto> GetSettingsAsync()
    {
        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();

        if (settings is null)
        {
            settings = new FitnessSettings().StampNew(tenant);
            db.Settings.Add(settings);
            await db.SaveChangesAsync();
        }

        return FitnessMapper.ToDto(settings);
    }

    public async Task<FitnessSettingsDto> SaveSettingsAsync(FitnessSettingsDto request, Guid userId)
    {
        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();
        if (settings is null)
        {
            settings = new FitnessSettings().StampNew(tenant, userId);
            db.Settings.Add(settings);
        }
        else
        {
            settings.StampUpdated(userId);
        }

        settings.MemberNumberPrefix = request.MemberNumberPrefix;
        settings.DefaultNoticePeriodDays = request.DefaultNoticePeriodDays;
        settings.DefaultCoolingOffDays = request.DefaultCoolingOffDays;
        settings.MaxFreezeDaysPerYear = request.MaxFreezeDaysPerYear;
        settings.DefaultFreezeFeePerMonth = request.DefaultFreezeFeePerMonth;
        settings.DefaultBillingAnchor = request.DefaultBillingAnchor;
        settings.FixedBillingDayOfMonth = Math.Clamp(request.FixedBillingDayOfMonth, 1, 28);
        settings.DefaultProration = request.DefaultProration;
        settings.InvoiceGraceDays = request.InvoiceGraceDays;
        settings.DefaultLateFee = request.DefaultLateFee;
        settings.AutoRunBilling = request.AutoRunBilling;
        settings.BillingRunTime = request.BillingRunTime;
        settings.AccessCacheSeconds = request.AccessCacheSeconds;
        settings.CaptureImageOnDenial = request.CaptureImageOnDenial;
        settings.AbsenceRiskDays = request.AbsenceRiskDays;
        settings.CriticalAbsenceDays = request.CriticalAbsenceDays;
        settings.AutoScoreChurn = request.AutoScoreChurn;
        settings.QuietHoursFrom = request.QuietHoursFrom;
        settings.QuietHoursTo = request.QuietHoursTo;
        settings.RespectQuietHours = request.RespectQuietHours;
        settings.FromEmail = request.FromEmail;
        settings.FromName = request.FromName;
        settings.SmsSenderId = request.SmsSenderId;
        settings.LeadResponseSlaMinutes = request.LeadResponseSlaMinutes;
        settings.DiscountApprovalThresholdPercent = request.DiscountApprovalThresholdPercent;
        settings.RefundApprovalThreshold = request.RefundApprovalThreshold;
        settings.WriteOffApprovalThreshold = request.WriteOffApprovalThreshold;
        settings.RequirePinForOverrides = request.RequirePinForOverrides;

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(settings);
    }

    public Task EnsureProvisionedAsync(Guid companyId, Guid branchId, Guid businessUnitId, Guid userId, bool includeSampleData)
        => initializer.EnsureProvisionedAsync(companyId, branchId, businessUnitId, userId, includeSampleData);

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Whether the club is open right now, honouring a dated override over the weekday row.
    /// Shared with the access engine, so "closed" means the same thing at the door and on screen.
    /// </summary>
    internal static bool IsOpenAt(FitnessClub club, DateTime at)
    {
        if (club.IsTemporarilyClosed) return false;

        var schedules = club.Schedules.Where(s => !s.IsDeleted).ToList();
        if (schedules.Count == 0) return true;

        var todayOverride = schedules.FirstOrDefault(s => s.OverrideDate?.Date == at.Date);
        var schedule = todayOverride ?? schedules.FirstOrDefault(s => s.OverrideDate is null && s.DayOfWeek == (int)at.DayOfWeek);

        if (schedule is null) return false;
        if (schedule.IsClosed) return false;

        return FitnessQueryExtensions.WithinWindow(at.TimeOfDay, schedule.OpensAt, schedule.ClosesAt);
    }

    internal static bool IsStaffedAt(FitnessClub club, DateTime at)
    {
        var schedules = club.Schedules.Where(s => !s.IsDeleted).ToList();
        var todayOverride = schedules.FirstOrDefault(s => s.OverrideDate?.Date == at.Date);
        var schedule = todayOverride ?? schedules.FirstOrDefault(s => s.OverrideDate is null && s.DayOfWeek == (int)at.DayOfWeek);

        if (schedule is null || schedule.IsClosed) return false;

        // No staffed window recorded means the club is staffed whenever it is open.
        var from = schedule.StaffedFrom ?? schedule.OpensAt;
        var to = schedule.StaffedTo ?? schedule.ClosesAt;
        return FitnessQueryExtensions.WithinWindow(at.TimeOfDay, from, to);
    }

    private static void Apply(FitnessClub c, SaveClubDto r)
    {
        c.Code = r.Code;
        c.Name = r.Name;
        c.ClubType = r.ClubType;
        c.Phone = r.Phone;
        c.Email = r.Email;
        c.AddressLine = r.AddressLine;
        c.City = r.City;
        c.PostCode = r.PostCode;
        c.CountryCode = r.CountryCode;
        c.Latitude = r.Latitude;
        c.Longitude = r.Longitude;
        c.TimeZoneId = r.TimeZoneId;
        c.CurrencyCode = r.CurrencyCode;
        c.UnitSystem = r.UnitSystem;
        c.WarehouseId = r.WarehouseId;
        c.PosStoreId = r.PosStoreId;
        c.DefaultTaxGroupId = r.DefaultTaxGroupId;
        c.DefaultTaxPercent = r.DefaultTaxPercent;
        c.SoftCapacity = r.SoftCapacity;
        c.HardCapacity = r.HardCapacity;
        c.DefaultBookingPolicyId = r.DefaultBookingPolicyId;
        c.DefaultCancellationPolicyId = r.DefaultCancellationPolicyId;
        c.DefaultDunningPolicyId = r.DefaultDunningPolicyId;
        c.AccessBalanceThreshold = r.AccessBalanceThreshold;
        c.AntiPassback = r.AntiPassback;
        c.AntiPassbackMinutes = r.AntiPassbackMinutes;
        c.OfflinePolicy = r.OfflinePolicy;
        c.MinimumAge = r.MinimumAge;
        c.GuardianRequiredBelowAge = r.GuardianRequiredBelowAge;
        c.RequiresWaiver = r.RequiresWaiver;
        c.RequiresHealthScreening = r.RequiresHealthScreening;
        c.AllowsCrossClubVisits = r.AllowsCrossClubVisits;
        c.CrossClubVisitFee = r.CrossClubVisitFee;
        c.IsTemporarilyClosed = r.IsTemporarilyClosed;
        c.ClosureNote = r.ClosureNote;
        c.LogoUrl = r.LogoUrl;
        c.ReceiptFooter = r.ReceiptFooter;
        c.BrandCode = r.BrandCode;
        c.IsActive = r.IsActive;
        c.Description = r.Description;
    }

    private async Task SeedDefaultScheduleAsync(FitnessClub club, Guid userId)
    {
        for (var day = 0; day < 7; day++)
        {
            var weekend = day is 0 or 6;
            db.ClubSchedules.Add(new ClubSchedule
            {
                ClubId = club.Id,
                DayOfWeek = day,
                OpensAt = weekend ? new TimeSpan(8, 0, 0) : new TimeSpan(6, 0, 0),
                ClosesAt = weekend ? new TimeSpan(18, 0, 0) : new TimeSpan(22, 0, 0),
                StaffedFrom = weekend ? new TimeSpan(8, 0, 0) : new TimeSpan(7, 0, 0),
                StaffedTo = weekend ? new TimeSpan(17, 0, 0) : new TimeSpan(21, 0, 0),
            }.StampNew(tenant, userId));
        }

        await db.SaveChangesAsync();
    }

    private async Task CancelClassesInClosureAsync(ClubClosure closure, Guid userId)
    {
        var affected = await db.ClassOccurrences.ForTenant(tenant)
            .Where(o => o.ClubId == closure.ClubId
                     && o.StartsAt >= closure.StartsOn
                     && o.StartsAt < closure.EndsOn.AddDays(1)
                     && o.Status != ClassOccurrenceStatus.Cancelled)
            .ToListAsync();

        foreach (var occurrence in affected)
        {
            occurrence.Status = ClassOccurrenceStatus.Cancelled;
            occurrence.CancellationReason = closure.Reason;
            occurrence.CancelledAt = DateTime.UtcNow;
            occurrence.StampUpdated(userId);
        }

        // Credits always go back when the club is the one cancelling.
        var bookings = await db.ClassBookings.ForTenant(tenant)
            .Where(k => affected.Select(a => a.Id).Contains(k.ClassOccurrenceId)
                     && (k.Status == BookingStatus.Booked || k.Status == BookingStatus.Waitlisted))
            .ToListAsync();

        foreach (var booking in bookings)
        {
            booking.Status = BookingStatus.ClassCancelled;
            booking.CancelledAt = DateTime.UtcNow;
            booking.StampUpdated(userId);
        }

        await db.SaveChangesAsync();
    }
}
