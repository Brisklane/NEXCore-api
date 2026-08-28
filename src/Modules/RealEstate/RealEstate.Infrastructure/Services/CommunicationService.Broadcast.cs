using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Broadcasts, notification rules and the alert sweep.
///
/// A broadcast is the one feature in this application that can damage a company's reputation in a
/// single click, so it is built to make the careless case hard: the default is a dry run, the
/// suppression list is shown before sending, and the run records who pressed the button.
/// </summary>
public partial class CommunicationService
{
    public async Task<BroadcastRunDto> RunBroadcastAsync(BroadcastRequestDto dto, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("Give the broadcast a name so it can be found afterwards.");

        var settings = await SettingsAsync();
        var body = dto.Body;

        MessageTemplate? template = null;

        if (dto.MessageTemplateId is not null)
        {
            template = await Db.MessageTemplates.ForCompany(Tenant)
                .FirstOrDefaultAsync(t => t.Id == dto.MessageTemplateId)
                ?? throw new InvalidOperationException("That message template does not exist.");

            if (dto.Channel == NotificationChannel.WhatsApp && !template.IsProviderApproved)
                throw new InvalidOperationException(
                    $"“{template.Name}” has not been approved by the messaging provider, so it cannot be broadcast.");

            body = template.Body;
        }

        if (string.IsNullOrWhiteSpace(body))
            throw new InvalidOperationException("There is nothing to send.");

        var audience = await ResolveSegmentAsync(dto);

        if (audience.Count == 0)
            throw new InvalidOperationException(
                "That segment matched nobody. Check the filters before sending.");

        var run = new BroadcastRun
        {
            Reference = await numbering.NextMasterCodeAsync(Db.BroadcastRuns, "BRD"),
            Name = dto.Name,
            Channel = dto.Channel,
            MessageTemplateId = dto.MessageTemplateId,
            Subject = dto.Subject ?? template?.Subject,
            Body = body,
            AttachmentUrl = dto.AttachmentUrl,
            SegmentKey = dto.SegmentKey,
            SegmentFilterJson = dto.SegmentFilterJson,
            ProjectId = dto.ProjectId,
            SocietyId = dto.SocietyId,
            ScheduledFor = dto.ScheduledFor,
            TargetCount = audience.Count,
            RunByUserId = userId,
            Status = dto.DryRun ? "DryRun" : dto.ScheduledFor is null ? "Sending" : "Scheduled",
        };

        var recipients = new List<BroadcastRecipient>();

        foreach (var member in audience)
        {
            var suppression = await SuppressionReasonAsync(member.PartyId, dto.Channel, settings);

            recipients.Add(new BroadcastRecipient
            {
                BroadcastRunId = run.Id,
                PartyId = member.PartyId,
                Address = member.Address,
                WasSuppressed = suppression is not null,
                SuppressionReason = suppression,
                SentAt = suppression is null && !dto.DryRun && dto.ScheduledFor is null ? DateTime.UtcNow : null,
            });
        }

        run.SuppressedCount = recipients.Count(r => r.WasSuppressed);
        run.SentCount = recipients.Count(r => r.SentAt is not null);

        // A dry run tells the truth about what would happen and writes nothing. That is the whole
        // point — somebody about to message four thousand people can see the eleven who opted out
        // and the nine hundred it is currently the middle of the night for.
        if (dto.DryRun)
        {
            var preview = MapBroadcast(run, template?.Name, null);
            preview.Status = "DryRun";
            return preview;
        }

        if (dto.ScheduledFor is null && recipients.Count(r => !r.WasSuppressed) == 0)
            throw new InvalidOperationException(
                "Every recipient in that segment is currently suppressed, so nothing would be sent. "
                + string.Join(" ", recipients.Select(r => r.SuppressionReason).Distinct().Take(3)));

        var approval = await RaiseApprovalAsync(
            nameof(BroadcastRun), run.Id, run.Reference, recipients.Count,
            $"Broadcast “{run.Name}” to {recipients.Count(r => !r.WasSuppressed)} recipients over {dto.Channel}",
            userId, projectId: dto.ProjectId);

        run.ApprovalRequestId = approval?.Id;

        if (approval is not null)
        {
            run.Status = "PendingApproval";

            foreach (var recipient in recipients) recipient.SentAt = null;

            run.SentCount = 0;
        }
        else if (dto.ScheduledFor is null)
        {
            run.StartedAt = DateTime.UtcNow;
            run.CompletedAt = DateTime.UtcNow;
            run.Status = "Sent";
        }

        run.StampNew(Tenant, userId);
        Db.BroadcastRuns.Add(run);

        foreach (var recipient in recipients)
        {
            recipient.StampNew(Tenant, userId);
            Db.BroadcastRecipients.Add(recipient);
        }

        await Db.SaveChangesAsync();

        return MapBroadcast(run, template?.Name, (await AgentUserNamesAsync([userId])).GetValueOrDefault(userId));
    }

