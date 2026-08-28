using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// The dunning ladder, legal notices and write-offs — the last third of <see cref="MoneyService"/>.
///
/// Dunning is a *designed sequence*, not a retry count: reminder, SMS, call task, first notice,
/// final notice, legal referral, cancellation review. Each step has a channel, a delay and an
/// action, and the whole ladder pauses the moment a customer promises to pay.
/// </summary>
public partial class MoneyService
{
    public async Task<List<DunningPolicyDto>> GetDunningPoliciesAsync(string? appliesTo)
    {
        var policies = await Db.DunningPolicies.ForCompany(Tenant)
            .WhereIf(!string.IsNullOrWhiteSpace(appliesTo), p => p.AppliesTo == appliesTo)
            .Include(p => p.Steps)
            .OrderBy(p => p.AppliesTo).ThenBy(p => p.Name)
            .ToListAsync();

        var projects = await ProjectNamesAsync(policies.Select(p => p.ProjectId));
        var ids = policies.Select(p => p.Id).ToList();

        var caseCounts = await Db.DunningCases.ForCompany(Tenant)
            .Where(c => ids.Contains(c.DunningPolicyId) && !c.IsClosed)
            .GroupBy(c => c.DunningPolicyId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count);

        var templates = await Db.MessageTemplates.ForCompany(Tenant).ToDictionaryAsync(t => t.Id, t => t.Name);

        return policies.Select(p => new DunningPolicyDto
        {
            Id = p.Id,
            Name = p.Name,
            ProjectId = p.ProjectId,
            ProjectName = p.ProjectId is null ? null : projects.GetValueOrDefault(p.ProjectId.Value),
            AppliesTo = p.AppliesTo,
            IsActive = p.IsActive,
            IsDefault = p.IsDefault,
            MinimumOverdueAmount = p.MinimumOverdueAmount,
            StepCount = p.Steps.Count,
            ActiveCaseCount = caseCounts.GetValueOrDefault(p.Id),
            Steps = p.Steps.OrderBy(s => s.StepNumber).Select(s => new DunningStepDto
            {
                Id = s.Id,
                StepNumber = s.StepNumber,
                Name = s.Name,
                DaysAfterDue = s.DaysAfterDue,
                Action = s.Action,
                Channel = s.Channel,
                MessageTemplateId = s.MessageTemplateId,
                MessageTemplateName = s.MessageTemplateId is null ? null : templates.GetValueOrDefault(s.MessageTemplateId.Value),
                DocumentTemplateId = s.DocumentTemplateId,
                AssignToRole = s.AssignToRole,
                FeeAmount = s.FeeAmount,
                RequiresApproval = s.RequiresApproval,
                StopsOnPromise = s.StopsOnPromise,
            }).ToList(),
        }).ToList();
    }

    public async Task<DunningPolicyDto> SaveDunningPolicyAsync(DunningPolicyDto dto, Guid userId)
    {
        var policy = dto.Id != Guid.Empty
            ? await Db.DunningPolicies.ForCompany(Tenant).Include(p => p.Steps).FirstOrDefaultAsync(p => p.Id == dto.Id)
            : null;

        if (policy is null)
        {
            policy = new DunningPolicy().StampNew(Tenant, userId);
            Db.DunningPolicies.Add(policy);
        }
        else
        {
            Db.DunningSteps.RemoveRange(policy.Steps);
            policy.StampUpdated(userId);
        }

        policy.Name = dto.Name;
        policy.ProjectId = dto.ProjectId;
        policy.AppliesTo = dto.AppliesTo;
        policy.IsActive = dto.IsActive;
        policy.IsDefault = dto.IsDefault;
        policy.MinimumOverdueAmount = dto.MinimumOverdueAmount;

        foreach (var step in dto.Steps.OrderBy(s => s.StepNumber))
        {
            policy.Steps.Add(new DunningStep
            {
                StepNumber = step.StepNumber,
                Name = step.Name,
                DaysAfterDue = step.DaysAfterDue,
                Action = step.Action,
                Channel = step.Channel,
                MessageTemplateId = step.MessageTemplateId,
                DocumentTemplateId = step.DocumentTemplateId,
                AssignToRole = step.AssignToRole,
                FeeAmount = step.FeeAmount,
                RequiresApproval = step.RequiresApproval,
                StopsOnPromise = step.StopsOnPromise,
            }.StampNew(Tenant, userId));
        }

        if (dto.IsDefault)
        {
            var others = await Db.DunningPolicies.ForCompany(Tenant)
                .Where(p => p.AppliesTo == dto.AppliesTo && p.Id != policy.Id && p.IsDefault)
                .ToListAsync();
            foreach (var o in others) o.IsDefault = false;
        }

        await Db.SaveChangesAsync();
        return (await GetDunningPoliciesAsync(dto.AppliesTo)).First(p => p.Id == policy.Id);
    }

