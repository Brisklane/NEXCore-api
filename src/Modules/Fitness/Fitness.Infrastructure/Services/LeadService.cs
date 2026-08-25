using Fitness.Application.DTOs;
using Fitness.Application.Services.Interfaces;
using Fitness.Domain.Entities;
using Fitness.Domain.Enums;
using Fitness.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;

namespace Fitness.Infrastructure.Services;

/// <summary>
/// Leads, tours, trials and referrals.
///
/// The speed-to-lead clock is the reason this service exists in the shape it does. In this market
/// nothing else on the sales floor moves conversion as much as how quickly the first genuine
/// contact happens, so the SLA is a first-class field on the lead, the board sorts on it, and a
/// breach is a countable event rather than a feeling the sales manager has.
/// </summary>
public class LeadService(
    FitnessDbContext db,
    IFitnessTenant tenant,
    IMemberService members,
    IAccessService access) : ILeadService
{
    // ── Board ────────────────────────────────────────────────────────────────

    /// <summary>
    /// The pipeline board.
    ///
    /// Columns in the order a lead actually travels, and the header numbers a sales manager runs
    /// the morning meeting from — how many are breaching, how many follow-ups are overdue, and
    /// what the median response time is.
    /// </summary>
    public async Task<LeadBoardDto> GetBoardAsync(Guid? clubId, Guid? assignedStaffId)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();
        var sla = settings?.LeadResponseSlaMinutes ?? 15;

        var open = await db.Leads.ForTenant(tenant)
            .Where(l => l.Status != LeadStatus.Won && l.Status != LeadStatus.Lost)
            .WhereIf(clubId is not null, l => l.ClubId == clubId)
            .WhereIf(assignedStaffId is not null, l => l.AssignedStaffId == assignedStaffId)
            .Include(l => l.LeadSource)
            .OrderBy(l => l.ReceivedAt)
            .ToListAsync();

        var staffNames = await db.Staff.ForTenant(tenant)
            .Select(s => new { s.Id, Name = s.FirstName + " " + s.LastName })
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        var board = new LeadBoardDto
        {
            ClubId = clubId,
            GeneratedAt = now,
            SlaMinutes = sla,
            TotalOpen = open.Count,
        };

        if (clubId is not null)
        {
            board.ClubName = await db.Clubs.ForTenant(tenant)
                .Where(c => c.Id == clubId).Select(c => c.Name).FirstOrDefaultAsync();
        }

        var stages = new[]
        {
            (LeadStatus.New, "New"),
            (LeadStatus.Contacted, "Contacted"),
            (LeadStatus.TourBooked, "Tour booked"),
            (LeadStatus.Toured, "Toured"),
            (LeadStatus.Trialling, "On trial"),
            (LeadStatus.Negotiating, "Negotiating"),
            (LeadStatus.Nurturing, "Nurturing"),
        };

        foreach (var (status, label) in stages)
        {
            var inStage = open.Where(l => l.Status == status).ToList();

            board.Columns.Add(new LeadBoardColumnDto
            {
                Status = status,
                Label = label,
                Count = inStage.Count,
                EstimatedValue = inStage.Sum(l => l.WonValue ?? 0),
                Leads = [.. inStage.Select(l =>
                {
                    var dto = FitnessMapper.ToSummary(l, now, sla);
                    if (l.AssignedStaffId is not null)
                        dto.AssignedStaffName = staffNames.GetValueOrDefault(l.AssignedStaffId.Value);
                    return dto;
                })],
            });
        }

        board.BreachingSla = open.Count(l => l.FirstContactedAt is null
                                          && l.ReceivedAt < now.AddMinutes(-sla));

        board.OverdueFollowUps = open.Count(l => l.NextFollowUpOn is not null && l.NextFollowUpOn < now);

        var closedThisMonth = await db.Leads.ForTenant(tenant)
            .Where(l => (l.WonOn >= monthStart) || (l.LostOn >= monthStart))
            .WhereIf(clubId is not null, l => l.ClubId == clubId)
            .Select(l => new { l.WonOn, l.LostOn })
            .ToListAsync();

        board.WonThisMonth = closedThisMonth.Count(l => l.WonOn is not null);
        board.LostThisMonth = closedThisMonth.Count(l => l.LostOn is not null);
        board.ConversionPercent = FitnessMapper.Percent(board.WonThisMonth, board.WonThisMonth + board.LostThisMonth);

        var responses = await db.Leads.ForTenant(tenant)
            .Where(l => l.ResponseMinutes != null && l.ReceivedAt >= monthStart)
            .WhereIf(clubId is not null, l => l.ClubId == clubId)
            .Select(l => l.ResponseMinutes!.Value)
            .ToListAsync();

        board.MedianResponseMinutes = Median(responses);

        return board;
    }

    public async Task<PaginatedResponse<LeadSummaryDto>> ListAsync(
        Guid? clubId, LeadStatus? status, Guid? sourceId, Guid? assignedStaffId,
        bool? slaBreached, DateTime? from, DateTime? to, string? search, PaginationParams pagination)
    {
        var now = DateTime.UtcNow;
        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();
        var sla = settings?.LeadResponseSlaMinutes ?? 15;

        var query = db.Leads.ForTenant(tenant)
            .Include(l => l.LeadSource)
            .WhereIf(clubId is not null, l => l.ClubId == clubId)
            .WhereIf(status is not null, l => l.Status == status)
            .WhereIf(sourceId is not null, l => l.LeadSourceId == sourceId)
            .WhereIf(assignedStaffId is not null, l => l.AssignedStaffId == assignedStaffId)
            .WhereIf(slaBreached == true, l => l.SlaBreached)
            .WhereIf(from is not null, l => l.ReceivedAt >= from)
            .WhereIf(to is not null, l => l.ReceivedAt <= to);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(l =>
                l.FirstName.ToLower().Contains(term) ||
                (l.LastName != null && l.LastName.ToLower().Contains(term)) ||
                (l.Phone != null && l.Phone.Contains(term)) ||
                (l.Email != null && l.Email.ToLower().Contains(term)));
        }

        var total = await query.CountAsync();

        var page = await query
            .OrderByDescending(l => l.ReceivedAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        var staffNames = await db.Staff.ForTenant(tenant)
            .Select(s => new { s.Id, Name = s.FirstName + " " + s.LastName })
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        var items = page.Select(l =>
        {
            var dto = FitnessMapper.ToSummary(l, now, sla);
            if (l.AssignedStaffId is not null)
                dto.AssignedStaffName = staffNames.GetValueOrDefault(l.AssignedStaffId.Value);
            return dto;
        }).ToList();

        return PaginatedResponse<LeadSummaryDto>.Ok(
                   items,
                   total,
                   pagination.PageNumber,
                   pagination.PageSize);
    }

    public async Task<LeadDetailDto?> GetAsync(Guid leadId)
    {
        var now = DateTime.UtcNow;
        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();
        var sla = settings?.LeadResponseSlaMinutes ?? 15;

        var lead = await db.Leads.ForTenant(tenant)
            .Include(l => l.LeadSource)
            .Include(l => l.Activities.Where(a => !a.IsDeleted))
            .FirstOrDefaultAsync(l => l.Id == leadId);

        if (lead is null) return null;

        var summary = FitnessMapper.ToSummary(lead, now, sla);

        var detail = new LeadDetailDto
        {
            Id = summary.Id,
            ClubId = summary.ClubId,
            FirstName = summary.FirstName,
            LastName = summary.LastName,
            FullName = summary.FullName,
            Phone = summary.Phone,
            Email = summary.Email,
            Status = summary.Status,
            SourceName = summary.SourceName,
            SourceKind = summary.SourceKind,
            ReceivedAt = summary.ReceivedAt,
            FirstContactedAt = summary.FirstContactedAt,
            ResponseMinutes = summary.ResponseMinutes,
            SlaBreached = summary.SlaBreached,
            MinutesToSlaBreach = summary.MinutesToSlaBreach,
            AssignedStaffId = summary.AssignedStaffId,
            LastActivityAt = summary.LastActivityAt,
            NextFollowUpOn = summary.NextFollowUpOn,
            FollowUpOverdue = summary.FollowUpOverdue,
            ContactAttempts = summary.ContactAttempts,
            AgeDays = summary.AgeDays,
            TourBookedFor = summary.TourBookedFor,
            TrialEndsOn = summary.TrialEndsOn,
            Goal = summary.Goal,

            MemberId = lead.MemberId,
            ContactId = lead.ContactId,
            DateOfBirth = lead.DateOfBirth,
            LeadSourceId = lead.LeadSourceId,
            CampaignId = lead.CampaignId,
            ReferredByMemberId = lead.ReferredByMemberId,
            Notes = lead.Notes,
            AssignedAt = lead.AssignedAt,
            TouredOn = lead.TouredOn,
            TrialStartedOn = lead.TrialStartedOn,
            WonOn = lead.WonOn,
            ResultingAgreementId = lead.ResultingAgreementId,
            WonValue = lead.WonValue,
            LostOn = lead.LostOn,
            LossReasonId = lead.LossReasonId,
            LossNote = lead.LossNote,
            AttributedCost = lead.AttributedCost,
            Activities = [.. lead.Activities.Where(a => !a.IsDeleted).OrderByDescending(a => a.OccurredAt).Select(FitnessMapper.ToDto)],
        };

        detail.ClubName = await db.Clubs.ForTenant(tenant)
            .Where(c => c.Id == lead.ClubId).Select(c => c.Name).FirstOrDefaultAsync();

        if (lead.AssignedStaffId is not null)
        {
            detail.AssignedStaffName = await db.Staff.ForTenant(tenant)
                .Where(s => s.Id == lead.AssignedStaffId)
                .Select(s => s.FirstName + " " + s.LastName)
                .FirstOrDefaultAsync();
        }

        if (lead.LossReasonId is not null)
        {
            detail.LossReasonName = await db.LossReasons.ForTenant(tenant)
                .Where(r => r.Id == lead.LossReasonId).Select(r => r.Name).FirstOrDefaultAsync();
        }

        if (lead.ReferredByMemberId is not null)
        {
            detail.ReferredByName = await db.Members.ForTenant(tenant)
                .Where(m => m.Id == lead.ReferredByMemberId)
                .Select(m => m.FirstName + " " + m.LastName)
                .FirstOrDefaultAsync();
        }

        if (lead.CampaignId is not null)
        {
            detail.CampaignName = await db.Campaigns.ForTenant(tenant)
                .Where(c => c.Id == lead.CampaignId).Select(c => c.Name).FirstOrDefaultAsync();
        }

        detail.Tours = [.. (await db.Tours.ForTenant(tenant)
            .Where(t => t.LeadId == leadId)
            .Include(t => t.Lead)
            .OrderByDescending(t => t.ScheduledFor)
            .ToListAsync())
            .Select(FitnessMapper.ToDto)];

        detail.Trials = [.. (await db.Trials.ForTenant(tenant)
            .Where(t => t.LeadId == leadId)
            .Include(t => t.Lead)
            .OrderByDescending(t => t.StartsOn)
            .ToListAsync())
            .Select(t => FitnessMapper.ToDto(t, now))];

        return detail;
    }

    public async Task<LeadDetailDto> CreateAsync(SaveLeadDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var lead = new FitnessLead
        {
            ClubId = request.ClubId,
            Status = LeadStatus.New,
            ReceivedAt = now,
        }.StampNew(tenant, userId);

        Apply(lead, request);

        // A promo code on the way in ties the lead to the campaign that produced it, which is how
        // marketing spend is judged later.
        if (!string.IsNullOrWhiteSpace(request.PromoCodeText))
        {
            var code = await db.PromoCodes.ForTenant(tenant)
                .Include(c => c.PromotionRule)
                .FirstOrDefaultAsync(c => c.CodeText.ToUpper() == request.PromoCodeText.ToUpper().Trim());

            if (code is not null)
            {
                lead.PromoCodeId = code.Id;
                lead.CampaignId ??= code.PromotionRule?.CampaignId;
            }
        }

        // Round-robin when nobody is named, so a web enquiry lands on a person rather than a queue.
        if (lead.AssignedStaffId is null)
            lead.AssignedStaffId = await NextConsultantAsync(request.ClubId);

        if (lead.AssignedStaffId is not null) lead.AssignedAt = now;

        db.Leads.Add(lead);

        db.LeadActivities.Add(new LeadActivity
        {
            LeadId = lead.Id,
            Kind = LeadActivityKind.Note,
            OccurredAt = now,
            Summary = "Enquiry received",
            ToStatus = LeadStatus.New,
        }.StampNew(tenant, userId));

        await db.SaveChangesAsync();
        return (await GetAsync(lead.Id))!;
    }

    public async Task<LeadDetailDto> UpdateAsync(Guid leadId, SaveLeadDto request, Guid userId)
    {
        var lead = await db.Leads.ForTenant(tenant).FirstOrDefaultAsync(l => l.Id == leadId)
            ?? throw new InvalidOperationException("Lead not found.");

        Apply(lead, request);
        lead.StampUpdated(userId);

        await db.SaveChangesAsync();
        return (await GetAsync(leadId))!;
    }

    public async Task<LeadDetailDto> AssignAsync(Guid leadId, Guid staffId, Guid userId)
    {
        var now = DateTime.UtcNow;

        var lead = await db.Leads.ForTenant(tenant).FirstOrDefaultAsync(l => l.Id == leadId)
            ?? throw new InvalidOperationException("Lead not found.");

        var staffName = await db.Staff.ForTenant(tenant)
            .Where(s => s.Id == staffId)
            .Select(s => s.FirstName + " " + s.LastName)
            .FirstOrDefaultAsync();

        lead.AssignedStaffId = staffId;
        lead.AssignedAt = now;
        lead.StampUpdated(userId);

        db.LeadActivities.Add(new LeadActivity
        {
            LeadId = leadId,
            Kind = LeadActivityKind.Note,
            OccurredAt = now,
            Summary = $"Assigned to {staffName}",
            StaffId = staffId,
            StaffName = staffName,
        }.StampNew(tenant, userId));

        await db.SaveChangesAsync();
        return (await GetAsync(leadId))!;
    }

    /// <summary>
    /// Logging a touch on a lead.
    ///
    /// The one that matters is the *first successful contact* — it stops the SLA clock and sets
    /// the response time. An email that bounced or a call that rang out is logged but does not
    /// count, which is the difference between measuring effort and measuring contact.
    /// </summary>
    public async Task<LeadActivityDto> LogActivityAsync(LogLeadActivityDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var lead = await db.Leads.ForTenant(tenant).FirstOrDefaultAsync(l => l.Id == request.LeadId)
            ?? throw new InvalidOperationException("Lead not found.");

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();
        var sla = settings?.LeadResponseSlaMinutes ?? 15;

        var staffName = await db.Staff.ForTenant(tenant)
            .Where(s => s.Id == userId)
            .Select(s => s.FirstName + " " + s.LastName)
            .FirstOrDefaultAsync();

        var activity = new LeadActivity
        {
            LeadId = request.LeadId,
            Kind = request.Kind,
            OccurredAt = now,
            Summary = request.Summary,
            Outcome = request.Outcome,
            StaffId = userId == Guid.Empty ? null : userId,
            StaffName = staffName,
            WasSuccessfulContact = request.WasSuccessfulContact,
            FromStatus = lead.Status,
            ToStatus = request.MoveToStatus,
            FollowUpOn = request.FollowUpOn,
        }.StampNew(tenant, userId);

        db.LeadActivities.Add(activity);

        lead.ContactAttempts++;
        lead.LastActivityAt = now;
        lead.NextFollowUpOn = request.FollowUpOn ?? lead.NextFollowUpOn;

        if (request.WasSuccessfulContact && lead.FirstContactedAt is null)
        {
            lead.FirstContactedAt = now;
            lead.ResponseMinutes = (int)(now - lead.ReceivedAt).TotalMinutes;
            lead.SlaBreached = lead.ResponseMinutes > sla;

            if (lead.Status == LeadStatus.New) lead.Status = LeadStatus.Contacted;
        }

        if (request.MoveToStatus is not null) lead.Status = request.MoveToStatus.Value;

        lead.StampUpdated(userId);
        await db.SaveChangesAsync();

        return FitnessMapper.ToDto(activity);
    }

    public async Task<LeadDetailDto> MoveStageAsync(Guid leadId, LeadStatus status, Guid userId)
    {
        var now = DateTime.UtcNow;

        var lead = await db.Leads.ForTenant(tenant).FirstOrDefaultAsync(l => l.Id == leadId)
            ?? throw new InvalidOperationException("Lead not found.");

        var from = lead.Status;
        lead.Status = status;
        lead.LastActivityAt = now;
        lead.StampUpdated(userId);

        db.LeadActivities.Add(new LeadActivity
        {
            LeadId = leadId,
            Kind = LeadActivityKind.StageChange,
            OccurredAt = now,
            Summary = $"Moved from {from} to {status}",
            FromStatus = from,
            ToStatus = status,
            StaffId = userId == Guid.Empty ? null : userId,
        }.StampNew(tenant, userId));

        await db.SaveChangesAsync();
        return (await GetAsync(leadId))!;
    }

    public async Task<LeadDetailDto> CloseAsync(CloseLeadDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var lead = await db.Leads.ForTenant(tenant).FirstOrDefaultAsync(l => l.Id == request.LeadId)
            ?? throw new InvalidOperationException("Lead not found.");

        if (request.Won)
        {
            lead.Status = LeadStatus.Won;
            lead.WonOn = now;
            lead.ResultingAgreementId = request.ResultingAgreementId;

            if (request.ResultingAgreementId is not null)
            {
                lead.WonValue = await db.Agreements.ForTenant(tenant)
                    .Where(a => a.Id == request.ResultingAgreementId)
                    .Select(a => (decimal?)a.Price)
                    .FirstOrDefaultAsync();
            }
        }
        else
        {
            var reason = request.LossReasonId is null
                ? null
                : await db.LossReasons.ForTenant(tenant).FirstOrDefaultAsync(r => r.Id == request.LossReasonId);

            if (reason?.RequiresNote == true && string.IsNullOrWhiteSpace(request.Note))
                throw new InvalidOperationException($"'{reason.Name}' needs a note explaining what happened.");

            lead.Status = LeadStatus.Lost;
            lead.LostOn = now;
            lead.LossReasonId = request.LossReasonId;
            lead.LossNote = request.Note;
        }

        lead.LastActivityAt = now;
        lead.NextFollowUpOn = null;
        lead.StampUpdated(userId);

        db.LeadActivities.Add(new LeadActivity
        {
            LeadId = lead.Id,
            Kind = LeadActivityKind.StageChange,
            OccurredAt = now,
            Summary = request.Won ? "Joined" : "Lost",
            Outcome = request.Note,
            ToStatus = lead.Status,
            StaffId = userId == Guid.Empty ? null : userId,
        }.StampNew(tenant, userId));

        await db.SaveChangesAsync();
        return (await GetAsync(lead.Id))!;
    }

    /// <summary>
    /// Turning a won lead into a member.
    ///
    /// Carries the attribution across — the source, the campaign, the consultant, the referrer —
    /// because that chain is what tells the club which marketing actually produced a member, and
    /// it is lost forever if the join starts from a blank form.
    /// </summary>
    public async Task<JoinResultDto> ConvertAsync(Guid leadId, JoinMemberDto request, Guid userId)
    {
        var lead = await db.Leads.ForTenant(tenant).FirstOrDefaultAsync(l => l.Id == leadId)
            ?? throw new InvalidOperationException("Lead not found.");

        request.LeadId = leadId;
        request.Member.LeadSourceId ??= lead.LeadSourceId;
        request.Member.ReferredByMemberId ??= lead.ReferredByMemberId;
        request.SoldByStaffId ??= lead.AssignedStaffId;

        if (string.IsNullOrWhiteSpace(request.Member.FirstName)) request.Member.FirstName = lead.FirstName;
        if (string.IsNullOrWhiteSpace(request.Member.LastName)) request.Member.LastName = lead.LastName ?? string.Empty;
        request.Member.Phone ??= lead.Phone;
        request.Member.Email ??= lead.Email;
        request.Member.DateOfBirth ??= lead.DateOfBirth;

        var result = await members.JoinAsync(request, userId);

        lead.MemberId = result.MemberId;
        lead.Status = LeadStatus.Won;
        lead.WonOn = DateTime.UtcNow;
        lead.ResultingAgreementId = result.AgreementId;
        lead.WonValue = result.RecurringAmount;
        lead.StampUpdated(userId);

        // Close the referral loop, so both sides get their reward without anybody remembering to.
        if (lead.ReferredByMemberId is not null)
        {
            var referral = await db.Referrals.ForTenant(tenant)
                .FirstOrDefaultAsync(r => r.LeadId == leadId && !r.Converted);

            if (referral is not null)
            {
                referral.Converted = true;
                referral.ConvertedOn = DateTime.UtcNow;
                referral.ReferredMemberId = result.MemberId;
                referral.StampUpdated(userId);

                await AwardReferralRewardsAsync(referral, userId);
            }
        }

        // A trial that converted is the trial's whole purpose; record it so the conversion rate
        // per trial type is real.
        var trial = await db.Trials.ForTenant(tenant)
            .FirstOrDefaultAsync(t => t.LeadId == leadId && !t.Converted);

        if (trial is not null)
        {
            trial.Converted = true;
            trial.ConvertedOn = DateTime.UtcNow;
            trial.MemberId = result.MemberId;
            trial.ResultingAgreementId = result.AgreementId;
            trial.StampUpdated(userId);
        }

        var tour = await db.Tours.ForTenant(tenant)
            .Where(t => t.LeadId == leadId && t.CompletedAt != null)
            .OrderByDescending(t => t.ScheduledFor)
            .FirstOrDefaultAsync();

        if (tour is not null && tour.CompletedAt?.Date == DateTime.UtcNow.Date)
        {
            tour.ConvertedOnDay = true;
            tour.StampUpdated(userId);
        }

        await db.SaveChangesAsync();
        return result;
    }

    // ── Tours ────────────────────────────────────────────────────────────────

    public async Task<TourDto> BookTourAsync(BookTourDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var lead = await db.Leads.ForTenant(tenant).FirstOrDefaultAsync(l => l.Id == request.LeadId)
            ?? throw new InvalidOperationException("Lead not found.");

        var tour = new Tour
        {
            LeadId = request.LeadId,
            ClubId = request.ClubId,
            StaffId = request.StaffId ?? lead.AssignedStaffId,
            ScheduledFor = request.ScheduledFor,
            DurationMinutes = request.DurationMinutes,
            Notes = request.Notes,
        }.StampNew(tenant, userId);

        db.Tours.Add(tour);

        lead.Status = LeadStatus.TourBooked;
        lead.TourBookedFor = request.ScheduledFor;
        lead.LastActivityAt = now;
        lead.NextFollowUpOn = request.ScheduledFor;
        lead.StampUpdated(userId);

        db.LeadActivities.Add(new LeadActivity
        {
            LeadId = request.LeadId,
            Kind = LeadActivityKind.Tour,
            OccurredAt = now,
            Summary = $"Tour booked for {request.ScheduledFor:ddd d MMM HH:mm}",
            ToStatus = LeadStatus.TourBooked,
            StaffId = userId == Guid.Empty ? null : userId,
            FollowUpOn = request.ScheduledFor,
        }.StampNew(tenant, userId));

        if (request.SendConfirmation)
        {
            db.MessageLog.Add(new MessageLog
            {
                LeadId = request.LeadId,
                ClubId = request.ClubId,
                Channel = lead.Email is not null ? MessageChannel.Email : MessageChannel.Sms,
                Status = MessageStatus.Queued,
                Recipient = lead.Email ?? lead.Phone,
                Subject = "Your visit is booked",
                BodyPreview = $"See you on {request.ScheduledFor:dddd d MMMM} at {request.ScheduledFor:HH:mm}.",
                QueuedAt = now,
            }.StampNew(tenant, userId));

            tour.ReminderSent = true;
        }

        await db.SaveChangesAsync();

        var saved = await db.Tours.ForTenant(tenant).Include(t => t.Lead).FirstAsync(t => t.Id == tour.Id);
        return FitnessMapper.ToDto(saved);
    }

    public async Task<TourDto> UpdateTourAsync(Guid tourId, TourDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var tour = await db.Tours.ForTenant(tenant)
            .Include(t => t.Lead)
            .FirstOrDefaultAsync(t => t.Id == tourId)
            ?? throw new InvalidOperationException("Tour not found.");

        tour.ScheduledFor = request.ScheduledFor;
        tour.StaffId = request.StaffId;
        tour.DurationMinutes = request.DurationMinutes;
        tour.ArrivedAt = request.ArrivedAt;
        tour.CompletedAt = request.CompletedAt;
        tour.WasNoShow = request.WasNoShow;
        tour.WasCancelled = request.WasCancelled;
        tour.CancellationReason = request.CancellationReason;
        tour.Notes = request.Notes;
        tour.StampUpdated(userId);

        var lead = tour.Lead;
        if (lead is not null)
        {
            if (tour.CompletedAt is not null && lead.Status == LeadStatus.TourBooked)
            {
                lead.Status = LeadStatus.Toured;
                lead.TouredOn = tour.CompletedAt;
            }

            // A no-show is a follow-up, not a dead end — most rebook when asked.
            if (tour.WasNoShow)
            {
                lead.NextFollowUpOn = now.AddHours(2);

                db.LeadActivities.Add(new LeadActivity
                {
                    LeadId = lead.Id,
                    Kind = LeadActivityKind.Tour,
                    OccurredAt = now,
                    Summary = "Did not arrive for their tour",
                    Outcome = "Follow up to rebook",
                    FollowUpOn = lead.NextFollowUpOn,
                }.StampNew(tenant, userId));
            }

            lead.LastActivityAt = now;
            lead.StampUpdated(userId);
        }

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(tour);
    }

    public async Task<List<TourDto>> GetToursAsync(Guid clubId, DateTime from, DateTime to, Guid? staffId)
    {
        var tours = await db.Tours.ForTenant(tenant)
            .Where(t => t.ClubId == clubId && t.ScheduledFor >= from && t.ScheduledFor <= to)
            .WhereIf(staffId is not null, t => t.StaffId == staffId)
            .Include(t => t.Lead)
            .OrderBy(t => t.ScheduledFor)
            .ToListAsync();

        var staffNames = await db.Staff.ForTenant(tenant)
            .Select(s => new { s.Id, Name = s.FirstName + " " + s.LastName })
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        return [.. tours.Select(t =>
        {
            var dto = FitnessMapper.ToDto(t);
            if (t.StaffId is not null) dto.StaffName = staffNames.GetValueOrDefault(t.StaffId.Value);
            return dto;
        })];
    }

    // ── Trials ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Issuing a trial.
    ///
    /// Backed by a real day pass so the door honours it, with a hard expiry and a conversion
    /// sequence attached. A trial that is only a note in the diary is a trial nobody follows up.
    /// </summary>
    public async Task<TrialPassDto> IssueTrialAsync(IssueTrialDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var lead = await db.Leads.ForTenant(tenant).FirstOrDefaultAsync(l => l.Id == request.LeadId)
            ?? throw new InvalidOperationException("Lead not found.");

        var endsOn = request.StartsOn.Date.AddDays(request.DurationDays);

        var trial = new TrialPass
        {
            LeadId = request.LeadId,
            ClubId = request.ClubId,
            PlanId = request.PlanId,
            StartsOn = request.StartsOn.Date,
            EndsOn = endsOn,
            VisitsAllowed = request.VisitsAllowed,
            Price = request.Price,
            IssuedByStaffId = userId == Guid.Empty ? null : userId,
        }.StampNew(tenant, userId);

        db.Trials.Add(trial);

        // The physical pass the door reads.
        var dayPass = await access.IssueDayPassAsync(new IssueDayPassDto
        {
            ClubId = request.ClubId,
            PlanId = request.PlanId,
            VisitorName = $"{lead.FirstName} {lead.LastName}".Trim(),
            VisitorPhone = lead.Phone,
            VisitorEmail = lead.Email,
            VisitorDateOfBirth = lead.DateOfBirth,
            ValidFrom = request.StartsOn,
            ValidTo = endsOn,
            MaxEntries = request.VisitsAllowed > 0 ? request.VisitsAllowed : 99,
            PriceOverride = request.Price,
            PaymentMethod = request.PaymentMethod ?? PaymentMethod.Cash,
            IsTrial = true,
            CreateLead = false,
            IssueTemporaryCredential = request.IssueCredential,
        }, userId);

        trial.DayPassId = dayPass.Id;

        lead.Status = LeadStatus.Trialling;
        lead.TrialStartedOn = trial.StartsOn;
        lead.TrialEndsOn = trial.EndsOn;
        lead.LastActivityAt = now;

        // Follow up before it ends, not after — a trial chased on day eight has already lapsed.
        lead.NextFollowUpOn = endsOn.AddDays(-1);
        lead.StampUpdated(userId);

        db.LeadActivities.Add(new LeadActivity
        {
            LeadId = lead.Id,
            Kind = LeadActivityKind.TrialIssued,
            OccurredAt = now,
            Summary = $"{request.DurationDays}-day trial issued, ending {endsOn:d MMM}",
            ToStatus = LeadStatus.Trialling,
            StaffId = userId == Guid.Empty ? null : userId,
            FollowUpOn = lead.NextFollowUpOn,
        }.StampNew(tenant, userId));

        if (request.StartConversionSequence)
        {
            trial.ConversionSequenceStarted = true;

            db.MessageLog.Add(new MessageLog
            {
                LeadId = lead.Id,
                ClubId = request.ClubId,
                Channel = lead.Email is not null ? MessageChannel.Email : MessageChannel.Sms,
                Status = MessageStatus.Queued,
                Recipient = lead.Email ?? lead.Phone,
                Subject = "Welcome — your trial starts now",
                BodyPreview = $"Your {request.DurationDays}-day trial runs until {endsOn:d MMMM}. " +
                              "Book your first class to make the most of it.",
                QueuedAt = now,
            }.StampNew(tenant, userId));
        }

        await db.SaveChangesAsync();

        var saved = await db.Trials.ForTenant(tenant).Include(t => t.Lead).FirstAsync(t => t.Id == trial.Id);
        return FitnessMapper.ToDto(saved, now);
    }

    public async Task<List<TrialPassDto>> GetTrialsAsync(Guid? clubId, bool activeOnly)
    {
        var now = DateTime.UtcNow;

        var trials = await db.Trials.ForTenant(tenant)
            .WhereIf(clubId is not null, t => t.ClubId == clubId)
            .WhereIf(activeOnly, t => !t.Converted && t.EndsOn >= now.Date)
            .Include(t => t.Lead)
            .OrderBy(t => t.EndsOn)
            .ToListAsync();

        var planNames = await db.Plans.ForTenant(tenant)
            .Select(p => new { p.Id, p.Name }).ToDictionaryAsync(p => p.Id, p => p.Name);

        var staffNames = await db.Staff.ForTenant(tenant)
            .Select(s => new { s.Id, Name = s.FirstName + " " + s.LastName })
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        return [.. trials.Select(t =>
        {
            var dto = FitnessMapper.ToDto(t, now);
            if (t.PlanId is not null) dto.PlanName = planNames.GetValueOrDefault(t.PlanId.Value);
            if (t.IssuedByStaffId is not null) dto.IssuedByName = staffNames.GetValueOrDefault(t.IssuedByStaffId.Value);
            return dto;
        })];
    }

    // ── Referrals ────────────────────────────────────────────────────────────

    public async Task<ReferralDto> CreateReferralAsync(CreateReferralDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var referrer = await db.Members.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == request.ReferrerMemberId)
            ?? throw new InvalidOperationException("Referring member not found.");

        var referral = new Referral
        {
            ReferrerMemberId = request.ReferrerMemberId,
            ClubId = request.ClubId,
            ReferredName = request.ReferredName,
            ReferredPhone = request.ReferredPhone,
            ReferredEmail = request.ReferredEmail,
            ReferredOn = now,
            ReferralCode = FitnessNumbering.NewReferralCode(referrer.FirstName),
            ReferrerRewardValue = request.ReferrerRewardValue,
            ReferrerRewardPoints = request.ReferrerRewardPoints,
            ReferredRewardValue = request.ReferredRewardValue,
            CampaignId = request.CampaignId,
        }.StampNew(tenant, userId);

        db.Referrals.Add(referral);

        if (request.CreateLead)
        {
            var source = await db.LeadSources.ForTenant(tenant)
                .FirstOrDefaultAsync(s => s.Kind == LeadSourceKind.Referral && s.IsActive);

            var parts = request.ReferredName.Split(' ', 2);

            var lead = new FitnessLead
            {
                ClubId = request.ClubId,
                FirstName = parts[0],
                LastName = parts.Length > 1 ? parts[1] : null,
                Phone = request.ReferredPhone,
                Email = request.ReferredEmail,
                Status = LeadStatus.New,
                ReceivedAt = now,
                LeadSourceId = source?.Id,
                ReferredByMemberId = request.ReferrerMemberId,
                CampaignId = request.CampaignId,
                Notes = $"Referred by {FitnessMapper.FullName(referrer)}",
                AssignedStaffId = await NextConsultantAsync(request.ClubId),
            }.StampNew(tenant, userId);

            db.Leads.Add(lead);
            referral.LeadId = lead.Id;
        }

        await db.SaveChangesAsync();

        var saved = await db.Referrals.ForTenant(tenant)
            .Include(r => r.ReferrerMember)
            .FirstAsync(r => r.Id == referral.Id);

        return FitnessMapper.ToDto(saved);
    }

    public async Task<List<ReferralDto>> GetReferralsAsync(Guid? clubId, Guid? memberId, bool? converted)
    {
        var referrals = await db.Referrals.ForTenant(tenant)
            .WhereIf(clubId is not null, r => r.ClubId == clubId)
            .WhereIf(memberId is not null, r => r.ReferrerMemberId == memberId)
            .WhereIf(converted is not null, r => r.Converted == converted)
            .Include(r => r.ReferrerMember)
            .OrderByDescending(r => r.ReferredOn)
            .Take(500)
            .ToListAsync();

        var campaignNames = await db.Campaigns.ForTenant(tenant)
            .Select(c => new { c.Id, c.Name }).ToDictionaryAsync(c => c.Id, c => c.Name);

        return [.. referrals.Select(r =>
        {
            var dto = FitnessMapper.ToDto(r);
            if (r.CampaignId is not null) dto.CampaignName = campaignNames.GetValueOrDefault(r.CampaignId.Value);
            return dto;
        })];
    }

    // ── Sources, reasons, targets ────────────────────────────────────────────

    public async Task<List<LeadSourceDto>> GetSourcesAsync(Guid? clubId, bool activeOnly)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var sources = await db.LeadSources.ForTenant(tenant)
            .WhereIf(clubId is not null, s => s.ClubId == clubId || s.ClubId == null)
            .WhereIf(activeOnly, s => s.IsActive)
            .OrderBy(s => s.DisplayOrder).ThenBy(s => s.Name)
            .ToListAsync();

        var monthLeads = await db.Leads.ForTenant(tenant)
            .Where(l => l.ReceivedAt >= monthStart && l.LeadSourceId != null)
            .WhereIf(clubId is not null, l => l.ClubId == clubId)
            .GroupBy(l => l.LeadSourceId!.Value)
            .Select(g => new { SourceId = g.Key, Leads = g.Count(), Joins = g.Count(l => l.WonOn != null) })
            .ToListAsync();

        return [.. sources.Select(s =>
        {
            var dto = FitnessMapper.ToDto(s);
            var stats = monthLeads.FirstOrDefault(m => m.SourceId == s.Id);

            dto.LeadsThisMonth = stats?.Leads ?? 0;
            dto.JoinsThisMonth = stats?.Joins ?? 0;
            dto.ConversionPercent = FitnessMapper.Percent(dto.JoinsThisMonth, dto.LeadsThisMonth);
            dto.CostPerLead = dto.LeadsThisMonth == 0 ? 0 : Math.Round(s.MonthlyCost / dto.LeadsThisMonth, 2);
            dto.CostPerAcquisition = dto.JoinsThisMonth == 0 ? 0 : Math.Round(s.MonthlyCost / dto.JoinsThisMonth, 2);

            return dto;
        })];
    }

    public async Task<LeadSourceDto> SaveSourceAsync(Guid? id, LeadSourceDto request, Guid userId)
    {
        LeadSource source;
        if (id is null)
        {
            source = new LeadSource().StampNew(tenant, userId);
            db.LeadSources.Add(source);
        }
        else
        {
            source = await db.LeadSources.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == id)
                ?? throw new InvalidOperationException("Lead source not found.");
            source.StampUpdated(userId);
        }

        source.Name = request.Name;
        source.Kind = request.Kind;
        source.ClubId = request.ClubId;
        source.DisplayOrder = request.DisplayOrder;
        source.MonthlyCost = request.MonthlyCost;
        source.TrackingCode = request.TrackingCode;
        source.IsActive = request.IsActive;

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(source);
    }

    public async Task<List<LossReasonDto>> GetLossReasonsAsync()
    {
        var reasons = await db.LossReasons.ForTenant(tenant)
            .OrderBy(r => r.DisplayOrder).ThenBy(r => r.Name)
            .ToListAsync();

        var counts = await db.Leads.ForTenant(tenant)
            .Where(l => l.LossReasonId != null)
            .GroupBy(l => l.LossReasonId!.Value)
            .Select(g => new { ReasonId = g.Key, Count = g.Count() })
            .ToListAsync();

        return [.. reasons.Select(r =>
        {
            var dto = FitnessMapper.ToDto(r);
            dto.UseCount = counts.FirstOrDefault(c => c.ReasonId == r.Id)?.Count ?? 0;
            return dto;
        })];
    }

    public async Task<List<SalesTargetDto>> GetTargetsAsync(Guid? clubId, DateTime? periodStart)
    {
        var now = DateTime.UtcNow;
        var start = periodStart?.Date ?? new DateTime(now.Year, now.Month, 1);

        var targets = await db.SalesTargets.ForTenant(tenant)
            .Where(t => t.PeriodStart == start)
            .WhereIf(clubId is not null, t => t.ClubId == clubId)
            .ToListAsync();

        var staff = await db.Staff.ForTenant(tenant)
            .Select(s => new { s.Id, Name = s.FirstName + " " + s.LastName, s.PhotoUrl })
            .ToListAsync();

        var clubNames = await db.Clubs.ForTenant(tenant)
            .Select(c => new { c.Id, c.Name }).ToDictionaryAsync(c => c.Id, c => c.Name);

        var results = new List<SalesTargetDto>();

        foreach (var target in targets)
        {
            // Recompute the actual so the board is live rather than as of the last nightly job.
            target.ActualValue = target.MetricName.ToLowerInvariant() switch
            {
                "joins" => await db.Leads.ForTenant(tenant)
                    .CountAsync(l => l.ClubId == target.ClubId
                                  && (target.StaffId == null || l.AssignedStaffId == target.StaffId)
                                  && l.WonOn >= target.PeriodStart && l.WonOn <= target.PeriodEnd),
                "tours" => await db.Tours.ForTenant(tenant)
                    .CountAsync(t => t.ClubId == target.ClubId
                                  && (target.StaffId == null || t.StaffId == target.StaffId)
                                  && t.CompletedAt >= target.PeriodStart && t.CompletedAt <= target.PeriodEnd),
                "revenue" => await db.Agreements.ForTenant(tenant)
                    .Where(a => a.ClubId == target.ClubId
                             && (target.StaffId == null || a.SoldByStaffId == target.StaffId)
                             && a.StartsOn >= target.PeriodStart && a.StartsOn <= target.PeriodEnd)
                    .SumAsync(a => (decimal?)a.Price) ?? 0m,
                _ => target.ActualValue,
            };

            target.AchievementPercent = FitnessMapper.Percent(target.ActualValue, target.TargetValue);
            target.IsAchieved = target.ActualValue >= target.TargetValue;

            var person = staff.FirstOrDefault(s => s.Id == target.StaffId);

            var elapsed = (decimal)(now.Date - target.PeriodStart).TotalDays;
            var totalDays = Math.Max(1, (decimal)(target.PeriodEnd - target.PeriodStart).TotalDays);
            var expected = Math.Round(target.TargetValue * Math.Clamp(elapsed / totalDays, 0, 1), 1);

            results.Add(new SalesTargetDto
            {
                Id = target.Id,
                ClubId = target.ClubId,
                ClubName = clubNames.GetValueOrDefault(target.ClubId),
                StaffId = target.StaffId,
                StaffName = person?.Name,
                PhotoUrl = person?.PhotoUrl,
                PeriodStart = target.PeriodStart,
                PeriodEnd = target.PeriodEnd,
                MetricName = target.MetricName,
                TargetValue = target.TargetValue,
                ActualValue = target.ActualValue,
                AchievementPercent = target.AchievementPercent,
                BonusOnAchievement = target.BonusOnAchievement,
                IsAchieved = target.IsAchieved,
                ExpectedByNow = expected,
                IsOnPace = target.ActualValue >= expected,
            });
        }

        await db.SaveChangesAsync();

        var rank = 1;
        foreach (var result in results.OrderByDescending(r => r.AchievementPercent)) result.Rank = rank++;

        return [.. results.OrderBy(r => r.Rank)];
    }

    public async Task<SalesTargetDto> SaveTargetAsync(Guid? id, SalesTargetDto request, Guid userId)
    {
        SalesTarget target;
        if (id is null)
        {
            target = new SalesTarget { ClubId = request.ClubId }.StampNew(tenant, userId);
            db.SalesTargets.Add(target);
        }
        else
        {
            target = await db.SalesTargets.ForTenant(tenant).FirstOrDefaultAsync(t => t.Id == id)
                ?? throw new InvalidOperationException("Target not found.");
            target.StampUpdated(userId);
        }

        target.StaffId = request.StaffId;
        target.PeriodStart = request.PeriodStart.Date;
        target.PeriodEnd = request.PeriodEnd.Date;
        target.MetricName = request.MetricName;
        target.TargetValue = request.TargetValue;
        target.BonusOnAchievement = request.BonusOnAchievement;

        await db.SaveChangesAsync();

        return (await GetTargetsAsync(request.ClubId, request.PeriodStart)).First(t => t.Id == target.Id);
    }

    /// <summary>
    /// Flags leads that have blown their first-response SLA. Runs on a short timer.
    ///
    /// A breach also raises a task, because a flag on a board that nobody opens is not an
    /// escalation.
    /// </summary>
    public async Task<int> FlagSlaBreachesAsync()
    {
        var now = DateTime.UtcNow;

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();
        var sla = settings?.LeadResponseSlaMinutes ?? 15;
        var cutoff = now.AddMinutes(-sla);

        var breaching = await db.Leads.ForTenant(tenant)
            .Where(l => !l.SlaBreached && l.FirstContactedAt == null && l.ReceivedAt < cutoff
                     && l.Status != LeadStatus.Won && l.Status != LeadStatus.Lost)
            .ToListAsync();

        foreach (var lead in breaching)
        {
            lead.SlaBreached = true;
            lead.StampUpdated(Guid.Empty);

            db.LeadActivities.Add(new LeadActivity
            {
                LeadId = lead.Id,
                Kind = LeadActivityKind.Note,
                OccurredAt = now,
                Summary = $"No contact within {sla} minutes — response target missed",
            }.StampNew(tenant));

            if (lead.AssignedStaffId is not null)
            {
                db.MessageLog.Add(new MessageLog
                {
                    StaffId = lead.AssignedStaffId,
                    LeadId = lead.Id,
                    ClubId = lead.ClubId,
                    Channel = MessageChannel.DeskAlert,
                    Status = MessageStatus.Queued,
                    Subject = "Lead waiting",
                    BodyPreview = $"{lead.FirstName} {lead.LastName} enquired {(int)(now - lead.ReceivedAt).TotalMinutes} minutes ago.",
                    QueuedAt = now,
                }.StampNew(tenant));
            }
        }

        await db.SaveChangesAsync();
        return breaching.Count;
    }

    // ═══ Internals ═══════════════════════════════════════════════════════════

    private static void Apply(FitnessLead l, SaveLeadDto r)
    {
        l.ClubId = r.ClubId;
        l.FirstName = r.FirstName;
        l.LastName = r.LastName;
        l.Phone = r.Phone;
        l.Email = r.Email;
        l.DateOfBirth = r.DateOfBirth;
        l.LeadSourceId = r.LeadSourceId;
        l.CampaignId = r.CampaignId;
        l.ReferredByMemberId = r.ReferredByMemberId;
        l.Goal = r.Goal;
        l.InterestedInPlanId = r.InterestedInPlanId?.ToString();
        l.Notes = r.Notes;
        if (r.AssignedStaffId is not null) l.AssignedStaffId = r.AssignedStaffId;
        if (r.NextFollowUpOn is not null) l.NextFollowUpOn = r.NextFollowUpOn;
    }

    /// <summary>
    /// Round-robin assignment: whoever has the fewest open leads gets the next one.
    ///
    /// Fewest-open rather than strict rotation, because rotation hands leads to whoever is on
    /// holiday and load-balancing does not.
    /// </summary>
    private async Task<Guid?> NextConsultantAsync(Guid clubId)
    {
        var consultants = await db.Staff.ForTenant(tenant)
            .Where(s => s.ClubId == clubId && s.IsActive && s.CanSell)
            .Select(s => s.Id)
            .ToListAsync();

        if (consultants.Count == 0) return null;

        var loads = await db.Leads.ForTenant(tenant)
            .Where(l => l.AssignedStaffId != null && consultants.Contains(l.AssignedStaffId.Value)
                     && l.Status != LeadStatus.Won && l.Status != LeadStatus.Lost)
            .GroupBy(l => l.AssignedStaffId!.Value)
            .Select(g => new { StaffId = g.Key, Count = g.Count() })
            .ToListAsync();

        return consultants
            .OrderBy(id => loads.FirstOrDefault(l => l.StaffId == id)?.Count ?? 0)
            .First();
    }

    private async Task AwardReferralRewardsAsync(Referral referral, Guid userId)
    {
        var now = DateTime.UtcNow;

        if (referral.ReferrerRewardPoints > 0)
        {
            var account = await db.LoyaltyAccounts.ForTenant(tenant)
                .FirstOrDefaultAsync(a => a.MemberId == referral.ReferrerMemberId);

            if (account is not null)
            {
                account.PointsBalance += referral.ReferrerRewardPoints;
                account.LifetimePoints += referral.ReferrerRewardPoints;

                db.LoyaltyTransactions.Add(new LoyaltyTransaction
                {
                    LoyaltyAccountId = account.Id,
                    MemberId = referral.ReferrerMemberId,
                    Kind = LoyaltyEventKind.Referral,
                    OccurredAt = now,
                    Points = referral.ReferrerRewardPoints,
                    BalanceAfter = account.PointsBalance,
                    Reason = $"Referred {referral.ReferredName}",
                    SourceEntityId = referral.Id,
                    SourceEntityType = nameof(Referral),
                }.StampNew(tenant, userId));
            }
        }

        if (referral.ReferrerRewardValue > 0)
        {
            var referrer = await db.Members.ForTenant(tenant)
                .FirstOrDefaultAsync(m => m.Id == referral.ReferrerMemberId);

            if (referrer is not null)
            {
                referrer.CreditBalance += referral.ReferrerRewardValue;
                referrer.StampUpdated(userId);

                db.MemberNotes.Add(new MemberNote
                {
                    MemberId = referrer.Id,
                    Kind = InteractionKind.SystemEvent,
                    Body = $"Referral reward of {referral.ReferrerRewardValue:0.00} credited — " +
                           $"{referral.ReferredName} joined.",
                    OccurredAt = now,
                }.StampNew(tenant, userId));
            }
        }

        referral.ReferrerRewarded = true;
        referral.ReferrerRewardedOn = now;
    }

    private static int Median(List<int> values)
    {
        if (values.Count == 0) return 0;
        var sorted = values.OrderBy(v => v).ToList();
        var mid = sorted.Count / 2;
        return sorted.Count % 2 == 0 ? (sorted[mid - 1] + sorted[mid]) / 2 : sorted[mid];
    }
}