    private sealed record AudienceMember(Guid PartyId, string? Address);

    /// <summary>
    /// Turns a segment key into the people it means. Kept explicit rather than expression-driven,
    /// because a segment somebody cannot read is a segment nobody can check before sending.
    /// </summary>
    private async Task<List<AudienceMember>> ResolveSegmentAsync(BroadcastRequestDto dto)
    {
        var partyIds = new List<Guid>();

        switch (dto.SegmentKey)
        {
            case "Custom":
                partyIds = dto.ExplicitPartyIds;
                break;

            case "AllCustomers":
                partyIds = await Db.Bookings.ForCompany(Tenant)
                    .WhereIf(dto.ProjectId.HasValue, b => b.ProjectId == dto.ProjectId)
                    .Where(b => b.Status != BookingStatus.Cancelled)
                    .Select(b => b.PrimaryApplicantPartyId)
                    .Distinct()
                    .ToListAsync();
                break;

            case "Defaulters":
                partyIds = await Db.Bookings.ForCompany(Tenant)
                    .WhereIf(dto.ProjectId.HasValue, b => b.ProjectId == dto.ProjectId)
                    .Where(b => b.OverdueAmount > 0m && b.Status != BookingStatus.Cancelled)
                    .Select(b => b.PrimaryApplicantPartyId)
                    .Distinct()
                    .ToListAsync();
                break;

            case "DueThisMonth":
                var monthEnd = new DateOnly(Today.Year, Today.Month, 1).AddMonths(1).AddDays(-1);

                partyIds = await Db.Bookings.ForCompany(Tenant)
                    .WhereIf(dto.ProjectId.HasValue, b => b.ProjectId == dto.ProjectId)
                    .Where(b => b.NextDueDate != null && b.NextDueDate <= monthEnd
                             && b.Status != BookingStatus.Cancelled)
                    .Select(b => b.PrimaryApplicantPartyId)
                    .Distinct()
                    .ToListAsync();
                break;

            case "OpenEnquiries":
                partyIds = await Db.Enquiries.ForCompany(Tenant)
                    .WhereIf(dto.ProjectId.HasValue, e => e.ProjectId == dto.ProjectId)
                    .Where(e => e.ClosedAt == null && e.PartyId != null)
                    .Select(e => e.PartyId!.Value)
                    .Distinct()
                    .ToListAsync();
                break;

            case "ActiveTenants":
                var liveTenancies = new[] { TenancyStatus.Active, TenancyStatus.NoticeGiven, TenancyStatus.Expiring };

                var tenancyIds = await Db.Tenancies.ForCompany(Tenant)
                    .Where(t => liveTenancies.Contains(t.Status))
                    .Select(t => t.Id)
                    .ToListAsync();

                partyIds = await Db.TenancyParties.ForCompany(Tenant)
                    .Where(p => tenancyIds.Contains(p.TenancyId))
                    .Select(p => p.PartyId)
                    .Distinct()
                    .ToListAsync();
                break;

            case "Landlords":
                partyIds = await Db.Landlords.ForCompany(Tenant)
                    .Select(l => l.PartyId)
                    .Distinct()
                    .ToListAsync();
                break;

            case "Residents":
                partyIds = await Db.Residents.ForCompany(Tenant)
                    .WhereIf(dto.SocietyId.HasValue, r => r.SocietyId == dto.SocietyId)
                    .Where(r => r.PartyId != Guid.Empty)
                    .Select(r => r.PartyId)
                    .Distinct()
                    .ToListAsync();
                break;

            case "ChannelPartners":
                partyIds = await Db.ChannelPartners.ForCompany(Tenant)
                    .Where(p => p.PartyId != null && p.Status == PartnerStatus.Active)
                    .Select(p => p.PartyId!.Value)
                    .Distinct()
                    .ToListAsync();
                break;

            case "PossessionPending":
                partyIds = await Db.Bookings.ForCompany(Tenant)
                    .WhereIf(dto.ProjectId.HasValue, b => b.ProjectId == dto.ProjectId)
                    .Where(b => b.Status == BookingStatus.PossessionOffered)
                    .Select(b => b.PrimaryApplicantPartyId)
                    .Distinct()
                    .ToListAsync();
                break;

            default:
                throw new InvalidOperationException($"“{dto.SegmentKey}” is not a segment this application knows.");
        }

        if (partyIds.Count == 0) return [];

        var contacts = await Db.Parties.ForCompany(Tenant)
            .Where(p => partyIds.Contains(p.Id))
            .Select(p => new { p.Id, p.PrimaryPhone, p.PrimaryEmail })
            .ToListAsync();

        return contacts.Select(c => new AudienceMember(
            c.Id,
            dto.Channel == NotificationChannel.Email ? c.PrimaryEmail : c.PrimaryPhone)).ToList();
    }

