using System.Diagnostics;
using Fitness.Application.DTOs;
using Fitness.Application.Services.Interfaces;
using Fitness.Domain.Entities;
using Fitness.Domain.Enums;
using Fitness.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;

namespace Fitness.Infrastructure.Services;

/// <summary>
/// The door.
///
/// This is the highest-traffic path in the product and the one with the least forgiving budget:
/// a member is standing in a turnstile, and the answer has to arrive before they notice they are
/// waiting. Three consequences run through the whole file:
///
/// 1. **The decision is a small number of indexed reads.** Everything expensive — does this
///    person owe money, is their waiver current, are they frozen — was precomputed into
///    <see cref="MemberAlert"/> rows by <see cref="MemberService.RefreshAlertsAsync"/>. The door
///    reads those rather than re-deriving them.
/// 2. **Every refusal is a sentence the member can act on.** "Access denied" sends someone to
///    reception with no idea why. "Your membership is frozen until 3 March" sends them there
///    knowing what to ask for.
/// 3. **It works when the network does not.** Controllers hold a cached entitlement list and
///    replay what they buffered. A gym whose door stops working is a gym with a queue in the
///    car park.
/// </summary>
public class AccessService(
    FitnessDbContext db,
    IFitnessTenant tenant,
    FitnessNumbering numbering,
    IMemberService members,
    IBillingService billing,
    IComplianceService compliance) : IAccessService
{
    /// <summary>
    /// Evaluates a credential against a door.
    ///
    /// The order of checks is deliberate: cheapest and most decisive first, so the common case —
    /// a valid member at an open club — short-circuits after four reads. Identity, then the hard
    /// blocks, then the entitlement, then the counting rules.
    /// </summary>
    public async Task<AccessDecisionDto> DecideAsync(AccessRequestDto request)
    {
        var sw = Stopwatch.StartNew();
        var now = request.OccurredAt ?? DateTime.UtcNow;

        var club = await db.Clubs.ForTenant(tenant)
            .Include(c => c.Schedules.Where(s => !s.IsDeleted))
            .FirstOrDefaultAsync(c => c.Id == request.ClubId)
            ?? throw new InvalidOperationException("Club not found.");

        Door? door = request.DoorId is null
            ? null
            : await db.Doors.ForTenant(tenant).FirstOrDefaultAsync(d => d.Id == request.DoorId);

        // ── 1. Who is this? ──────────────────────────────────────────────────

        var credential = await db.Credentials.ForTenant(tenant)
            .Include(c => c.Member)
            .FirstOrDefaultAsync(c => c.Identifier == request.CredentialIdentifier);

        if (credential is null)
        {
            // A day pass or a temporary trial credential is not a member credential.
            var pass = await db.DayPasses.ForTenant(tenant)
                .FirstOrDefaultAsync(p => p.ClubId == club.Id
                                       && p.PassNumber == request.CredentialIdentifier
                                       && p.ValidFrom <= now && p.ValidTo >= now);

            if (pass is not null) return await DecideDayPassAsync(pass, club, door, request, now, sw);

            return await RefuseAsync(request, club, door, null, null,
                AccessDenialReason.CredentialUnknown,
                "That card is not recognised. Please see reception.", now, sw);
        }

        if (credential.Status != CredentialStatus.Active)
        {
            return await RefuseAsync(request, club, door, credential.MemberId, credential.Id,
                AccessDenialReason.CredentialInactive,
                credential.Status == CredentialStatus.Lost
                    ? "This card was reported lost. Reception can issue a replacement."
                    : "This card is no longer active. Please see reception.", now, sw);
        }

        if (credential.ExpiresOn is not null && credential.ExpiresOn < now)
        {
            return await RefuseAsync(request, club, door, credential.MemberId, credential.Id,
                AccessDenialReason.CredentialInactive,
                "This card has expired. Reception can issue a new one.", now, sw);
        }

        var member = credential.Member
            ?? throw new InvalidOperationException("Credential has no member.");

        // ── 2. Hard blocks, read from the precomputed alerts ─────────────────

        var alerts = await db.MemberAlerts.ForTenant(tenant)
            .Where(a => a.MemberId == member.Id && (a.ExpiresOn == null || a.ExpiresOn > now))
            .ToListAsync();

        var blocker = alerts.FirstOrDefault(a => a.BlocksAccess);
        if (blocker is not null)
        {
            var reason = blocker.Kind switch
            {
                MemberAlertKind.Banned => AccessDenialReason.Banned,
                MemberAlertKind.OutstandingBalance => AccessDenialReason.OutstandingBalance,
                MemberAlertKind.WaiverMissing or MemberAlertKind.WaiverExpired => AccessDenialReason.WaiverNotSigned,
                MemberAlertKind.MedicalClearanceRequired => AccessDenialReason.MedicalClearanceRequired,
                _ => AccessDenialReason.MembershipFrozen,
            };

            var decision = await RefuseAsync(request, club, door, member.Id, credential.Id,
                reason, blocker.Message, now, sw);

            decision.ResolutionAction = blocker.ActionLabel;
            decision.ResolutionRoute = blocker.ActionRoute;
            decision.AmountDue = reason == AccessDenialReason.OutstandingBalance ? member.AccountBalance : null;
            return decision;
        }

        // ── 3. Is the membership live? ───────────────────────────────────────

        var statusRefusal = member.Status switch
        {
            MemberStatus.Cancelled => (AccessDenialReason.MembershipCancelled,
                "This membership has been cancelled. Reception can help you rejoin."),
            MemberStatus.Expired => (AccessDenialReason.MembershipExpired,
                "This membership has expired. Reception can renew it."),
            MemberStatus.Suspended => (AccessDenialReason.MembershipSuspended,
                "This membership is suspended. Please see reception."),
            MemberStatus.Frozen => (AccessDenialReason.MembershipFrozen,
                "This membership is frozen. Reception can restart it."),
            MemberStatus.Lead => (AccessDenialReason.NoActiveMembership,
                "There is no active membership on this card. Please see reception."),
            _ => (AccessDenialReason.None, string.Empty),
        };

        if (statusRefusal.Item1 != AccessDenialReason.None)
        {
            return await RefuseAsync(request, club, door, member.Id, credential.Id,
                statusRefusal.Item1, statusRefusal.Item2, now, sw);
        }

        // ── 4. Is the club open? ─────────────────────────────────────────────

        if (!ClubService.IsOpenAt(club, now))
        {
            var closure = await db.ClubClosures.ForTenant(tenant)
                .FirstOrDefaultAsync(c => c.ClubId == club.Id && c.BlocksAccess
                                       && c.StartsOn <= now.Date && c.EndsOn >= now.Date);

            return await RefuseAsync(request, club, door, member.Id, credential.Id,
                AccessDenialReason.ClubClosed,
                closure is not null
                    ? $"The club is closed — {closure.Reason}."
                    : "The club is closed right now.", now, sw);
        }

        // ── 5. Does the plan cover this club, this area, at this time? ────────

        var agreement = await db.Agreements.ForTenant(tenant)
            .Include(a => a.Plan).ThenInclude(p => p!.Entitlements).ThenInclude(e => e.TimeBands)
            .Where(a => a.MemberId == member.Id
                     && (a.Status == AgreementStatus.Active || a.Status == AgreementStatus.NoticeGiven))
            .OrderByDescending(a => a.StartsOn)
            .FirstOrDefaultAsync();

        if (agreement is null)
        {
            return await RefuseAsync(request, club, door, member.Id, credential.Id,
                AccessDenialReason.NoActiveMembership,
                "There is no active membership on this card. Please see reception.", now, sw);
        }

        var plan = agreement.Plan;

        if (plan is not null && plan.RestrictedToClubId is not null && plan.RestrictedToClubId != club.Id)
        {
            return await RefuseAsync(request, club, door, member.Id, credential.Id,
                AccessDenialReason.ClubNotPermitted,
                "This membership is for a different club. Reception can arrange a visit.", now, sw);
        }

        var crossClub = member.HomeClubId != club.Id;
        if (crossClub && plan is not null && !plan.AllowsCrossClubAccess)
        {
            return await RefuseAsync(request, club, door, member.Id, credential.Id,
                AccessDenialReason.ClubNotPermitted,
                "This membership covers your home club only. Reception can arrange a visit.", now, sw);
        }

        // Time bands: a plan with none is unrestricted; a plan with some must match one of them.
        var clubEntitlement = plan?.Entitlements
            .FirstOrDefault(e => e.Kind == EntitlementKind.ClubAccess && !e.IsDeleted);

        var bands = clubEntitlement?.TimeBands.Where(b => !b.IsDeleted).ToList() ?? [];

        if (bands.Count > 0 && !bands.Any(b =>
                FitnessQueryExtensions.CoversDay(b.DaysOfWeekMask, now.DayOfWeek) &&
                FitnessQueryExtensions.WithinWindow(now.TimeOfDay, b.StartsAt, b.EndsAt)))
        {
            var next = DescribeNextBand(bands, now);
            return await RefuseAsync(request, club, door, member.Id, credential.Id,
                AccessDenialReason.OutsideAccessHours,
                $"Your membership covers off-peak hours. {next}", now, sw);
        }

        // ── 6. Area-level rules for a door that leads somewhere specific ─────

        if (door?.AreaId is not null)
        {
            var area = await db.Areas.ForTenant(tenant).FirstOrDefaultAsync(a => a.Id == door.AreaId);

            if (area is not null)
            {
                if (area.IsOutOfService)
                {
                    return await RefuseAsync(request, club, door, member.Id, credential.Id,
                        AccessDenialReason.AreaNotPermitted,
                        $"{area.Name} is closed — {area.OutOfServiceNote ?? "out of service"}.", now, sw);
                }

                if (area.RequiresEntitlement && plan is not null &&
                    !plan.Entitlements.Any(e => !e.IsDeleted
                                             && e.Kind == EntitlementKind.AreaAccess
                                             && (e.TargetId == area.Id || e.TargetId is null)))
                {
                    return await RefuseAsync(request, club, door, member.Id, credential.Id,
                        AccessDenialReason.AreaNotPermitted,
                        $"{area.Name} is not included in your membership. Reception can add it.", now, sw);
                }

                var age = FitnessMapper.AgeOn(member.DateOfBirth, now);
                if (area.MinimumAge is not null && age is not null && age < area.MinimumAge)
                {
                    return await RefuseAsync(request, club, door, member.Id, credential.Id,
                        AccessDenialReason.UnderAge,
                        $"{area.Name} is for members aged {area.MinimumAge} and over.", now, sw);
                }
            }
        }

        // ── 7. A door that only opens for a booked class ─────────────────────

        Guid? bookingId = null;
        if (door?.RequiresClassBooking == true && request.Direction != ReaderDirection.Out)
        {
            var window = now.AddMinutes(door.ClassBookingWindowMinutes);

            var booking = await db.ClassBookings.ForTenant(tenant)
                .Include(k => k.ClassOccurrence).ThenInclude(o => o!.ClassType)
                .Where(k => k.MemberId == member.Id
                         && k.Status == BookingStatus.Booked
                         && k.ClassOccurrence!.StartsAt <= window
                         && k.ClassOccurrence.EndsAt >= now)
                .FirstOrDefaultAsync();

            if (booking is null)
            {
                return await RefuseAsync(request, club, door, member.Id, credential.Id,
                    AccessDenialReason.NoClassBooked,
                    "This door opens for booked classes only. Nothing is booked for you right now.", now, sw);
            }

            bookingId = booking.Id;
        }

        // ── 8. Guardian requirement for a junior ─────────────────────────────

        var memberAge = FitnessMapper.AgeOn(member.DateOfBirth, now);
        if (memberAge is not null && memberAge < club.GuardianRequiredBelowAge && request.Direction != ReaderDirection.Out)
        {
            var guardianInside = await IsGuardianInClubAsync(member, club.Id, now);
            if (!guardianInside)
            {
                return await RefuseAsync(request, club, door, member.Id, credential.Id,
                    AccessDenialReason.GuardianRequired,
                    $"Members under {club.GuardianRequiredBelowAge} need a parent or guardian in the club.", now, sw);
            }
        }

        // ── 9. Anti-passback ─────────────────────────────────────────────────

        var mode = door?.AntiPassbackOverride ?? club.AntiPassback;
        var openVisit = await db.CheckIns.ForTenant(tenant)
            .Where(c => c.MemberId == member.Id && c.ClubId == club.Id && c.CheckedOutAt == null)
            .OrderByDescending(c => c.CheckedInAt)
            .FirstOrDefaultAsync();

        var passbackViolation = request.Direction != ReaderDirection.Out
            && openVisit is not null
            && mode switch
            {
                AntiPassbackMode.Hard => true,
                AntiPassbackMode.Timed => openVisit.CheckedInAt.AddMinutes(club.AntiPassbackMinutes) > now,
                _ => false,
            };

        if (passbackViolation && mode == AntiPassbackMode.Hard)
        {
            return await RefuseAsync(request, club, door, member.Id, credential.Id,
                AccessDenialReason.AntiPassback,
                "This card is already inside. Please use the exit reader on the way out.", now, sw);
        }

        // ── 10. Occupancy ────────────────────────────────────────────────────

        if (club.HardCapacity is not null && club.CurrentOccupancy >= club.HardCapacity
            && request.Direction != ReaderDirection.Out)
        {
            return await RefuseAsync(request, club, door, member.Id, credential.Id,
                AccessDenialReason.OccupancyFull,
                "The club is at capacity right now. Please wait a few minutes.", now, sw);
        }

        // ── 11. Visit allowance ──────────────────────────────────────────────

        VisitAllowanceUsage? allowance = null;
        if (plan is not null && plan.VisitsPerPeriod > 0 && request.Direction != ReaderDirection.Out)
        {
            allowance = await GetOrCreateAllowanceAsync(member.Id, agreement.Id, plan, now);

            if (allowance.Used >= allowance.Allowance)
            {
                if (!(clubEntitlement?.AllowOverage ?? false))
                {
                    return await RefuseAsync(request, club, door, member.Id, credential.Id,
                        AccessDenialReason.VisitAllowanceExhausted,
                        $"You have used all {allowance.Allowance} visits for this period. " +
                        $"It resets on {allowance.PeriodEnd:d MMM}.", now, sw);
                }
            }
        }

        // ── Granted ──────────────────────────────────────────────────────────

        return request.Direction == ReaderDirection.Out
            ? await GrantExitAsync(member, club, door, credential, request, openVisit, now, sw)
            : await GrantEntryAsync(member, club, door, credential, agreement, allowance, bookingId,
                                    crossClub, passbackViolation, alerts, request, now, sw);
    }

    /// <summary>
    /// Check-in from the desk, where a person has already identified the member.
    ///
    /// Runs the same engine rather than a softer parallel path — the whole point of one decision
    /// function is that the desk and the turnstile cannot disagree. Staff can override a refusal,
    /// and that override is always recorded with a reason.
    /// </summary>
    public async Task<AccessDecisionDto> ManualCheckInAsync(ManualCheckInDto request, Guid userId)
    {
        var credential = await db.Credentials.ForTenant(tenant)
            .Where(c => c.MemberId == request.MemberId && c.Status == CredentialStatus.Active)
            .OrderByDescending(c => c.IssuedOn)
            .FirstOrDefaultAsync();

        var decision = await DecideAsync(new AccessRequestDto
        {
            ClubId = request.ClubId,
            CredentialIdentifier = credential?.Identifier ?? string.Empty,
            Method = CredentialType.ManualLookup,
            Direction = ReaderDirection.In,
        });

        if (decision.Decision == AccessDecision.Granted || !request.OverrideDenial)
        {
            if (decision.Decision != AccessDecision.Granted && !request.OverrideDenial)
                return decision;
        }

        if (decision.Decision != AccessDecision.Granted && request.OverrideDenial)
        {
            if (string.IsNullOrWhiteSpace(request.OverrideReason))
                throw new InvalidOperationException("An override needs a reason — it is the only record of why entry was allowed.");

            var member = await db.Members.ForTenant(tenant)
                .FirstOrDefaultAsync(m => m.Id == request.MemberId)
                ?? throw new InvalidOperationException("Member not found.");

            var club = await db.Clubs.ForTenant(tenant).FirstAsync(c => c.Id == request.ClubId);

            var checkIn = await RecordCheckInAsync(member, club, null, credential,
                CredentialType.ManualLookup, request.ClassBookingId, request.AppointmentId,
                null, null, DateTime.UtcNow, true, userId);

            db.AccessEvents.Add(new AccessEvent
            {
                ClubId = club.Id,
                MemberId = member.Id,
                CredentialId = credential?.Id,
                Method = CredentialType.ManualLookup,
                Decision = AccessDecision.ManualOverride,
                DenialReason = decision.DenialReason,
                DecisionMessage = decision.Message,
                Direction = ReaderDirection.In,
                OverriddenByStaffId = userId,
                OverrideReason = request.OverrideReason,
                CheckInId = checkIn.Id,
                OccurredAt = DateTime.UtcNow,
            }.StampNew(tenant, userId));

            await db.SaveChangesAsync();

            await compliance.LogAsync("AccessOverride", nameof(CheckIn), checkIn.Id, member.Id,
                $"{decision.DenialReason}: {request.OverrideReason}", false, userId);

            decision.Decision = AccessDecision.ManualOverride;
            decision.CheckInId = checkIn.Id;
            decision.Message = $"Let in by staff — {request.OverrideReason}";
        }

        return decision;
    }

    public async Task<CheckInDto> CheckOutAsync(Guid checkInId, Guid userId)
    {
        var checkIn = await db.CheckIns.ForTenant(tenant)
            .Include(c => c.Member)
            .FirstOrDefaultAsync(c => c.Id == checkInId)
            ?? throw new InvalidOperationException("Check-in not found.");

        if (checkIn.CheckedOutAt is not null) return FitnessMapper.ToDto(checkIn);

        var now = DateTime.UtcNow;
        checkIn.CheckedOutAt = now;
        checkIn.DurationMinutes = (int)(now - checkIn.CheckedInAt).TotalMinutes;
        checkIn.StampUpdated(userId);

        await AdjustOccupancyAsync(checkIn.ClubId, checkIn.AreaId, -1);
        await db.SaveChangesAsync();

        return FitnessMapper.ToDto(checkIn);
    }

    /// <summary>
    /// Closes anyone the system still thinks is inside.
    ///
    /// Necessary because most people do not scan out, and an occupancy counter that only ever
    /// goes up is worse than none at all. Duration is estimated from the club's median visit
    /// rather than left null, so attendance reporting stays usable — and the row is flagged so
    /// nobody mistakes an estimate for a measurement.
    /// </summary>
    public async Task<int> SweepOpenCheckInsAsync()
    {
        var now = DateTime.UtcNow;
        var cutoff = now.AddHours(-6);

        var stale = await db.CheckIns.ForTenant(tenant)
            .Where(c => c.CheckedOutAt == null && c.CheckedInAt < cutoff)
            .ToListAsync();

        if (stale.Count == 0) return 0;

        var medianByClub = await db.CheckIns.ForTenant(tenant)
            .Where(c => c.DurationMinutes != null && !c.AutoClosed && c.CheckedInAt > now.AddDays(-30))
            .GroupBy(c => c.ClubId)
            .Select(g => new { ClubId = g.Key, Median = (int)g.Average(c => c.DurationMinutes!.Value) })
            .ToDictionaryAsync(x => x.ClubId, x => x.Median);

        foreach (var visit in stale)
        {
            var estimate = medianByClub.GetValueOrDefault(visit.ClubId, 65);
            visit.CheckedOutAt = visit.CheckedInAt.AddMinutes(estimate);
            visit.DurationMinutes = estimate;
            visit.AutoClosed = true;
        }

        // Recount rather than decrement: after a sweep, the only trustworthy occupancy is a fresh one.
        var clubIds = stale.Select(s => s.ClubId).Distinct().ToList();
        await db.SaveChangesAsync();

        foreach (var clubId in clubIds) await RecountOccupancyAsync(clubId);
        await db.SaveChangesAsync();

        return stale.Count;
    }

    // ── Occupancy ────────────────────────────────────────────────────────────

    public async Task<OccupancyDto> GetOccupancyAsync(Guid clubId)
    {
        var now = DateTime.UtcNow;

        var club = await db.Clubs.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == clubId)
            ?? throw new InvalidOperationException("Club not found.");

        var inClub = await db.CheckIns.ForTenant(tenant)
            .Where(c => c.ClubId == clubId && c.CheckedOutAt == null)
            .Include(c => c.Member)
            .OrderByDescending(c => c.CheckedInAt)
            .ToListAsync();

        var areas = await db.Areas.ForTenant(tenant)
            .Where(a => a.ClubId == clubId && a.IsActive)
            .OrderBy(a => a.DisplayOrder)
            .ToListAsync();

        return new OccupancyDto
        {
            ClubId = clubId,
            ClubName = club.Name,
            Occupancy = inClub.Count,
            SoftCapacity = club.SoftCapacity,
            HardCapacity = club.HardCapacity,
            PercentFull = FitnessMapper.Percent(inClub.Count, club.HardCapacity ?? club.SoftCapacity ?? 0),
            IsOverSoftCap = club.SoftCapacity is not null && inClub.Count >= club.SoftCapacity,
            IsAtHardCap = club.HardCapacity is not null && inClub.Count >= club.HardCapacity,
            AsAt = now,
            Areas = [.. areas.Select(a => new AreaOccupancyDto
            {
                AreaId = a.Id,
                AreaName = a.Name,
                Kind = a.Kind,
                Occupancy = a.CurrentOccupancy,
                Capacity = a.Capacity,
                PercentFull = FitnessMapper.Percent(a.CurrentOccupancy, a.Capacity ?? 0),
            })],
            InClub = [.. inClub.Select(FitnessMapper.ToDto)],
        };
    }

    public async Task<OccupancyTrendDto> GetOccupancyTrendAsync(Guid clubId, DateTime from, DateTime to)
    {
        var snapshots = await db.OccupancySnapshots.ForTenant(tenant)
            .Where(s => s.ClubId == clubId && s.AreaId == null && s.TakenAt >= from && s.TakenAt <= to)
            .OrderBy(s => s.TakenAt)
            .ToListAsync();

        var points = snapshots.Select(s => new OccupancyPointDto
        {
            At = s.TakenAt,
            DayOfWeek = (int)s.TakenAt.DayOfWeek,
            Hour = s.TakenAt.Hour,
            Occupancy = s.Occupancy,
            CheckIns = s.CheckInsInInterval,
        }).ToList();

        var peak = points.OrderByDescending(p => p.Occupancy).FirstOrDefault();

        return new OccupancyTrendDto
        {
            ClubId = clubId,
            From = from,
            To = to,
            Points = points,
            PeakOccupancy = peak?.Occupancy ?? 0,
            PeakAt = peak?.At,
        };
    }

    // ── Event log ────────────────────────────────────────────────────────────

    public async Task<PaginatedResponse<AccessEventDto>> GetEventsAsync(
        Guid? clubId, Guid? doorId, Guid? memberId, AccessDecision? decision,
        DateTime? from, DateTime? to, PaginationParams pagination)
    {
        var query = db.AccessEvents.ForTenant(tenant)
            .WhereIf(clubId is not null, e => e.ClubId == clubId)
            .WhereIf(doorId is not null, e => e.DoorId == doorId)
            .WhereIf(memberId is not null, e => e.MemberId == memberId)
            .WhereIf(decision is not null, e => e.Decision == decision)
            .WhereIf(from is not null, e => e.OccurredAt >= from)
            .WhereIf(to is not null, e => e.OccurredAt <= to);

        var total = await query.CountAsync();

        var page = await query
            .OrderByDescending(e => e.OccurredAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        var memberIds = page.Where(e => e.MemberId is not null).Select(e => e.MemberId!.Value).Distinct().ToList();
        var memberNames = await db.Members.ForTenant(tenant)
            .Where(m => memberIds.Contains(m.Id))
            .Select(m => new { m.Id, Name = m.FirstName + " " + m.LastName, m.MemberNumber })
            .ToListAsync();

        var doorNames = await db.Doors.ForTenant(tenant)
            .Select(d => new { d.Id, d.Name })
            .ToDictionaryAsync(d => d.Id, d => d.Name);

        var items = page.Select(e =>
        {
            var dto = FitnessMapper.ToDto(e);
            var m = memberNames.FirstOrDefault(x => x.Id == e.MemberId);
            dto.MemberName = m?.Name;
            dto.MemberNumber = m?.MemberNumber;
            if (e.DoorId is not null) dto.DoorName = doorNames.GetValueOrDefault(e.DoorId.Value);
            return dto;
        }).ToList();

        return PaginatedResponse<AccessEventDto>.Ok(
                   items,
                   total,
                   pagination.PageNumber,
                   pagination.PageSize);
    }

    public async Task<PaginatedResponse<CheckInDto>> GetCheckInsAsync(
        Guid? clubId, Guid? memberId, DateTime? from, DateTime? to, PaginationParams pagination)
    {
        var query = db.CheckIns.ForTenant(tenant)
            .Include(c => c.Member)
            .WhereIf(clubId is not null, c => c.ClubId == clubId)
            .WhereIf(memberId is not null, c => c.MemberId == memberId)
            .WhereIf(from is not null, c => c.CheckedInAt >= from)
            .WhereIf(to is not null, c => c.CheckedInAt <= to);

        var total = await query.CountAsync();

        var page = await query
            .OrderByDescending(c => c.CheckedInAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        return PaginatedResponse<CheckInDto>.Ok(
                   [.. page.Select(FitnessMapper.ToDto)],
                   total,
                   pagination.PageNumber,
                   pagination.PageSize);
    }

    // ── Hardware ─────────────────────────────────────────────────────────────

    public async Task<List<DoorDto>> GetDoorsAsync(Guid? clubId)
    {
        var today = DateTime.UtcNow.Date;

        var doors = await db.Doors.ForTenant(tenant)
            .WhereIf(clubId is not null, d => d.ClubId == clubId)
            .Include(d => d.Club)
            .Include(d => d.Area)
            .Include(d => d.Controller)
            .OrderBy(d => d.Name)
            .ToListAsync();

        var traffic = await db.AccessEvents.ForTenant(tenant)
            .Where(e => e.OccurredAt >= today && e.DoorId != null)
            .GroupBy(e => e.DoorId!.Value)
            .Select(g => new
            {
                DoorId = g.Key,
                Entries = g.Count(e => e.Decision == AccessDecision.Granted),
                Denials = g.Count(e => e.Decision == AccessDecision.Denied),
                Last = g.Max(e => e.OccurredAt),
            })
            .ToListAsync();

        return [.. doors.Select(d =>
        {
            var dto = FitnessMapper.ToDto(d);
            var t = traffic.FirstOrDefault(x => x.DoorId == d.Id);
            dto.EntriesToday = t?.Entries ?? 0;
            dto.DenialsToday = t?.Denials ?? 0;
            dto.LastEventAt = t?.Last;
            return dto;
        })];
    }

    public async Task<DoorDto> SaveDoorAsync(Guid? id, SaveDoorDto request, Guid userId)
    {
        Door door;
        if (id is null)
        {
            door = new Door { ClubId = request.ClubId }.StampNew(tenant, userId);
            db.Doors.Add(door);
        }
        else
        {
            door = await db.Doors.ForTenant(tenant).FirstOrDefaultAsync(d => d.Id == id)
                ?? throw new InvalidOperationException("Door not found.");
            door.StampUpdated(userId);
        }

        door.AreaId = request.AreaId;
        door.Name = request.Name;
        door.Direction = request.Direction;
        door.ControllerId = request.ControllerId;
        door.ReaderAddress = request.ReaderAddress;
        door.HardwareKind = request.HardwareKind;
        door.CountsOccupancy = request.CountsOccupancy;
        door.RequiresClassBooking = request.RequiresClassBooking;
        door.ClassBookingWindowMinutes = request.ClassBookingWindowMinutes;
        door.StaffOnly = request.StaffOnly;
        door.AntiPassbackOverride = request.AntiPassbackOverride;
        door.IsActive = request.IsActive;

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(door);
    }

    public async Task DeleteDoorAsync(Guid id, Guid userId)
    {
        var door = await db.Doors.ForTenant(tenant).FirstOrDefaultAsync(d => d.Id == id)
            ?? throw new InvalidOperationException("Door not found.");

        door.StampDeleted(userId);
        await db.SaveChangesAsync();
    }

    public async Task<List<AccessControllerDto>> GetControllersAsync(Guid? clubId)
    {
        var now = DateTime.UtcNow;

        var controllers = await db.Controllers.ForTenant(tenant)
            .WhereIf(clubId is not null, c => c.ClubId == clubId)
            .Include(c => c.Doors.Where(d => !d.IsDeleted))
            .OrderBy(c => c.Name)
            .ToListAsync();

        var clubNames = await db.Clubs.ForTenant(tenant)
            .Select(c => new { c.Id, c.Name })
            .ToDictionaryAsync(c => c.Id, c => c.Name);

        return [.. controllers.Select(c =>
        {
            var dto = FitnessMapper.ToDto(c, now);
            dto.ClubName = clubNames.GetValueOrDefault(c.ClubId);
            return dto;
        })];
    }

    public async Task<AccessControllerDto> SaveControllerAsync(Guid? id, SaveControllerDto request, Guid userId)
    {
        AccessController controller;
        if (id is null)
        {
            controller = new AccessController { ClubId = request.ClubId }.StampNew(tenant, userId);
            db.Controllers.Add(controller);
        }
        else
        {
            controller = await db.Controllers.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new InvalidOperationException("Controller not found.");
            controller.StampUpdated(userId);
        }

        controller.Name = request.Name;
        controller.Vendor = request.Vendor;
        controller.Model = request.Model;
        controller.FirmwareVersion = request.FirmwareVersion;
        controller.IpAddress = request.IpAddress;
        controller.SerialNumber = request.SerialNumber;
        controller.OfflinePolicy = request.OfflinePolicy;
        controller.CacheSeconds = request.CacheSeconds;
        controller.HeartbeatTimeoutMinutes = request.HeartbeatTimeoutMinutes;
        controller.IsActive = request.IsActive;

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(controller, DateTime.UtcNow);
    }

    /// <summary>
    /// The list a controller caches so it can decide on its own during an outage.
    ///
    /// Deliberately thin: an identifier, a yes/no, and the window it applies in. It is not a copy
    /// of the entitlement engine — a panel that tried to reproduce eleven rules would drift from
    /// the server the first time one of them changed. It answers "would this normally open?", and
    /// anything more nuanced waits for the network to come back.
    /// </summary>
    public async Task<AccessCacheDto> GetControllerCacheAsync(Guid controllerId)
    {
        var now = DateTime.UtcNow;

        var controller = await db.Controllers.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == controllerId)
            ?? throw new InvalidOperationException("Controller not found.");

        var credentials = await db.Credentials.ForTenant(tenant)
            .Where(c => c.Status == CredentialStatus.Active && (c.ExpiresOn == null || c.ExpiresOn > now))
            .Include(c => c.Member)
            .ToListAsync();

        var eligibleMemberIds = await db.Agreements.ForTenant(tenant)
            .Where(a => a.Status == AgreementStatus.Active || a.Status == AgreementStatus.NoticeGiven)
            .Select(a => a.MemberId)
            .ToListAsync();

        var blockedMemberIds = await db.MemberAlerts.ForTenant(tenant)
            .Where(a => a.BlocksAccess && (a.ExpiresOn == null || a.ExpiresOn > now))
            .Select(a => a.MemberId)
            .Distinct()
            .ToListAsync();

        var eligible = eligibleMemberIds.ToHashSet();
        var blocked = blockedMemberIds.ToHashSet();

        var entries = credentials
            .Where(c => c.Member is not null && c.Member.HomeClubId == controller.ClubId)
            .Select(c => new CachedCredentialDto
            {
                Identifier = c.Identifier,
                MemberId = c.MemberId,
                IsAllowed = eligible.Contains(c.MemberId) && !blocked.Contains(c.MemberId),
                DaysOfWeekMask = 127,
                ValidUntil = c.ExpiresOn,
                DisplayName = c.Member is null ? null : FitnessMapper.FullName(c.Member),
            })
            .ToList();

        controller.LastSyncAt = now;
        await db.SaveChangesAsync();

        return new AccessCacheDto
        {
            ControllerId = controllerId,
            GeneratedAt = now,
            OfflinePolicy = controller.OfflinePolicy,
            ValidSeconds = controller.CacheSeconds,
            Credentials = entries,
        };
    }

    public async Task RecordHeartbeatAsync(Guid controllerId, int pendingEvents)
    {
        var controller = await db.Controllers.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == controllerId);
        if (controller is null) return;

        controller.LastHeartbeatAt = DateTime.UtcNow;
        controller.PendingEventCount = pendingEvents;
        controller.IsOnline = true;
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Replays what a panel buffered while it was offline.
    ///
    /// The events are recorded as they happened, flagged as offline decisions, and *not*
    /// re-adjudicated — the door already let those people in, and pretending otherwise would
    /// produce an attendance record that disagrees with reality.
    /// </summary>
    public async Task<int> ReplayOfflineEventsAsync(Guid controllerId, List<AccessRequestDto> events)
    {
        var controller = await db.Controllers.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == controllerId)
            ?? throw new InvalidOperationException("Controller not found.");

        var now = DateTime.UtcNow;
        var replayed = 0;

        foreach (var e in events.OrderBy(e => e.OccurredAt ?? now))
        {
            var occurredAt = e.OccurredAt ?? now;

            var credential = await db.Credentials.ForTenant(tenant)
                .Include(c => c.Member)
                .FirstOrDefaultAsync(c => c.Identifier == e.CredentialIdentifier);

            db.AccessEvents.Add(new AccessEvent
            {
                ClubId = controller.ClubId,
                DoorId = e.DoorId,
                ControllerId = controllerId,
                OccurredAt = occurredAt,
                MemberId = credential?.MemberId,
                CredentialId = credential?.Id,
                CredentialIdentifier = e.CredentialIdentifier,
                Method = e.Method,
                Decision = AccessDecision.Granted,
                Direction = e.Direction,
                WasOfflineDecision = true,
                ReplayedAt = now,
                DecisionMessage = "Decided by the panel during an outage",
            }.StampNew(tenant));

            // Only entries become visits; an exit read closes one if it is still open.
            if (credential?.Member is not null && e.Direction != ReaderDirection.Out)
            {
                var club = await db.Clubs.ForTenant(tenant).FirstAsync(c => c.Id == controller.ClubId);
                await RecordCheckInAsync(credential.Member, club, e.DoorId is null ? null
                        : await db.Doors.ForTenant(tenant).FirstOrDefaultAsync(d => d.Id == e.DoorId),
                    credential, e.Method, null, null, null, null, occurredAt, false, Guid.Empty);
            }

            replayed++;
        }

        controller.PendingEventCount = 0;
        controller.LastSyncAt = now;
        await db.SaveChangesAsync();
        await RecountOccupancyAsync(controller.ClubId);
        await db.SaveChangesAsync();

        return replayed;
    }

    public async Task<DoorDto> ReleaseDoorAsync(Guid doorId, string reason, Guid userId)
    {
        var door = await db.Doors.ForTenant(tenant)
            .Include(d => d.Club)
            .Include(d => d.Controller)
            .FirstOrDefaultAsync(d => d.Id == doorId)
            ?? throw new InvalidOperationException("Door not found.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Releasing a door needs a reason — it is always logged.");

        db.AccessEvents.Add(new AccessEvent
        {
            ClubId = door.ClubId,
            DoorId = door.Id,
            ControllerId = door.ControllerId,
            OccurredAt = DateTime.UtcNow,
            Decision = AccessDecision.ManualOverride,
            Method = CredentialType.ManualLookup,
            OverriddenByStaffId = userId,
            OverrideReason = reason,
            DecisionMessage = "Door released from reception",
        }.StampNew(tenant, userId));

        await db.SaveChangesAsync();
        await compliance.LogAsync("DoorReleased", nameof(Door), door.Id, null, reason, false, userId);

        return FitnessMapper.ToDto(door);
    }

    public async Task<List<AccessRuleDto>> GetRulesAsync(Guid? clubId)
    {
        var rules = await db.AccessRules.ForTenant(tenant)
            .WhereIf(clubId is not null, r => r.ClubId == clubId || r.ClubId == null)
            .Include(r => r.Windows.Where(w => !w.IsDeleted))
            .OrderByDescending(r => r.IsDefault).ThenBy(r => r.Name)
            .ToListAsync();

        return [.. rules.Select(FitnessMapper.ToDto)];
    }

    public async Task<AccessRuleDto> SaveRuleAsync(Guid? id, AccessRuleDto request, Guid userId)
    {
        AccessRule rule;
        if (id is null)
        {
            rule = new AccessRule().StampNew(tenant, userId);
            db.AccessRules.Add(rule);
        }
        else
        {
            rule = await db.AccessRules.ForTenant(tenant)
                .Include(r => r.Windows.Where(w => !w.IsDeleted))
                .FirstOrDefaultAsync(r => r.Id == id)
                ?? throw new InvalidOperationException("Access rule not found.");
            rule.StampUpdated(userId);
        }

        rule.Name = request.Name;
        rule.ClubId = request.ClubId;
        rule.BalanceThreshold = request.BalanceThreshold;
        rule.RequiresWaiver = request.RequiresWaiver;
        rule.RequiresMedicalClearance = request.RequiresMedicalClearance;
        rule.RespectsOccupancyCap = request.RespectsOccupancyCap;
        rule.AntiPassback = request.AntiPassback;
        rule.MinimumAge = request.MinimumAge;
        rule.RequiresGuardian = request.RequiresGuardian;
        rule.MaxVisitsPerPeriod = request.MaxVisitsPerPeriod;
        rule.VisitLimitBasis = request.VisitLimitBasis;
        rule.IsDefault = request.IsDefault;
        rule.IsActive = request.IsActive;

        foreach (var existing in rule.Windows.Where(w => !w.IsDeleted)) existing.StampDeleted(userId);

        foreach (var w in request.Windows)
        {
            db.AccessRuleWindows.Add(new AccessRuleWindow
            {
                AccessRuleId = rule.Id,
                DaysOfWeekMask = w.DaysOfWeekMask,
                StartsAt = w.StartsAt,
                EndsAt = w.EndsAt,
                Label = w.Label,
            }.StampNew(tenant, userId));
        }

        await db.SaveChangesAsync();

        var saved = await db.AccessRules.ForTenant(tenant)
            .Include(r => r.Windows.Where(w => !w.IsDeleted))
            .FirstAsync(r => r.Id == rule.Id);

        return FitnessMapper.ToDto(saved);
    }

    // ── Guests & day passes ──────────────────────────────────────────────────

    public async Task<GuestVisitDto> RegisterGuestAsync(RegisterGuestDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var host = await db.Members.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == request.HostMemberId)
            ?? throw new InvalidOperationException("Host member not found.");

        var club = await db.Clubs.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == request.ClubId)
            ?? throw new InvalidOperationException("Club not found.");

        // A guest signs their own waiver. The host's signature covers the host, and that is the
        // whole reason the club can let a stranger onto a squat rack.
        if (club.RequiresWaiver && request.SignatureImageUrl is null)
            throw new InvalidOperationException("The guest needs to sign a waiver before they can come in.");

        var usedAllowance = false;
        decimal fee = request.FeeOverride ?? 0m;

        if (request.UseHostAllowance)
        {
            var allowance = await GetGuestAllowanceAsync(host.Id, now);
            if (allowance is { Used: var used, Allowance: var total } && used < total)
            {
                allowance.Used++;
                allowance.LastUsedAt = now;
                usedAllowance = true;
                fee = 0m;
            }
        }

        var guest = new GuestVisit
        {
            ClubId = request.ClubId,
            HostMemberId = request.HostMemberId,
            GuestName = request.GuestName,
            GuestPhone = request.GuestPhone,
            GuestEmail = request.GuestEmail,
            GuestDateOfBirth = request.GuestDateOfBirth,
            VisitedOn = now,
            WaiverSigned = request.SignatureImageUrl is not null,
            UsedHostAllowance = usedAllowance,
            FeeCharged = fee,
        }.StampNew(tenant, userId);

        db.GuestVisits.Add(guest);

        if (request.WaiverTemplateId is not null && request.SignatureImageUrl is not null)
        {
            var signature = await compliance.SignWaiverAsync(new SignWaiverDto
            {
                WaiverTemplateId = request.WaiverTemplateId.Value,
                ClubId = request.ClubId,
                SignerName = request.GuestName,
                SignerEmail = request.GuestEmail,
                SignerPhone = request.GuestPhone,
                SignerDateOfBirth = request.GuestDateOfBirth,
                GuestVisitId = guest.Id,
                SignatureImageUrl = request.SignatureImageUrl,
                CapturedVia = "Guest registration",
            }, userId);

            guest.WaiverSignatureId = signature.Id;
        }

        // A visit is a check-in like any other, so occupancy and reporting stay correct.
        db.CheckIns.Add(new CheckIn
        {
            ClubId = request.ClubId,
            Kind = VisitKind.Guest,
            CheckedInAt = now,
            Method = CredentialType.ManualLookup,
            HostMemberId = host.Id,
            WasManualEntry = true,
            CheckedInByStaffId = userId,
            FeeCharged = fee,
        }.StampNew(tenant, userId));

        await AdjustOccupancyAsync(request.ClubId, null, 1);

        // Every guest is a prospect. Not creating a lead is leaving money on the counter.
        if (request.CreateLead)
        {
            var lead = new FitnessLead
            {
                ClubId = request.ClubId,
                FirstName = request.GuestName.Split(' ').FirstOrDefault() ?? request.GuestName,
                LastName = request.GuestName.Contains(' ') ? request.GuestName[(request.GuestName.IndexOf(' ') + 1)..] : null,
                Phone = request.GuestPhone,
                Email = request.GuestEmail,
                DateOfBirth = request.GuestDateOfBirth,
                Status = LeadStatus.New,
                ReceivedAt = now,
                Notes = $"Visited as a guest of {FitnessMapper.FullName(host)}",
                ReferredByMemberId = host.Id,
            }.StampNew(tenant, userId);

            db.Leads.Add(lead);
            guest.CreatedLeadId = lead.Id;
        }

        if (fee > 0)
        {
            await billing.CreateAdHocInvoiceAsync(host.Id, request.ClubId, [
                new InvoiceLineDto
                {
                    ChargeKind = ChargeKind.GuestFee,
                    LineDescription = $"Guest visit — {request.GuestName}",
                    Quantity = 1,
                    UnitPrice = fee,
                    LineTotal = fee,
                },
            ], userId);
        }

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(guest);
    }

    public async Task<DayPassDto> IssueDayPassAsync(IssueDayPassDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var club = await db.Clubs.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == request.ClubId)
            ?? throw new InvalidOperationException("Club not found.");

        var plan = request.PlanId is null
            ? null
            : await db.Plans.ForTenant(tenant).FirstOrDefaultAsync(p => p.Id == request.PlanId);

        var price = request.PriceOverride ?? plan?.Price ?? 0m;
        var validTo = request.ValidTo
            ?? (plan?.ValidForDays > 0 ? request.ValidFrom.AddDays(plan.ValidForDays) : request.ValidFrom.Date.AddDays(1).AddSeconds(-1));

        var pass = new DayPass
        {
            PassNumber = await numbering.NextDayPassNumberAsync(request.ClubId, now),
            ClubId = request.ClubId,
            MemberId = request.MemberId,
            VisitorName = request.VisitorName,
            VisitorPhone = request.VisitorPhone,
            VisitorEmail = request.VisitorEmail,
            VisitorDateOfBirth = request.VisitorDateOfBirth,
            PlanId = request.PlanId,
            ValidFrom = request.ValidFrom,
            ValidTo = validTo,
            MaxEntries = request.MaxEntries,
            AmountPaid = price,
            WaiverSigned = request.SignatureImageUrl is not null,
            IsTrial = request.IsTrial,
            IssuedByStaffId = userId,
        }.StampNew(tenant, userId);

        db.DayPasses.Add(pass);

        if (club.RequiresWaiver && request.WaiverTemplateId is not null && request.SignatureImageUrl is not null)
        {
            var signature = await compliance.SignWaiverAsync(new SignWaiverDto
            {
                WaiverTemplateId = request.WaiverTemplateId.Value,
                ClubId = request.ClubId,
                MemberId = request.MemberId,
                SignerName = request.VisitorName,
                SignerEmail = request.VisitorEmail,
                SignerPhone = request.VisitorPhone,
                SignerDateOfBirth = request.VisitorDateOfBirth,
                DayPassId = pass.Id,
                SignatureImageUrl = request.SignatureImageUrl,
                CapturedVia = "Day pass",
            }, userId);

            pass.WaiverSignatureId = signature.Id;
        }
        else if (club.RequiresWaiver)
        {
            throw new InvalidOperationException("A waiver has to be signed before a day pass can be issued.");
        }

        if (request.CreateLead && request.MemberId is null)
        {
            var lead = new FitnessLead
            {
                ClubId = request.ClubId,
                FirstName = request.VisitorName.Split(' ').FirstOrDefault() ?? request.VisitorName,
                LastName = request.VisitorName.Contains(' ') ? request.VisitorName[(request.VisitorName.IndexOf(' ') + 1)..] : null,
                Phone = request.VisitorPhone,
                Email = request.VisitorEmail,
                DateOfBirth = request.VisitorDateOfBirth,
                Status = request.IsTrial ? LeadStatus.Trialling : LeadStatus.New,
                ReceivedAt = now,
                Notes = request.IsTrial ? "Started a trial" : "Bought a day pass",
                TrialStartedOn = request.IsTrial ? request.ValidFrom : null,
                TrialEndsOn = request.IsTrial ? validTo : null,
            }.StampNew(tenant, userId);

            db.Leads.Add(lead);
            pass.CreatedLeadId = lead.Id;
        }

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(pass, now);
    }

    public async Task<PaginatedResponse<DayPassDto>> ListDayPassesAsync(
        Guid? clubId, DateTime? from, DateTime? to, PaginationParams pagination)
    {
        var now = DateTime.UtcNow;

        var query = db.DayPasses.ForTenant(tenant)
            .WhereIf(clubId is not null, p => p.ClubId == clubId)
            .WhereIf(from is not null, p => p.ValidFrom >= from)
            .WhereIf(to is not null, p => p.ValidFrom <= to);

        var total = await query.CountAsync();

        var page = await query
            .OrderByDescending(p => p.ValidFrom)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        return PaginatedResponse<DayPassDto>.Ok(
                   [.. page.Select(p => FitnessMapper.ToDto(p, now))],
                   total,
                   pagination.PageNumber,
                   pagination.PageSize);
    }

    // ── Front desk ───────────────────────────────────────────────────────────

    /// <summary>
    /// Everything the front-desk screen renders, in one call.
    ///
    /// The receptionist has a queue. A screen that needs six requests to paint is a screen that
    /// paints while somebody waits, so this is one round trip even though it costs several
    /// queries server-side.
    /// </summary>
    public async Task<FrontDeskDto> GetFrontDeskAsync(Guid clubId, Guid? staffId)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var tomorrow = today.AddDays(1);

        var club = await db.Clubs.ForTenant(tenant)
            .Include(c => c.Schedules.Where(s => !s.IsDeleted))
            .FirstOrDefaultAsync(c => c.Id == clubId)
            ?? throw new InvalidOperationException("Club not found.");

        var dto = new FrontDeskDto
        {
            ClubId = clubId,
            ClubName = club.Name,
            GeneratedAt = now,
            Occupancy = await GetOccupancyAsync(clubId),
            IsOpenNow = ClubService.IsOpenAt(club, now),
            IsStaffedNow = ClubService.IsStaffedAt(club, now),
        };

        var schedule = club.Schedules.FirstOrDefault(s => s.OverrideDate?.Date == today)
                       ?? club.Schedules.FirstOrDefault(s => s.OverrideDate is null && s.DayOfWeek == (int)now.DayOfWeek);
        dto.ClosesAt = schedule?.ClosesAt;

        dto.RecentCheckIns = [.. (await db.CheckIns.ForTenant(tenant)
            .Where(c => c.ClubId == clubId && c.CheckedInAt >= today)
            .Include(c => c.Member)
            .OrderByDescending(c => c.CheckedInAt)
            .Take(15)
            .ToListAsync())
            .Select(FitnessMapper.ToDto)];

        dto.ClassesToday = [.. (await db.ClassOccurrences.ForTenant(tenant)
            .Where(o => o.ClubId == clubId && o.StartsAt >= today && o.StartsAt < tomorrow)
            .Include(o => o.ClassType)
            .Include(o => o.Room)
            .OrderBy(o => o.StartsAt)
            .ToListAsync())
            .Select(o => FitnessMapper.ToSummary(o, now))];

        dto.AppointmentsToday = [.. (await db.Appointments.ForTenant(tenant)
            .Where(a => a.ClubId == clubId && a.StartsAt >= today && a.StartsAt < tomorrow
                     && a.Status != AppointmentStatus.Cancelled)
            .Include(a => a.Service)
            .Include(a => a.Member)
            .Include(a => a.Participants)
            .OrderBy(a => a.StartsAt)
            .ToListAsync())
            .Select(FitnessMapper.ToSummary)];

        dto.MyTasks = [.. (await db.RetentionTasks.ForTenant(tenant)
            .Where(t => t.ClubId == clubId && t.CompletedAt == null && !t.IsDismissed
                     && (staffId == null || t.AssignedStaffId == staffId || t.AssignedStaffId == null))
            .Include(t => t.Member)
            .OrderBy(t => t.DueOn)
            .Take(10)
            .ToListAsync())
            .Select(t => FitnessMapper.ToDto(t, now))];

        dto.UrgentAlerts = [.. (await db.MemberAlerts.ForTenant(tenant)
            .Where(a => a.Severity == AlertSeverity.Blocking && a.AcknowledgedAt == null
                     && a.Member!.HomeClubId == clubId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .ToListAsync())
            .Select(FitnessMapper.ToDto)];

        dto.Announcements = [.. (await db.Announcements.ForTenant(tenant)
            .Where(a => (a.ClubId == clubId || a.ClubId == null) && a.IsPublished
                     && a.ShowFrom <= now && (a.ShowUntil == null || a.ShowUntil >= now)
                     && a.ShowOnKiosk)
            .OrderByDescending(a => a.IsUrgent).ThenByDescending(a => a.ShowFrom)
            .Take(5)
            .ToListAsync())
            .Select(a => FitnessMapper.ToDto(a, now))];

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();
        var slaMinutes = settings?.LeadResponseSlaMinutes ?? 15;

        dto.OpenLeads = await db.Leads.ForTenant(tenant)
            .CountAsync(l => l.ClubId == clubId && l.Status != LeadStatus.Won && l.Status != LeadStatus.Lost);

        dto.LeadsBreachingSla = await db.Leads.ForTenant(tenant)
            .CountAsync(l => l.ClubId == clubId && l.FirstContactedAt == null
                          && l.ReceivedAt < now.AddMinutes(-slaMinutes)
                          && l.Status != LeadStatus.Won && l.Status != LeadStatus.Lost);

        var arrears = await db.Invoices.ForTenant(tenant)
            .Where(i => i.ClubId == clubId && i.BalanceDue > 0 && i.DueOn < now)
            .GroupBy(i => 1)
            .Select(g => new { Count = g.Select(i => i.MemberId).Distinct().Count(), Amount = g.Sum(i => i.BalanceDue) })
            .FirstOrDefaultAsync();

        dto.OverdueBalances = arrears?.Count ?? 0;
        dto.OverdueAmount = arrears?.Amount ?? 0m;

        dto.WaiversOutstanding = await db.MemberAlerts.ForTenant(tenant)
            .CountAsync(a => a.Member!.HomeClubId == clubId
                          && (a.Kind == MemberAlertKind.WaiverMissing || a.Kind == MemberAlertKind.WaiverExpired));

        var session = await db.CashSessions.ForTenant(tenant)
            .Where(s => s.ClubId == clubId && s.Status == CashSessionStatus.Open)
            .FirstOrDefaultAsync();

        dto.OpenCashSessionId = session?.Id;
        dto.CashSessionTakings = session is null ? 0 : session.CashSales + session.CardSales + session.OtherSales;

        return dto;
    }

    // ═══ Internals ═══════════════════════════════════════════════════════════

    private async Task<AccessDecisionDto> GrantEntryAsync(
        Member member, FitnessClub club, Door? door, MemberCredential credential,
        Agreement agreement, VisitAllowanceUsage? allowance, Guid? bookingId,
        bool crossClub, bool softPassback, List<MemberAlert> alerts,
        AccessRequestDto request, DateTime now, Stopwatch sw)
    {
        var fee = crossClub ? club.CrossClubVisitFee : 0m;

        var checkIn = await RecordCheckInAsync(member, club, door, credential, request.Method,
            bookingId, null, agreement.Id, allowance, now, false, Guid.Empty);

        checkIn.FeeCharged = fee;

        if (allowance is not null)
        {
            allowance.Used++;
            allowance.LastUsedAt = now;
            checkIn.ConsumedVisitAllowance = true;
        }

        credential.LastUsedAt = now;

        var accessEvent = new AccessEvent
        {
            ClubId = club.Id,
            DoorId = door?.Id,
            ControllerId = door?.ControllerId,
            OccurredAt = now,
            MemberId = member.Id,
            CredentialId = credential.Id,
            CredentialIdentifier = request.CredentialIdentifier,
            Method = request.Method,
            Decision = softPassback ? AccessDecision.GrantedWithWarning : AccessDecision.Granted,
            Direction = ReaderDirection.In,
            WasOfflineDecision = request.WasOfflineDecision,
            CheckInId = checkIn.Id,
            DecisionMs = (int)sw.ElapsedMilliseconds,
            DecisionMessage = softPassback ? "Already inside — flagged for review" : null,
        }.StampNew(tenant);

        db.AccessEvents.Add(accessEvent);
        await db.SaveChangesAsync();

        var greeting = member.PreferredName ?? member.FirstName;
        var visitNumber = member.TotalVisits;
        var isMilestone = visitNumber is 1 or 50 or 100 or 250 or 500 or 1000;
        var isBirthday = member.DateOfBirth is not null
                         && member.DateOfBirth.Value.Month == now.Month
                         && member.DateOfBirth.Value.Day == now.Day;

        var message = isBirthday
            ? $"Happy birthday, {greeting}!"
            : isMilestone
                ? $"Welcome back, {greeting} — that's visit number {visitNumber}."
                : $"Welcome back, {greeting}.";

        if (fee > 0) message += $" A {fee:0.00} visitor fee applies at this club.";

        return new AccessDecisionDto
        {
            Decision = accessEvent.Decision,
            DenialReason = AccessDenialReason.None,
            Message = message,
            MemberId = member.Id,
            MemberName = FitnessMapper.FullName(member),
            PreferredName = member.PreferredName,
            PhotoUrl = member.PhotoUrl,
            MemberNumber = member.MemberNumber,
            MemberStatus = member.Status,
            CheckInId = checkIn.Id,
            AccessEventId = accessEvent.Id,
            Alerts = [.. alerts.Where(a => !a.BlocksAccess).Select(FitnessMapper.ToDto)],
            NextBooking = (await members.GetUpcomingAsync(member.Id)).FirstOrDefault(),
            ClubOccupancy = club.CurrentOccupancy,
            ClubCapacity = club.HardCapacity ?? club.SoftCapacity,
            VisitNumber = visitNumber,
            IsMilestoneVisit = isMilestone,
            IsBirthday = isBirthday,
            DecisionMs = (int)sw.ElapsedMilliseconds,
        };
    }

    private async Task<AccessDecisionDto> GrantExitAsync(
        Member member, FitnessClub club, Door? door, MemberCredential credential,
        AccessRequestDto request, CheckIn? openVisit, DateTime now, Stopwatch sw)
    {
        if (openVisit is not null)
        {
            openVisit.CheckedOutAt = now;
            openVisit.DurationMinutes = (int)(now - openVisit.CheckedInAt).TotalMinutes;
            await AdjustOccupancyAsync(club.Id, door?.AreaId, -1);
        }

        var accessEvent = new AccessEvent
        {
            ClubId = club.Id,
            DoorId = door?.Id,
            ControllerId = door?.ControllerId,
            OccurredAt = now,
            MemberId = member.Id,
            CredentialId = credential.Id,
            CredentialIdentifier = request.CredentialIdentifier,
            Method = request.Method,
            Decision = AccessDecision.Granted,
            Direction = ReaderDirection.Out,
            CheckInId = openVisit?.Id,
            DecisionMs = (int)sw.ElapsedMilliseconds,
        }.StampNew(tenant);

        db.AccessEvents.Add(accessEvent);
        await db.SaveChangesAsync();

        var minutes = openVisit?.DurationMinutes;
        return new AccessDecisionDto
        {
            Decision = AccessDecision.Granted,
            Message = minutes is null
                ? "Goodbye."
                : $"See you next time — {minutes} minutes today.",
            MemberId = member.Id,
            MemberName = FitnessMapper.FullName(member),
            PreferredName = member.PreferredName,
            PhotoUrl = member.PhotoUrl,
            MemberNumber = member.MemberNumber,
            CheckInId = openVisit?.Id,
            AccessEventId = accessEvent.Id,
            ClubOccupancy = club.CurrentOccupancy,
            DecisionMs = (int)sw.ElapsedMilliseconds,
        };
    }

    private async Task<AccessDecisionDto> DecideDayPassAsync(
        DayPass pass, FitnessClub club, Door? door, AccessRequestDto request, DateTime now, Stopwatch sw)
    {
        if (pass.EntriesUsed >= pass.MaxEntries)
        {
            return await RefuseAsync(request, club, door, pass.MemberId, null,
                AccessDenialReason.VisitAllowanceExhausted,
                "This pass has been used. Reception can sell you another.", now, sw);
        }

        if (club.RequiresWaiver && !pass.WaiverSigned)
        {
            return await RefuseAsync(request, club, door, pass.MemberId, null,
                AccessDenialReason.WaiverNotSigned,
                "Please sign the waiver at reception before coming in.", now, sw);
        }

        pass.EntriesUsed++;

        var checkIn = new CheckIn
        {
            ClubId = club.Id,
            MemberId = pass.MemberId,
            Kind = pass.IsTrial ? VisitKind.Trial : VisitKind.DayPass,
            CheckedInAt = now,
            Method = request.Method,
            DoorId = door?.Id,
            AreaId = door?.AreaId,
            DayPassId = pass.Id,
        }.StampNew(tenant);

        db.CheckIns.Add(checkIn);
        await AdjustOccupancyAsync(club.Id, door?.AreaId, 1);

        var accessEvent = new AccessEvent
        {
            ClubId = club.Id,
            DoorId = door?.Id,
            OccurredAt = now,
            MemberId = pass.MemberId,
            CredentialIdentifier = request.CredentialIdentifier,
            Method = request.Method,
            Decision = AccessDecision.Granted,
            Direction = ReaderDirection.In,
            CheckInId = checkIn.Id,
            DecisionMs = (int)sw.ElapsedMilliseconds,
        }.StampNew(tenant);

        db.AccessEvents.Add(accessEvent);
        await db.SaveChangesAsync();

        var remaining = pass.MaxEntries - pass.EntriesUsed;
        return new AccessDecisionDto
        {
            Decision = AccessDecision.Granted,
            Message = $"Welcome, {pass.VisitorName}." +
                      (remaining > 0 ? $" {remaining} more visits on this pass." : " This was your last visit on this pass."),
            MemberName = pass.VisitorName,
            CheckInId = checkIn.Id,
            AccessEventId = accessEvent.Id,
            ClubOccupancy = club.CurrentOccupancy,
            DecisionMs = (int)sw.ElapsedMilliseconds,
        };
    }

    private async Task<AccessDecisionDto> RefuseAsync(
        AccessRequestDto request, FitnessClub club, Door? door,
        Guid? memberId, Guid? credentialId, AccessDenialReason reason, string message,
        DateTime now, Stopwatch sw)
    {
        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();

        var accessEvent = new AccessEvent
        {
            ClubId = club.Id,
            DoorId = door?.Id,
            ControllerId = door?.ControllerId,
            OccurredAt = now,
            MemberId = memberId,
            CredentialId = credentialId,
            CredentialIdentifier = request.CredentialIdentifier,
            Method = request.Method,
            Decision = AccessDecision.Denied,
            DenialReason = reason,
            DecisionMessage = message,
            Direction = request.Direction,
            WasOfflineDecision = request.WasOfflineDecision,
            DecisionMs = (int)sw.ElapsedMilliseconds,
        }.StampNew(tenant);

        db.AccessEvents.Add(accessEvent);
        await db.SaveChangesAsync();

        Member? member = memberId is null
            ? null
            : await db.Members.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == memberId);

        return new AccessDecisionDto
        {
            Decision = AccessDecision.Denied,
            DenialReason = reason,
            Message = message,
            MemberId = memberId,
            MemberName = member is null ? null : FitnessMapper.FullName(member),
            PreferredName = member?.PreferredName,
            PhotoUrl = member?.PhotoUrl,
            MemberNumber = member?.MemberNumber,
            MemberStatus = member?.Status,
            AccessEventId = accessEvent.Id,
            ClubOccupancy = club.CurrentOccupancy,
            ClubCapacity = club.HardCapacity ?? club.SoftCapacity,
            DecisionMs = (int)sw.ElapsedMilliseconds,
        };
    }

    private async Task<CheckIn> RecordCheckInAsync(
        Member member, FitnessClub club, Door? door, MemberCredential? credential,
        CredentialType method, Guid? bookingId, Guid? appointmentId,
        Guid? agreementId, VisitAllowanceUsage? allowance, DateTime now, bool manual, Guid staffId)
    {
        var checkIn = new CheckIn
        {
            ClubId = club.Id,
            MemberId = member.Id,
            Kind = VisitKind.Member,
            CheckedInAt = now,
            Method = method,
            CredentialId = credential?.Id,
            DoorId = door?.Id,
            AreaId = door?.AreaId,
            ClassBookingId = bookingId,
            AppointmentId = appointmentId,
            AgreementId = agreementId,
            WasManualEntry = manual,
            CheckedInByStaffId = manual ? staffId : null,
        }.StampNew(tenant, staffId == Guid.Empty ? null : staffId);

        db.CheckIns.Add(checkIn);

        // The rolled-up counters the desk and the churn engine read, kept current here rather than
        // recomputed later — a member list that shows a stale "last visit" is a member list nobody
        // trusts.
        member.LastVisitOn = now;
        member.TotalVisits++;
        member.VisitsThisMonth = now.Month == (member.LastVisitOn?.Month ?? now.Month) ? member.VisitsThisMonth + 1 : 1;

        await AdjustOccupancyAsync(club.Id, door?.AreaId, 1);
        return checkIn;
    }

    private async Task AdjustOccupancyAsync(Guid clubId, Guid? areaId, int delta)
    {
        var club = await db.Clubs.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == clubId);
        if (club is not null) club.CurrentOccupancy = Math.Max(0, club.CurrentOccupancy + delta);

        if (areaId is not null)
        {
            var area = await db.Areas.ForTenant(tenant).FirstOrDefaultAsync(a => a.Id == areaId);
            if (area is not null) area.CurrentOccupancy = Math.Max(0, area.CurrentOccupancy + delta);
        }
    }

    private async Task RecountOccupancyAsync(Guid clubId)
    {
        var actual = await db.CheckIns.ForTenant(tenant)
            .CountAsync(c => c.ClubId == clubId && c.CheckedOutAt == null);

        var club = await db.Clubs.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == clubId);
        if (club is not null) club.CurrentOccupancy = actual;
    }

    private async Task<VisitAllowanceUsage> GetOrCreateAllowanceAsync(
        Guid memberId, Guid agreementId, MembershipPlan plan, DateTime now)
    {
        var (start, end) = PeriodFor(plan.VisitLimitBasis, now);

        var usage = await db.AllowanceUsage.ForTenant(tenant)
            .FirstOrDefaultAsync(u => u.MemberId == memberId
                                   && u.AgreementId == agreementId
                                   && u.Kind == EntitlementKind.ClubAccess
                                   && u.PeriodStart == start);

        if (usage is not null) return usage;

        usage = new VisitAllowanceUsage
        {
            MemberId = memberId,
            AgreementId = agreementId,
            Kind = EntitlementKind.ClubAccess,
            PeriodStart = start,
            PeriodEnd = end,
            Allowance = plan.VisitsPerPeriod,
        }.StampNew(tenant);

        db.AllowanceUsage.Add(usage);
        return usage;
    }

    private async Task<VisitAllowanceUsage?> GetGuestAllowanceAsync(Guid memberId, DateTime now)
    {
        var agreement = await db.Agreements.ForTenant(tenant)
            .Include(a => a.Plan)
            .Where(a => a.MemberId == memberId && a.Status == AgreementStatus.Active)
            .FirstOrDefaultAsync();

        if (agreement?.Plan is null || agreement.Plan.GuestPassesPerPeriod == 0) return null;

        var (start, end) = PeriodFor(EntitlementLimit.PerMonth, now);

        var usage = await db.AllowanceUsage.ForTenant(tenant)
            .FirstOrDefaultAsync(u => u.MemberId == memberId
                                   && u.Kind == EntitlementKind.GuestPass
                                   && u.PeriodStart == start);

        if (usage is null)
        {
            usage = new VisitAllowanceUsage
            {
                MemberId = memberId,
                AgreementId = agreement.Id,
                Kind = EntitlementKind.GuestPass,
                PeriodStart = start,
                PeriodEnd = end,
                Allowance = agreement.Plan.GuestPassesPerPeriod,
            }.StampNew(tenant);

            db.AllowanceUsage.Add(usage);
        }

        return usage;
    }

    private static (DateTime Start, DateTime End) PeriodFor(EntitlementLimit basis, DateTime now) => basis switch
    {
        EntitlementLimit.PerDay => (now.Date, now.Date.AddDays(1).AddSeconds(-1)),
        EntitlementLimit.PerWeek => (
            now.Date.AddDays(-(int)now.DayOfWeek),
            now.Date.AddDays(7 - (int)now.DayOfWeek).AddSeconds(-1)),
        _ => (
            new DateTime(now.Year, now.Month, 1),
            new DateTime(now.Year, now.Month, 1).AddMonths(1).AddSeconds(-1)),
    };

    private async Task<bool> IsGuardianInClubAsync(Member junior, Guid clubId, DateTime now)
    {
        if (junior.HouseholdId is null) return false;

        var guardianIds = await db.HouseholdMembers.ForTenant(tenant)
            .Where(h => h.HouseholdId == junior.HouseholdId
                     && (h.Role == HouseholdRole.Primary || h.Role == HouseholdRole.Partner))
            .Select(h => h.MemberId)
            .ToListAsync();

        if (guardianIds.Count == 0) return false;

        return await db.CheckIns.ForTenant(tenant)
            .AnyAsync(c => c.ClubId == clubId
                        && c.MemberId != null && guardianIds.Contains(c.MemberId.Value)
                        && c.CheckedOutAt == null);
    }

    /// <summary>
    /// "Your membership covers off-peak hours. It opens again at 16:00 today." — so an off-peak
    /// member turned away at 08:00 knows when to come back rather than assuming it is a fault.
    /// </summary>
    private static string DescribeNextBand(List<AccessTimeBand> bands, DateTime now)
    {
        var todayBands = bands
            .Where(b => FitnessQueryExtensions.CoversDay(b.DaysOfWeekMask, now.DayOfWeek))
            .Where(b => b.StartsAt > now.TimeOfDay)
            .OrderBy(b => b.StartsAt)
            .ToList();

        if (todayBands.Count > 0)
            return $"It opens again at {todayBands[0].StartsAt:hh\\:mm} today.";

        for (var offset = 1; offset <= 7; offset++)
        {
            var day = now.AddDays(offset);
            var next = bands
                .Where(b => FitnessQueryExtensions.CoversDay(b.DaysOfWeekMask, day.DayOfWeek))
                .OrderBy(b => b.StartsAt)
                .FirstOrDefault();

            if (next is not null)
                return $"Your next window is {day:dddd} from {next.StartsAt:hh\\:mm}.";
        }

        return "Reception can tell you which hours are included.";
    }
}
