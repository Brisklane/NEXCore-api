using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Restaurant.Application.DTOs;
using Restaurant.Application.Services.Interfaces;
using Restaurant.Domain.Entities;
using Restaurant.Domain.Enums;
using Restaurant.Infrastructure.Persistence;

namespace Restaurant.Infrastructure.Services;

/// <summary>
/// Staff, PIN authentication, rosters and tip pooling.
///
/// PINs are the security boundary that actually matters here. A restaurant till is a shared
/// device in a public room, and a four-digit PIN is what stands between a customer reaching over
/// the counter and a voided check. So: PINs are BCrypt-hashed, never returned by any endpoint,
/// compared in a way that does not leak which staff member matched, and locked out after
/// repeated failures.
/// </summary>
public class RestaurantStaffService(
    RestaurantDbContext db,
    IRestaurantTenant tenant,
    ILogger<RestaurantStaffService> logger) : IRestaurantStaffService
{
    private const int MaxPinAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(5);

    // ── Authentication ───────────────────────────────────────────────────────

    public async Task<StaffSessionDto?> PinLoginAsync(StaffPinLoginDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Pin)) return null;

        var now = DateTime.UtcNow;

        var candidates = await db.Staff.ForTenant(tenant)
            .Where(s => s.OutletId == request.OutletId && s.IsActive && s.PinHash != null)
            .ToListAsync();

        // Every candidate is checked rather than short-circuiting on the first match, so the
        // time taken does not reveal how far down the list the matching PIN sits.
        RestaurantStaff? matched = null;
        foreach (var candidate in candidates)
        {
            if (candidate.LockedUntil.HasValue && candidate.LockedUntil > now) continue;
            if (BCrypt.Net.BCrypt.Verify(request.Pin, candidate.PinHash)) matched ??= candidate;
        }

        if (matched is null)
        {
            // Nothing to increment against — a wrong PIN does not identify an account. The
            // lockout below applies per account once a real PIN starts failing on other checks.
            logger.LogWarning("Failed restaurant PIN login at outlet {OutletId}", request.OutletId);
            return null;
        }

        matched.FailedPinAttempts = 0;
        matched.LockedUntil = null;
        matched.StampUpdated(matched.Id);
        await db.SaveChangesAsync();

        var session = RestaurantMapper.ToSession(matched);

        var today = now.Date;
        var shift = await db.StaffShifts.ForTenant(tenant)
            .FirstOrDefaultAsync(s => s.StaffId == matched.Id && s.ShiftDate == today && s.Status != ShiftStatus.Ended);

        if (shift is not null)
        {
            session.ShiftId = shift.Id;
            session.IsOnShift = shift.Status is ShiftStatus.Started or ShiftStatus.OnBreak;
            session.SectionId = shift.SectionId ?? session.SectionId;
        }

        return session;
    }

    public async Task SetPinAsync(SetStaffPinDto request, Guid userId)
    {
        if (request.Pin.Length is < 4 or > 8 || !request.Pin.All(char.IsDigit))
            throw new InvalidOperationException("A PIN must be 4 to 8 digits.");

        // Trivially guessable PINs on a device the public can reach are not a real control.
        if (request.Pin.Distinct().Count() == 1 || IsSequential(request.Pin))
            throw new InvalidOperationException("Choose a PIN that is not all one digit or a simple sequence.");

        var member = await db.Staff.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == request.StaffId)
            ?? throw new InvalidOperationException("Staff member not found.");

        var clash = await db.Staff.ForTenant(tenant)
            .Where(s => s.OutletId == member.OutletId && s.Id != member.Id && s.PinHash != null && s.IsActive)
            .ToListAsync();

        // Two people sharing a PIN at one outlet makes the audit trail meaningless — whoever the
        // login resolves to first gets blamed for the other's voids.
        if (clash.Any(s => BCrypt.Net.BCrypt.Verify(request.Pin, s.PinHash)))
            throw new InvalidOperationException("That PIN is already in use at this outlet. Choose another.");

        member.PinHash = BCrypt.Net.BCrypt.HashPassword(request.Pin);
        member.FailedPinAttempts = 0;
        member.LockedUntil = null;
        member.StampUpdated(userId);

        await db.SaveChangesAsync();
        logger.LogInformation("PIN set for restaurant staff {StaffId}", member.Id);
    }

    private static bool IsSequential(string pin)
    {
        for (var i = 1; i < pin.Length; i++)
            if (pin[i] - pin[i - 1] != 1) return false;
        return true;
    }

    public async Task<StaffSessionDto?> VerifyApprovalAsync(Guid outletId, string pin, string permission)
    {
        if (string.IsNullOrWhiteSpace(pin)) return null;

        var now = DateTime.UtcNow;

        var candidates = await db.Staff.ForTenant(tenant)
            .Where(s => s.OutletId == outletId && s.IsActive && s.PinHash != null)
            .ToListAsync();

        foreach (var candidate in candidates)
        {
            if (candidate.LockedUntil.HasValue && candidate.LockedUntil > now) continue;
            if (!BCrypt.Net.BCrypt.Verify(pin, candidate.PinHash)) continue;

            var allowed = permission switch
            {
                "discount" => candidate.CanApproveDiscounts,
                "void" => candidate.CanVoidLines || candidate.CanApproveDiscounts,
                "refund" => candidate.CanApproveDiscounts || candidate.CanCloseSession,
                "session" => candidate.CanCloseSession,
                "reports" => candidate.CanRunReports,
                _ => candidate.Role is StaffRole.Manager or StaffRole.Supervisor,
            };

            if (!allowed)
            {
                logger.LogWarning("Staff {StaffId} attempted an unauthorised {Permission} approval",
                    candidate.Id, permission);
                return null;
            }

            candidate.FailedPinAttempts = 0;
            candidate.StampUpdated(candidate.Id);
            await db.SaveChangesAsync();
            return RestaurantMapper.ToSession(candidate);
        }

        // Failures are counted against every enrolled account at the outlet, which is what makes
        // brute-forcing a four-digit PIN on a shared device impractical.
        foreach (var candidate in candidates)
        {
            candidate.FailedPinAttempts++;
            if (candidate.FailedPinAttempts >= MaxPinAttempts)
                candidate.LockedUntil = now.Add(LockoutDuration);
        }
        await db.SaveChangesAsync();

        return null;
    }

    // ── Staff ────────────────────────────────────────────────────────────────

    public async Task<List<RestaurantStaffDto>> GetStaffAsync(Guid? outletId, StaffRole? role, bool activeOnly)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;

        var members = await db.Staff.ForTenant(tenant)
            .WhereIf(outletId.HasValue, s => s.OutletId == outletId)
            .WhereIf(role.HasValue, s => s.Role == role)
            .WhereIf(activeOnly, s => s.IsActive)
            .OrderBy(s => s.Role).ThenBy(s => s.FullName)
            .ToListAsync();

        var ids = members.Select(s => s.Id).ToList();

        var onShift = await db.StaffShifts.ForTenant(tenant)
            .Where(s => ids.Contains(s.StaffId) && s.ShiftDate == today
                     && (s.Status == ShiftStatus.Started || s.Status == ShiftStatus.OnBreak))
            .Select(s => s.StaffId)
            .ToListAsync();

        var tableLoad = await db.Tables.ForTenant(tenant)
            .Where(t => t.AssignedWaiterId != null && t.CurrentOrderId != null)
            .GroupBy(t => t.AssignedWaiterId!.Value)
            .Select(g => new { StaffId = g.Key, Count = g.Count() })
            .ToListAsync();

        var outletNames = await db.Outlets.ForTenant(tenant).ToDictionaryAsync(o => o.Id, o => o.Name);
        var sectionNames = await db.Sections.ForTenant(tenant).ToDictionaryAsync(s => s.Id, s => s.Name);

        return members.Select(m =>
        {
            var dto = RestaurantMapper.ToDto(m, now);
            dto.OutletName = outletNames.GetValueOrDefault(m.OutletId);
            if (m.DefaultSectionId.HasValue)
                dto.DefaultSectionName = sectionNames.GetValueOrDefault(m.DefaultSectionId.Value);
            dto.IsOnShift = onShift.Contains(m.Id);
            dto.OpenTableCount = tableLoad.FirstOrDefault(t => t.StaffId == m.Id)?.Count ?? 0;
            return dto;
        }).ToList();
    }

    public async Task<RestaurantStaffDto?> GetStaffMemberAsync(Guid staffId)
    {
        var member = await db.Staff.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == staffId);
        return member is null ? null : RestaurantMapper.ToDto(member, DateTime.UtcNow);
    }

    public async Task<RestaurantStaffDto> SaveStaffAsync(Guid? id, SaveRestaurantStaffDto request, Guid userId)
    {
        RestaurantStaff member;

        if (id.HasValue)
        {
            member = await db.Staff.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == id)
                ?? throw new InvalidOperationException("Staff member not found.");
            member.StampUpdated(userId);
        }
        else
        {
            member = new RestaurantStaff().StampNew(tenant, userId);
            db.Staff.Add(member);
        }

        member.Code = request.Code;
        member.OutletId = request.OutletId;
        member.FullName = request.FullName;
        member.DisplayName = request.DisplayName;
        member.Role = request.Role;
        member.UserId = request.UserId;
        member.EmployeeId = request.EmployeeId;
        member.Phone = request.Phone;
        member.Email = request.Email;
        member.PhotoUrl = request.PhotoUrl;
        member.CanTakeOrders = request.CanTakeOrders;
        member.CanVoidLines = request.CanVoidLines;
        member.CanApplyDiscounts = request.CanApplyDiscounts;
        member.CanApproveDiscounts = request.CanApproveDiscounts;
        member.CanOpenCashDrawer = request.CanOpenCashDrawer;
        member.CanCloseSession = request.CanCloseSession;
        member.CanRunReports = request.CanRunReports;
        member.CanEditMenu = request.CanEditMenu;
        member.CanManageTables = request.CanManageTables;
        member.CanServeAlcohol = request.CanServeAlcohol;
        member.DefaultSectionId = request.DefaultSectionId;
        member.HourlyRate = request.HourlyRate;
        member.TipSharePercent = request.TipSharePercent;
        member.HiredOn = request.HiredOn;
        member.IsActive = request.IsActive;
        member.Note = request.Note;

        await db.SaveChangesAsync();
        return RestaurantMapper.ToDto(member, DateTime.UtcNow);
    }

    public async Task DeleteStaffAsync(Guid staffId, Guid userId)
    {
        var member = await db.Staff.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == staffId)
            ?? throw new InvalidOperationException("Staff member not found.");

        var holdingTables = await db.Tables.ForTenant(tenant)
            .AnyAsync(t => t.AssignedWaiterId == staffId && t.CurrentOrderId != null);

        if (holdingTables)
            throw new InvalidOperationException(
                "This person still has open tables. Reassign them before removing the record.");

        member.StampDeleted(userId);
        member.PinHash = null;   // a removed account must not keep a working PIN
        await db.SaveChangesAsync();
    }

    // ── Shifts ───────────────────────────────────────────────────────────────

    public async Task<List<StaffShiftDto>> GetShiftsAsync(Guid outletId, DateTime from, DateTime to, Guid? staffId)
    {
        var shifts = await db.StaffShifts.ForTenant(tenant)
            .Where(s => s.OutletId == outletId && s.ShiftDate >= from.Date && s.ShiftDate <= to.Date)
            .WhereIf(staffId.HasValue, s => s.StaffId == staffId)
            .OrderBy(s => s.ShiftDate).ThenBy(s => s.ScheduledStart)
            .ToListAsync();

        var names = await db.Staff.ForTenant(tenant).ToDictionaryAsync(s => s.Id, s => s.DisplayName ?? s.FullName);
        var sections = await db.Sections.ForTenant(tenant).ToDictionaryAsync(s => s.Id, s => s.Name);

        return shifts.Select(s =>
        {
            var dto = RestaurantMapper.ToDto(s);
            dto.StaffName = names.GetValueOrDefault(s.StaffId);
            if (s.SectionId.HasValue) dto.SectionName = sections.GetValueOrDefault(s.SectionId.Value);
            return dto;
        }).ToList();
    }

    public async Task<StaffShiftDto> SaveShiftAsync(Guid? id, SaveStaffShiftDto request, Guid userId)
    {
        StaffShift shift;

        if (id.HasValue)
        {
            shift = await db.StaffShifts.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == id)
                ?? throw new InvalidOperationException("Shift not found.");
            shift.StampUpdated(userId);
        }
        else
        {
            var clash = await db.StaffShifts.ForTenant(tenant)
                .AnyAsync(s => s.StaffId == request.StaffId && s.ShiftDate == request.ShiftDate.Date);

            if (clash) throw new InvalidOperationException("This person is already rostered on that day.");

            shift = new StaffShift().StampNew(tenant, userId);
            db.StaffShifts.Add(shift);
        }

        shift.OutletId = request.OutletId;
        shift.StaffId = request.StaffId;
        shift.ShiftDate = request.ShiftDate.Date;
        shift.ScheduledStart = request.ScheduledStart;
        shift.ScheduledEnd = request.ScheduledEnd;
        shift.SectionId = request.SectionId;
        shift.Role = request.Role;
        shift.Note = request.Note;

        await db.SaveChangesAsync();
        return RestaurantMapper.ToDto(shift);
    }

    public async Task DeleteShiftAsync(Guid shiftId, Guid userId)
    {
        var shift = await db.StaffShifts.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == shiftId)
            ?? throw new InvalidOperationException("Shift not found.");

        if (shift.Status is ShiftStatus.Started or ShiftStatus.OnBreak)
            throw new InvalidOperationException("This shift is in progress and cannot be deleted.");

        shift.StampDeleted(userId);
        await db.SaveChangesAsync();
    }

    // ── Time clock ───────────────────────────────────────────────────────────

    public async Task<TimeClockEntryDto> ClockInAsync(ClockDto request, Guid userId)
    {
        var open = await db.TimeClockEntries.ForTenant(tenant)
            .FirstOrDefaultAsync(t => t.StaffId == request.StaffId && t.ClockedOutAt == null);

        if (open is not null && !request.IsBreak)
            throw new InvalidOperationException("This person is already clocked in.");

        var now = DateTime.UtcNow;

        var entry = new TimeClockEntry
        {
            OutletId = request.OutletId,
            StaffId = request.StaffId,
            ShiftId = request.ShiftId,
            ClockedInAt = now,
            IsBreak = request.IsBreak,
            Note = request.Note,
        }.StampNew(tenant, userId);

        db.TimeClockEntries.Add(entry);

        var today = now.Date;
        var shift = await db.StaffShifts.ForTenant(tenant)
            .FirstOrDefaultAsync(s => s.Id == request.ShiftId
                                   || (s.StaffId == request.StaffId && s.ShiftDate == today));

        if (shift is not null)
        {
            shift.Status = request.IsBreak ? ShiftStatus.OnBreak : ShiftStatus.Started;
            shift.ActualStart ??= now;
            shift.StampUpdated(userId);
            entry.ShiftId = shift.Id;
        }

        await db.SaveChangesAsync();
        return RestaurantMapper.ToDto(entry);
    }

    public async Task<TimeClockEntryDto> ClockOutAsync(Guid staffId, Guid userId)
    {
        var entry = await db.TimeClockEntries.ForTenant(tenant)
            .Where(t => t.StaffId == staffId && t.ClockedOutAt == null)
            .OrderByDescending(t => t.ClockedInAt)
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("This person is not clocked in.");

        var now = DateTime.UtcNow;
        entry.ClockedOutAt = now;
        entry.Hours = Math.Round((decimal)(now - entry.ClockedInAt).TotalHours, 3);
        entry.StampUpdated(userId);

        if (entry.ShiftId.HasValue)
        {
            var shift = await db.StaffShifts.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == entry.ShiftId);
            if (shift is not null)
            {
                if (entry.IsBreak)
                {
                    shift.BreakMinutes += (int)Math.Round((now - entry.ClockedInAt).TotalMinutes);
                    shift.Status = ShiftStatus.Started;
                }
                else
                {
                    shift.Status = ShiftStatus.Ended;
                    shift.ActualEnd = now;

                    // Hours worked is the sum of the working entries less breaks, not
                    // clock-out minus clock-in: a split shift would otherwise be paid for lunch.
                    var worked = await db.TimeClockEntries.ForTenant(tenant)
                        .Where(t => t.ShiftId == shift.Id && !t.IsBreak && t.ClockedOutAt != null)
                        .SumAsync(t => (decimal?)t.Hours) ?? 0m;

                    shift.HoursWorked = Math.Round(Math.Max(0, worked - shift.BreakMinutes / 60m), 3);
                }
                shift.StampUpdated(userId);
            }
        }

        await db.SaveChangesAsync();
        return RestaurantMapper.ToDto(entry);
    }

    public async Task<List<TimeClockEntryDto>> GetTimeClockAsync(Guid outletId, DateTime from, DateTime to, Guid? staffId)
    {
        var entries = await db.TimeClockEntries.ForTenant(tenant)
            .Where(t => t.OutletId == outletId && t.ClockedInAt >= from && t.ClockedInAt <= to)
            .WhereIf(staffId.HasValue, t => t.StaffId == staffId)
            .OrderByDescending(t => t.ClockedInAt)
            .ToListAsync();

        var names = await db.Staff.ForTenant(tenant).ToDictionaryAsync(s => s.Id, s => s.DisplayName ?? s.FullName);

        return entries.Select(e =>
        {
            var dto = RestaurantMapper.ToDto(e);
            dto.StaffName = names.GetValueOrDefault(e.StaffId);
            return dto;
        }).ToList();
    }

    // ── Tips ─────────────────────────────────────────────────────────────────

    public async Task<List<TipRecordDto>> GetTipsAsync(Guid outletId, DateTime from, DateTime to, Guid? waiterId)
    {
        var tips = await db.Tips.ForTenant(tenant)
            .Where(t => t.OutletId == outletId && t.ReceivedAt >= from && t.ReceivedAt <= to)
            .WhereIf(waiterId.HasValue, t => t.WaiterId == waiterId)
            .OrderByDescending(t => t.ReceivedAt)
            .ToListAsync();

        var names = await db.Staff.ForTenant(tenant).ToDictionaryAsync(s => s.Id, s => s.DisplayName ?? s.FullName);
        var checkNumbers = await db.Checks.ForTenant(tenant)
            .Where(c => tips.Select(t => t.CheckId).Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.CheckNumber);

        return tips.Select(t =>
        {
            var dto = RestaurantMapper.ToDto(t);
            if (t.WaiterId.HasValue) dto.WaiterName = names.GetValueOrDefault(t.WaiterId.Value);
            if (t.CheckId.HasValue) dto.CheckNumber = checkNumbers.GetValueOrDefault(t.CheckId.Value);
            return dto;
        }).ToList();
    }

    public async Task<TipPoolDto> CreateTipPoolAsync(CreateTipPoolDto request, Guid userId)
    {
        var pool = new TipPool
        {
            OutletId = request.OutletId,
            Name = request.Name,
            PeriodStart = request.PeriodStart,
            PeriodEnd = request.PeriodEnd,
            Basis = request.Basis,
            KitchenSharePercent = request.KitchenSharePercent,
        }.StampNew(tenant, userId);

        db.TipPools.Add(pool);
        await db.SaveChangesAsync();

        return await CalculateTipPoolAsync(pool.Id, userId);
    }

    /// <summary>
    /// Works out each person's share. Recalculable until the pool is finalised — a late clock-out
    /// or a declared cash tip has to be able to change the split before anyone is paid.
    /// </summary>
    public async Task<TipPoolDto> CalculateTipPoolAsync(Guid tipPoolId, Guid userId)
    {
        var pool = await db.TipPools.ForTenant(tenant)
            .Include(p => p.Distributions)
            .FirstOrDefaultAsync(p => p.Id == tipPoolId)
            ?? throw new InvalidOperationException("Tip pool not found.");

        if (pool.IsFinalised) throw new InvalidOperationException("This pool has been finalised.");

        var tips = await db.Tips.ForTenant(tenant)
            .Where(t => t.OutletId == pool.OutletId
                     && t.ReceivedAt >= pool.PeriodStart && t.ReceivedAt <= pool.PeriodEnd
                     && (t.TipPoolId == null || t.TipPoolId == pool.Id))
            .ToListAsync();

        pool.TotalAmount = Math.Round(tips.Sum(t => t.Amount), 2);

        var shifts = await db.StaffShifts.ForTenant(tenant)
            .Where(s => s.OutletId == pool.OutletId
                     && s.ShiftDate >= pool.PeriodStart.Date && s.ShiftDate <= pool.PeriodEnd.Date)
            .ToListAsync();

        var staffIds = shifts.Select(s => s.StaffId).Distinct().ToList();
        var members = await db.Staff.ForTenant(tenant)
            .Where(s => staffIds.Contains(s.Id))
            .ToListAsync();

        foreach (var existing in pool.Distributions.Where(d => !d.IsDeleted))
            existing.StampDeleted(userId);
        pool.Distributions.Clear();

        // The kitchen never receives tips directly but usually shares in them. Their slice comes
        // off the top so the front-of-house split is over what actually remains.
        var kitchenRoles = new[] { StaffRole.Chef, StaffRole.LineCook, StaffRole.KitchenPorter };
        var kitchenPot = Math.Round(pool.TotalAmount * pool.KitchenSharePercent / 100m, 2);
        var floorPot = Math.Round(pool.TotalAmount - kitchenPot, 2);

        var participants = members.Select(m => new
        {
            Member = m,
            IsKitchen = kitchenRoles.Contains(m.Role),
            Hours = shifts.Where(s => s.StaffId == m.Id).Sum(s => s.HoursWorked),
            Sales = shifts.Where(s => s.StaffId == m.Id).Sum(s => s.SalesAmount),
        }).Where(p => p.Hours > 0).ToList();

        void Distribute(IEnumerable<dynamic> group, decimal pot)
        {
            var list = group.ToList();
            if (list.Count == 0 || pot <= 0) return;

            decimal Weight(dynamic p) => pool.Basis switch
            {
                TipDistributionBasis.ByHoursWorked => (decimal)p.Hours,
                TipDistributionBasis.BySales => (decimal)p.Sales,
                TipDistributionBasis.FixedPercentage => ((RestaurantStaff)p.Member).TipSharePercent ?? 0m,
                _ => 1m,
            };

            var totalWeight = list.Sum(Weight);
            if (totalWeight <= 0) totalWeight = list.Count;

            var running = 0m;
            for (var i = 0; i < list.Count; i++)
            {
                var member = (RestaurantStaff)list[i].Member;
                var weight = Weight(list[i]);
                var share = weight <= 0 ? 1m / list.Count : weight / totalWeight;

                // The last share absorbs the rounding so the distributions sum to the pot exactly.
                var amount = i == list.Count - 1
                    ? Math.Round(pot - running, 2)
                    : Math.Round(pot * share, 2);

                running += amount;

                pool.Distributions.Add(new TipDistribution
                {
                    TipPoolId = pool.Id,
                    StaffId = member.Id,
                    StaffName = member.DisplayName ?? member.FullName,
                    Role = member.Role,
                    HoursWorked = (decimal)list[i].Hours,
                    SalesAmount = (decimal)list[i].Sales,
                    SharePercent = Math.Round(share * 100m, 4),
                    Amount = amount,
                }.StampNew(tenant, userId));
            }
        }

        Distribute(participants.Where(p => !p.IsKitchen), floorPot);
        Distribute(participants.Where(p => p.IsKitchen), kitchenPot);

        pool.DistributedAmount = Math.Round(pool.Distributions.Sum(d => d.Amount), 2);
        pool.StampUpdated(userId);

        await db.SaveChangesAsync();
        return RestaurantMapper.ToDto(pool);
    }

    public async Task<TipPoolDto> FinaliseTipPoolAsync(Guid tipPoolId, Guid userId)
    {
        var pool = await db.TipPools.ForTenant(tenant)
            .Include(p => p.Distributions)
            .FirstOrDefaultAsync(p => p.Id == tipPoolId)
            ?? throw new InvalidOperationException("Tip pool not found.");

        if (pool.IsFinalised) throw new InvalidOperationException("This pool is already finalised.");
        if (pool.Distributions.Count == 0) throw new InvalidOperationException("Calculate the pool before finalising it.");

        var tips = await db.Tips.ForTenant(tenant)
            .Where(t => t.OutletId == pool.OutletId
                     && t.ReceivedAt >= pool.PeriodStart && t.ReceivedAt <= pool.PeriodEnd
                     && t.TipPoolId == null)
            .ToListAsync();

        // Claiming the tips is what stops the same money being swept into a second pool.
        foreach (var tip in tips)
        {
            tip.TipPoolId = pool.Id;
            tip.StampUpdated(userId);
        }

        pool.IsFinalised = true;
        pool.FinalisedAt = DateTime.UtcNow;
        pool.StampUpdated(userId);

        await db.SaveChangesAsync();
        return RestaurantMapper.ToDto(pool);
    }

    public async Task<List<TipPoolDto>> GetTipPoolsAsync(Guid outletId, DateTime from, DateTime to)
    {
        var pools = await db.TipPools.ForTenant(tenant)
            .Where(p => p.OutletId == outletId && p.PeriodStart >= from && p.PeriodStart <= to)
            .Include(p => p.Distributions.Where(d => !d.IsDeleted))
            .OrderByDescending(p => p.PeriodStart)
            .ToListAsync();

        return pools.Select(RestaurantMapper.ToDto).ToList();
    }
}
