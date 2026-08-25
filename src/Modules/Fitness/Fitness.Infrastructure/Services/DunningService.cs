using Fitness.Application.DTOs;
using Fitness.Application.Services.Interfaces;
using Fitness.Domain.Entities;
using Fitness.Domain.Enums;
using Fitness.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;

namespace Fitness.Infrastructure.Services;

/// <summary>
/// The collection ladder.
///
/// A failed direct debit is not an event, it is the start of a sequence: retry, email, text, ask
/// for a new card, raise a call task, suspend the fob, write it off. What recovers money is the
/// *sequence* — and specifically that it keeps happening without anybody remembering to do it.
///
/// The one rule worth stating: a case is opened per invoice and closed the moment the money
/// arrives, from anywhere. Somebody paying at the desk must silently stop the texts, or the club
/// spends its goodwill chasing people who have already paid.
/// </summary>
public class DunningService(
    FitnessDbContext db,
    IFitnessTenant tenant,
    FitnessNumbering numbering,
    IBillingService billing) : IDunningService
{
    public async Task<PaginatedResponse<DunningCaseDto>> ListCasesAsync(
        Guid? clubId, DunningCaseStatus? status, Guid? assignedStaffId, PaginationParams pagination)
    {
        var now = DateTime.UtcNow;

        var query = db.DunningCases.ForTenant(tenant)
            .Include(c => c.Member)
            .Include(c => c.Events.Where(e => !e.IsDeleted))
            .WhereIf(clubId is not null, c => c.ClubId == clubId)
            .WhereIf(status is not null, c => c.Status == status)
            .WhereIf(assignedStaffId is not null, c => c.AssignedToStaffId == assignedStaffId);

        var total = await query.CountAsync();

        var page = await query
            .OrderBy(c => c.Status)
            .ThenByDescending(c => c.AmountOutstanding)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        var policyIds = page.Select(c => c.DunningPolicyId).Distinct().ToList();
        var stepCounts = await db.DunningSteps.ForTenant(tenant)
            .Where(s => policyIds.Contains(s.DunningPolicyId))
            .GroupBy(s => s.DunningPolicyId)
            .Select(g => new { PolicyId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PolicyId, x => x.Count);

        var items = new List<DunningCaseDto>();
        foreach (var c in page) items.Add(await EnrichAsync(c, now, stepCounts.GetValueOrDefault(c.DunningPolicyId)));

        return PaginatedResponse<DunningCaseDto>.Ok(
                   items,
                   total,
                   pagination.PageNumber,
                   pagination.PageSize);
    }

    public async Task<DunningCaseDto?> GetCaseAsync(Guid caseId)
    {
        var now = DateTime.UtcNow;

        var dunningCase = await db.DunningCases.ForTenant(tenant)
            .Include(c => c.Member)
            .Include(c => c.Events.Where(e => !e.IsDeleted))
            .FirstOrDefaultAsync(c => c.Id == caseId);

        if (dunningCase is null) return null;

        var stepCount = await db.DunningSteps.ForTenant(tenant)
            .CountAsync(s => s.DunningPolicyId == dunningCase.DunningPolicyId);

        return await EnrichAsync(dunningCase, now, stepCount);
    }

    public async Task<DunningCaseDto> OpenCaseAsync(Guid invoiceId, PaymentFailureReason reason, Guid userId)
    {
        var now = DateTime.UtcNow;

        var invoice = await db.Invoices.ForTenant(tenant)
            .Include(i => i.Member)
            .FirstOrDefaultAsync(i => i.Id == invoiceId)
            ?? throw new InvalidOperationException("Invoice not found.");

        var existing = await db.DunningCases.ForTenant(tenant)
            .Include(c => c.Member)
            .Include(c => c.Events.Where(e => !e.IsDeleted))
            .FirstOrDefaultAsync(c => c.InvoiceId == invoiceId && c.Status == DunningCaseStatus.Open);

        // Idempotent: two failed attempts on one invoice is one problem, not two.
        if (existing is not null) return await EnrichAsync(existing, now, 0);

        var club = await db.Clubs.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == invoice.ClubId);

        var policy = await ResolvePolicyAsync(club)
            ?? throw new InvalidOperationException("No dunning policy is configured for this club.");

        var firstStep = policy.Steps.OrderBy(s => s.StepNumber).FirstOrDefault();

        var dunningCase = new DunningCase
        {
            CaseNumber = await numbering.NextDunningCaseNumberAsync(now),
            MemberId = invoice.MemberId,
            InvoiceId = invoiceId,
            AgreementId = invoice.AgreementId,
            ClubId = invoice.ClubId,
            DunningPolicyId = policy.Id,
            Status = DunningCaseStatus.Open,
            OpenedOn = now,
            AmountOutstanding = invoice.BalanceDue,
            InitialFailureReason = reason,
            CurrentStep = 0,
            NextStepDueOn = firstStep is null ? null : now.Date.AddDays(firstStep.DelayDays),
        }.StampNew(tenant, userId);

        db.DunningCases.Add(dunningCase);
        invoice.DunningCaseId = dunningCase.Id;
        invoice.Status = InvoiceStatus.InDunning;

        if (invoice.Member is not null && invoice.Member.Status == MemberStatus.Active)
            invoice.Member.Status = MemberStatus.PastDue;

        db.DunningEvents.Add(new DunningEvent
        {
            DunningCaseId = dunningCase.Id,
            StepNumber = 0,
            Action = DunningAction.Retry,
            OccurredAt = now,
            Succeeded = false,
            Detail = $"Collection failed — {Describe(reason)}",
        }.StampNew(tenant, userId));

        await db.SaveChangesAsync();
        return await EnrichAsync(dunningCase, now, policy.Steps.Count);
    }

    /// <summary>
    /// A human intervening in a case: retry now, pause it, hand it to someone, write it off, or
    /// record that the member promised to pay on a date.
    ///
    /// The promise-to-pay is the useful one. It pauses the ladder until the promised date rather
    /// than cancelling it, which is exactly what a collections clerk wants and what an
    /// on-or-off pause cannot express.
    /// </summary>
    public async Task<DunningCaseDto> ActionCaseAsync(DunningActionDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var dunningCase = await db.DunningCases.ForTenant(tenant)
            .Include(c => c.Member)
            .Include(c => c.Events.Where(e => !e.IsDeleted))
            .FirstOrDefaultAsync(c => c.Id == request.DunningCaseId)
            ?? throw new InvalidOperationException("Case not found.");

        switch (request.Action.ToLowerInvariant())
        {
            case "retry":
                await AttemptCollectionAsync(dunningCase, userId, manual: true);
                break;

            case "pause":
                dunningCase.IsPaused = true;
                dunningCase.PauseReason = request.Note;
                LogEvent(dunningCase, DunningAction.CreateCallTask, true, $"Paused — {request.Note}", userId);
                break;

            case "resume":
                dunningCase.IsPaused = false;
                dunningCase.PauseReason = null;
                LogEvent(dunningCase, DunningAction.CreateCallTask, true, "Resumed", userId);
                break;

            case "assign":
                dunningCase.AssignedToStaffId = request.AssignToStaffId;
                LogEvent(dunningCase, DunningAction.CreateCallTask, true, "Assigned for a call", userId);
                break;

            case "promise":
                if (request.PromiseToPayOn is null)
                    throw new InvalidOperationException("A promise to pay needs a date.");

                dunningCase.IsPaused = true;
                dunningCase.PauseReason = $"Promised to pay by {request.PromiseToPayOn:d MMM yyyy}";
                dunningCase.NextStepDueOn = request.PromiseToPayOn.Value.AddDays(1);
                LogEvent(dunningCase, DunningAction.CreateCallTask, true,
                    $"Member promised payment by {request.PromiseToPayOn:d MMM yyyy}. {request.Note}", userId);
                break;

            case "writeoff":
                await WriteOffCaseAsync(dunningCase, request.Note ?? "Written off manually", userId);
                break;

            default:
                throw new InvalidOperationException($"'{request.Action}' is not something that can be done to a case.");
        }

        dunningCase.StampUpdated(userId);
        await db.SaveChangesAsync();

        var stepCount = await db.DunningSteps.ForTenant(tenant)
            .CountAsync(s => s.DunningPolicyId == dunningCase.DunningPolicyId);

        return await EnrichAsync(dunningCase, now, stepCount);
    }

    /// <summary>
    /// Walks every open case to its next due step. Runs nightly.
    ///
    /// Steps are executed one per run rather than catching up several at once — a member who was
    /// missed for three days should get one message today, not three.
    /// </summary>
    public async Task<int> ProcessDueStepsAsync()
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var processed = 0;

        var due = await db.DunningCases.ForTenant(tenant)
            .Include(c => c.Member)
            .Where(c => c.Status == DunningCaseStatus.Open && !c.IsPaused
                     && c.NextStepDueOn != null && c.NextStepDueOn <= today)
            .Take(500)
            .ToListAsync();

        foreach (var dunningCase in due)
        {
            var policy = await db.DunningPolicies.ForTenant(tenant)
                .Include(p => p.Steps.Where(s => !s.IsDeleted))
                .FirstOrDefaultAsync(p => p.Id == dunningCase.DunningPolicyId);

            if (policy is null) continue;

            // Money may have arrived from anywhere since the last run.
            var invoice = dunningCase.InvoiceId is null
                ? null
                : await db.Invoices.ForTenant(tenant).FirstOrDefaultAsync(i => i.Id == dunningCase.InvoiceId);

            if (invoice is not null && invoice.BalanceDue <= 0)
            {
                await CloseAsRecoveredAsync(dunningCase, dunningCase.AmountOutstanding, "Paid before the next step", Guid.Empty);
                processed++;
                continue;
            }

            var next = policy.Steps
                .Where(s => s.StepNumber > dunningCase.CurrentStep)
                .OrderBy(s => s.StepNumber)
                .FirstOrDefault();

            if (next is null)
            {
                // Off the end of the ladder: write off if the policy says to, otherwise escalate.
                if (policy.WriteOffAfterDays > 0 && (today - dunningCase.OpenedOn.Date).Days >= policy.WriteOffAfterDays)
                    await WriteOffCaseAsync(dunningCase, "End of the collection process", Guid.Empty);
                else
                {
                    dunningCase.Status = DunningCaseStatus.Escalated;
                    LogEvent(dunningCase, DunningAction.ReferToCollections, true,
                        "Every step has been tried. Escalated for a decision.", Guid.Empty);
                }

                processed++;
                continue;
            }

            await ExecuteStepAsync(dunningCase, next, policy, Guid.Empty);

            dunningCase.CurrentStep = next.StepNumber;

            var following = policy.Steps
                .Where(s => s.StepNumber > next.StepNumber)
                .OrderBy(s => s.StepNumber)
                .FirstOrDefault();

            dunningCase.NextStepDueOn = following is null
                ? (policy.WriteOffAfterDays > 0 ? dunningCase.OpenedOn.Date.AddDays(policy.WriteOffAfterDays) : null)
                : today.AddDays(following.DelayDays);

            dunningCase.StampUpdated(Guid.Empty);
            processed++;
        }

        await db.SaveChangesAsync();
        return processed;
    }

    // ── Policies ─────────────────────────────────────────────────────────────

    public async Task<List<DunningPolicyDto>> GetPoliciesAsync(Guid? clubId)
    {
        var policies = await db.DunningPolicies.ForTenant(tenant)
            .WhereIf(clubId is not null, p => p.ClubId == clubId || p.ClubId == null)
            .Include(p => p.Steps.Where(s => !s.IsDeleted))
            .OrderByDescending(p => p.IsDefault).ThenBy(p => p.Name)
            .ToListAsync();

        var templates = await db.MessageTemplates.ForTenant(tenant)
            .Select(t => new { t.Id, t.Name })
            .ToDictionaryAsync(t => t.Id, t => t.Name);

        var openCases = await db.DunningCases.ForTenant(tenant)
            .Where(c => c.Status == DunningCaseStatus.Open)
            .GroupBy(c => c.DunningPolicyId)
            .Select(g => new { PolicyId = g.Key, Count = g.Count(), Amount = g.Sum(c => c.AmountOutstanding) })
            .ToListAsync();

        return [.. policies.Select(p =>
        {
            var dto = FitnessMapper.ToDto(p);
            var stats = openCases.FirstOrDefault(o => o.PolicyId == p.Id);
            dto.OpenCaseCount = stats?.Count ?? 0;
            dto.AmountInRecovery = stats?.Amount ?? 0m;

            foreach (var step in dto.Steps.Where(s => s.MessageTemplateId is not null))
                step.MessageTemplateName = templates.GetValueOrDefault(step.MessageTemplateId!.Value);

            return dto;
        })];
    }

    public async Task<DunningPolicyDto> SavePolicyAsync(Guid? id, DunningPolicyDto request, Guid userId)
    {
        DunningPolicy policy;
        if (id is null)
        {
            policy = new DunningPolicy().StampNew(tenant, userId);
            db.DunningPolicies.Add(policy);
        }
        else
        {
            policy = await db.DunningPolicies.ForTenant(tenant)
                .Include(p => p.Steps.Where(s => !s.IsDeleted))
                .FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new InvalidOperationException("Dunning policy not found.");
            policy.StampUpdated(userId);
        }

        policy.Name = request.Name;
        policy.ClubId = request.ClubId;
        policy.IsDefault = request.IsDefault;
        policy.WriteOffAfterDays = request.WriteOffAfterDays;
        policy.SuspendAccessAfterDays = request.SuspendAccessAfterDays;
        policy.IsActive = request.IsActive;

        foreach (var existing in policy.Steps.Where(s => !s.IsDeleted)) existing.StampDeleted(userId);

        var number = 1;
        foreach (var step in request.Steps.OrderBy(s => s.StepNumber))
        {
            db.DunningSteps.Add(new DunningStep
            {
                DunningPolicyId = policy.Id,
                StepNumber = number++,
                DelayDays = step.DelayDays,
                Action = step.Action,
                Channel = step.Channel,
                MessageTemplateId = step.MessageTemplateId,
                FeeAmount = step.FeeAmount,
                SkipOnTechnicalFailure = step.SkipOnTechnicalFailure,
            }.StampNew(tenant, userId));
        }

        await db.SaveChangesAsync();

        var saved = await db.DunningPolicies.ForTenant(tenant)
            .Include(p => p.Steps.Where(s => !s.IsDeleted))
            .FirstAsync(p => p.Id == policy.Id);

        return FitnessMapper.ToDto(saved);
    }

    /// <summary>
    /// The arrears report: what is owed, how old it is, and whose card is the problem.
    ///
    /// Ageing bands rather than a flat list, because 30-day debt and 90-day debt need completely
    /// different conversations — and the failure-reason breakdown tells a manager whether they
    /// have a collections problem or a card-expiry problem, which are also different.
    /// </summary>
    public async Task<ArrearsReportDto> GetArrearsAsync(Guid? clubId, DateTime? asAt)
    {
        var at = (asAt ?? DateTime.UtcNow).Date;

        var overdue = await db.Invoices.ForTenant(tenant)
            .Where(i => i.BalanceDue > 0 && i.DueOn < at
                     && i.Status != InvoiceStatus.Cancelled && i.Status != InvoiceStatus.WrittenOff)
            .WhereIf(clubId is not null, i => i.ClubId == clubId)
            .Include(i => i.Member)
            .ToListAsync();

        var report = new ArrearsReportDto
        {
            ClubId = clubId,
            AsAt = at,
            TotalOutstanding = overdue.Sum(i => i.BalanceDue),
            MemberCount = overdue.Select(i => i.MemberId).Distinct().Count(),
        };

        foreach (var invoice in overdue)
        {
            var age = (at - invoice.DueOn.Date).Days;
            if (age <= 0) report.Current += invoice.BalanceDue;
            else if (age <= 30) report.Days1To30 += invoice.BalanceDue;
            else if (age <= 60) report.Days31To60 += invoice.BalanceDue;
            else if (age <= 90) report.Days61To90 += invoice.BalanceDue;
            else report.Over90Days += invoice.BalanceDue;
        }

        var cases = await db.DunningCases.ForTenant(tenant)
            .WhereIf(clubId is not null, c => c.ClubId == clubId)
            .ToListAsync();

        report.OpenDunningCases = cases.Count(c => c.Status == DunningCaseStatus.Open);
        report.InRecovery = cases.Where(c => c.Status == DunningCaseStatus.Open).Sum(c => c.AmountOutstanding);

        var monthStart = new DateTime(at.Year, at.Month, 1);
        var recoveredThisMonth = cases
            .Where(c => c.Status == DunningCaseStatus.Recovered && c.ClosedOn >= monthStart)
            .ToList();

        report.RecoveredThisMonth = recoveredThisMonth.Sum(c => c.AmountRecovered);

        var closedThisMonth = cases.Count(c => c.ClosedOn >= monthStart);
        report.RecoveryRatePercent = closedThisMonth == 0
            ? 0
            : recoveredThisMonth.Count * 100 / closedThisMonth;

        // The per-member list, worst first.
        var methods = await db.PaymentMethods.ForTenant(tenant)
            .Where(p => p.IsActive && p.IsDefault)
            .Select(p => p.MemberId)
            .ToListAsync();

        var methodSet = methods.ToHashSet();
        var suspended = await db.MemberAlerts.ForTenant(tenant)
            .Where(a => a.Kind == MemberAlertKind.OutstandingBalance && a.BlocksAccess)
            .Select(a => a.MemberId)
            .ToListAsync();

        var suspendedSet = suspended.ToHashSet();

        report.Members = [.. overdue
            .GroupBy(i => i.MemberId)
            .Select(g =>
            {
                var oldest = g.OrderBy(i => i.DueOn).First();
                var age = (at - oldest.DueOn.Date).Days;
                var dunningCase = cases.FirstOrDefault(c => c.MemberId == g.Key && c.Status == DunningCaseStatus.Open);

                return new ArrearsLineDto
                {
                    MemberId = g.Key,
                    MemberName = oldest.Member is null ? "" : FitnessMapper.FullName(oldest.Member),
                    MemberNumber = oldest.Member?.MemberNumber,
                    Phone = oldest.Member?.Phone,
                    Outstanding = g.Sum(i => i.BalanceDue),
                    DaysOverdue = age,
                    AgeBand = age <= 30 ? "1–30 days" : age <= 60 ? "31–60 days" : age <= 90 ? "61–90 days" : "Over 90 days",
                    LastFailureReason = dunningCase?.InitialFailureReason,
                    HasValidPaymentMethod = methodSet.Contains(g.Key),
                    AccessSuspended = suspendedSet.Contains(g.Key),
                    DunningCaseId = dunningCase?.Id,
                    DunningStep = dunningCase?.CurrentStep ?? 0,
                };
            })
            .OrderByDescending(m => m.DaysOverdue)
            .ThenByDescending(m => m.Outstanding)];

        report.ByFailureReason = [.. cases
            .Where(c => c.Status == DunningCaseStatus.Open)
            .GroupBy(c => c.InitialFailureReason)
            .Select(g => new RevenueLineDto
            {
                Label = Describe(g.Key),
                Amount = g.Sum(c => c.AmountOutstanding),
                Count = g.Count(),
                PercentOfTotal = report.InRecovery == 0 ? 0
                    : Math.Round(g.Sum(c => c.AmountOutstanding) / report.InRecovery * 100m, 1),
            })
            .OrderByDescending(l => l.Amount)];

        return report;
    }

    // ═══ Internals ═══════════════════════════════════════════════════════════

    private async Task ExecuteStepAsync(DunningCase dunningCase, DunningStep step, DunningPolicy policy, Guid userId)
    {
        var now = DateTime.UtcNow;

        // A technical failure is the gateway's fault, not the member's — chasing them for it
        // burns goodwill for nothing.
        if (step.SkipOnTechnicalFailure && dunningCase.InitialFailureReason == PaymentFailureReason.TechnicalError
            && step.Action is DunningAction.SendSms or DunningAction.AddLateFee or DunningAction.SuspendAccess)
        {
            LogEvent(dunningCase, step.Action, true, "Skipped — the original failure was technical", userId);
            return;
        }

        switch (step.Action)
        {
            case DunningAction.Retry:
                await AttemptCollectionAsync(dunningCase, userId, manual: false);
                break;

            case DunningAction.SendEmail or DunningAction.SendSms or DunningAction.SendPush:
                await QueueMessageAsync(dunningCase, step, userId);
                break;

            case DunningAction.RequestCardUpdate:
                await QueueMessageAsync(dunningCase, step, userId, "Please update your payment details");
                break;

            case DunningAction.CreateCallTask:
                db.RetentionTasks.Add(new RetentionTask
                {
                    MemberId = dunningCase.MemberId,
                    ClubId = dunningCase.ClubId,
                    AssignedStaffId = dunningCase.AssignedToStaffId,
                    Title = $"Call about {dunningCase.AmountOutstanding:0.00} outstanding",
                    Detail = $"Case {dunningCase.CaseNumber}, open {(DateTime.UtcNow - dunningCase.OpenedOn).Days} days. " +
                             $"Original failure: {Describe(dunningCase.InitialFailureReason)}.",
                    Trigger = "Dunning",
                    DueOn = now.Date,
                    Priority = 1,
                }.StampNew(tenant, userId));

                LogEvent(dunningCase, step.Action, true, "Call task raised", userId);
                break;

            case DunningAction.AddLateFee when step.FeeAmount > 0:
                await billing.CreateAdHocInvoiceAsync(dunningCase.MemberId, dunningCase.ClubId, [
                    new InvoiceLineDto
                    {
                        ChargeKind = ChargeKind.LateFee,
                        LineDescription = "Late payment fee",
                        Quantity = 1,
                        UnitPrice = step.FeeAmount,
                        LineTotal = step.FeeAmount,
                    },
                ], userId);

                dunningCase.LateFeesAdded += step.FeeAmount;
                dunningCase.AmountOutstanding += step.FeeAmount;
                LogEvent(dunningCase, step.Action, true, $"Late fee of {step.FeeAmount:0.00} added", userId);
                break;

            case DunningAction.SuspendAccess:
                await SuspendAccessAsync(dunningCase, userId);
                break;

            case DunningAction.CancelAgreement:
                await CancelAgreementAsync(dunningCase, userId);
                break;

            case DunningAction.WriteOff:
                await WriteOffCaseAsync(dunningCase, "Reached the write-off step", userId);
                break;

            case DunningAction.ReferToCollections:
                dunningCase.Status = DunningCaseStatus.Escalated;
                LogEvent(dunningCase, step.Action, true, "Referred to an external agency", userId);
                break;
        }
    }

    private async Task AttemptCollectionAsync(DunningCase dunningCase, Guid userId, bool manual)
    {
        var now = DateTime.UtcNow;

        var invoice = dunningCase.InvoiceId is null
            ? null
            : await db.Invoices.ForTenant(tenant).FirstOrDefaultAsync(i => i.Id == dunningCase.InvoiceId);

        if (invoice is null || invoice.BalanceDue <= 0)
        {
            await CloseAsRecoveredAsync(dunningCase, dunningCase.AmountOutstanding, "Already settled", userId);
            return;
        }

        var method = await db.PaymentMethods.ForTenant(tenant)
            .Where(p => p.MemberId == dunningCase.MemberId && p.IsActive && p.IsDefault)
            .FirstOrDefaultAsync();

        if (method is null)
        {
            LogEvent(dunningCase, DunningAction.Retry, false, "No payment method on file to retry against", userId);
            return;
        }

        dunningCase.RetryAttempts++;
        dunningCase.LastRetryOn = now;

        var result = await billing.TakePaymentAsync(new TakePaymentDto
        {
            MemberId = dunningCase.MemberId,
            ClubId = dunningCase.ClubId,
            InvoiceId = invoice.Id,
            Amount = invoice.BalanceDue,
            Method = method.Method,
            PaymentMethodRefId = method.Id,
            CardBrand = method.CardBrand,
            CardLastFour = method.CardLastFour,
            Notes = manual ? "Retried by staff" : "Automatic retry",
            IdempotencyKey = $"dunning:{dunningCase.Id}:attempt:{dunningCase.RetryAttempts}",
        }, userId);

        if (result.Status == PaymentStatus.Succeeded)
        {
            await CloseAsRecoveredAsync(dunningCase, result.AmountTaken,
                manual ? "Recovered on a manual retry" : $"Recovered on retry {dunningCase.RetryAttempts}", userId);
        }
        else
        {
            LogEvent(dunningCase, DunningAction.Retry, false,
                $"Retry {dunningCase.RetryAttempts} failed", userId);
        }
    }

    private async Task QueueMessageAsync(DunningCase dunningCase, DunningStep step, Guid userId, string? subject = null)
    {
        var member = dunningCase.Member
            ?? await db.Members.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == dunningCase.MemberId);

        if (member is null) return;

        var channel = step.Channel ?? MessageChannel.Email;

        // Consent does not apply to a message about money the member owes — this is a service
        // message, not marketing, and that distinction is exactly what transactional means.
        var recipient = channel switch
        {
            MessageChannel.Email => member.Email,
            MessageChannel.Sms or MessageChannel.WhatsApp => member.Phone,
            _ => null,
        };

        db.MessageLog.Add(new MessageLog
        {
            MemberId = member.Id,
            ClubId = dunningCase.ClubId,
            Channel = channel,
            Status = recipient is null ? MessageStatus.Failed : MessageStatus.Queued,
            MessageTemplateId = step.MessageTemplateId,
            DunningCaseId = dunningCase.Id,
            Recipient = recipient,
            Subject = subject ?? "A payment on your membership did not go through",
            BodyPreview = $"{dunningCase.AmountOutstanding:0.00} is outstanding on your account.",
            FailureReason = recipient is null ? $"No {channel} address on file" : null,
            QueuedAt = DateTime.UtcNow,
        }.StampNew(tenant, userId));

        LogEvent(dunningCase, step.Action, recipient is not null,
            recipient is null ? $"No {channel} address on file" : $"{channel} queued to {recipient}", userId);
    }

    private async Task SuspendAccessAsync(DunningCase dunningCase, Guid userId)
    {
        var member = dunningCase.Member
            ?? await db.Members.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == dunningCase.MemberId);

        if (member is null) return;

        var already = await db.Suspensions.ForTenant(tenant)
            .AnyAsync(s => s.MemberId == member.Id && s.LiftedOn == null
                        && s.Reason == SuspensionReason.UnpaidBalance);

        if (already)
        {
            LogEvent(dunningCase, DunningAction.SuspendAccess, true, "Already suspended", userId);
            return;
        }

        db.Suspensions.Add(new MembershipSuspension
        {
            MemberId = member.Id,
            AgreementId = dunningCase.AgreementId,
            StartsOn = DateTime.UtcNow.Date,
            Reason = SuspensionReason.UnpaidBalance,
            ReasonNote = $"Case {dunningCase.CaseNumber} — {dunningCase.AmountOutstanding:0.00} outstanding",
            ContinuesBilling = true,
            AutoLiftsWhenResolved = true,
            ImposedByUserId = userId,
        }.StampNew(tenant, userId));

        member.Status = MemberStatus.Suspended;
        member.StampUpdated(userId);

        dunningCase.Status = DunningCaseStatus.Suspended;

        LogEvent(dunningCase, DunningAction.SuspendAccess, true,
            "Access suspended until the balance is cleared", userId);
    }

    private async Task CancelAgreementAsync(DunningCase dunningCase, Guid userId)
    {
        if (dunningCase.AgreementId is null) return;

        var agreement = await db.Agreements.ForTenant(tenant)
            .FirstOrDefaultAsync(a => a.Id == dunningCase.AgreementId);

        if (agreement is null || agreement.Status == AgreementStatus.Cancelled) return;

        agreement.Status = AgreementStatus.Cancelled;
        agreement.CancelledOn = DateTime.UtcNow;
        agreement.EndsOn = DateTime.UtcNow.Date;
        agreement.LeaveReason = LeaveReason.Other;
        agreement.LeaveNote = $"Cancelled for non-payment — case {dunningCase.CaseNumber}";
        agreement.NextBillingOn = null;
        agreement.StampUpdated(userId);

        var future = await db.BillingSchedules.ForTenant(tenant)
            .Where(s => s.AgreementId == agreement.Id && !s.IsBilled)
            .ToListAsync();

        foreach (var charge in future)
        {
            charge.IsSkipped = true;
            charge.SkipReason = "Cancelled for non-payment";
        }

        LogEvent(dunningCase, DunningAction.CancelAgreement, true, "Membership cancelled for non-payment", userId);
    }

    private async Task WriteOffCaseAsync(DunningCase dunningCase, string reason, Guid userId)
    {
        await billing.WriteOffAsync(dunningCase.MemberId, dunningCase.InvoiceId,
            dunningCase.AmountOutstanding, reason, userId);

        dunningCase.Status = DunningCaseStatus.WrittenOff;
        dunningCase.ClosedOn = DateTime.UtcNow;

        LogEvent(dunningCase, DunningAction.WriteOff, true,
            $"{dunningCase.AmountOutstanding:0.00} written off — {reason}", userId);
    }

    private async Task CloseAsRecoveredAsync(DunningCase dunningCase, decimal amount, string detail, Guid userId)
    {
        dunningCase.Status = DunningCaseStatus.Recovered;
        dunningCase.ClosedOn = DateTime.UtcNow;
        dunningCase.AmountRecovered += amount;
        dunningCase.AmountOutstanding = 0;

        LogEvent(dunningCase, DunningAction.Retry, true, detail, userId, amount);

        // Lift the suspension the ladder imposed, if it did.
        var suspension = await db.Suspensions.ForTenant(tenant)
            .Include(s => s.Member)
            .FirstOrDefaultAsync(s => s.MemberId == dunningCase.MemberId
                                   && s.LiftedOn == null
                                   && s.Reason == SuspensionReason.UnpaidBalance);

        if (suspension is not null)
        {
            suspension.LiftedOn = DateTime.UtcNow;
            suspension.LiftedByUserId = userId;

            if (suspension.Member is not null) suspension.Member.Status = MemberStatus.Active;
        }

        var member = dunningCase.Member
            ?? await db.Members.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == dunningCase.MemberId);

        if (member?.Status == MemberStatus.PastDue) member.Status = MemberStatus.Active;
    }

    private void LogEvent(
        DunningCase dunningCase, DunningAction action, bool succeeded, string detail, Guid userId,
        decimal? collected = null)
    {
        db.DunningEvents.Add(new DunningEvent
        {
            DunningCaseId = dunningCase.Id,
            StepNumber = dunningCase.CurrentStep,
            Action = action,
            OccurredAt = DateTime.UtcNow,
            Succeeded = succeeded,
            Detail = detail,
            AmountCollected = collected,
            PerformedByUserId = userId == Guid.Empty ? null : userId,
        }.StampNew(tenant, userId));
    }

    private async Task<DunningPolicy?> ResolvePolicyAsync(FitnessClub? club)
    {
        if (club?.DefaultDunningPolicyId is not null)
        {
            var specific = await db.DunningPolicies.ForTenant(tenant)
                .Include(p => p.Steps.Where(s => !s.IsDeleted))
                .FirstOrDefaultAsync(p => p.Id == club.DefaultDunningPolicyId && p.IsActive);

            if (specific is not null) return specific;
        }

        return await db.DunningPolicies.ForTenant(tenant)
            .Include(p => p.Steps.Where(s => !s.IsDeleted))
            .FirstOrDefaultAsync(p => p.IsDefault && p.IsActive);
    }

    private async Task<DunningCaseDto> EnrichAsync(DunningCase c, DateTime now, int totalSteps)
    {
        var dto = FitnessMapper.ToDto(c, now, totalSteps);

        dto.ClubName = await db.Clubs.ForTenant(tenant)
            .Where(x => x.Id == c.ClubId).Select(x => x.Name).FirstOrDefaultAsync();

        if (c.InvoiceId is not null)
        {
            dto.InvoiceNumber = await db.Invoices.ForTenant(tenant)
                .Where(i => i.Id == c.InvoiceId).Select(i => i.InvoiceNumber).FirstOrDefaultAsync();
        }

        if (c.AssignedToStaffId is not null)
        {
            dto.AssignedToName = await db.Staff.ForTenant(tenant)
                .Where(s => s.Id == c.AssignedToStaffId)
                .Select(s => s.FirstName + " " + s.LastName)
                .FirstOrDefaultAsync();
        }

        dto.HasValidPaymentMethod = await db.PaymentMethods.ForTenant(tenant)
            .AnyAsync(p => p.MemberId == c.MemberId && p.IsActive && p.IsDefault);

        dto.AccessSuspended = await db.Suspensions.ForTenant(tenant)
            .AnyAsync(s => s.MemberId == c.MemberId && s.LiftedOn == null
                        && s.Reason == SuspensionReason.UnpaidBalance);

        var next = await db.DunningSteps.ForTenant(tenant)
            .Where(s => s.DunningPolicyId == c.DunningPolicyId && s.StepNumber > c.CurrentStep)
            .OrderBy(s => s.StepNumber)
            .FirstOrDefaultAsync();

        dto.NextAction = next is null ? "No further steps" : DescribeAction(next);

        return dto;
    }

    private static string DescribeAction(DunningStep step) => step.Action switch
    {
        DunningAction.Retry => "Retry the payment",
        DunningAction.SendEmail => "Send an email",
        DunningAction.SendSms => "Send a text",
        DunningAction.SendPush => "Send a push notification",
        DunningAction.RequestCardUpdate => "Ask for updated card details",
        DunningAction.CreateCallTask => "Raise a call task",
        DunningAction.AddLateFee => $"Add a {step.FeeAmount:0.00} late fee",
        DunningAction.SuspendAccess => "Suspend access",
        DunningAction.CancelAgreement => "Cancel the membership",
        DunningAction.WriteOff => "Write the balance off",
        DunningAction.ReferToCollections => "Refer to collections",
        _ => step.Action.ToString(),
    };

    private static string Describe(PaymentFailureReason reason) => reason switch
    {
        PaymentFailureReason.InsufficientFunds => "Insufficient funds",
        PaymentFailureReason.CardExpired => "Card expired",
        PaymentFailureReason.CardDeclined => "Card declined",
        PaymentFailureReason.MandateCancelled => "Direct debit cancelled",
        PaymentFailureReason.AccountClosed => "Bank account closed",
        PaymentFailureReason.Disputed => "Disputed by the member",
        PaymentFailureReason.TechnicalError => "Technical error",
        PaymentFailureReason.NoPaymentMethod => "No payment method on file",
        _ => "Other",
    };
}