    public async Task<PaginatedResponse<DunningCaseDto>> GetDunningCasesAsync(ListQueryDto query, bool openOnly)
    {
        var q = Db.DunningCases.ForCompany(Tenant)
            .WhereIf(openOnly, c => !c.IsClosed)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), c => c.Reference.Contains(query.Search!))
            .OrderByDescending(c => c.OverdueAmount);

        return await PageAsync(q, query, MapCasesAsync);
    }

    private async Task<List<DunningCaseDto>> MapCasesAsync(List<DunningCase> cases)
    {
        if (cases.Count == 0) return [];

        var names = await PartyNamesAsync(cases.Select(c => c.PartyId));
        var bookingIds = cases.Where(c => c.BookingId.HasValue).Select(c => c.BookingId!.Value).Distinct().ToList();

        var bookings = bookingIds.Count == 0
            ? []
            : await Db.Bookings.ForCompany(Tenant).Where(b => bookingIds.Contains(b.Id))
                .ToDictionaryAsync(b => b.Id, b => b.Reference);

        var policyIds = cases.Select(c => c.DunningPolicyId).Distinct().ToList();
        var policies = await Db.DunningPolicies.ForCompany(Tenant)
            .Where(p => policyIds.Contains(p.Id))
            .Include(p => p.Steps)
            .ToDictionaryAsync(p => p.Id, p => p);

        return cases.Select(c =>
        {
            var policy = policies.GetValueOrDefault(c.DunningPolicyId);
            var step = policy?.Steps.FirstOrDefault(s => s.StepNumber == c.CurrentStep);

            return new DunningCaseDto
            {
                Id = c.Id,
                Reference = c.Reference,
                PartyId = c.PartyId,
                PartyName = names.GetValueOrDefault(c.PartyId, "—"),
                BookingId = c.BookingId,
                BookingReference = c.BookingId is null ? null : bookings.GetValueOrDefault(c.BookingId.Value),
                TenancyId = c.TenancyId,
                PolicyName = policy?.Name ?? "—",
                OpenedOn = c.OpenedOn,
                CurrentStep = c.CurrentStep,
                CurrentStepName = step?.Name,
                NextStepDueAt = c.NextStepDueAt,
                OverdueAmount = c.OverdueAmount,
                SurchargeAmount = c.SurchargeAmount,
                DaysOverdue = c.DaysOverdue,
                LastContactedAt = c.LastContactedAt,
                IsSuspended = c.IsSuspended,
                SuspensionReason = c.SuspensionReason,
                IsClosed = c.IsClosed,
                Outcome = c.Outcome,
            };
        }).ToList();
    }

    public async Task<DunningCaseDto?> GetDunningCaseAsync(Guid id)
    {
        var dunningCase = await Db.DunningCases.ForCompany(Tenant)
            .Include(c => c.Events)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (dunningCase is null) return null;

        var dto = (await MapCasesAsync([dunningCase]))[0];

        dto.Events = dunningCase.Events
            .OrderByDescending(e => e.OccurredAt)
            .Select(e => new DunningEventDto
            {
                Id = e.Id,
                StepNumber = e.StepNumber,
                Action = e.Action,
                Channel = e.Channel,
                OccurredAt = e.OccurredAt,
                Succeeded = e.Succeeded,
                FailureReason = e.FailureReason,
                AmountAtEvent = e.AmountAtEvent,
                Note = e.Note,
                LegalNoticeId = e.LegalNoticeId,
            })
            .ToList();

        return dto;
    }

    public async Task<DunningCaseDto> SuspendDunningAsync(Guid caseId, DateOnly? until, string reason, Guid userId)
    {
        var dunningCase = await Db.DunningCases.ForCompany(Tenant).FirstOrDefaultAsync(c => c.Id == caseId)
            ?? throw new InvalidOperationException("That dunning case does not exist.");

        dunningCase.IsSuspended = true;
        dunningCase.SuspendedUntil = until;
        dunningCase.SuspensionReason = reason;
        dunningCase.StampUpdated(userId);

        await Db.SaveChangesAsync();
        return (await GetDunningCaseAsync(caseId))!;
    }

    /// <summary>
    /// Walks every overdue booking down its ladder by one step where a step is due.
    ///
    /// Opens a case for anything newly overdue past the policy's floor, skips anything suspended
    /// by a live promise, and fires exactly one step per run per case — a ladder that fires three
    /// steps at once is a ladder nobody will keep switched on.
    /// </summary>
    public async Task<int> RunDunningAsync(DateOnly asOf)
    {
        var policies = await Db.DunningPolicies.ForCompany(Tenant)
            .Where(p => p.IsActive)
            .Include(p => p.Steps)
            .ToListAsync();

        if (policies.Count == 0) return 0;

        var bookingPolicy = policies.FirstOrDefault(p => p.AppliesTo == "Booking" && p.IsDefault)
                            ?? policies.FirstOrDefault(p => p.AppliesTo == "Booking");

        if (bookingPolicy is null) return 0;

        var overdueBookings = await Db.Bookings.ForCompany(Tenant)
            .Where(b => b.OverdueAmount >= bookingPolicy.MinimumOverdueAmount
                     && b.OverdueAmount > 0
                     && b.Status != BookingStatus.Cancelled)
            .ToListAsync();

        var bookingIds = overdueBookings.Select(b => b.Id).ToList();

        var existing = await Db.DunningCases.ForCompany(Tenant)
            .Where(c => c.BookingId != null && bookingIds.Contains(c.BookingId!.Value) && !c.IsClosed)
            .ToListAsync();

        var actions = 0;

        foreach (var booking in overdueBookings)
        {
            var dunningCase = existing.FirstOrDefault(c => c.BookingId == booking.Id);

            if (dunningCase is null)
            {
                dunningCase = new DunningCase
                {
                    Reference = await numbering.NextMasterCodeAsync(Db.DunningCases, "DUN"),
                    PartyId = booking.PrimaryApplicantPartyId,
                    BookingId = booking.Id,
                    DunningPolicyId = bookingPolicy.Id,
                    OpenedOn = asOf,
                    CurrentStep = 0,
                    NextStepDueAt = DateTime.UtcNow,
                }.StampNew(Tenant);

                Db.DunningCases.Add(dunningCase);
                booking.DunningCaseId = dunningCase.Id;
            }

            dunningCase.OverdueAmount = booking.OverdueAmount;
            dunningCase.SurchargeAmount = booking.TotalSurcharge - booking.TotalWaived;
            dunningCase.DaysOverdue = booking.DaysOverdue;

            // A live promise, or a manual suspension that has not lapsed, pauses the ladder.
            if (dunningCase.IsSuspended)
            {
                if (dunningCase.SuspendedUntil is null || dunningCase.SuspendedUntil >= asOf) continue;

                dunningCase.IsSuspended = false;
                dunningCase.SuspensionReason = null;
                dunningCase.SuspendedUntil = null;
            }

            var nextStep = bookingPolicy.Steps
                .Where(s => s.StepNumber > dunningCase.CurrentStep && booking.DaysOverdue >= s.DaysAfterDue)
                .OrderBy(s => s.StepNumber)
                .FirstOrDefault();

            if (nextStep is null) continue;
            if (dunningCase.NextStepDueAt is not null && dunningCase.NextStepDueAt > DateTime.UtcNow) continue;

            await FireStepAsync(dunningCase, nextStep, booking);
            actions++;
        }

        await Db.SaveChangesAsync();
        return actions;
    }

    private async Task FireStepAsync(DunningCase dunningCase, DunningStep step, Booking booking)
    {
        var succeeded = true;
        Guid? noticeId = null;

        switch (step.Action)
        {
            case DunningAction.SendReminder:
                await QueueNotificationAsync(
                    "instalment_overdue",
                    step.Name,
                    $"{booking.OverdueAmount:N0} {booking.CurrencyCode} is overdue on {booking.Reference}.",
                    "/portal/customer",
                    recipientPartyId: booking.PrimaryApplicantPartyId,
                    entityType: "Booking", entityId: booking.Id,
                    severity: AlertSeverity.Warning);
                break;

            case DunningAction.CreateCallTask:
                Db.FollowUpTasks.Add(new FollowUpTask
                {
                    Title = $"Call about {booking.Reference} — {booking.OverdueAmount:N0} overdue",
                    Note = step.Name,
                    SuggestedAction = ActivityKind.Call,
                    DueAt = DateTime.UtcNow,
                    Priority = booking.DaysOverdue > 60 ? TicketPriority.High : TicketPriority.Normal,
                    State = TaskState.Open,
                    PartyId = booking.PrimaryApplicantPartyId,
                    BookingId = booking.Id,
                    DunningCaseId = dunningCase.Id,
                    AssignedToUserId = dunningCase.AssignedToUserId ?? Guid.Empty,
                    IsAutoGenerated = true,
                    SourceRuleKey = $"dunning_step_{step.StepNumber}",
                }.StampNew(Tenant));
                break;

            case DunningAction.IssueNotice:
            case DunningAction.IssueFinalNotice:
            case DunningAction.ReferToLegal:
                var notice = new LegalNotice
                {
                    NoticeNumber = await numbering.NextNoticeNumberAsync(DateTime.UtcNow),
                    PartyId = booking.PrimaryApplicantPartyId,
                    BookingId = booking.Id,
                    DunningCaseId = dunningCase.Id,
                    NoticeType = step.Action == DunningAction.IssueFinalNotice ? "FinalNotice" : "FirstNotice",
                    IssuedOn = Today,
                    ComplyByDate = Today.AddDays(15),
                    AmountDemanded = booking.OverdueAmount + dunningCase.SurchargeAmount,
                    TemplateVersionId = null,
                }.StampNew(Tenant);

                Db.LegalNotices.Add(notice);
                noticeId = notice.Id;

                if (step.Action == DunningAction.ReferToLegal)
                {
                    booking.IsUnderLitigation = false; // referred, not yet filed
                    dunningCase.Outcome = "Legal";
                }
                break;

            case DunningAction.ProposeCancellation:
                await QueueNotificationAsync(
                    "cancellation_review",
                    $"{booking.Reference} is due a cancellation review",
                    $"{booking.DaysOverdue} days overdue, {booking.OverdueAmount:N0} outstanding.",
                    $"/realestate/bookings/{booking.Id}",
                    recipientUserId: dunningCase.AssignedToUserId,
                    entityType: "Booking", entityId: booking.Id,
                    severity: AlertSeverity.Critical);
                break;

            case DunningAction.ApplySurcharge:
                // Surcharge accrues nightly on its own; this step exists so a ladder can make the
                // moment explicit on the customer's timeline rather than have it appear silently.
                break;

            case DunningAction.SuspendServices:
                dunningCase.Outcome = "ServicesSuspended";
                break;
        }

        if (step.FeeAmount is > 0m)
        {
            Db.CustomerLedgerEntries.Add(new CustomerLedgerEntry
            {
                PartyId = booking.PrimaryApplicantPartyId,
                BookingId = booking.Id,
                EntryDate = Today,
                Kind = LedgerEntryKind.Adjustment,
                Description = $"{step.Name} fee",
                DebitAmount = step.FeeAmount.Value,
                CurrencyCode = booking.CurrencyCode,
            }.StampNew(Tenant));
        }

        dunningCase.Events.Add(new DunningEvent
        {
            StepNumber = step.StepNumber,
            Action = step.Action,
            Channel = step.Channel,
            OccurredAt = DateTime.UtcNow,
            Succeeded = succeeded,
            AmountAtEvent = booking.OverdueAmount,
            LegalNoticeId = noticeId,
            Note = step.Name,
        }.StampNew(Tenant));

        dunningCase.CurrentStep = step.StepNumber;
        dunningCase.LastContactedAt = DateTime.UtcNow;

        // The next step is not due until its own day count is reached.
        var following = await Db.DunningSteps.ForCompany(Tenant)
            .Where(s => s.DunningPolicyId == dunningCase.DunningPolicyId && s.StepNumber > step.StepNumber)
            .OrderBy(s => s.StepNumber)
            .FirstOrDefaultAsync();

        dunningCase.NextStepDueAt = following is null
            ? null
            : DateTime.UtcNow.AddDays(Math.Max(1, following.DaysAfterDue - step.DaysAfterDue));
    }

    // ═══ Legal notices ═══════════════════════════════════════════════════════

    public async Task<LegalNoticeDto> IssueNoticeAsync(
        Guid? bookingId, Guid? tenancyId, string noticeType, DateOnly? complyBy, Guid? templateId, Guid userId)
    {
        if (bookingId is null && tenancyId is null)
            throw new InvalidOperationException("A notice has to be against a booking or a tenancy.");

        var partyId = Guid.Empty;
        var amount = 0m;

        if (bookingId is not null)
        {
            var booking = await Db.Bookings.ForCompany(Tenant).FirstOrDefaultAsync(b => b.Id == bookingId)
                ?? throw new InvalidOperationException("That booking does not exist.");
            partyId = booking.PrimaryApplicantPartyId;
            amount = booking.OverdueAmount;
        }
        else
        {
            var tenancy = await Db.Tenancies.ForCompany(Tenant).FirstOrDefaultAsync(t => t.Id == tenancyId)
                ?? throw new InvalidOperationException("That tenancy does not exist.");
            amount = tenancy.ArrearsAmount;

            var lead = await Db.TenancyParties.ForCompany(Tenant)
                .Where(p => p.TenancyId == tenancy.Id && p.IsLeadTenant)
                .Select(p => p.PartyId)
                .FirstOrDefaultAsync();
            partyId = lead;
        }

        var notice = new LegalNotice
        {
            NoticeNumber = await numbering.NextNoticeNumberAsync(DateTime.UtcNow),
            PartyId = partyId,
            BookingId = bookingId,
            TenancyId = tenancyId,
            NoticeType = noticeType,
            IssuedOn = Today,
            ComplyByDate = complyBy ?? Today.AddDays(15),
            AmountDemanded = amount,
            IssuedByUserId = userId,
        }.StampNew(Tenant, userId);

        // A final or legal notice is a serious instrument; it goes through the approval matrix.
        if (noticeType is "FinalNotice" or "LegalDemand" or "CancellationNotice")
        {
            var approval = await RaiseApprovalAsync(
                "LegalNotice", notice.Id, notice.NoticeNumber, amount,
                $"Issue {noticeType} for {amount:N0}", userId);
            notice.ApprovalRequestId = approval?.Id;
        }

        Db.LegalNotices.Add(notice);
        await Db.SaveChangesAsync();

        return (await GetNoticeAsync(notice.Id))!;
    }

    public async Task<LegalNoticeDto> RecordServiceAsync(
        Guid noticeId, string method, string? reference, string? evidenceUrl, DateOnly servedOn, Guid userId)
    {
        var notice = await Db.LegalNotices.ForCompany(Tenant).FirstOrDefaultAsync(n => n.Id == noticeId)
            ?? throw new InvalidOperationException("That notice does not exist.");

        notice.ServedOn = servedOn;
        notice.ServiceMethod = method;
        notice.ServiceReference = reference;
        notice.ServiceEvidenceUrl = evidenceUrl;
        notice.StampUpdated(userId);

        await Db.SaveChangesAsync();
        return (await GetNoticeAsync(noticeId))!;
    }

    private async Task<LegalNoticeDto?> GetNoticeAsync(Guid id)
    {
        var n = await Db.LegalNotices.ForCompany(Tenant).FirstOrDefaultAsync(x => x.Id == id);
        if (n is null) return null;

        var names = await PartyNamesAsync([n.PartyId]);
        var bookingRef = n.BookingId is null
            ? null
            : await Db.Bookings.ForCompany(Tenant).Where(b => b.Id == n.BookingId).Select(b => b.Reference).FirstOrDefaultAsync();

        return new LegalNoticeDto
        {
            Id = n.Id,
            NoticeNumber = n.NoticeNumber,
            NoticeType = n.NoticeType,
            PartyId = n.PartyId,
            PartyName = names.GetValueOrDefault(n.PartyId, "—"),
            BookingId = n.BookingId,
            BookingReference = bookingRef,
            TenancyId = n.TenancyId,
            IssuedOn = n.IssuedOn,
            ComplyByDate = n.ComplyByDate,
            AmountDemanded = n.AmountDemanded,
            DocumentUrl = n.DocumentUrl,
            ServedOn = n.ServedOn,
            ServiceMethod = n.ServiceMethod,
            ServiceReference = n.ServiceReference,
            ServiceEvidenceUrl = n.ServiceEvidenceUrl,
            IsAcknowledged = n.IsAcknowledged,
            IsComplied = n.IsComplied,
            IsWithdrawn = n.IsWithdrawn,
            IsOverdue = !n.IsComplied && n.ComplyByDate is not null && n.ComplyByDate < Today,
        };
    }

    public async Task<PaginatedResponse<LegalNoticeDto>> GetNoticesAsync(ListQueryDto query)
    {
        var q = Db.LegalNotices.ForCompany(Tenant)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Search), n => n.NoticeNumber.Contains(query.Search!))
            .WhereIf(query.FromDate.HasValue, n => n.IssuedOn >= query.FromDate)
            .WhereIf(query.ToDate.HasValue, n => n.IssuedOn <= query.ToDate)
            .OrderByDescending(n => n.IssuedOn);

        return await PageAsync(q, query, async rows =>
        {
            var names = await PartyNamesAsync(rows.Select(r => r.PartyId));
            var bookingIds = rows.Where(r => r.BookingId.HasValue).Select(r => r.BookingId!.Value).Distinct().ToList();

            var bookings = bookingIds.Count == 0
                ? []
                : await Db.Bookings.ForCompany(Tenant).Where(b => bookingIds.Contains(b.Id))
                    .ToDictionaryAsync(b => b.Id, b => b.Reference);

            return rows.Select(n => new LegalNoticeDto
            {
                Id = n.Id,
                NoticeNumber = n.NoticeNumber,
                NoticeType = n.NoticeType,
                PartyId = n.PartyId,
                PartyName = names.GetValueOrDefault(n.PartyId, "—"),
                BookingId = n.BookingId,
                BookingReference = n.BookingId is null ? null : bookings.GetValueOrDefault(n.BookingId.Value),
                TenancyId = n.TenancyId,
                IssuedOn = n.IssuedOn,
                ComplyByDate = n.ComplyByDate,
                AmountDemanded = n.AmountDemanded,
                DocumentUrl = n.DocumentUrl,
                ServedOn = n.ServedOn,
                ServiceMethod = n.ServiceMethod,
                IsAcknowledged = n.IsAcknowledged,
                IsComplied = n.IsComplied,
                IsWithdrawn = n.IsWithdrawn,
                IsOverdue = !n.IsComplied && n.ComplyByDate is not null && n.ComplyByDate < Today,
            }).ToList();
        });
    }

    // ═══ Write-offs ══════════════════════════════════════════════════════════

    public async Task<WriteOffDto> RequestWriteOffAsync(WriteOffDto dto, Guid userId)
    {
        if (dto.TotalAmount <= 0m)
            throw new InvalidOperationException("Enter the amount to be written off.");

        var writeOff = new WriteOff
        {
            Reference = await numbering.NextMasterCodeAsync(Db.WriteOffs, "WOF"),
            PartyId = dto.PartyId,
            BookingId = dto.BookingId,
            TenancyId = dto.TenancyId,
            PrincipalAmount = dto.PrincipalAmount,
            SurchargeAmount = dto.SurchargeAmount,
            TotalAmount = dto.TotalAmount,
            WriteOffDate = dto.WriteOffDate == default ? Today : dto.WriteOffDate,
            ReasonCodeId = dto.ReasonCodeId ?? Guid.Empty,
            Note = dto.Note,
            RequestedByUserId = userId,
            Outcome = ApprovalOutcome.Pending,
            IsProvisionOnly = dto.IsProvisionOnly,
        }.StampNew(Tenant, userId);

        var approval = await RaiseApprovalAsync(
            "WriteOff", writeOff.Id, writeOff.Reference, dto.TotalAmount,
            $"Write off {dto.TotalAmount:N0}", userId, note: dto.Note);

        writeOff.ApprovalRequestId = approval?.Id;

        Db.WriteOffs.Add(writeOff);
        await Db.SaveChangesAsync();

        return (await GetWriteOffAsync(writeOff.Id))!;
    }

    public async Task<WriteOffDto> DecideWriteOffAsync(Guid id, ApprovalOutcome outcome, string? comment, Guid userId)
    {
        var writeOff = await Db.WriteOffs.ForCompany(Tenant).FirstOrDefaultAsync(w => w.Id == id)
            ?? throw new InvalidOperationException("That write-off does not exist.");

        if (writeOff.Outcome != ApprovalOutcome.Pending)
            throw new InvalidOperationException("This write-off has already been decided.");

        writeOff.Outcome = outcome;
        writeOff.Note = string.IsNullOrWhiteSpace(comment) ? writeOff.Note : $"{writeOff.Note}\n{comment}".Trim();
        writeOff.StampUpdated(userId);

        if (outcome == ApprovalOutcome.Approved && !writeOff.IsProvisionOnly && writeOff.BookingId is not null)
        {
            var instalments = await Db.Instalments.ForCompany(Tenant)
                .Where(i => i.BookingId == writeOff.BookingId && i.Balance > 0)
                .OrderBy(i => i.DueDate)
                .ToListAsync();

            var remaining = writeOff.PrincipalAmount;

            foreach (var instalment in instalments)
            {
                if (remaining <= 0m) break;

                var applied = Math.Min(instalment.Balance, remaining);
                instalment.WaivedAmount += applied;
                instalment.Balance = RealEstateMapper.Money(instalment.TotalAmount - instalment.PaidAmount - instalment.WaivedAmount);
                if (instalment.Balance <= 0m) instalment.Status = InstalmentStatus.Waived;
                instalment.StampUpdated(userId);
                remaining -= applied;
            }

            Db.CustomerLedgerEntries.Add(new CustomerLedgerEntry
            {
                PartyId = writeOff.PartyId,
                BookingId = writeOff.BookingId,
                EntryDate = writeOff.WriteOffDate,
                Kind = LedgerEntryKind.Adjustment,
                Description = $"Write-off {writeOff.Reference}",
                CreditAmount = writeOff.TotalAmount,
            }.StampNew(Tenant, userId));
        }

        await Db.SaveChangesAsync();

        if (writeOff.BookingId is not null)
        {
            await RecalculateBookingTotalsAsync(writeOff.BookingId.Value, userId);
            await Db.SaveChangesAsync();
        }

        return (await GetWriteOffAsync(id))!;
    }

    private async Task<WriteOffDto?> GetWriteOffAsync(Guid id)
    {
        var w = await Db.WriteOffs.ForCompany(Tenant).FirstOrDefaultAsync(x => x.Id == id);
        if (w is null) return null;

        var names = await PartyNamesAsync([w.PartyId]);
        var reasons = await ReasonLabelsAsync([w.ReasonCodeId]);
        var bookingRef = w.BookingId is null
            ? null
            : await Db.Bookings.ForCompany(Tenant).Where(b => b.Id == w.BookingId).Select(b => b.Reference).FirstOrDefaultAsync();

        return new WriteOffDto
        {
            Id = w.Id,
            Reference = w.Reference,
            PartyId = w.PartyId,
            PartyName = names.GetValueOrDefault(w.PartyId, "—"),
            BookingId = w.BookingId,
            BookingReference = bookingRef,
            PrincipalAmount = w.PrincipalAmount,
            SurchargeAmount = w.SurchargeAmount,
            TotalAmount = w.TotalAmount,
            WriteOffDate = w.WriteOffDate,
            ReasonLabel = reasons.GetValueOrDefault(w.ReasonCodeId, "—"),
            Note = w.Note,
            RequestedByName = "—",
            Outcome = w.Outcome,
            IsProvisionOnly = w.IsProvisionOnly,
        };
    }
}
