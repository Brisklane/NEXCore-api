using Fitness.Application.DTOs;
using Fitness.Application.Services.Interfaces;
using Fitness.Domain.Entities;
using Fitness.Domain.Enums;
using Fitness.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;

namespace Fitness.Infrastructure.Services;

/// <summary>
/// Staff, the rota, the time clock and the commission engine.
///
/// The commission engine is the part generic systems get wrong. A trainer is routinely paid three
/// ways at once — a rate per session delivered, a percentage of the packages they sell, and a
/// bonus for hitting a target — each with its own base, its own tiers and its own cap. So rules
/// are rows, accruals happen the moment the earning event happens, and a statement is the sum of
/// however many of them applied. A trainer can see their earnings move during the month, which is
/// both fairer and a great deal quieter than finding out on payday.
/// </summary>
public class StaffService(
    FitnessDbContext db,
    IFitnessTenant tenant,
    FitnessNumbering numbering) : IStaffService
{
    // ── Staff ────────────────────────────────────────────────────────────────

    public async Task<PaginatedResponse<StaffSummaryDto>> ListAsync(
        Guid? clubId, StaffRoleKind? role, bool activeOnly, string? search, PaginationParams pagination)
    {
        var now = DateTime.UtcNow;

        var query = db.Staff.ForTenant(tenant)
            .Include(s => s.Club)
            .Include(s => s.Certifications.Where(c => !c.IsDeleted))
            .WhereIf(clubId is not null, s => s.ClubId == clubId)
            .WhereIf(role is not null, s => s.RoleKind == role)
            .WhereIf(activeOnly, s => s.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(s =>
                s.FirstName.ToLower().Contains(term) ||
                s.LastName.ToLower().Contains(term) ||
                (s.Email != null && s.Email.ToLower().Contains(term)));
        }

        var total = await query.CountAsync();

        var page = await query
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        var ids = page.Select(s => s.Id).ToList();
        var clockedIn = await db.TimeClock.ForTenant(tenant)
            .Where(t => ids.Contains(t.StaffId) && t.ClockedOutAt == null)
            .Select(t => new { t.StaffId, t.ClockedInAt })
            .ToListAsync();

        var items = page.Select(s =>
        {
            var dto = FitnessMapper.ToSummary(s, now);
            var clock = clockedIn.FirstOrDefault(c => c.StaffId == s.Id);
            dto.IsClockedIn = clock is not null;
            dto.ClockedInAt = clock?.ClockedInAt;
            return dto;
        }).ToList();

        return PaginatedResponse<StaffSummaryDto>.Ok(
                   items,
                   total,
                   pagination.PageNumber,
                   pagination.PageSize);
    }

    public async Task<StaffDetailDto?> GetAsync(Guid staffId)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var member = await db.Staff.ForTenant(tenant)
            .Include(s => s.Club)
            .Include(s => s.Certifications.Where(c => !c.IsDeleted))
            .FirstOrDefaultAsync(s => s.Id == staffId);

        if (member is null) return null;

        var summary = FitnessMapper.ToSummary(member, now);

        var dto = new StaffDetailDto
        {
            Id = summary.Id,
            EmployeeId = summary.EmployeeId,
            UserId = summary.UserId,
            ClubId = summary.ClubId,
            ClubName = summary.ClubName,
            FirstName = summary.FirstName,
            LastName = summary.LastName,
            FullName = summary.FullName,
            DisplayName = summary.DisplayName,
            PhotoUrl = summary.PhotoUrl,
            Phone = summary.Phone,
            Email = summary.Email,
            RoleKind = summary.RoleKind,
            StartedOn = summary.StartedOn,
            LeftOn = summary.LeftOn,
            IsContractor = summary.IsContractor,
            IsBookable = summary.IsBookable,
            IsActive = summary.IsActive,
            CertificationStatus = summary.CertificationStatus,
            ExpiringCertifications = summary.ExpiringCertifications,

            HasPin = !string.IsNullOrEmpty(member.PinHash),
            AccessCredentialId = member.AccessCredentialId,
            AccessRuleId = member.AccessRuleId,
            CanSell = member.CanSell,
            CanTrain = member.CanTrain,
            CanTeach = member.CanTeach,
            CanApproveOverrides = member.CanApproveOverrides,
            AdditionalClubIds = ParseGuids(member.AdditionalClubIds),
            Certifications = [.. member.Certifications.Where(c => !c.IsDeleted).Select(c => FitnessMapper.ToDto(c, now))],
        };

        if (member.AccessRuleId is not null)
        {
            dto.AccessRuleName = await db.AccessRules.ForTenant(tenant)
                .Where(r => r.Id == member.AccessRuleId).Select(r => r.Name).FirstOrDefaultAsync();
        }

        var bookable = await db.BookableStaff.ForTenant(tenant)
            .Include(b => b.Availability.Where(a => !a.IsDeleted))
            .FirstOrDefaultAsync(b => b.StaffId == staffId);

        if (bookable is not null) dto.BookableProfile = FitnessMapper.ToDto(bookable);

        // This period's performance, so opening the record is worth doing.
        dto.SessionsThisPeriod = await db.Appointments.ForTenant(tenant)
            .CountAsync(a => a.StaffId == staffId && a.CompletedAt >= monthStart);

        dto.ClassesThisPeriod = await db.ClassOccurrences.ForTenant(tenant)
            .CountAsync(o => (o.InstructorStaffId == staffId || o.SubstituteStaffId == staffId)
                          && o.StartsAt >= monthStart && o.Status == ClassOccurrenceStatus.Completed);

        dto.SalesThisPeriod = await db.Agreements.ForTenant(tenant)
            .CountAsync(a => a.SoldByStaffId == staffId && a.StartsOn >= monthStart);

        dto.CommissionThisPeriod = await db.CommissionAccruals.ForTenant(tenant)
            .Where(a => a.StaffId == staffId && a.EarnedOn >= monthStart && !a.IsReversed)
            .SumAsync(a => (decimal?)a.Amount) ?? 0m;

        var minutes = await db.TimeClock.ForTenant(tenant)
            .Where(t => t.StaffId == staffId && t.ClockedInAt >= monthStart && t.WorkedMinutes != null)
            .SumAsync(t => (int?)t.WorkedMinutes) ?? 0;

        dto.HoursThisPeriod = minutes / 60;

        var clientIds = await db.CoachAssignments.ForTenant(tenant)
            .Where(c => c.StaffId == staffId && c.EndedOn == null)
            .Select(c => c.MemberId)
            .ToListAsync();

        dto.AssignedClients = clientIds.Count;

        if (clientIds.Count > 0)
        {
            var stillActive = await db.Members.ForTenant(tenant)
                .CountAsync(m => clientIds.Contains(m.Id) && m.Status == MemberStatus.Active);

            dto.ClientRetentionPercent = FitnessMapper.Percent(stillActive, clientIds.Count);
        }

        var openClock = await db.TimeClock.ForTenant(tenant)
            .FirstOrDefaultAsync(t => t.StaffId == staffId && t.ClockedOutAt == null);

        dto.IsClockedIn = openClock is not null;
        dto.ClockedInAt = openClock?.ClockedInAt;

        return dto;
    }

    public async Task<StaffDetailDto> SaveAsync(Guid? id, SaveStaffDto request, Guid userId)
    {
        FitnessStaff member;
        if (id is null)
        {
            member = new FitnessStaff { ClubId = request.ClubId }.StampNew(tenant, userId);
            db.Staff.Add(member);
        }
        else
        {
            member = await db.Staff.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == id)
                ?? throw new InvalidOperationException("Staff member not found.");
            member.StampUpdated(userId);
        }

        member.EmployeeId = request.EmployeeId;
        member.UserId = request.UserId;
        member.ClubId = request.ClubId;
        member.FirstName = request.FirstName;
        member.LastName = request.LastName;
        member.DisplayName = request.DisplayName;
        member.Phone = request.Phone;
        member.Email = request.Email;
        member.PhotoUrl = request.PhotoUrl;
        member.RoleKind = request.RoleKind;
        member.StartedOn = request.StartedOn;
        member.LeftOn = request.LeftOn;
        member.IsContractor = request.IsContractor;
        member.AccessRuleId = request.AccessRuleId;
        member.CanSell = request.CanSell;
        member.CanTrain = request.CanTrain;
        member.CanTeach = request.CanTeach;
        member.CanApproveOverrides = request.CanApproveOverrides;
        member.IsBookable = request.IsBookable;
        member.AdditionalClubIds = request.AdditionalClubIds.Count == 0
            ? null
            : string.Join(',', request.AdditionalClubIds);
        member.IsActive = request.IsActive;

        // A PIN is hashed on the way in and never stored, logged or returned in the clear.
        if (!string.IsNullOrWhiteSpace(request.Pin))
        {
            if (request.Pin.Length < 4)
                throw new InvalidOperationException("A PIN needs at least four digits.");

            member.PinHash = BCrypt.Net.BCrypt.HashPassword(request.Pin);
        }

        await db.SaveChangesAsync();

        // A bookable trainer needs a profile, or the diary has nobody to book.
        if (member.IsBookable)
        {
            var exists = await db.BookableStaff.ForTenant(tenant)
                .AnyAsync(b => b.StaffId == member.Id && b.ClubId == member.ClubId);

            if (!exists)
            {
                db.BookableStaff.Add(new BookableStaff
                {
                    StaffId = member.Id,
                    ClubId = member.ClubId,
                    DisplayName = member.DisplayName ?? $"{member.FirstName} {member.LastName}",
                    PhotoUrl = member.PhotoUrl,
                    IsContractor = member.IsContractor,
                }.StampNew(tenant, userId));

                await db.SaveChangesAsync();
            }
        }

        return (await GetAsync(member.Id))!;
    }

    public async Task DeactivateAsync(Guid staffId, DateTime leftOn, Guid userId)
    {
        var member = await db.Staff.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == staffId)
            ?? throw new InvalidOperationException("Staff member not found.");

        var upcoming = await db.Appointments.ForTenant(tenant)
            .CountAsync(a => a.StaffId == staffId && a.StartsAt > DateTime.UtcNow
                          && a.Status != AppointmentStatus.Cancelled);

        var classes = await db.ClassOccurrences.ForTenant(tenant)
            .CountAsync(o => o.InstructorStaffId == staffId && o.StartsAt > DateTime.UtcNow
                          && o.Status != ClassOccurrenceStatus.Cancelled);

        if (upcoming + classes > 0)
            throw new InvalidOperationException(
                $"{upcoming} session(s) and {classes} class(es) are still assigned to this person. " +
                "Reassign or cancel them first.");

        member.IsActive = false;
        member.LeftOn = leftOn;
        member.PinHash = null;
        member.StampUpdated(userId);

        // Their fob stops working the day they leave.
        if (member.AccessCredentialId is not null)
        {
            var credential = await db.Credentials.ForTenant(tenant)
                .FirstOrDefaultAsync(c => c.Id == member.AccessCredentialId);

            if (credential is not null)
            {
                credential.Status = CredentialStatus.Deactivated;
                credential.DeactivatedOn = leftOn;
                credential.DeactivationReason = "Left the club";
            }
        }

        var bookable = await db.BookableStaff.ForTenant(tenant)
            .Where(b => b.StaffId == staffId)
            .ToListAsync();

        foreach (var profile in bookable)
        {
            profile.IsActive = false;
            profile.AcceptingNewClients = false;
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// PIN sign-in on a shared terminal.
    ///
    /// Deliberately vague on failure — "that PIN was not recognised" rather than saying whether it
    /// matched nobody or matched an inactive person, because a shared screen in a public reception
    /// is not a place to leak who works here.
    /// </summary>
    public async Task<StaffPinResultDto> VerifyPinAsync(StaffPinLoginDto request)
    {
        var now = DateTime.UtcNow;

        var candidates = await db.Staff.ForTenant(tenant)
            .Where(s => s.ClubId == request.ClubId && s.IsActive && s.PinHash != null)
            .Include(s => s.Certifications.Where(c => !c.IsDeleted))
            .ToListAsync();

        var match = candidates.FirstOrDefault(s => BCrypt.Net.BCrypt.Verify(request.Pin, s.PinHash));

        if (match is null)
            return new StaffPinResultDto { Success = false, Message = "That PIN was not recognised." };

        var roles = await db.StaffRoles.ForTenant(tenant)
            .Where(r => r.BaseKind == match.RoleKind && r.IsActive)
            .ToListAsync();

        var permissions = roles
            .SelectMany(r => r.Permissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct()
            .ToList();

        return new StaffPinResultDto
        {
            Success = true,
            Staff = FitnessMapper.ToSummary(match, now),
            Permissions = permissions,
        };
    }

    /// <summary>
    /// A manager authorising something above someone else's limit.
    ///
    /// Checks the PIN, checks the limit, and writes an audit row naming what was authorised and
    /// why. An override with no record is indistinguishable from a fraud.
    /// </summary>
    public async Task<bool> VerifyOverrideAsync(ManagerOverrideDto request, Guid userId)
    {
        var candidates = await db.Staff.ForTenant(tenant)
            .Where(s => s.ClubId == request.ClubId && s.IsActive && s.CanApproveOverrides && s.PinHash != null)
            .ToListAsync();

        var manager = candidates.FirstOrDefault(s => BCrypt.Net.BCrypt.Verify(request.Pin, s.PinHash));
        if (manager is null) return false;

        if (request.Amount is not null)
        {
            var role = await db.StaffRoles.ForTenant(tenant)
                .Where(r => r.BaseKind == manager.RoleKind && r.IsActive)
                .OrderByDescending(r => r.RefundLimit ?? 0)
                .FirstOrDefaultAsync();

            var limit = request.Action.Contains("refund", StringComparison.OrdinalIgnoreCase)
                ? role?.RefundLimit
                : request.Action.Contains("write", StringComparison.OrdinalIgnoreCase)
                    ? role?.WriteOffLimit
                    : null;

            if (limit is not null && request.Amount > limit)
                throw new InvalidOperationException(
                    $"{manager.FirstName} can authorise up to {limit:0.00}. This needs someone more senior.");
        }

        db.Audit.Add(new AuditEntry
        {
            ClubId = request.ClubId,
            OccurredAt = DateTime.UtcNow,
            ActorUserId = userId,
            ActorStaffId = manager.Id,
            ActorName = $"{manager.FirstName} {manager.LastName}",
            Action = "Override",
            EntityType = request.EntityType ?? "Unknown",
            EntityId = request.EntityId,
            ChangeSummary = $"{request.Action}" + (request.Amount is not null ? $" for {request.Amount:0.00}" : ""),
            Reason = request.Reason,
        }.StampNew(tenant, userId));

        await db.SaveChangesAsync();
        return true;
    }

    // ── Roles & certifications ───────────────────────────────────────────────

    public async Task<List<StaffRoleDto>> GetRolesAsync(Guid? clubId)
    {
        var roles = await db.StaffRoles.ForTenant(tenant)
            .WhereIf(clubId is not null, r => r.ClubId == clubId || r.ClubId == null)
            .OrderBy(r => r.Name)
            .ToListAsync();

        var counts = await db.Staff.ForTenant(tenant)
            .Where(s => s.IsActive)
            .GroupBy(s => s.RoleKind)
            .Select(g => new { Kind = g.Key, Count = g.Count() })
            .ToListAsync();

        return [.. roles.Select(r =>
        {
            var dto = FitnessMapper.ToDto(r);
            dto.StaffCount = counts.FirstOrDefault(c => c.Kind == r.BaseKind)?.Count ?? 0;
            return dto;
        })];
    }

    public async Task<StaffRoleDto> SaveRoleAsync(Guid? id, StaffRoleDto request, Guid userId)
    {
        StaffRole role;
        if (id is null)
        {
            role = new StaffRole().StampNew(tenant, userId);
            db.StaffRoles.Add(role);
        }
        else
        {
            role = await db.StaffRoles.ForTenant(tenant).FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new InvalidOperationException("Role not found.");

            if (role.IsSystemRole && role.Name != request.Name)
                throw new InvalidOperationException("A built-in role cannot be renamed, but its permissions can be changed.");

            role.StampUpdated(userId);
        }

        role.Name = request.Name;
        role.ClubId = request.ClubId;
        role.BaseKind = request.BaseKind;
        role.Permissions = string.Join(',', request.Permissions);
        role.DiscountLimitPercent = request.DiscountLimitPercent;
        role.RefundLimit = request.RefundLimit;
        role.WriteOffLimit = request.WriteOffLimit;
        role.CanOverridePolicies = request.CanOverridePolicies;
        role.CanViewMedicalData = request.CanViewMedicalData;
        role.CanExportMemberData = request.CanExportMemberData;
        role.IsActive = request.IsActive;

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(role);
    }

    public async Task<List<StaffCertificationDto>> GetCertificationsAsync(Guid? clubId, Guid? staffId, bool expiringOnly)
    {
        var now = DateTime.UtcNow;
        var soon = now.AddDays(60);

        var certifications = await db.Certifications.ForTenant(tenant)
            .Include(c => c.Staff)
            .WhereIf(staffId is not null, c => c.StaffId == staffId)
            .WhereIf(clubId is not null, c => c.Staff!.ClubId == clubId)
            .WhereIf(expiringOnly, c => c.ExpiresOn != null && c.ExpiresOn <= soon)
            .OrderBy(c => c.ExpiresOn ?? DateTime.MaxValue)
            .ToListAsync();

        return [.. certifications.Select(c => FitnessMapper.ToDto(c, now))];
    }

    public async Task<StaffCertificationDto> SaveCertificationAsync(Guid? id, StaffCertificationDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        StaffCertification certification;
        if (id is null)
        {
            certification = new StaffCertification { StaffId = request.StaffId }.StampNew(tenant, userId);
            db.Certifications.Add(certification);
        }
        else
        {
            certification = await db.Certifications.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new InvalidOperationException("Certification not found.");
            certification.StampUpdated(userId);
        }

        certification.Name = request.Name;
        certification.Category = request.Category;
        certification.IssuingBody = request.IssuingBody;
        certification.ReferenceNumber = request.ReferenceNumber;
        certification.IssuedOn = request.IssuedOn;
        certification.ExpiresOn = request.ExpiresOn;
        certification.DocumentUrl = request.DocumentUrl;
        certification.BlocksWorkOnExpiry = request.BlocksWorkOnExpiry;
        certification.Status = request.ExpiresOn is null ? CertificationStatus.Valid
            : request.ExpiresOn < now ? CertificationStatus.Expired
            : CertificationStatus.Valid;
        certification.ReminderSent = false;

        await db.SaveChangesAsync();

        var saved = await db.Certifications.ForTenant(tenant)
            .Include(c => c.Staff)
            .FirstAsync(c => c.Id == certification.Id);

        return FitnessMapper.ToDto(saved, now);
    }

    // ── Rota ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// The rota grid, with the coverage gaps.
    ///
    /// The gaps are the reason the screen exists: a manager does not need to be shown their own
    /// shifts, they need to be shown the Thursday evening nobody is on reception.
    /// </summary>
    public async Task<RotaDto> GetRotaAsync(Guid clubId, DateTime from, DateTime to)
    {
        var now = DateTime.UtcNow;

        var club = await db.Clubs.ForTenant(tenant)
            .Include(c => c.Schedules.Where(s => !s.IsDeleted))
            .FirstOrDefaultAsync(c => c.Id == clubId)
            ?? throw new InvalidOperationException("Club not found.");

        var shifts = await db.Shifts.ForTenant(tenant)
            .Where(s => s.ClubId == clubId && s.StartsAt >= from && s.StartsAt <= to)
            .Include(s => s.Assignments.Where(a => !a.IsDeleted)).ThenInclude(a => a.Staff)
            .OrderBy(s => s.StartsAt)
            .ToListAsync();

        var staff = await db.Staff.ForTenant(tenant)
            .Where(s => s.ClubId == clubId && s.IsActive)
            .Include(s => s.Certifications.Where(c => !c.IsDeleted))
            .ToListAsync();

        var timeOff = await db.TimeOff.ForTenant(tenant)
            .Where(t => staff.Select(s => s.Id).Contains(t.StaffId) && t.StartsAt <= to && t.EndsAt >= from)
            .ToListAsync();

        var staffNames = staff.ToDictionary(s => s.Id, s => $"{s.FirstName} {s.LastName}");

        var dto = new RotaDto
        {
            ClubId = clubId,
            ClubName = club.Name,
            From = from,
            To = to,
            Shifts = [.. shifts.Select(FitnessMapper.ToDto)],
            Staff = [.. staff.Select(s => FitnessMapper.ToSummary(s, now))],
            TimeOff = [.. timeOff.Select(t => new StaffTimeOffDto
            {
                Id = t.Id,
                StaffId = t.StaffId,
                StaffName = staffNames.GetValueOrDefault(t.StaffId),
                StartsAt = t.StartsAt,
                EndsAt = t.EndsAt,
                Reason = t.Reason,
                Note = t.Note,
                IsAllDay = t.IsAllDay,
                IsApproved = t.IsApproved,
            })],
        };

        dto.TotalHours = dto.Shifts.Sum(s => s.Hours);
        dto.UnassignedShifts = dto.Shifts.Count(s => s.IsUnderStaffed);

        dto.OpenSwapRequests = await db.SwapRequests.ForTenant(tenant)
            .CountAsync(r => !r.IsApproved && !r.IsCancelled
                          && r.ShiftAssignment!.Shift!.ClubId == clubId);

        // Hours the club is open with nobody rostered.
        for (var day = from.Date; day <= to.Date; day = day.AddDays(1))
        {
            var schedule = club.Schedules.FirstOrDefault(s => s.OverrideDate?.Date == day)
                           ?? club.Schedules.FirstOrDefault(s => s.OverrideDate is null && s.DayOfWeek == (int)day.DayOfWeek);

            if (schedule is null || schedule.IsClosed) continue;

            var staffedFrom = day.Add(schedule.StaffedFrom ?? schedule.OpensAt);
            var staffedTo = day.Add(schedule.StaffedTo ?? schedule.ClosesAt);

            var covering = shifts
                .Where(s => s.StartsAt < staffedTo && s.EndsAt > staffedFrom
                         && s.Assignments.Any(a => !a.IsDeleted))
                .OrderBy(s => s.StartsAt)
                .ToList();

            var cursor = staffedFrom;
            foreach (var shift in covering)
            {
                if (shift.StartsAt > cursor)
                {
                    dto.CoverageGaps.Add(new CoverageGapDto
                    {
                        From = cursor,
                        To = shift.StartsAt,
                        Message = $"Nobody rostered {cursor:HH:mm}–{shift.StartsAt:HH:mm} on {cursor:ddd d MMM}",
                        IsCritical = true,
                    });
                }

                if (shift.EndsAt > cursor) cursor = shift.EndsAt;
            }

            if (cursor < staffedTo)
            {
                dto.CoverageGaps.Add(new CoverageGapDto
                {
                    From = cursor,
                    To = staffedTo,
                    Message = $"Nobody rostered {cursor:HH:mm}–{staffedTo:HH:mm} on {cursor:ddd d MMM}",
                    IsCritical = true,
                });
            }
        }

        return dto;
    }

    public async Task<ShiftDto> SaveShiftAsync(Guid? id, SaveShiftDto request, Guid userId)
    {
        if (request.EndsAt <= request.StartsAt)
            throw new InvalidOperationException("A shift has to end after it starts.");

        Shift shift;
        if (id is null)
        {
            shift = new Shift { ClubId = request.ClubId }.StampNew(tenant, userId);
            db.Shifts.Add(shift);
        }
        else
        {
            shift = await db.Shifts.ForTenant(tenant)
                .Include(s => s.Assignments.Where(a => !a.IsDeleted))
                .FirstOrDefaultAsync(s => s.Id == id)
                ?? throw new InvalidOperationException("Shift not found.");
            shift.StampUpdated(userId);
        }

        shift.Title = request.Title;
        shift.Position = request.Position;
        shift.StartsAt = request.StartsAt;
        shift.EndsAt = request.EndsAt;
        shift.BreakMinutes = request.BreakMinutes;
        shift.RequiredHeadcount = Math.Max(1, request.RequiredHeadcount);
        shift.RequiredCertification = request.RequiredCertification;
        shift.Note = request.Note;

        var existing = shift.Assignments.Where(a => !a.IsDeleted).ToList();
        foreach (var gone in existing.Where(a => !request.StaffIds.Contains(a.StaffId)))
            gone.StampDeleted(userId);

        foreach (var staffId in request.StaffIds.Where(sid => !existing.Any(a => a.StaffId == sid)))
        {
            // A required certification is enforced rather than suggested — an uninsured lifeguard
            // on the rota is worse than an empty slot.
            if (!string.IsNullOrWhiteSpace(request.RequiredCertification))
            {
                var holds = await db.Certifications.ForTenant(tenant)
                    .AnyAsync(c => c.StaffId == staffId
                                && c.Name.ToLower().Contains(request.RequiredCertification.ToLower())
                                && (c.ExpiresOn == null || c.ExpiresOn > request.StartsAt));

                if (!holds)
                {
                    var person = await db.Staff.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == staffId);
                    throw new InvalidOperationException(
                        $"{person?.FirstName ?? "That person"} does not hold a valid {request.RequiredCertification}.");
                }
            }

            var clash = await db.ShiftAssignments.ForTenant(tenant)
                .AnyAsync(a => a.StaffId == staffId
                            && a.Shift!.StartsAt < request.EndsAt && a.Shift.EndsAt > request.StartsAt
                            && a.ShiftId != shift.Id);

            if (clash)
            {
                var person = await db.Staff.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == staffId);
                throw new InvalidOperationException($"{person?.FirstName ?? "That person"} is already on another shift then.");
            }

            db.ShiftAssignments.Add(new ShiftAssignment
            {
                ShiftId = shift.Id,
                StaffId = staffId,
                Status = ShiftStatus.Confirmed,
            }.StampNew(tenant, userId));
        }

        shift.AssignedHeadcount = request.StaffIds.Count;
        await db.SaveChangesAsync();

        var saved = await db.Shifts.ForTenant(tenant)
            .Include(s => s.Assignments.Where(a => !a.IsDeleted)).ThenInclude(a => a.Staff)
            .FirstAsync(s => s.Id == shift.Id);

        return FitnessMapper.ToDto(saved);
    }

    public async Task DeleteShiftAsync(Guid id, Guid userId)
    {
        var shift = await db.Shifts.ForTenant(tenant)
            .Include(s => s.Assignments.Where(a => !a.IsDeleted))
            .FirstOrDefaultAsync(s => s.Id == id)
            ?? throw new InvalidOperationException("Shift not found.");

        if (shift.Assignments.Any(a => a.ClockedInAt is not null))
            throw new InvalidOperationException("Somebody has already clocked in on this shift. It cannot be deleted.");

        foreach (var assignment in shift.Assignments) assignment.StampDeleted(userId);
        shift.StampDeleted(userId);

        await db.SaveChangesAsync();
    }

    public async Task<List<ShiftDto>> PublishRotaAsync(Guid clubId, DateTime from, DateTime to, Guid userId)
    {
        var now = DateTime.UtcNow;

        var shifts = await db.Shifts.ForTenant(tenant)
            .Where(s => s.ClubId == clubId && s.StartsAt >= from && s.StartsAt <= to && !s.IsPublished)
            .Include(s => s.Assignments.Where(a => !a.IsDeleted)).ThenInclude(a => a.Staff)
            .ToListAsync();

        foreach (var shift in shifts)
        {
            shift.IsPublished = true;
            shift.PublishedAt = now;
            shift.Status = shift.Assignments.Count == 0 ? ShiftStatus.Open : ShiftStatus.Published;
            shift.StampUpdated(userId);

            foreach (var assignment in shift.Assignments.Where(a => !a.IsDeleted))
            {
                db.MessageLog.Add(new MessageLog
                {
                    StaffId = assignment.StaffId,
                    ClubId = clubId,
                    Channel = MessageChannel.Push,
                    Status = MessageStatus.Queued,
                    Subject = "Your rota is out",
                    BodyPreview = $"{shift.Position ?? "Shift"} on {shift.StartsAt:ddd d MMM}, " +
                                  $"{shift.StartsAt:HH:mm}–{shift.EndsAt:HH:mm}.",
                    QueuedAt = now,
                }.StampNew(tenant, userId));
            }
        }

        await db.SaveChangesAsync();
        return [.. shifts.Select(FitnessMapper.ToDto)];
    }

    public async Task<ShiftSwapRequestDto> RequestSwapAsync(
        Guid assignmentId, Guid? offerToStaffId, string? reason, Guid userId)
    {
        var assignment = await db.ShiftAssignments.ForTenant(tenant)
            .Include(a => a.Shift)
            .Include(a => a.Staff)
            .FirstOrDefaultAsync(a => a.Id == assignmentId)
            ?? throw new InvalidOperationException("Shift assignment not found.");

        if (assignment.ClockedInAt is not null)
            throw new InvalidOperationException("That shift has already started.");

        var swap = new ShiftSwapRequest
        {
            ShiftAssignmentId = assignmentId,
            RequestedByStaffId = assignment.StaffId,
            OfferedToStaffId = offerToStaffId,
            RequestedAt = DateTime.UtcNow,
            Reason = reason,
        }.StampNew(tenant, userId);

        db.SwapRequests.Add(swap);

        assignment.Status = ShiftStatus.SwapRequested;
        assignment.StampUpdated(userId);

        await db.SaveChangesAsync();

        return await BuildSwapDtoAsync(swap, assignment);
    }

    public async Task<ShiftSwapRequestDto> RespondToSwapAsync(Guid swapId, bool accept, Guid staffId, Guid userId)
    {
        var now = DateTime.UtcNow;

        var swap = await db.SwapRequests.ForTenant(tenant)
            .Include(s => s.ShiftAssignment).ThenInclude(a => a!.Shift)
            .FirstOrDefaultAsync(s => s.Id == swapId)
            ?? throw new InvalidOperationException("Swap request not found.");

        var assignment = swap.ShiftAssignment
            ?? throw new InvalidOperationException("The shift behind that request no longer exists.");

        swap.RespondedAt = now;
        swap.StampUpdated(userId);

        if (!accept)
        {
            swap.IsCancelled = true;
            assignment.Status = ShiftStatus.Confirmed;
            await db.SaveChangesAsync();
            return await BuildSwapDtoAsync(swap, assignment);
        }

        var shift = assignment.Shift!;

        var clash = await db.ShiftAssignments.ForTenant(tenant)
            .AnyAsync(a => a.StaffId == staffId && a.Id != assignment.Id
                        && a.Shift!.StartsAt < shift.EndsAt && a.Shift.EndsAt > shift.StartsAt);

        if (clash) throw new InvalidOperationException("You are already on another shift at that time.");

        if (!string.IsNullOrWhiteSpace(shift.RequiredCertification))
        {
            var holds = await db.Certifications.ForTenant(tenant)
                .AnyAsync(c => c.StaffId == staffId
                            && c.Name.ToLower().Contains(shift.RequiredCertification.ToLower())
                            && (c.ExpiresOn == null || c.ExpiresOn > shift.StartsAt));

            if (!holds)
                throw new InvalidOperationException($"That shift needs a valid {shift.RequiredCertification}.");
        }

        swap.AcceptedByStaffId = staffId;
        swap.IsApproved = true;
        swap.ApprovedByUserId = userId;

        assignment.StaffId = staffId;
        assignment.Status = ShiftStatus.Confirmed;
        assignment.StampUpdated(userId);

        await db.SaveChangesAsync();
        return await BuildSwapDtoAsync(swap, assignment);
    }

    public async Task<List<ShiftSwapRequestDto>> GetSwapRequestsAsync(Guid clubId, bool openOnly)
    {
        var swaps = await db.SwapRequests.ForTenant(tenant)
            .Include(s => s.ShiftAssignment).ThenInclude(a => a!.Shift)
            .Where(s => s.ShiftAssignment!.Shift!.ClubId == clubId)
            .WhereIf(openOnly, s => !s.IsApproved && !s.IsCancelled)
            .OrderByDescending(s => s.RequestedAt)
            .ToListAsync();

        var results = new List<ShiftSwapRequestDto>();
        foreach (var swap in swaps)
            results.Add(await BuildSwapDtoAsync(swap, swap.ShiftAssignment!));

        return results;
    }

    // ── Time clock ───────────────────────────────────────────────────────────

    public async Task<TimeClockEntryDto> ClockInAsync(ClockDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var open = await db.TimeClock.ForTenant(tenant)
            .FirstOrDefaultAsync(t => t.StaffId == request.StaffId && t.ClockedOutAt == null);

        if (open is not null)
            throw new InvalidOperationException("You are already clocked in. Clock out first.");

        // Match to a rostered shift so lateness is measurable rather than anecdotal.
        var shift = await db.ShiftAssignments.ForTenant(tenant)
            .Include(a => a.Shift)
            .Where(a => a.StaffId == request.StaffId
                     && a.Shift!.StartsAt <= now.AddHours(2) && a.Shift.EndsAt >= now)
            .OrderBy(a => a.Shift!.StartsAt)
            .FirstOrDefaultAsync();

        var entry = new TimeClockEntry
        {
            StaffId = request.StaffId,
            ClubId = request.ClubId,
            ShiftAssignmentId = shift?.Id,
            ClockedInAt = now,
            Device = request.Device,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            GeofencePassed = true,
        }.StampNew(tenant, userId);

        db.TimeClock.Add(entry);

        if (shift?.Shift is not null)
        {
            shift.ClockedInAt = now;
            shift.Status = ShiftStatus.Confirmed;

            var lateBy = (int)(now - shift.Shift.StartsAt).TotalMinutes;
            if (lateBy > 5)
            {
                shift.WasLate = true;
                shift.LateMinutes = lateBy;
            }

            shift.StampUpdated(userId);
        }

        await db.SaveChangesAsync();

        var saved = await db.TimeClock.ForTenant(tenant)
            .Include(t => t.Staff)
            .FirstAsync(t => t.Id == entry.Id);

        return FitnessMapper.ToDto(saved);
    }

    public async Task<TimeClockEntryDto> ClockOutAsync(ClockDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var entry = await db.TimeClock.ForTenant(tenant)
            .Include(t => t.Staff)
            .FirstOrDefaultAsync(t => t.StaffId == request.StaffId && t.ClockedOutAt == null)
            ?? throw new InvalidOperationException("You are not clocked in.");

        entry.ClockedOutAt = now;
        entry.BreakMinutes = request.BreakMinutes ?? entry.BreakMinutes;
        entry.WorkedMinutes = Math.Max(0, (int)(now - entry.ClockedInAt).TotalMinutes - entry.BreakMinutes);
        entry.StampUpdated(userId);

        if (entry.ShiftAssignmentId is not null)
        {
            var assignment = await db.ShiftAssignments.ForTenant(tenant)
                .FirstOrDefaultAsync(a => a.Id == entry.ShiftAssignmentId);

            if (assignment is not null)
            {
                assignment.ClockedOutAt = now;
                assignment.Status = ShiftStatus.Completed;
                assignment.StampUpdated(userId);
            }
        }

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(entry);
    }

    public async Task<TimesheetDto> GetTimesheetAsync(Guid staffId, DateTime from, DateTime to)
    {
        var person = await db.Staff.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == staffId)
            ?? throw new InvalidOperationException("Staff member not found.");

        var entries = await db.TimeClock.ForTenant(tenant)
            .Where(t => t.StaffId == staffId && t.ClockedInAt >= from && t.ClockedInAt <= to)
            .Include(t => t.Staff)
            .OrderBy(t => t.ClockedInAt)
            .ToListAsync();

        var assignments = await db.ShiftAssignments.ForTenant(tenant)
            .Include(a => a.Shift)
            .Where(a => a.StaffId == staffId && a.Shift!.StartsAt >= from && a.Shift.StartsAt <= to)
            .ToListAsync();

        var rostered = assignments.Sum(a =>
            (decimal)(a.Shift!.EndsAt - a.Shift.StartsAt).TotalHours - a.Shift.BreakMinutes / 60m);

        var worked = entries.Sum(e => (e.WorkedMinutes ?? 0) / 60m);

        var dto = new TimesheetDto
        {
            StaffId = staffId,
            StaffName = $"{person.FirstName} {person.LastName}",
            ClubId = person.ClubId,
            PeriodStart = from,
            PeriodEnd = to,
            TotalHours = Math.Round(worked, 2),
            RosteredHours = Math.Round(rostered, 2),
            VarianceHours = Math.Round(worked - rostered, 2),
            LateCount = assignments.Count(a => a.WasLate),
            NoShowCount = assignments.Count(a => a.WasNoShow),
            IsApproved = entries.Count > 0 && entries.All(e => e.IsApproved),
            Entries = [.. entries.Select(FitnessMapper.ToDto)],
        };

        // Variance against the rostered shift, per entry, which is what a manager reviews.
        foreach (var entry in dto.Entries)
        {
            var assignment = assignments.FirstOrDefault(a => a.Id == entry.ShiftAssignmentId);
            if (assignment?.Shift is null) continue;

            var expected = (int)(assignment.Shift.EndsAt - assignment.Shift.StartsAt).TotalMinutes - assignment.Shift.BreakMinutes;
            entry.VarianceMinutes = (entry.WorkedMinutes ?? 0) - expected;
        }

        return dto;
    }

    public async Task<TimeClockEntryDto> AdjustEntryAsync(
        Guid entryId, DateTime? inAt, DateTime? outAt, int? breakMinutes, string note, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(note))
            throw new InvalidOperationException("Adjusting a timesheet needs a note — it is what people are paid on.");

        var entry = await db.TimeClock.ForTenant(tenant)
            .Include(t => t.Staff)
            .FirstOrDefaultAsync(t => t.Id == entryId)
            ?? throw new InvalidOperationException("Time entry not found.");

        // The original is preserved in the note, so an adjustment is auditable rather than invisible.
        var original = $"was {entry.ClockedInAt:HH:mm}–{entry.ClockedOutAt:HH:mm}, {entry.BreakMinutes}m break";

        if (inAt is not null) entry.ClockedInAt = inAt.Value;
        if (outAt is not null) entry.ClockedOutAt = outAt.Value;
        if (breakMinutes is not null) entry.BreakMinutes = breakMinutes.Value;

        if (entry.ClockedOutAt is not null)
            entry.WorkedMinutes = Math.Max(0,
                (int)(entry.ClockedOutAt.Value - entry.ClockedInAt).TotalMinutes - entry.BreakMinutes);

        entry.WasEdited = true;
        entry.EditNote = $"{note} ({original})";
        entry.StampUpdated(userId);

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(entry);
    }

    public async Task<int> ApproveTimesheetAsync(Guid staffId, DateTime from, DateTime to, Guid userId)
    {
        var entries = await db.TimeClock.ForTenant(tenant)
            .Where(t => t.StaffId == staffId && t.ClockedInAt >= from && t.ClockedInAt <= to
                     && t.ClockedOutAt != null && !t.IsApproved)
            .ToListAsync();

        foreach (var entry in entries)
        {
            entry.IsApproved = true;
            entry.ApprovedByUserId = userId;
            entry.StampUpdated(userId);
        }

        await db.SaveChangesAsync();
        return entries.Count;
    }

    // ── Commission ───────────────────────────────────────────────────────────

    public async Task<List<CommissionRuleDto>> GetCommissionRulesAsync(Guid? clubId, Guid? staffId)
    {
        var rules = await db.CommissionRules.ForTenant(tenant)
            .WhereIf(clubId is not null, r => r.ClubId == clubId || r.ClubId == null)
            .WhereIf(staffId is not null, r => r.StaffId == staffId || r.StaffId == null)
            .OrderBy(r => r.Priority).ThenBy(r => r.Name)
            .ToListAsync();

        var staffNames = await db.Staff.ForTenant(tenant)
            .Select(s => new { s.Id, Name = s.FirstName + " " + s.LastName })
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        var services = await db.Services.ForTenant(tenant)
            .Select(s => new { s.Id, s.Name }).ToDictionaryAsync(s => s.Id, s => s.Name);

        var classTypes = await db.ClassTypes.ForTenant(tenant)
            .Select(c => new { c.Id, c.Name }).ToDictionaryAsync(c => c.Id, c => c.Name);

        var plans = await db.Plans.ForTenant(tenant)
            .Select(p => new { p.Id, p.Name }).ToDictionaryAsync(p => p.Id, p => p.Name);

        return [.. rules.Select(r =>
        {
            var dto = FitnessMapper.ToDto(r);
            if (r.StaffId is not null) dto.StaffName = staffNames.GetValueOrDefault(r.StaffId.Value);
            if (r.ServiceId is not null) dto.ServiceName = services.GetValueOrDefault(r.ServiceId.Value);
            if (r.ClassTypeId is not null) dto.ClassTypeName = classTypes.GetValueOrDefault(r.ClassTypeId.Value);
            if (r.PlanId is not null) dto.PlanName = plans.GetValueOrDefault(r.PlanId.Value);
            return dto;
        })];
    }

    public async Task<CommissionRuleDto> SaveCommissionRuleAsync(Guid? id, CommissionRuleDto request, Guid userId)
    {
        CommissionRule rule;
        if (id is null)
        {
            rule = new CommissionRule().StampNew(tenant, userId);
            db.CommissionRules.Add(rule);
        }
        else
        {
            rule = await db.CommissionRules.ForTenant(tenant).FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new InvalidOperationException("Commission rule not found.");
            rule.StampUpdated(userId);
        }

        rule.Name = request.Name;
        rule.ClubId = request.ClubId;
        rule.StaffId = request.StaffId;
        rule.AppliesToRole = request.AppliesToRole;
        rule.Basis = request.Basis;
        rule.RatePerUnit = request.RatePerUnit;
        rule.Percentage = request.Percentage;
        rule.Threshold = request.Threshold;
        rule.AcceleratedRate = request.AcceleratedRate;
        rule.PeriodCap = request.PeriodCap;
        rule.ServiceId = request.ServiceId;
        rule.ClassTypeId = request.ClassTypeId;
        rule.PlanId = request.PlanId;
        rule.EffectiveFrom = request.EffectiveFrom;
        rule.EffectiveTo = request.EffectiveTo;
        rule.Priority = request.Priority;
        rule.IsActive = request.IsActive;

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(rule);
    }

    /// <summary>
    /// Accrues commission at the moment the earning event happens.
    ///
    /// Several rules can pay on one event — a per-session rate and a percentage of value, say —
    /// so every matching rule accrues its own line, and the period cap is honoured per rule
    /// rather than in aggregate.
    /// </summary>
    public async Task<List<CommissionAccrualDto>> AccrueAsync(
        Guid staffId, CommissionBasis basis, decimal baseValue, decimal quantity,
        Guid? sourceId, string? sourceType, Guid? memberId, string? narrative, Guid userId)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var person = await db.Staff.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == staffId);
        if (person is null) return [];

        var rules = await db.CommissionRules.ForTenant(tenant)
            .Where(r => r.IsActive && r.Basis == basis
                     && (r.StaffId == staffId || (r.StaffId == null && (r.AppliesToRole == null || r.AppliesToRole == person.RoleKind)))
                     && (r.ClubId == null || r.ClubId == person.ClubId)
                     && r.EffectiveFrom <= now && (r.EffectiveTo == null || r.EffectiveTo >= now))
            .OrderBy(r => r.Priority)
            .ToListAsync();

        var accruals = new List<CommissionAccrual>();

        foreach (var rule in rules)
        {
            // Already earned under this rule this period, for the threshold and the cap.
            var earnedThisPeriod = await db.CommissionAccruals.ForTenant(tenant)
                .Where(a => a.StaffId == staffId && a.CommissionRuleId == rule.Id
                         && a.EarnedOn >= monthStart && !a.IsReversed)
                .SumAsync(a => (decimal?)a.Amount) ?? 0m;

            var unitsThisPeriod = await db.CommissionAccruals.ForTenant(tenant)
                .Where(a => a.StaffId == staffId && a.CommissionRuleId == rule.Id
                         && a.EarnedOn >= monthStart && !a.IsReversed)
                .SumAsync(a => (decimal?)a.Quantity) ?? 0m;

            decimal amount;

            if (rule.Percentage > 0)
            {
                amount = Math.Round(baseValue * rule.Percentage / 100m, 2);
            }
            else
            {
                // A tiered rule pays the base rate up to the threshold and the accelerated rate
                // beyond it — the point of a tier is that it changes at the boundary, not after.
                var rate = rule.AcceleratedRate > 0 && unitsThisPeriod >= rule.Threshold
                    ? rule.AcceleratedRate
                    : rule.RatePerUnit;

                amount = Math.Round(rate * quantity, 2);
            }

            if (rule.Threshold > 0 && rule.Percentage > 0 && earnedThisPeriod + amount <= rule.Threshold)
                continue;

            if (rule.PeriodCap > 0)
            {
                var headroom = Math.Max(0, rule.PeriodCap - earnedThisPeriod);
                amount = Math.Min(amount, headroom);
                if (amount <= 0) continue;
            }

            if (amount <= 0) continue;

            var accrual = new CommissionAccrual
            {
                StaffId = staffId,
                ClubId = person.ClubId,
                CommissionRuleId = rule.Id,
                Basis = basis,
                EarnedOn = now,
                Amount = amount,
                BaseValue = baseValue,
                Quantity = quantity,
                SourceEntityId = sourceId,
                SourceEntityType = sourceType,
                MemberId = memberId,
                Narrative = narrative ?? rule.Name,
            }.StampNew(tenant, userId);

            db.CommissionAccruals.Add(accrual);
            accruals.Add(accrual);
        }

        await db.SaveChangesAsync();
        return [.. accruals.Select(FitnessMapper.ToDto)];
    }

    /// <summary>
    /// Builds the period's statements from the accruals already recorded.
    ///
    /// A preview computes without writing, because a commission statement is a number somebody is
    /// going to be paid and a manager should see it before it exists.
    /// </summary>
    public async Task<List<CommissionStatementDto>> GenerateStatementsAsync(GenerateCommissionDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var staffQuery = db.Staff.ForTenant(tenant)
            .Where(s => s.ClubId == request.ClubId && s.IsActive);

        if (request.StaffIds.Count > 0)
            staffQuery = staffQuery.Where(s => request.StaffIds.Contains(s.Id));

        var people = await staffQuery.ToListAsync();
        var statements = new List<CommissionStatementDto>();

        foreach (var person in people)
        {
            var accruals = await db.CommissionAccruals.ForTenant(tenant)
                .Where(a => a.StaffId == person.Id && !a.IsReversed
                         && a.EarnedOn >= request.PeriodStart && a.EarnedOn <= request.PeriodEnd
                         && a.CommissionStatementId == null)
                .Include(a => a.Staff)
                .ToListAsync();

            if (accruals.Count == 0) continue;

            var existing = await db.CommissionStatements.ForTenant(tenant)
                .FirstOrDefaultAsync(s => s.StaffId == person.Id
                                       && s.PeriodStart == request.PeriodStart.Date
                                       && s.Status != CommissionStatementStatus.Rejected);

            if (existing is not null && !request.PreviewOnly)
                throw new InvalidOperationException(
                    $"{person.FirstName} already has a statement for that period ({existing.StatementNumber}).");

            var statement = new CommissionStatement
            {
                StatementNumber = request.PreviewOnly ? "PREVIEW" : await numbering.NextStatementNumberAsync(now),
                StaffId = person.Id,
                ClubId = person.ClubId,
                PeriodStart = request.PeriodStart.Date,
                PeriodEnd = request.PeriodEnd.Date,
                Status = CommissionStatementStatus.Draft,
                SessionCommission = accruals.Where(a => a.Basis is CommissionBasis.PerSessionDelivered
                                                          or CommissionBasis.PercentOfSessionValue).Sum(a => a.Amount),
                ClassCommission = accruals.Where(a => a.Basis is CommissionBasis.PerClassTaught
                                                        or CommissionBasis.PerClassHead).Sum(a => a.Amount),
                SalesCommission = accruals.Where(a => a.Basis is CommissionBasis.PercentOfMembershipSold
                                                        or CommissionBasis.PercentOfPackageSold).Sum(a => a.Amount),
                RetailCommission = accruals.Where(a => a.Basis == CommissionBasis.PercentOfRetailSold).Sum(a => a.Amount),
                Bonus = accruals.Where(a => a.Basis is CommissionBasis.TargetBonus
                                              or CommissionBasis.FlatPerPeriod).Sum(a => a.Amount),
                SessionsDelivered = accruals.Count(a => a.Basis == CommissionBasis.PerSessionDelivered),
                ClassesTaught = accruals.Count(a => a.Basis == CommissionBasis.PerClassTaught),
                MembershipsSold = accruals.Count(a => a.Basis == CommissionBasis.PercentOfMembershipSold),
                PackagesSold = accruals.Count(a => a.Basis == CommissionBasis.PercentOfPackageSold),
            }.StampNew(tenant, userId);

            statement.Total = statement.SessionCommission + statement.ClassCommission
                            + statement.SalesCommission + statement.RetailCommission + statement.Bonus;

            var dto = FitnessMapper.ToDto(statement);
            dto.StaffName = $"{person.FirstName} {person.LastName}";
            dto.PhotoUrl = person.PhotoUrl;
            dto.Accruals = [.. accruals.Select(FitnessMapper.ToDto)];

            if (!request.PreviewOnly)
            {
                db.CommissionStatements.Add(statement);
                foreach (var accrual in accruals) accrual.CommissionStatementId = statement.Id;
            }

            statements.Add(dto);
        }

        if (!request.PreviewOnly) await db.SaveChangesAsync();
        return statements;
    }

    public async Task<CommissionStatementDto?> GetStatementAsync(Guid statementId)
    {
        var statement = await db.CommissionStatements.ForTenant(tenant)
            .Include(s => s.Staff)
            .FirstOrDefaultAsync(s => s.Id == statementId);

        if (statement is null) return null;

        var dto = FitnessMapper.ToDto(statement);

        dto.ClubName = await db.Clubs.ForTenant(tenant)
            .Where(c => c.Id == statement.ClubId).Select(c => c.Name).FirstOrDefaultAsync();

        var accruals = await db.CommissionAccruals.ForTenant(tenant)
            .Where(a => a.CommissionStatementId == statementId)
            .Include(a => a.Staff)
            .OrderBy(a => a.EarnedOn)
            .ToListAsync();

        var memberIds = accruals.Where(a => a.MemberId is not null).Select(a => a.MemberId!.Value).Distinct().ToList();
        var names = await db.Members.ForTenant(tenant)
            .Where(m => memberIds.Contains(m.Id))
            .Select(m => new { m.Id, Name = m.FirstName + " " + m.LastName })
            .ToDictionaryAsync(m => m.Id, m => m.Name);

        dto.Accruals = [.. accruals.Select(a =>
        {
            var line = FitnessMapper.ToDto(a);
            if (a.MemberId is not null) line.MemberName = names.GetValueOrDefault(a.MemberId.Value);
            return line;
        })];

        return dto;
    }

    public async Task<PaginatedResponse<CommissionStatementDto>> ListStatementsAsync(
        Guid? clubId, Guid? staffId, CommissionStatementStatus? status, PaginationParams pagination)
    {
        var query = db.CommissionStatements.ForTenant(tenant)
            .Include(s => s.Staff)
            .WhereIf(clubId is not null, s => s.ClubId == clubId)
            .WhereIf(staffId is not null, s => s.StaffId == staffId)
            .WhereIf(status is not null, s => s.Status == status);

        var total = await query.CountAsync();

        var page = await query
            .OrderByDescending(s => s.PeriodStart).ThenBy(s => s.Staff!.LastName)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        return PaginatedResponse<CommissionStatementDto>.Ok(
                   [.. page.Select(FitnessMapper.ToDto)],
                   total,
                   pagination.PageNumber,
                   pagination.PageSize);
    }

    public async Task<CommissionStatementDto> ApproveStatementAsync(ApproveStatementDto request, Guid userId)
    {
        var statement = await db.CommissionStatements.ForTenant(tenant)
            .Include(s => s.Staff)
            .FirstOrDefaultAsync(s => s.Id == request.StatementId)
            ?? throw new InvalidOperationException("Statement not found.");

        if (statement.Status == CommissionStatementStatus.Exported)
            throw new InvalidOperationException("This statement has already gone to payroll.");

        if (request.Approve)
        {
            if (request.AdjustmentAmount is not null && request.AdjustmentAmount != 0)
            {
                if (string.IsNullOrWhiteSpace(request.AdjustmentReason))
                    throw new InvalidOperationException("An adjustment needs a reason.");

                statement.Adjustments += request.AdjustmentAmount.Value;
                statement.Total += request.AdjustmentAmount.Value;
            }

            statement.Status = CommissionStatementStatus.Approved;
            statement.ApprovedAt = DateTime.UtcNow;
            statement.ApprovedByUserId = userId;
        }
        else
        {
            statement.Status = CommissionStatementStatus.Rejected;
            statement.RejectionNote = request.Note;

            // Rejecting releases the accruals so a corrected statement can pick them up.
            var accruals = await db.CommissionAccruals.ForTenant(tenant)
                .Where(a => a.CommissionStatementId == statement.Id)
                .ToListAsync();

            foreach (var accrual in accruals) accrual.CommissionStatementId = null;
        }

        statement.StampUpdated(userId);
        await db.SaveChangesAsync();

        return (await GetStatementAsync(statement.Id))!;
    }

    public async Task<CommissionStatementDto> ExportStatementAsync(Guid statementId, Guid userId)
    {
        var statement = await db.CommissionStatements.ForTenant(tenant)
            .Include(s => s.Staff)
            .FirstOrDefaultAsync(s => s.Id == statementId)
            ?? throw new InvalidOperationException("Statement not found.");

        if (statement.Status != CommissionStatementStatus.Approved)
            throw new InvalidOperationException("Only an approved statement can go to payroll.");

        statement.Status = CommissionStatementStatus.Exported;
        statement.ExportedAt = DateTime.UtcNow;

        // HR receives one posted total per person per period, not a hundred accrual lines.
        statement.PayrollReference = $"FIT-{statement.PeriodStart:yyyyMM}-{statement.StatementNumber}";
        statement.StampUpdated(userId);

        await db.SaveChangesAsync();
        return (await GetStatementAsync(statementId))!;
    }

    // ═══ Internals ═══════════════════════════════════════════════════════════

    private async Task<ShiftSwapRequestDto> BuildSwapDtoAsync(ShiftSwapRequest swap, ShiftAssignment assignment)
    {
        var ids = new[] { swap.RequestedByStaffId, swap.OfferedToStaffId ?? Guid.Empty, swap.AcceptedByStaffId ?? Guid.Empty }
            .Where(g => g != Guid.Empty).Distinct().ToList();

        var names = await db.Staff.ForTenant(tenant)
            .Where(s => ids.Contains(s.Id))
            .Select(s => new { s.Id, Name = s.FirstName + " " + s.LastName })
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        var shift = assignment.Shift
            ?? await db.Shifts.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == assignment.ShiftId);

        return new ShiftSwapRequestDto
        {
            Id = swap.Id,
            ShiftAssignmentId = swap.ShiftAssignmentId,
            ShiftId = assignment.ShiftId,
            ShiftStartsAt = shift?.StartsAt ?? default,
            ShiftEndsAt = shift?.EndsAt ?? default,
            ShiftPosition = shift?.Position,
            RequestedByStaffId = swap.RequestedByStaffId,
            RequestedByName = names.GetValueOrDefault(swap.RequestedByStaffId),
            OfferedToStaffId = swap.OfferedToStaffId,
            OfferedToName = swap.OfferedToStaffId is null ? null : names.GetValueOrDefault(swap.OfferedToStaffId.Value),
            AcceptedByStaffId = swap.AcceptedByStaffId,
            AcceptedByName = swap.AcceptedByStaffId is null ? null : names.GetValueOrDefault(swap.AcceptedByStaffId.Value),
            RequestedAt = swap.RequestedAt,
            RespondedAt = swap.RespondedAt,
            Reason = swap.Reason,
            IsApproved = swap.IsApproved,
            IsCancelled = swap.IsCancelled,
            IsOpenToAll = swap.OfferedToStaffId is null,
        };
    }

    private static List<Guid> ParseGuids(string? csv)
        => string.IsNullOrWhiteSpace(csv)
            ? []
            : [.. csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
                .Where(g => g != Guid.Empty)];
}
