using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Application.Services.Interfaces;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// Payment plans, demands, the surcharge engine, receipts, allocation and the collections desk.
///
/// The engine the whole Development line of business turns on, and the one that has to be right to
/// the paisa. Three rules run through all of it:
///
/// 1. **The ledger is append-only.** A reversal writes a new row; nothing is edited in place.
/// 2. **Allocation is explainable.** Every allocation line records the rule that produced it and
///    whether a human overrode it, because this is the single most-argued number in the module.
/// 3. **Escrow is split at allocation**, not reconstructed at audit time.
/// </summary>
public partial class MoneyService(
    RealEstateDbContext db,
    IRealEstateTenant tenant,
    RealEstateNumbering numbering)
    : RealEstateServiceBase(db, tenant), IMoneyService
{
    // ═══ Templates & plans ═══════════════════════════════════════════════════

    public async Task<List<PaymentPlanTemplateDto>> GetTemplatesAsync(Guid? projectId, bool activeOnly)
    {
        var templates = await Db.PaymentPlanTemplates.ForCompany(Tenant)
            .WhereIf(projectId.HasValue, t => t.ProjectId == projectId)
            .WhereIf(activeOnly, t => t.IsActive)
            .Include(t => t.Lines)
            .OrderBy(t => t.Name).ThenByDescending(t => t.Version)
            .ToListAsync();

        var projects = await ProjectNamesAsync(templates.Select(t => (Guid?)t.ProjectId));
        var ids = templates.Select(t => t.Id).ToList();

        var usage = await Db.PaymentPlans.ForCompany(Tenant)
            .Where(p => p.PaymentPlanTemplateId != null && ids.Contains(p.PaymentPlanTemplateId!.Value))
            .GroupBy(p => p.PaymentPlanTemplateId!.Value)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Id, x => x.Count);

        var policies = await Db.SurchargePolicies.ForCompany(Tenant).ToDictionaryAsync(p => p.Id, p => p.Name);
        var milestones = await Db.ProjectMilestones.ForCompany(Tenant).ToDictionaryAsync(m => m.Id, m => m.Name);

        return templates.Select(t => MapTemplate(t, projects, usage, policies, milestones)).ToList();
    }

    public async Task<PaymentPlanTemplateDto?> GetTemplateAsync(Guid id)
    {
        var template = await Db.PaymentPlanTemplates.ForCompany(Tenant)
            .Include(t => t.Lines)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (template is null) return null;

        var projects = await ProjectNamesAsync([template.ProjectId]);
        var policies = await Db.SurchargePolicies.ForCompany(Tenant).ToDictionaryAsync(p => p.Id, p => p.Name);
        var milestones = await Db.ProjectMilestones.ForCompany(Tenant)
            .Where(m => m.ProjectId == template.ProjectId)
            .ToDictionaryAsync(m => m.Id, m => m.Name);

        return MapTemplate(template, projects, [], policies, milestones);
    }

    private static PaymentPlanTemplateDto MapTemplate(
        PaymentPlanTemplate t,
        Dictionary<Guid, string> projects,
        Dictionary<Guid, int> usage,
        Dictionary<Guid, string> policies,
        Dictionary<Guid, string> milestones)
    {
        var lines = t.Lines.OrderBy(l => l.SortOrder).Select(l => new PaymentPlanTemplateLineDto
        {
            Id = l.Id,
            Kind = l.Kind,
            Label = l.Label,
            SortOrder = l.SortOrder,
            Percent = l.Percent,
            FixedAmount = l.FixedAmount,
            Count = l.Count,
            Frequency = l.Frequency,
            StartOffsetDays = l.StartOffsetDays,
            StartOffsetMonths = l.StartOffsetMonths,
            ProjectMilestoneId = l.ProjectMilestoneId,
            MilestoneName = l.ProjectMilestoneId is null ? null : milestones.GetValueOrDefault(l.ProjectMilestoneId.Value),
            MilestoneCode = l.MilestoneCode,
            ChargeKind = l.ChargeKind,
            IsTaxable = l.IsTaxable,
            TaxPercent = l.TaxPercent,
        }).ToList();

        var totalPercent = lines.Sum(l => l.Percent * l.Count);
        var instalments = lines.Sum(l => l.Count);
        var months = lines.Max(l => l.StartOffsetMonths + MonthsFor(l.Frequency, l.Count));

        return new PaymentPlanTemplateDto
        {
            Id = t.Id,
            ProjectId = t.ProjectId,
            ProjectName = projects.GetValueOrDefault(t.ProjectId) ?? "—",
            Name = t.Name,
            Code = t.Code,
            Version = t.Version,
            IsActive = t.IsActive,
            IsDefault = t.IsDefault,
            EffectiveFrom = t.EffectiveFrom,
            EffectiveTo = t.EffectiveTo,
            SurchargePolicyId = t.SurchargePolicyId,
            SurchargePolicyName = t.SurchargePolicyId is null ? null : policies.GetValueOrDefault(t.SurchargePolicyId.Value),
            DunningPolicyId = t.DunningPolicyId,
            EarlyPaymentRebatePercentPerMonth = t.EarlyPaymentRebatePercentPerMonth,
            LumpSumDiscountPercent = t.LumpSumDiscountPercent,
            LumpSumWindowDays = t.LumpSumWindowDays,
            Note = t.Note,
            TotalPercent = totalPercent,
            IsBalanced = Math.Abs(totalPercent - 100m) < 0.01m,
            InstalmentCount = instalments,
            DurationMonths = months,
            UsageCount = usage.GetValueOrDefault(t.Id),
            Lines = lines,
        };
    }

    private static int MonthsFor(InstalmentFrequency frequency, int count) => frequency switch
    {
        InstalmentFrequency.Monthly => count,
        InstalmentFrequency.BiMonthly => count * 2,
        InstalmentFrequency.Quarterly => count * 3,
        InstalmentFrequency.HalfYearly => count * 6,
        InstalmentFrequency.Yearly => count * 12,
        _ => 0,
    };

    public async Task<PaymentPlanTemplateDto> SaveTemplateAsync(PaymentPlanTemplateDto dto, Guid userId)
    {
        var template = dto.Id != Guid.Empty
            ? await Db.PaymentPlanTemplates.ForCompany(Tenant).Include(t => t.Lines).FirstOrDefaultAsync(t => t.Id == dto.Id)
            : null;

        if (template is null)
        {
            template = new PaymentPlanTemplate { ProjectId = dto.ProjectId }.StampNew(Tenant, userId);
            Db.PaymentPlanTemplates.Add(template);
        }
        else
        {
            Db.PaymentPlanTemplateLines.RemoveRange(template.Lines);
            template.StampUpdated(userId);
        }

        template.Name = dto.Name;
        template.Code = dto.Code;
        template.Version = dto.Version <= 0 ? 1 : dto.Version;
        template.IsActive = dto.IsActive;
        template.IsDefault = dto.IsDefault;
        template.EffectiveFrom = dto.EffectiveFrom == default ? Today : dto.EffectiveFrom;
        template.EffectiveTo = dto.EffectiveTo;
        template.SurchargePolicyId = dto.SurchargePolicyId;
        template.DunningPolicyId = dto.DunningPolicyId;
        template.EarlyPaymentRebatePercentPerMonth = dto.EarlyPaymentRebatePercentPerMonth;
        template.LumpSumDiscountPercent = dto.LumpSumDiscountPercent;
        template.LumpSumWindowDays = dto.LumpSumWindowDays;
        template.Note = dto.Note;

        foreach (var line in dto.Lines.OrderBy(l => l.SortOrder))
        {
            template.Lines.Add(new PaymentPlanTemplateLine
            {
                Kind = line.Kind,
                Label = line.Label,
                SortOrder = line.SortOrder,
                Percent = line.Percent,
                FixedAmount = line.FixedAmount,
                Count = line.Count <= 0 ? 1 : line.Count,
                Frequency = line.Frequency,
                StartOffsetDays = line.StartOffsetDays,
                StartOffsetMonths = line.StartOffsetMonths,
                ProjectMilestoneId = line.ProjectMilestoneId,
                MilestoneCode = line.MilestoneCode,
                ChargeKind = line.ChargeKind,
                IsTaxable = line.IsTaxable,
                TaxPercent = line.TaxPercent,
            }.StampNew(Tenant, userId));
        }

        if (dto.IsDefault)
        {
            var others = await Db.PaymentPlanTemplates.ForCompany(Tenant)
                .Where(t => t.ProjectId == dto.ProjectId && t.Id != template.Id && t.IsDefault)
                .ToListAsync();
            foreach (var o in others) o.IsDefault = false;
        }

        await Db.SaveChangesAsync();
        return (await GetTemplateAsync(template.Id))!;
    }

    /// <summary>
    /// Expands a template (or a hand-built plan) into an actual schedule, without writing anything.
    ///
    /// Percentages are applied to the total consideration and the **last instalment absorbs the
    /// rounding**, so the schedule always sums to the price exactly. A plan that is a rupee short
    /// produces a customer who cannot clear their file.
    /// </summary>
    public async Task<PaymentPlanPreviewDto> PreviewPlanAsync(
        Guid? templateId, PaymentPlanCustomDto? custom, decimal totalConsideration, DateOnly startDate, Guid? projectId)
    {
        var currency = await CurrencyAsync();
        List<PaymentPlanTemplateLineDto> lines;
        string? name;
        decimal rebatePerMonth = 0m;

        if (custom is not null && custom.Lines.Count > 0)
        {
            lines = custom.Lines.OrderBy(l => l.SortOrder).ToList();
            name = custom.Name ?? "Custom plan";
            startDate = custom.StartDate == default ? startDate : custom.StartDate;
        }
        else
        {
            var template = templateId is null
                ? null
                : await Db.PaymentPlanTemplates.ForCompany(Tenant).Include(t => t.Lines)
                    .FirstOrDefaultAsync(t => t.Id == templateId);

            if (template is null)
                throw new InvalidOperationException("Pick a payment plan before continuing.");

            var milestones = await Db.ProjectMilestones.ForCompany(Tenant)
                .Where(m => m.ProjectId == template.ProjectId)
                .ToDictionaryAsync(m => m.Id, m => m.Name);

            lines = template.Lines.OrderBy(l => l.SortOrder).Select(l => new PaymentPlanTemplateLineDto
            {
                Kind = l.Kind,
                Label = l.Label,
                SortOrder = l.SortOrder,
                Percent = l.Percent,
                FixedAmount = l.FixedAmount,
                Count = l.Count,
                Frequency = l.Frequency,
                StartOffsetDays = l.StartOffsetDays,
                StartOffsetMonths = l.StartOffsetMonths,
                ProjectMilestoneId = l.ProjectMilestoneId,
                MilestoneName = l.ProjectMilestoneId is null ? null : milestones.GetValueOrDefault(l.ProjectMilestoneId.Value),
                ChargeKind = l.ChargeKind,
                IsTaxable = l.IsTaxable,
                TaxPercent = l.TaxPercent,
            }).ToList();

            name = template.Name;
            rebatePerMonth = template.EarlyPaymentRebatePercentPerMonth;
        }

        var instalments = ExpandLines(lines, totalConsideration, startDate);

        var preview = new PaymentPlanPreviewDto
        {
            TemplateName = name,
            StartDate = startDate,
            EndDate = instalments.Where(i => i.DueDate.HasValue).Max(i => i.DueDate),
            TotalAmount = RealEstateMapper.Money(instalments.Sum(i => i.TotalAmount)),
            CurrencyCode = currency,
            InstalmentCount = instalments.Count,
            DownPayment = instalments.Where(i => i.Kind is InstalmentKind.BookingAmount or InstalmentKind.ConfirmationAmount)
                                     .Sum(i => i.TotalAmount),
            PossessionBalance = instalments.Where(i => i.Kind == InstalmentKind.PossessionBalance).Sum(i => i.TotalAmount),
            Instalments = instalments,
            IsDeviation = custom is not null,
            DeviationNote = custom?.DeviationReason,
        };

        var periodic = instalments.Where(i => i.Kind == InstalmentKind.Periodic).ToList();
        preview.MonthlyAverage = periodic.Count == 0 ? 0m : RealEstateMapper.Money(periodic.Average(i => i.TotalAmount));
        preview.DurationMonths = preview.EndDate is null
            ? 0
            : ((preview.EndDate.Value.Year - startDate.Year) * 12) + preview.EndDate.Value.Month - startDate.Month;

        // What a customer paying everything today would save.
        preview.EarlyPaymentRebateAvailable = rebatePerMonth <= 0m
            ? 0m
            : RealEstateMapper.Money(instalments
                .Where(i => i.DueDate.HasValue)
                .Sum(i => i.TotalAmount * rebatePerMonth / 100m *
                          Math.Max(0, (i.DueDate!.Value.DayNumber - startDate.DayNumber) / 30m)));

        return preview;
    }

    private static List<InstalmentDto> ExpandLines(
        List<PaymentPlanTemplateLineDto> lines, decimal totalConsideration, DateOnly startDate)
    {
        var result = new List<InstalmentDto>();
        var sequence = 0;

        foreach (var line in lines)
        {
            var perInstalment = line.FixedAmount > 0
                ? line.FixedAmount
                : RealEstateMapper.Money(totalConsideration * line.Percent / 100m);

            for (var i = 0; i < Math.Max(1, line.Count); i++)
            {
                sequence++;

                DateOnly? due = line.Kind == InstalmentKind.MilestoneLinked
                    ? null
                    : startDate
                        .AddMonths(line.StartOffsetMonths + OffsetMonths(line.Frequency, i))
                        .AddDays(line.StartOffsetDays);

                var tax = line.IsTaxable ? RealEstateMapper.Money(perInstalment * line.TaxPercent / 100m) : 0m;

                result.Add(new InstalmentDto
                {
                    SequenceNumber = sequence,
                    Kind = line.Kind,
                    Label = line.Count > 1 ? $"{line.Label} {i + 1}/{line.Count}" : line.Label,
                    DueDate = due,
                    ProjectMilestoneId = line.ProjectMilestoneId,
                    MilestoneName = line.MilestoneName,
                    Amount = perInstalment,
                    TaxAmount = tax,
                    TotalAmount = perInstalment + tax,
                    Balance = perInstalment + tax,
                    Status = InstalmentStatus.NotDue,
                    ChargeKind = line.ChargeKind,
                });
            }
        }

        // The plan must sum to the price exactly. Any rounding drift lands on the last principal
        // instalment rather than being spread, so no line ends in a fraction nobody can pay.
        var principal = result.Where(r => r.ChargeKind is null).ToList();
        if (principal.Count > 0)
        {
            var drift = RealEstateMapper.Money(totalConsideration - principal.Sum(r => r.Amount));
            if (drift != 0m)
            {
                var last = principal[^1];
                last.Amount += drift;
                last.TotalAmount += drift;
                last.Balance += drift;
            }
        }

        return result;
    }

    private static int OffsetMonths(InstalmentFrequency frequency, int index) => frequency switch
    {
        InstalmentFrequency.Monthly => index,
        InstalmentFrequency.BiMonthly => index * 2,
        InstalmentFrequency.Quarterly => index * 3,
        InstalmentFrequency.HalfYearly => index * 6,
        InstalmentFrequency.Yearly => index * 12,
        _ => 0,
    };

    public async Task<PaymentPlanDto?> GetPlanAsync(Guid bookingId)
    {
        var plan = await Db.PaymentPlans.ForCompany(Tenant)
            .Include(p => p.Instalments)
            .Where(p => p.BookingId == bookingId && p.IsCurrent)
            .FirstOrDefaultAsync();

        if (plan is null) return null;

        var booking = await Db.Bookings.ForCompany(Tenant).FirstOrDefaultAsync(b => b.Id == bookingId);
        var policy = plan.SurchargePolicyId is null
            ? null
            : await Db.SurchargePolicies.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == plan.SurchargePolicyId);

        var milestoneIds = plan.Instalments.Where(i => i.ProjectMilestoneId.HasValue)
            .Select(i => i.ProjectMilestoneId!.Value).Distinct().ToList();

        var milestones = milestoneIds.Count == 0
            ? []
            : await Db.ProjectMilestones.ForCompany(Tenant)
                .Where(m => milestoneIds.Contains(m.Id))
                .ToDictionaryAsync(m => m.Id, m => new { m.Name, m.Status });

        var demandNumbers = await Db.Demands.ForCompany(Tenant)
            .Where(d => d.BookingId == bookingId)
            .ToDictionaryAsync(d => d.Id, d => d.DemandNumber);

        return new PaymentPlanDto
        {
            Id = plan.Id,
            BookingId = plan.BookingId,
            BookingReference = booking?.Reference,
            Reference = plan.Reference,
            Version = plan.Version,
            IsCurrent = plan.IsCurrent,
            IsCustom = plan.IsCustom,
            IsRestructure = plan.IsRestructure,
            RevisionReason = plan.RevisionReason,
            SupersedesPlanId = plan.SupersedesPlanId,
            StartDate = plan.StartDate,
            EndDate = plan.EndDate,
            TotalAmount = plan.TotalAmount,
            TotalDemanded = plan.TotalDemanded,
            TotalPaid = plan.TotalPaid,
            Outstanding = plan.Outstanding,
            CurrencyCode = booking?.CurrencyCode ?? await CurrencyAsync(),
            SurchargePolicyName = policy?.Name,
            EarlyPaymentRebateEarned = plan.EarlyPaymentRebateEarned,
            RestructureFee = plan.RestructureFee,
            Instalments = plan.Instalments.OrderBy(i => i.SequenceNumber).Select(i => new InstalmentDto
            {
                Id = i.Id,
                SequenceNumber = i.SequenceNumber,
                Kind = i.Kind,
                Label = i.Label,
                DueDate = i.DueDate,
                ProjectMilestoneId = i.ProjectMilestoneId,
                MilestoneName = i.ProjectMilestoneId is null ? null : milestones.GetValueOrDefault(i.ProjectMilestoneId.Value)?.Name,
                MilestoneStatus = i.ProjectMilestoneId is null ? null : milestones.GetValueOrDefault(i.ProjectMilestoneId.Value)?.Status,
                Amount = i.Amount,
                TaxAmount = i.TaxAmount,
                TotalAmount = i.TotalAmount,
                PaidAmount = i.PaidAmount,
                WaivedAmount = i.WaivedAmount,
                Balance = i.Balance,
                Status = i.Status,
                SurchargeAccrued = i.SurchargeAccrued,
                SurchargePaid = i.SurchargePaid,
                SurchargeWaived = i.SurchargeWaived,
                SurchargeOutstanding = i.SurchargeAccrued - i.SurchargePaid - i.SurchargeWaived,
                DaysOverdue = i.DaysOverdue,
                DemandId = i.DemandId,
                DemandNumber = i.DemandId is null ? null : demandNumbers.GetValueOrDefault(i.DemandId.Value),
                FirstPaidOn = i.FirstPaidOn,
                SettledOn = i.SettledOn,
                IsOnHold = i.IsOnHold,
                HoldReason = i.HoldReason,
                ChargeKind = i.ChargeKind,
            }).ToList(),
        };
    }

    /// <summary>
    /// Rebuilds a defaulter's remaining schedule over a longer horizon.
    ///
    /// The old plan is kept and marked superseded rather than edited, so the original terms survive
    /// the restructure — which is exactly what a dispute six months later turns on.
    /// </summary>
    public async Task<PlanRestructureResult> RestructureInternalAsync(PlanRestructureDto dto, Guid userId)
    {
        var booking = await Db.Bookings.ForCompany(Tenant).FirstOrDefaultAsync(b => b.Id == dto.BookingId)
            ?? throw new InvalidOperationException("That booking does not exist.");

        var current = await Db.PaymentPlans.ForCompany(Tenant)
            .Include(p => p.Instalments)
            .FirstOrDefaultAsync(p => p.BookingId == dto.BookingId && p.IsCurrent)
            ?? throw new InvalidOperationException("This booking has no live payment plan.");

        var unpaid = current.Instalments
            .Where(i => i.Status != InstalmentStatus.Paid && i.Status != InstalmentStatus.Cancelled)
            .ToList();

        var outstanding = unpaid.Sum(i => i.Balance);
        var surcharge = unpaid.Sum(i => i.SurchargeAccrued - i.SurchargePaid - i.SurchargeWaived);

        var toSchedule = outstanding
            + (dto.CapitaliseSurcharge ? surcharge : 0m)
            + dto.RestructureFee
            - (dto.DownPaymentRequired ?? 0m);

        if (toSchedule <= 0m)
            throw new InvalidOperationException("There is nothing left to reschedule on this booking.");

        var count = dto.NewInstalmentCount <= 0 ? 12 : dto.NewInstalmentCount;
        var per = RealEstateMapper.Money(toSchedule / count);
        var start = dto.NewStartDate == default ? Today.AddMonths(1) : dto.NewStartDate;

        var newInstalments = new List<InstalmentDto>();

        if (dto.DownPaymentRequired is > 0m)
        {
            newInstalments.Add(new InstalmentDto
            {
                SequenceNumber = 1,
                Kind = InstalmentKind.BookingAmount,
                Label = "Restructure down payment",
                DueDate = start,
                Amount = dto.DownPaymentRequired.Value,
                TotalAmount = dto.DownPaymentRequired.Value,
                Balance = dto.DownPaymentRequired.Value,
                Status = InstalmentStatus.Due,
            });
        }

        for (var i = 0; i < count; i++)
        {
            newInstalments.Add(new InstalmentDto
            {
                SequenceNumber = newInstalments.Count + 1,
                Kind = InstalmentKind.Periodic,
                Label = $"Restructured instalment {i + 1}/{count}",
                DueDate = start.AddMonths(OffsetMonths(dto.Frequency, i) + (dto.DownPaymentRequired is > 0m ? 1 : 0)),
                Amount = per,
                TotalAmount = per,
                Balance = per,
                Status = InstalmentStatus.NotDue,
            });
        }

        // Rounding lands on the last line so the restructure clears the debt exactly.
        var drift = RealEstateMapper.Money(toSchedule - newInstalments.Where(i => i.Kind == InstalmentKind.Periodic).Sum(i => i.Amount));
        if (drift != 0m && newInstalments.Count > 0)
        {
            var last = newInstalments[^1];
            last.Amount += drift;
            last.TotalAmount += drift;
            last.Balance += drift;
        }

        var result = new PlanRestructureResult
        {
            BookingReference = booking.Reference,
            OutstandingBefore = outstanding,
            SurchargeBefore = surcharge,
            AmountRescheduled = toSchedule,
            Instalments = newInstalments,
        };

        if (dto.DryRun) return result;

        // Approval first: a restructure changes the contract's cash profile.
        var approval = await RaiseApprovalAsync(
            "PlanRestructure", booking.Id, booking.Reference, toSchedule,
            $"Reschedule {booking.Reference} over {count} instalments", userId, booking.ProjectId,
            reasonCodeId: dto.ReasonCodeId, note: dto.Note);

        foreach (var old in unpaid)
        {
            old.Status = InstalmentStatus.Restructured;
            old.StampUpdated(userId);
        }

        current.IsCurrent = false;
        current.StampUpdated(userId);

        var plan = new PaymentPlan
        {
            BookingId = booking.Id,
            PaymentPlanTemplateId = current.PaymentPlanTemplateId,
            Reference = $"{current.Reference}-R{current.Version + 1}",
            Version = current.Version + 1,
            IsCurrent = true,
            IsCustom = true,
            IsRestructure = true,
            SupersedesPlanId = current.Id,
            RevisionReason = "Restructure",
            StartDate = start,
            EndDate = newInstalments.Where(i => i.DueDate.HasValue).Max(i => i.DueDate),
            TotalAmount = toSchedule,
            Outstanding = toSchedule,
            SurchargePolicyId = current.SurchargePolicyId,
            DunningPolicyId = current.DunningPolicyId,
            RestructureFee = dto.RestructureFee,
            RestructureApprovalRequestId = approval?.Id,
        }.StampNew(Tenant, userId);

        foreach (var i in newInstalments)
        {
            plan.Instalments.Add(new Instalment
            {
                BookingId = booking.Id,
                SequenceNumber = i.SequenceNumber,
                Kind = i.Kind,
                Label = i.Label,
                DueDate = i.DueDate,
                Amount = i.Amount,
                TotalAmount = i.TotalAmount,
                Balance = i.Balance,
                Status = i.DueDate <= Today ? InstalmentStatus.Due : InstalmentStatus.NotDue,
            }.StampNew(Tenant, userId));
        }

        Db.PaymentPlans.Add(plan);

        booking.PaymentPlanId = plan.Id;
        booking.StampUpdated(userId);

        if (dto.ReasonCodeId.HasValue)
        {
            await WriteAuditNoteAsync("Booking", booking.Id, "PlanRestructure", dto.ReasonCodeId.Value, userId,
                before: $"{outstanding:N0} over {unpaid.Count} instalments",
                after: $"{toSchedule:N0} over {count} instalments",
                amountImpact: dto.RestructureFee, note: dto.Note,
                approvalRequestId: approval?.Id, highRisk: true, entityReference: booking.Reference);
        }

        await Db.SaveChangesAsync();
        await RecalculateBookingTotalsAsync(booking.Id, userId);

        result.PlanId = plan.Id;
        return result;
    }

    public async Task<PaymentPlanDto> RestructureAsync(PlanRestructureDto dto, Guid userId)
    {
        var result = await RestructureInternalAsync(dto, userId);
        if (dto.DryRun)
        {
            return new PaymentPlanDto
            {
                BookingId = dto.BookingId,
                BookingReference = result.BookingReference,
                Reference = "(preview)",
                StartDate = dto.NewStartDate,
                TotalAmount = result.AmountRescheduled,
                Outstanding = result.AmountRescheduled,
                IsRestructure = true,
                Instalments = result.Instalments,
                CurrencyCode = await CurrencyAsync(),
            };
        }

        return (await GetPlanAsync(dto.BookingId))!;
    }

    public class PlanRestructureResult
    {
        public Guid? PlanId { get; set; }
        public string BookingReference { get; set; } = string.Empty;
        public decimal OutstandingBefore { get; set; }
        public decimal SurchargeBefore { get; set; }
        public decimal AmountRescheduled { get; set; }
        public List<InstalmentDto> Instalments { get; set; } = [];
    }

    // ═══ Surcharge ═══════════════════════════════════════════════════════════

    public async Task<List<SurchargePolicyDto>> GetSurchargePoliciesAsync(Guid? projectId)
    {
        var policies = await Db.SurchargePolicies.ForCompany(Tenant)
            .WhereIf(projectId.HasValue, p => p.ProjectId == projectId || p.ProjectId == null)
            .OrderBy(p => p.Name)
            .ToListAsync();

        var projects = await ProjectNamesAsync(policies.Select(p => p.ProjectId));

        return policies.Select(p => new SurchargePolicyDto
        {
            Id = p.Id,
            Name = p.Name,
            ProjectId = p.ProjectId,
            ProjectName = p.ProjectId is null ? null : projects.GetValueOrDefault(p.ProjectId.Value),
            Basis = p.Basis,
            Rate = p.Rate,
            FlatAmount = p.FlatAmount,
            GraceDays = p.GraceDays,
            IsCompounding = p.IsCompounding,
            CapPercent = p.CapPercent,
            CapAmount = p.CapAmount,
            StopAfterDays = p.StopAfterDays,
            WaiverRequiresApproval = p.WaiverRequiresApproval,
            IsActive = p.IsActive,
            ExampleOn100kFor30Days = ComputeSurcharge(p, 100_000m, 30),
        }).ToList();
    }

    public async Task<SurchargePolicyDto> SaveSurchargePolicyAsync(SurchargePolicyDto dto, Guid userId)
    {
        var policy = dto.Id != Guid.Empty
            ? await Db.SurchargePolicies.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == dto.Id)
            : null;

        if (policy is null)
        {
            policy = new SurchargePolicy().StampNew(Tenant, userId);
            Db.SurchargePolicies.Add(policy);
        }
        else policy.StampUpdated(userId);

        policy.Name = dto.Name;
        policy.ProjectId = dto.ProjectId;
        policy.Basis = dto.Basis;
        policy.Rate = dto.Rate;
        policy.FlatAmount = dto.FlatAmount;
        policy.GraceDays = dto.GraceDays;
        policy.IsCompounding = dto.IsCompounding;
        policy.CapPercent = dto.CapPercent;
        policy.CapAmount = dto.CapAmount;
        policy.StopAfterDays = dto.StopAfterDays;
        policy.WaiverRequiresApproval = dto.WaiverRequiresApproval;
        policy.IsActive = dto.IsActive;

        await Db.SaveChangesAsync();
        dto.Id = policy.Id;
        dto.ExampleOn100kFor30Days = ComputeSurcharge(policy, 100_000m, 30);
        return dto;
    }

    /// <summary>
    /// The late-payment surcharge for one overdue amount over a number of days.
    ///
    /// Written as one pure function on purpose: it is quoted on demands, on statements, on the
    /// collection desk and in the settings preview, and all four have to agree exactly.
    /// </summary>
    private static decimal ComputeSurcharge(SurchargePolicy policy, decimal overdue, int daysOverdue)
    {
        var chargeableDays = daysOverdue - policy.GraceDays;
        if (chargeableDays <= 0 || overdue <= 0m) return 0m;

        if (policy.StopAfterDays is int stop && chargeableDays > stop) chargeableDays = stop;

        decimal amount;

        switch (policy.Basis)
        {
            case SurchargeBasis.PerDayOnOverdue:
                amount = policy.IsCompounding
                    ? overdue * ((decimal)Math.Pow(1 + (double)(policy.Rate / 100m), chargeableDays) - 1m)
                    : overdue * policy.Rate / 100m * chargeableDays;
                break;

            case SurchargeBasis.PerMonthOnOverdue:
            case SurchargeBasis.PerMonthOnOutstanding:
                // Part months count whole, which is the market convention and what the terms say.
                var months = (int)Math.Ceiling(chargeableDays / 30m);
                amount = policy.IsCompounding
                    ? overdue * ((decimal)Math.Pow(1 + (double)(policy.Rate / 100m), months) - 1m)
                    : overdue * policy.Rate / 100m * months;
                break;

            case SurchargeBasis.FlatPerInstalment:
                amount = policy.FlatAmount;
                break;

            default:
                amount = 0m;
                break;
        }

        if (policy.CapPercent > 0m) amount = Math.Min(amount, overdue * policy.CapPercent / 100m);
        if (policy.CapAmount > 0m) amount = Math.Min(amount, policy.CapAmount);

        return RealEstateMapper.Money(amount);
    }

    /// <summary>
    /// The nightly accrual. Recomputes each overdue instalment's surcharge from first principles
    /// and writes the delta, so a policy change or a corrected due date self-heals rather than
    /// leaving a wrong number that nobody can explain.
    /// </summary>
    public async Task<int> AccrueSurchargeAsync(DateOnly asOf)
    {
        var overdue = await (
            from i in Db.Instalments.ForCompany(Tenant)
            join p in Db.PaymentPlans.ForCompany(Tenant) on i.PaymentPlanId equals p.Id
            where p.IsCurrent
                && !i.IsOnHold
                && i.DueDate != null
                && i.DueDate < asOf
                && i.Balance > 0
                && i.Status != InstalmentStatus.Cancelled
                && i.Status != InstalmentStatus.Waived
                && i.Status != InstalmentStatus.Restructured
            select new { Instalment = i, Plan = p })
            .ToListAsync();

        if (overdue.Count == 0) return 0;

        var policyIds = overdue.Where(x => x.Plan.SurchargePolicyId.HasValue)
            .Select(x => x.Plan.SurchargePolicyId!.Value).Distinct().ToList();

        var policies = policyIds.Count == 0
            ? []
            : await Db.SurchargePolicies.ForCompany(Tenant)
                .Where(p => policyIds.Contains(p.Id) && p.IsActive)
                .ToDictionaryAsync(p => p.Id, p => p);

        var touched = 0;

        foreach (var row in overdue)
        {
            if (row.Plan.SurchargePolicyId is null) continue;
            if (!policies.TryGetValue(row.Plan.SurchargePolicyId.Value, out var policy)) continue;

            var instalment = row.Instalment;
            var days = RealEstateMapper.DaysOverdue(instalment.DueDate, asOf);
            var expected = ComputeSurcharge(policy, instalment.Balance, days);
            var delta = RealEstateMapper.Money(expected - instalment.SurchargeAccrued);

            instalment.DaysOverdue = days;
            if (instalment.Status is InstalmentStatus.Due or InstalmentStatus.NotDue or InstalmentStatus.PartiallyPaid)
                instalment.Status = InstalmentStatus.Overdue;

            if (delta == 0m)
            {
                instalment.SurchargeAccruedUpTo = asOf;
                continue;
            }

            instalment.SurchargeAccrued = expected;
            instalment.SurchargeAccruedUpTo = asOf;

            Db.SurchargeAccruals.Add(new SurchargeAccrual
            {
                InstalmentId = instalment.Id,
                BookingId = instalment.BookingId,
                SurchargePolicyId = policy.Id,
                AccrualDate = asOf,
                OverdueBase = instalment.Balance,
                RateApplied = policy.Rate,
                Amount = delta,
                CumulativeAmount = expected,
                DaysOverdue = days,
                CapReached = (policy.CapPercent > 0m && expected >= instalment.Balance * policy.CapPercent / 100m)
                             || (policy.CapAmount > 0m && expected >= policy.CapAmount),
            }.StampNew(Tenant));

            Db.CustomerLedgerEntries.Add(new CustomerLedgerEntry
            {
                PartyId = Guid.Empty,
                BookingId = instalment.BookingId,
                EntryDate = asOf,
                Kind = LedgerEntryKind.Surcharge,
                Description = $"Late payment surcharge — {instalment.Label}",
                DebitAmount = delta,
                SourceInstalmentId = instalment.Id,
            }.StampNew(Tenant));

            touched++;
        }

        await Db.SaveChangesAsync();

        foreach (var bookingId in overdue.Select(o => o.Instalment.BookingId).Distinct())
            await RecalculateBookingTotalsAsync(bookingId, Guid.Empty);

        await Db.SaveChangesAsync();
        return touched;
    }

    public async Task<SurchargeWaiverDto> RequestWaiverAsync(SurchargeWaiverRequestDto dto, Guid userId)
    {
        var booking = await Db.Bookings.ForCompany(Tenant).FirstOrDefaultAsync(b => b.Id == dto.BookingId)
            ?? throw new InvalidOperationException("That booking does not exist.");

        if (dto.Amount <= 0m)
            throw new InvalidOperationException("Enter the amount to be waived.");

        var waiver = new SurchargeWaiver
        {
            BookingId = booking.Id,
            InstalmentId = dto.InstalmentId,
            Reference = await numbering.NextMasterCodeAsync(Db.SurchargeWaivers, "WVR"),
            RequestedAmount = dto.Amount,
            RequestedByUserId = userId,
            RequestedOn = Today,
            ReasonCodeId = dto.ReasonCodeId,
            Note = dto.Note,
            Outcome = ApprovalOutcome.Pending,
        }.StampNew(Tenant, userId);

        var approval = await RaiseApprovalAsync(
            "SurchargeWaiver", waiver.Id, waiver.Reference, dto.Amount,
            $"Waive {dto.Amount:N0} surcharge on {booking.Reference}", userId, booking.ProjectId,
            reasonCodeId: dto.ReasonCodeId, note: dto.Note);

        waiver.ApprovalRequestId = approval?.Id;

        // No approval matrix configured for waivers means it is allowed outright — but it is still
        // recorded as a reason-coded override, because quiet waiving is how revenue leaks.
        if (approval is null)
        {
            waiver.Outcome = ApprovalOutcome.AutoApproved;
            waiver.ApprovedAmount = dto.Amount;
            waiver.ApprovedByUserId = userId;
            waiver.ApprovedAt = DateTime.UtcNow;
        }

        Db.SurchargeWaivers.Add(waiver);
        await Db.SaveChangesAsync();

        if (waiver.Outcome == ApprovalOutcome.AutoApproved)
            await ApplyWaiverAsync(waiver, userId);

        return (await GetWaiverAsync(waiver.Id))!;
    }

    public async Task<SurchargeWaiverDto> DecideWaiverAsync(
        Guid waiverId, ApprovalOutcome outcome, decimal? approvedAmount, string? comment, Guid userId)
    {
        var waiver = await Db.SurchargeWaivers.ForCompany(Tenant).FirstOrDefaultAsync(w => w.Id == waiverId)
            ?? throw new InvalidOperationException("That waiver request does not exist.");

        if (waiver.Outcome != ApprovalOutcome.Pending)
            throw new InvalidOperationException("This waiver has already been decided.");

        waiver.Outcome = outcome;
        waiver.ApprovedAmount = outcome == ApprovalOutcome.Approved
            ? approvedAmount ?? waiver.RequestedAmount
            : 0m;
        waiver.ApprovedByUserId = userId;
        waiver.ApprovedAt = DateTime.UtcNow;
        waiver.Note = string.IsNullOrWhiteSpace(comment) ? waiver.Note : $"{waiver.Note}\n{comment}".Trim();
        waiver.StampUpdated(userId);

        await Db.SaveChangesAsync();

        if (outcome == ApprovalOutcome.Approved) await ApplyWaiverAsync(waiver, userId);

        return (await GetWaiverAsync(waiverId))!;
    }

    private async Task ApplyWaiverAsync(SurchargeWaiver waiver, Guid userId)
    {
        var remaining = waiver.ApprovedAmount;
        if (remaining <= 0m) return;

        var instalments = await Db.Instalments.ForCompany(Tenant)
            .Where(i => i.BookingId == waiver.BookingId)
            .WhereIf(waiver.InstalmentId.HasValue, i => i.Id == waiver.InstalmentId)
            .Where(i => i.SurchargeAccrued > i.SurchargePaid + i.SurchargeWaived)
            .OrderBy(i => i.DueDate)
            .ToListAsync();

        foreach (var instalment in instalments)
        {
            if (remaining <= 0m) break;

            var outstanding = instalment.SurchargeAccrued - instalment.SurchargePaid - instalment.SurchargeWaived;
            var applied = Math.Min(outstanding, remaining);

            instalment.SurchargeWaived += applied;
            instalment.StampUpdated(userId);
            remaining -= applied;
        }

        Db.CustomerLedgerEntries.Add(new CustomerLedgerEntry
        {
            PartyId = Guid.Empty,
            BookingId = waiver.BookingId,
            EntryDate = Today,
            Kind = LedgerEntryKind.SurchargeWaiver,
            Description = $"Surcharge waived — {waiver.Reference}",
            CreditAmount = waiver.ApprovedAmount - remaining,
        }.StampNew(Tenant, userId));

        await WriteAuditNoteAsync("Booking", waiver.BookingId, "SurchargeWaiver", waiver.ReasonCodeId, userId,
            after: $"{waiver.ApprovedAmount:N0} waived", amountImpact: waiver.ApprovedAmount,
            note: waiver.Note, approvalRequestId: waiver.ApprovalRequestId, highRisk: true);

        await Db.SaveChangesAsync();
        await RecalculateBookingTotalsAsync(waiver.BookingId, userId);
        await Db.SaveChangesAsync();
    }

    private async Task<SurchargeWaiverDto?> GetWaiverAsync(Guid id)
    {
        var w = await Db.SurchargeWaivers.ForCompany(Tenant).FirstOrDefaultAsync(x => x.Id == id);
        if (w is null) return null;

        var booking = await Db.Bookings.ForCompany(Tenant).FirstOrDefaultAsync(b => b.Id == w.BookingId);
        var names = booking is null ? [] : await PartyNamesAsync([booking.PrimaryApplicantPartyId]);
        var reasons = await ReasonLabelsAsync([w.ReasonCodeId]);
        var instalment = w.InstalmentId is null
            ? null
            : await Db.Instalments.ForCompany(Tenant).FirstOrDefaultAsync(i => i.Id == w.InstalmentId);

        return new SurchargeWaiverDto
        {
            Id = w.Id,
            Reference = w.Reference,
            BookingId = w.BookingId,
            BookingReference = booking?.Reference ?? "—",
            ApplicantName = booking is null ? "—" : names.GetValueOrDefault(booking.PrimaryApplicantPartyId, "—"),
            InstalmentId = w.InstalmentId,
            InstalmentLabel = instalment?.Label,
            RequestedAmount = w.RequestedAmount,
            ApprovedAmount = w.ApprovedAmount,
            RequestedByName = "—",
            RequestedOn = w.RequestedOn,
            ReasonLabel = reasons.GetValueOrDefault(w.ReasonCodeId, "—"),
            Note = w.Note,
            Outcome = w.Outcome,
            ApprovedAt = w.ApprovedAt,
        };
    }

    public async Task<PaginatedResponse<SurchargeWaiverDto>> GetWaiversAsync(ListQueryDto query, ApprovalOutcome? outcome)
    {
        var q = Db.SurchargeWaivers.ForCompany(Tenant)
            .WhereIf(outcome.HasValue, w => w.Outcome == outcome)
            .OrderByDescending(w => w.RequestedOn);

        return await PageAsync(q, query, async rows =>
        {
            var result = new List<SurchargeWaiverDto>();
            foreach (var r in rows)
            {
                var dto = await GetWaiverAsync(r.Id);
                if (dto is not null) result.Add(dto);
            }
            return result;
        });
    }

    /// <summary>
    /// Rolls a booking's derived totals up from its instalments and receipts.
    ///
    /// Stored rather than computed on read because the collection desk, the dashboard and the
    /// inventory board all sort on them, and summing a decade of ledger rows on every page load
    /// is how a system becomes unusable at year three.
    /// </summary>
    private async Task RecalculateBookingTotalsAsync(Guid bookingId, Guid userId)
    {
        var booking = await Db.Bookings.ForCompany(Tenant).FirstOrDefaultAsync(b => b.Id == bookingId);
        if (booking is null) return;

        var instalments = await Db.Instalments.ForCompany(Tenant)
            .Where(i => i.BookingId == bookingId && i.Status != InstalmentStatus.Restructured && i.Status != InstalmentStatus.Cancelled)
            .ToListAsync();

        var today = Today;

        booking.TotalDemanded = instalments.Where(i => i.DemandId != null).Sum(i => i.TotalAmount);
        booking.TotalPaid = instalments.Sum(i => i.PaidAmount);
        booking.TotalSurcharge = instalments.Sum(i => i.SurchargeAccrued);
        booking.TotalWaived = instalments.Sum(i => i.WaivedAmount + i.SurchargeWaived);

        var surchargeOutstanding = instalments.Sum(i => i.SurchargeAccrued - i.SurchargePaid - i.SurchargeWaived);
        booking.Outstanding = RealEstateMapper.Money(instalments.Sum(i => i.Balance) + surchargeOutstanding);

        var overdue = instalments.Where(i => i.DueDate != null && i.DueDate < today && i.Balance > 0).ToList();
        booking.OverdueAmount = RealEstateMapper.Money(overdue.Sum(i => i.Balance));
        booking.DaysOverdue = overdue.Count == 0 ? 0 : overdue.Max(i => RealEstateMapper.DaysOverdue(i.DueDate, today));

        booking.CollectionPercent = RealEstateMapper.Percent(booking.TotalPaid, booking.TotalConsideration);

        var next = instalments
            .Where(i => i.Balance > 0 && i.DueDate != null)
            .OrderBy(i => i.DueDate)
            .FirstOrDefault();

        booking.NextDueDate = next?.DueDate;
        booking.NextDueAmount = next?.Balance ?? 0m;

        if (booking.Status == BookingStatus.Confirmed && booking.DaysOverdue > 0)
            booking.Status = BookingStatus.Defaulting;
        else if (booking.Status == BookingStatus.Defaulting && booking.DaysOverdue == 0)
            booking.Status = BookingStatus.Confirmed;

        if (userId != Guid.Empty) booking.StampUpdated(userId);

        var plan = await Db.PaymentPlans.ForCompany(Tenant)
            .FirstOrDefaultAsync(p => p.BookingId == bookingId && p.IsCurrent);

        if (plan is not null)
        {
            plan.TotalDemanded = booking.TotalDemanded;
            plan.TotalPaid = booking.TotalPaid;
            plan.Outstanding = booking.Outstanding;
        }
    }

    // The remaining members are implemented in MoneyService.Collections.cs — the file is split
    // because demands, receipts and dunning are three distinct engines and one 3,000-line class
    // is not a thing anybody can review.
}
