using Fitness.Application.DTOs;
using Fitness.Application.Services.Interfaces;
using Fitness.Domain.Entities;
using Fitness.Domain.Enums;
using Fitness.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;

namespace Fitness.Infrastructure.Services;

/// <summary>
/// Personal training, appointments and the session-credit ledger.
///
/// The credit ledger is the point. "How many sessions do I have left?" is the question personal
/// training runs on, and a club that answers it with a number it cannot explain will eventually
/// have an argument it cannot win. Every grant, consumption, expiry and refund is a row, so the
/// answer is always a list rather than an assertion.
///
/// Sign-off, not the clock, is what consumes a credit — a trainer marking the session delivered
/// is the commercial event, and it is also what accrues their commission.
/// </summary>
public class AppointmentService(
    FitnessDbContext db,
    IFitnessTenant tenant,
    FitnessNumbering numbering,
    IBillingService billing,
    IStaffService staff) : IAppointmentService
{
    // ── Bookable staff ───────────────────────────────────────────────────────

    public async Task<List<BookableStaffDto>> GetBookableStaffAsync(Guid? clubId, Guid? serviceId, bool activeOnly)
    {
        var now = DateTime.UtcNow;
        var weekStart = now.Date.AddDays(-(int)now.DayOfWeek);
        var weekEnd = weekStart.AddDays(7);

        var bookable = await db.BookableStaff.ForTenant(tenant)
            .WhereIf(clubId is not null, b => b.ClubId == clubId)
            .WhereIf(activeOnly, b => b.IsActive)
            .Include(b => b.Staff)
            .Include(b => b.Availability.Where(a => !a.IsDeleted))
            .OrderBy(b => b.DisplayName)
            .ToListAsync();

        if (serviceId is not null)
        {
            bookable = [.. bookable.Where(b =>
                string.IsNullOrWhiteSpace(b.ServiceIds) || b.ServiceIds.Contains(serviceId.ToString()!))];
        }

        var staffIds = bookable.Select(b => b.StaffId).ToList();

        // Materialised before the duration sum: PostgreSQL has no date-diff EF translation, and a
        // week of one club's sessions is a handful of rows either way.
        var weekSessions = await db.Appointments.ForTenant(tenant)
            .Where(a => staffIds.Contains(a.StaffId) && a.StartsAt >= weekStart && a.StartsAt < weekEnd
                     && a.Status != AppointmentStatus.Cancelled)
            .Select(a => new { a.StaffId, a.StartsAt, a.EndsAt })
            .ToListAsync();

        var sessions = weekSessions
            .GroupBy(a => a.StaffId)
            .Select(g => new
            {
                StaffId = g.Key,
                Count = g.Count(),
                Minutes = (int)g.Sum(a => (a.EndsAt - a.StartsAt).TotalMinutes),
            })
            .ToList();

        var clients = await db.CoachAssignments.ForTenant(tenant)
            .Where(c => staffIds.Contains(c.StaffId) && c.EndedOn == null)
            .GroupBy(c => c.StaffId)
            .Select(g => new { StaffId = g.Key, Count = g.Count() })
            .ToListAsync();

        return [.. bookable.Select(b =>
        {
            var dto = FitnessMapper.ToDto(b);

            var s = sessions.FirstOrDefault(x => x.StaffId == b.StaffId);
            dto.SessionsThisWeek = s?.Count ?? 0;
            dto.BookedHoursThisWeek = (s?.Minutes ?? 0) / 60;

            // Available hours come from the weekly pattern, less breaks.
            dto.AvailableHoursThisWeek = b.Availability
                .Where(a => !a.IsDeleted)
                .Sum(a => (int)((a.EndsAt - a.StartsAt).TotalHours
                    - (a.BreakStartsAt is not null && a.BreakEndsAt is not null
                        ? (a.BreakEndsAt.Value - a.BreakStartsAt.Value).TotalHours : 0)));

            dto.UtilisationPercent = FitnessMapper.Percent(dto.BookedHoursThisWeek, dto.AvailableHoursThisWeek);
            dto.ActiveClients = clients.FirstOrDefault(x => x.StaffId == b.StaffId)?.Count ?? 0;

            return dto;
        })];
    }

    public async Task<BookableStaffDto> SaveBookableStaffAsync(Guid? id, BookableStaffDto request, Guid userId)
    {
        BookableStaff bookable;
        if (id is null)
        {
            bookable = new BookableStaff { StaffId = request.StaffId, ClubId = request.ClubId }.StampNew(tenant, userId);
            db.BookableStaff.Add(bookable);
        }
        else
        {
            bookable = await db.BookableStaff.ForTenant(tenant)
                .Include(b => b.Availability.Where(a => !a.IsDeleted))
                .FirstOrDefaultAsync(b => b.Id == id)
                ?? throw new InvalidOperationException("Trainer profile not found.");
            bookable.StampUpdated(userId);
        }

        bookable.DisplayName = request.DisplayName;
        bookable.PhotoUrl = request.PhotoUrl;
        bookable.Bio = request.Bio;
        bookable.Specialities = request.Specialities;
        bookable.ServiceIds = request.ServiceIds.Count == 0 ? null : string.Join(',', request.ServiceIds);
        bookable.HourlyRate = request.HourlyRate;
        bookable.BookableOnline = request.BookableOnline;
        bookable.DefaultBufferMinutes = request.DefaultBufferMinutes;
        bookable.BookingWindowDays = request.BookingWindowDays;
        bookable.IsContractor = request.IsContractor;
        bookable.MaxClientsPerDay = request.MaxClientsPerDay;
        bookable.AcceptingNewClients = request.AcceptingNewClients;
        bookable.IsActive = request.IsActive;

        await db.SaveChangesAsync();

        var saved = await db.BookableStaff.ForTenant(tenant)
            .Include(b => b.Staff)
            .Include(b => b.Availability.Where(a => !a.IsDeleted))
            .FirstAsync(b => b.Id == bookable.Id);

        return FitnessMapper.ToDto(saved);
    }

    public async Task<List<StaffAvailabilityDto>> SaveAvailabilityAsync(
        Guid bookableStaffId, List<StaffAvailabilityDto> availability, Guid userId)
    {
        var existing = await db.Availability.ForTenant(tenant)
            .Where(a => a.BookableStaffId == bookableStaffId)
            .ToListAsync();

        foreach (var gone in existing) gone.StampDeleted(userId);

        foreach (var slot in availability)
        {
            if (slot.EndsAt <= slot.StartsAt)
                throw new InvalidOperationException("An availability window has to end after it starts.");

            db.Availability.Add(new StaffAvailability
            {
                BookableStaffId = bookableStaffId,
                ClubId = slot.ClubId,
                DayOfWeek = slot.DayOfWeek,
                StartsAt = slot.StartsAt,
                EndsAt = slot.EndsAt,
                BreakStartsAt = slot.BreakStartsAt,
                BreakEndsAt = slot.BreakEndsAt,
                EffectiveFrom = slot.EffectiveFrom,
                EffectiveTo = slot.EffectiveTo,
            }.StampNew(tenant, userId));
        }

        await db.SaveChangesAsync();

        var saved = await db.Availability.ForTenant(tenant)
            .Where(a => a.BookableStaffId == bookableStaffId)
            .OrderBy(a => a.DayOfWeek).ThenBy(a => a.StartsAt)
            .ToListAsync();

        return [.. saved.Select(FitnessMapper.ToDto)];
    }

    public async Task<StaffTimeOffDto> AddTimeOffAsync(StaffTimeOffDto request, Guid userId)
    {
        var clashes = await db.Appointments.ForTenant(tenant)
            .Where(a => a.StaffId == request.StaffId
                     && a.Status != AppointmentStatus.Cancelled
                     && a.StartsAt < request.EndsAt && a.EndsAt > request.StartsAt)
            .CountAsync();

        if (clashes > 0)
            throw new InvalidOperationException(
                $"{clashes} session(s) are booked in that window. Move or cancel them before blocking the time out.");

        var timeOff = new StaffTimeOff
        {
            StaffId = request.StaffId,
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            Reason = request.Reason,
            Note = request.Note,
            IsAllDay = request.IsAllDay,
            IsApproved = request.IsApproved,
            ApprovedByUserId = request.IsApproved ? userId : null,
        }.StampNew(tenant, userId);

        db.TimeOff.Add(timeOff);
        await db.SaveChangesAsync();

        return new StaffTimeOffDto
        {
            Id = timeOff.Id,
            StaffId = timeOff.StaffId,
            StartsAt = timeOff.StartsAt,
            EndsAt = timeOff.EndsAt,
            Reason = timeOff.Reason,
            Note = timeOff.Note,
            IsAllDay = timeOff.IsAllDay,
            IsApproved = timeOff.IsApproved,
        };
    }

    /// <summary>
    /// Open slots for a trainer, or across every trainer who can deliver the service.
    ///
    /// Walks the weekly pattern day by day and subtracts what is already booked, their breaks and
    /// their time off. The member's own preferred coach floats to the top, because "first
    /// available" that ignores who someone actually trains with is not helpful.
    /// </summary>
    public async Task<List<AvailabilitySlotDto>> FindAvailabilityAsync(AvailabilitySearchDto request)
    {
        var now = DateTime.UtcNow;

        var service = await db.Services.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == request.ServiceId)
            ?? throw new InvalidOperationException("Service not found.");

        var candidates = await db.BookableStaff.ForTenant(tenant)
            .Where(b => b.ClubId == request.ClubId && b.IsActive && b.BookableOnline)
            .WhereIf(!request.AnyStaff && request.StaffId is not null, b => b.StaffId == request.StaffId)
            .Include(b => b.Availability.Where(a => !a.IsDeleted))
            .Include(b => b.Staff)
            .ToListAsync();

        candidates = [.. candidates.Where(b =>
            string.IsNullOrWhiteSpace(b.ServiceIds) || b.ServiceIds.Contains(request.ServiceId.ToString()))];

        var staffIds = candidates.Select(c => c.StaffId).ToList();

        var booked = await db.Appointments.ForTenant(tenant)
            .Where(a => staffIds.Contains(a.StaffId)
                     && a.StartsAt < request.To && a.EndsAt > request.From
                     && a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.LateCancelled)
            .Select(a => new { a.StaffId, a.StartsAt, a.EndsAt })
            .ToListAsync();

        var timeOff = await db.TimeOff.ForTenant(tenant)
            .Where(t => staffIds.Contains(t.StaffId) && t.StartsAt < request.To && t.EndsAt > request.From)
            .Select(t => new { t.StaffId, t.StartsAt, t.EndsAt })
            .ToListAsync();

        var preferredCoachId = request.MemberId is null
            ? null
            : await db.CoachAssignments.ForTenant(tenant)
                .Where(c => c.MemberId == request.MemberId && c.EndedOn == null && c.IsPrimary)
                .Select(c => (Guid?)c.StaffId)
                .FirstOrDefaultAsync();

        var slots = new List<AvailabilitySlotDto>();
        var step = service.DurationMinutes + service.BufferMinutes;

        foreach (var trainer in candidates)
        {
            var horizon = request.To;
            var maxAhead = now.AddDays(trainer.BookingWindowDays);
            if (horizon > maxAhead) horizon = maxAhead;

            for (var day = request.From.Date; day <= horizon.Date; day = day.AddDays(1))
            {
                var pattern = trainer.Availability
                    .Where(a => !a.IsDeleted && a.DayOfWeek == (int)day.DayOfWeek)
                    .Where(a => (a.EffectiveFrom is null || a.EffectiveFrom <= day)
                             && (a.EffectiveTo is null || a.EffectiveTo >= day))
                    .ToList();

                foreach (var window in pattern)
                {
                    var cursor = day.Add(window.StartsAt);
                    var windowEnd = day.Add(window.EndsAt);

                    while (cursor.AddMinutes(service.DurationMinutes) <= windowEnd)
                    {
                        var slotEnd = cursor.AddMinutes(service.DurationMinutes);

                        var inPast = cursor <= now;
                        var inBreak = window.BreakStartsAt is not null && window.BreakEndsAt is not null
                            && cursor.TimeOfDay < window.BreakEndsAt.Value && slotEnd.TimeOfDay > window.BreakStartsAt.Value;

                        var clash = booked.Any(b => b.StaffId == trainer.StaffId
                                                 && cursor < b.EndsAt && slotEnd > b.StartsAt);

                        var off = timeOff.Any(t => t.StaffId == trainer.StaffId
                                                && cursor < t.EndsAt && slotEnd > t.StartsAt);

                        if (!inPast && !inBreak && !clash && !off)
                        {
                            slots.Add(new AvailabilitySlotDto
                            {
                                StaffId = trainer.StaffId,
                                StaffName = trainer.DisplayName,
                                PhotoUrl = trainer.PhotoUrl,
                                StartsAt = cursor,
                                EndsAt = slotEnd,
                                Price = service.Price,
                                IsPreferredCoach = trainer.StaffId == preferredCoachId,
                            });
                        }

                        cursor = cursor.AddMinutes(step);
                    }
                }
            }
        }

        return [.. slots
            .OrderByDescending(s => s.IsPreferredCoach)
            .ThenBy(s => s.StartsAt)
            .Take(200)];
    }

    // ── Appointments ─────────────────────────────────────────────────────────

    public async Task<PaginatedResponse<AppointmentSummaryDto>> ListAsync(
        Guid? clubId, Guid? staffId, Guid? memberId, AppointmentStatus? status,
        DateTime? from, DateTime? to, PaginationParams pagination)
    {
        var query = db.Appointments.ForTenant(tenant)
            .Include(a => a.Service)
            .Include(a => a.Member)
            .Include(a => a.Participants.Where(p => !p.IsDeleted))
            .WhereIf(clubId is not null, a => a.ClubId == clubId)
            .WhereIf(staffId is not null, a => a.StaffId == staffId)
            .WhereIf(memberId is not null, a => a.MemberId == memberId)
            .WhereIf(status is not null, a => a.Status == status)
            .WhereIf(from is not null, a => a.StartsAt >= from)
            .WhereIf(to is not null, a => a.StartsAt <= to);

        var total = await query.CountAsync();

        var page = await query
            .OrderByDescending(a => a.StartsAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        var items = await DecorateAsync(page);

        return PaginatedResponse<AppointmentSummaryDto>.Ok(
                   items,
                   total,
                   pagination.PageNumber,
                   pagination.PageSize);
    }

    public async Task<AppointmentDetailDto?> GetAsync(Guid appointmentId)
    {
        var now = DateTime.UtcNow;

        var appointment = await db.Appointments.ForTenant(tenant)
            .Include(a => a.Service)
            .Include(a => a.Member)
            .Include(a => a.Participants.Where(p => !p.IsDeleted)).ThenInclude(p => p.Member)
            .FirstOrDefaultAsync(a => a.Id == appointmentId);

        if (appointment is null) return null;

        var summary = FitnessMapper.ToSummary(appointment);

        var detail = new AppointmentDetailDto
        {
            Id = summary.Id,
            AppointmentNumber = summary.AppointmentNumber,
            ClubId = summary.ClubId,
            ServiceId = summary.ServiceId,
            ServiceName = summary.ServiceName,
            Kind = summary.Kind,
            ColourHex = summary.ColourHex,
            StaffId = summary.StaffId,
            MemberId = summary.MemberId,
            MemberName = summary.MemberName,
            MemberPhotoUrl = summary.MemberPhotoUrl,
            MemberPhone = summary.MemberPhone,
            Status = summary.Status,
            StartsAt = summary.StartsAt,
            EndsAt = summary.EndsAt,
            DurationMinutes = summary.DurationMinutes,
            CheckedInAt = summary.CheckedInAt,
            CompletedAt = summary.CompletedAt,
            ParticipantCount = summary.ParticipantCount,
            IsFirstSession = summary.IsFirstSession,
            IsSignedOff = summary.IsSignedOff,
            CreditsUsed = summary.CreditsUsed,
            AmountPaid = summary.AmountPaid,

            Channel = appointment.Channel,
            RoomId = appointment.RoomId,
            ResourceId = appointment.ResourceId,
            CancelledAt = appointment.CancelledAt,
            CancellationReason = appointment.CancellationReason,
            PaymentKind = appointment.PaymentKind,
            SessionPackagePurchaseId = appointment.SessionPackagePurchaseId,
            InvoiceId = appointment.InvoiceId,
            PenaltyCharged = appointment.PenaltyCharged,
            SeriesId = appointment.SeriesId,
            RescheduledFromId = appointment.RescheduledFromId,
            SessionNotes = appointment.SessionNotes,
            PlanForNextSession = appointment.PlanForNextSession,
            WorkoutId = appointment.WorkoutId,
            Participants = [.. appointment.Participants.Where(p => !p.IsDeleted).Select(FitnessMapper.ToDto)],
        };

        var trainer = await db.Staff.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == appointment.StaffId);
        detail.StaffName = trainer is null ? string.Empty : trainer.DisplayName ?? $"{trainer.FirstName} {trainer.LastName}";

        if (appointment.RoomId is not null)
        {
            detail.RoomName = await db.Rooms.ForTenant(tenant)
                .Where(r => r.Id == appointment.RoomId).Select(r => r.Name).FirstOrDefaultAsync();
        }

        if (appointment.WorkoutId is not null)
        {
            detail.WorkoutName = await db.Workouts.ForTenant(tenant)
                .Where(w => w.Id == appointment.WorkoutId).Select(w => w.Name).FirstOrDefaultAsync();
        }

        // What the trainer needs in front of them: the client's flags, their goals, and what
        // happened last time.
        if (appointment.MemberId is not null)
        {
            detail.MedicalFlags = [.. (await db.MedicalFlags.ForTenant(tenant)
                .Where(f => f.MemberId == appointment.MemberId && f.VisibleToInstructors && f.ResolvedOn == null)
                .ToListAsync())
                .Select(FitnessMapper.ToDto)];

            detail.Goals = [.. (await db.Goals.ForTenant(tenant)
                .Where(g => g.MemberId == appointment.MemberId && g.Status == GoalStatus.Active)
                .ToListAsync())
                .Select(g => FitnessMapper.ToDto(g, now))];

            var recent = await db.Appointments.ForTenant(tenant)
                .Where(a => a.MemberId == appointment.MemberId && a.Id != appointmentId
                         && a.Status == AppointmentStatus.Completed)
                .Include(a => a.Service)
                .Include(a => a.Participants)
                .OrderByDescending(a => a.StartsAt)
                .Take(3)
                .ToListAsync();

            detail.RecentSessions = [.. recent.Select(FitnessMapper.ToSummary)];

            detail.SessionsRemaining = await db.SessionCredits.ForTenant(tenant)
                .Where(c => c.MemberId == appointment.MemberId && !c.IsExpired
                         && c.Kind == EntitlementKind.PersonalTraining)
                .SumAsync(c => (int?)c.Remaining) ?? 0;
        }

        return detail;
    }

    public async Task<List<AppointmentSummaryDto>> GetDiaryAsync(Guid clubId, DateTime forDate, Guid? staffId)
    {
        var start = forDate.Date;
        var end = start.AddDays(1);

        var appointments = await db.Appointments.ForTenant(tenant)
            .Where(a => a.ClubId == clubId && a.StartsAt >= start && a.StartsAt < end)
            .WhereIf(staffId is not null, a => a.StaffId == staffId)
            .Include(a => a.Service)
            .Include(a => a.Member)
            .Include(a => a.Participants.Where(p => !p.IsDeleted))
            .OrderBy(a => a.StartsAt)
            .ToListAsync();

        return await DecorateAsync(appointments);
    }

    /// <summary>
    /// Books a session — or a whole recurring run of them.
    ///
    /// Conflicts are checked against the trainer, the room and the member, in that order, because
    /// a trainer double-booked is the failure that actually strands someone at reception.
    /// </summary>
    public async Task<AppointmentDetailDto> CreateAsync(CreateAppointmentDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var service = await db.Services.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == request.ServiceId)
            ?? throw new InvalidOperationException("Service not found.");

        var duration = request.DurationMinutesOverride ?? service.DurationMinutes;
        var starts = request.StartsAt;
        var ends = starts.AddMinutes(duration);

        if (!request.OverrideConflicts) await AssertNoConflictAsync(request.StaffId, request.RoomId, request.MemberId, starts, ends, null);

        Guid? seriesId = null;
        if (request.IsRecurring && request.OccurrenceCount > 1)
        {
            var series = new AppointmentSeries
            {
                ClubId = request.ClubId,
                StaffId = request.StaffId,
                MemberId = request.MemberId ?? Guid.Empty,
                ServiceId = request.ServiceId,
                DaysOfWeekMask = 1 << (int)starts.DayOfWeek,
                StartsAt = starts.TimeOfDay,
                DurationMinutes = duration,
                SeriesStart = starts.Date,
                OccurrenceCount = request.OccurrenceCount,
                RepeatEveryWeeks = request.RepeatEveryWeeks,
            }.StampNew(tenant, userId);

            db.AppointmentSeries.Add(series);
            seriesId = series.Id;
        }

        var occurrences = request.IsRecurring ? Math.Max(1, request.OccurrenceCount) : 1;
        Appointment? first = null;

        for (var i = 0; i < occurrences; i++)
        {
            var occurrenceStart = starts.AddDays(i * 7 * request.RepeatEveryWeeks);
            var occurrenceEnd = occurrenceStart.AddMinutes(duration);

            // A clash in the middle of a run stops the run rather than silently skipping it.
            if (i > 0 && !request.OverrideConflicts)
            {
                var clash = await HasConflictAsync(request.StaffId, request.RoomId, request.MemberId,
                    occurrenceStart, occurrenceEnd, null);

                if (clash) break;
            }

            var appointment = new Appointment
            {
                AppointmentNumber = await numbering.NextAppointmentNumberAsync(now),
                ClubId = request.ClubId,
                ServiceId = request.ServiceId,
                StaffId = request.StaffId,
                MemberId = request.MemberId,
                Status = AppointmentStatus.Confirmed,
                Channel = request.Channel,
                StartsAt = occurrenceStart,
                EndsAt = occurrenceEnd,
                RoomId = request.RoomId ?? service.RequiredRoomId,
                ResourceId = request.ResourceId ?? service.RequiredResourceId,
                PaymentKind = request.PaymentKind,
                SessionPackagePurchaseId = request.SessionPackagePurchaseId,
                SeriesId = seriesId,
            }.StampNew(tenant, userId);

            if (request.MemberId is not null)
            {
                appointment.IsFirstSession = !await db.Appointments.ForTenant(tenant)
                    .AnyAsync(a => a.MemberId == request.MemberId && a.Status == AppointmentStatus.Completed);
            }

            db.Appointments.Add(appointment);

            foreach (var participantId in request.AdditionalMemberIds.Distinct())
            {
                if (participantId == request.MemberId) continue;

                db.AppointmentParticipants.Add(new AppointmentParticipant
                {
                    AppointmentId = appointment.Id,
                    MemberId = participantId,
                    Status = AppointmentStatus.Confirmed,
                }.StampNew(tenant, userId));
            }

            // Drop-in sessions are paid for now; credit sessions are consumed at sign-off, so a
            // cancelled session never quietly eats one.
            if (request.PaymentKind == BookingPaymentKind.DropInPayment
                && request.PaymentMethod is not null && service.Price > 0 && request.MemberId is not null)
            {
                var payment = await billing.TakePaymentAsync(new TakePaymentDto
                {
                    MemberId = request.MemberId.Value,
                    ClubId = request.ClubId,
                    Amount = service.Price,
                    Method = request.PaymentMethod.Value,
                    CashSessionId = request.CashSessionId,
                    Notes = $"{service.Name} on {occurrenceStart:d MMM}",
                    IdempotencyKey = $"appt:{appointment.Id}",
                }, userId);

                appointment.AmountPaid = payment.AmountTaken;
            }

            first ??= appointment;
        }

        await db.SaveChangesAsync();
        return (await GetAsync(first!.Id))!;
    }

    public async Task<AppointmentDetailDto> RescheduleAsync(
        Guid appointmentId, DateTime newStart, Guid? newStaffId, Guid userId)
    {
        var appointment = await db.Appointments.ForTenant(tenant)
            .Include(a => a.Service)
            .FirstOrDefaultAsync(a => a.Id == appointmentId)
            ?? throw new InvalidOperationException("Appointment not found.");

        if (appointment.Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled)
            throw new InvalidOperationException("A completed or cancelled session cannot be moved.");

        var duration = (int)(appointment.EndsAt - appointment.StartsAt).TotalMinutes;
        var staffId = newStaffId ?? appointment.StaffId;
        var newEnd = newStart.AddMinutes(duration);

        await AssertNoConflictAsync(staffId, appointment.RoomId, appointment.MemberId, newStart, newEnd, appointmentId);

        // A move is a new record pointing at the old one, so the history reads as a chain rather
        // than as a booking that mysteriously changed time.
        var replacement = new Appointment
        {
            AppointmentNumber = await numbering.NextAppointmentNumberAsync(DateTime.UtcNow),
            ClubId = appointment.ClubId,
            ServiceId = appointment.ServiceId,
            StaffId = staffId,
            MemberId = appointment.MemberId,
            Status = AppointmentStatus.Confirmed,
            Channel = appointment.Channel,
            StartsAt = newStart,
            EndsAt = newEnd,
            RoomId = appointment.RoomId,
            ResourceId = appointment.ResourceId,
            PaymentKind = appointment.PaymentKind,
            SessionPackagePurchaseId = appointment.SessionPackagePurchaseId,
            AmountPaid = appointment.AmountPaid,
            RescheduledFromId = appointment.Id,
            IsFirstSession = appointment.IsFirstSession,
        }.StampNew(tenant, userId);

        db.Appointments.Add(replacement);

        appointment.Status = AppointmentStatus.Rescheduled;
        appointment.StampUpdated(userId);

        await db.SaveChangesAsync();
        return (await GetAsync(replacement.Id))!;
    }

    public async Task<AppointmentDetailDto> CancelAsync(
        Guid appointmentId, string? reason, bool waivePenalty, Guid userId)
    {
        var now = DateTime.UtcNow;

        var appointment = await db.Appointments.ForTenant(tenant)
            .Include(a => a.Service)
            .FirstOrDefaultAsync(a => a.Id == appointmentId)
            ?? throw new InvalidOperationException("Appointment not found.");

        if (appointment.Status == AppointmentStatus.Completed)
            throw new InvalidOperationException("A delivered session cannot be cancelled. Reverse the sign-off instead.");

        var service = appointment.Service;
        var hoursUntil = (appointment.StartsAt - now).TotalHours;
        var isLate = service is not null && hoursUntil < service.FreeCancelHours;

        appointment.Status = isLate && !waivePenalty ? AppointmentStatus.LateCancelled : AppointmentStatus.Cancelled;
        appointment.CancelledAt = now;
        appointment.CancellationReason = reason;
        appointment.StampUpdated(userId);

        if (isLate && !waivePenalty && service is not null && appointment.MemberId is not null)
        {
            // One-to-one cancellation policy is stricter than a class's, because the trainer's
            // hour is gone either way.
            if (service.LateCancelOutcome is PolicyOutcome.ForfeitCredit or PolicyOutcome.ForfeitCreditAndFee)
                await ConsumeCreditAsync(appointment, "Late cancellation", userId);

            if (service.LateCancelOutcome is PolicyOutcome.ChargeFee or PolicyOutcome.ForfeitCreditAndFee)
            {
                appointment.PenaltyCharged = service.Price;

                await billing.CreateAdHocInvoiceAsync(appointment.MemberId.Value, appointment.ClubId, [
                    new InvoiceLineDto
                    {
                        ChargeKind = ChargeKind.LateCancelFee,
                        LineDescription = $"Late cancellation — {service.Name} on {appointment.StartsAt:d MMM HH:mm}",
                        Quantity = 1,
                        UnitPrice = service.Price,
                        LineTotal = service.Price,
                    },
                ], userId);
            }
        }

        await db.SaveChangesAsync();
        return (await GetAsync(appointmentId))!;
    }

    public async Task<AppointmentDetailDto> CheckInAsync(Guid appointmentId, Guid? memberId, Guid userId)
    {
        var now = DateTime.UtcNow;

        var appointment = await db.Appointments.ForTenant(tenant)
            .Include(a => a.Participants.Where(p => !p.IsDeleted))
            .FirstOrDefaultAsync(a => a.Id == appointmentId)
            ?? throw new InvalidOperationException("Appointment not found.");

        if (memberId is not null && memberId != appointment.MemberId)
        {
            var participant = appointment.Participants.FirstOrDefault(p => p.MemberId == memberId)
                ?? throw new InvalidOperationException("That member is not on this session.");

            participant.Status = AppointmentStatus.CheckedIn;
            participant.CheckedInAt = now;
            participant.StampUpdated(userId);
        }
        else
        {
            appointment.Status = AppointmentStatus.CheckedIn;
            appointment.CheckedInAt = now;
            appointment.StampUpdated(userId);
        }

        await db.SaveChangesAsync();
        return (await GetAsync(appointmentId))!;
    }

    /// <summary>
    /// The trainer marking a session delivered.
    ///
    /// This is the commercial event, not the calendar: it consumes the credit, accrues the
    /// trainer's commission, and releases the deferred revenue that credit was carrying. Doing it
    /// on the clock instead would pay trainers for sessions nobody turned up to.
    /// </summary>
    public async Task<AppointmentDetailDto> SignOffAsync(SignOffSessionDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var appointment = await db.Appointments.ForTenant(tenant)
            .Include(a => a.Service)
            .Include(a => a.Participants.Where(p => !p.IsDeleted))
            .FirstOrDefaultAsync(a => a.Id == request.AppointmentId)
            ?? throw new InvalidOperationException("Appointment not found.");

        if (appointment.CompletedAt is not null)
            throw new InvalidOperationException("This session has already been signed off.");

        appointment.Status = AppointmentStatus.Completed;
        appointment.CompletedAt = now;
        appointment.SessionNotes = request.SessionNotes ?? appointment.SessionNotes;
        appointment.PlanForNextSession = request.PlanForNextSession ?? appointment.PlanForNextSession;
        appointment.StampUpdated(userId);

        var sessionValue = appointment.Service?.Price ?? 0m;

        var signOff = new SessionSignOff
        {
            AppointmentId = appointment.Id,
            MemberId = appointment.MemberId ?? Guid.Empty,
            StaffId = appointment.StaffId,
            SignedOffAt = now,
            MemberConfirmed = request.MemberConfirmed,
            MemberSignatureUrl = request.MemberSignatureUrl,
            CreditsConsumed = request.CreditsConsumed,
            SessionValue = sessionValue,
            Note = request.SessionNotes,
        }.StampNew(tenant, userId);

        db.SignOffs.Add(signOff);

        if (appointment.PaymentKind == BookingPaymentKind.PackCredit)
        {
            await ConsumeCreditAsync(appointment, "Session delivered", userId);

            foreach (var participant in appointment.Participants.Where(p => !p.IsDeleted))
                await ConsumeParticipantCreditAsync(participant, appointment, userId);
        }

        // The trainer's commission accrues the moment the session is delivered, so their
        // "earned this period" figure is live rather than a month-end surprise.
        var accruals = await staff.AccrueAsync(
            appointment.StaffId,
            CommissionBasis.PerSessionDelivered,
            sessionValue,
            1,
            appointment.Id,
            nameof(Appointment),
            appointment.MemberId,
            $"{appointment.Service?.Name} on {appointment.StartsAt:d MMM}",
            userId);

        if (accruals.Count > 0)
        {
            signOff.CommissionAccrued = true;
            signOff.CommissionAccrualId = accruals[0].Id;
        }

        await db.SaveChangesAsync();
        return (await GetAsync(appointment.Id))!;
    }

    public async Task<AppointmentDetailDto> MarkNoShowAsync(
        Guid appointmentId, Guid? memberId, bool waivePenalty, Guid userId)
    {
        var appointment = await db.Appointments.ForTenant(tenant)
            .Include(a => a.Service)
            .Include(a => a.Participants.Where(p => !p.IsDeleted))
            .FirstOrDefaultAsync(a => a.Id == appointmentId)
            ?? throw new InvalidOperationException("Appointment not found.");

        var service = appointment.Service;

        if (memberId is not null && memberId != appointment.MemberId)
        {
            var participant = appointment.Participants.FirstOrDefault(p => p.MemberId == memberId);
            if (participant is not null)
            {
                participant.Status = AppointmentStatus.NoShow;
                participant.StampUpdated(userId);

                if (!waivePenalty && service?.NoShowOutcome
                        is PolicyOutcome.ForfeitCredit or PolicyOutcome.ForfeitCreditAndFee)
                    await ConsumeParticipantCreditAsync(participant, appointment, userId);
            }
        }
        else
        {
            appointment.Status = AppointmentStatus.NoShow;
            appointment.StampUpdated(userId);

            if (!waivePenalty && service is not null && appointment.MemberId is not null)
            {
                if (service.NoShowOutcome is PolicyOutcome.ForfeitCredit or PolicyOutcome.ForfeitCreditAndFee)
                    await ConsumeCreditAsync(appointment, "No-show", userId);

                if (service.NoShowOutcome is PolicyOutcome.ChargeFee or PolicyOutcome.ForfeitCreditAndFee)
                {
                    appointment.PenaltyCharged = service.Price;

                    await billing.CreateAdHocInvoiceAsync(appointment.MemberId.Value, appointment.ClubId, [
                        new InvoiceLineDto
                        {
                            ChargeKind = ChargeKind.NoShowFee,
                            LineDescription = $"No-show — {service.Name} on {appointment.StartsAt:d MMM HH:mm}",
                            Quantity = 1,
                            UnitPrice = service.Price,
                            LineTotal = service.Price,
                        },
                    ], userId);
                }
            }
        }

        await db.SaveChangesAsync();
        return (await GetAsync(appointmentId))!;
    }

    // ── Packages & credits ───────────────────────────────────────────────────

    /// <summary>
    /// Selling a block of sessions.
    ///
    /// The money and the credits are separate records because they have different lifetimes: the
    /// cash arrives today, the revenue is earned session by session, and the credits can expire
    /// with the money already collected. That gap is exactly the deferred-revenue liability, and
    /// it is created here rather than discovered at year end.
    /// </summary>
    public async Task<SessionPackagePurchaseDto> SellPackageAsync(SellPackageDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var plan = request.PlanId is null
            ? null
            : await db.Plans.ForTenant(tenant).FirstOrDefaultAsync(p => p.Id == request.PlanId);

        var service = request.ServiceId is null
            ? null
            : await db.Services.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == request.ServiceId);

        var sessions = request.Sessions > 0 ? request.Sessions : plan?.CreditCount ?? 1;
        var total = request.PriceOverride ?? plan?.Price ?? (service?.Price ?? 0m) * sessions;
        var perSession = sessions == 0 ? 0 : Math.Round(total / sessions, 2);

        var expires = request.ExpiresOn
            ?? (plan?.ValidForDays > 0 ? now.Date.AddDays(plan.ValidForDays) : null);

        var purchase = new SessionPackagePurchase
        {
            PurchaseNumber = await numbering.NextPackageNumberAsync(now),
            MemberId = request.MemberId,
            ClubId = request.ClubId,
            PlanId = request.PlanId,
            ServiceId = request.ServiceId,
            StaffId = request.StaffId,
            PurchasedOn = now,
            SessionsPurchased = sessions,
            SessionsRemaining = sessions,
            TotalPrice = total,
            PricePerSession = perSession,
            ExpiresOn = expires,
            SoldByStaffId = request.SoldByStaffId,
            IsTransferable = plan?.CreditsTransferable ?? false,
            IsRefundable = plan?.CreditsRefundable ?? false,
        }.StampNew(tenant, userId);

        db.PackagePurchases.Add(purchase);

        var credit = new SessionCredit
        {
            MemberId = request.MemberId,
            SessionPackagePurchaseId = purchase.Id,
            ServiceId = request.ServiceId,
            Kind = EntitlementKind.PersonalTraining,
            Granted = sessions,
            Remaining = sessions,
            ExpiresOn = expires,
            UnitValue = perSession,
        }.StampNew(tenant, userId);

        db.SessionCredits.Add(credit);

        db.CreditMovements.Add(new SessionCreditMovement
        {
            SessionCreditId = credit.Id,
            MemberId = request.MemberId,
            Kind = SessionCreditMovementKind.Purchased,
            OccurredAt = now,
            Quantity = sessions,
            BalanceAfter = sessions,
            Note = $"Bought {sessions} sessions",
        }.StampNew(tenant, userId));

        var invoice = await billing.CreateAdHocInvoiceAsync(request.MemberId, request.ClubId, [
            new InvoiceLineDto
            {
                ChargeKind = ChargeKind.SessionPackage,
                LineDescription = plan?.Name ?? $"{sessions} × {service?.Name ?? "session"}",
                Quantity = sessions,
                UnitPrice = perSession,
                LineTotal = total,
                PlanId = request.PlanId,
            },
        ], userId);

        purchase.InvoiceId = invoice.Id;

        // Earned per session taken, not on the day the money arrives.
        var deferred = new DeferredRevenueSchedule
        {
            MemberId = request.MemberId,
            InvoiceId = invoice.Id,
            ClubId = request.ClubId,
            Basis = RevenueRecognitionBasis.OnConsumption,
            TotalAmount = total,
            RemainingAmount = total,
            ServiceStart = now.Date,
            ServiceEnd = expires ?? now.Date.AddYears(1),
            TotalUnits = sessions,
            RevenueAccountId = plan?.RevenueAccountId,
            DeferredAccountId = plan?.DeferredRevenueAccountId,
        }.StampNew(tenant, userId);

        db.DeferredRevenue.Add(deferred);
        purchase.DeferredRevenueScheduleId = deferred.Id;

        if (request.TakePaymentNow && total > 0)
        {
            await billing.TakePaymentAsync(new TakePaymentDto
            {
                MemberId = request.MemberId,
                ClubId = request.ClubId,
                InvoiceId = invoice.Id,
                Amount = total,
                Method = request.PaymentMethod,
                CashSessionId = request.CashSessionId,
                Notes = $"Session package {purchase.PurchaseNumber}",
                IdempotencyKey = $"package:{purchase.Id}",
            }, userId);
        }

        // Selling a package is a commissionable act in its own right, separately from delivering it.
        if (request.SoldByStaffId is not null)
        {
            await staff.AccrueAsync(request.SoldByStaffId.Value, CommissionBasis.PercentOfPackageSold,
                total, 1, purchase.Id, nameof(SessionPackagePurchase), request.MemberId,
                $"Sold {sessions} sessions", userId);
        }

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(purchase, now);
    }

    public async Task<List<SessionPackagePurchaseDto>> GetPackagesAsync(Guid? clubId, Guid? memberId, bool activeOnly)
    {
        var now = DateTime.UtcNow;

        var packages = await db.PackagePurchases.ForTenant(tenant)
            .WhereIf(clubId is not null, p => p.ClubId == clubId)
            .WhereIf(memberId is not null, p => p.MemberId == memberId)
            .WhereIf(activeOnly, p => !p.IsExpired && p.SessionsRemaining > 0)
            .Include(p => p.Member)
            .OrderByDescending(p => p.PurchasedOn)
            .ToListAsync();

        var serviceNames = await db.Services.ForTenant(tenant)
            .Select(s => new { s.Id, s.Name }).ToDictionaryAsync(s => s.Id, s => s.Name);

        var planNames = await db.Plans.ForTenant(tenant)
            .Select(p => new { p.Id, p.Name }).ToDictionaryAsync(p => p.Id, p => p.Name);

        var staffNames = await db.Staff.ForTenant(tenant)
            .Select(s => new { s.Id, Name = s.DisplayName ?? s.FirstName + " " + s.LastName })
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        return [.. packages.Select(p =>
        {
            var dto = FitnessMapper.ToDto(p, now);
            if (p.ServiceId is not null) dto.ServiceName = serviceNames.GetValueOrDefault(p.ServiceId.Value);
            if (p.PlanId is not null) dto.PlanName = planNames.GetValueOrDefault(p.PlanId.Value);
            if (p.StaffId is not null) dto.StaffName = staffNames.GetValueOrDefault(p.StaffId.Value);
            if (p.SoldByStaffId is not null) dto.SoldByName = staffNames.GetValueOrDefault(p.SoldByStaffId.Value);
            return dto;
        })];
    }

    public async Task<List<SessionCreditDto>> GetCreditsAsync(Guid memberId)
    {
        var credits = await db.SessionCredits.ForTenant(tenant)
            .Where(c => c.MemberId == memberId)
            .Include(c => c.Movements.Where(m => !m.IsDeleted))
            .OrderBy(c => c.IsExpired).ThenBy(c => c.ExpiresOn ?? DateTime.MaxValue)
            .ToListAsync();

        var serviceNames = await db.Services.ForTenant(tenant)
            .Select(s => new { s.Id, s.Name }).ToDictionaryAsync(s => s.Id, s => s.Name);

        var classTypeNames = await db.ClassTypes.ForTenant(tenant)
            .Select(c => new { c.Id, c.Name }).ToDictionaryAsync(c => c.Id, c => c.Name);

        return [.. credits.Select(c =>
        {
            var dto = FitnessMapper.ToDto(c);
            if (c.ServiceId is not null) dto.ServiceName = serviceNames.GetValueOrDefault(c.ServiceId.Value);
            if (c.ClassTypeId is not null) dto.ClassTypeName = classTypeNames.GetValueOrDefault(c.ClassTypeId.Value);
            dto.Movements = [.. c.Movements
                .Where(m => !m.IsDeleted)
                .OrderByDescending(m => m.OccurredAt)
                .Take(50)
                .Select(FitnessMapper.ToDto)];
            return dto;
        })];
    }

    public async Task<SessionCreditDto> AdjustCreditsAsync(AdjustCreditsDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new InvalidOperationException("Adjusting credits needs a reason — it is money.");

        var credit = request.SessionCreditId is not null
            ? await db.SessionCredits.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == request.SessionCreditId)
            : await db.SessionCredits.ForTenant(tenant)
                .Where(c => c.MemberId == request.MemberId && c.Kind == EntitlementKind.PersonalTraining)
                .OrderBy(c => c.ExpiresOn ?? DateTime.MaxValue)
                .FirstOrDefaultAsync();

        if (credit is null)
        {
            credit = new SessionCredit
            {
                MemberId = request.MemberId,
                Kind = EntitlementKind.PersonalTraining,
                ExpiresOn = request.NewExpiryOn,
            }.StampNew(tenant, userId);

            db.SessionCredits.Add(credit);
        }

        credit.Granted += Math.Max(0, request.Quantity);
        credit.Remaining += request.Quantity;
        if (credit.Remaining < 0) credit.Remaining = 0;
        if (request.NewExpiryOn is not null)
        {
            credit.ExpiresOn = request.NewExpiryOn;
            credit.IsExpired = request.NewExpiryOn < now;
        }

        credit.StampUpdated(userId);

        db.CreditMovements.Add(new SessionCreditMovement
        {
            SessionCreditId = credit.Id,
            MemberId = request.MemberId,
            Kind = request.Quantity >= 0 ? SessionCreditMovementKind.Granted : SessionCreditMovementKind.Adjusted,
            OccurredAt = now,
            Quantity = request.Quantity,
            BalanceAfter = credit.Remaining,
            Note = request.Reason,
            PerformedByStaffId = userId,
        }.StampNew(tenant, userId));

        await db.SaveChangesAsync();

        var saved = await db.SessionCredits.ForTenant(tenant)
            .Include(c => c.Movements.Where(m => !m.IsDeleted))
            .FirstAsync(c => c.Id == credit.Id);

        return FitnessMapper.ToDto(saved);
    }

    /// <summary>
    /// Expires credits whose date has passed, releasing the revenue they were holding.
    ///
    /// The revenue release matters: an expired pack is money the club has genuinely earned — the
    /// member bought availability and did not use it — and leaving it deferred forever
    /// understates income indefinitely.
    /// </summary>
    public async Task<int> ExpireDueCreditsAsync()
    {
        var now = DateTime.UtcNow;
        var expired = 0;

        var due = await db.SessionCredits.ForTenant(tenant)
            .Where(c => !c.IsExpired && c.ExpiresOn != null && c.ExpiresOn < now && c.Remaining > 0)
            .ToListAsync();

        foreach (var credit in due)
        {
            var lost = credit.Remaining;

            db.CreditMovements.Add(new SessionCreditMovement
            {
                SessionCreditId = credit.Id,
                MemberId = credit.MemberId,
                Kind = SessionCreditMovementKind.Expired,
                OccurredAt = now,
                Quantity = -lost,
                BalanceAfter = 0,
                Note = $"Expired on {credit.ExpiresOn:d MMM yyyy}",
            }.StampNew(tenant));

            credit.Remaining = 0;
            credit.IsExpired = true;

            if (credit.SessionPackagePurchaseId is not null)
            {
                var purchase = await db.PackagePurchases.ForTenant(tenant)
                    .FirstOrDefaultAsync(p => p.Id == credit.SessionPackagePurchaseId);

                if (purchase is not null)
                {
                    purchase.SessionsRemaining = 0;
                    purchase.IsExpired = true;

                    if (purchase.DeferredRevenueScheduleId is not null)
                        await RecogniseRemainingAsync(purchase.DeferredRevenueScheduleId.Value, lost, "Credits expired");
                }
            }

            expired++;
        }

        // Packages whose date passed even with nothing left to lose.
        var stalePackages = await db.PackagePurchases.ForTenant(tenant)
            .Where(p => !p.IsExpired && p.ExpiresOn != null && p.ExpiresOn < now)
            .ToListAsync();

        foreach (var package in stalePackages) package.IsExpired = true;

        await db.SaveChangesAsync();
        return expired;
    }

    public async Task<CoachAssignmentDto> AssignCoachAsync(Guid memberId, Guid staffId, bool isPrimary, Guid userId)
    {
        var now = DateTime.UtcNow;

        var member = await db.Members.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == memberId)
            ?? throw new InvalidOperationException("Member not found.");

        if (isPrimary)
        {
            var current = await db.CoachAssignments.ForTenant(tenant)
                .Where(c => c.MemberId == memberId && c.IsPrimary && c.EndedOn == null)
                .ToListAsync();

            foreach (var old in current)
            {
                old.EndedOn = now;
                old.EndReason = "Reassigned";
                old.StampUpdated(userId);
            }

            member.AssignedCoachId = staffId;
            member.StampUpdated(userId);
        }

        var assignment = new CoachAssignment
        {
            MemberId = memberId,
            StaffId = staffId,
            ClubId = member.HomeClubId,
            AssignedOn = now,
            IsPrimary = isPrimary,
            AssignedByUserId = userId,
        }.StampNew(tenant, userId);

        db.CoachAssignments.Add(assignment);
        await db.SaveChangesAsync();

        var staffName = await db.Staff.ForTenant(tenant)
            .Where(s => s.Id == staffId)
            .Select(s => s.DisplayName ?? s.FirstName + " " + s.LastName)
            .FirstOrDefaultAsync();

        return new CoachAssignmentDto
        {
            Id = assignment.Id,
            MemberId = memberId,
            MemberName = FitnessMapper.FullName(member),
            StaffId = staffId,
            StaffName = staffName,
            ClubId = assignment.ClubId,
            AssignedOn = assignment.AssignedOn,
            IsPrimary = isPrimary,
        };
    }

    public async Task<List<CoachAssignmentDto>> GetCoachClientsAsync(Guid staffId, bool activeOnly)
    {
        var assignments = await db.CoachAssignments.ForTenant(tenant)
            .Where(c => c.StaffId == staffId)
            .WhereIf(activeOnly, c => c.EndedOn == null)
            .Include(c => c.Member)
            .OrderBy(c => c.Member!.LastName)
            .ToListAsync();

        return [.. assignments.Select(a => new CoachAssignmentDto
        {
            Id = a.Id,
            MemberId = a.MemberId,
            MemberName = a.Member is null ? null : FitnessMapper.FullName(a.Member),
            StaffId = a.StaffId,
            ClubId = a.ClubId,
            AssignedOn = a.AssignedOn,
            EndedOn = a.EndedOn,
            EndReason = a.EndReason,
            IsPrimary = a.IsPrimary,
        })];
    }

    /// <summary>
    /// The trainer's own screen.
    ///
    /// Their diary, their clients who are drifting, the check-ins they owe replies to, and what
    /// they have earned. A trainer who can see their commission accruing during the month behaves
    /// differently from one who finds out on payday.
    /// </summary>
    public async Task<TrainerDayDto> GetTrainerDayAsync(Guid staffId, DateTime forDate)
    {
        var now = DateTime.UtcNow;
        var start = forDate.Date;
        var end = start.AddDays(1);
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var trainer = await db.Staff.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == staffId)
            ?? throw new InvalidOperationException("Staff member not found.");

        var dto = new TrainerDayDto
        {
            StaffId = staffId,
            StaffName = trainer.DisplayName ?? $"{trainer.FirstName} {trainer.LastName}",
            ClubId = trainer.ClubId,
            ForDate = start,
        };

        var appointments = await db.Appointments.ForTenant(tenant)
            .Where(a => a.StaffId == staffId && a.StartsAt >= start && a.StartsAt < end
                     && a.Status != AppointmentStatus.Cancelled)
            .Include(a => a.Service)
            .Include(a => a.Member)
            .Include(a => a.Participants.Where(p => !p.IsDeleted))
            .OrderBy(a => a.StartsAt)
            .ToListAsync();

        dto.Appointments = await DecorateAsync(appointments);
        dto.SessionsToday = appointments.Count;
        dto.SessionsCompleted = appointments.Count(a => a.Status == AppointmentStatus.Completed);
        dto.HoursBooked = Math.Round((decimal)appointments.Sum(a => (a.EndsAt - a.StartsAt).TotalHours), 1);

        dto.Classes = [.. (await db.ClassOccurrences.ForTenant(tenant)
            .Where(o => (o.InstructorStaffId == staffId || o.SubstituteStaffId == staffId)
                     && o.StartsAt >= start && o.StartsAt < end
                     && o.Status != ClassOccurrenceStatus.Cancelled)
            .Include(o => o.ClassType)
            .Include(o => o.Room)
            .OrderBy(o => o.StartsAt)
            .ToListAsync())
            .Select(o => FitnessMapper.ToSummary(o, now))];

        dto.DueCheckIns = [.. (await db.CoachCheckIns.ForTenant(tenant)
            .Where(c => c.StaffId == staffId && !c.IsComplete && c.DueOn <= now.AddDays(2))
            .Include(c => c.Member)
            .OrderBy(c => c.DueOn)
            .Take(10)
            .ToListAsync())
            .Select(c => new CoachCheckInDto
            {
                Id = c.Id,
                MemberId = c.MemberId,
                MemberName = c.Member is null ? null : FitnessMapper.FullName(c.Member),
                MemberPhotoUrl = c.Member?.PhotoUrl,
                StaffId = c.StaffId,
                DueOn = c.DueOn,
                PeriodStart = c.PeriodStart,
                PeriodEnd = c.PeriodEnd,
                MemberSubmittedAt = c.MemberSubmittedAt,
                CoachRepliedAt = c.CoachRepliedAt,
                AwaitingCoach = c.MemberSubmittedAt is not null && c.CoachRepliedAt is null,
                AwaitingMember = c.MemberSubmittedAt is null,
                IsOverdue = c.DueOn < now && !c.IsComplete,
            })];

        dto.Tasks = [.. (await db.RetentionTasks.ForTenant(tenant)
            .Where(t => t.AssignedStaffId == staffId && t.CompletedAt == null && !t.IsDismissed)
            .Include(t => t.Member)
            .OrderBy(t => t.DueOn)
            .Take(10)
            .ToListAsync())
            .Select(t => FitnessMapper.ToDto(t, now))];

        dto.CommissionThisPeriod = await db.CommissionAccruals.ForTenant(tenant)
            .Where(a => a.StaffId == staffId && a.EarnedOn >= monthStart && !a.IsReversed)
            .SumAsync(a => (decimal?)a.Amount) ?? 0m;

        var clientIds = await db.CoachAssignments.ForTenant(tenant)
            .Where(c => c.StaffId == staffId && c.EndedOn == null)
            .Select(c => c.MemberId)
            .ToListAsync();

        dto.ActiveClients = clientIds.Count;

        dto.ClientsAtRisk = await db.ChurnScores.ForTenant(tenant)
            .CountAsync(c => clientIds.Contains(c.MemberId)
                          && (c.Band == ChurnRiskBand.AtRisk || c.Band == ChurnRiskBand.Critical));

        var openClock = await db.TimeClock.ForTenant(tenant)
            .FirstOrDefaultAsync(t => t.StaffId == staffId && t.ClockedOutAt == null);

        dto.IsClockedIn = openClock is not null;

        return dto;
    }

    // ═══ Internals ═══════════════════════════════════════════════════════════

    private async Task<List<AppointmentSummaryDto>> DecorateAsync(List<Appointment> appointments)
    {
        var staffIds = appointments.Select(a => a.StaffId).Distinct().ToList();
        var names = await db.Staff.ForTenant(tenant)
            .Where(s => staffIds.Contains(s.Id))
            .Select(s => new { s.Id, Name = s.DisplayName ?? s.FirstName + " " + s.LastName })
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        var roomIds = appointments.Where(a => a.RoomId is not null).Select(a => a.RoomId!.Value).Distinct().ToList();
        var rooms = await db.Rooms.ForTenant(tenant)
            .Where(r => roomIds.Contains(r.Id))
            .Select(r => new { r.Id, r.Name })
            .ToDictionaryAsync(r => r.Id, r => r.Name);

        var signedOff = await db.SignOffs.ForTenant(tenant)
            .Where(s => appointments.Select(a => a.Id).Contains(s.AppointmentId))
            .Select(s => s.AppointmentId)
            .ToListAsync();

        var signedSet = signedOff.ToHashSet();

        return [.. appointments.Select(a =>
        {
            var dto = FitnessMapper.ToSummary(a);
            dto.StaffName = names.GetValueOrDefault(a.StaffId, string.Empty);
            if (a.RoomId is not null) dto.RoomName = rooms.GetValueOrDefault(a.RoomId.Value);
            dto.IsSignedOff = signedSet.Contains(a.Id);
            return dto;
        })];
    }

    private async Task AssertNoConflictAsync(
        Guid staffId, Guid? roomId, Guid? memberId, DateTime starts, DateTime ends, Guid? excludeId)
    {
        var trainerClash = await db.Appointments.ForTenant(tenant)
            .Where(a => a.StaffId == staffId && a.Id != excludeId
                     && a.Status != AppointmentStatus.Cancelled && a.Status != AppointmentStatus.LateCancelled
                     && a.Status != AppointmentStatus.Rescheduled
                     && a.StartsAt < ends && a.EndsAt > starts)
            .Include(a => a.Member)
            .FirstOrDefaultAsync();

        if (trainerClash is not null)
            throw new InvalidOperationException(
                $"That trainer already has a session at {trainerClash.StartsAt:HH:mm}" +
                (trainerClash.Member is null ? "." : $" with {FitnessMapper.FullName(trainerClash.Member)}."));

        var offClash = await db.TimeOff.ForTenant(tenant)
            .FirstOrDefaultAsync(t => t.StaffId == staffId && t.StartsAt < ends && t.EndsAt > starts);

        if (offClash is not null)
            throw new InvalidOperationException($"That trainer is unavailable then — {offClash.Reason}.");

        var classClash = await db.ClassOccurrences.ForTenant(tenant)
            .Where(o => (o.InstructorStaffId == staffId || o.SubstituteStaffId == staffId)
                     && o.Status != ClassOccurrenceStatus.Cancelled
                     && o.StartsAt < ends && o.EndsAt > starts)
            .Include(o => o.ClassType)
            .FirstOrDefaultAsync();

        if (classClash is not null)
            throw new InvalidOperationException(
                $"That trainer is teaching {classClash.ClassType?.Name} at {classClash.StartsAt:HH:mm}.");

        if (roomId is not null)
        {
            var roomClash = await db.Appointments.ForTenant(tenant)
                .AnyAsync(a => a.RoomId == roomId && a.Id != excludeId
                            && a.Status != AppointmentStatus.Cancelled
                            && a.StartsAt < ends && a.EndsAt > starts);

            if (roomClash) throw new InvalidOperationException("That room is already booked at that time.");
        }

        if (memberId is not null)
        {
            var memberClash = await db.Appointments.ForTenant(tenant)
                .AnyAsync(a => a.MemberId == memberId && a.Id != excludeId
                            && a.Status != AppointmentStatus.Cancelled
                            && a.StartsAt < ends && a.EndsAt > starts);

            if (memberClash) throw new InvalidOperationException("That member already has a session booked then.");
        }
    }

    private async Task<bool> HasConflictAsync(
        Guid staffId, Guid? roomId, Guid? memberId, DateTime starts, DateTime ends, Guid? excludeId)
    {
        try
        {
            await AssertNoConflictAsync(staffId, roomId, memberId, starts, ends, excludeId);
            return false;
        }
        catch (InvalidOperationException)
        {
            return true;
        }
    }

    private async Task ConsumeCreditAsync(Appointment appointment, string reason, Guid userId)
    {
        if (appointment.MemberId is null) return;

        var credit = await db.SessionCredits.ForTenant(tenant)
            .Where(c => c.MemberId == appointment.MemberId && !c.IsExpired && c.Remaining > 0
                     && c.Kind == EntitlementKind.PersonalTraining
                     && (c.ServiceId == null || c.ServiceId == appointment.ServiceId))
            .OrderBy(c => c.ExpiresOn ?? DateTime.MaxValue)
            .FirstOrDefaultAsync();

        if (credit is null) return;

        credit.Used += appointment.CreditsUsed;
        credit.Remaining -= appointment.CreditsUsed;

        var movement = new SessionCreditMovement
        {
            SessionCreditId = credit.Id,
            MemberId = appointment.MemberId.Value,
            Kind = reason.Contains("No-show", StringComparison.OrdinalIgnoreCase)
                ? SessionCreditMovementKind.ForfeitedNoShow
                : SessionCreditMovementKind.Consumed,
            OccurredAt = DateTime.UtcNow,
            Quantity = -appointment.CreditsUsed,
            BalanceAfter = credit.Remaining,
            AppointmentId = appointment.Id,
            Note = reason,
        }.StampNew(tenant, userId);

        db.CreditMovements.Add(movement);
        appointment.SessionCreditMovementId = movement.Id;

        if (credit.SessionPackagePurchaseId is not null)
        {
            var purchase = await db.PackagePurchases.ForTenant(tenant)
                .FirstOrDefaultAsync(p => p.Id == credit.SessionPackagePurchaseId);

            if (purchase is not null)
            {
                purchase.SessionsUsed += appointment.CreditsUsed;
                purchase.SessionsRemaining = Math.Max(0, purchase.SessionsRemaining - appointment.CreditsUsed);

                // A consumed session is revenue the club has now earned.
                if (purchase.DeferredRevenueScheduleId is not null)
                    await RecogniseRemainingAsync(purchase.DeferredRevenueScheduleId.Value,
                        appointment.CreditsUsed, "Session delivered");
            }
        }
    }

    private async Task ConsumeParticipantCreditAsync(
        AppointmentParticipant participant, Appointment appointment, Guid userId)
    {
        var credit = await db.SessionCredits.ForTenant(tenant)
            .Where(c => c.MemberId == participant.MemberId && !c.IsExpired && c.Remaining > 0
                     && c.Kind == EntitlementKind.PersonalTraining)
            .OrderBy(c => c.ExpiresOn ?? DateTime.MaxValue)
            .FirstOrDefaultAsync();

        if (credit is null) return;

        credit.Used += participant.CreditsUsed;
        credit.Remaining -= participant.CreditsUsed;

        var movement = new SessionCreditMovement
        {
            SessionCreditId = credit.Id,
            MemberId = participant.MemberId,
            Kind = SessionCreditMovementKind.Consumed,
            OccurredAt = DateTime.UtcNow,
            Quantity = -participant.CreditsUsed,
            BalanceAfter = credit.Remaining,
            AppointmentId = appointment.Id,
            Note = "Small-group session delivered",
        }.StampNew(tenant, userId);

        db.CreditMovements.Add(movement);
        participant.SessionCreditMovementId = movement.Id;
    }

    /// <summary>Releases the share of a consumption-based deferral that these units represent.</summary>
    private async Task RecogniseRemainingAsync(Guid scheduleId, int units, string trigger)
    {
        var schedule = await db.DeferredRevenue.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == scheduleId);
        if (schedule is null || schedule.IsClosed || schedule.TotalUnits == 0) return;

        var perUnit = schedule.TotalAmount / schedule.TotalUnits;
        var amount = Math.Round(Math.Min(perUnit * units, schedule.RemainingAmount), 2);
        if (amount <= 0) return;

        db.DeferredRevenueEntries.Add(new DeferredRevenueEntry
        {
            ScheduleId = scheduleId,
            RecognisedOn = DateTime.UtcNow.Date,
            Amount = amount,
            Trigger = trigger,
            UnitsConsumed = units,
        }.StampNew(tenant));

        schedule.ConsumedUnits += units;
        schedule.RecognisedAmount += amount;
        schedule.RemainingAmount = Math.Max(0, schedule.TotalAmount - schedule.RecognisedAmount);

        if (schedule.RemainingAmount <= 0.005m || schedule.ConsumedUnits >= schedule.TotalUnits)
        {
            schedule.IsClosed = true;
            schedule.ClosedOn = DateTime.UtcNow;
        }
    }
}
