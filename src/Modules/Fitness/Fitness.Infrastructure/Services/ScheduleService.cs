using Fitness.Application.DTOs;
using Fitness.Application.Services.Interfaces;
using Fitness.Domain.Entities;
using Fitness.Domain.Enums;
using Fitness.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Fitness.Infrastructure.Services;

/// <summary>
/// The timetable and the booking engine.
///
/// The interesting part is not the calendar; it is the policy. A booking is a capacity check, an
/// entitlement check and a payment decision at once, and a cancellation is a small piece of
/// contract law: whether it is late, what that costs, whether it counts as a strike, and whether
/// the strike bans them. Boutique studios live or die on getting that exactly right — too soft
/// and the 06:00 class is half empty with a waitlist, too hard and members stop booking at all.
///
/// Every rule that costs a member something is previewed before it is applied.
/// </summary>
public class ScheduleService(
    FitnessDbContext db,
    IFitnessTenant tenant,
    IBillingService billing) : IScheduleService
{
    // ── Class types ──────────────────────────────────────────────────────────

    public async Task<List<ClassTypeDto>> GetClassTypesAsync(Guid? clubId, bool activeOnly)
    {
        var since = DateTime.UtcNow.AddDays(-30);

        var types = await db.ClassTypes.ForTenant(tenant)
            .WhereIf(activeOnly, c => c.IsActive)
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name)
            .ToListAsync();

        var stats = await db.ClassOccurrences.ForTenant(tenant)
            .Where(o => o.StartsAt >= since && o.Status == ClassOccurrenceStatus.Completed)
            .WhereIf(clubId is not null, o => o.ClubId == clubId)
            .GroupBy(o => o.ClassTypeId)
            .Select(g => new
            {
                ClassTypeId = g.Key,
                Count = g.Count(),
                Attendance = (int)g.Average(o => (double)o.AttendedCount),
                Capacity = g.Sum(o => o.Capacity),
                Attended = g.Sum(o => o.AttendedCount),
            })
            .ToListAsync();

        var policies = await db.BookingPolicies.ForTenant(tenant)
            .Select(p => new { p.Id, p.Name }).ToDictionaryAsync(p => p.Id, p => p.Name);

        var cancellations = await db.CancellationPolicies.ForTenant(tenant)
            .Select(p => new { p.Id, p.Name }).ToDictionaryAsync(p => p.Id, p => p.Name);

        return [.. types.Select(t =>
        {
            var dto = FitnessMapper.ToDto(t);
            var s = stats.FirstOrDefault(x => x.ClassTypeId == t.Id);
            dto.OccurrencesLast30Days = s?.Count ?? 0;
            dto.AverageAttendance = s?.Attendance ?? 0;
            dto.AverageFillPercent = s is null ? 0 : FitnessMapper.Percent(s.Attended, s.Capacity);
            if (t.BookingPolicyId is not null) dto.BookingPolicyName = policies.GetValueOrDefault(t.BookingPolicyId.Value);
            if (t.CancellationPolicyId is not null) dto.CancellationPolicyName = cancellations.GetValueOrDefault(t.CancellationPolicyId.Value);
            return dto;
        })];
    }

    public async Task<ClassTypeDto> SaveClassTypeAsync(Guid? id, SaveClassTypeDto request, Guid userId)
    {
        ClassType type;
        if (id is null)
        {
            type = new ClassType().StampNew(tenant, userId);
            db.ClassTypes.Add(type);
        }
        else
        {
            type = await db.ClassTypes.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new InvalidOperationException("Class type not found.");
            type.StampUpdated(userId);
        }

        type.Code = request.Code;
        type.Name = request.Name;
        type.Discipline = request.Discipline;
        type.MarketingBlurb = request.MarketingBlurb;
        type.ImageUrl = request.ImageUrl;
        type.ColourHex = request.ColourHex;
        type.DisplayOrder = request.DisplayOrder;
        type.DefaultDurationMinutes = request.DefaultDurationMinutes;
        type.DefaultCapacity = request.DefaultCapacity;
        type.Intensity = Math.Clamp(request.Intensity, 1, 5);
        type.EquipmentNeeded = request.EquipmentNeeded;
        type.MinimumAge = request.MinimumAge;
        type.MaximumAge = request.MaximumAge;
        type.RequiresSkillClearance = request.RequiresSkillClearance;
        type.RequiredSkillId = request.RequiredSkillId;
        type.AllowsDropIn = request.AllowsDropIn;
        type.DropInPrice = request.DropInPrice;
        type.CreditCost = request.CreditCost;
        type.BookingPolicyId = request.BookingPolicyId;
        type.CancellationPolicyId = request.CancellationPolicyId;
        type.AvailableToMarketplace = request.AvailableToMarketplace;
        type.IsBookable = request.IsBookable;
        type.IsActive = request.IsActive;
        type.Description = request.Description;

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(type);
    }

    public async Task DeleteClassTypeAsync(Guid id, Guid userId)
    {
        var type = await db.ClassTypes.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new InvalidOperationException("Class type not found.");

        var upcoming = await db.ClassOccurrences.ForTenant(tenant)
            .CountAsync(o => o.ClassTypeId == id && o.StartsAt > DateTime.UtcNow
                          && o.Status != ClassOccurrenceStatus.Cancelled);

        if (upcoming > 0)
            throw new InvalidOperationException(
                $"{upcoming} classes of this type are still on the timetable. Remove them first, " +
                "or mark the type inactive so it stops being scheduled.");

        type.StampDeleted(userId);
        await db.SaveChangesAsync();
    }

    // ── Schedules ────────────────────────────────────────────────────────────

    public async Task<List<ClassScheduleDto>> GetSchedulesAsync(Guid clubId, string? seasonCode, bool publishedOnly)
    {
        var now = DateTime.UtcNow;

        var schedules = await db.ClassSchedules.ForTenant(tenant)
            .Where(s => s.ClubId == clubId)
            .WhereIf(!string.IsNullOrWhiteSpace(seasonCode), s => s.SeasonCode == seasonCode)
            .WhereIf(publishedOnly, s => s.IsPublished)
            .Include(s => s.ClassType)
            .Include(s => s.Room)
            .OrderBy(s => s.StartsAt)
            .ToListAsync();

        var counts = await db.ClassOccurrences.ForTenant(tenant)
            .Where(o => o.ClubId == clubId && o.StartsAt > now && o.ClassScheduleId != null)
            .GroupBy(o => o.ClassScheduleId!.Value)
            .Select(g => new { ScheduleId = g.Key, Count = g.Count() })
            .ToListAsync();

        var instructors = await db.Staff.ForTenant(tenant)
            .Select(s => new { s.Id, Name = s.DisplayName ?? s.FirstName + " " + s.LastName })
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        return [.. schedules.Select(s =>
        {
            var dto = FitnessMapper.ToDto(s);
            dto.UpcomingOccurrences = counts.FirstOrDefault(c => c.ScheduleId == s.Id)?.Count ?? 0;
            if (s.InstructorStaffId is not null) dto.InstructorName = instructors.GetValueOrDefault(s.InstructorStaffId.Value);
            return dto;
        })];
    }

    public async Task<ClassScheduleDto> SaveScheduleAsync(Guid? id, SaveClassScheduleDto request, Guid userId)
    {
        ClassSchedule schedule;
        if (id is null)
        {
            schedule = new ClassSchedule { ClubId = request.ClubId }.StampNew(tenant, userId);
            db.ClassSchedules.Add(schedule);
        }
        else
        {
            schedule = await db.ClassSchedules.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == id)
                ?? throw new InvalidOperationException("Schedule not found.");
            schedule.StampUpdated(userId);
        }

        schedule.ClassTypeId = request.ClassTypeId;
        schedule.RoomId = request.RoomId;
        schedule.InstructorStaffId = request.InstructorStaffId;
        schedule.DaysOfWeekMask = request.DaysOfWeekMask;
        schedule.StartsAt = request.StartsAt;
        schedule.DurationMinutes = request.DurationMinutes;
        schedule.Capacity = request.Capacity;
        schedule.MarketplaceCapacity = request.MarketplaceCapacity;
        schedule.EffectiveFrom = request.EffectiveFrom.Date;
        schedule.EffectiveTo = request.EffectiveTo?.Date;
        schedule.RepeatEveryWeeks = Math.Max(1, request.RepeatEveryWeeks);
        schedule.GenerateAheadDays = request.GenerateAheadDays;
        schedule.SeasonCode = request.SeasonCode;
        schedule.IsPublished = request.IsPublished;

        await db.SaveChangesAsync();

        if (schedule.IsPublished) await GenerateForScheduleAsync(schedule, userId);

        var saved = await db.ClassSchedules.ForTenant(tenant)
            .Include(s => s.ClassType)
            .Include(s => s.Room)
            .FirstAsync(s => s.Id == schedule.Id);

        return FitnessMapper.ToDto(saved);
    }

    public async Task DeleteScheduleAsync(Guid id, bool cancelFutureOccurrences, Guid userId)
    {
        var now = DateTime.UtcNow;

        var schedule = await db.ClassSchedules.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new InvalidOperationException("Schedule not found.");

        var future = await db.ClassOccurrences.ForTenant(tenant)
            .Where(o => o.ClassScheduleId == id && o.StartsAt > now && o.Status != ClassOccurrenceStatus.Cancelled)
            .ToListAsync();

        var withBookings = future.Where(o => o.BookedCount > 0).ToList();

        if (withBookings.Count > 0 && !cancelFutureOccurrences)
            throw new InvalidOperationException(
                $"{withBookings.Count} upcoming classes already have bookings. Confirm that you want them " +
                "cancelled and everyone told, or change the schedule's end date instead.");

        foreach (var occurrence in future)
        {
            await CancelOccurrenceAsync(new CancelOccurrenceDto
            {
                OccurrenceId = occurrence.Id,
                Reason = "This class has been taken off the timetable",
                NotifyBookedMembers = true,
            }, userId);
        }

        schedule.StampDeleted(userId);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Everything wrong with a timetable, before members can see it.
    ///
    /// Three real conflicts and one soft one: an instructor in two rooms at once, a room booked
    /// twice, a class inside a closure, and a class with no instructor. The first two are
    /// blocking, because publishing them creates a Tuesday morning nobody can fix.
    /// </summary>
    public async Task<List<ScheduleConflictDto>> CheckConflictsAsync(Guid clubId, string? seasonCode)
    {
        var conflicts = new List<ScheduleConflictDto>();
        var now = DateTime.UtcNow;
        var horizon = now.AddDays(60);

        var occurrences = await db.ClassOccurrences.ForTenant(tenant)
            .Where(o => o.ClubId == clubId && o.StartsAt >= now && o.StartsAt <= horizon
                     && o.Status != ClassOccurrenceStatus.Cancelled)
            .Include(o => o.ClassType)
            .Include(o => o.Room)
            .OrderBy(o => o.StartsAt)
            .ToListAsync();

        var staffNames = await db.Staff.ForTenant(tenant)
            .Select(s => new { s.Id, Name = s.DisplayName ?? s.FirstName + " " + s.LastName })
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        // Instructor double-booked.
        foreach (var group in occurrences
                     .Where(o => o.InstructorStaffId is not null)
                     .GroupBy(o => o.SubstituteStaffId ?? o.InstructorStaffId))
        {
            var ordered = group.OrderBy(o => o.StartsAt).ToList();
            for (var i = 1; i < ordered.Count; i++)
            {
                if (ordered[i].StartsAt >= ordered[i - 1].EndsAt) continue;

                conflicts.Add(new ScheduleConflictDto
                {
                    ConflictType = "InstructorDoubleBooked",
                    Message = $"{staffNames.GetValueOrDefault(group.Key!.Value, "An instructor")} is down for " +
                              $"{ordered[i - 1].ClassType?.Name} and {ordered[i].ClassType?.Name} at the same time.",
                    OccursAt = ordered[i].StartsAt,
                    OccurrenceId = ordered[i].Id,
                    ConflictsWithId = ordered[i - 1].Id,
                    ConflictsWithLabel = ordered[i - 1].ClassType?.Name,
                    IsBlocking = true,
                });
            }
        }

        // Room double-booked.
        foreach (var group in occurrences.Where(o => o.RoomId is not null).GroupBy(o => o.RoomId))
        {
            var ordered = group.OrderBy(o => o.StartsAt).ToList();
            for (var i = 1; i < ordered.Count; i++)
            {
                if (ordered[i].StartsAt >= ordered[i - 1].EndsAt) continue;

                conflicts.Add(new ScheduleConflictDto
                {
                    ConflictType = "RoomDoubleBooked",
                    Message = $"{ordered[i].Room?.Name} has two classes overlapping at " +
                              $"{ordered[i].StartsAt:HH:mm} on {ordered[i].StartsAt:ddd d MMM}.",
                    OccursAt = ordered[i].StartsAt,
                    OccurrenceId = ordered[i].Id,
                    ConflictsWithId = ordered[i - 1].Id,
                    IsBlocking = true,
                });
            }
        }

        // Scheduled inside a closure.
        var closures = await db.ClubClosures.ForTenant(tenant)
            .Where(c => c.ClubId == clubId && c.EndsOn >= now.Date)
            .ToListAsync();

        foreach (var closure in closures)
        {
            foreach (var clash in occurrences.Where(o => o.StartsAt.Date >= closure.StartsOn && o.StartsAt.Date <= closure.EndsOn))
            {
                conflicts.Add(new ScheduleConflictDto
                {
                    ConflictType = "DuringClosure",
                    Message = $"{clash.ClassType?.Name} on {clash.StartsAt:ddd d MMM} falls inside the closure — {closure.Reason}.",
                    OccursAt = clash.StartsAt,
                    OccurrenceId = clash.Id,
                    IsBlocking = false,
                });
            }
        }

        // No instructor.
        foreach (var orphan in occurrences.Where(o => o.InstructorStaffId is null && o.SubstituteStaffId is null))
        {
            conflicts.Add(new ScheduleConflictDto
            {
                ConflictType = "NoInstructor",
                Message = $"{orphan.ClassType?.Name} on {orphan.StartsAt:ddd d MMM HH:mm} has nobody teaching it.",
                OccursAt = orphan.StartsAt,
                OccurrenceId = orphan.Id,
                IsBlocking = false,
            });
        }

        return [.. conflicts.OrderBy(c => c.OccursAt)];
    }

    public async Task<ClassScheduleDto> PublishScheduleAsync(Guid scheduleId, Guid userId)
    {
        var schedule = await db.ClassSchedules.ForTenant(tenant)
            .Include(s => s.ClassType)
            .Include(s => s.Room)
            .FirstOrDefaultAsync(s => s.Id == scheduleId)
            ?? throw new InvalidOperationException("Schedule not found.");

        schedule.IsPublished = true;
        schedule.StampUpdated(userId);
        await db.SaveChangesAsync();

        await GenerateForScheduleAsync(schedule, userId);

        var blocking = (await CheckConflictsAsync(schedule.ClubId, schedule.SeasonCode))
            .Where(c => c.IsBlocking)
            .ToList();

        if (blocking.Count > 0)
        {
            schedule.IsPublished = false;
            await db.SaveChangesAsync();
            throw new InvalidOperationException(
                "Publishing was stopped by " + blocking.Count + " conflict(s): " + blocking[0].Message);
        }

        return FitnessMapper.ToDto(schedule);
    }

    /// <summary>
    /// Materialises occurrences out to each schedule's horizon.
    ///
    /// Runs nightly and on publish. The horizon exists because a member on a premium plan can book
    /// six weeks ahead, and an occurrence they cannot see is an occurrence they cannot book.
    /// </summary>
    public async Task<int> GenerateOccurrencesAsync(Guid? clubId)
    {
        var schedules = await db.ClassSchedules.ForTenant(tenant)
            .Where(s => s.IsPublished && s.IsActive)
            .WhereIf(clubId is not null, s => s.ClubId == clubId)
            .Include(s => s.ClassType)
            .ToListAsync();

        var created = 0;
        foreach (var schedule in schedules) created += await GenerateForScheduleAsync(schedule, Guid.Empty);

        return created;
    }

    // ── Timetable ────────────────────────────────────────────────────────────

    public async Task<TimetableDto> GetTimetableAsync(
        Guid clubId, DateTime from, DateTime to,
        Guid? classTypeId, Guid? instructorStaffId, Guid? roomId, Guid? viewerMemberId)
    {
        var now = DateTime.UtcNow;

        var club = await db.Clubs.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == clubId)
            ?? throw new InvalidOperationException("Club not found.");

        var occurrences = await db.ClassOccurrences.ForTenant(tenant)
            .Where(o => o.ClubId == clubId && o.StartsAt >= from && o.StartsAt <= to)
            .WhereIf(classTypeId is not null, o => o.ClassTypeId == classTypeId)
            .WhereIf(instructorStaffId is not null,
                o => o.InstructorStaffId == instructorStaffId || o.SubstituteStaffId == instructorStaffId)
            .WhereIf(roomId is not null, o => o.RoomId == roomId)
            .Include(o => o.ClassType)
            .Include(o => o.Room)
            .OrderBy(o => o.StartsAt)
            .ToListAsync();

        var staff = await db.Staff.ForTenant(tenant)
            .Where(s => s.IsActive && (s.CanTeach || s.RoleKind == StaffRoleKind.GroupInstructor))
            .ToListAsync();

        var staffLookup = staff.ToDictionary(s => s.Id, s => (Name: s.DisplayName ?? $"{s.FirstName} {s.LastName}", s.PhotoUrl));

        var dto = new TimetableDto
        {
            ClubId = clubId,
            ClubName = club.Name,
            From = from,
            To = to,
            ClassTypes = await GetClassTypesAsync(clubId, true),
            Rooms = [.. (await db.Rooms.ForTenant(tenant)
                .Where(r => r.ClubId == clubId && r.IsActive)
                .Include(r => r.Spots.Where(s => !s.IsDeleted))
                .ToListAsync()).Select(FitnessMapper.ToDto)],
            Instructors = [.. staff.Select(s => FitnessMapper.ToSummary(s, now))],
        };

        // The viewer's own bookings, so the timetable can say "Booked" rather than making them look.
        var viewerBookings = viewerMemberId is null
            ? []
            : await db.ClassBookings.ForTenant(tenant)
                .Where(k => k.MemberId == viewerMemberId
                         && occurrences.Select(o => o.Id).Contains(k.ClassOccurrenceId)
                         && (k.Status == BookingStatus.Booked || k.Status == BookingStatus.Waitlisted
                          || k.Status == BookingStatus.CheckedIn || k.Status == BookingStatus.Attended))
                .ToListAsync();

        foreach (var occurrence in occurrences)
        {
            var item = FitnessMapper.ToSummary(occurrence, now);

            var instructorId = occurrence.SubstituteStaffId ?? occurrence.InstructorStaffId;
            if (instructorId is not null && staffLookup.TryGetValue(instructorId.Value, out var s))
            {
                item.InstructorName = s.Name;
                item.InstructorPhotoUrl = s.PhotoUrl;
            }

            var mine = viewerBookings.FirstOrDefault(b => b.ClassOccurrenceId == occurrence.Id);
            if (mine is not null)
            {
                item.ViewerIsBooked = mine.Status != BookingStatus.Waitlisted;
                item.ViewerIsWaitlisted = mine.Status == BookingStatus.Waitlisted;
                item.ViewerBookingId = mine.Id;
            }
            else if (viewerMemberId is not null)
            {
                // Cheap pre-check for the button state; the full check runs on the booking itself.
                item.ViewerCanBook = item.BookingIsOpen && item.SpacesLeft > 0;
                if (!item.ViewerCanBook)
                {
                    item.ViewerBlockReason = item.SpacesLeft <= 0
                        ? "Full — you can join the waitlist"
                        : item.BookingOpensAt > now
                            ? $"Booking opens {item.BookingOpensAt:ddd d MMM HH:mm}"
                            : "Booking has closed";
                }
            }

            dto.Occurrences.Add(item);
        }

        dto.TotalClasses = dto.Occurrences.Count;
        dto.TotalCapacity = dto.Occurrences.Sum(o => o.Capacity);
        dto.TotalBooked = dto.Occurrences.Sum(o => o.BookedCount);
        dto.AverageFillPercent = FitnessMapper.Percent(dto.TotalBooked, dto.TotalCapacity);

        return dto;
    }

    public async Task<ClassOccurrenceDetailDto?> GetOccurrenceAsync(Guid occurrenceId, Guid? viewerMemberId)
    {
        var now = DateTime.UtcNow;

        var occurrence = await db.ClassOccurrences.ForTenant(tenant)
            .Include(o => o.ClassType)
            .Include(o => o.Room).ThenInclude(r => r!.Spots.Where(s => !s.IsDeleted))
            .Include(o => o.Bookings.Where(b => !b.IsDeleted)).ThenInclude(b => b.Member)
            .FirstOrDefaultAsync(o => o.Id == occurrenceId);

        if (occurrence is null) return null;

        var summary = FitnessMapper.ToSummary(occurrence, now);

        var detail = new ClassOccurrenceDetailDto
        {
            Id = summary.Id,
            ClubId = summary.ClubId,
            ClassTypeId = summary.ClassTypeId,
            ClassTypeName = summary.ClassTypeName,
            ColourHex = summary.ColourHex,
            Discipline = summary.Discipline,
            Intensity = summary.Intensity,
            RoomId = summary.RoomId,
            RoomName = summary.RoomName,
            InstructorStaffId = summary.InstructorStaffId,
            HasSubstitute = summary.HasSubstitute,
            StartsAt = summary.StartsAt,
            EndsAt = summary.EndsAt,
            DurationMinutes = summary.DurationMinutes,
            Status = summary.Status,
            Capacity = summary.Capacity,
            BookedCount = summary.BookedCount,
            WaitlistCount = summary.WaitlistCount,
            AttendedCount = summary.AttendedCount,
            NoShowCount = summary.NoShowCount,
            SpacesLeft = summary.SpacesLeft,
            FillPercent = summary.FillPercent,
            HasSpotMap = summary.HasSpotMap,
            AllowsDropIn = summary.AllowsDropIn,
            DropInPrice = summary.DropInPrice,
            BookingOpensAt = summary.BookingOpensAt,
            BookingClosesAt = summary.BookingClosesAt,
            BookingIsOpen = summary.BookingIsOpen,
            CancellationReason = summary.CancellationReason,

            ClassScheduleId = occurrence.ClassScheduleId,
            SubstituteStaffId = occurrence.SubstituteStaffId,
            MarketplaceCapacity = occurrence.MarketplaceCapacity,
            MarketplaceBookedCount = occurrence.MarketplaceBookedCount,
            SpotReleaseMinutes = occurrence.SpotReleaseMinutes,
            CancelledAt = occurrence.CancelledAt,
            CancellationNotified = occurrence.CancellationNotified,
            WorkoutId = occurrence.WorkoutId,
            Note = occurrence.Note,
        };

        var instructorId = occurrence.SubstituteStaffId ?? occurrence.InstructorStaffId;
        if (instructorId is not null)
        {
            var staff = await db.Staff.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == instructorId);
            detail.InstructorName = staff is null ? null : staff.DisplayName ?? $"{staff.FirstName} {staff.LastName}";
            detail.InstructorPhotoUrl = staff?.PhotoUrl;
        }

        if (occurrence.WorkoutId is not null)
        {
            detail.WorkoutName = await db.Workouts.ForTenant(tenant)
                .Where(w => w.Id == occurrence.WorkoutId).Select(w => w.Name).FirstOrDefaultAsync();
        }

        var bookings = occurrence.Bookings.Where(b => !b.IsDeleted).ToList();
        var attending = bookings.Where(b => b.Status != BookingStatus.Waitlisted).OrderBy(b => b.BookedAt).ToList();
        var waiting = bookings.Where(b => b.Status == BookingStatus.Waitlisted).OrderBy(b => b.WaitlistPosition).ToList();

        // Medical flags on the roster, because a condition discovered mid-class is discovered too late.
        var memberIds = attending.Where(b => b.MemberId is not null).Select(b => b.MemberId!.Value).ToList();
        var flags = await db.MedicalFlags.ForTenant(tenant)
            .Where(f => memberIds.Contains(f.MemberId) && f.VisibleToInstructors && f.ResolvedOn == null)
            .ToListAsync();

        detail.Bookings = [.. attending.Select(b =>
        {
            var dto = FitnessMapper.ToDto(b);
            dto.MedicalFlags = [.. flags.Where(f => f.MemberId == b.MemberId).Select(FitnessMapper.ToDto)];
            return dto;
        })];

        detail.Waitlist = [.. waiting.Select(FitnessMapper.ToDto)];

        if (occurrence.Room?.HasSpotMap == true)
        {
            var taken = bookings
                .Where(b => b.SpotId is not null && b.Status != BookingStatus.Cancelled)
                .ToDictionary(b => b.SpotId!.Value, b => b);

            detail.SpotMap = [.. occurrence.Room.Spots
                .Where(s => !s.IsDeleted)
                .OrderBy(s => s.GridRow).ThenBy(s => s.GridColumn)
                .Select(s =>
                {
                    var dto = FitnessMapper.ToDto(s);
                    if (taken.TryGetValue(s.Id, out var b))
                    {
                        dto.BookedByMemberId = b.MemberId;
                        dto.BookedByName = b.Member is null ? b.GuestName : FitnessMapper.FullName(b.Member);
                    }
                    return dto;
                })];
        }

        if (viewerMemberId is not null)
        {
            var mine = bookings.FirstOrDefault(b => b.MemberId == viewerMemberId
                                                 && b.Status != BookingStatus.Cancelled
                                                 && b.Status != BookingStatus.LateCancelled);
            detail.ViewerIsBooked = mine is not null && mine.Status != BookingStatus.Waitlisted;
            detail.ViewerIsWaitlisted = mine?.Status == BookingStatus.Waitlisted;
            detail.ViewerBookingId = mine?.Id;
        }

        return detail;
    }

    public async Task<ClassOccurrenceDetailDto> UpdateOccurrenceAsync(UpdateOccurrenceDto request, Guid userId)
    {
        var occurrence = await db.ClassOccurrences.ForTenant(tenant)
            .Include(o => o.ClassType)
            .FirstOrDefaultAsync(o => o.Id == request.OccurrenceId)
            ?? throw new InvalidOperationException("Class not found.");

        var changes = new List<string>();

        if (request.RoomId is not null && request.RoomId != occurrence.RoomId)
        {
            occurrence.RoomId = request.RoomId;
            var room = await db.Rooms.ForTenant(tenant).FirstOrDefaultAsync(r => r.Id == request.RoomId);
            changes.Add($"moved to {room?.Name}");
        }

        if (request.SubstituteStaffId is not null && request.SubstituteStaffId != occurrence.SubstituteStaffId)
        {
            occurrence.SubstituteStaffId = request.SubstituteStaffId;
            var staff = await db.Staff.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == request.SubstituteStaffId);
            changes.Add($"taught by {(staff is null ? "a substitute" : staff.DisplayName ?? staff.FirstName)}");
        }

        if (request.InstructorStaffId is not null) occurrence.InstructorStaffId = request.InstructorStaffId;

        if (request.StartsAt is not null && request.StartsAt != occurrence.StartsAt)
        {
            var duration = request.DurationMinutes ?? (int)(occurrence.EndsAt - occurrence.StartsAt).TotalMinutes;
            occurrence.StartsAt = request.StartsAt.Value;
            occurrence.EndsAt = request.StartsAt.Value.AddMinutes(duration);
            changes.Add($"moved to {request.StartsAt:ddd d MMM HH:mm}");
        }
        else if (request.DurationMinutes is not null)
        {
            occurrence.EndsAt = occurrence.StartsAt.AddMinutes(request.DurationMinutes.Value);
        }

        if (request.Capacity is not null)
        {
            if (request.Capacity < occurrence.BookedCount)
                throw new InvalidOperationException(
                    $"{occurrence.BookedCount} people are already booked. Capacity cannot go below that " +
                    "without cancelling someone's booking first.");

            occurrence.Capacity = request.Capacity.Value;
        }

        if (request.WorkoutId is not null) occurrence.WorkoutId = request.WorkoutId;
        if (request.Note is not null) occurrence.Note = request.Note;

        occurrence.StampUpdated(userId);

        if (request.NotifyBookedMembers && changes.Count > 0)
            await NotifyBookedAsync(occurrence, $"{occurrence.ClassType?.Name} has been {string.Join(" and ", changes)}.", userId);

        await db.SaveChangesAsync();
        return (await GetOccurrenceAsync(occurrence.Id, null))!;
    }

    /// <summary>
    /// Cancelling a class.
    ///
    /// Credits always go back — the member did nothing wrong — and everyone booked is told. The
    /// order matters: refund first, notify second, so a notification failure cannot leave someone
    /// out of pocket.
    /// </summary>
    public async Task<ClassOccurrenceDetailDto> CancelOccurrenceAsync(CancelOccurrenceDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var occurrence = await db.ClassOccurrences.ForTenant(tenant)
            .Include(o => o.ClassType)
            .Include(o => o.Bookings.Where(b => !b.IsDeleted))
            .FirstOrDefaultAsync(o => o.Id == request.OccurrenceId)
            ?? throw new InvalidOperationException("Class not found.");

        occurrence.Status = ClassOccurrenceStatus.Cancelled;
        occurrence.CancellationReason = request.Reason;
        occurrence.CancelledAt = now;
        occurrence.CancelledByStaffId = userId;
        occurrence.StampUpdated(userId);

        foreach (var booking in occurrence.Bookings.Where(b =>
                     b.Status is BookingStatus.Booked or BookingStatus.Waitlisted))
        {
            booking.Status = BookingStatus.ClassCancelled;
            booking.CancelledAt = now;
            booking.StampUpdated(userId);

            if (request.RefundCredits) await ReturnCreditAsync(booking, "Class cancelled by the club", userId);

            if (booking.AmountPaid > 0 && booking.MemberId is not null)
            {
                await billing.IssueCreditNoteAsync(new IssueCreditNoteDto
                {
                    MemberId = booking.MemberId.Value,
                    Amount = booking.AmountPaid,
                    Reason = $"{occurrence.ClassType?.Name} on {occurrence.StartsAt:d MMM} was cancelled",
                    AppliedToBalance = true,
                }, userId);
            }
        }

        occurrence.BookedCount = 0;
        occurrence.WaitlistCount = 0;

        if (request.NotifyBookedMembers)
        {
            await NotifyBookedAsync(occurrence,
                $"{occurrence.ClassType?.Name} on {occurrence.StartsAt:ddd d MMM HH:mm} has been cancelled — {request.Reason}. " +
                "Your credit has been returned.", userId);

            occurrence.CancellationNotified = true;
        }

        await db.SaveChangesAsync();
        return (await GetOccurrenceAsync(occurrence.Id, null))!;
    }

    // ── Bookings ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Whether this member may book this class, and what it would cost them.
    ///
    /// Runs before the button is pressed so the app can say "Book — 1 credit" or "Full — join the
    /// waitlist" rather than letting someone tap and then explaining why not.
    /// </summary>
    public async Task<BookingEligibilityDto> CheckEligibilityAsync(Guid occurrenceId, Guid memberId)
    {
        var now = DateTime.UtcNow;
        var result = new BookingEligibilityDto();

        var occurrence = await db.ClassOccurrences.ForTenant(tenant)
            .Include(o => o.ClassType)
            .Include(o => o.Room).ThenInclude(r => r!.Spots.Where(s => !s.IsDeleted))
            .FirstOrDefaultAsync(o => o.Id == occurrenceId)
            ?? throw new InvalidOperationException("Class not found.");

        var member = await db.Members.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == memberId)
            ?? throw new InvalidOperationException("Member not found.");

        var policy = await ResolveBookingPolicyAsync(occurrence);
        var classType = occurrence.ClassType;

        // ── Hard stops ───────────────────────────────────────────────────────

        if (occurrence.Status == ClassOccurrenceStatus.Cancelled)
        {
            result.BlockReason = "This class has been cancelled.";
            return result;
        }

        if (occurrence.StartsAt < now)
        {
            result.BlockReason = "This class has already started.";
            return result;
        }

        if (member.Status is not (MemberStatus.Active or MemberStatus.Trial or MemberStatus.WonBack))
        {
            result.BlockReason = member.Status == MemberStatus.Frozen
                ? "Bookings are paused while the membership is frozen."
                : $"This membership is {member.Status.ToString().ToLower()}.";
            return result;
        }

        // Booking bans from accumulated strikes.
        var ban = await GetBookingBanAsync(memberId, occurrence, now);
        if (ban is not null)
        {
            result.IsBookingBanned = true;
            result.BanEndsOn = ban;
            result.BlockReason = $"Booking is paused until {ban:d MMM} after repeated late cancellations.";
            return result;
        }

        // ── Window ───────────────────────────────────────────────────────────

        var opensAt = occurrence.BookingOpensAt ?? occurrence.StartsAt.AddDays(-policy.BookingOpensDaysBefore);
        var closesAt = occurrence.BookingClosesAt ?? occurrence.StartsAt.AddMinutes(-policy.BookingClosesMinutesBefore);

        result.BookingOpensAt = opensAt;
        result.WithinBookingWindow = now >= opensAt && now <= closesAt;

        if (now < opensAt)
        {
            result.BlockReason = $"Booking opens {opensAt:ddd d MMM} at {opensAt:HH:mm}.";
            return result;
        }

        if (now > closesAt)
        {
            result.BlockReason = "Booking for this class has closed.";
            return result;
        }

        // ── Age & clearance ──────────────────────────────────────────────────

        var age = FitnessMapper.AgeOn(member.DateOfBirth, now);
        result.MeetsAgeRequirement = true;

        if (classType?.MinimumAge is not null && age is not null && age < classType.MinimumAge)
        {
            result.MeetsAgeRequirement = false;
            result.BlockReason = $"This class is for ages {classType.MinimumAge} and over.";
            return result;
        }

        if (classType?.MaximumAge is not null && age is not null && age > classType.MaximumAge)
        {
            result.MeetsAgeRequirement = false;
            result.BlockReason = $"This class is for ages {classType.MaximumAge} and under.";
            return result;
        }

        result.HasSkillClearance = true;
        if (classType?.RequiresSkillClearance == true)
        {
            result.HasSkillClearance = await db.SkillClearances.ForTenant(tenant)
                .AnyAsync(c => c.MemberId == memberId && !c.IsRevoked
                            && (c.ExpiresOn == null || c.ExpiresOn > now));

            if (!result.HasSkillClearance)
            {
                result.BlockReason = "A coach needs to sign you off for this class first.";
                return result;
            }
        }

        // ── Concurrency limits ───────────────────────────────────────────────

        result.WithinConcurrentLimit = true;

        if (policy.MaxConcurrentBookings > 0)
        {
            var held = await db.ClassBookings.ForTenant(tenant)
                .CountAsync(b => b.MemberId == memberId && b.Status == BookingStatus.Booked
                              && b.ClassOccurrence!.StartsAt > now);

            if (held >= policy.MaxConcurrentBookings)
            {
                result.WithinConcurrentLimit = false;
                result.BlockReason = $"You can hold {policy.MaxConcurrentBookings} bookings at once. Cancel one to book another.";
                return result;
            }
        }

        if (policy.MaxBookingsPerDay > 0)
        {
            var sameDay = await db.ClassBookings.ForTenant(tenant)
                .CountAsync(b => b.MemberId == memberId && b.Status == BookingStatus.Booked
                              && b.ClassOccurrence!.StartsAt.Date == occurrence.StartsAt.Date);

            if (sameDay >= policy.MaxBookingsPerDay)
            {
                result.BlockReason = $"You already have {sameDay} classes booked that day.";
                return result;
            }
        }

        if (policy.PreventDuplicateSameDay)
        {
            var duplicate = await db.ClassBookings.ForTenant(tenant)
                .AnyAsync(b => b.MemberId == memberId && b.Status == BookingStatus.Booked
                            && b.ClassOccurrence!.ClassTypeId == occurrence.ClassTypeId
                            && b.ClassOccurrence.StartsAt.Date == occurrence.StartsAt.Date);

            if (duplicate)
            {
                result.BlockReason = "You are already booked into this class that day.";
                return result;
            }
        }

        // ── How it gets paid for ─────────────────────────────────────────────

        var (paymentKind, creditsRequired, creditsAvailable, amount, entitled) =
            await ResolvePaymentAsync(member, occurrence, classType);

        result.PaymentKind = paymentKind;
        result.CreditsRequired = creditsRequired;
        result.CreditsAvailable = creditsAvailable;
        result.AmountPayable = amount;
        result.HasEntitlement = entitled;

        if (!entitled && paymentKind == BookingPaymentKind.PackCredit && creditsAvailable < creditsRequired)
        {
            result.BlockReason = classType?.AllowsDropIn == true
                ? $"No credits left — a drop-in is {classType.DropInPrice:0.00}."
                : "No credits left for this class. Reception can top you up.";

            if (classType?.AllowsDropIn != true) return result;
        }

        // ── Capacity ─────────────────────────────────────────────────────────

        if (occurrence.BookedCount >= occurrence.Capacity)
        {
            result.CanWaitlist = policy.WaitlistEnabled && occurrence.WaitlistCount < policy.MaxWaitlistLength;
            result.WaitlistPosition = occurrence.WaitlistCount + 1;
            result.BlockReason = result.CanWaitlist
                ? $"Full — you would be number {result.WaitlistPosition} on the waitlist."
                : "Full, and the waitlist is full too.";
            return result;
        }

        result.CanBook = true;

        if (occurrence.Room?.HasSpotMap == true)
        {
            var taken = await db.ClassBookings.ForTenant(tenant)
                .Where(b => b.ClassOccurrenceId == occurrenceId && b.SpotId != null
                         && b.Status != BookingStatus.Cancelled && b.Status != BookingStatus.LateCancelled)
                .Select(b => b.SpotId!.Value)
                .ToListAsync();

            result.AvailableSpots = [.. occurrence.Room.Spots
                .Where(s => !s.IsDeleted && !s.IsOutOfService && !s.IsReserved && !taken.Contains(s.Id))
                .OrderBy(s => s.GridRow).ThenBy(s => s.GridColumn)
                .Select(FitnessMapper.ToDto)];
        }

        return result;
    }

    public async Task<ClassBookingDto> BookAsync(CreateBookingDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var occurrence = await db.ClassOccurrences.ForTenant(tenant)
            .Include(o => o.ClassType)
            .Include(o => o.Room)
            .FirstOrDefaultAsync(o => o.Id == request.ClassOccurrenceId)
            ?? throw new InvalidOperationException("Class not found.");

        var policy = await ResolveBookingPolicyAsync(occurrence);

        // ── A non-member drop-in ─────────────────────────────────────────────

        if (request.MemberId is null)
        {
            if (occurrence.ClassType?.AllowsDropIn != true)
                throw new InvalidOperationException("This class does not take drop-ins.");

            if (occurrence.BookedCount >= occurrence.Capacity)
                throw new InvalidOperationException("This class is full.");

            var guestBooking = new ClassBooking
            {
                ClassOccurrenceId = occurrence.Id,
                GuestName = request.GuestName,
                GuestPhone = request.GuestPhone,
                GuestEmail = request.GuestEmail,
                Status = BookingStatus.Booked,
                PaymentKind = BookingPaymentKind.DropInPayment,
                Channel = request.Channel,
                BookedAt = now,
                SpotId = request.SpotId,
                AmountPaid = occurrence.ClassType.DropInPrice,
                BookedByStaffId = userId,
                Note = request.Note,
            }.StampNew(tenant, userId);

            db.ClassBookings.Add(guestBooking);
            occurrence.BookedCount++;
            if (occurrence.BookedCount >= occurrence.Capacity) occurrence.Status = ClassOccurrenceStatus.Full;

            await db.SaveChangesAsync();
            return FitnessMapper.ToDto(guestBooking);
        }

        // ── A member ─────────────────────────────────────────────────────────

        var memberId = request.MemberId.Value;
        var eligibility = await CheckEligibilityAsync(occurrence.Id, memberId);

        if (!eligibility.CanBook && !eligibility.CanWaitlist && !request.OverridePolicy)
            throw new InvalidOperationException(eligibility.BlockReason ?? "This class cannot be booked.");

        if (request.OverridePolicy && string.IsNullOrWhiteSpace(request.OverrideReason))
            throw new InvalidOperationException("Overriding a booking rule needs a reason — it is always logged.");

        var waitlisting = !eligibility.CanBook && (eligibility.CanWaitlist || request.OverridePolicy) && request.JoinWaitlistIfFull;

        var existing = await db.ClassBookings.ForTenant(tenant)
            .FirstOrDefaultAsync(b => b.ClassOccurrenceId == occurrence.Id && b.MemberId == memberId
                                   && b.Status != BookingStatus.Cancelled
                                   && b.Status != BookingStatus.LateCancelled
                                   && b.Status != BookingStatus.ClassCancelled);

        if (existing is not null)
            throw new InvalidOperationException("You are already booked into this class.");

        // A named spot can only be held by one person, checked before the credit is spent.
        if (request.SpotId is not null)
        {
            var spotTaken = await db.ClassBookings.ForTenant(tenant)
                .AnyAsync(b => b.ClassOccurrenceId == occurrence.Id && b.SpotId == request.SpotId
                            && b.Status != BookingStatus.Cancelled && b.Status != BookingStatus.LateCancelled);

            if (spotTaken) throw new InvalidOperationException("That spot has just been taken. Please choose another.");
        }

        var spotLabel = request.SpotId is null
            ? null
            : occurrence.Room?.Spots.FirstOrDefault(s => s.Id == request.SpotId)?.Label;

        var booking = new ClassBooking
        {
            ClassOccurrenceId = occurrence.Id,
            MemberId = memberId,
            Status = waitlisting ? BookingStatus.Waitlisted : BookingStatus.Booked,
            PaymentKind = eligibility.PaymentKind,
            Channel = request.Channel,
            BookedAt = now,
            SpotId = waitlisting ? null : request.SpotId,
            SpotLabel = waitlisting ? null : spotLabel,
            WaitlistPosition = waitlisting ? occurrence.WaitlistCount + 1 : null,
            Note = request.Note,
            BookedByStaffId = userId == Guid.Empty ? null : userId,
        }.StampNew(tenant, userId);

        db.ClassBookings.Add(booking);

        // ── Pay for it ───────────────────────────────────────────────────────

        switch (eligibility.PaymentKind)
        {
            case BookingPaymentKind.PackCredit:
                // A waitlisted booking *holds* the credit rather than spending it — otherwise a
                // promotion at 05:50 fails because the credit was spent elsewhere at midnight.
                await ConsumeCreditAsync(booking, occurrence, eligibility.CreditsRequired,
                    hold: waitlisting && policy.HoldCreditOnWaitlist, userId);
                break;

            case BookingPaymentKind.DropInPayment when !waitlisting:
                booking.AmountPaid = eligibility.AmountPayable;

                if (request.PaymentMethod is not null && eligibility.AmountPayable > 0)
                {
                    var payment = await billing.TakePaymentAsync(new TakePaymentDto
                    {
                        MemberId = memberId,
                        ClubId = occurrence.ClubId,
                        Amount = eligibility.AmountPayable,
                        Method = request.PaymentMethod.Value,
                        CashSessionId = request.CashSessionId,
                        Notes = $"Drop-in — {occurrence.ClassType?.Name}",
                        IdempotencyKey = $"booking:{booking.Id}",
                    }, userId);

                    booking.PaymentId = payment.PaymentId;
                }
                break;
        }

        if (waitlisting)
        {
            occurrence.WaitlistCount++;
        }
        else
        {
            occurrence.BookedCount++;
            if (occurrence.BookedCount >= occurrence.Capacity) occurrence.Status = ClassOccurrenceStatus.Full;
        }

        occurrence.StampUpdated(userId);

        if (request.OverridePolicy)
        {
            db.MemberNotes.Add(new MemberNote
            {
                MemberId = memberId,
                Kind = InteractionKind.SystemEvent,
                Body = $"Booking rule overridden for {occurrence.ClassType?.Name} on {occurrence.StartsAt:d MMM} — {request.OverrideReason}",
                OccurredAt = now,
            }.StampNew(tenant, userId));
        }

        await db.SaveChangesAsync();

        var saved = await db.ClassBookings.ForTenant(tenant)
            .Include(b => b.Member)
            .Include(b => b.ClassOccurrence).ThenInclude(o => o!.ClassType)
            .Include(b => b.ClassOccurrence).ThenInclude(o => o!.Room)
            .FirstAsync(b => b.Id == booking.Id);

        return FitnessMapper.ToDto(saved);
    }

    /// <summary>
    /// What cancelling now will cost.
    ///
    /// Shown before the member confirms, always. A studio that silently takes a credit for a
    /// cancellation eleven hours before a twelve-hour window gets a complaint; one that says
    /// "this is a late cancellation and will use your credit — cancel anyway?" does not.
    /// </summary>
    public async Task<CancelBookingPreviewDto> PreviewCancelAsync(Guid bookingId)
    {
        var now = DateTime.UtcNow;

        var booking = await db.ClassBookings.ForTenant(tenant)
            .Include(b => b.ClassOccurrence).ThenInclude(o => o!.ClassType)
            .FirstOrDefaultAsync(b => b.Id == bookingId)
            ?? throw new InvalidOperationException("Booking not found.");

        var occurrence = booking.ClassOccurrence!;
        var policy = await ResolveCancellationPolicyAsync(occurrence);

        var hoursUntil = (int)(occurrence.StartsAt - now).TotalHours;
        var isLate = hoursUntil < policy.FreeCancelHours;

        var strikes = await db.Strikes.ForTenant(tenant)
            .CountAsync(s => s.MemberId == booking.MemberId && !s.IsWaived && s.ExpiresOn > now);

        var preview = new CancelBookingPreviewDto
        {
            BookingId = bookingId,
            IsLate = isLate,
            HoursUntilStart = Math.Max(0, hoursUntil),
            FreeCancelHours = policy.FreeCancelHours,
            Outcome = isLate ? policy.LateCancelOutcome : PolicyOutcome.Nothing,
            CurrentStrikes = strikes,
            StrikeThreshold = policy.StrikeThreshold,
            WaitlistLength = occurrence.WaitlistCount,
        };

        if (isLate)
        {
            preview.CreditWillBeForfeited = policy.LateCancelOutcome
                is PolicyOutcome.ForfeitCredit or PolicyOutcome.ForfeitCreditAndFee;

            preview.FeeWillBeCharged = policy.LateCancelOutcome
                is PolicyOutcome.ChargeFee or PolicyOutcome.ForfeitCreditAndFee
                ? policy.LateCancelFee : 0m;

            preview.StrikeWillBeIssued = policy.StrikeThreshold > 0;

            if (preview.StrikeWillBeIssued && strikes + 1 >= policy.StrikeThreshold)
                preview.WillTriggerBan = true;
        }

        preview.Explanation = isLate
            ? $"This is inside the {policy.FreeCancelHours}-hour window, so it counts as a late cancellation. " +
              (preview.CreditWillBeForfeited ? "Your credit will be used. " : "") +
              (preview.FeeWillBeCharged > 0 ? $"A {preview.FeeWillBeCharged:0.00} fee applies. " : "") +
              (preview.WillTriggerBan
                  ? $"This would be strike {strikes + 1} of {policy.StrikeThreshold}, which pauses booking for {policy.BookingBanDays} days."
                  : preview.StrikeWillBeIssued
                      ? $"It counts as strike {strikes + 1} of {policy.StrikeThreshold}."
                      : "") +
              (occurrence.WaitlistCount > 0
                  ? $" {occurrence.WaitlistCount} people are waiting, so your place will go to someone."
                  : "")
            : $"There are {hoursUntil} hours until this class, so cancelling is free and your credit comes back." +
              (occurrence.WaitlistCount > 0 ? $" {occurrence.WaitlistCount} people on the waitlist will be offered your place." : "");

        return preview;
    }

    public async Task<ClassBookingDto> CancelBookingAsync(CancelBookingDto request, Guid userId)
    {
        var now = DateTime.UtcNow;
        var preview = await PreviewCancelAsync(request.BookingId);

        var booking = await db.ClassBookings.ForTenant(tenant)
            .Include(b => b.Member)
            .Include(b => b.ClassOccurrence).ThenInclude(o => o!.ClassType)
            .FirstOrDefaultAsync(b => b.Id == request.BookingId)
            ?? throw new InvalidOperationException("Booking not found.");

        var occurrence = booking.ClassOccurrence!;
        var wasWaitlisted = booking.Status == BookingStatus.Waitlisted;
        var applyPenalty = preview.IsLate && !request.WaivePenalty && !wasWaitlisted;

        booking.Status = applyPenalty ? BookingStatus.LateCancelled : BookingStatus.Cancelled;
        booking.CancelledAt = now;
        booking.Note = request.Reason ?? booking.Note;
        booking.StampUpdated(userId);

        if (applyPenalty)
        {
            var policy = await ResolveCancellationPolicyAsync(occurrence);

            if (preview.CreditWillBeForfeited)
            {
                booking.CreditForfeited = true;
                // The credit stays spent — the whole point of the policy.
            }
            else
            {
                await ReturnCreditAsync(booking, "Cancelled in time", userId);
            }

            if (preview.FeeWillBeCharged > 0 && booking.MemberId is not null)
            {
                booking.PenaltyCharged = preview.FeeWillBeCharged;

                await billing.CreateAdHocInvoiceAsync(booking.MemberId.Value, occurrence.ClubId, [
                    new InvoiceLineDto
                    {
                        ChargeKind = ChargeKind.LateCancelFee,
                        LineDescription = $"Late cancellation — {occurrence.ClassType?.Name}, {occurrence.StartsAt:d MMM HH:mm}",
                        Quantity = 1,
                        UnitPrice = preview.FeeWillBeCharged,
                        LineTotal = preview.FeeWillBeCharged,
                    },
                ], userId);
            }

            if (preview.StrikeWillBeIssued && booking.MemberId is not null)
            {
                booking.StrikeIssued = true;

                db.Strikes.Add(new LateCancelStrike
                {
                    MemberId = booking.MemberId.Value,
                    ClassBookingId = booking.Id,
                    ClubId = occurrence.ClubId,
                    OccurredOn = now,
                    WasNoShow = false,
                    ClassName = occurrence.ClassType?.Name,
                    FeeCharged = preview.FeeWillBeCharged,
                    ExpiresOn = now.AddDays(policy.StrikeWindowDays),
                }.StampNew(tenant, userId));
            }
        }
        else
        {
            await ReturnCreditAsync(booking, request.WaivePenalty ? "Penalty waived" : "Cancelled in time", userId);

            if (request.WaivePenalty)
            {
                db.MemberNotes.Add(new MemberNote
                {
                    MemberId = booking.MemberId ?? Guid.Empty,
                    Kind = InteractionKind.SystemEvent,
                    Body = $"Late-cancellation penalty waived for {occurrence.ClassType?.Name} — {request.WaiveReason}",
                    OccurredAt = now,
                }.StampNew(tenant, userId));
            }
        }

        if (wasWaitlisted)
        {
            occurrence.WaitlistCount = Math.Max(0, occurrence.WaitlistCount - 1);
            await ResequenceWaitlistAsync(occurrence.Id, userId);
        }
        else
        {
            occurrence.BookedCount = Math.Max(0, occurrence.BookedCount - 1);
            if (occurrence.Status == ClassOccurrenceStatus.Full) occurrence.Status = ClassOccurrenceStatus.Open;

            // The freed place goes to the waitlist immediately, not on the next timer tick.
            await PromoteFromWaitlistAsync(occurrence, userId);
        }

        occurrence.StampUpdated(userId);
        await db.SaveChangesAsync();

        return FitnessMapper.ToDto(booking);
    }

    public async Task<ClassBookingDto> CheckInToClassAsync(Guid bookingId, Guid userId)
    {
        var now = DateTime.UtcNow;

        var booking = await db.ClassBookings.ForTenant(tenant)
            .Include(b => b.Member)
            .Include(b => b.ClassOccurrence).ThenInclude(o => o!.ClassType)
            .FirstOrDefaultAsync(b => b.Id == bookingId)
            ?? throw new InvalidOperationException("Booking not found.");

        if (booking.Status == BookingStatus.Waitlisted)
            throw new InvalidOperationException("This member is on the waitlist, not booked in.");

        booking.Status = BookingStatus.CheckedIn;
        booking.CheckedInAt = now;
        booking.StampUpdated(userId);

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(booking);
    }

    public async Task<List<ClassBookingDto>> GetMemberBookingsAsync(
        Guid memberId, DateTime? from, DateTime? to, bool upcomingOnly)
    {
        var now = DateTime.UtcNow;

        var bookings = await db.ClassBookings.ForTenant(tenant)
            .Where(b => b.MemberId == memberId)
            .WhereIf(upcomingOnly, b => b.ClassOccurrence!.StartsAt >= now
                                     && (b.Status == BookingStatus.Booked || b.Status == BookingStatus.Waitlisted))
            .WhereIf(from is not null, b => b.ClassOccurrence!.StartsAt >= from)
            .WhereIf(to is not null, b => b.ClassOccurrence!.StartsAt <= to)
            .Include(b => b.Member)
            .Include(b => b.ClassOccurrence).ThenInclude(o => o!.ClassType)
            .Include(b => b.ClassOccurrence).ThenInclude(o => o!.Room)
            .OrderBy(b => b.ClassOccurrence!.StartsAt)
            .Take(200)
            .ToListAsync();

        return [.. bookings.Select(FitnessMapper.ToDto)];
    }

    /// <summary>
    /// The instructor's register.
    ///
    /// Completing the class is what applies the no-show policy — deliberately an explicit act, so
    /// an instructor who forgets to mark the register does not accidentally fine eight people.
    /// </summary>
    public async Task<ClassOccurrenceDetailDto> MarkAttendanceAsync(MarkAttendanceDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var occurrence = await db.ClassOccurrences.ForTenant(tenant)
            .Include(o => o.ClassType)
            .Include(o => o.Bookings.Where(b => !b.IsDeleted))
            .FirstOrDefaultAsync(o => o.Id == request.ClassOccurrenceId)
            ?? throw new InvalidOperationException("Class not found.");

        foreach (var id in request.AttendedBookingIds)
        {
            var booking = occurrence.Bookings.FirstOrDefault(b => b.Id == id);
            if (booking is null) continue;

            booking.Status = BookingStatus.Attended;
            booking.CheckedInAt ??= now;
            booking.StampUpdated(userId);
        }

        var policy = await ResolveCancellationPolicyAsync(occurrence);

        foreach (var id in request.NoShowBookingIds)
        {
            var booking = occurrence.Bookings.FirstOrDefault(b => b.Id == id);
            if (booking is null || booking.MemberId is null) continue;

            booking.Status = BookingStatus.NoShow;
            booking.StampUpdated(userId);

            if (policy.NoShowOutcome is PolicyOutcome.ForfeitCredit or PolicyOutcome.ForfeitCreditAndFee)
                booking.CreditForfeited = true;

            if (policy.NoShowOutcome is PolicyOutcome.ChargeFee or PolicyOutcome.ForfeitCreditAndFee
                && policy.NoShowFee > 0)
            {
                booking.PenaltyCharged = policy.NoShowFee;

                await billing.CreateAdHocInvoiceAsync(booking.MemberId.Value, occurrence.ClubId, [
                    new InvoiceLineDto
                    {
                        ChargeKind = ChargeKind.NoShowFee,
                        LineDescription = $"No-show — {occurrence.ClassType?.Name}, {occurrence.StartsAt:d MMM HH:mm}",
                        Quantity = 1,
                        UnitPrice = policy.NoShowFee,
                        LineTotal = policy.NoShowFee,
                    },
                ], userId);
            }

            if (policy.StrikeThreshold > 0)
            {
                booking.StrikeIssued = true;

                db.Strikes.Add(new LateCancelStrike
                {
                    MemberId = booking.MemberId.Value,
                    ClassBookingId = booking.Id,
                    ClubId = occurrence.ClubId,
                    OccurredOn = now,
                    WasNoShow = true,
                    ClassName = occurrence.ClassType?.Name,
                    FeeCharged = booking.PenaltyCharged,
                    ExpiresOn = now.AddDays(policy.StrikeWindowDays),
                }.StampNew(tenant, userId));
            }
        }

        foreach (var memberId in request.WalkInMemberIds)
        {
            db.ClassBookings.Add(new ClassBooking
            {
                ClassOccurrenceId = occurrence.Id,
                MemberId = memberId,
                Status = BookingStatus.Attended,
                PaymentKind = BookingPaymentKind.Entitlement,
                Channel = BookingChannel.Instructor,
                BookedAt = now,
                CheckedInAt = now,
                Note = "Walk-in",
            }.StampNew(tenant, userId));

            occurrence.BookedCount++;
        }

        occurrence.AttendedCount = occurrence.Bookings.Count(b => b.Status is BookingStatus.Attended or BookingStatus.CheckedIn)
                                   + request.WalkInMemberIds.Count;
        occurrence.NoShowCount = occurrence.Bookings.Count(b => b.Status == BookingStatus.NoShow);

        if (request.CompleteClass) occurrence.Status = ClassOccurrenceStatus.Completed;
        occurrence.StampUpdated(userId);

        await db.SaveChangesAsync();
        return (await GetOccurrenceAsync(occurrence.Id, null))!;
    }

    /// <summary>
    /// Promotes waitlisted members into places that opened, and releases spots nobody claimed
    /// shortly before the class starts.
    ///
    /// Runs on a short timer. The spot release is what stops a full-with-a-waitlist class running
    /// with four empty bikes because four people booked and forgot.
    /// </summary>
    public async Task<int> ProcessWaitlistsAsync()
    {
        var now = DateTime.UtcNow;
        var horizon = now.AddDays(7);
        var actions = 0;

        var withWaitlists = await db.ClassOccurrences.ForTenant(tenant)
            .Where(o => o.WaitlistCount > 0 && o.StartsAt > now && o.StartsAt <= horizon
                     && o.Status != ClassOccurrenceStatus.Cancelled)
            .Include(o => o.ClassType)
            .ToListAsync();

        foreach (var occurrence in withWaitlists)
        {
            if (occurrence.BookedCount >= occurrence.Capacity) continue;
            actions += await PromoteFromWaitlistAsync(occurrence, Guid.Empty);
        }

        // Release unclaimed spots just before the start.
        var startingSoon = await db.ClassOccurrences.ForTenant(tenant)
            .Where(o => o.StartsAt > now && o.StartsAt <= now.AddMinutes(15)
                     && o.WaitlistCount > 0 && o.Status != ClassOccurrenceStatus.Cancelled)
            .Include(o => o.Bookings.Where(b => !b.IsDeleted))
            .ToListAsync();

        foreach (var occurrence in startingSoon)
        {
            var releaseAt = occurrence.StartsAt.AddMinutes(-occurrence.SpotReleaseMinutes);
            if (now < releaseAt) continue;

            var unclaimed = occurrence.Bookings
                .Where(b => b.Status == BookingStatus.Booked && b.CheckedInAt is null)
                .ToList();

            foreach (var booking in unclaimed.Take(occurrence.WaitlistCount))
            {
                booking.Status = BookingStatus.Cancelled;
                booking.CancelledAt = now;
                booking.Note = "Place released — not claimed before the class";
                occurrence.BookedCount--;
                actions++;
            }

            await PromoteFromWaitlistAsync(occurrence, Guid.Empty);
        }

        await db.SaveChangesAsync();
        return actions;
    }

    /// <summary>Closes finished classes and applies the no-show policy to anyone who did not arrive.</summary>
    public async Task<int> ProcessFinishedClassesAsync()
    {
        var now = DateTime.UtcNow;
        var closed = 0;

        var finished = await db.ClassOccurrences.ForTenant(tenant)
            .Where(o => o.EndsAt < now
                     && o.Status != ClassOccurrenceStatus.Completed
                     && o.Status != ClassOccurrenceStatus.Cancelled)
            .Include(o => o.ClassType)
            .Include(o => o.Bookings.Where(b => !b.IsDeleted))
            .Take(200)
            .ToListAsync();

        foreach (var occurrence in finished)
        {
            var policy = await ResolveCancellationPolicyAsync(occurrence);
            var graceEnd = occurrence.StartsAt.AddMinutes(policy.NoShowGraceMinutes);
            if (now < graceEnd) continue;

            var noShows = occurrence.Bookings
                .Where(b => b.Status == BookingStatus.Booked && b.MemberId is not null)
                .Select(b => b.Id)
                .ToList();

            var attended = occurrence.Bookings
                .Where(b => b.Status == BookingStatus.CheckedIn)
                .Select(b => b.Id)
                .ToList();

            await MarkAttendanceAsync(new MarkAttendanceDto
            {
                ClassOccurrenceId = occurrence.Id,
                AttendedBookingIds = attended,
                NoShowBookingIds = noShows,
                CompleteClass = true,
            }, Guid.Empty);

            closed++;
        }

        return closed;
    }

    // ── Policies ─────────────────────────────────────────────────────────────

    public async Task<List<BookingPolicyDto>> GetBookingPoliciesAsync(Guid? clubId)
    {
        var policies = await db.BookingPolicies.ForTenant(tenant)
            .WhereIf(clubId is not null, p => p.ClubId == clubId || p.ClubId == null)
            .OrderByDescending(p => p.IsDefault).ThenBy(p => p.Name)
            .ToListAsync();

        return [.. policies.Select(FitnessMapper.ToDto)];
    }

    public async Task<BookingPolicyDto> SaveBookingPolicyAsync(Guid? id, BookingPolicyDto request, Guid userId)
    {
        BookingPolicy policy;
        if (id is null)
        {
            policy = new BookingPolicy().StampNew(tenant, userId);
            db.BookingPolicies.Add(policy);
        }
        else
        {
            policy = await db.BookingPolicies.ForTenant(tenant).FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new InvalidOperationException("Booking policy not found.");
            policy.StampUpdated(userId);
        }

        policy.Name = request.Name;
        policy.ClubId = request.ClubId;
        policy.BookingOpensDaysBefore = request.BookingOpensDaysBefore;
        policy.BookingClosesMinutesBefore = request.BookingClosesMinutesBefore;
        policy.MaxConcurrentBookings = request.MaxConcurrentBookings;
        policy.MaxBookingsPerDay = request.MaxBookingsPerDay;
        policy.MaxBookingsPerWeek = request.MaxBookingsPerWeek;
        policy.WaitlistEnabled = request.WaitlistEnabled;
        policy.MaxWaitlistLength = request.MaxWaitlistLength;
        policy.HoldCreditOnWaitlist = request.HoldCreditOnWaitlist;
        policy.WaitlistConfirmMinutes = request.WaitlistConfirmMinutes;
        policy.PreventDuplicateSameDay = request.PreventDuplicateSameDay;
        policy.RequiresPaymentUpFront = request.RequiresPaymentUpFront;
        policy.IsDefault = request.IsDefault;
        policy.IsActive = request.IsActive;

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(policy);
    }

    public async Task<List<CancellationPolicyDto>> GetCancellationPoliciesAsync(Guid? clubId)
    {
        var policies = await db.CancellationPolicies.ForTenant(tenant)
            .WhereIf(clubId is not null, p => p.ClubId == clubId || p.ClubId == null)
            .OrderByDescending(p => p.IsDefault).ThenBy(p => p.Name)
            .ToListAsync();

        return [.. policies.Select(FitnessMapper.ToDto)];
    }

    public async Task<CancellationPolicyDto> SaveCancellationPolicyAsync(
        Guid? id, CancellationPolicyDto request, Guid userId)
    {
        CancellationPolicy policy;
        if (id is null)
        {
            policy = new CancellationPolicy().StampNew(tenant, userId);
            db.CancellationPolicies.Add(policy);
        }
        else
        {
            policy = await db.CancellationPolicies.ForTenant(tenant).FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new InvalidOperationException("Cancellation policy not found.");
            policy.StampUpdated(userId);
        }

        policy.Name = request.Name;
        policy.ClubId = request.ClubId;
        policy.FreeCancelHours = request.FreeCancelHours;
        policy.LateCancelOutcome = request.LateCancelOutcome;
        policy.LateCancelFee = request.LateCancelFee;
        policy.NoShowOutcome = request.NoShowOutcome;
        policy.NoShowFee = request.NoShowFee;
        policy.NoShowGraceMinutes = request.NoShowGraceMinutes;
        policy.StrikeThreshold = request.StrikeThreshold;
        policy.StrikeWindowDays = request.StrikeWindowDays;
        policy.BookingBanDays = request.BookingBanDays;
        policy.IsDefault = request.IsDefault;
        policy.IsActive = request.IsActive;

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(policy);
    }

    public async Task<List<LateCancelStrikeDto>> GetStrikesAsync(Guid memberId, bool activeOnly)
    {
        var now = DateTime.UtcNow;

        var strikes = await db.Strikes.ForTenant(tenant)
            .Where(s => s.MemberId == memberId)
            .WhereIf(activeOnly, s => !s.IsWaived && s.ExpiresOn > now)
            .Include(s => s.Member)
            .OrderByDescending(s => s.OccurredOn)
            .ToListAsync();

        return [.. strikes.Select(FitnessMapper.ToDto)];
    }

    public async Task<LateCancelStrikeDto> WaiveStrikeAsync(Guid strikeId, string reason, Guid userId)
    {
        var strike = await db.Strikes.ForTenant(tenant)
            .Include(s => s.Member)
            .FirstOrDefaultAsync(s => s.Id == strikeId)
            ?? throw new InvalidOperationException("Strike not found.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Waiving a strike needs a reason.");

        // Kept rather than deleted, so the pattern stays visible even when one is forgiven.
        strike.IsWaived = true;
        strike.WaivedReason = reason;
        strike.WaivedByStaffId = userId;
        strike.StampUpdated(userId);

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(strike);
    }

    // ═══ Internals ═══════════════════════════════════════════════════════════

    private async Task<int> GenerateForScheduleAsync(ClassSchedule schedule, Guid userId)
    {
        var now = DateTime.UtcNow;
        var from = schedule.GeneratedThrough ?? schedule.EffectiveFrom;
        if (from < now.Date) from = now.Date;

        var to = now.Date.AddDays(schedule.GenerateAheadDays);
        if (schedule.EffectiveTo is not null && schedule.EffectiveTo < to) to = schedule.EffectiveTo.Value;
        if (to <= from) return 0;

        var policy = await db.BookingPolicies.ForTenant(tenant)
            .Where(p => p.IsActive && (p.ClubId == schedule.ClubId || p.ClubId == null))
            .OrderByDescending(p => p.ClubId != null).ThenByDescending(p => p.IsDefault)
            .FirstOrDefaultAsync() ?? new BookingPolicy();

        var closures = await db.ClubClosures.ForTenant(tenant)
            .Where(c => c.ClubId == schedule.ClubId && c.CancelsClasses && c.EndsOn >= from)
            .ToListAsync();

        var existing = await db.ClassOccurrences.ForTenant(tenant)
            .Where(o => o.ClassScheduleId == schedule.Id && o.StartsAt >= from)
            .Select(o => o.StartsAt)
            .ToListAsync();

        var existingSet = existing.ToHashSet();
        var created = 0;

        for (var day = from; day <= to; day = day.AddDays(1))
        {
            if (!FitnessQueryExtensions.CoversDay(schedule.DaysOfWeekMask, day.DayOfWeek)) continue;

            // Fortnightly and four-weekly patterns count from the schedule's start.
            if (schedule.RepeatEveryWeeks > 1)
            {
                var weeksElapsed = (int)((day - schedule.EffectiveFrom).TotalDays / 7);
                if (weeksElapsed % schedule.RepeatEveryWeeks != 0) continue;
            }

            var startsAt = day.Date.Add(schedule.StartsAt);
            if (startsAt < now || existingSet.Contains(startsAt)) continue;
            if (closures.Any(c => day.Date >= c.StartsOn && day.Date <= c.EndsOn)) continue;

            db.ClassOccurrences.Add(new ClassOccurrence
            {
                ClubId = schedule.ClubId,
                ClassTypeId = schedule.ClassTypeId,
                ClassScheduleId = schedule.Id,
                RoomId = schedule.RoomId,
                InstructorStaffId = schedule.InstructorStaffId,
                StartsAt = startsAt,
                EndsAt = startsAt.AddMinutes(schedule.DurationMinutes),
                Status = ClassOccurrenceStatus.Open,
                Capacity = schedule.Capacity,
                MarketplaceCapacity = schedule.MarketplaceCapacity,
                BookingOpensAt = startsAt.AddDays(-policy.BookingOpensDaysBefore),
                BookingClosesAt = startsAt.AddMinutes(-policy.BookingClosesMinutesBefore),
            }.StampNew(tenant, userId));

            created++;
        }

        schedule.GeneratedThrough = to;
        await db.SaveChangesAsync();

        return created;
    }

    private async Task<BookingPolicy> ResolveBookingPolicyAsync(ClassOccurrence occurrence)
    {
        if (occurrence.ClassType?.BookingPolicyId is not null)
        {
            var specific = await db.BookingPolicies.ForTenant(tenant)
                .FirstOrDefaultAsync(p => p.Id == occurrence.ClassType.BookingPolicyId);
            if (specific is not null) return specific;
        }

        var club = await db.Clubs.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == occurrence.ClubId);
        if (club?.DefaultBookingPolicyId is not null)
        {
            var clubPolicy = await db.BookingPolicies.ForTenant(tenant)
                .FirstOrDefaultAsync(p => p.Id == club.DefaultBookingPolicyId);
            if (clubPolicy is not null) return clubPolicy;
        }

        return await db.BookingPolicies.ForTenant(tenant)
            .Where(p => p.IsDefault && p.IsActive)
            .FirstOrDefaultAsync() ?? new BookingPolicy();
    }

    private async Task<CancellationPolicy> ResolveCancellationPolicyAsync(ClassOccurrence occurrence)
    {
        if (occurrence.ClassType?.CancellationPolicyId is not null)
        {
            var specific = await db.CancellationPolicies.ForTenant(tenant)
                .FirstOrDefaultAsync(p => p.Id == occurrence.ClassType.CancellationPolicyId);
            if (specific is not null) return specific;
        }

        var club = await db.Clubs.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == occurrence.ClubId);
        if (club?.DefaultCancellationPolicyId is not null)
        {
            var clubPolicy = await db.CancellationPolicies.ForTenant(tenant)
                .FirstOrDefaultAsync(p => p.Id == club.DefaultCancellationPolicyId);
            if (clubPolicy is not null) return clubPolicy;
        }

        return await db.CancellationPolicies.ForTenant(tenant)
            .Where(p => p.IsDefault && p.IsActive)
            .FirstOrDefaultAsync() ?? new CancellationPolicy();
    }

    /// <summary>
    /// How this member pays for this class: an included entitlement, a pack credit, or a drop-in.
    ///
    /// Checked in that order because it is the order of the member's expectation — an unlimited
    /// member should never be told they have no credits.
    /// </summary>
    private async Task<(BookingPaymentKind Kind, int Required, int Available, decimal Amount, bool Entitled)>
        ResolvePaymentAsync(Member member, ClassOccurrence occurrence, ClassType? classType)
    {
        var required = classType?.CreditCost ?? 1;

        var agreement = await db.Agreements.ForTenant(tenant)
            .Include(a => a.Plan).ThenInclude(p => p!.Entitlements)
            .Where(a => a.MemberId == member.Id && a.Status == AgreementStatus.Active)
            .FirstOrDefaultAsync();

        var entitlement = agreement?.Plan?.Entitlements
            .FirstOrDefault(e => !e.IsDeleted
                              && e.Kind == EntitlementKind.ClassBooking
                              && (e.TargetId is null || e.TargetId == occurrence.ClassTypeId));

        if (entitlement is not null && entitlement.Limit == EntitlementLimit.Unlimited)
            return (BookingPaymentKind.Entitlement, 0, 0, 0m, true);

        var credit = await db.SessionCredits.ForTenant(tenant)
            .Where(c => c.MemberId == member.Id && !c.IsExpired && c.Remaining > 0
                     && c.Kind == EntitlementKind.ClassBooking
                     && (c.ClassTypeId == null || c.ClassTypeId == occurrence.ClassTypeId))
            .OrderBy(c => c.ExpiresOn ?? DateTime.MaxValue)
            .FirstOrDefaultAsync();

        var available = credit?.Remaining ?? 0;

        if (available >= required)
            return (BookingPaymentKind.PackCredit, required, available, 0m, false);

        if (entitlement is not null)
        {
            // A capped entitlement — "eight classes a month" — is still an entitlement while it lasts.
            var (start, _) = MonthWindow(DateTime.UtcNow);
            var usedThisPeriod = await db.ClassBookings.ForTenant(tenant)
                .CountAsync(b => b.MemberId == member.Id
                              && b.PaymentKind == BookingPaymentKind.Entitlement
                              && b.BookedAt >= start
                              && b.Status != BookingStatus.Cancelled);

            if (usedThisPeriod < entitlement.Quantity)
                return (BookingPaymentKind.Entitlement, 0, entitlement.Quantity - usedThisPeriod, 0m, true);
        }

        return (BookingPaymentKind.DropInPayment, required, available, classType?.DropInPrice ?? 0m, false);
    }

    private async Task ConsumeCreditAsync(
        ClassBooking booking, ClassOccurrence occurrence, int quantity, bool hold, Guid userId)
    {
        if (quantity <= 0 || booking.MemberId is null) return;

        var credit = await db.SessionCredits.ForTenant(tenant)
            .Where(c => c.MemberId == booking.MemberId && !c.IsExpired && c.Remaining > 0
                     && c.Kind == EntitlementKind.ClassBooking
                     && (c.ClassTypeId == null || c.ClassTypeId == occurrence.ClassTypeId))
            .OrderBy(c => c.ExpiresOn ?? DateTime.MaxValue)
            .FirstOrDefaultAsync();

        if (credit is null) return;

        if (hold)
        {
            credit.Held += quantity;
            credit.Remaining -= quantity;
        }
        else
        {
            credit.Used += quantity;
            credit.Remaining -= quantity;
        }

        var movement = new SessionCreditMovement
        {
            SessionCreditId = credit.Id,
            MemberId = booking.MemberId.Value,
            Kind = SessionCreditMovementKind.Consumed,
            OccurredAt = DateTime.UtcNow,
            Quantity = -quantity,
            BalanceAfter = credit.Remaining,
            ClassBookingId = booking.Id,
            WasHeld = hold,
            Note = hold
                ? $"Held for the waitlist — {occurrence.ClassType?.Name}"
                : $"{occurrence.ClassType?.Name} on {occurrence.StartsAt:d MMM}",
        }.StampNew(tenant, userId);

        db.CreditMovements.Add(movement);

        booking.CreditsUsed = quantity;
        booking.SessionCreditMovementId = movement.Id;
    }

    private async Task ReturnCreditAsync(ClassBooking booking, string reason, Guid userId)
    {
        if (booking.CreditsUsed <= 0 || booking.MemberId is null || booking.CreditForfeited) return;

        var movement = booking.SessionCreditMovementId is null
            ? null
            : await db.CreditMovements.ForTenant(tenant)
                .Include(m => m.SessionCredit)
                .FirstOrDefaultAsync(m => m.Id == booking.SessionCreditMovementId);

        var credit = movement?.SessionCredit
            ?? await db.SessionCredits.ForTenant(tenant)
                .FirstOrDefaultAsync(c => c.MemberId == booking.MemberId && c.Kind == EntitlementKind.ClassBooking);

        if (credit is null) return;

        if (movement?.WasHeld == true) credit.Held = Math.Max(0, credit.Held - booking.CreditsUsed);
        else credit.Used = Math.Max(0, credit.Used - booking.CreditsUsed);

        credit.Remaining += booking.CreditsUsed;

        db.CreditMovements.Add(new SessionCreditMovement
        {
            SessionCreditId = credit.Id,
            MemberId = booking.MemberId.Value,
            Kind = SessionCreditMovementKind.Refunded,
            OccurredAt = DateTime.UtcNow,
            Quantity = booking.CreditsUsed,
            BalanceAfter = credit.Remaining,
            ClassBookingId = booking.Id,
            Note = reason,
        }.StampNew(tenant, userId));

        booking.CreditsUsed = 0;
    }

    private async Task<int> PromoteFromWaitlistAsync(ClassOccurrence occurrence, Guid userId)
    {
        var promoted = 0;
        var now = DateTime.UtcNow;

        while (occurrence.BookedCount < occurrence.Capacity)
        {
            var next = await db.ClassBookings.ForTenant(tenant)
                .Include(b => b.Member)
                .Where(b => b.ClassOccurrenceId == occurrence.Id && b.Status == BookingStatus.Waitlisted)
                .OrderBy(b => b.WaitlistPosition ?? int.MaxValue).ThenBy(b => b.BookedAt)
                .FirstOrDefaultAsync();

            if (next is null) break;

            next.Status = BookingStatus.Booked;
            next.PromotedAt = now;
            next.WaitlistPosition = null;
            next.StampUpdated(userId);

            // The held credit becomes a spent one — which is why it was held in the first place.
            if (next.SessionCreditMovementId is not null)
            {
                var movement = await db.CreditMovements.ForTenant(tenant)
                    .Include(m => m.SessionCredit)
                    .FirstOrDefaultAsync(m => m.Id == next.SessionCreditMovementId);

                if (movement?.WasHeld == true && movement.SessionCredit is not null)
                {
                    movement.SessionCredit.Held = Math.Max(0, movement.SessionCredit.Held - next.CreditsUsed);
                    movement.SessionCredit.Used += next.CreditsUsed;
                    movement.WasHeld = false;
                }
            }

            occurrence.BookedCount++;
            occurrence.WaitlistCount = Math.Max(0, occurrence.WaitlistCount - 1);

            if (next.MemberId is not null)
            {
                db.MessageLog.Add(new MessageLog
                {
                    MemberId = next.MemberId,
                    ClubId = occurrence.ClubId,
                    Channel = MessageChannel.Push,
                    Status = MessageStatus.Queued,
                    Subject = "You're in",
                    BodyPreview = $"A place opened up — you're booked into {occurrence.ClassType?.Name} " +
                                  $"on {occurrence.StartsAt:ddd d MMM} at {occurrence.StartsAt:HH:mm}.",
                    QueuedAt = now,
                }.StampNew(tenant, userId));
            }

            promoted++;
        }

        if (promoted > 0)
        {
            await ResequenceWaitlistAsync(occurrence.Id, userId);
            if (occurrence.BookedCount >= occurrence.Capacity) occurrence.Status = ClassOccurrenceStatus.Full;
        }

        return promoted;
    }

    private async Task ResequenceWaitlistAsync(Guid occurrenceId, Guid userId)
    {
        var waiting = await db.ClassBookings.ForTenant(tenant)
            .Where(b => b.ClassOccurrenceId == occurrenceId && b.Status == BookingStatus.Waitlisted)
            .OrderBy(b => b.WaitlistPosition ?? int.MaxValue).ThenBy(b => b.BookedAt)
            .ToListAsync();

        for (var i = 0; i < waiting.Count; i++)
        {
            waiting[i].WaitlistPosition = i + 1;
            waiting[i].StampUpdated(userId);
        }
    }

    private async Task<DateTime?> GetBookingBanAsync(Guid memberId, ClassOccurrence occurrence, DateTime now)
    {
        var policy = await ResolveCancellationPolicyAsync(occurrence);
        if (policy.StrikeThreshold <= 0) return null;

        var strikes = await db.Strikes.ForTenant(tenant)
            .Where(s => s.MemberId == memberId && !s.IsWaived && s.ExpiresOn > now)
            .OrderByDescending(s => s.OccurredOn)
            .ToListAsync();

        if (strikes.Count < policy.StrikeThreshold) return null;

        var banEnds = strikes[0].OccurredOn.AddDays(policy.BookingBanDays);
        return banEnds > now ? banEnds : null;
    }

    private async Task NotifyBookedAsync(ClassOccurrence occurrence, string message, Guid userId)
    {
        var bookings = await db.ClassBookings.ForTenant(tenant)
            .Where(b => b.ClassOccurrenceId == occurrence.Id
                     && b.MemberId != null
                     && (b.Status == BookingStatus.Booked || b.Status == BookingStatus.Waitlisted
                      || b.Status == BookingStatus.ClassCancelled))
            .ToListAsync();

        foreach (var booking in bookings)
        {
            db.MessageLog.Add(new MessageLog
            {
                MemberId = booking.MemberId,
                ClubId = occurrence.ClubId,
                Channel = MessageChannel.Push,
                Status = MessageStatus.Queued,
                Subject = "Class update",
                BodyPreview = message,
                QueuedAt = DateTime.UtcNow,
            }.StampNew(tenant, userId));
        }
    }

    private static (DateTime Start, DateTime End) MonthWindow(DateTime now)
        => (new DateTime(now.Year, now.Month, 1), new DateTime(now.Year, now.Month, 1).AddMonths(1));
}