    private static BroadcastRunDto MapBroadcast(BroadcastRun run, string? templateName, string? runByName) => new()
    {
        Id = run.Id,
        Reference = run.Reference,
        Name = run.Name,
        Channel = run.Channel,
        MessageTemplateName = templateName,
        Subject = run.Subject,
        Body = run.Body,
        SegmentKey = run.SegmentKey,
        ScheduledFor = run.ScheduledFor,
        StartedAt = run.StartedAt,
        CompletedAt = run.CompletedAt,
        TargetCount = run.TargetCount,
        SentCount = run.SentCount,
        SuppressedCount = run.SuppressedCount,
        DeliveredCount = run.DeliveredCount,
        ReadCount = run.ReadCount,
        FailedCount = run.FailedCount,
        OptOutCount = run.OptOutCount,
        Cost = run.Cost,
        Status = run.Status,
        RunByName = runByName,
        DeliveryRatePercent = RealEstateMapper.Percent(run.DeliveredCount, run.SentCount),
        ReadRatePercent = RealEstateMapper.Percent(run.ReadCount, run.DeliveredCount),
    };

    public async Task<PaginatedResponse<BroadcastRunDto>> GetBroadcastsAsync(ListQueryDto query)
    {
        var q = Db.BroadcastRuns.ForCompany(Tenant)
            .WhereIf(query.ProjectId.HasValue, b => b.ProjectId == query.ProjectId)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), b => b.Name.Contains(query.Search!))
            .OrderByDescending(b => b.CreatedAt);

        return await PageAsync(q, query, async rows =>
        {
            var templateIds = rows.Where(r => r.MessageTemplateId != null)
                .Select(r => r.MessageTemplateId!.Value).Distinct().ToList();

            var templates = templateIds.Count == 0
                ? []
                : await Db.MessageTemplates.ForCompany(Tenant)
                    .Where(t => templateIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.Name);

            var users = await AgentUserNamesAsync(rows.Select(r => r.RunByUserId));
            var projects = await ProjectNamesAsync(rows.Select(r => r.ProjectId));

            var societyIds = rows.Where(r => r.SocietyId != null).Select(r => r.SocietyId!.Value).Distinct().ToList();

            var societies = societyIds.Count == 0
                ? []
                : await Db.Societies.ForCompany(Tenant)
                    .Where(s => societyIds.Contains(s.Id)).ToDictionaryAsync(s => s.Id, s => s.Name);

            return rows.Select(r =>
            {
                var dto = MapBroadcast(r,
                    r.MessageTemplateId is null ? null : templates.GetValueOrDefault(r.MessageTemplateId.Value),
                    r.RunByUserId is null ? null : users.GetValueOrDefault(r.RunByUserId.Value));

                dto.ProjectName = r.ProjectId is null ? null : projects.GetValueOrDefault(r.ProjectId.Value);
                dto.SocietyName = r.SocietyId is null ? null : societies.GetValueOrDefault(r.SocietyId.Value);

                return dto;
            }).ToList();
        });
    }

    // ═══ Notification rules ══════════════════════════════════════════════════

    public async Task<List<NotificationRuleDto>> GetNotificationRulesAsync()
    {
        var rules = await Db.NotificationRules.ForCompany(Tenant)
            .OrderBy(r => r.RuleKey)
            .ToListAsync();

        var since = DateTime.UtcNow.AddDays(-30);

        var fired = await Db.NotificationLogs.ForCompany(Tenant)
            .Where(n => n.QueuedAt >= since)
            .GroupBy(n => n.RuleKey)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var users = await AgentUserNamesAsync(rules.Select(r => r.TargetUserId));
        var projects = await ProjectNamesAsync(rules.Select(r => r.ProjectId));

        return rules.Select(r => new NotificationRuleDto
        {
            Id = r.Id,
            RuleKey = r.RuleKey,
            Name = r.Name,
            IsEnabled = r.IsEnabled,
            Severity = r.Severity,
            Channels = SplitChannels(r.Channels),
            TargetRoles = (r.TargetRoles ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList(),
            TargetUserId = r.TargetUserId,
            TargetUserName = r.TargetUserId is null ? null : users.GetValueOrDefault(r.TargetUserId.Value),
            LeadDays = r.LeadDays,
            ThresholdAmount = r.ThresholdAmount,
            IsDigest = r.IsDigest,
            DigestSchedule = r.DigestSchedule,
            RespectQuietHours = r.RespectQuietHours,
            EscalateAfterHours = r.EscalateAfterHours,
            EscalateToRole = r.EscalateToRole,
            MessageTemplateId = r.MessageTemplateId,
            ProjectName = r.ProjectId is null ? null : projects.GetValueOrDefault(r.ProjectId.Value),
            FiredLast30Days = fired.GetValueOrDefault(r.RuleKey),
        }).ToList();
    }

    private static List<NotificationChannel> SplitChannels(string channels)
        => channels.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(c => Enum.TryParse<NotificationChannel>(c, out var parsed) ? parsed : NotificationChannel.InApp)
            .Distinct()
            .ToList();

    public async Task<NotificationRuleDto> SaveNotificationRuleAsync(NotificationRuleDto dto, Guid userId)
    {
        var isNew = dto.Id == Guid.Empty;

        var rule = isNew
            ? new NotificationRule()
            : await Db.NotificationRules.ForCompany(Tenant).FirstOrDefaultAsync(r => r.Id == dto.Id)
              ?? throw new InvalidOperationException("That rule does not exist.");

        if (string.IsNullOrWhiteSpace(dto.RuleKey))
            throw new InvalidOperationException("A rule needs its key — it is what the code fires on.");

        if (isNew)
        {
            var duplicate = await Db.NotificationRules.ForCompany(Tenant)
                .AnyAsync(r => r.RuleKey == dto.RuleKey);

            if (duplicate)
                throw new InvalidOperationException($"There is already a rule for “{dto.RuleKey}”.");
        }

        if (dto.Channels.Count == 0)
            throw new InvalidOperationException("A rule with no channel cannot reach anybody. Choose at least one.");

        // A critical alert that respects quiet hours is not a critical alert. Rather than silently
        // ignoring the setting at fire time, the contradiction is refused here.
        if (dto.Severity == AlertSeverity.Critical && dto.RespectQuietHours && dto.IsDigest)
            throw new InvalidOperationException(
                "A critical rule cannot be both quiet-hours-respecting and a digest — that can delay it by a day. "
                + "Lower the severity, or send it immediately.");

        rule.RuleKey = dto.RuleKey;
        rule.Name = dto.Name;
        rule.IsEnabled = dto.IsEnabled;
        rule.Severity = dto.Severity;
        rule.Channels = string.Join(",", dto.Channels.Distinct());
        rule.TargetRoles = dto.TargetRoles.Count == 0 ? null : string.Join(",", dto.TargetRoles);
        rule.TargetUserId = dto.TargetUserId;
        rule.LeadDays = dto.LeadDays;
        rule.ThresholdAmount = dto.ThresholdAmount;
        rule.IsDigest = dto.IsDigest;
        rule.DigestSchedule = dto.DigestSchedule;
        rule.RespectQuietHours = dto.RespectQuietHours;
        rule.EscalateAfterHours = dto.EscalateAfterHours;
        rule.EscalateToRole = dto.EscalateToRole;
        rule.MessageTemplateId = dto.MessageTemplateId;

        if (isNew)
        {
            rule.StampNew(Tenant, userId);
            Db.NotificationRules.Add(rule);
        }
        else
        {
            rule.StampUpdated(userId);
        }

        await Db.SaveChangesAsync();

        return (await GetNotificationRulesAsync()).First(r => r.Id == rule.Id);
    }

    public async Task<PaginatedResponse<NotificationDto>> GetNotificationsAsync(
        ListQueryDto query, Guid userId, bool unreadOnly)
    {
        var q = Db.NotificationLogs.ForCompany(Tenant)
            .Where(n => n.RecipientUserId == userId && n.Channel == NotificationChannel.InApp)
            .WhereIf(unreadOnly, n => n.ReadAt == null)
            .OrderByDescending(n => n.QueuedAt);

        return await PageAsync(q, query, (NotificationLog n) => new NotificationDto
        {
            Id = n.Id,
            RuleKey = n.RuleKey,
            Channel = n.Channel,
            Severity = n.Severity,
            Title = n.Title,
            Body = n.Body,
            DeepLink = n.DeepLink,
            EntityType = n.EntityType,
            EntityId = n.EntityId,
            QueuedAt = n.QueuedAt,
            SentAt = n.SentAt,
            ReadAt = n.ReadAt,
            IsRead = n.ReadAt is not null,
            Failed = n.Failed,
            IsEscalation = n.IsEscalation,
        });
    }

    public async Task MarkNotificationsReadAsync(List<Guid> ids, Guid userId)
    {
        if (ids.Count == 0) return;

        var notifications = await Db.NotificationLogs.ForCompany(Tenant)
            .Where(n => ids.Contains(n.Id) && n.RecipientUserId == userId && n.ReadAt == null)
            .ToListAsync();

        foreach (var notification in notifications)
        {
            notification.ReadAt = DateTime.UtcNow;
            notification.StampUpdated(userId);
        }

        await Db.SaveChangesAsync();
    }

    /// <summary>
    /// The nightly sweep that turns dated obligations into notifications.
    ///
    /// It is deliberately idempotent — each source carries its own "alert sent" flag, so running
    /// the sweep twice in a day does not message anybody twice. That flag is cleared whenever the
    /// underlying date changes, which is what makes a renewed licence alert again next year.
    /// </summary>
    public async Task<int> RunAlertSweepAsync()
    {
        var settings = await SettingsAsync();
        var today = Today;
        var raised = 0;

        // ── Approvals about to lapse ─────────────────────────────────────────

        var approvals = await Db.ApprovalRecords.ForCompany(Tenant)
            .Where(a => !a.RenewalAlertSent && a.ValidUntil != null
                     && (a.State == ApprovalState.Granted || a.State == ApprovalState.GrantedWithConditions))
            .ToListAsync();

        foreach (var approval in approvals.Where(a => a.ValidUntil!.Value.DayNumber - today.DayNumber <= a.AlertDaysBefore))
        {
            await QueueNotificationAsync(
                "approval.expiring",
                $"{approval.Kind} expires on {approval.ValidUntil:dd MMM yyyy}",
                $"{approval.ApprovalNumber ?? approval.Reference} from {approval.Authority ?? "the authority"}.",
                $"/realestate/compliance/approvals/{approval.Id}",
                recipientUserId: approval.OwnerUserId,
                entityType: nameof(ApprovalRecord),
                entityId: approval.Id,
                severity: approval.IsMandatory ? AlertSeverity.Critical : AlertSeverity.Warning);

            approval.RenewalAlertSent = true;
            raised++;
        }

        // ── Licences ─────────────────────────────────────────────────────────

        var licences = await Db.LicenceRecords.ForCompany(Tenant)
            .Where(l => l.IsCurrent && !l.RenewalAlertSent)
            .ToListAsync();

        foreach (var licence in licences.Where(l => l.ExpiresOn.DayNumber - today.DayNumber <= l.AlertDaysBefore))
        {
            await QueueNotificationAsync(
                "licence.expiring",
                $"{licence.LicenceType} licence expires on {licence.ExpiresOn:dd MMM yyyy}",
                licence.IsMandatoryToTrade
                    ? "This licence is required to trade. Renew it before it lapses."
                    : $"Renewal fee {licence.RenewalFee:N0}.",
                $"/realestate/compliance/licences/{licence.Id}",
                entityType: nameof(LicenceRecord),
                entityId: licence.Id,
                severity: licence.IsMandatoryToTrade ? AlertSeverity.Critical : AlertSeverity.Warning);

            licence.RenewalAlertSent = true;
            raised++;
        }

        // ── Bank guarantees ──────────────────────────────────────────────────

        var guarantees = await Db.BankGuarantees.ForCompany(Tenant)
            .Where(g => g.Status == "Active" && !g.ExpiryAlertSent)
            .ToListAsync();

        foreach (var guarantee in guarantees.Where(g => g.ExpiresOn.DayNumber - today.DayNumber <= g.AlertDaysBefore))
        {
            await QueueNotificationAsync(
                "guarantee.expiring",
                $"Guarantee {guarantee.GuaranteeNumber} expires on {guarantee.ExpiresOn:dd MMM yyyy}",
                $"{guarantee.Amount:N0} of cover from {guarantee.IssuingBank ?? "the bank"}"
                + (guarantee.IsAutoRenewing ? ", renewing automatically." : ", not set to renew."),
                "/realestate/finance/guarantees",
                entityType: nameof(BankGuarantee),
                entityId: guarantee.Id,
                severity: guarantee.IsAutoRenewing ? AlertSeverity.Info : AlertSeverity.Critical);

            guarantee.ExpiryAlertSent = true;
            raised++;
        }

        // ── Compliance calendar ──────────────────────────────────────────────

        var calendar = await Db.ComplianceCalendarEntries.ForCompany(Tenant)
            .Where(e => !e.IsCompleted && !e.AlertSent)
            .ToListAsync();

        foreach (var entry in calendar.Where(e => e.DueDate.DayNumber - today.DayNumber <= e.AlertDaysBefore))
        {
            await QueueNotificationAsync(
                "compliance.due",
                entry.Title,
                $"Due {entry.DueDate:dd MMM yyyy}."
                + (entry.PenaltyIfMissed is > 0m ? $" Penalty if missed: {entry.PenaltyIfMissed:N0}." : null),
                "/realestate/compliance/calendar",
                recipientUserId: entry.OwnerUserId,
                entityType: nameof(ComplianceCalendarEntry),
                entityId: entry.Id,
                severity: entry.Severity);

            entry.AlertSent = true;
            entry.IsOverdue = entry.DueDate < today;
            raised++;
        }

        // ── Safety certificates on let property ──────────────────────────────

        var certificates = await Db.ComplianceCertificates.ForCompany(Tenant)
            .Where(c => c.IsCurrent && !c.RenewalBooked && c.ExpiresOn <= today.AddDays(45))
            .ToListAsync();

        foreach (var certificate in certificates)
        {
            await QueueNotificationAsync(
                "certificate.expiring",
                $"{certificate.Kind} certificate expires on {certificate.ExpiresOn:dd MMM yyyy}",
                "Book the renewal inspection. A let property without a current certificate is an offence.",
                $"/realestate/leasing/compliance/{certificate.Id}",
                entityType: nameof(ComplianceCertificate),
                entityId: certificate.Id,
                severity: AlertSeverity.Critical);

            raised++;
        }

        // ── Holds about to expire ────────────────────────────────────────────

        var holds = await Db.UnitHolds.ForCompany(Tenant)
            .Where(h => h.Status == HoldStatus.Active && h.ExpiresAt <= DateTime.UtcNow.AddHours(6))
            .ToListAsync();

        foreach (var hold in holds)
        {
            await QueueNotificationAsync(
                "hold.expiring",
                "A unit hold expires shortly",
                $"It lapses at {hold.ExpiresAt:HH:mm} and the unit returns to inventory.",
                "/realestate/inventory/holds",
                recipientUserId: hold.HeldByUserId,
                entityType: nameof(UnitHold),
                entityId: hold.Id,
                severity: AlertSeverity.Warning);

            raised++;
        }

        // ── Escalations on approvals nobody has actioned ─────────────────────

        var pending = await Db.ApprovalRequests.ForCompany(Tenant)
            .Where(a => a.Outcome == ApprovalOutcome.Pending
                     && a.EscalatesAt != null && a.EscalatesAt < DateTime.UtcNow)
            .ToListAsync();

        foreach (var request in pending)
        {
            await QueueNotificationAsync(
                "approval.escalated",
                $"An approval has been waiting since {request.RequestedAt:dd MMM HH:mm}",
                request.Summary,
                $"/realestate/admin/approvals/{request.Id}",
                entityType: nameof(ApprovalRequest),
                entityId: request.Id,
                severity: AlertSeverity.Warning);

            // Escalating once is a reminder; escalating every night is noise. The timer is pushed
            // forward so the same request does not fire again until it has waited as long again.
            request.EscalatesAt = DateTime.UtcNow.AddHours(24);
            raised++;
        }

        // ── Tenancies coming to an end ───────────────────────────────────────

        if (settings.EstateManagementEnabled)
        {
            var horizon = today.AddDays(90);
            var live = new[] { TenancyStatus.Active, TenancyStatus.Expiring };

            var tenancies = await Db.Tenancies.ForCompany(Tenant)
                .Where(t => live.Contains(t.Status) && t.EndDate != null
                         && t.EndDate >= today && t.EndDate <= horizon)
                .ToListAsync();

            var alreadyRenewing = await Db.TenancyRenewals.ForCompany(Tenant)
                .Where(r => tenancies.Select(t => t.Id).Contains(r.TenancyId))
                .Select(r => r.TenancyId)
                .ToListAsync();

            foreach (var tenancy in tenancies.Where(t => !alreadyRenewing.Contains(t.Id)))
            {
                await QueueNotificationAsync(
                    "tenancy.expiring",
                    $"Tenancy {tenancy.Reference} ends on {tenancy.EndDate:dd MMM yyyy}",
                    "Start the renewal conversation before the notice period runs out.",
                    $"/realestate/leasing/tenancies/{tenancy.Id}",
                    recipientUserId: tenancy.ManagedByUserId,
                    entityType: nameof(Tenancy),
                    entityId: tenancy.Id,
                    severity: AlertSeverity.Warning);

                if (tenancy.Status == TenancyStatus.Active && tenancy.EndDate <= today.AddDays(60))
                {
                    tenancy.Status = TenancyStatus.Expiring;
                }

                raised++;
            }
        }

        await Db.SaveChangesAsync();

        return raised;
    }
}
