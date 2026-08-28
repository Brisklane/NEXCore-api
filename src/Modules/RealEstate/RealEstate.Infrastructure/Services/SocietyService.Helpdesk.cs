using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Application.Services.Interfaces;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// The resident helpdesk, notices and polls, and building control.
///
/// The helpdesk's whole value is the SLA clock. A complaint that has been open for three days with
/// nobody assigned is the thing that loses a management contract, so the due time is set from
/// category and priority at the moment it is raised, breaches escalate on their own, and every
/// update is visible to the resident unless somebody deliberately marks it internal.
/// </summary>
public partial class SocietyService
{
    // ═══ Helpdesk ════════════════════════════════════════════════════════════

    public async Task<ComplaintDetailDto> CreateComplaintAsync(ComplaintCreateDto dto, Guid userId)
    {
        _ = await RequireAsync<Society>(dto.SocietyId, "That society does not exist.");

        var partyId = dto.PartyId ?? Guid.Empty;
        Resident? resident = null;

        if (dto.ResidentId is not null)
        {
            resident = await Db.Residents.ForCompany(Tenant).FirstOrDefaultAsync(r => r.Id == dto.ResidentId);
            if (resident is not null) partyId = resident.PartyId;
        }

        if (partyId == Guid.Empty)
            throw new InvalidOperationException("Say who is raising the complaint.");

        var now = DateTime.UtcNow;

        var complaint = new Complaint
        {
            TicketNumber = await numbering.NextComplaintNumberAsync(now),
            SocietyId = dto.SocietyId,
            UnitId = dto.UnitId ?? resident?.UnitId,
            ResidentId = dto.ResidentId,
            PartyId = partyId,
            Category = dto.Category,
            Priority = dto.Priority,
            Status = TicketStatus.Open,
            Title = dto.Title,
            Description = dto.Description,
            Location = dto.Location,
            PhotoUrls = dto.PhotoUrls.Count == 0 ? null : string.Join('\n', dto.PhotoUrls),
            RaisedAt = now,
            RaisedVia = dto.RaisedVia,
            SlaDueAt = now.AddHours(SlaHours(dto.Category, dto.Priority)),
        }.StampNew(Tenant, userId);

        Db.Complaints.Add(complaint);

        complaint.Updates.Add(new ComplaintUpdate
        {
            ComplaintId = complaint.Id,
            PostedAt = now,
            UserId = userId,
            NewStatus = TicketStatus.Open,
            Note = "Logged.",
            IsVisibleToResident = true,
            IsFromResident = true,
        }.StampNew(Tenant, userId));

        await Db.SaveChangesAsync();

        await QueueNotificationAsync(
            "ComplaintLogged",
            $"Complaint {complaint.TicketNumber} logged",
            $"We aim to respond by {complaint.SlaDueAt:ddd d MMM 'at' HH:mm}.",
            $"/realestate/complaints/{complaint.Id}",
            recipientPartyId: partyId,
            entityType: "Complaint",
            entityId: complaint.Id);

        await Db.SaveChangesAsync();
        return (await GetComplaintAsync(complaint.Id))!;
    }

    /// <summary>
    /// How long we have. A lift with somebody in it is not the same problem as a flickering
    /// corridor light, and one SLA for both makes the number meaningless.
    /// </summary>
    private static int SlaHours(ComplaintCategory category, TicketPriority priority) => priority switch
    {
        TicketPriority.Emergency => 2,
        TicketPriority.High => category is ComplaintCategory.Lift or ComplaintCategory.Security
                                        or ComplaintCategory.Water or ComplaintCategory.Electrical ? 4 : 8,
        TicketPriority.Normal => category is ComplaintCategory.Lift or ComplaintCategory.Security ? 12 : 48,
        _ => 96,
    };

