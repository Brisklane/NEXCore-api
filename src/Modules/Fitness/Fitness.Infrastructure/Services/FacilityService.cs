using Fitness.Application.DTOs;
using Fitness.Application.Services.Interfaces;
using Fitness.Domain.Entities;
using Fitness.Domain.Enums;
using Fitness.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;

namespace Fitness.Infrastructure.Services;

/// <summary>
/// Lockers, courts, equipment and the maintenance behind them.
///
/// The connection worth noticing: **a broken machine takes itself out of everything**. A fault
/// reported by scanning the sticker on a bike marks the asset out of service, and the spot map,
/// the court grid and the resource booking engine all read that same flag — so nobody books
/// bike 14 while it is broken, without anyone having to remember to update the timetable.
/// </summary>
public class FacilityService(
    FitnessDbContext db,
    IFitnessTenant tenant,
    FitnessNumbering numbering,
    IBillingService billing) : IFacilityService
{
    // ── Lockers ──────────────────────────────────────────────────────────────

    public async Task<List<LockerBankDto>> GetLockerBanksAsync(Guid clubId)
    {
        var now = DateTime.UtcNow;
        var monthEnd = new DateTime(now.Year, now.Month, 1).AddMonths(1);

        var banks = await db.LockerBanks.ForTenant(tenant)
            .Where(b => b.ClubId == clubId)
            .Include(b => b.Lockers.Where(l => !l.IsDeleted))
            .OrderBy(b => b.Name)
            .ToListAsync();

        var lockerIds = banks.SelectMany(b => b.Lockers).Select(l => l.Id).ToList();

        var assignments = await db.LockerAssignments.ForTenant(tenant)
            .Where(a => lockerIds.Contains(a.LockerId) && a.ReleasedOn == null)
            .Include(a => a.Member)
            .Include(a => a.Locker)
            .ToListAsync();

        return [.. banks.Select(bank =>
        {
            var lockers = bank.Lockers.Where(l => !l.IsDeleted).ToList();
            var bankAssignments = assignments.Where(a => lockers.Any(l => l.Id == a.LockerId)).ToList();

            return new LockerBankDto
            {
                Id = bank.Id,
                ClubId = bank.ClubId,
                Name = bank.Name,
                Location = bank.Location,
                AreaId = bank.AreaId,
                TotalLockers = lockers.Count,
                RentedCount = lockers.Count(l => l.Status == LockerStatus.Rented),
                FreeCount = lockers.Count(l => l.Status == LockerStatus.Free),
                OutOfOrderCount = lockers.Count(l => l.Status == LockerStatus.OutOfOrder),
                OccupancyPercent = FitnessMapper.Percent(lockers.Count(l => l.Status == LockerStatus.Rented), lockers.Count),
                SupportsRental = bank.SupportsRental,
                SupportsDayUse = bank.SupportsDayUse,
                IsActive = bank.IsActive,
                MonthlyRentalRevenue = bankAssignments.Where(a => !a.IsDayUse).Sum(a => a.Rate),
                ExpiringThisMonth = bankAssignments.Count(a => a.EndsOn is not null && a.EndsOn < monthEnd),
                Lockers = [.. lockers
                    .OrderBy(l => l.Number.Length).ThenBy(l => l.Number)
                    .Select(l => FitnessMapper.ToDto(l, assignments.FirstOrDefault(a => a.LockerId == l.Id), now))],
            };
        })];
    }

    public async Task<LockerBankDto> SaveLockerBankAsync(Guid? id, LockerBankDto request, Guid userId)
    {
        LockerBank bank;
        if (id is null)
        {
            bank = new LockerBank { ClubId = request.ClubId }.StampNew(tenant, userId);
            db.LockerBanks.Add(bank);
        }
        else
        {
            bank = await db.LockerBanks.ForTenant(tenant)
                .Include(b => b.Lockers.Where(l => !l.IsDeleted))
                .FirstOrDefaultAsync(b => b.Id == id)
                ?? throw new InvalidOperationException("Locker bank not found.");
            bank.StampUpdated(userId);
        }

        bank.Name = request.Name;
        bank.Location = request.Location;
        bank.AreaId = request.AreaId;
        bank.SupportsRental = request.SupportsRental;
        bank.SupportsDayUse = request.SupportsDayUse;
        bank.IsActive = request.IsActive;

        var existing = bank.Lockers.Where(l => !l.IsDeleted).ToList();
        var keptIds = request.Lockers.Where(l => l.Id != Guid.Empty).Select(l => l.Id).ToHashSet();

        foreach (var gone in existing.Where(l => !keptIds.Contains(l.Id)))
        {
            if (gone.Status == LockerStatus.Rented)
                throw new InvalidOperationException($"Locker {gone.Number} is rented. Release it before removing it.");

            gone.StampDeleted(userId);
        }

        foreach (var lockerDto in request.Lockers)
        {
            var locker = existing.FirstOrDefault(l => l.Id == lockerDto.Id);
            if (locker is null)
            {
                locker = new Locker { LockerBankId = bank.Id, ClubId = bank.ClubId }.StampNew(tenant, userId);
                db.Lockers.Add(locker);
            }
            else
            {
                locker.StampUpdated(userId);
            }

            locker.Number = lockerDto.Number;
            locker.Size = lockerDto.Size;
            locker.LockType = lockerDto.LockType;
            locker.KeyNumber = lockerDto.KeyNumber;
            locker.MonthlyRate = lockerDto.MonthlyRate;
            locker.AnnualRate = lockerDto.AnnualRate;
            locker.Deposit = lockerDto.Deposit;

            // Status is owned by the assignment lifecycle; only out-of-order is set by hand.
            if (lockerDto.Status == LockerStatus.OutOfOrder || locker.Status == LockerStatus.OutOfOrder)
            {
                locker.Status = lockerDto.Status;
                locker.OutOfOrderNote = lockerDto.OutOfOrderNote;
            }
        }

        bank.TotalLockers = request.Lockers.Count;
        await db.SaveChangesAsync();

        return (await GetLockerBanksAsync(bank.ClubId)).First(b => b.Id == bank.Id);
    }

    public async Task<LockerAssignmentDto> AssignLockerAsync(AssignLockerDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var locker = await db.Lockers.ForTenant(tenant)
            .Include(l => l.LockerBank)
            .FirstOrDefaultAsync(l => l.Id == request.LockerId)
            ?? throw new InvalidOperationException("Locker not found.");

        if (locker.Status == LockerStatus.OutOfOrder)
            throw new InvalidOperationException($"Locker {locker.Number} is out of order — {locker.OutOfOrderNote}.");

        var occupied = await db.LockerAssignments.ForTenant(tenant)
            .Include(a => a.Member)
            .FirstOrDefaultAsync(a => a.LockerId == request.LockerId && a.ReleasedOn == null);

        if (occupied is not null)
            throw new InvalidOperationException(
                $"Locker {locker.Number} is already let to " +
                $"{(occupied.Member is null ? "another member" : FitnessMapper.FullName(occupied.Member))}.");

        var rate = request.RateOverride ?? (request.IsDayUse ? 0m : locker.MonthlyRate);

        var assignment = new LockerAssignment
        {
            LockerId = request.LockerId,
            MemberId = request.MemberId,
            ClubId = locker.ClubId,
            StartsOn = request.StartsOn.Date,
            EndsOn = request.IsDayUse ? request.StartsOn.Date : request.EndsOn?.Date,
            IsDayUse = request.IsDayUse,
            Rate = rate,
            DepositHeld = request.Deposit,
            AutoRenews = request.AutoRenews,
            NextBillingOn = request.AutoRenews ? request.EndsOn?.Date : null,
            KeyIssued = request.KeyIssued,
        }.StampNew(tenant, userId);

        db.LockerAssignments.Add(assignment);

        locker.Status = request.IsDayUse ? LockerStatus.DayUse : LockerStatus.Rented;
        locker.CurrentAssignmentId = assignment.Id;
        locker.StampUpdated(userId);

        if (request.ChargeNow && (rate > 0 || request.Deposit > 0))
        {
            var lines = new List<InvoiceLineDto>();

            if (rate > 0)
            {
                lines.Add(new InvoiceLineDto
                {
                    ChargeKind = ChargeKind.LockerRental,
                    LineDescription = $"Locker {locker.Number} — {locker.LockerBank?.Name}",
                    PeriodStart = assignment.StartsOn,
                    PeriodEnd = assignment.EndsOn,
                    Quantity = 1,
                    UnitPrice = rate,
                    LineTotal = rate,
                });
            }

            if (request.Deposit > 0)
            {
                lines.Add(new InvoiceLineDto
                {
                    ChargeKind = ChargeKind.Other,
                    LineDescription = "Locker key deposit (refundable)",
                    Quantity = 1,
                    UnitPrice = request.Deposit,
                    LineTotal = request.Deposit,
                });
            }

            var invoice = await billing.CreateAdHocInvoiceAsync(request.MemberId, locker.ClubId, lines, userId);
            assignment.InvoiceId = invoice.Id;

            if (request.PaymentMethod is not null)
            {
                await billing.TakePaymentAsync(new TakePaymentDto
                {
                    MemberId = request.MemberId,
                    ClubId = locker.ClubId,
                    InvoiceId = invoice.Id,
                    Amount = invoice.Total,
                    Method = request.PaymentMethod.Value,
                    Notes = $"Locker {locker.Number}",
                    IdempotencyKey = $"locker:{assignment.Id}",
                }, userId);
            }
        }

        await db.SaveChangesAsync();

        var saved = await db.LockerAssignments.ForTenant(tenant)
            .Include(a => a.Locker)
            .Include(a => a.Member)
            .FirstAsync(a => a.Id == assignment.Id);

        return FitnessMapper.ToDto(saved, now);
    }

    public async Task<LockerAssignmentDto> ReleaseLockerAsync(ReleaseLockerDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var assignment = await db.LockerAssignments.ForTenant(tenant)
            .Include(a => a.Locker)
            .Include(a => a.Member)
            .FirstOrDefaultAsync(a => a.Id == request.AssignmentId)
            ?? throw new InvalidOperationException("Locker assignment not found.");

        assignment.ReleasedOn = now;
        assignment.KeyReturned = request.KeyReturned;
        assignment.DepositReturned = request.DepositReturned;
        assignment.WasReclaimed = request.WasReclaimed;
        assignment.ReclaimNote = request.Note;
        assignment.StampUpdated(userId);

        if (assignment.Locker is not null)
        {
            assignment.Locker.Status = LockerStatus.Free;
            assignment.Locker.CurrentAssignmentId = null;
            assignment.Locker.StampUpdated(userId);
        }

        // A deposit only comes back if the key did.
        if (request.DepositReturned > 0 && request.KeyReturned)
        {
            await billing.IssueCreditNoteAsync(new IssueCreditNoteDto
            {
                MemberId = assignment.MemberId,
                Amount = request.DepositReturned,
                Reason = $"Locker {assignment.Locker?.Number} deposit returned",
                AppliedToBalance = true,
            }, userId);
        }

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(assignment, now);
    }

    public async Task<PaginatedResponse<LockerAssignmentDto>> GetLockerAssignmentsAsync(
        Guid? clubId, Guid? memberId, bool activeOnly, PaginationParams pagination)
    {
        var now = DateTime.UtcNow;

        var query = db.LockerAssignments.ForTenant(tenant)
            .Include(a => a.Locker).ThenInclude(l => l!.LockerBank)
            .Include(a => a.Member)
            .WhereIf(clubId is not null, a => a.ClubId == clubId)
            .WhereIf(memberId is not null, a => a.MemberId == memberId)
            .WhereIf(activeOnly, a => a.ReleasedOn == null);

        var total = await query.CountAsync();

        var page = await query
            .OrderByDescending(a => a.StartsOn)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        return PaginatedResponse<LockerAssignmentDto>.Ok(
                   [.. page.Select(a =>
            {
                var dto = FitnessMapper.ToDto(a, now);
                dto.BankName = a.Locker?.LockerBank?.Name;
                return dto;
            })],
                   total,
                   pagination.PageNumber,
                   pagination.PageSize);
    }

    /// <summary>
    /// Sweeps day lockers and chases expired rentals. Runs overnight.
    ///
    /// Every club has a row of lockers whose rental lapsed months ago and which nobody chased.
    /// This is the job that stops that happening: notice first, then reclaim.
    /// </summary>
    public async Task<int> SweepLockersAsync()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var actions = 0;

        // Day lockers are released at close.
        var dayUse = await db.LockerAssignments.ForTenant(tenant)
            .Include(a => a.Locker)
            .Where(a => a.IsDayUse && a.ReleasedOn == null && a.StartsOn < today)
            .ToListAsync();

        foreach (var assignment in dayUse)
        {
            assignment.ReleasedOn = now;
            assignment.ReclaimNote = "Day locker swept overnight";

            if (assignment.Locker is not null)
            {
                assignment.Locker.Status = LockerStatus.Free;
                assignment.Locker.CurrentAssignmentId = null;
            }

            actions++;
        }

        // Rentals ending within a fortnight get a notice, once.
        var expiring = await db.LockerAssignments.ForTenant(tenant)
            .Include(a => a.Locker)
            .Include(a => a.Member)
            .Where(a => !a.IsDayUse && a.ReleasedOn == null && !a.ExpiryNoticeSent
                     && a.EndsOn != null && a.EndsOn <= today.AddDays(14))
            .ToListAsync();

        foreach (var assignment in expiring)
        {
            db.MessageLog.Add(new MessageLog
            {
                MemberId = assignment.MemberId,
                ClubId = assignment.ClubId,
                Channel = MessageChannel.Email,
                Status = MessageStatus.Queued,
                Recipient = assignment.Member?.Email,
                Subject = "Your locker rental is ending",
                BodyPreview = $"Locker {assignment.Locker?.Number} is booked until {assignment.EndsOn:d MMMM}. " +
                              "Let us know if you would like to keep it.",
                QueuedAt = now,
            }.StampNew(tenant));

            assignment.ExpiryNoticeSent = true;
            assignment.ExpiryNoticeSentOn = now;
            actions++;
        }

        // Auto-renewing rentals bill again rather than lapsing.
        var renewing = await db.LockerAssignments.ForTenant(tenant)
            .Include(a => a.Locker)
            .Where(a => a.AutoRenews && a.ReleasedOn == null
                     && a.NextBillingOn != null && a.NextBillingOn <= today)
            .ToListAsync();

        foreach (var assignment in renewing)
        {
            await billing.CreateAdHocInvoiceAsync(assignment.MemberId, assignment.ClubId, [
                new InvoiceLineDto
                {
                    ChargeKind = ChargeKind.LockerRental,
                    LineDescription = $"Locker {assignment.Locker?.Number} — renewal",
                    PeriodStart = today,
                    PeriodEnd = today.AddMonths(1).AddDays(-1),
                    Quantity = 1,
                    UnitPrice = assignment.Rate,
                    LineTotal = assignment.Rate,
                },
            ], Guid.Empty);

            assignment.EndsOn = today.AddMonths(1);
            assignment.NextBillingOn = today.AddMonths(1);
            assignment.ExpiryNoticeSent = false;
            actions++;
        }

        // A month past its end date with no response, the locker comes back.
        var abandoned = await db.LockerAssignments.ForTenant(tenant)
            .Include(a => a.Locker)
            .Where(a => !a.IsDayUse && !a.AutoRenews && a.ReleasedOn == null && a.ExpiryNoticeSent
                     && a.EndsOn != null && a.EndsOn < today.AddDays(-30))
            .ToListAsync();

        foreach (var assignment in abandoned)
        {
            assignment.ReleasedOn = now;
            assignment.WasReclaimed = true;
            assignment.ReclaimNote = $"Reclaimed — rental ended {assignment.EndsOn:d MMM} and was not renewed";

            if (assignment.Locker is not null)
            {
                assignment.Locker.Status = LockerStatus.Free;
                assignment.Locker.CurrentAssignmentId = null;
            }

            actions++;
        }

        await db.SaveChangesAsync();
        return actions;
    }

    // ── Resources ────────────────────────────────────────────────────────────

    public async Task<List<BookableResourceDto>> GetResourcesAsync(Guid clubId, ResourceKind? kind, bool activeOnly)
    {
        var now = DateTime.UtcNow;
        var weekStart = now.Date.AddDays(-(int)now.DayOfWeek);
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var resources = await db.Resources.ForTenant(tenant)
            .Where(r => r.ClubId == clubId)
            .WhereIf(kind is not null, r => r.Kind == kind)
            .WhereIf(activeOnly, r => r.IsActive)
            .Include(r => r.SlotRules.Where(s => !s.IsDeleted))
            .OrderBy(r => r.Kind).ThenBy(r => r.DisplayOrder).ThenBy(r => r.Name)
            .ToListAsync();

        var ids = resources.Select(r => r.Id).ToList();

        var bookings = await db.ResourceBookings.ForTenant(tenant)
            .Where(b => ids.Contains(b.BookableResourceId)
                     && b.Status != ResourceBookingStatus.Cancelled
                     && b.StartsAt >= monthStart)
            .Select(b => new { b.BookableResourceId, b.StartsAt, b.Amount })
            .ToListAsync();

        var areaNames = await db.Areas.ForTenant(tenant)
            .Where(a => a.ClubId == clubId)
            .Select(a => new { a.Id, a.Name })
            .ToDictionaryAsync(a => a.Id, a => a.Name);

        return [.. resources.Select(r =>
        {
            var dto = FitnessMapper.ToDto(r);
            if (r.AreaId is not null) dto.AreaName = areaNames.GetValueOrDefault(r.AreaId.Value);

            var mine = bookings.Where(b => b.BookableResourceId == r.Id).ToList();
            dto.BookingsThisWeek = mine.Count(b => b.StartsAt >= weekStart);
            dto.RevenueThisMonth = mine.Sum(b => b.Amount);

            // Bookable slots in a week, from the rules, so utilisation means something.
            var weeklySlots = r.SlotRules
                .Where(s => !s.IsDeleted && !s.IsBlocked)
                .Sum(s => CountDays(s.DaysOfWeekMask) * (int)((s.EndsAt - s.StartsAt).TotalMinutes / Math.Max(1, r.SlotMinutes)));

            dto.UtilisationPercent = FitnessMapper.Percent(dto.BookingsThisWeek, weeklySlots);

            return dto;
        })];
    }

    public async Task<BookableResourceDto> SaveResourceAsync(Guid? id, BookableResourceDto request, Guid userId)
    {
        BookableResource resource;
        if (id is null)
        {
            resource = new BookableResource { ClubId = request.ClubId }.StampNew(tenant, userId);
            db.Resources.Add(resource);
        }
        else
        {
            resource = await db.Resources.ForTenant(tenant)
                .Include(r => r.SlotRules.Where(s => !s.IsDeleted))
                .FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new InvalidOperationException("Resource not found.");
            resource.StampUpdated(userId);
        }

        resource.AreaId = request.AreaId;
        resource.Name = request.Name;
        resource.Kind = request.Kind;
        resource.Capacity = Math.Max(1, request.Capacity);
        resource.DisplayOrder = request.DisplayOrder;
        resource.ColourHex = request.ColourHex;
        resource.SlotMinutes = Math.Max(5, request.SlotMinutes);
        resource.BufferMinutes = request.BufferMinutes;
        resource.MemberRate = request.MemberRate;
        resource.NonMemberRate = request.NonMemberRate;
        resource.PeakSurcharge = request.PeakSurcharge;
        resource.BookingWindowDays = request.BookingWindowDays;
        resource.MaxConcurrentBookingsPerMember = request.MaxConcurrentBookingsPerMember;
        resource.FreeCancelHours = request.FreeCancelHours;
        resource.LateCancelOutcome = request.LateCancelOutcome;
        resource.NoShowOutcome = request.NoShowOutcome;
        resource.LinkedDoorId = request.LinkedDoorId;
        resource.EquipmentAssetId = request.EquipmentAssetId;
        resource.BookableOnline = request.BookableOnline;
        resource.IsOutOfService = request.IsOutOfService;
        resource.OutOfServiceNote = request.OutOfServiceNote;
        resource.IsActive = request.IsActive;

        foreach (var existing in resource.SlotRules.Where(s => !s.IsDeleted)) existing.StampDeleted(userId);

        foreach (var ruleDto in request.SlotRules)
        {
            db.ResourceSlotRules.Add(new ResourceSlotRule
            {
                BookableResourceId = resource.Id,
                DaysOfWeekMask = ruleDto.DaysOfWeekMask,
                StartsAt = ruleDto.StartsAt,
                EndsAt = ruleDto.EndsAt,
                IsPeak = ruleDto.IsPeak,
                RateOverride = ruleDto.RateOverride,
                IsBlocked = ruleDto.IsBlocked,
                BlockReason = ruleDto.BlockReason,
                EffectiveFrom = ruleDto.EffectiveFrom,
                EffectiveTo = ruleDto.EffectiveTo,
            }.StampNew(tenant, userId));
        }

        await db.SaveChangesAsync();
        return (await GetResourcesAsync(resource.ClubId, null, false)).First(r => r.Id == resource.Id);
    }

    /// <summary>
    /// The court grid: one row per resource, one column per slot, for one day.
    ///
    /// Exactly the interaction a leisure-centre desk expects, and nothing like a list of bookings.
    /// Every cell already knows its state, its price and whether it is peak, so the screen does no
    /// arithmetic of its own.
    /// </summary>
    public async Task<ResourceGridDto> GetGridAsync(Guid clubId, DateTime forDate, ResourceKind? kind)
    {
        var now = DateTime.UtcNow;
        var day = forDate.Date;

        var club = await db.Clubs.ForTenant(tenant)
            .Include(c => c.Schedules.Where(s => !s.IsDeleted))
            .FirstOrDefaultAsync(c => c.Id == clubId)
            ?? throw new InvalidOperationException("Club not found.");

        var schedule = club.Schedules.FirstOrDefault(s => s.OverrideDate?.Date == day)
                       ?? club.Schedules.FirstOrDefault(s => s.OverrideDate is null && s.DayOfWeek == (int)day.DayOfWeek);

        var opensAt = schedule?.OpensAt ?? new TimeSpan(6, 0, 0);
        var closesAt = schedule?.ClosesAt ?? new TimeSpan(22, 0, 0);

        var resources = await db.Resources.ForTenant(tenant)
            .Where(r => r.ClubId == clubId && r.IsActive)
            .WhereIf(kind is not null, r => r.Kind == kind)
            .Include(r => r.SlotRules.Where(s => !s.IsDeleted))
            .OrderBy(r => r.DisplayOrder).ThenBy(r => r.Name)
            .ToListAsync();

        var bookings = await db.ResourceBookings.ForTenant(tenant)
            .Where(b => b.ClubId == clubId
                     && b.StartsAt >= day && b.StartsAt < day.AddDays(1)
                     && b.Status != ResourceBookingStatus.Cancelled)
            .Include(b => b.Member)
            .ToListAsync();

        var grid = new ResourceGridDto
        {
            ClubId = clubId,
            ClubName = club.Name,
            ForDate = day,
            FilterKind = kind,
            OpensAt = opensAt,
            ClosesAt = closesAt,
            SlotMinutes = resources.Count == 0 ? 60 : resources.Min(r => r.SlotMinutes),
            RevenueToday = bookings.Sum(b => b.Amount),
        };

        foreach (var resource in resources)
        {
            var row = new ResourceGridRowDto
            {
                ResourceId = resource.Id,
                ResourceName = resource.Name,
                Kind = resource.Kind,
                ColourHex = resource.ColourHex,
                IsOutOfService = resource.IsOutOfService,
            };

            for (var t = opensAt; t + TimeSpan.FromMinutes(resource.SlotMinutes) <= closesAt;
                 t += TimeSpan.FromMinutes(resource.SlotMinutes))
            {
                var start = day.Add(t);
                var end = start.AddMinutes(resource.SlotMinutes);

                var rule = resource.SlotRules
                    .Where(s => !s.IsDeleted
                             && FitnessQueryExtensions.CoversDay(s.DaysOfWeekMask, day.DayOfWeek)
                             && s.StartsAt <= t && s.EndsAt >= t + TimeSpan.FromMinutes(resource.SlotMinutes))
                    .Where(s => (s.EffectiveFrom is null || s.EffectiveFrom <= day)
                             && (s.EffectiveTo is null || s.EffectiveTo >= day))
                    .OrderByDescending(s => s.IsBlocked)
                    .FirstOrDefault();

                var booking = bookings.FirstOrDefault(b =>
                    b.BookableResourceId == resource.Id && b.StartsAt < end && b.EndsAt > start);

                var rate = rule?.RateOverride
                           ?? resource.MemberRate + (rule?.IsPeak == true ? resource.PeakSurcharge : 0m);

                var state = resource.IsOutOfService ? "Blocked"
                    : booking is not null ? "Booked"
                    : rule?.IsBlocked == true ? "Blocked"
                    : rule is null ? "Blocked"
                    : end <= now ? "Past"
                    : rule.IsPeak ? "Peak"
                    : "Free";

                row.Slots.Add(new ResourceGridSlotDto
                {
                    StartsAt = start,
                    EndsAt = end,
                    State = state,
                    BookingId = booking?.Id,
                    BookedByName = booking is null
                        ? null
                        : booking.Member is null ? booking.GuestName : FitnessMapper.FullName(booking.Member),
                    IsPeak = rule?.IsPeak ?? false,
                    Rate = rate,
                    BlockReason = resource.IsOutOfService ? resource.OutOfServiceNote : rule?.BlockReason,
                });

                grid.TotalSlots++;
                if (booking is not null) grid.BookedSlots++;
            }

            grid.Rows.Add(row);
        }

        grid.UtilisationPercent = FitnessMapper.Percent(grid.BookedSlots, grid.TotalSlots);
        return grid;
    }

    public async Task<ResourceBookingDto> BookResourceAsync(CreateResourceBookingDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var resource = await db.Resources.ForTenant(tenant)
            .Include(r => r.SlotRules.Where(s => !s.IsDeleted))
            .FirstOrDefaultAsync(r => r.Id == request.BookableResourceId)
            ?? throw new InvalidOperationException("Resource not found.");

        if (resource.IsOutOfService)
            throw new InvalidOperationException($"{resource.Name} is out of service — {resource.OutOfServiceNote}.");

        var duration = request.DurationMinutes ?? resource.SlotMinutes;
        var starts = request.StartsAt;
        var ends = starts.AddMinutes(duration);

        if (starts < now) throw new InvalidOperationException("That slot is in the past.");

        if (starts > now.AddDays(resource.BookingWindowDays))
            throw new InvalidOperationException($"{resource.Name} can be booked up to {resource.BookingWindowDays} days ahead.");

        if (!request.OverrideConflicts)
        {
            var clash = await db.ResourceBookings.ForTenant(tenant)
                .AnyAsync(b => b.BookableResourceId == resource.Id
                            && b.Status != ResourceBookingStatus.Cancelled
                            && b.StartsAt < ends && b.EndsAt > starts);

            if (clash) throw new InvalidOperationException("That slot has just been taken.");
        }

        // The slot rules decide whether it is bookable at all, and at what price.
        var rule = resource.SlotRules
            .Where(s => !s.IsDeleted
                     && FitnessQueryExtensions.CoversDay(s.DaysOfWeekMask, starts.DayOfWeek)
                     && s.StartsAt <= starts.TimeOfDay && s.EndsAt >= ends.TimeOfDay)
            .OrderByDescending(s => s.IsBlocked)
            .FirstOrDefault();

        if (rule is null && !request.OverrideConflicts)
            throw new InvalidOperationException($"{resource.Name} is not bookable at that time.");

        if (rule?.IsBlocked == true && !request.OverrideConflicts)
            throw new InvalidOperationException($"That slot is blocked — {rule.BlockReason}.");

        if (request.MemberId is not null && resource.MaxConcurrentBookingsPerMember > 0)
        {
            var held = await db.ResourceBookings.ForTenant(tenant)
                .CountAsync(b => b.MemberId == request.MemberId
                              && b.BookableResourceId == resource.Id
                              && b.Status == ResourceBookingStatus.Booked
                              && b.StartsAt > now);

            if (held >= resource.MaxConcurrentBookingsPerMember)
                throw new InvalidOperationException(
                    $"You already hold {held} bookings for {resource.Name}. Cancel one to book another.");
        }

        var baseRate = request.MemberId is not null ? resource.MemberRate : resource.NonMemberRate;
        var amount = request.PriceOverride
                     ?? rule?.RateOverride
                     ?? baseRate + (rule?.IsPeak == true ? resource.PeakSurcharge : 0m);

        var booking = new ResourceBooking
        {
            BookingNumber = await numbering.NextResourceBookingNumberAsync(now),
            BookableResourceId = resource.Id,
            ClubId = resource.ClubId,
            MemberId = request.MemberId,
            GuestName = request.GuestName,
            GuestPhone = request.GuestPhone,
            StartsAt = starts,
            EndsAt = ends,
            Status = ResourceBookingStatus.Booked,
            Channel = request.Channel,
            ParticipantCount = request.ParticipantCount,
            ParticipantMemberIds = request.ParticipantMemberIds.Count == 0
                ? null
                : string.Join(',', request.ParticipantMemberIds),
            Amount = amount,
            BookedByStaffId = userId == Guid.Empty ? null : userId,
            Note = request.Note,
        }.StampNew(tenant, userId);

        db.ResourceBookings.Add(booking);

        if (amount > 0 && request.MemberId is not null)
        {
            var invoice = await billing.CreateAdHocInvoiceAsync(request.MemberId.Value, resource.ClubId, [
                new InvoiceLineDto
                {
                    ChargeKind = ChargeKind.ResourceBooking,
                    LineDescription = $"{resource.Name} — {starts:ddd d MMM HH:mm}",
                    Quantity = 1,
                    UnitPrice = amount,
                    LineTotal = amount,
                },
            ], userId);

            booking.InvoiceId = invoice.Id;

            if (request.PaymentMethod is not null)
            {
                var payment = await billing.TakePaymentAsync(new TakePaymentDto
                {
                    MemberId = request.MemberId.Value,
                    ClubId = resource.ClubId,
                    InvoiceId = invoice.Id,
                    Amount = amount,
                    Method = request.PaymentMethod.Value,
                    CashSessionId = request.CashSessionId,
                    IdempotencyKey = $"resource:{booking.Id}",
                }, userId);

                booking.PaymentId = payment.PaymentId;
            }
        }

        await db.SaveChangesAsync();

        var saved = await db.ResourceBookings.ForTenant(tenant)
            .Include(b => b.BookableResource)
            .Include(b => b.Member)
            .FirstAsync(b => b.Id == booking.Id);

        return FitnessMapper.ToDto(saved);
    }

    public async Task<ResourceBookingDto> CancelResourceBookingAsync(
        Guid bookingId, string? reason, bool waivePenalty, Guid userId)
    {
        var now = DateTime.UtcNow;

        var booking = await db.ResourceBookings.ForTenant(tenant)
            .Include(b => b.BookableResource)
            .Include(b => b.Member)
            .FirstOrDefaultAsync(b => b.Id == bookingId)
            ?? throw new InvalidOperationException("Booking not found.");

        var resource = booking.BookableResource;
        var hoursUntil = (booking.StartsAt - now).TotalHours;
        var isLate = resource is not null && hoursUntil < resource.FreeCancelHours;

        booking.Status = isLate && !waivePenalty
            ? ResourceBookingStatus.LateCancelled
            : ResourceBookingStatus.Cancelled;
        booking.CancelledAt = now;
        booking.CancellationReason = reason;
        booking.StampUpdated(userId);

        if (isLate && !waivePenalty && resource is not null && booking.MemberId is not null
            && resource.LateCancelOutcome is PolicyOutcome.ChargeFee or PolicyOutcome.ForfeitCreditAndFee)
        {
            booking.PenaltyCharged = booking.Amount;

            await billing.CreateAdHocInvoiceAsync(booking.MemberId.Value, booking.ClubId, [
                new InvoiceLineDto
                {
                    ChargeKind = ChargeKind.LateCancelFee,
                    LineDescription = $"Late cancellation — {resource.Name}, {booking.StartsAt:d MMM HH:mm}",
                    Quantity = 1,
                    UnitPrice = booking.Amount,
                    LineTotal = booking.Amount,
                },
            ], userId);
        }
        else if (booking.InvoiceId is not null && booking.MemberId is not null && booking.Amount > 0)
        {
            // Cancelled in time — the charge comes back.
            await billing.IssueCreditNoteAsync(new IssueCreditNoteDto
            {
                MemberId = booking.MemberId.Value,
                InvoiceId = booking.InvoiceId,
                Amount = booking.Amount,
                Reason = "Booking cancelled in time",
                AppliedToBalance = true,
            }, userId);
        }

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(booking);
    }

    public async Task<ResourceBookingDto> CheckInResourceBookingAsync(Guid bookingId, Guid userId)
    {
        var booking = await db.ResourceBookings.ForTenant(tenant)
            .Include(b => b.BookableResource)
            .Include(b => b.Member)
            .FirstOrDefaultAsync(b => b.Id == bookingId)
            ?? throw new InvalidOperationException("Booking not found.");

        booking.Status = ResourceBookingStatus.CheckedIn;
        booking.CheckedInAt = DateTime.UtcNow;
        booking.StampUpdated(userId);

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(booking);
    }

    public async Task<PaginatedResponse<ResourceBookingDto>> ListResourceBookingsAsync(
        Guid? clubId, Guid? memberId, Guid? resourceId, DateTime? from, DateTime? to, PaginationParams pagination)
    {
        var query = db.ResourceBookings.ForTenant(tenant)
            .Include(b => b.BookableResource)
            .Include(b => b.Member)
            .WhereIf(clubId is not null, b => b.ClubId == clubId)
            .WhereIf(memberId is not null, b => b.MemberId == memberId)
            .WhereIf(resourceId is not null, b => b.BookableResourceId == resourceId)
            .WhereIf(from is not null, b => b.StartsAt >= from)
            .WhereIf(to is not null, b => b.StartsAt <= to);

        var total = await query.CountAsync();

        var page = await query
            .OrderByDescending(b => b.StartsAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        return PaginatedResponse<ResourceBookingDto>.Ok(
                   [.. page.Select(FitnessMapper.ToDto)],
                   total,
                   pagination.PageNumber,
                   pagination.PageSize);
    }

    // ── Equipment ────────────────────────────────────────────────────────────

    public async Task<PaginatedResponse<EquipmentAssetDto>> GetEquipmentAsync(
        Guid? clubId, AssetStatus? status, string? category, string? search, PaginationParams pagination)
    {
        var now = DateTime.UtcNow;

        var query = db.Equipment.ForTenant(tenant)
            .WhereIf(clubId is not null, e => e.ClubId == clubId)
            .WhereIf(status is not null, e => e.Status == status)
            .WhereIf(!string.IsNullOrWhiteSpace(category), e => e.Category == category);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(e => e.Name.ToLower().Contains(term)
                                  || (e.SerialNumber != null && e.SerialNumber.ToLower().Contains(term))
                                  || (e.AssetTag != null && e.AssetTag.ToLower().Contains(term))
                                  || (e.Model != null && e.Model.ToLower().Contains(term)));
        }

        var total = await query.CountAsync();

        var page = await query
            .OrderBy(e => e.Category).ThenBy(e => e.Name)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        var ids = page.Select(e => e.Id).ToList();

        var workOrders = await db.WorkOrders.ForTenant(tenant)
            .Where(w => w.EquipmentAssetId != null && ids.Contains(w.EquipmentAssetId.Value)
                     && w.Status != WorkOrderStatus.Completed && w.Status != WorkOrderStatus.Cancelled)
            .GroupBy(w => w.EquipmentAssetId!.Value)
            .Select(g => new { AssetId = g.Key, Count = g.Count() })
            .ToListAsync();

        var faults = await db.FaultReports.ForTenant(tenant)
            .Where(f => f.EquipmentAssetId != null && ids.Contains(f.EquipmentAssetId.Value)
                     && f.ReportedAt >= now.AddDays(-90))
            .GroupBy(f => f.EquipmentAssetId!.Value)
            .Select(g => new { AssetId = g.Key, Count = g.Count() })
            .ToListAsync();

        var clubNames = await db.Clubs.ForTenant(tenant)
            .Select(c => new { c.Id, c.Name }).ToDictionaryAsync(c => c.Id, c => c.Name);

        var areaNames = await db.Areas.ForTenant(tenant)
            .Select(a => new { a.Id, a.Name }).ToDictionaryAsync(a => a.Id, a => a.Name);

        var items = page.Select(e =>
        {
            var dto = FitnessMapper.ToDto(e, now);
            dto.ClubName = clubNames.GetValueOrDefault(e.ClubId);
            if (e.AreaId is not null) dto.AreaName = areaNames.GetValueOrDefault(e.AreaId.Value);
            dto.OpenWorkOrders = workOrders.FirstOrDefault(w => w.AssetId == e.Id)?.Count ?? 0;
            dto.FaultsLast90Days = faults.FirstOrDefault(f => f.AssetId == e.Id)?.Count ?? 0;
            return dto;
        }).ToList();

        return PaginatedResponse<EquipmentAssetDto>.Ok(
                   items,
                   total,
                   pagination.PageNumber,
                   pagination.PageSize);
    }

    public async Task<EquipmentAssetDto?> GetAssetAsync(Guid assetId)
    {
        var now = DateTime.UtcNow;

        var asset = await db.Equipment.ForTenant(tenant).FirstOrDefaultAsync(e => e.Id == assetId);
        if (asset is null) return null;

        var dto = FitnessMapper.ToDto(asset, now);

        dto.ClubName = await db.Clubs.ForTenant(tenant)
            .Where(c => c.Id == asset.ClubId).Select(c => c.Name).FirstOrDefaultAsync();

        dto.OpenWorkOrders = await db.WorkOrders.ForTenant(tenant)
            .CountAsync(w => w.EquipmentAssetId == assetId
                          && w.Status != WorkOrderStatus.Completed && w.Status != WorkOrderStatus.Cancelled);

        return dto;
    }

    /// <summary>Resolves the QR sticker on the machine — how a fault actually gets reported.</summary>
    public async Task<EquipmentAssetDto?> GetAssetByQrAsync(string qrCode)
    {
        var asset = await db.Equipment.ForTenant(tenant).FirstOrDefaultAsync(e => e.QrCode == qrCode);
        return asset is null ? null : FitnessMapper.ToDto(asset, DateTime.UtcNow);
    }

    public async Task<EquipmentAssetDto> SaveAssetAsync(Guid? id, SaveEquipmentDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        EquipmentAsset asset;
        if (id is null)
        {
            asset = new EquipmentAsset { ClubId = request.ClubId }.StampNew(tenant, userId);

            // Every asset gets a sticker code on creation — a machine with no code cannot be
            // reported by scanning it, which is the only reporting route staff reliably use.
            asset.QrCode = $"FIT-{Guid.NewGuid().ToString("N")[..10].ToUpperInvariant()}";
            db.Equipment.Add(asset);
        }
        else
        {
            asset = await db.Equipment.ForTenant(tenant).FirstOrDefaultAsync(e => e.Id == id)
                ?? throw new InvalidOperationException("Equipment not found.");
            asset.StampUpdated(userId);
        }

        asset.Code = request.Code;
        asset.AreaId = request.AreaId;
        asset.Name = request.Name;
        asset.Category = request.Category;
        asset.Manufacturer = request.Manufacturer;
        asset.Model = request.Model;
        asset.SerialNumber = request.SerialNumber;
        asset.AssetTag = request.AssetTag;
        asset.PurchasedOn = request.PurchasedOn;
        asset.PurchaseCost = request.PurchaseCost;
        asset.SupplierId = request.SupplierId;
        asset.WarrantyEndsOn = request.WarrantyEndsOn;
        asset.ServiceContractReference = request.ServiceContractReference;
        asset.ServiceContractEndsOn = request.ServiceContractEndsOn;
        asset.InstalledOn = request.InstalledOn;
        asset.UsageHours = request.UsageHours;
        asset.Description = request.Description;
        asset.IsActive = request.IsActive;

        if (asset.Status != request.Status) await ApplyStatusAsync(asset, request.Status, null, userId);

        await db.SaveChangesAsync();
        return (await GetAssetAsync(asset.Id))!;
    }

    public async Task<EquipmentAssetDto> SetAssetStatusAsync(Guid assetId, AssetStatus status, string? note, Guid userId)
    {
        var asset = await db.Equipment.ForTenant(tenant).FirstOrDefaultAsync(e => e.Id == assetId)
            ?? throw new InvalidOperationException("Equipment not found.");

        await ApplyStatusAsync(asset, status, note, userId);
        await db.SaveChangesAsync();

        return (await GetAssetAsync(assetId))!;
    }

    public async Task<EquipmentUsageLogDto> RecordUsageAsync(
        Guid assetId, decimal cumulativeHours, string? source, Guid userId)
    {
        var now = DateTime.UtcNow;

        var asset = await db.Equipment.ForTenant(tenant).FirstOrDefaultAsync(e => e.Id == assetId)
            ?? throw new InvalidOperationException("Equipment not found.");

        var delta = Math.Max(0, cumulativeHours - asset.UsageHours);

        var log = new EquipmentUsageLog
        {
            EquipmentAssetId = assetId,
            ReadOn = now,
            CumulativeHours = cumulativeHours,
            HoursSinceLastRead = delta,
            Source = source,
        }.StampNew(tenant, userId);

        db.EquipmentUsage.Add(log);

        asset.UsageHours = cumulativeHours;
        asset.UsageReadOn = now;
        asset.StampUpdated(userId);

        // Usage-triggered maintenance falls due here rather than on a calendar.
        var schedules = await db.MaintenanceSchedules.ForTenant(tenant)
            .Where(m => m.EquipmentAssetId == assetId && m.IsActive
                     && m.Trigger == MaintenanceTrigger.UsageHours && m.IntervalUsageHours > 0)
            .ToListAsync();

        foreach (var schedule in schedules)
        {
            var lastServiceHours = schedule.LastPerformedOn is null ? 0 : schedule.IntervalUsageHours;
            if (cumulativeHours - lastServiceHours >= schedule.IntervalUsageHours && schedule.NextDueOn is null)
                schedule.NextDueOn = now.Date;
        }

        await db.SaveChangesAsync();

        return new EquipmentUsageLogDto
        {
            Id = log.Id,
            EquipmentAssetId = assetId,
            EquipmentName = asset.Name,
            ReadOn = log.ReadOn,
            CumulativeHours = log.CumulativeHours,
            HoursSinceLastRead = log.HoursSinceLastRead,
            Source = log.Source,
        };
    }

    // ── Maintenance ──────────────────────────────────────────────────────────

    public async Task<List<MaintenanceScheduleDto>> GetMaintenanceSchedulesAsync(Guid? clubId, bool dueOnly)
    {
        var now = DateTime.UtcNow;

        var schedules = await db.MaintenanceSchedules.ForTenant(tenant)
            .WhereIf(clubId is not null, m => m.ClubId == clubId)
            .WhereIf(dueOnly, m => m.IsActive && m.NextDueOn != null && m.NextDueOn <= now.Date.AddDays(7))
            .Include(m => m.EquipmentAsset)
            .OrderBy(m => m.NextDueOn ?? DateTime.MaxValue)
            .ToListAsync();

        var staffNames = await db.Staff.ForTenant(tenant)
            .Select(s => new { s.Id, Name = s.FirstName + " " + s.LastName })
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        return [.. schedules.Select(m =>
        {
            var dto = FitnessMapper.ToDto(m, now);
            if (m.DefaultAssigneeStaffId is not null)
                dto.DefaultAssigneeName = staffNames.GetValueOrDefault(m.DefaultAssigneeStaffId.Value);
            return dto;
        })];
    }

    public async Task<MaintenanceScheduleDto> SaveMaintenanceScheduleAsync(
        Guid? id, MaintenanceScheduleDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        MaintenanceSchedule schedule;
        if (id is null)
        {
            schedule = new MaintenanceSchedule { ClubId = request.ClubId }.StampNew(tenant, userId);
            db.MaintenanceSchedules.Add(schedule);
        }
        else
        {
            schedule = await db.MaintenanceSchedules.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == id)
                ?? throw new InvalidOperationException("Maintenance schedule not found.");
            schedule.StampUpdated(userId);
        }

        schedule.EquipmentAssetId = request.EquipmentAssetId;
        schedule.AppliesToCategory = request.AppliesToCategory;
        schedule.TaskName = request.TaskName;
        schedule.Instructions = request.Instructions;
        schedule.Trigger = request.Trigger;
        schedule.IntervalDays = request.IntervalDays;
        schedule.IntervalUsageHours = request.IntervalUsageHours;
        schedule.EstimatedMinutes = request.EstimatedMinutes;
        schedule.DefaultAssigneeStaffId = request.DefaultAssigneeStaffId;
        schedule.AutoCreateWorkOrder = request.AutoCreateWorkOrder;
        schedule.IsActive = request.IsActive;

        if (schedule.NextDueOn is null && schedule.Trigger == MaintenanceTrigger.Interval && schedule.IntervalDays > 0)
            schedule.NextDueOn = (schedule.LastPerformedOn ?? now.Date).AddDays(schedule.IntervalDays);

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(schedule, now);
    }

    /// <summary>Raises work orders for maintenance that has fallen due. Runs nightly.</summary>
    public async Task<int> GenerateDueWorkOrdersAsync()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var created = 0;

        var due = await db.MaintenanceSchedules.ForTenant(tenant)
            .Where(m => m.IsActive && m.AutoCreateWorkOrder && m.NextDueOn != null && m.NextDueOn <= today)
            .Include(m => m.EquipmentAsset)
            .ToListAsync();

        foreach (var schedule in due)
        {
            // One open work order per schedule — a job raised nightly for a week is seven jobs
            // nobody does.
            var alreadyOpen = await db.WorkOrders.ForTenant(tenant)
                .AnyAsync(w => w.MaintenanceScheduleId == schedule.Id
                            && w.Status != WorkOrderStatus.Completed && w.Status != WorkOrderStatus.Cancelled);

            if (alreadyOpen) continue;

            // A category-wide schedule raises one order per asset in the category.
            var assets = schedule.EquipmentAssetId is not null
                ? [await db.Equipment.ForTenant(tenant).FirstAsync(e => e.Id == schedule.EquipmentAssetId)]
                : await db.Equipment.ForTenant(tenant)
                    .Where(e => e.ClubId == schedule.ClubId && e.Category == schedule.AppliesToCategory
                             && e.Status == AssetStatus.InService)
                    .ToListAsync();

            foreach (var asset in assets)
            {
                db.WorkOrders.Add(new WorkOrder
                {
                    WorkOrderNumber = await numbering.NextWorkOrderNumberAsync(now),
                    ClubId = schedule.ClubId,
                    EquipmentAssetId = asset.Id,
                    MaintenanceScheduleId = schedule.Id,
                    Title = $"{schedule.TaskName} — {asset.Name}",
                    Detail = schedule.Instructions,
                    Status = schedule.DefaultAssigneeStaffId is null ? WorkOrderStatus.Open : WorkOrderStatus.Assigned,
                    Priority = WorkOrderPriority.Normal,
                    RaisedOn = now,
                    DueOn = today.AddDays(7),
                    AssignedStaffId = schedule.DefaultAssigneeStaffId,
                }.StampNew(tenant));

                created++;
            }

            schedule.NextDueOn = schedule.Trigger == MaintenanceTrigger.Interval && schedule.IntervalDays > 0
                ? today.AddDays(schedule.IntervalDays)
                : null;
        }

        await db.SaveChangesAsync();
        return created;
    }

    public async Task<PaginatedResponse<WorkOrderDto>> GetWorkOrdersAsync(
        Guid? clubId, WorkOrderStatus? status, WorkOrderPriority? priority, Guid? assignedStaffId,
        PaginationParams pagination)
    {
        var now = DateTime.UtcNow;

        var query = db.WorkOrders.ForTenant(tenant)
            .Include(w => w.EquipmentAsset)
            .WhereIf(clubId is not null, w => w.ClubId == clubId)
            .WhereIf(status is not null, w => w.Status == status)
            .WhereIf(priority is not null, w => w.Priority == priority)
            .WhereIf(assignedStaffId is not null, w => w.AssignedStaffId == assignedStaffId);

        var total = await query.CountAsync();

        var page = await query
            .OrderByDescending(w => w.Priority).ThenBy(w => w.DueOn ?? DateTime.MaxValue)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        var staffNames = await db.Staff.ForTenant(tenant)
            .Select(s => new { s.Id, Name = s.FirstName + " " + s.LastName })
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        var clubNames = await db.Clubs.ForTenant(tenant)
            .Select(c => new { c.Id, c.Name }).ToDictionaryAsync(c => c.Id, c => c.Name);

        var items = page.Select(w =>
        {
            var dto = FitnessMapper.ToDto(w, now);
            dto.ClubName = clubNames.GetValueOrDefault(w.ClubId);
            if (w.AssignedStaffId is not null) dto.AssignedStaffName = staffNames.GetValueOrDefault(w.AssignedStaffId.Value);
            return dto;
        }).ToList();

        return PaginatedResponse<WorkOrderDto>.Ok(
                   items,
                   total,
                   pagination.PageNumber,
                   pagination.PageSize);
    }

    public async Task<WorkOrderDto> SaveWorkOrderAsync(Guid? id, SaveWorkOrderDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        WorkOrder order;
        if (id is null)
        {
            order = new WorkOrder
            {
                WorkOrderNumber = await numbering.NextWorkOrderNumberAsync(now),
                ClubId = request.ClubId,
                RaisedOn = now,
            }.StampNew(tenant, userId);

            db.WorkOrders.Add(order);
        }
        else
        {
            order = await db.WorkOrders.ForTenant(tenant)
                .Include(w => w.EquipmentAsset)
                .FirstOrDefaultAsync(w => w.Id == id)
                ?? throw new InvalidOperationException("Work order not found.");
            order.StampUpdated(userId);
        }

        order.EquipmentAssetId = request.EquipmentAssetId;
        order.FaultReportId = request.FaultReportId;
        order.Title = request.Title;
        order.Detail = request.Detail;
        order.Priority = request.Priority;
        order.DueOn = request.DueOn;
        order.AssignedStaffId = request.AssignedStaffId;
        order.ContractorName = request.ContractorName;
        order.ContractorReference = request.ContractorReference;

        if (order.Status == WorkOrderStatus.Open && request.AssignedStaffId is not null)
            order.Status = WorkOrderStatus.Assigned;

        // A critical fault takes the machine off the floor immediately, without a second step.
        if (request.Priority == WorkOrderPriority.Critical && request.EquipmentAssetId is not null)
        {
            var asset = await db.Equipment.ForTenant(tenant)
                .FirstOrDefaultAsync(e => e.Id == request.EquipmentAssetId);

            if (asset is not null && asset.Status == AssetStatus.InService)
                await ApplyStatusAsync(asset, AssetStatus.OutOfOrder, request.Title, userId);
        }

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(order, now);
    }

    public async Task<WorkOrderDto> CompleteWorkOrderAsync(CompleteWorkOrderDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var order = await db.WorkOrders.ForTenant(tenant)
            .Include(w => w.EquipmentAsset)
            .FirstOrDefaultAsync(w => w.Id == request.WorkOrderId)
            ?? throw new InvalidOperationException("Work order not found.");

        order.Status = WorkOrderStatus.Completed;
        order.CompletedOn = now;
        order.ResolutionNote = request.ResolutionNote;
        order.LabourCost = request.LabourCost;
        order.PartsCost = request.PartsCost;
        order.TotalCost = request.LabourCost + request.PartsCost;
        order.PartsUsed = request.PartsUsed;
        order.DowntimeHours = request.DowntimeHours;
        order.PhotoUrls = request.PhotoUrls.Count == 0 ? null : string.Join(',', request.PhotoUrls);
        order.StampUpdated(userId);

        var asset = order.EquipmentAsset;
        if (asset is not null)
        {
            asset.TotalMaintenanceCost += order.TotalCost;
            asset.TotalDowntimeHours += request.DowntimeHours;
            asset.LastServicedOn = now;

            if (request.ReturnToService && asset.Status is AssetStatus.OutOfOrder
                    or AssetStatus.UnderMaintenance or AssetStatus.AwaitingParts)
            {
                await ApplyStatusAsync(asset, AssetStatus.InService, null, userId);
            }

            asset.StampUpdated(userId);
        }

        if (order.MaintenanceScheduleId is not null)
        {
            var schedule = await db.MaintenanceSchedules.ForTenant(tenant)
                .FirstOrDefaultAsync(m => m.Id == order.MaintenanceScheduleId);

            if (schedule is not null)
            {
                schedule.LastPerformedOn = now;
                schedule.NextDueOn = schedule.Trigger == MaintenanceTrigger.Interval && schedule.IntervalDays > 0
                    ? now.Date.AddDays(schedule.IntervalDays)
                    : null;

                if (asset is not null && schedule.IntervalDays > 0)
                    asset.NextServiceDueOn = now.Date.AddDays(schedule.IntervalDays);
            }
        }

        if (order.FaultReportId is not null)
        {
            var fault = await db.FaultReports.ForTenant(tenant)
                .FirstOrDefaultAsync(f => f.Id == order.FaultReportId);

            if (fault is not null)
            {
                fault.IsResolved = true;
                fault.ResolvedOn = now;
            }
        }

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(order, now);
    }

    /// <summary>
    /// Reporting a fault from the floor.
    ///
    /// Two taps after scanning the sticker: what is wrong, and how bad. Everything else — taking
    /// the machine out of service, raising the work order, freeing the bookings that pointed at
    /// it — happens from that.
    /// </summary>
    public async Task<FaultReportDto> ReportFaultAsync(ReportFaultDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        EquipmentAsset? asset = null;

        if (request.EquipmentAssetId is not null)
        {
            asset = await db.Equipment.ForTenant(tenant).FirstOrDefaultAsync(e => e.Id == request.EquipmentAssetId);
        }
        else if (!string.IsNullOrWhiteSpace(request.QrCode))
        {
            asset = await db.Equipment.ForTenant(tenant).FirstOrDefaultAsync(e => e.QrCode == request.QrCode);
            if (asset is null) throw new InvalidOperationException("That code did not match any equipment.");
        }

        var fault = new FaultReport
        {
            ClubId = asset?.ClubId ?? request.ClubId,
            EquipmentAssetId = asset?.Id,
            AreaId = request.AreaId ?? asset?.AreaId,
            FaultDescription = request.FaultDescription,
            Severity = request.Severity,
            ReportedAt = now,
            ReportedByStaffId = userId == Guid.Empty ? null : userId,
            ReportedByMemberId = request.ReportedByMemberId,
            PhotoUrl = request.PhotoUrl,
            TakenOutOfService = request.TakeOutOfService,
        }.StampNew(tenant, userId);

        db.FaultReports.Add(fault);

        if (request.TakeOutOfService && asset is not null)
            await ApplyStatusAsync(asset, AssetStatus.OutOfOrder, request.FaultDescription, userId);

        if (request.CreateWorkOrder)
        {
            var order = new WorkOrder
            {
                WorkOrderNumber = await numbering.NextWorkOrderNumberAsync(now),
                ClubId = fault.ClubId,
                EquipmentAssetId = asset?.Id,
                FaultReportId = fault.Id,
                Title = asset is null ? request.FaultDescription : $"Fault — {asset.Name}",
                Detail = request.FaultDescription,
                Status = WorkOrderStatus.Open,
                Priority = request.Severity,
                RaisedOn = now,
                DueOn = request.Severity == WorkOrderPriority.Critical ? now.Date : now.Date.AddDays(3),
            }.StampNew(tenant, userId);

            db.WorkOrders.Add(order);
            fault.WorkOrderId = order.Id;
        }

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(fault);
    }

    public async Task<List<FaultReportDto>> GetFaultsAsync(Guid? clubId, bool openOnly)
    {
        var faults = await db.FaultReports.ForTenant(tenant)
            .WhereIf(clubId is not null, f => f.ClubId == clubId)
            .WhereIf(openOnly, f => !f.IsResolved)
            .OrderByDescending(f => f.Severity).ThenByDescending(f => f.ReportedAt)
            .Take(200)
            .ToListAsync();

        var assetNames = await db.Equipment.ForTenant(tenant)
            .Select(e => new { e.Id, e.Name }).ToDictionaryAsync(e => e.Id, e => e.Name);

        var areaNames = await db.Areas.ForTenant(tenant)
            .Select(a => new { a.Id, a.Name }).ToDictionaryAsync(a => a.Id, a => a.Name);

        var staffNames = await db.Staff.ForTenant(tenant)
            .Select(s => new { s.Id, Name = s.FirstName + " " + s.LastName })
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        var workOrderNumbers = await db.WorkOrders.ForTenant(tenant)
            .Select(w => new { w.Id, w.WorkOrderNumber })
            .ToDictionaryAsync(w => w.Id, w => w.WorkOrderNumber);

        return [.. faults.Select(f =>
        {
            var dto = FitnessMapper.ToDto(f);
            if (f.EquipmentAssetId is not null) dto.EquipmentName = assetNames.GetValueOrDefault(f.EquipmentAssetId.Value);
            if (f.AreaId is not null) dto.AreaName = areaNames.GetValueOrDefault(f.AreaId.Value);
            if (f.ReportedByStaffId is not null) dto.ReportedByName = staffNames.GetValueOrDefault(f.ReportedByStaffId.Value);
            if (f.WorkOrderId is not null) dto.WorkOrderNumber = workOrderNumbers.GetValueOrDefault(f.WorkOrderId.Value);
            return dto;
        })];
    }

    // ═══ Internals ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Changes an asset's status and propagates it everywhere the machine appears.
    ///
    /// This is the join that makes the maintenance screen worth having: taking a bike out of
    /// service here removes it from the spin studio's spot map and blocks the court it backs,
    /// with no second action to forget.
    /// </summary>
    private async Task ApplyStatusAsync(EquipmentAsset asset, AssetStatus status, string? note, Guid userId)
    {
        var now = DateTime.UtcNow;
        var wasWorking = asset.Status == AssetStatus.InService;

        asset.Status = status;
        asset.OutOfServiceNote = status == AssetStatus.InService ? null : note ?? asset.OutOfServiceNote;
        asset.OutOfServiceSince = status == AssetStatus.InService ? null : asset.OutOfServiceSince ?? now;
        if (status == AssetStatus.Retired) asset.RetiredOn = now;
        asset.StampUpdated(userId);

        var unavailable = status is not AssetStatus.InService;

        // Spot maps.
        var spots = await db.RoomSpots.ForTenant(tenant)
            .Where(s => s.EquipmentAssetId == asset.Id)
            .ToListAsync();

        foreach (var spot in spots)
        {
            spot.IsOutOfService = unavailable;
            spot.StampUpdated(userId);
        }

        // Bookable resources backed by this machine.
        var resources = await db.Resources.ForTenant(tenant)
            .Where(r => r.EquipmentAssetId == asset.Id)
            .ToListAsync();

        foreach (var resource in resources)
        {
            resource.IsOutOfService = unavailable;
            resource.OutOfServiceNote = unavailable ? note : null;
            resource.StampUpdated(userId);
        }

        // Anyone already booked onto a spot that just broke has to be moved.
        if (unavailable && wasWorking && spots.Count > 0)
        {
            var spotIds = spots.Select(s => s.Id).ToList();

            var affected = await db.ClassBookings.ForTenant(tenant)
                .Where(b => b.SpotId != null && spotIds.Contains(b.SpotId.Value)
                         && b.Status == BookingStatus.Booked
                         && b.ClassOccurrence!.StartsAt > now)
                .Include(b => b.ClassOccurrence).ThenInclude(o => o!.ClassType)
                .ToListAsync();

            foreach (var booking in affected)
            {
                booking.SpotId = null;
                booking.SpotLabel = null;
                booking.Note = $"{booking.Note} Spot released — equipment out of service.".Trim();
                booking.StampUpdated(userId);

                if (booking.MemberId is not null)
                {
                    db.MessageLog.Add(new MessageLog
                    {
                        MemberId = booking.MemberId,
                        ClubId = asset.ClubId,
                        Channel = MessageChannel.Push,
                        Status = MessageStatus.Queued,
                        Subject = "Your spot has moved",
                        BodyPreview = $"The equipment at your spot for {booking.ClassOccurrence?.ClassType?.Name} " +
                                      "is out of service. We have moved you to another one — see you there.",
                        QueuedAt = now,
                    }.StampNew(tenant, userId));
                }
            }
        }
    }

    private static int CountDays(int mask)
    {
        var count = 0;
        for (var i = 0; i < 7; i++) if ((mask & (1 << i)) != 0) count++;
        return count;
    }
}
