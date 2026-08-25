using System.Text.Json;
using Fitness.Application.DTOs;
using Fitness.Application.Services.Interfaces;
using Fitness.Domain.Entities;
using Fitness.Domain.Enums;
using Fitness.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;

namespace Fitness.Infrastructure.Services;

/// <summary>
/// Retention: scoring who is about to leave, the board that gets them called, and the automation
/// that reaches the ones nobody has time to call.
///
/// Churn is the only number that decides whether a gym survives, so the scoring here is
/// deliberately **explainable rather than clever**. Every member's risk band comes with the
/// reasons behind it, in words a coach can open a conversation with — "you were coming three
/// times a week and we haven't seen you in a fortnight, everything all right?" is a phone call
/// somebody will actually make. A score of 0.71 is not.
/// </summary>
public class RetentionService(
    FitnessDbContext db,
    IFitnessTenant tenant) : IRetentionService
{
    // ── Board ────────────────────────────────────────────────────────────────

    public async Task<RetentionBoardDto> GetBoardAsync(Guid? clubId, ChurnRiskBand? band, Guid? ownerStaffId)
    {
        var now = DateTime.UtcNow;
        var weekStart = now.Date.AddDays(-7);
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var scores = await db.ChurnScores.ForTenant(tenant)
            .Include(c => c.Member)
            .Include(c => c.Factors.Where(f => !f.IsDeleted))
            .WhereIf(clubId is not null, c => c.ClubId == clubId)
            .WhereIf(band is not null, c => c.Band == band)
            .WhereIf(ownerStaffId is not null, c => c.OwnerStaffId == ownerStaffId)
            .ToListAsync();

        var board = new RetentionBoardDto
        {
            ClubId = clubId,
            GeneratedAt = now,
            LastScoredAt = scores.Count == 0 ? null : scores.Max(s => s.ComputedOn),
            HealthyCount = scores.Count(s => s.Band == ChurnRiskBand.Healthy),
            WatchCount = scores.Count(s => s.Band == ChurnRiskBand.Watch),
            AtRiskCount = scores.Count(s => s.Band == ChurnRiskBand.AtRisk),
            CriticalCount = scores.Count(s => s.Band == ChurnRiskBand.Critical),
            NewlyAtRisk = scores.Count(s => s.BandWorsened),
        };

        if (clubId is not null)
        {
            board.ClubName = await db.Clubs.ForTenant(tenant)
                .Where(c => c.Id == clubId).Select(c => c.Name).FirstOrDefaultAsync();
        }

        // Everything the board shows is sorted worst-first and carries its reasons.
        var atRisk = scores
            .Where(s => s.Band is ChurnRiskBand.AtRisk or ChurnRiskBand.Critical)
            .OrderByDescending(s => s.Score)
            .Take(200)
            .ToList();

        var memberIds = atRisk.Select(s => s.MemberId).ToList();

        var agreements = await db.Agreements.ForTenant(tenant)
            .Where(a => memberIds.Contains(a.MemberId) && a.Status == AgreementStatus.Active)
            .Include(a => a.Plan)
            .Select(a => new { a.MemberId, a.Price, a.BillingPeriod, PlanName = a.Plan!.Name })
            .ToListAsync();

        var lifetime = await db.Payments.ForTenant(tenant)
            .Where(p => memberIds.Contains(p.MemberId) && p.Status == PaymentStatus.Succeeded)
            .GroupBy(p => p.MemberId)
            .Select(g => new { MemberId = g.Key, Total = g.Sum(p => p.Amount) })
            .ToListAsync();

        var staffNames = await db.Staff.ForTenant(tenant)
            .Select(s => new { s.Id, Name = s.FirstName + " " + s.LastName })
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        foreach (var score in atRisk)
        {
            var dto = FitnessMapper.ToDto(score);

            var agreement = agreements.FirstOrDefault(a => a.MemberId == score.MemberId);
            dto.MonthlyValue = agreement is null
                ? 0
                : Math.Round(FitnessQueryExtensions.ToMonthly(agreement.Price, agreement.BillingPeriod), 2);
            dto.PlanName = agreement?.PlanName;
            dto.LifetimeValue = lifetime.FirstOrDefault(l => l.MemberId == score.MemberId)?.Total ?? 0m;

            if (score.OwnerStaffId is not null)
                dto.OwnerStaffName = staffNames.GetValueOrDefault(score.OwnerStaffId.Value);

            board.Members.Add(dto);
        }

        board.ValueAtRisk = Math.Round(board.Members.Sum(m => m.MonthlyValue), 2);

        board.OpenTasks = [.. (await db.RetentionTasks.ForTenant(tenant)
            .Where(t => t.CompletedAt == null && !t.IsDismissed)
            .WhereIf(clubId is not null, t => t.ClubId == clubId)
            .WhereIf(ownerStaffId is not null, t => t.AssignedStaffId == ownerStaffId)
            .Include(t => t.Member)
            .OrderBy(t => t.DueOn)
            .Take(50)
            .ToListAsync())
            .Select(t => FitnessMapper.ToDto(t, now))];

        // How well the club is doing at this, not just how many are at risk.
        board.ContactedThisWeek = await db.RetentionTasks.ForTenant(tenant)
            .CountAsync(t => t.CompletedAt >= weekStart && (clubId == null || t.ClubId == clubId));

        board.RecoveredThisMonth = await db.ChurnScores.ForTenant(tenant)
            .CountAsync(c => c.Band == ChurnRiskBand.Healthy
                          && c.PreviousBand != null && c.PreviousBand != ChurnRiskBand.Healthy
                          && c.ComputedOn >= monthStart
                          && (clubId == null || c.ClubId == clubId));

        var cancellations = await db.CancellationRequests.ForTenant(tenant)
            .Where(c => c.RequestedOn >= monthStart)
            .WhereIf(clubId is not null, c => c.Agreement!.ClubId == clubId)
            .Select(c => c.WasSaved)
            .ToListAsync();

        board.SavedThisMonth = cancellations.Count(s => s);
        board.SaveRatePercent = FitnessMapper.Percent(board.SavedThisMonth, cancellations.Count);

        return board;
    }

    public async Task<ChurnScoreDto?> GetScoreAsync(Guid memberId)
    {
        var score = await db.ChurnScores.ForTenant(tenant)
            .Include(c => c.Member)
            .Include(c => c.Factors.Where(f => !f.IsDeleted))
            .FirstOrDefaultAsync(c => c.MemberId == memberId);

        return score is null ? null : FitnessMapper.ToDto(score);
    }

    /// <summary>
    /// Recomputes every member's risk band. Runs nightly.
    ///
    /// The model is a weighted sum of things a human would notice, and every contributing factor
    /// is written down with the sentence that explains it. Two design choices matter:
    ///
    /// - **Attendance is measured against the member's own baseline**, not a club average. Someone
    ///   who came twice a week and now comes once has halved their engagement; someone who always
    ///   came once a month has not changed at all, and flagging them wastes a call.
    /// - **Never-attended is scored highest of all.** A member who joined and never came back is
    ///   the most reliably lost person in the database, and the one most easily saved in the
    ///   first fortnight.
    /// </summary>
    public async Task<int> ScoreAllAsync(Guid? clubId)
    {
        var now = DateTime.UtcNow;

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync() ?? new FitnessSettings();
        var riskDays = settings.AbsenceRiskDays;
        var criticalDays = settings.CriticalAbsenceDays;

        var members = await db.Members.ForTenant(tenant)
            .Where(m => m.Status == MemberStatus.Active || m.Status == MemberStatus.PastDue
                     || m.Status == MemberStatus.WonBack)
            .WhereIf(clubId is not null, m => m.HomeClubId == clubId)
            .ToListAsync();

        if (members.Count == 0) return 0;

        var memberIds = members.Select(m => m.Id).ToList();
        var ninetyDaysAgo = now.AddDays(-90);
        var thirtyDaysAgo = now.AddDays(-30);

        // Everything the model needs, gathered in five queries rather than five per member.
        var recentVisits = await db.CheckIns.ForTenant(tenant)
            .Where(c => c.MemberId != null && memberIds.Contains(c.MemberId.Value) && c.CheckedInAt >= ninetyDaysAgo)
            .Select(c => new { MemberId = c.MemberId!.Value, c.CheckedInAt })
            .ToListAsync();

        var upcomingBookings = await db.ClassBookings.ForTenant(tenant)
            .Where(b => b.MemberId != null && memberIds.Contains(b.MemberId.Value)
                     && b.Status == BookingStatus.Booked && b.ClassOccurrence!.StartsAt > now)
            .Select(b => b.MemberId!.Value)
            .Distinct()
            .ToListAsync();

        var upcomingAppointments = await db.Appointments.ForTenant(tenant)
            .Where(a => a.MemberId != null && memberIds.Contains(a.MemberId.Value)
                     && a.Status == AppointmentStatus.Confirmed && a.StartsAt > now)
            .Select(a => a.MemberId!.Value)
            .Distinct()
            .ToListAsync();

        var agreements = await db.Agreements.ForTenant(tenant)
            .Where(a => memberIds.Contains(a.MemberId)
                     && (a.Status == AgreementStatus.Active || a.Status == AgreementStatus.NoticeGiven))
            .Select(a => new { a.MemberId, a.Status, a.MinimumTermEndsOn, a.EndsOn, a.CancellationEffectiveOn })
            .ToListAsync();

        var failedPayments = await db.DunningCases.ForTenant(tenant)
            .Where(c => memberIds.Contains(c.MemberId) && c.Status == DunningCaseStatus.Open)
            .Select(c => c.MemberId)
            .Distinct()
            .ToListAsync();

        var openComplaints = await db.Complaints.ForTenant(tenant)
            .Where(c => c.MemberId != null && memberIds.Contains(c.MemberId.Value)
                     && c.Status != ComplaintStatus.Resolved && c.Status != ComplaintStatus.Closed)
            .Select(c => c.MemberId!.Value)
            .Distinct()
            .ToListAsync();

        var detractors = await db.NpsResponses.ForTenant(tenant)
            .Where(n => memberIds.Contains(n.MemberId) && n.Score <= 6 && n.RespondedAt >= ninetyDaysAgo)
            .Select(n => n.MemberId)
            .Distinct()
            .ToListAsync();

        var unusedCredits = await db.SessionCredits.ForTenant(tenant)
            .Where(c => memberIds.Contains(c.MemberId) && !c.IsExpired && c.Remaining > 0)
            .GroupBy(c => c.MemberId)
            .Select(g => new { MemberId = g.Key, Remaining = g.Sum(c => c.Remaining) })
            .ToListAsync();

        var strikes = await db.Strikes.ForTenant(tenant)
            .Where(s => memberIds.Contains(s.MemberId) && !s.IsWaived && s.ExpiresOn > now)
            .GroupBy(s => s.MemberId)
            .Select(g => new { MemberId = g.Key, Count = g.Count() })
            .ToListAsync();

        var coaches = await db.CoachAssignments.ForTenant(tenant)
            .Where(c => memberIds.Contains(c.MemberId) && c.EndedOn == null && c.IsPrimary)
            .Select(c => new { c.MemberId, c.StaffId })
            .ToListAsync();

        var existingScores = await db.ChurnScores.ForTenant(tenant)
            .Where(c => memberIds.Contains(c.MemberId))
            .Include(c => c.Factors)
            .ToListAsync();

        var scored = 0;

        foreach (var member in members)
        {
            var visits = recentVisits.Where(v => v.MemberId == member.Id).ToList();
            var lastVisit = visits.Count == 0 ? member.LastVisitOn : visits.Max(v => v.CheckedInAt);
            var daysSince = lastVisit is null ? 9999 : (int)(now.Date - lastVisit.Value.Date).TotalDays;

            var recent30 = visits.Count(v => v.CheckedInAt >= thirtyDaysAgo);
            var perWeekNow = Math.Round(recent30 / 4.3m, 2);
            var perWeekBaseline = member.VisitFrequencyBaseline > 0
                ? member.VisitFrequencyBaseline
                : Math.Round(visits.Count / 12.9m, 2);

            var tenureDays = member.JoinedOn is null ? 0 : (int)(now.Date - member.JoinedOn.Value.Date).TotalDays;
            var agreement = agreements.FirstOrDefault(a => a.MemberId == member.Id);

            var daysToEnd = agreement?.CancellationEffectiveOn is not null
                ? (int)(agreement.CancellationEffectiveOn.Value.Date - now.Date).TotalDays
                : agreement?.MinimumTermEndsOn is not null
                    ? (int)(agreement.MinimumTermEndsOn.Value.Date - now.Date).TotalDays
                    : 9999;

            var factors = new List<(ChurnFactorKind Kind, int Weight, string Explanation, string? Action)>();
            var score = 0;

            // ── Attendance ───────────────────────────────────────────────────

            if (member.TotalVisits == 0 && tenureDays > 7)
            {
                var weight = Math.Min(45, 25 + tenureDays / 2);
                score += weight;
                factors.Add((ChurnFactorKind.NeverAttendedAfterJoining, weight,
                    $"Joined {tenureDays} days ago and has never been in.",
                    "Call and book their induction — this is the most saveable member on the list."));
            }
            else if (daysSince >= criticalDays)
            {
                var weight = Math.Min(40, 25 + (daysSince - criticalDays) / 3);
                score += weight;
                factors.Add((ChurnFactorKind.NoRecentVisit, weight,
                    $"No visit in {daysSince} days.",
                    "Phone call, not a text — this far out, a message will be ignored."));
            }
            else if (daysSince >= riskDays)
            {
                var weight = 15 + (daysSince - riskDays);
                score += weight;
                factors.Add((ChurnFactorKind.NoRecentVisit, weight,
                    $"No visit in {daysSince} days.",
                    "A short, friendly check-in now is worth ten win-back calls in two months."));
            }

            // Measured against their own normal, not the club's.
            if (perWeekBaseline >= 1 && perWeekNow < perWeekBaseline * 0.5m && member.TotalVisits > 5)
            {
                var weight = 20;
                score += weight;
                factors.Add((ChurnFactorKind.VisitFrequencyDropped, weight,
                    $"Down to {perWeekNow:0.#} visits a week from {perWeekBaseline:0.#}.",
                    "Something changed — ask what, before they decide it was the gym."));
            }

            if (!upcomingBookings.Contains(member.Id) && !upcomingAppointments.Contains(member.Id))
            {
                var weight = 10;
                score += weight;
                factors.Add((ChurnFactorKind.NoUpcomingBooking, weight,
                    "Nothing booked in the diary.",
                    "Get one thing in the calendar — a booked class is the strongest predictor of the next visit."));
            }

            // ── Money ────────────────────────────────────────────────────────

            if (failedPayments.Contains(member.Id))
            {
                var weight = 25;
                score += weight;
                factors.Add((ChurnFactorKind.PaymentFailed, weight,
                    "A payment has failed and is still outstanding.",
                    "Often a card that expired rather than a decision — fixing it quietly saves the membership."));
            }
            else if (member.AccountBalance > 0)
            {
                var weight = 12;
                score += weight;
                factors.Add((ChurnFactorKind.OutstandingBalance, weight,
                    $"{member.AccountBalance:0.00} outstanding.",
                    "Clear it before it becomes a reason to leave."));
            }

            // ── Contract ─────────────────────────────────────────────────────

            if (agreement?.Status == AgreementStatus.NoticeGiven)
            {
                var weight = 50;
                score += weight;
                factors.Add((ChurnFactorKind.ContractEndingSoon, weight,
                    $"Notice given — leaving on {agreement.CancellationEffectiveOn:d MMM}.",
                    "Still savable until the effective date. Make the offer."));
            }
            else if (daysToEnd is > 0 and <= 45)
            {
                var weight = 15;
                score += weight;
                factors.Add((ChurnFactorKind.ContractEndingSoon, weight,
                    $"Minimum term ends in {daysToEnd} days.",
                    "Have the renewal conversation before they have it with themselves."));
            }

            // ── Experience ───────────────────────────────────────────────────

            if (openComplaints.Contains(member.Id))
            {
                var weight = 20;
                score += weight;
                factors.Add((ChurnFactorKind.ComplaintOpen, weight,
                    "They have an unresolved complaint.",
                    "Resolve it personally. An unanswered complaint is a decision being made."));
            }

            if (detractors.Contains(member.Id))
            {
                var weight = 15;
                score += weight;
                factors.Add((ChurnFactorKind.LowNpsScore, weight,
                    "Scored the club 6 or below recently.",
                    "Ask what would have made it a nine."));
            }

            var strikeCount = strikes.FirstOrDefault(s => s.MemberId == member.Id)?.Count ?? 0;
            if (strikeCount >= 2)
            {
                var weight = 10;
                score += weight;
                factors.Add((ChurnFactorKind.RepeatedNoShows, weight,
                    $"{strikeCount} late cancellations or no-shows recently.",
                    "Booking and not turning up is usually a scheduling problem, not a motivation one."));
            }

            var credits = unusedCredits.FirstOrDefault(c => c.MemberId == member.Id)?.Remaining ?? 0;
            if (credits > 0 && daysSince > riskDays)
            {
                var weight = 12;
                score += weight;
                factors.Add((ChurnFactorKind.CreditsUnused, weight,
                    $"{credits} paid-for sessions unused.",
                    "They have already paid — remind them, and they usually come back for them."));
            }

            // ── Tenure ───────────────────────────────────────────────────────
            // The first ninety days carry most of the industry's churn.

            if (tenureDays is > 0 and < 90 && member.TotalVisits < 8)
            {
                var weight = 15;
                score += weight;
                factors.Add((ChurnFactorKind.ShortTenure, weight,
                    $"Only {member.TotalVisits} visits in their first {tenureDays} days.",
                    "The habit has not formed yet. This is the window where a coach changes the outcome."));
            }

            var frozenSince = await db.Freezes.ForTenant(tenant)
                .Where(f => f.MemberId == member.Id && !f.IsReleased)
                .OrderBy(f => f.StartsOn)
                .Select(f => (DateTime?)f.StartsOn)
                .FirstOrDefaultAsync();

            if (frozenSince is not null && (now - frozenSince.Value).TotalDays > 60)
            {
                var weight = 18;
                score += weight;
                factors.Add((ChurnFactorKind.FrozenTooLong, weight,
                    $"Frozen since {frozenSince:d MMM} — over two months.",
                    "A long freeze usually ends in a cancellation unless somebody makes contact."));
            }

            score = Math.Clamp(score, 0, 100);

            var newBand = score switch
            {
                >= 60 => ChurnRiskBand.Critical,
                >= 35 => ChurnRiskBand.AtRisk,
                >= 15 => ChurnRiskBand.Watch,
                _ => ChurnRiskBand.Healthy,
            };

            // ── Persist ──────────────────────────────────────────────────────

            var existing = existingScores.FirstOrDefault(s => s.MemberId == member.Id);

            if (existing is null)
            {
                existing = new ChurnScore { MemberId = member.Id, ClubId = member.HomeClubId }.StampNew(tenant);
                db.ChurnScores.Add(existing);
            }
            else
            {
                existing.PreviousBand = existing.Band;
                existing.PreviousScore = existing.Score;
                foreach (var factor in existing.Factors.Where(f => !f.IsDeleted)) db.ChurnFactors.Remove(factor);
                existing.StampUpdated(Guid.Empty);
            }

            existing.ComputedOn = now;
            existing.Score = score;
            existing.Band = newBand;
            existing.BandWorsened = existing.PreviousBand is not null && newBand > existing.PreviousBand;
            existing.DaysSinceLastVisit = daysSince == 9999 ? 0 : daysSince;
            existing.VisitsPerWeekNow = perWeekNow;
            existing.VisitsPerWeekBaseline = perWeekBaseline;
            existing.TenureDays = tenureDays;
            existing.HasUpcomingBooking = upcomingBookings.Contains(member.Id) || upcomingAppointments.Contains(member.Id);
            existing.HasOutstandingBalance = member.AccountBalance > 0;
            existing.HasFailedPayment = failedPayments.Contains(member.Id);
            existing.DaysToContractEnd = daysToEnd == 9999 ? 0 : daysToEnd;
            existing.OwnerStaffId = coaches.FirstOrDefault(c => c.MemberId == member.Id)?.StaffId ?? member.AssignedCoachId;

            // A band that got worse is a new problem, so it goes back on the list.
            if (existing.BandWorsened) existing.IsActioned = false;

            foreach (var (kind, weight, explanation, action) in factors.OrderByDescending(f => f.Weight))
            {
                db.ChurnFactors.Add(new ChurnFactor
                {
                    ChurnScoreId = existing.Id,
                    Kind = kind,
                    Weight = weight,
                    Explanation = explanation,
                    SuggestedAction = action,
                }.StampNew(tenant));
            }

            member.RiskBand = newBand;
            member.VisitFrequencyBaseline = perWeekBaseline;

            // A member crossing into serious risk gets a task, not just a colour change.
            if (existing.BandWorsened && newBand is ChurnRiskBand.AtRisk or ChurnRiskBand.Critical)
            {
                var alreadyOpen = await db.RetentionTasks.ForTenant(tenant)
                    .AnyAsync(t => t.MemberId == member.Id && t.CompletedAt == null && !t.IsDismissed);

                if (!alreadyOpen)
                {
                    db.RetentionTasks.Add(new RetentionTask
                    {
                        MemberId = member.Id,
                        ClubId = member.HomeClubId,
                        AssignedStaffId = existing.OwnerStaffId,
                        Title = newBand == ChurnRiskBand.Critical
                            ? $"Call {member.FirstName} — high risk of leaving"
                            : $"Check in with {member.FirstName}",
                        Detail = factors.OrderByDescending(f => f.Weight).FirstOrDefault().Explanation,
                        Trigger = "Churn scoring",
                        DueOn = newBand == ChurnRiskBand.Critical ? now.Date : now.Date.AddDays(2),
                        Priority = newBand == ChurnRiskBand.Critical ? 1 : 2,
                        ChurnScoreId = existing.Id,
                    }.StampNew(tenant));
                }
            }

            scored++;
        }

        await db.SaveChangesAsync();
        return scored;
    }

    // ── Tasks ────────────────────────────────────────────────────────────────

    public async Task<List<RetentionTaskDto>> GetTasksAsync(Guid? clubId, Guid? staffId, bool openOnly)
    {
        var now = DateTime.UtcNow;

        var tasks = await db.RetentionTasks.ForTenant(tenant)
            .WhereIf(clubId is not null, t => t.ClubId == clubId)
            .WhereIf(staffId is not null, t => t.AssignedStaffId == staffId)
            .WhereIf(openOnly, t => t.CompletedAt == null && !t.IsDismissed)
            .Include(t => t.Member)
            .OrderBy(t => t.Priority).ThenBy(t => t.DueOn)
            .Take(200)
            .ToListAsync();

        var staffNames = await db.Staff.ForTenant(tenant)
            .Select(s => new { s.Id, Name = s.FirstName + " " + s.LastName })
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        return [.. tasks.Select(t =>
        {
            var dto = FitnessMapper.ToDto(t, now);
            if (t.AssignedStaffId is not null) dto.AssignedStaffName = staffNames.GetValueOrDefault(t.AssignedStaffId.Value);
            if (t.CompletedByStaffId is not null) dto.CompletedByName = staffNames.GetValueOrDefault(t.CompletedByStaffId.Value);
            return dto;
        })];
    }

    public async Task<RetentionTaskDto> CreateTaskAsync(RetentionTaskDto request, Guid userId)
    {
        var task = new RetentionTask
        {
            MemberId = request.MemberId,
            ClubId = request.ClubId,
            AssignedStaffId = request.AssignedStaffId,
            Title = request.Title,
            Detail = request.Detail,
            Trigger = request.Trigger ?? "Manual",
            DueOn = request.DueOn == default ? DateTime.UtcNow.Date : request.DueOn,
            Priority = request.Priority == 0 ? 2 : request.Priority,
        }.StampNew(tenant, userId);

        db.RetentionTasks.Add(task);
        await db.SaveChangesAsync();

        var saved = await db.RetentionTasks.ForTenant(tenant)
            .Include(t => t.Member)
            .FirstAsync(t => t.Id == task.Id);

        return FitnessMapper.ToDto(saved, DateTime.UtcNow);
    }

    /// <summary>
    /// Completing a retention task.
    ///
    /// The outcome goes on the member's timeline as well as the task, because the next person to
    /// open that record needs to know somebody already called and what was said — otherwise the
    /// member gets rung twice in a week, which is worse than not being rung at all.
    /// </summary>
    public async Task<RetentionTaskDto> CompleteTaskAsync(CompleteTaskDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var task = await db.RetentionTasks.ForTenant(tenant)
            .Include(t => t.Member)
            .FirstOrDefaultAsync(t => t.Id == request.TaskId)
            ?? throw new InvalidOperationException("Task not found.");

        if (request.Dismiss)
        {
            task.IsDismissed = true;
            task.DismissReason = request.DismissReason;
        }
        else
        {
            task.CompletedAt = now;
            task.CompletedByStaffId = userId == Guid.Empty ? null : userId;
            task.Outcome = request.Outcome;
        }

        task.StampUpdated(userId);

        if (request.LogOnMemberTimeline && !request.Dismiss)
        {
            db.MemberNotes.Add(new MemberNote
            {
                MemberId = task.MemberId,
                Kind = InteractionKind.Call,
                Body = $"{task.Title} — {request.Outcome}",
                OccurredAt = now,
                StaffId = userId == Guid.Empty ? null : userId,
                RelatedEntityId = task.Id,
                RelatedEntityType = nameof(RetentionTask),
            }.StampNew(tenant, userId));
        }

        // Mark the score actioned so the member drops off today's list without losing the history.
        if (task.ChurnScoreId is not null)
        {
            var score = await db.ChurnScores.ForTenant(tenant)
                .FirstOrDefaultAsync(c => c.Id == task.ChurnScoreId);

            if (score is not null)
            {
                score.IsActioned = true;
                score.ActionedOn = now;
            }
        }

        if (request.FollowUpOn is not null)
        {
            db.RetentionTasks.Add(new RetentionTask
            {
                MemberId = task.MemberId,
                ClubId = task.ClubId,
                AssignedStaffId = task.AssignedStaffId,
                Title = $"Follow up — {task.Title}",
                Detail = request.Outcome,
                Trigger = "Follow-up",
                DueOn = request.FollowUpOn.Value,
                Priority = task.Priority,
            }.StampNew(tenant, userId));
        }

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(task, now);
    }

    // ── Journeys ─────────────────────────────────────────────────────────────

    public async Task<List<EngagementJourneyDto>> GetJourneysAsync(Guid? clubId)
    {
        var journeys = await db.Journeys.ForTenant(tenant)
            .WhereIf(clubId is not null, j => j.ClubId == clubId || j.ClubId == null)
            .Include(j => j.Steps.Where(s => !s.IsDeleted))
            .OrderByDescending(j => j.IsActive).ThenBy(j => j.Name)
            .ToListAsync();

        var enrolled = await db.JourneyEnrolments.ForTenant(tenant)
            .Where(e => e.CompletedAt == null && e.ExitedAt == null)
            .GroupBy(e => e.EngagementJourneyId)
            .Select(g => new { JourneyId = g.Key, Count = g.Count() })
            .ToListAsync();

        var templates = await db.MessageTemplates.ForTenant(tenant)
            .Select(t => new { t.Id, t.Name }).ToDictionaryAsync(t => t.Id, t => t.Name);

        var segments = await db.Segments.ForTenant(tenant)
            .Select(s => new { s.Id, s.Name }).ToDictionaryAsync(s => s.Id, s => s.Name);

        return [.. journeys.Select(j =>
        {
            var dto = FitnessMapper.ToDto(j);
            dto.CurrentlyEnrolled = enrolled.FirstOrDefault(e => e.JourneyId == j.Id)?.Count ?? 0;
            if (j.SegmentId is not null) dto.SegmentName = segments.GetValueOrDefault(j.SegmentId.Value);

            foreach (var step in dto.Steps.Where(s => s.MessageTemplateId is not null))
                step.MessageTemplateName = templates.GetValueOrDefault(step.MessageTemplateId!.Value);

            return dto;
        })];
    }

    public async Task<EngagementJourneyDto> SaveJourneyAsync(Guid? id, EngagementJourneyDto request, Guid userId)
    {
        EngagementJourney journey;
        if (id is null)
        {
            // Created switched off. An automation that starts messaging the moment somebody saves
            // a draft is how a club sends four hundred wrong emails.
            journey = new EngagementJourney { IsActive = false }.StampNew(tenant, userId);
            db.Journeys.Add(journey);
        }
        else
        {
            journey = await db.Journeys.ForTenant(tenant)
                .Include(j => j.Steps.Where(s => !s.IsDeleted))
                .FirstOrDefaultAsync(j => j.Id == id)
                ?? throw new InvalidOperationException("Journey not found.");
            journey.StampUpdated(userId);
        }

        journey.Name = request.Name;
        journey.ClubId = request.ClubId;
        journey.Trigger = request.Trigger;
        journey.TriggerThresholdDays = request.TriggerThresholdDays;
        journey.SegmentId = request.SegmentId;
        journey.PreventReEnrolment = request.PreventReEnrolment;
        journey.ReEnrolmentCooldownDays = request.ReEnrolmentCooldownDays;
        journey.SuccessMetric = request.SuccessMetric;

        foreach (var existing in journey.Steps.Where(s => !s.IsDeleted)) existing.StampDeleted(userId);

        var number = 1;
        foreach (var stepDto in request.Steps.OrderBy(s => s.StepNumber))
        {
            db.JourneySteps.Add(new JourneyStep
            {
                EngagementJourneyId = journey.Id,
                StepNumber = number++,
                Kind = stepDto.Kind,
                DelayHours = stepDto.DelayHours,
                Channel = stepDto.Channel,
                MessageTemplateId = stepDto.MessageTemplateId,
                ConditionExpression = stepDto.ConditionExpression,
                OnFalseStepNumber = stepDto.OnFalseStepNumber,
                TagToApply = stepDto.TagToApply,
                OfferPromotionRuleId = stepDto.OfferPromotionRuleId,
                LoyaltyPointsToGrant = stepDto.LoyaltyPointsToGrant,
                TaskTitle = stepDto.TaskTitle,
                TaskAssignStaffId = stepDto.TaskAssignStaffId,
            }.StampNew(tenant, userId));
        }

        await db.SaveChangesAsync();
        return (await GetJourneysAsync(request.ClubId)).First(j => j.Id == journey.Id);
    }

    public async Task<EngagementJourneyDto> SetJourneyActiveAsync(Guid id, bool active, Guid userId)
    {
        var journey = await db.Journeys.ForTenant(tenant)
            .Include(j => j.Steps.Where(s => !s.IsDeleted))
            .FirstOrDefaultAsync(j => j.Id == id)
            ?? throw new InvalidOperationException("Journey not found.");

        if (active && journey.Steps.Count(s => !s.IsDeleted) == 0)
            throw new InvalidOperationException("A journey with no steps has nothing to do. Add at least one.");

        journey.IsActive = active;
        journey.StampUpdated(userId);

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(journey);
    }

    public async Task<List<JourneyEnrolmentDto>> GetEnrolmentsAsync(Guid journeyId, bool activeOnly)
    {
        var enrolments = await db.JourneyEnrolments.ForTenant(tenant)
            .Where(e => e.EngagementJourneyId == journeyId)
            .WhereIf(activeOnly, e => e.CompletedAt == null && e.ExitedAt == null)
            .Include(e => e.Member)
            .Include(e => e.EngagementJourney)
            .OrderByDescending(e => e.EnrolledAt)
            .Take(500)
            .ToListAsync();

        var stepCount = await db.JourneySteps.ForTenant(tenant)
            .CountAsync(s => s.EngagementJourneyId == journeyId);

        return [.. enrolments.Select(e => new JourneyEnrolmentDto
        {
            Id = e.Id,
            EngagementJourneyId = e.EngagementJourneyId,
            JourneyName = e.EngagementJourney?.Name,
            MemberId = e.MemberId,
            MemberName = e.Member is null ? null : FitnessMapper.FullName(e.Member),
            EnrolledAt = e.EnrolledAt,
            CurrentStep = e.CurrentStep,
            TotalSteps = stepCount,
            NextStepDueAt = e.NextStepDueAt,
            CompletedAt = e.CompletedAt,
            ExitedAt = e.ExitedAt,
            ExitReason = e.ExitReason,
            WasSuccessful = e.WasSuccessful,
            IsPaused = e.IsPaused,
        })];
    }

    /// <summary>
    /// Enrols members whose trigger has fired, and advances everyone mid-journey. Runs hourly.
    ///
    /// The success check comes first: a member who came back on their own should leave the "we
    /// miss you" sequence immediately rather than receive the rest of it.
    /// </summary>
    public async Task<int> ProcessJourneysAsync()
    {
        var now = DateTime.UtcNow;
        var actions = 0;

        var journeys = await db.Journeys.ForTenant(tenant)
            .Where(j => j.IsActive)
            .Include(j => j.Steps.Where(s => !s.IsDeleted))
            .ToListAsync();

        foreach (var journey in journeys) actions += await EnrolTriggeredAsync(journey, now);

        // ── Advance ──────────────────────────────────────────────────────────

        var due = await db.JourneyEnrolments.ForTenant(tenant)
            .Where(e => e.CompletedAt == null && e.ExitedAt == null && !e.IsPaused
                     && e.NextStepDueAt != null && e.NextStepDueAt <= now)
            .Include(e => e.Member)
            .Include(e => e.EngagementJourney).ThenInclude(j => j!.Steps.Where(s => !s.IsDeleted))
            .Take(1000)
            .ToListAsync();

        foreach (var enrolment in due)
        {
            var journey = enrolment.EngagementJourney;
            if (journey is null) continue;

            if (await HasSucceededAsync(journey, enrolment, now))
            {
                enrolment.WasSuccessful = true;
                enrolment.SuccessAt = now;
                enrolment.CompletedAt = now;
                enrolment.ExitReason = "The member did the thing this journey was asking for";
                journey.SuccessCount++;
                journey.CompletedCount++;
                actions++;
                continue;
            }

            var step = journey.Steps
                .Where(s => s.StepNumber > enrolment.CurrentStep)
                .OrderBy(s => s.StepNumber)
                .FirstOrDefault();

            if (step is null)
            {
                enrolment.CompletedAt = now;
                journey.CompletedCount++;
                actions++;
                continue;
            }

            var nextStepNumber = await ExecuteStepAsync(journey, step, enrolment, now);

            enrolment.CurrentStep = step.StepNumber;

            var following = nextStepNumber is null
                ? journey.Steps.Where(s => s.StepNumber > step.StepNumber).OrderBy(s => s.StepNumber).FirstOrDefault()
                : journey.Steps.FirstOrDefault(s => s.StepNumber == nextStepNumber);

            if (following is null)
            {
                enrolment.CompletedAt = now;
                journey.CompletedCount++;
            }
            else
            {
                enrolment.NextStepDueAt = now.AddHours(following.DelayHours);
            }

            enrolment.StampUpdated(Guid.Empty);
            actions++;
        }

        await db.SaveChangesAsync();
        return actions;
    }

    // ── Campaigns ────────────────────────────────────────────────────────────

    public async Task<PaginatedResponse<CampaignDto>> ListCampaignsAsync(Guid? clubId, PaginationParams pagination)
    {
        var query = db.Campaigns.ForTenant(tenant)
            .WhereIf(clubId is not null, c => c.ClubId == clubId || c.ClubId == null);

        var total = await query.CountAsync();

        var page = await query
            .OrderByDescending(c => c.SentAt ?? c.ScheduledFor ?? c.CreatedAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        var templates = await db.MessageTemplates.ForTenant(tenant)
            .Select(t => new { t.Id, t.Name }).ToDictionaryAsync(t => t.Id, t => t.Name);

        var segments = await db.Segments.ForTenant(tenant)
            .Select(s => new { s.Id, s.Name }).ToDictionaryAsync(s => s.Id, s => s.Name);

        var items = page.Select(c =>
        {
            var dto = FitnessMapper.ToDto(c);
            if (c.MessageTemplateId is not null) dto.MessageTemplateName = templates.GetValueOrDefault(c.MessageTemplateId.Value);
            if (c.SegmentId is not null) dto.SegmentName = segments.GetValueOrDefault(c.SegmentId.Value);
            return dto;
        }).ToList();

        return PaginatedResponse<CampaignDto>.Ok(
                   items,
                   total,
                   pagination.PageNumber,
                   pagination.PageSize);
    }

    public async Task<CampaignDto> SaveCampaignAsync(Guid? id, CampaignDto request, Guid userId)
    {
        Campaign campaign;
        if (id is null)
        {
            campaign = new Campaign().StampNew(tenant, userId);
            db.Campaigns.Add(campaign);
        }
        else
        {
            campaign = await db.Campaigns.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new InvalidOperationException("Campaign not found.");

            if (campaign.IsSent)
                throw new InvalidOperationException("A campaign that has already gone out cannot be edited.");

            campaign.StampUpdated(userId);
        }

        campaign.Name = request.Name;
        campaign.ClubId = request.ClubId;
        campaign.Channel = request.Channel;
        campaign.MessageTemplateId = request.MessageTemplateId;
        campaign.SegmentId = request.SegmentId;
        campaign.ScheduledFor = request.ScheduledFor;
        campaign.Cost = request.Cost;
        campaign.PromotionRuleId = request.PromotionRuleId;

        // The audience size, so a manager sees who this is about to reach before it goes.
        if (request.SegmentId is not null)
        {
            var segment = await db.Segments.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == request.SegmentId);
            if (segment is not null)
            {
                var members = await EvaluateSegmentAsync(segment);
                campaign.RecipientCount = members.Count;
                segment.LastCount = members.Count;
                segment.LastCountedAt = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(campaign);
    }

    /// <summary>
    /// Sending a campaign.
    ///
    /// Consent is enforced per member per channel, and suppression is *recorded* rather than
    /// silent — "why didn't they get it?" has to be answerable, and "they withdrew SMS consent in
    /// March" is the answer. Quiet hours defer rather than drop, so nobody is texted at 3am.
    /// </summary>
    public async Task<CampaignDto> SendCampaignAsync(SendCampaignDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var campaign = await db.Campaigns.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == request.CampaignId)
            ?? throw new InvalidOperationException("Campaign not found.");

        if (campaign.IsSent && !request.TestSendOnly)
            throw new InvalidOperationException("This campaign has already been sent.");

        var template = campaign.MessageTemplateId is null
            ? null
            : await db.MessageTemplates.ForTenant(tenant).FirstOrDefaultAsync(t => t.Id == campaign.MessageTemplateId);

        if (request.TestSendOnly)
        {
            db.MessageLog.Add(new MessageLog
            {
                ClubId = campaign.ClubId ?? Guid.Empty,
                Channel = campaign.Channel,
                Status = MessageStatus.Queued,
                MessageTemplateId = campaign.MessageTemplateId,
                CampaignId = campaign.Id,
                Recipient = request.TestRecipient,
                Subject = $"[TEST] {template?.Subject ?? campaign.Name}",
                BodyPreview = Preview(template?.Body),
                QueuedAt = now,
            }.StampNew(tenant, userId));

            await db.SaveChangesAsync();
            return FitnessMapper.ToDto(campaign);
        }

        if (request.ScheduleFor is not null)
        {
            campaign.ScheduledFor = request.ScheduleFor;
            await db.SaveChangesAsync();
            return FitnessMapper.ToDto(campaign);
        }

        var segment = campaign.SegmentId is null
            ? null
            : await db.Segments.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == campaign.SegmentId);

        var recipients = segment is null ? [] : await EvaluateSegmentAsync(segment);

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync() ?? new FitnessSettings();
        var isTransactional = template?.IsTransactional ?? false;

        foreach (var member in recipients)
        {
            var (allowed, reason) = await CheckConsentAsync(member, campaign.Channel, isTransactional);

            var recipient = campaign.Channel switch
            {
                MessageChannel.Email => member.Email,
                MessageChannel.Sms or MessageChannel.WhatsApp => member.Phone,
                _ => member.Id.ToString(),
            };

            if (!allowed || recipient is null)
            {
                campaign.SuppressedCount++;

                db.MessageLog.Add(new MessageLog
                {
                    MemberId = member.Id,
                    ClubId = campaign.ClubId ?? member.HomeClubId,
                    Channel = campaign.Channel,
                    Status = MessageStatus.SuppressedNoConsent,
                    MessageTemplateId = campaign.MessageTemplateId,
                    CampaignId = campaign.Id,
                    Subject = template?.Subject ?? campaign.Name,
                    FailureReason = recipient is null ? $"No {campaign.Channel} address on file" : reason,
                    QueuedAt = now,
                }.StampNew(tenant, userId));

                continue;
            }

            // Quiet hours defer rather than drop: the member still gets it, just not at 3am.
            var deferred = settings.RespectQuietHours
                           && campaign.Channel is MessageChannel.Sms or MessageChannel.Push or MessageChannel.WhatsApp
                           && FitnessQueryExtensions.WithinWindow(now.TimeOfDay, settings.QuietHoursFrom, settings.QuietHoursTo);

            db.MessageLog.Add(new MessageLog
            {
                MemberId = member.Id,
                ClubId = campaign.ClubId ?? member.HomeClubId,
                Channel = campaign.Channel,
                Status = deferred ? MessageStatus.Deferred : MessageStatus.Queued,
                MessageTemplateId = campaign.MessageTemplateId,
                CampaignId = campaign.Id,
                Recipient = recipient,
                Subject = Merge(template?.Subject, member),
                BodyPreview = Preview(Merge(template?.Body, member)),
                QueuedAt = now,
            }.StampNew(tenant, userId));

            campaign.SentCount++;
        }

        campaign.RecipientCount = recipients.Count;
        campaign.SentAt = now;
        campaign.IsSent = true;
        campaign.StampUpdated(userId);

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(campaign);
    }

    public async Task<List<MessageTemplateDto>> GetTemplatesAsync(Guid? clubId, MessageChannel? channel)
    {
        var templates = await db.MessageTemplates.ForTenant(tenant)
            .WhereIf(clubId is not null, t => t.ClubId == clubId || t.ClubId == null)
            .WhereIf(channel is not null, t => t.Channel == channel)
            .OrderBy(t => t.Purpose).ThenBy(t => t.Name)
            .ToListAsync();

        var usage = await db.MessageLog.ForTenant(tenant)
            .Where(m => m.MessageTemplateId != null)
            .GroupBy(m => m.MessageTemplateId!.Value)
            .Select(g => new { TemplateId = g.Key, Count = g.Count() })
            .ToListAsync();

        return [.. templates.Select(t =>
        {
            var dto = FitnessMapper.ToDto(t);
            dto.UseCount = usage.FirstOrDefault(u => u.TemplateId == t.Id)?.Count ?? 0;
            return dto;
        })];
    }

    public async Task<MessageTemplateDto> SaveTemplateAsync(Guid? id, MessageTemplateDto request, Guid userId)
    {
        MessageTemplate template;
        if (id is null)
        {
            template = new MessageTemplate().StampNew(tenant, userId);
            db.MessageTemplates.Add(template);
        }
        else
        {
            template = await db.MessageTemplates.ForTenant(tenant).FirstOrDefaultAsync(t => t.Id == id)
                ?? throw new InvalidOperationException("Template not found.");
            template.StampUpdated(userId);
        }

        template.Name = request.Name;
        template.ClubId = request.ClubId;
        template.Channel = request.Channel;
        template.Subject = request.Subject;
        template.Body = request.Body;
        template.PlainTextBody = request.PlainTextBody;
        template.LanguageCode = request.LanguageCode;
        template.Purpose = request.Purpose;
        template.IsTransactional = request.IsTransactional;
        template.IsActive = request.IsActive;

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(template);
    }

    // ── Segments ─────────────────────────────────────────────────────────────

    public async Task<List<SegmentDto>> GetSegmentsAsync(Guid? clubId)
    {
        var segments = await db.Segments.ForTenant(tenant)
            .WhereIf(clubId is not null, s => s.ClubId == clubId || s.ClubId == null)
            .OrderBy(s => s.Name)
            .ToListAsync();

        return [.. segments.Select(FitnessMapper.ToDto)];
    }

    public async Task<SegmentDto> SaveSegmentAsync(Guid? id, SaveSegmentDto request, Guid userId)
    {
        Segment segment;
        if (id is null)
        {
            segment = new Segment().StampNew(tenant, userId);
            db.Segments.Add(segment);
        }
        else
        {
            segment = await db.Segments.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == id)
                ?? throw new InvalidOperationException("Segment not found.");
            segment.StampUpdated(userId);
        }

        segment.Name = request.Name;
        segment.ClubId = request.ClubId;
        segment.DefinitionJson = request.DefinitionJson;
        segment.Description = request.Description;

        var members = await EvaluateSegmentAsync(segment);
        segment.LastCount = members.Count;
        segment.LastCountedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(segment);
    }

    public async Task<PaginatedResponse<MemberSummaryDto>> PreviewSegmentAsync(Guid segmentId, PaginationParams pagination)
    {
        var now = DateTime.UtcNow;

        var segment = await db.Segments.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == segmentId)
            ?? throw new InvalidOperationException("Segment not found.");

        var members = await EvaluateSegmentAsync(segment);

        var page = members
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToList();

        return PaginatedResponse<MemberSummaryDto>.Ok(
                   [.. page.Select(m => FitnessMapper.ToSummary(m, now))],
                   members.Count,
                   pagination.PageNumber,
                   pagination.PageSize);
    }

    public async Task<PaginatedResponse<MessageLogDto>> GetMessageLogAsync(
        Guid? clubId, Guid? memberId, MessageChannel? channel, MessageStatus? status,
        DateTime? from, DateTime? to, PaginationParams pagination)
    {
        var query = db.MessageLog.ForTenant(tenant)
            .WhereIf(clubId is not null, m => m.ClubId == clubId)
            .WhereIf(memberId is not null, m => m.MemberId == memberId)
            .WhereIf(channel is not null, m => m.Channel == channel)
            .WhereIf(status is not null, m => m.Status == status)
            .WhereIf(from is not null, m => m.QueuedAt >= from)
            .WhereIf(to is not null, m => m.QueuedAt <= to);

        var total = await query.CountAsync();

        var page = await query
            .OrderByDescending(m => m.QueuedAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        var memberIds = page.Where(m => m.MemberId is not null).Select(m => m.MemberId!.Value).Distinct().ToList();
        var names = await db.Members.ForTenant(tenant)
            .Where(m => memberIds.Contains(m.Id))
            .Select(m => new { m.Id, Name = m.FirstName + " " + m.LastName })
            .ToDictionaryAsync(m => m.Id, m => m.Name);

        var templates = await db.MessageTemplates.ForTenant(tenant)
            .Select(t => new { t.Id, t.Name }).ToDictionaryAsync(t => t.Id, t => t.Name);

        var campaigns = await db.Campaigns.ForTenant(tenant)
            .Select(c => new { c.Id, c.Name }).ToDictionaryAsync(c => c.Id, c => c.Name);

        var items = page.Select(m =>
        {
            var dto = FitnessMapper.ToDto(m);
            if (m.MemberId is not null) dto.MemberName = names.GetValueOrDefault(m.MemberId.Value);
            if (m.MessageTemplateId is not null) dto.TemplateName = templates.GetValueOrDefault(m.MessageTemplateId.Value);
            if (m.CampaignId is not null) dto.CampaignName = campaigns.GetValueOrDefault(m.CampaignId.Value);
            return dto;
        }).ToList();

        return PaginatedResponse<MessageLogDto>.Ok(
                   items,
                   total,
                   pagination.PageNumber,
                   pagination.PageSize);
    }

    // ── Loyalty ──────────────────────────────────────────────────────────────

    public async Task<LoyaltyAccountDto?> GetLoyaltyAsync(Guid memberId)
    {
        var account = await db.LoyaltyAccounts.ForTenant(tenant)
            .Include(a => a.Member)
            .Include(a => a.Tier)
            .FirstOrDefaultAsync(a => a.MemberId == memberId);

        if (account is null) return null;

        var dto = FitnessMapper.ToDto(account);

        var nextTier = await db.LoyaltyTiers.ForTenant(tenant)
            .Where(t => t.PointsRequired > account.PointsBalance && t.IsActive)
            .OrderBy(t => t.PointsRequired)
            .FirstOrDefaultAsync();

        if (nextTier is not null)
        {
            dto.NextTierName = nextTier.Name;
            dto.PointsToNextTier = nextTier.PointsRequired - account.PointsBalance;
        }

        dto.RecentTransactions = [.. (await db.LoyaltyTransactions.ForTenant(tenant)
            .Where(t => t.MemberId == memberId)
            .OrderByDescending(t => t.OccurredAt)
            .Take(25)
            .ToListAsync())
            .Select(FitnessMapper.ToDto)];

        return dto;
    }

    public async Task<LoyaltyTransactionDto> AwardPointsAsync(AwardPointsDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var account = await db.LoyaltyAccounts.ForTenant(tenant)
            .Include(a => a.Tier)
            .FirstOrDefaultAsync(a => a.MemberId == request.MemberId);

        if (account is null)
        {
            var member = await db.Members.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == request.MemberId)
                ?? throw new InvalidOperationException("Member not found.");

            account = new LoyaltyAccount { MemberId = request.MemberId, ClubId = member.HomeClubId }.StampNew(tenant, userId);
            db.LoyaltyAccounts.Add(account);
        }

        // A tier multiplier is the point of tiers — earning faster is the benefit.
        var points = account.Tier is null
            ? request.Points
            : (int)Math.Round(request.Points * account.Tier.EarnMultiplier);

        account.PointsBalance += points;
        account.LifetimePoints += points;
        account.StampUpdated(userId);

        var transaction = new LoyaltyTransaction
        {
            LoyaltyAccountId = account.Id,
            MemberId = request.MemberId,
            Kind = request.Kind,
            OccurredAt = now,
            Points = points,
            BalanceAfter = account.PointsBalance,
            Reason = request.Reason,
            ExpiresOn = request.ExpiresOn,
            AwardedByStaffId = userId == Guid.Empty ? null : userId,
        }.StampNew(tenant, userId);

        db.LoyaltyTransactions.Add(transaction);

        await UpdateTierAsync(account, userId);
        await SyncMemberPointsAsync(request.MemberId, account.PointsBalance, userId);

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(transaction);
    }

    public async Task<LoyaltyTransactionDto> RedeemPointsAsync(RedeemPointsDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var account = await db.LoyaltyAccounts.ForTenant(tenant)
            .FirstOrDefaultAsync(a => a.MemberId == request.MemberId)
            ?? throw new InvalidOperationException("This member has no loyalty account.");

        if (account.PointsBalance < request.Points)
            throw new InvalidOperationException(
                $"Only {account.PointsBalance} points available — {request.Points} were requested.");

        account.PointsBalance -= request.Points;
        account.PointsRedeemed += request.Points;
        account.StampUpdated(userId);

        var transaction = new LoyaltyTransaction
        {
            LoyaltyAccountId = account.Id,
            MemberId = request.MemberId,
            Kind = LoyaltyEventKind.Redemption,
            OccurredAt = now,
            Points = -request.Points,
            BalanceAfter = account.PointsBalance,
            Reason = request.Reason,
            RedemptionValue = request.RedemptionValue,
            SourceEntityId = request.SaleId,
            AwardedByStaffId = userId == Guid.Empty ? null : userId,
        }.StampNew(tenant, userId);

        db.LoyaltyTransactions.Add(transaction);

        if (request.ApplyToBalance && request.RedemptionValue > 0)
        {
            var member = await db.Members.ForTenant(tenant).FirstAsync(m => m.Id == request.MemberId);
            member.CreditBalance += request.RedemptionValue;
            member.StampUpdated(userId);
        }

        await SyncMemberPointsAsync(request.MemberId, account.PointsBalance, userId);
        await db.SaveChangesAsync();

        return FitnessMapper.ToDto(transaction);
    }

    public async Task<List<LoyaltyTierDto>> GetTiersAsync(Guid? clubId)
    {
        var tiers = await db.LoyaltyTiers.ForTenant(tenant)
            .WhereIf(clubId is not null, t => t.ClubId == clubId || t.ClubId == null)
            .OrderBy(t => t.Ordinal)
            .ToListAsync();

        var counts = await db.LoyaltyAccounts.ForTenant(tenant)
            .Where(a => a.TierId != null)
            .GroupBy(a => a.TierId!.Value)
            .Select(g => new { TierId = g.Key, Count = g.Count() })
            .ToListAsync();

        return [.. tiers.Select(t =>
        {
            var dto = FitnessMapper.ToDto(t);
            dto.MemberCount = counts.FirstOrDefault(c => c.TierId == t.Id)?.Count ?? 0;
            return dto;
        })];
    }

    public async Task<LoyaltyTierDto> SaveTierAsync(Guid? id, LoyaltyTierDto request, Guid userId)
    {
        LoyaltyTier tier;
        if (id is null)
        {
            tier = new LoyaltyTier().StampNew(tenant, userId);
            db.LoyaltyTiers.Add(tier);
        }
        else
        {
            tier = await db.LoyaltyTiers.ForTenant(tenant).FirstOrDefaultAsync(t => t.Id == id)
                ?? throw new InvalidOperationException("Tier not found.");
            tier.StampUpdated(userId);
        }

        tier.Name = request.Name;
        tier.ClubId = request.ClubId;
        tier.Ordinal = request.Ordinal;
        tier.PointsRequired = request.PointsRequired;
        tier.ColourHex = request.ColourHex;
        tier.BadgeUrl = request.BadgeUrl;
        tier.EarnMultiplier = request.EarnMultiplier <= 0 ? 1 : request.EarnMultiplier;
        tier.Benefits = request.Benefits;
        tier.RetentionMonths = request.RetentionMonths;
        tier.IsActive = request.IsActive;

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(tier);
    }

    // ── Challenges & badges ──────────────────────────────────────────────────

    public async Task<List<ChallengeDto>> GetChallengesAsync(Guid? clubId, bool activeOnly, Guid? viewerMemberId)
    {
        var now = DateTime.UtcNow;

        var challenges = await db.Challenges.ForTenant(tenant)
            .WhereIf(clubId is not null, c => c.ClubId == clubId || c.ClubId == null)
            .WhereIf(activeOnly, c => c.IsPublished && c.EndsOn >= now.Date)
            .Include(c => c.Participants.Where(p => !p.IsDeleted)).ThenInclude(p => p.Member)
            .OrderByDescending(c => c.StartsOn)
            .ToListAsync();

        return [.. challenges.Select(c =>
        {
            var dto = FitnessMapper.ToDto(c, now);

            dto.TopParticipants = [.. c.Participants
                .Where(p => !p.IsDeleted && p.Member?.LeaderboardOptIn != false)
                .OrderByDescending(p => p.CurrentValue)
                .Take(10)
                .Select(p => FitnessMapper.ToDto(p, c.TargetValue))];

            if (viewerMemberId is not null)
            {
                var mine = c.Participants.FirstOrDefault(p => p.MemberId == viewerMemberId);
                if (mine is not null) dto.ViewerEntry = FitnessMapper.ToDto(mine, c.TargetValue);
            }

            return dto;
        })];
    }

    public async Task<ChallengeDto> SaveChallengeAsync(Guid? id, ChallengeDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        Challenge challenge;
        if (id is null)
        {
            challenge = new Challenge().StampNew(tenant, userId);
            db.Challenges.Add(challenge);
        }
        else
        {
            challenge = await db.Challenges.ForTenant(tenant)
                .Include(c => c.Participants.Where(p => !p.IsDeleted)).ThenInclude(p => p.Member)
                .FirstOrDefaultAsync(c => c.Id == id)
                ?? throw new InvalidOperationException("Challenge not found.");
            challenge.StampUpdated(userId);
        }

        challenge.Name = request.Name;
        challenge.ClubId = request.ClubId;
        challenge.Blurb = request.Blurb;
        challenge.ImageUrl = request.ImageUrl;
        challenge.Metric = request.Metric;
        challenge.CustomMetricName = request.CustomMetricName;
        challenge.Unit = request.Unit;
        challenge.StartsOn = request.StartsOn.Date;
        challenge.EndsOn = request.EndsOn.Date;
        challenge.TargetValue = request.TargetValue;
        challenge.IsTeamBased = request.IsTeamBased;
        challenge.IsOpenToAll = request.IsOpenToAll;
        challenge.SegmentId = request.SegmentId;
        challenge.EntryFee = request.EntryFee;
        challenge.Prize = request.Prize;
        challenge.PointsForCompletion = request.PointsForCompletion;
        challenge.IsPublished = request.IsPublished;
        challenge.IsActive = request.IsActive;

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(challenge, now);
    }

    public async Task<ChallengeParticipantDto> JoinChallengeAsync(
        Guid challengeId, Guid memberId, string? teamName, Guid userId)
    {
        var now = DateTime.UtcNow;

        var challenge = await db.Challenges.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == challengeId)
            ?? throw new InvalidOperationException("Challenge not found.");

        if (challenge.EndsOn < now.Date)
            throw new InvalidOperationException("That challenge has finished.");

        var existing = await db.ChallengeParticipants.ForTenant(tenant)
            .Include(p => p.Member)
            .FirstOrDefaultAsync(p => p.ChallengeId == challengeId && p.MemberId == memberId);

        if (existing is not null) return FitnessMapper.ToDto(existing, challenge.TargetValue);

        var participant = new ChallengeParticipant
        {
            ChallengeId = challengeId,
            MemberId = memberId,
            JoinedAt = now,
            TeamName = teamName,
        }.StampNew(tenant, userId);

        db.ChallengeParticipants.Add(participant);
        challenge.ParticipantCount++;

        await db.SaveChangesAsync();

        var saved = await db.ChallengeParticipants.ForTenant(tenant)
            .Include(p => p.Member)
            .FirstAsync(p => p.Id == participant.Id);

        return FitnessMapper.ToDto(saved, challenge.TargetValue);
    }

    /// <summary>
    /// Recomputes challenge standings and awards badges that have been earned. Runs nightly.
    ///
    /// Standings are recomputed from source rather than incremented, so a corrected check-in or a
    /// removed duplicate effort session heals the leaderboard instead of leaving it permanently
    /// slightly wrong.
    /// </summary>
    public async Task<int> ProcessChallengesAndBadgesAsync()
    {
        var now = DateTime.UtcNow;
        var actions = 0;

        var running = await db.Challenges.ForTenant(tenant)
            .Where(c => c.IsPublished && c.StartsOn <= now.Date && c.EndsOn >= now.Date.AddDays(-1))
            .Include(c => c.Participants.Where(p => !p.IsDeleted))
            .ToListAsync();

        foreach (var challenge in running)
        {
            var memberIds = challenge.Participants.Select(p => p.MemberId).ToList();
            if (memberIds.Count == 0) continue;

            var from = challenge.StartsOn;
            var to = challenge.EndsOn.AddDays(1);

            Dictionary<Guid, decimal> values = challenge.Metric switch
            {
                ChallengeMetric.Visits => (await db.CheckIns.ForTenant(tenant)
                    .Where(c => c.MemberId != null && memberIds.Contains(c.MemberId.Value)
                             && c.CheckedInAt >= from && c.CheckedInAt < to)
                    .GroupBy(c => c.MemberId!.Value)
                    .Select(g => new { MemberId = g.Key, Value = (decimal)g.Count() })
                    .ToListAsync()).ToDictionary(x => x.MemberId, x => x.Value),

                ChallengeMetric.Classes => (await db.ClassBookings.ForTenant(tenant)
                    .Where(b => b.MemberId != null && memberIds.Contains(b.MemberId.Value)
                             && b.Status == BookingStatus.Attended
                             && b.ClassOccurrence!.StartsAt >= from && b.ClassOccurrence.StartsAt < to)
                    .GroupBy(b => b.MemberId!.Value)
                    .Select(g => new { MemberId = g.Key, Value = (decimal)g.Count() })
                    .ToListAsync()).ToDictionary(x => x.MemberId, x => x.Value),

                ChallengeMetric.EffortPoints => (await db.EffortSessions.ForTenant(tenant)
                    .Where(e => memberIds.Contains(e.MemberId) && e.StartedAt >= from && e.StartedAt < to)
                    .GroupBy(e => e.MemberId)
                    .Select(g => new { MemberId = g.Key, Value = (decimal)g.Sum(e => e.EffortPoints) })
                    .ToListAsync()).ToDictionary(x => x.MemberId, x => x.Value),

                ChallengeMetric.Calories => (await db.EffortSessions.ForTenant(tenant)
                    .Where(e => memberIds.Contains(e.MemberId) && e.StartedAt >= from && e.StartedAt < to)
                    .GroupBy(e => e.MemberId)
                    .Select(g => new { MemberId = g.Key, Value = (decimal)(g.Sum(e => e.CaloriesBurned) ?? 0) })
                    .ToListAsync()).ToDictionary(x => x.MemberId, x => x.Value),

                ChallengeMetric.Distance => (await db.WorkoutResults.ForTenant(tenant)
                    .Where(r => memberIds.Contains(r.MemberId) && r.PerformedOn >= from && r.PerformedOn < to
                             && r.DistanceMetres != null)
                    .GroupBy(r => r.MemberId)
                    .Select(g => new { MemberId = g.Key, Value = g.Sum(r => r.DistanceMetres!.Value) })
                    .ToListAsync()).ToDictionary(x => x.MemberId, x => x.Value),

                ChallengeMetric.WeightLifted => (await db.WorkoutResults.ForTenant(tenant)
                    .Where(r => memberIds.Contains(r.MemberId) && r.PerformedOn >= from && r.PerformedOn < to
                             && r.LoadKg != null)
                    .GroupBy(r => r.MemberId)
                    .Select(g => new { MemberId = g.Key, Value = g.Sum(r => r.LoadKg!.Value) })
                    .ToListAsync()).ToDictionary(x => x.MemberId, x => x.Value),

                _ => [],
            };

            var ranked = challenge.Participants
                .Where(p => !p.IsDeleted)
                .OrderByDescending(p => values.GetValueOrDefault(p.MemberId))
                .ToList();

            var rank = 1;
            foreach (var participant in ranked)
            {
                var value = values.GetValueOrDefault(participant.MemberId);

                if (participant.CurrentValue != value) participant.LastProgressAt = now;
                participant.CurrentValue = value;
                participant.Rank = rank++;

                if (!participant.HasCompleted && challenge.TargetValue is not null && value >= challenge.TargetValue)
                {
                    participant.HasCompleted = true;
                    participant.CompletedAt = now;
                    challenge.CompletedCount++;

                    if (!participant.PointsAwarded && challenge.PointsForCompletion > 0)
                    {
                        await AwardPointsAsync(new AwardPointsDto
                        {
                            MemberId = participant.MemberId,
                            Points = challenge.PointsForCompletion,
                            Reason = $"Completed {challenge.Name}",
                            Kind = LoyaltyEventKind.ChallengeCompleted,
                        }, Guid.Empty);

                        participant.PointsAwarded = true;
                    }

                    actions++;
                }
            }
        }

        actions += await AwardBadgesAsync(now);

        await db.SaveChangesAsync();
        return actions;
    }

    public async Task<List<BadgeDto>> GetBadgesAsync(Guid? clubId)
    {
        var badges = await db.Badges.ForTenant(tenant)
            .WhereIf(clubId is not null, b => b.ClubId == clubId || b.ClubId == null)
            .OrderBy(b => b.DisplayOrder).ThenBy(b => b.Name)
            .ToListAsync();

        var counts = await db.MemberBadges.ForTenant(tenant)
            .GroupBy(m => m.BadgeId)
            .Select(g => new { BadgeId = g.Key, Count = g.Count() })
            .ToListAsync();

        return [.. badges.Select(b =>
        {
            var dto = FitnessMapper.ToDto(b);
            dto.AwardedCount = counts.FirstOrDefault(c => c.BadgeId == b.Id)?.Count ?? 0;
            return dto;
        })];
    }

    public async Task<List<MemberBadgeDto>> GetMemberBadgesAsync(Guid memberId)
    {
        var badges = await db.MemberBadges.ForTenant(tenant)
            .Where(m => m.MemberId == memberId)
            .Include(m => m.Badge)
            .OrderByDescending(m => m.EarnedOn)
            .ToListAsync();

        return [.. badges.Select(FitnessMapper.ToDto)];
    }

    // ── Feedback ─────────────────────────────────────────────────────────────

    public async Task<NpsResponseDto> RecordNpsAsync(NpsResponseDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var band = request.Score switch
        {
            >= 9 => "Promoter",
            >= 7 => "Passive",
            _ => "Detractor",
        };

        var response = new NpsResponse
        {
            MemberId = request.MemberId,
            ClubId = request.ClubId,
            Score = Math.Clamp(request.Score, 0, 10),
            Band = band,
            Comment = request.Comment,
            Trigger = request.Trigger,
            ClassOccurrenceId = request.ClassOccurrenceId,
            StaffId = request.StaffId,
            RespondedAt = now,
        }.StampNew(tenant, userId);

        db.NpsResponses.Add(response);

        // A detractor is a retention event, not a survey statistic. Somebody calls them.
        if (band == "Detractor")
        {
            var member = await db.Members.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == request.MemberId);

            db.RetentionTasks.Add(new RetentionTask
            {
                MemberId = request.MemberId,
                ClubId = request.ClubId,
                AssignedStaffId = member?.AssignedCoachId,
                Title = $"Call {member?.FirstName ?? "member"} — scored us {request.Score}/10",
                Detail = request.Comment,
                Trigger = "NPS detractor",
                DueOn = now.Date,
                Priority = 1,
            }.StampNew(tenant, userId));
        }

        await db.SaveChangesAsync();

        var saved = await db.NpsResponses.ForTenant(tenant)
            .Include(n => n.Member)
            .FirstAsync(n => n.Id == response.Id);

        return FitnessMapper.ToDto(saved);
    }

    public async Task<NpsSummaryDto> GetNpsSummaryAsync(Guid? clubId, DateTime from, DateTime to)
    {
        var responses = await db.NpsResponses.ForTenant(tenant)
            .Where(n => n.RespondedAt >= from && n.RespondedAt <= to)
            .WhereIf(clubId is not null, n => n.ClubId == clubId)
            .Include(n => n.Member)
            .ToListAsync();

        var promoters = responses.Count(r => r.Score >= 9);
        var passives = responses.Count(r => r.Score is >= 7 and <= 8);
        var detractors = responses.Count(r => r.Score <= 6);
        var total = responses.Count;

        var summary = new NpsSummaryDto
        {
            ClubId = clubId,
            From = from,
            To = to,
            ResponseCount = total,
            Promoters = promoters,
            Passives = passives,
            Detractors = detractors,
            Nps = total == 0 ? 0 : (promoters * 100 / total) - (detractors * 100 / total),
            AverageScore = total == 0 ? 0 : Math.Round((decimal)responses.Average(r => r.Score), 1),
            DetractorsAwaitingFollowUp = responses.Count(r => r.Score <= 6 && !r.FollowedUp),
        };

        var previousLength = to - from;
        var previous = await db.NpsResponses.ForTenant(tenant)
            .Where(n => n.RespondedAt >= from - previousLength && n.RespondedAt < from)
            .WhereIf(clubId is not null, n => n.ClubId == clubId)
            .Select(n => n.Score)
            .ToListAsync();

        if (previous.Count > 0)
        {
            var prevPromoters = previous.Count(s => s >= 9);
            var prevDetractors = previous.Count(s => s <= 6);
            summary.PreviousNps = (prevPromoters * 100 / previous.Count) - (prevDetractors * 100 / previous.Count);
            summary.Change = summary.Nps - summary.PreviousNps;
        }

        var membersAsked = await db.Members.ForTenant(tenant)
            .CountAsync(m => m.Status == MemberStatus.Active && (clubId == null || m.HomeClubId == clubId));

        summary.ResponseRatePercent = FitnessMapper.Percent(total, membersAsked);

        summary.RecentDetractors = [.. responses
            .Where(r => r.Score <= 6 && !r.FollowedUp)
            .OrderByDescending(r => r.RespondedAt)
            .Take(10)
            .Select(FitnessMapper.ToDto)];

        summary.Trend = [.. responses
            .GroupBy(r => new { r.RespondedAt.Year, r.RespondedAt.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g =>
            {
                var p = g.Count(r => r.Score >= 9);
                var d = g.Count(r => r.Score <= 6);
                return new NpsTrendPointDto
                {
                    Period = new DateTime(g.Key.Year, g.Key.Month, 1),
                    Label = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    Nps = (p * 100 / g.Count()) - (d * 100 / g.Count()),
                    ResponseCount = g.Count(),
                };
            })];

        return summary;
    }

    public async Task<PaginatedResponse<NpsResponseDto>> GetNpsResponsesAsync(
        Guid? clubId, string? band, bool needsFollowUpOnly, PaginationParams pagination)
    {
        var query = db.NpsResponses.ForTenant(tenant)
            .Include(n => n.Member)
            .WhereIf(clubId is not null, n => n.ClubId == clubId)
            .WhereIf(!string.IsNullOrWhiteSpace(band), n => n.Band == band)
            .WhereIf(needsFollowUpOnly, n => n.Score <= 6 && !n.FollowedUp);

        var total = await query.CountAsync();

        var page = await query
            .OrderByDescending(n => n.RespondedAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        return PaginatedResponse<NpsResponseDto>.Ok(
                   [.. page.Select(FitnessMapper.ToDto)],
                   total,
                   pagination.PageNumber,
                   pagination.PageSize);
    }

    public async Task<NpsResponseDto> FollowUpNpsAsync(Guid responseId, string note, Guid userId)
    {
        var response = await db.NpsResponses.ForTenant(tenant)
            .Include(n => n.Member)
            .FirstOrDefaultAsync(n => n.Id == responseId)
            ?? throw new InvalidOperationException("Response not found.");

        response.FollowedUp = true;
        response.FollowedUpAt = DateTime.UtcNow;
        response.FollowedUpByStaffId = userId == Guid.Empty ? null : userId;
        response.FollowUpNote = note;
        response.StampUpdated(userId);

        db.MemberNotes.Add(new MemberNote
        {
            MemberId = response.MemberId,
            Kind = InteractionKind.Call,
            Body = $"Followed up on their {response.Score}/10 score — {note}",
            OccurredAt = DateTime.UtcNow,
            StaffId = userId == Guid.Empty ? null : userId,
        }.StampNew(tenant, userId));

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(response);
    }

    public async Task<FeedbackDto> RecordFeedbackAsync(FeedbackDto request, Guid userId)
    {
        var feedback = new Feedback
        {
            MemberId = request.MemberId,
            ClubId = request.ClubId,
            Category = request.Category,
            Body = request.Body,
            Rating = request.Rating,
            SubmittedAt = DateTime.UtcNow,
            Channel = request.Channel,
            IsAnonymous = request.IsAnonymous,
        }.StampNew(tenant, userId);

        db.Feedback.Add(feedback);
        await db.SaveChangesAsync();

        return FitnessMapper.ToDto(feedback);
    }

    public async Task<PaginatedResponse<FeedbackDto>> GetFeedbackAsync(
        Guid? clubId, bool openOnly, PaginationParams pagination)
    {
        var query = db.Feedback.ForTenant(tenant)
            .WhereIf(clubId is not null, f => f.ClubId == clubId)
            .WhereIf(openOnly, f => !f.IsActioned);

        var total = await query.CountAsync();

        var page = await query
            .OrderByDescending(f => f.SubmittedAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        var memberIds = page.Where(f => f.MemberId is not null && !f.IsAnonymous)
            .Select(f => f.MemberId!.Value).Distinct().ToList();

        var names = await db.Members.ForTenant(tenant)
            .Where(m => memberIds.Contains(m.Id))
            .Select(m => new { m.Id, Name = m.FirstName + " " + m.LastName })
            .ToDictionaryAsync(m => m.Id, m => m.Name);

        var items = page.Select(f =>
        {
            var dto = FitnessMapper.ToDto(f);
            // Anonymous feedback stays anonymous even to a manager — that is what it was promised.
            if (!f.IsAnonymous && f.MemberId is not null) dto.MemberName = names.GetValueOrDefault(f.MemberId.Value);
            return dto;
        }).ToList();

        return PaginatedResponse<FeedbackDto>.Ok(
                   items,
                   total,
                   pagination.PageNumber,
                   pagination.PageSize);
    }

    public async Task<List<AnnouncementDto>> GetAnnouncementsAsync(Guid? clubId, bool liveOnly)
    {
        var now = DateTime.UtcNow;

        var announcements = await db.Announcements.ForTenant(tenant)
            .WhereIf(clubId is not null, a => a.ClubId == clubId || a.ClubId == null)
            .WhereIf(liveOnly, a => a.IsPublished && a.ShowFrom <= now && (a.ShowUntil == null || a.ShowUntil >= now))
            .OrderByDescending(a => a.IsUrgent).ThenByDescending(a => a.ShowFrom)
            .ToListAsync();

        return [.. announcements.Select(a => FitnessMapper.ToDto(a, now))];
    }

    public async Task<AnnouncementDto> SaveAnnouncementAsync(Guid? id, AnnouncementDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        Announcement announcement;
        if (id is null)
        {
            announcement = new Announcement().StampNew(tenant, userId);
            db.Announcements.Add(announcement);
        }
        else
        {
            announcement = await db.Announcements.ForTenant(tenant).FirstOrDefaultAsync(a => a.Id == id)
                ?? throw new InvalidOperationException("Announcement not found.");
            announcement.StampUpdated(userId);
        }

        announcement.ClubId = request.ClubId;
        announcement.Title = request.Title;
        announcement.Body = request.Body;
        announcement.ImageUrl = request.ImageUrl;
        announcement.ShowFrom = request.ShowFrom == default ? now : request.ShowFrom;
        announcement.ShowUntil = request.ShowUntil;
        announcement.ShowOnKiosk = request.ShowOnKiosk;
        announcement.ShowInApp = request.ShowInApp;
        announcement.ShowOnClubScreens = request.ShowOnClubScreens;
        announcement.IsUrgent = request.IsUrgent;
        announcement.SegmentId = request.SegmentId;
        announcement.IsPublished = request.IsPublished;

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(announcement, now);
    }

    // ═══ Internals ═══════════════════════════════════════════════════════════

    private async Task<int> EnrolTriggeredAsync(EngagementJourney journey, DateTime now)
    {
        var firstStep = journey.Steps.Where(s => !s.IsDeleted).OrderBy(s => s.StepNumber).FirstOrDefault();
        if (firstStep is null) return 0;

        var cooldown = now.AddDays(-journey.ReEnrolmentCooldownDays);

        var alreadyIn = await db.JourneyEnrolments.ForTenant(tenant)
            .Where(e => e.EngagementJourneyId == journey.Id
                     && (e.CompletedAt == null || e.EnrolledAt >= cooldown))
            .Select(e => e.MemberId)
            .ToListAsync();

        var excluded = alreadyIn.ToHashSet();

        var candidates = journey.Trigger switch
        {
            JourneyTrigger.MemberJoined => await db.Members.ForTenant(tenant)
                .Where(m => m.Status == MemberStatus.Active && m.JoinedOn >= now.AddDays(-1))
                .WhereIf(journey.ClubId is not null, m => m.HomeClubId == journey.ClubId)
                .Select(m => m.Id).ToListAsync(),

            JourneyTrigger.NoVisitForDays when journey.TriggerThresholdDays is not null =>
                await db.Members.ForTenant(tenant)
                    .Where(m => m.Status == MemberStatus.Active
                             && m.LastVisitOn != null
                             && m.LastVisitOn < now.AddDays(-journey.TriggerThresholdDays.Value))
                    .WhereIf(journey.ClubId is not null, m => m.HomeClubId == journey.ClubId)
                    .Select(m => m.Id).ToListAsync(),

            JourneyTrigger.PaymentFailed => await db.DunningCases.ForTenant(tenant)
                .Where(c => c.Status == DunningCaseStatus.Open && c.OpenedOn >= now.AddDays(-1))
                .WhereIf(journey.ClubId is not null, c => c.ClubId == journey.ClubId)
                .Select(c => c.MemberId).ToListAsync(),

            JourneyTrigger.ContractEndingInDays when journey.TriggerThresholdDays is not null =>
                await db.Agreements.ForTenant(tenant)
                    .Where(a => a.Status == AgreementStatus.Active
                             && a.MinimumTermEndsOn != null
                             && a.MinimumTermEndsOn.Value.Date == now.Date.AddDays(journey.TriggerThresholdDays.Value))
                    .WhereIf(journey.ClubId is not null, a => a.ClubId == journey.ClubId)
                    .Select(a => a.MemberId).ToListAsync(),

            JourneyTrigger.Birthday => await db.Members.ForTenant(tenant)
                .Where(m => m.Status == MemberStatus.Active && m.DateOfBirth != null
                         && m.DateOfBirth.Value.Month == now.Month && m.DateOfBirth.Value.Day == now.Day)
                .WhereIf(journey.ClubId is not null, m => m.HomeClubId == journey.ClubId)
                .Select(m => m.Id).ToListAsync(),

            JourneyTrigger.JoinAnniversary => await db.Members.ForTenant(tenant)
                .Where(m => m.Status == MemberStatus.Active && m.JoinedOn != null
                         && m.JoinedOn.Value.Month == now.Month && m.JoinedOn.Value.Day == now.Day
                         && m.JoinedOn.Value.Year < now.Year)
                .WhereIf(journey.ClubId is not null, m => m.HomeClubId == journey.ClubId)
                .Select(m => m.Id).ToListAsync(),

            JourneyTrigger.Cancelled => await db.CancellationRequests.ForTenant(tenant)
                .Where(c => c.IsProcessed && !c.WasSaved && c.EffectiveOn >= now.Date.AddDays(-1))
                .Select(c => c.MemberId).ToListAsync(),

            JourneyTrigger.TrialEnding when journey.TriggerThresholdDays is not null =>
                await db.Trials.ForTenant(tenant)
                    .Where(t => !t.Converted && t.MemberId != null
                             && t.EndsOn.Date == now.Date.AddDays(journey.TriggerThresholdDays.Value))
                    .Select(t => t.MemberId!.Value).ToListAsync(),

            JourneyTrigger.RiskBandChanged => await db.ChurnScores.ForTenant(tenant)
                .Where(c => c.BandWorsened && c.ComputedOn >= now.AddDays(-1)
                         && (c.Band == ChurnRiskBand.AtRisk || c.Band == ChurnRiskBand.Critical))
                .WhereIf(journey.ClubId is not null, c => c.ClubId == journey.ClubId)
                .Select(c => c.MemberId).ToListAsync(),

            JourneyTrigger.CreditsExpiring when journey.TriggerThresholdDays is not null =>
                await db.SessionCredits.ForTenant(tenant)
                    .Where(c => !c.IsExpired && c.Remaining > 0 && c.ExpiresOn != null
                             && c.ExpiresOn.Value.Date == now.Date.AddDays(journey.TriggerThresholdDays.Value))
                    .Select(c => c.MemberId).Distinct().ToListAsync(),

            _ => [],
        };

        var enrolled = 0;

        foreach (var memberId in candidates.Distinct().Where(id => !excluded.Contains(id)))
        {
            db.JourneyEnrolments.Add(new JourneyEnrolment
            {
                EngagementJourneyId = journey.Id,
                MemberId = memberId,
                EnrolledAt = now,
                CurrentStep = 0,
                NextStepDueAt = now.AddHours(firstStep.DelayHours),
            }.StampNew(tenant));

            journey.EnrolledCount++;
            enrolled++;
        }

        return enrolled;
    }

    private async Task<int?> ExecuteStepAsync(
        EngagementJourney journey, JourneyStep step, JourneyEnrolment enrolment, DateTime now)
    {
        var member = enrolment.Member
            ?? await db.Members.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == enrolment.MemberId);

        if (member is null) return null;

        switch (step.Kind)
        {
            case JourneyStepKind.SendEmail or JourneyStepKind.SendSms
              or JourneyStepKind.SendPush or JourneyStepKind.SendWhatsApp:
            {
                var channel = step.Channel ?? MessageChannel.Email;
                var template = step.MessageTemplateId is null
                    ? null
                    : await db.MessageTemplates.ForTenant(tenant).FirstOrDefaultAsync(t => t.Id == step.MessageTemplateId);

                var (allowed, reason) = await CheckConsentAsync(member, channel, template?.IsTransactional ?? false);

                var recipient = channel switch
                {
                    MessageChannel.Email => member.Email,
                    MessageChannel.Sms or MessageChannel.WhatsApp => member.Phone,
                    _ => member.Id.ToString(),
                };

                db.MessageLog.Add(new MessageLog
                {
                    MemberId = member.Id,
                    ClubId = journey.ClubId ?? member.HomeClubId,
                    Channel = channel,
                    Status = allowed && recipient is not null ? MessageStatus.Queued : MessageStatus.SuppressedNoConsent,
                    MessageTemplateId = step.MessageTemplateId,
                    JourneyEnrolmentId = enrolment.Id,
                    Recipient = recipient,
                    Subject = Merge(template?.Subject, member),
                    BodyPreview = Preview(Merge(template?.Body, member)),
                    FailureReason = allowed ? (recipient is null ? "No address on file" : null) : reason,
                    QueuedAt = now,
                }.StampNew(tenant));

                break;
            }

            case JourneyStepKind.CreateTask:
                db.RetentionTasks.Add(new RetentionTask
                {
                    MemberId = member.Id,
                    ClubId = journey.ClubId ?? member.HomeClubId,
                    AssignedStaffId = step.TaskAssignStaffId ?? member.AssignedCoachId,
                    Title = step.TaskTitle ?? journey.Name,
                    Trigger = journey.Name,
                    DueOn = now.Date,
                    Priority = 2,
                    JourneyEnrolmentId = enrolment.Id,
                }.StampNew(tenant));
                break;

            case JourneyStepKind.AddTag when step.TagToApply is not null:
            {
                var exists = await db.MemberTags.ForTenant(tenant)
                    .AnyAsync(t => t.MemberId == member.Id && t.Tag == step.TagToApply);

                if (!exists)
                {
                    db.MemberTags.Add(new MemberTag
                    {
                        MemberId = member.Id,
                        Tag = step.TagToApply,
                        IsSystemTag = true,
                    }.StampNew(tenant));
                }

                break;
            }

            case JourneyStepKind.RemoveTag when step.TagToApply is not null:
            {
                var tag = await db.MemberTags.ForTenant(tenant)
                    .FirstOrDefaultAsync(t => t.MemberId == member.Id && t.Tag == step.TagToApply);

                if (tag is not null) tag.StampDeleted(Guid.Empty);
                break;
            }

            case JourneyStepKind.GrantLoyaltyPoints when step.LoyaltyPointsToGrant > 0:
                await AwardPointsAsync(new AwardPointsDto
                {
                    MemberId = member.Id,
                    Points = step.LoyaltyPointsToGrant,
                    Reason = journey.Name,
                    Kind = LoyaltyEventKind.ManualAward,
                }, Guid.Empty);
                break;

            case JourneyStepKind.Condition:
                // A false condition branches rather than stopping, so a journey can say "if they
                // came back, thank them; if not, escalate".
                var passed = await EvaluateConditionAsync(step.ConditionExpression, member, now);
                return passed ? null : step.OnFalseStepNumber;

            case JourneyStepKind.ExitJourney:
                enrolment.ExitedAt = now;
                enrolment.ExitReason = "Journey step ended it";
                break;
        }

        return null;
    }

    private async Task<bool> HasSucceededAsync(EngagementJourney journey, JourneyEnrolment enrolment, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(journey.SuccessMetric)) return false;

        return journey.SuccessMetric.ToLowerInvariant() switch
        {
            "visited" => await db.CheckIns.ForTenant(tenant)
                .AnyAsync(c => c.MemberId == enrolment.MemberId && c.CheckedInAt >= enrolment.EnrolledAt),

            "booked" => await db.ClassBookings.ForTenant(tenant)
                .AnyAsync(b => b.MemberId == enrolment.MemberId && b.BookedAt >= enrolment.EnrolledAt),

            "paid" => await db.Payments.ForTenant(tenant)
                .AnyAsync(p => p.MemberId == enrolment.MemberId && p.Status == PaymentStatus.Succeeded
                            && p.ReceivedOn >= enrolment.EnrolledAt),

            "renewed" => await db.Agreements.ForTenant(tenant)
                .AnyAsync(a => a.MemberId == enrolment.MemberId && a.StartsOn >= enrolment.EnrolledAt.Date),

            _ => false,
        };
    }

    private async Task<bool> EvaluateConditionAsync(string? expression, Member member, DateTime now)
    {
        if (string.IsNullOrWhiteSpace(expression)) return true;

        return expression.ToLowerInvariant() switch
        {
            "hasvisited" => member.LastVisitOn is not null && member.LastVisitOn > now.AddDays(-7),
            "hasbalance" => member.AccountBalance > 0,
            "isactive" => member.Status == MemberStatus.Active,
            "hasbooking" => await db.ClassBookings.ForTenant(tenant)
                .AnyAsync(b => b.MemberId == member.Id && b.Status == BookingStatus.Booked
                            && b.ClassOccurrence!.StartsAt > now),
            _ => true,
        };
    }

    /// <summary>
    /// Whether this member may be contacted on this channel.
    ///
    /// Transactional messages — an invoice, a failed payment, a class cancellation — go regardless
    /// of marketing consent, because they are about something the member asked for. Everything
    /// else needs an explicit yes.
    /// </summary>
    private async Task<(bool Allowed, string? Reason)> CheckConsentAsync(
        Member member, MessageChannel channel, bool isTransactional)
    {
        if (isTransactional) return (true, null);

        var consent = await db.Consents.ForTenant(tenant)
            .Where(c => c.MemberId == member.Id && c.Channel == channel && c.Purpose == "Marketing")
            .OrderByDescending(c => c.DecidedAt)
            .FirstOrDefaultAsync();

        if (consent is null) return (false, $"No marketing consent recorded for {channel}");
        if (!consent.Granted) return (false, $"Marketing consent for {channel} withdrawn on {consent.DecidedAt:d MMM yyyy}");

        var preference = await db.Preferences.ForTenant(tenant)
            .FirstOrDefaultAsync(p => p.MemberId == member.Id);

        if (preference is not null && !preference.MarketingMessages)
            return (false, "Member has switched off marketing messages");

        return (true, null);
    }

    /// <summary>
    /// Evaluates a saved segment.
    ///
    /// Definitions are stored as JSON rather than SQL so a segment is portable and cannot be used
    /// to run arbitrary queries. The supported keys are deliberately few — the ones a marketer
    /// actually asks for.
    /// </summary>
    private async Task<List<Member>> EvaluateSegmentAsync(Segment segment)
    {
        var now = DateTime.UtcNow;

        var query = db.Members.ForTenant(tenant)
            .Include(m => m.HomeClub)
            .Where(m => !m.IsAnonymised);

        if (segment.ClubId is not null) query = query.Where(m => m.HomeClubId == segment.ClubId);

        Dictionary<string, JsonElement> filters;
        try
        {
            filters = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(segment.DefinitionJson) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }

        foreach (var (key, value) in filters)
        {
            switch (key.ToLowerInvariant())
            {
                case "status" when value.ValueKind == JsonValueKind.String:
                    if (Enum.TryParse<MemberStatus>(value.GetString(), true, out var status))
                        query = query.Where(m => m.Status == status);
                    break;

                case "riskband" when value.ValueKind == JsonValueKind.String:
                    if (Enum.TryParse<ChurnRiskBand>(value.GetString(), true, out var band))
                        query = query.Where(m => m.RiskBand == band);
                    break;

                case "novisitdays" when value.ValueKind == JsonValueKind.Number:
                    var cutoff = now.AddDays(-value.GetInt32());
                    query = query.Where(m => m.LastVisitOn == null || m.LastVisitOn < cutoff);
                    break;

                case "joinedafter" when value.ValueKind == JsonValueKind.String:
                    if (DateTime.TryParse(value.GetString(), out var after))
                        query = query.Where(m => m.JoinedOn >= after);
                    break;

                case "joinedbefore" when value.ValueKind == JsonValueKind.String:
                    if (DateTime.TryParse(value.GetString(), out var before))
                        query = query.Where(m => m.JoinedOn <= before);
                    break;

                case "hasbalance" when value.ValueKind is JsonValueKind.True or JsonValueKind.False:
                    query = value.GetBoolean()
                        ? query.Where(m => m.AccountBalance > 0)
                        : query.Where(m => m.AccountBalance <= 0);
                    break;

                case "minvisits" when value.ValueKind == JsonValueKind.Number:
                    var min = value.GetInt32();
                    query = query.Where(m => m.TotalVisits >= min);
                    break;

                case "maxvisits" when value.ValueKind == JsonValueKind.Number:
                    var max = value.GetInt32();
                    query = query.Where(m => m.TotalVisits <= max);
                    break;

                case "birthdaymonth" when value.ValueKind == JsonValueKind.Number:
                    var month = value.GetInt32();
                    query = query.Where(m => m.DateOfBirth != null && m.DateOfBirth.Value.Month == month);
                    break;

                case "tag" when value.ValueKind == JsonValueKind.String:
                    var tag = value.GetString();
                    query = query.Where(m => m.Tags.Any(t => !t.IsDeleted && t.Tag == tag));
                    break;

                case "planid" when value.ValueKind == JsonValueKind.String:
                    if (Guid.TryParse(value.GetString(), out var planId))
                    {
                        var onPlan = db.Agreements.ForTenant(tenant)
                            .Where(a => a.PlanId == planId && a.Status == AgreementStatus.Active)
                            .Select(a => a.MemberId);
                        query = query.Where(m => onPlan.Contains(m.Id));
                    }
                    break;
            }
        }

        return await query.Take(10000).ToListAsync();
    }

    private async Task<int> AwardBadgesAsync(DateTime now)
    {
        var awarded = 0;

        var badges = await db.Badges.ForTenant(tenant)
            .Where(b => b.IsActive && b.IsAutomatic && b.CriteriaExpression != null)
            .ToListAsync();

        if (badges.Count == 0) return 0;

        var alreadyHeld = await db.MemberBadges.ForTenant(tenant)
            .Select(m => new { m.MemberId, m.BadgeId })
            .ToListAsync();

        var held = alreadyHeld.Select(m => (m.MemberId, m.BadgeId)).ToHashSet();

        var members = await db.Members.ForTenant(tenant)
            .Where(m => m.Status == MemberStatus.Active)
            .Select(m => new { m.Id, m.TotalVisits, m.CurrentStreakDays, m.HomeClubId })
            .ToListAsync();

        foreach (var badge in badges)
        {
            var (metric, threshold) = ParseCriteria(badge.CriteriaExpression!);
            if (metric is null) continue;

            foreach (var member in members)
            {
                if (held.Contains((member.Id, badge.Id))) continue;

                var qualifies = metric switch
                {
                    "visits" => member.TotalVisits >= threshold,
                    "streak" => member.CurrentStreakDays >= threshold,
                    _ => false,
                };

                if (!qualifies) continue;

                db.MemberBadges.Add(new MemberBadge
                {
                    MemberId = member.Id,
                    BadgeId = badge.Id,
                    EarnedOn = now,
                    Context = badge.CriteriaDescription,
                }.StampNew(tenant));

                if (badge.PointsAwarded > 0)
                {
                    await AwardPointsAsync(new AwardPointsDto
                    {
                        MemberId = member.Id,
                        Points = badge.PointsAwarded,
                        Reason = $"Earned the {badge.Name} badge",
                        Kind = LoyaltyEventKind.Milestone,
                    }, Guid.Empty);
                }

                awarded++;
            }
        }

        return awarded;
    }

    private static (string? Metric, int Threshold) ParseCriteria(string expression)
    {
        // "visits>=100" or "streak>=30" — deliberately tiny, because a badge rule engine is not
        // where this product needs a DSL.
        var parts = expression.Split(">=", StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !int.TryParse(parts[1], out var threshold)) return (null, 0);
        return (parts[0].ToLowerInvariant(), threshold);
    }

    private async Task UpdateTierAsync(LoyaltyAccount account, Guid userId)
    {
        var tier = await db.LoyaltyTiers.ForTenant(tenant)
            .Where(t => t.IsActive && t.PointsRequired <= account.LifetimePoints)
            .OrderByDescending(t => t.PointsRequired)
            .FirstOrDefaultAsync();

        if (tier is null || tier.Id == account.TierId) return;

        account.TierId = tier.Id;
        account.TierAchievedOn = DateTime.UtcNow;

        db.MemberNotes.Add(new MemberNote
        {
            MemberId = account.MemberId,
            Kind = InteractionKind.SystemEvent,
            Body = $"Reached {tier.Name} loyalty tier",
            OccurredAt = DateTime.UtcNow,
        }.StampNew(tenant, userId));
    }

    private async Task SyncMemberPointsAsync(Guid memberId, int balance, Guid userId)
    {
        var member = await db.Members.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == memberId);
        if (member is null) return;

        member.LoyaltyPoints = balance;
        member.StampUpdated(userId);
    }

    private static string? Merge(string? text, Member member)
    {
        if (string.IsNullOrWhiteSpace(text)) return text;

        return text
            .Replace("{{member.firstName}}", member.PreferredName ?? member.FirstName)
            .Replace("{{member.lastName}}", member.LastName)
            .Replace("{{member.memberNumber}}", member.MemberNumber)
            .Replace("{{member.balance}}", member.AccountBalance.ToString("0.00"));
    }

    private static string? Preview(string? body)
        => body is null ? null : body.Length <= 200 ? body : body[..200];
}