    public async Task<ComplaintDetailDto?> GetComplaintAsync(Guid id)
    {
        var complaint = await Db.Complaints.ForCompany(Tenant)
            .Include(c => c.Updates)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (complaint is null) return null;

        var head = (await MapComplaintListAsync([complaint]))[0];

        var users = await AgentUserNamesAsync(complaint.Updates.Select(u => u.UserId));

        var detail = new ComplaintDetailDto
        {
            Id = head.Id,
            TicketNumber = head.TicketNumber,
            SocietyId = head.SocietyId,
            SocietyName = head.SocietyName,
            UnitId = head.UnitId,
            UnitLabel = head.UnitLabel,
            BlockName = head.BlockName,
            PartyId = head.PartyId,
            PartyName = head.PartyName,
            Phone = head.Phone,
            Category = head.Category,
            Priority = head.Priority,
            Status = head.Status,
            Title = head.Title,
            Location = head.Location,
            RaisedAt = head.RaisedAt,
            RaisedVia = head.RaisedVia,
            AssignedToName = head.AssignedToName,
            ContractorName = head.ContractorName,
            AcknowledgedAt = head.AcknowledgedAt,
            SlaDueAt = head.SlaDueAt,
            SlaBreached = head.SlaBreached,
            MinutesToSla = head.MinutesToSla,
            EscalationLevel = head.EscalationLevel,
            WorkOrderId = head.WorkOrderId,
            ResolvedAt = head.ResolvedAt,
            SatisfactionRating = head.SatisfactionRating,
            WasReopened = head.WasReopened,
            AgeHours = head.AgeHours,

            Description = complaint.Description ?? string.Empty,
            PhotoUrls = string.IsNullOrWhiteSpace(complaint.PhotoUrls)
                ? []
                : complaint.PhotoUrls.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList(),
            Resolution = complaint.Resolution,
            ClosedAt = complaint.ClosedAt,
            FeedbackNote = complaint.FeedbackNote,
            ReopenCount = complaint.ReopenCount,

            Updates = complaint.Updates.OrderByDescending(u => u.PostedAt).Select(u => new ComplaintUpdateDto
            {
                Id = u.Id,
                PostedAt = u.PostedAt,
                UserName = u.UserId is null ? null : users.GetValueOrDefault(u.UserId.Value),
                NewStatus = u.NewStatus,
                Note = u.Note,
                PhotoUrls = string.IsNullOrWhiteSpace(u.PhotoUrls)
                    ? []
                    : u.PhotoUrls.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList(),
                IsVisibleToResident = u.IsVisibleToResident,
                IsFromResident = u.IsFromResident,
            }).ToList(),
        };

        return detail;
    }

    public async Task<PaginatedResponse<ComplaintListItemDto>> GetComplaintsAsync(ComplaintSearchDto query)
    {
        var now = DateTime.UtcNow;

        var q = Db.Complaints.ForCompany(Tenant)
            .WhereIf(query.SocietyId.HasValue, c => c.SocietyId == query.SocietyId)
            .WhereIf(query.Categories.Count > 0, c => query.Categories.Contains(c.Category))
            .WhereIf(query.Statuses.Count > 0, c => query.Statuses.Contains(c.Status))
            .WhereIf(query.Priority.HasValue, c => c.Priority == query.Priority)
            .WhereIf(query.AssignedToUserId.HasValue, c => c.AssignedToUserId == query.AssignedToUserId)
            .WhereIf(query.UnitId.HasValue, c => c.UnitId == query.UnitId)
            .WhereIf(query.BreachingSlaOnly == true, c => c.SlaDueAt != null && c.SlaDueAt < now
                                                   && c.Status != TicketStatus.Closed && c.Status != TicketStatus.Resolved)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search),
                c => c.TicketNumber.Contains(query.Search!) || c.Title.Contains(query.Search!))
            .OrderByDescending(c => c.Priority).ThenBy(c => c.SlaDueAt);

        return await PageAsync(q, query, MapComplaintListAsync);
    }

    private async Task<List<ComplaintListItemDto>> MapComplaintListAsync(List<Complaint> complaints)
    {
        if (complaints.Count == 0) return [];

        var now = DateTime.UtcNow;

        var people = await Db.Parties.ForCompany(Tenant)
            .Where(p => complaints.Select(c => c.PartyId).Contains(p.Id))
            .ToListAsync();

        var unitIds = complaints.Where(c => c.UnitId.HasValue).Select(c => c.UnitId!.Value).Distinct().ToList();

        var units = unitIds.Count == 0
            ? []
            : await Db.Units.ForCompany(Tenant)
                .Where(u => unitIds.Contains(u.Id))
                .Select(u => new { u.Id, u.UnitNumber, u.ProjectNodeId })
                .ToListAsync();

        var nodeIds = units.Where(u => u.ProjectNodeId.HasValue).Select(u => u.ProjectNodeId!.Value).Distinct().ToList();

        var nodes = nodeIds.Count == 0
            ? []
            : await Db.ProjectNodes.ForCompany(Tenant)
                .Where(n => nodeIds.Contains(n.Id))
                .ToDictionaryAsync(n => n.Id, n => n.Name);

        var societies = await Db.Societies.ForCompany(Tenant)
            .Where(s => complaints.Select(c => c.SocietyId).Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Name);

        var assignees = await AgentUserNamesAsync(complaints.Select(c => c.AssignedToUserId));

        var contractorIds = complaints.Where(c => c.ContractorId.HasValue).Select(c => c.ContractorId!.Value).Distinct().ToList();

        var contractors = contractorIds.Count == 0
            ? []
            : await Db.Contractors.ForCompany(Tenant)
                .Where(c => contractorIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, c => c.Name);

        return complaints.Select(c =>
        {
            var person = people.FirstOrDefault(p => p.Id == c.PartyId);
            var unit = c.UnitId is null ? null : units.FirstOrDefault(u => u.Id == c.UnitId);
            var live = c.Status is not (TicketStatus.Closed or TicketStatus.Resolved);

            return new ComplaintListItemDto
            {
                Id = c.Id,
                TicketNumber = c.TicketNumber,
                SocietyId = c.SocietyId,
                SocietyName = societies.GetValueOrDefault(c.SocietyId),
                UnitId = c.UnitId,
                UnitLabel = unit?.UnitNumber,
                BlockName = unit?.ProjectNodeId is null ? null : nodes.GetValueOrDefault(unit.ProjectNodeId.Value),
                PartyId = c.PartyId,
                PartyName = person is null ? "—" : RealEstateMapper.DisplayName(person),
                Phone = person?.PrimaryPhone,
                Category = c.Category,
                Priority = c.Priority,
                Status = c.Status,
                Title = c.Title,
                Location = c.Location,
                RaisedAt = c.RaisedAt,
                RaisedVia = c.RaisedVia,
                AssignedToName = c.AssignedToUserId is null ? null : assignees.GetValueOrDefault(c.AssignedToUserId.Value),
                ContractorName = c.ContractorId is null ? null : contractors.GetValueOrDefault(c.ContractorId.Value),
                AcknowledgedAt = c.AcknowledgedAt,
                SlaDueAt = c.SlaDueAt,
                SlaBreached = live && c.SlaDueAt is not null && c.SlaDueAt < now,

                // Negative means overdue. The board sorts by it, so the worst is at the top.
                MinutesToSla = c.SlaDueAt is null || !live ? null : (int)(c.SlaDueAt.Value - now).TotalMinutes,

                EscalationLevel = c.EscalationLevel,
                WorkOrderId = c.WorkOrderId,
                ResolvedAt = c.ResolvedAt,
                SatisfactionRating = c.SatisfactionRating,
                WasReopened = c.WasReopened,
                AgeHours = (int)((c.ClosedAt ?? now) - c.RaisedAt).TotalHours,
            };
        }).ToList();
    }

    public async Task<ComplaintDetailDto> UpdateComplaintAsync(
        Guid id, TicketStatus? status, string note, List<string>? photoUrls, bool visibleToResident, Guid userId)
    {
        var complaint = await Db.Complaints.ForCompany(Tenant)
            .Include(c => c.Updates)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new InvalidOperationException("That complaint does not exist.");

        var now = DateTime.UtcNow;

        if (status is not null)
        {
            // Reopening is counted, because a ticket closed three times is a job that was never
            // actually done and the pattern is worth seeing.
            if (complaint.Status is TicketStatus.Closed or TicketStatus.Resolved
                && status is not (TicketStatus.Closed or TicketStatus.Resolved))
            {
                complaint.WasReopened = true;
                complaint.ReopenCount++;
                complaint.ResolvedAt = null;
                complaint.ClosedAt = null;
            }

            complaint.Status = status.Value;

            complaint.AcknowledgedAt ??= status is TicketStatus.Acknowledged or TicketStatus.Assigned or TicketStatus.InProgress
                ? now
                : null;

            if (status == TicketStatus.Resolved)
            {
                complaint.ResolvedAt = now;
                complaint.Resolution = note;
            }

            if (status == TicketStatus.Closed) complaint.ClosedAt = now;
        }

        complaint.Updates.Add(new ComplaintUpdate
        {
            ComplaintId = complaint.Id,
            PostedAt = now,
            UserId = userId,
            NewStatus = status,
            Note = note,
            PhotoUrls = photoUrls is null || photoUrls.Count == 0 ? null : string.Join('\n', photoUrls),
            IsVisibleToResident = visibleToResident,
        }.StampNew(Tenant, userId));

        complaint.StampUpdated(userId);

        if (visibleToResident)
        {
            await QueueNotificationAsync(
                "ComplaintUpdated",
                $"Update on {complaint.TicketNumber}",
                note,
                $"/realestate/complaints/{complaint.Id}",
                recipientPartyId: complaint.PartyId,
                entityType: "Complaint",
                entityId: complaint.Id);
        }

        await Db.SaveChangesAsync();
        return (await GetComplaintAsync(id))!;
    }

    public async Task<ComplaintDetailDto> AssignComplaintAsync(Guid id, Guid? userId2, Guid? contractorId, Guid userId)
    {
        var complaint = await Db.Complaints.ForCompany(Tenant)
            .Include(c => c.Updates)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new InvalidOperationException("That complaint does not exist.");

        var now = DateTime.UtcNow;

        complaint.AssignedToUserId = userId2;
        complaint.ContractorId = contractorId;
        complaint.Status = TicketStatus.Assigned;
        complaint.AcknowledgedAt ??= now;
        complaint.StampUpdated(userId);

        // A complaint needing a trade becomes a work order, so it joins the same queue the
        // maintenance team already works from rather than living in a second list.
        if (complaint.WorkOrderId is null && contractorId is not null)
        {
            var workOrder = new WorkOrder
            {
                OrderNumber = await numbering.NextWorkOrderNumberAsync(now),
                Source = WorkOrderSource.ResidentComplaint,
                SocietyId = complaint.SocietyId,
                UnitId = complaint.UnitId,
                ComplaintId = complaint.Id,
                Title = complaint.Title,
                Description = complaint.Description,
                LocationDetail = complaint.Location,
                Priority = complaint.Priority,
                Status = WorkOrderStatus.Assigned,
                ContractorId = contractorId,
                AssignedToUserId = userId2,
                AssignedAt = now,
                RaisedByPartyId = complaint.PartyId,
                RaisedAt = complaint.RaisedAt,
                CompletionDueAt = complaint.SlaDueAt,
                CostBearer = CostBearer.Society,
            }.StampNew(Tenant, userId);

            Db.WorkOrders.Add(workOrder);
            await Db.SaveChangesAsync();

            complaint.WorkOrderId = workOrder.Id;
        }

        var names = await AgentUserNamesAsync([userId2]);

        complaint.Updates.Add(new ComplaintUpdate
        {
            ComplaintId = complaint.Id,
            PostedAt = now,
            UserId = userId,
            NewStatus = TicketStatus.Assigned,
            Note = userId2 is null
                ? "Assigned to a contractor."
                : $"Assigned to {names.GetValueOrDefault(userId2.Value, "a team member")}.",
            IsVisibleToResident = true,
        }.StampNew(Tenant, userId));

        await Db.SaveChangesAsync();
        return (await GetComplaintAsync(id))!;
    }

    public async Task<ComplaintDetailDto> RateComplaintAsync(Guid id, int rating, string? note, Guid userId)
    {
        var complaint = await RequireAsync<Complaint>(id, "That complaint does not exist.");

        if (complaint.Status is not (TicketStatus.Resolved or TicketStatus.Closed))
            throw new InvalidOperationException("This complaint is still open. It can be rated once it is resolved.");

        complaint.SatisfactionRating = Math.Clamp(rating, 1, 5);
        complaint.FeedbackNote = note;
        complaint.StampUpdated(userId);

        // A poor rating reopens the ticket rather than closing the loop on a job the resident
        // says is not done. Closing on a one-star rating is how a helpdesk loses trust.
        if (complaint.SatisfactionRating <= 2)
        {
            complaint.Status = TicketStatus.Reopened;
            complaint.WasReopened = true;
            complaint.ReopenCount++;
            complaint.ClosedAt = null;

            Db.ComplaintUpdates.Add(new ComplaintUpdate
            {
                ComplaintId = complaint.Id,
                PostedAt = DateTime.UtcNow,
                UserId = userId,
                NewStatus = TicketStatus.Reopened,
                Note = $"Reopened automatically — the resident rated this {complaint.SatisfactionRating} out of 5.",
                IsVisibleToResident = true,
                IsFromResident = true,
            }.StampNew(Tenant, userId));
        }
        else complaint.Status = TicketStatus.Closed;

        await Db.SaveChangesAsync();
        return (await GetComplaintAsync(id))!;
    }

    /// <summary>
    /// Escalates anything past its SLA. Run on a schedule. Escalation is stepped rather than
    /// one-shot, so a ticket that stays broken keeps climbing rather than notifying once and
    /// being forgotten.
    /// </summary>
    public async Task<int> EscalateBreachedComplaintsAsync()
    {
        var now = DateTime.UtcNow;

        var breached = await Db.Complaints.ForCompany(Tenant)
            .Where(c => c.SlaDueAt != null && c.SlaDueAt < now
                     && c.Status != TicketStatus.Closed
                     && c.Status != TicketStatus.Resolved)
            .ToListAsync();

        var escalated = 0;

        foreach (var complaint in breached)
        {
            var overdueHours = (now - complaint.SlaDueAt!.Value).TotalHours;

            var level = overdueHours switch
            {
                < 4 => 1,
                < 24 => 2,
                < 72 => 3,
                _ => 4,
            };

            if (level <= complaint.EscalationLevel) continue;

            complaint.EscalationLevel = level;
            complaint.EscalatedAt = now;
            complaint.SlaBreached = true;
            complaint.Status = TicketStatus.Escalated;
            complaint.StampUpdated(complaint.AssignedToUserId ?? Guid.Empty);

            var audience = level switch
            {
                1 => "the assigned engineer",
                2 => "the facility manager",
                3 => "the committee",
                _ => "the managing agent",
            };

            await QueueNotificationAsync(
                "ComplaintEscalated",
                $"{complaint.TicketNumber} escalated to {audience}",
                $"{complaint.Title} has been open {overdueHours:N0} hours past its response time.",
                $"/realestate/complaints/{complaint.Id}",
                recipientUserId: complaint.AssignedToUserId,
                entityType: "Complaint",
                entityId: complaint.Id,
                severity: level >= 3 ? AlertSeverity.Critical : AlertSeverity.Warning);

            escalated++;
        }

        await Db.SaveChangesAsync();
        return escalated;
    }

    // ═══ Notices and polls ═══════════════════════════════════════════════════

    public async Task<PaginatedResponse<SocietyNoticeDto>> GetNoticesAsync(ListQueryDto query, Guid societyId)
    {
        var q = Db.SocietyNotices.ForCompany(Tenant)
            .Where(n => n.SocietyId == societyId)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), n => n.Title.Contains(query.Search!))
            .OrderByDescending(n => n.IsPinned).ThenByDescending(n => n.PublishedAt);

        return await PageAsync(q, query, MapNoticesAsync);
    }

    private async Task<List<SocietyNoticeDto>> MapNoticesAsync(List<SocietyNotice> notices)
    {
        if (notices.Count == 0) return [];

        var today = Today;
        var users = await AgentUserNamesAsync(notices.Select(n => n.PublishedByUserId));

        return notices.Select(n => new SocietyNoticeDto
        {
            Id = n.Id,
            SocietyId = n.SocietyId,
            Title = n.Title,
            Body = n.Body,
            NoticeType = n.NoticeType,
            Severity = n.Severity,
            PublishedAt = n.PublishedAt,
            ExpiresOn = n.ExpiresOn,
            PublishedByName = n.PublishedByUserId is null ? null : users.GetValueOrDefault(n.PublishedByUserId.Value),
            IsPinned = n.IsPinned,
            SendAsBroadcast = n.SendAsBroadcast,
            AttachmentUrl = n.AttachmentUrl,
            TargetBlocks = n.TargetBlocks,
            ReadCount = n.ReadCount,
            IsExpired = n.ExpiresOn is not null && n.ExpiresOn < today,
            IsActive = n.IsActive && (n.ExpiresOn is null || n.ExpiresOn >= today),
        }).ToList();
    }

    public async Task<SocietyNoticeDto> SaveNoticeAsync(SocietyNoticeDto dto, Guid userId)
    {
        var notice = dto.Id != Guid.Empty
            ? await Db.SocietyNotices.ForCompany(Tenant).FirstOrDefaultAsync(n => n.Id == dto.Id)
            : null;

        var isNew = notice is null;

        if (notice is null)
        {
            notice = new SocietyNotice
            {
                SocietyId = dto.SocietyId,
                PublishedByUserId = userId,
                PublishedAt = DateTime.UtcNow,
            }.StampNew(Tenant, userId);

            Db.SocietyNotices.Add(notice);
        }
        else notice.StampUpdated(userId);

        notice.Title = dto.Title;
        notice.Body = dto.Body;
        notice.NoticeType = dto.NoticeType;
        notice.Severity = dto.Severity;
        notice.ExpiresOn = dto.ExpiresOn;
        notice.IsPinned = dto.IsPinned;
        notice.SendAsBroadcast = dto.SendAsBroadcast;
        notice.AttachmentUrl = dto.AttachmentUrl;
        notice.TargetBlocks = dto.TargetBlocks;

        await Db.SaveChangesAsync();

        // A broadcast goes out once, on publication. Editing a notice afterwards must not
        // re-notify a thousand residents at midnight.
        if (isNew && dto.SendAsBroadcast)
        {
            var residents = await Db.Residents.ForCompany(Tenant)
                .Where(r => r.SocietyId == dto.SocietyId && r.MovedOutOn == null)
                .Select(r => r.PartyId)
                .ToListAsync();

            foreach (var party in residents)
            {
                await QueueNotificationAsync(
                    "SocietyNotice",
                    notice.Title,
                    notice.Body.Length > 200 ? notice.Body[..200] + "…" : notice.Body,
                    $"/realestate/notices/{notice.Id}",
                    recipientPartyId: party,
                    entityType: "SocietyNotice",
                    entityId: notice.Id,
                    severity: notice.Severity);
            }

            await Db.SaveChangesAsync();
        }

        return (await MapNoticesAsync([notice]))[0];
    }

    public async Task<List<SocietyPollDto>> GetPollsAsync(Guid societyId, bool openOnly)
    {
        var now = DateTime.UtcNow;

        var polls = await Db.SocietyPolls.ForCompany(Tenant)
            .Where(p => p.SocietyId == societyId)
            .WhereIf(openOnly, p => !p.IsClosed && p.OpensAt <= now && p.ClosesAt >= now)
            .OrderByDescending(p => p.OpensAt)
            .ToListAsync();

        if (polls.Count == 0) return [];

        var ids = polls.Select(p => p.Id).ToList();

        var votes = await Db.PollVotes.ForCompany(Tenant)
            .Where(v => ids.Contains(v.SocietyPollId))
            .Select(v => new { v.SocietyPollId, v.Choice })
            .ToListAsync();

        return polls.Select(p =>
        {
            var mine = votes.Where(v => v.SocietyPollId == p.Id).ToList();

            var options = string.IsNullOrWhiteSpace(p.Options)
                ? (p.PollType == "YesNo" ? ["Yes", "No"] : new List<string>())
                : p.Options.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();

            return new SocietyPollDto
            {
                Id = p.Id,
                SocietyId = p.SocietyId,
                Question = p.Question,
                Description = p.Description,
                PollType = p.PollType,
                Options = options,
                OpensAt = p.OpensAt,
                ClosesAt = p.ClosesAt,
                OneVotePerUnit = p.OneVotePerUnit,
                DefaultersMayVote = p.DefaultersMayVote,
                IsAnonymous = p.IsAnonymous,
                EligibleCount = p.EligibleCount,
                VoteCount = mine.Count,
                TurnoutPercent = RealEstateMapper.Percent(mine.Count, p.EligibleCount),
                IsClosed = p.IsClosed || p.ClosesAt < now,
                IsBinding = p.IsBinding,

                // Results are only published once voting closes. Showing a running tally changes
                // how people vote, and on a binding resolution that matters.
                Results = p.IsClosed || p.ClosesAt < now
                    ? mine.GroupBy(v => v.Choice)
                        .Select(g => new BreakdownSliceDto
                        {
                            Label = g.Key,
                            Value = g.Count(),
                            Percent = RealEstateMapper.Percent(g.Count(), mine.Count),
                            Count = g.Count(),
                        })
                        .OrderByDescending(s => s.Count)
                        .ToList()
                    : [],
            };
        }).ToList();
    }

    public async Task<SocietyPollDto> SavePollAsync(SocietyPollDto dto, Guid userId)
    {
        var poll = dto.Id != Guid.Empty
            ? await Db.SocietyPolls.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == dto.Id)
            : null;

        if (poll is null)
        {
            poll = new SocietyPoll { SocietyId = dto.SocietyId }.StampNew(Tenant, userId);
            Db.SocietyPolls.Add(poll);
        }
        else
        {
            var cast = await Db.PollVotes.ForCompany(Tenant).AnyAsync(v => v.SocietyPollId == poll.Id);

            if (cast)
                throw new InvalidOperationException("Votes have already been cast. A poll cannot be edited once voting has begun.");

            poll.StampUpdated(userId);
        }

        if (dto.ClosesAt <= dto.OpensAt)
            throw new InvalidOperationException("The poll has to close after it opens.");

        poll.Question = dto.Question;
        poll.Description = dto.Description;
        poll.PollType = dto.PollType;
        poll.Options = dto.Options.Count == 0 ? null : string.Join('\n', dto.Options);
        poll.OpensAt = dto.OpensAt;
        poll.ClosesAt = dto.ClosesAt;
        poll.OneVotePerUnit = dto.OneVotePerUnit;
        poll.DefaultersMayVote = dto.DefaultersMayVote;
        poll.IsAnonymous = dto.IsAnonymous;
        poll.IsBinding = dto.IsBinding;
        poll.IsClosed = dto.IsClosed;

        // The eligible roll is frozen when the poll is created, so turnout is measured against a
        // fixed denominator rather than one that moves as people move in and out.
        poll.EligibleCount = await Db.Residents.ForCompany(Tenant)
            .Where(r => r.SocietyId == dto.SocietyId && r.MovedOutOn == null)
            .WhereIf(dto.OneVotePerUnit, r => r.IsPrimaryContact)
            .WhereIf(!dto.DefaultersMayVote, r => !r.IsDefaulter)
            .CountAsync();

        await Db.SaveChangesAsync();
        return (await GetPollsAsync(dto.SocietyId, false)).First(p => p.Id == poll.Id);
    }

    public async Task<SocietyPollDto> VoteAsync(Guid pollId, Guid? unitId, string choice, string? comment, Guid userId)
    {
        var poll = await RequireAsync<SocietyPoll>(pollId, "That poll does not exist.");
        var now = DateTime.UtcNow;

        if (poll.IsClosed || poll.ClosesAt < now)
            throw new InvalidOperationException($"Voting closed on {poll.ClosesAt:dd MMM yyyy 'at' HH:mm}.");

        if (poll.OpensAt > now)
            throw new InvalidOperationException($"Voting opens on {poll.OpensAt:dd MMM yyyy 'at' HH:mm}.");

        if (poll.OneVotePerUnit && unitId is null)
            throw new InvalidOperationException("This poll is one vote per unit, so the vote has to name a unit.");

        // One vote per unit means one. This is the check that makes a binding resolution stand up.
        if (poll.OneVotePerUnit)
        {
            var already = await Db.PollVotes.ForCompany(Tenant)
                .AnyAsync(v => v.SocietyPollId == pollId && v.UnitId == unitId);

            if (already)
                throw new InvalidOperationException("This unit has already voted.");
        }

        var resident = unitId is null
            ? null
            : await Db.Residents.ForCompany(Tenant)
                .FirstOrDefaultAsync(r => r.UnitId == unitId && r.MovedOutOn == null && r.IsPrimaryContact);

        if (!poll.DefaultersMayVote && resident?.IsDefaulter == true)
            throw new InvalidOperationException("Members in arrears are not eligible to vote on this resolution.");

        Db.PollVotes.Add(new PollVote
        {
            SocietyPollId = pollId,
            UnitId = unitId,

            // An anonymous poll keeps no link to the voter. Storing it "just in case" and calling
            // the poll anonymous would be a lie to the members.
            PartyId = poll.IsAnonymous ? null : resident?.PartyId,
            Choice = choice,
            VotedAt = now,
            Comment = comment,
        }.StampNew(Tenant, userId));

        poll.VoteCount++;
        poll.StampUpdated(userId);

        await Db.SaveChangesAsync();
        return (await GetPollsAsync(poll.SocietyId, false)).First(p => p.Id == pollId);
    }
}
