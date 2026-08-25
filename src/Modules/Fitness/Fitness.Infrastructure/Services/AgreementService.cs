using Fitness.Application.DTOs;
using Fitness.Application.Services.Interfaces;
using Fitness.Domain.Entities;
using Fitness.Domain.Enums;
using Fitness.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;

namespace Fitness.Infrastructure.Services;

/// <summary>
/// Agreements, and the four things that happen to them: they get signed, changed, paused, and
/// eventually ended.
///
/// Two design decisions run through it. First, **commercial terms are copied onto the agreement
/// at signing**, never read through to the plan — a price rise must not reach back into contracts
/// already made. Second, **every change previews before it commits**: a freeze, an upgrade and a
/// cancellation all have a `Preview` method that returns the arithmetic and the dates, because a
/// proration nobody was warned about is the most common billing complaint in this industry.
/// </summary>
public class AgreementService(
    FitnessDbContext db,
    IFitnessTenant tenant,
    FitnessNumbering numbering,
    IBillingService billing) : IAgreementService
{
    public async Task<PaginatedResponse<AgreementSummaryDto>> ListAsync(
        Guid? clubId, Guid? memberId, AgreementStatus? status, Guid? planId,
        DateTime? endingBefore, PaginationParams pagination)
    {
        var now = DateTime.UtcNow;

        var query = db.Agreements.ForTenant(tenant)
            .Include(a => a.Member)
            .Include(a => a.Plan)
            .Include(a => a.Freezes.Where(f => !f.IsDeleted && !f.IsReleased))
            .WhereIf(clubId is not null, a => a.ClubId == clubId)
            .WhereIf(memberId is not null, a => a.MemberId == memberId)
            .WhereIf(status is not null, a => a.Status == status)
            .WhereIf(planId is not null, a => a.PlanId == planId)
            .WhereIf(endingBefore is not null, a => a.EndsOn != null && a.EndsOn <= endingBefore);

        var total = await query.CountAsync();

        var page = await query
            .OrderByDescending(a => a.StartsOn)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .ToListAsync();

        var clubNames = await db.Clubs.ForTenant(tenant)
            .Select(c => new { c.Id, c.Name })
            .ToDictionaryAsync(c => c.Id, c => c.Name);

        var items = page.Select(a =>
        {
            var dto = FitnessMapper.ToSummary(a, now);
            dto.ClubName = clubNames.GetValueOrDefault(a.ClubId);
            return dto;
        }).ToList();

        return PaginatedResponse<AgreementSummaryDto>.Ok(
                   items,
                   total,
                   pagination.PageNumber,
                   pagination.PageSize);
    }

    public async Task<AgreementDetailDto?> GetAsync(Guid agreementId)
    {
        var now = DateTime.UtcNow;

        var agreement = await db.Agreements.ForTenant(tenant)
            .Include(a => a.Member)
            .Include(a => a.Plan).ThenInclude(p => p!.Entitlements).ThenInclude(e => e.TimeBands)
            .Include(a => a.Amendments.Where(m => !m.IsDeleted))
            .Include(a => a.Freezes.Where(f => !f.IsDeleted))
            .FirstOrDefaultAsync(a => a.Id == agreementId);

        if (agreement is null) return null;

        var dto = FitnessMapper.ToDetail(agreement, now);

        dto.ClubName = await db.Clubs.ForTenant(tenant)
            .Where(c => c.Id == agreement.ClubId).Select(c => c.Name).FirstOrDefaultAsync();

        dto.UpcomingCharges = [.. (await db.BillingSchedules.ForTenant(tenant)
            .Where(s => s.AgreementId == agreementId && !s.IsBilled && s.DueOn >= now.Date)
            .OrderBy(s => s.DueOn)
            .Take(12)
            .ToListAsync())
            .Select(FitnessMapper.ToDto)];

        dto.Entitlements = [.. (agreement.Plan?.Entitlements.Where(e => !e.IsDeleted) ?? [])
            .Select(FitnessMapper.ToDto)];

        var money = await db.Invoices.ForTenant(tenant)
            .Where(i => i.AgreementId == agreementId)
            .GroupBy(i => 1)
            .Select(g => new { Billed = g.Sum(i => i.Total), Collected = g.Sum(i => i.AmountPaid) })
            .FirstOrDefaultAsync();

        dto.LifetimeBilled = money?.Billed ?? 0m;
        dto.LifetimeCollected = money?.Collected ?? 0m;

        if (agreement.PaymentMethodRefId is not null)
        {
            var method = await db.PaymentMethods.ForTenant(tenant)
                .FirstOrDefaultAsync(p => p.Id == agreement.PaymentMethodRefId);
            if (method is not null) dto.PaymentMethodLabel = FitnessMapper.ToDto(method, now).DisplayLabel;
        }

        if (agreement.SoldByStaffId is not null)
        {
            dto.SoldByName = await db.Staff.ForTenant(tenant)
                .Where(s => s.Id == agreement.SoldByStaffId)
                .Select(s => s.FirstName + " " + s.LastName)
                .FirstOrDefaultAsync();
        }

        if (agreement.PromotionRuleId is not null)
        {
            dto.PromotionName = await db.Promotions.ForTenant(tenant)
                .Where(p => p.Id == agreement.PromotionRuleId).Select(p => p.Name).FirstOrDefaultAsync();
        }

        return dto;
    }

    /// <summary>
    /// Creates the agreement, freezes its terms, applies the promotion, and builds the forward
    /// billing schedule — which is what makes the member's next twelve payments visible on the day
    /// they join rather than a month at a time.
    /// </summary>
    public async Task<AgreementDetailDto> CreateAsync(CreateAgreementDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var plan = await db.Plans.ForTenant(tenant)
            .Include(p => p.ClubPrices.Where(c => !c.IsDeleted))
            .FirstOrDefaultAsync(p => p.Id == request.PlanId)
            ?? throw new InvalidOperationException("Plan not found.");

        var member = await db.Members.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == request.MemberId)
            ?? throw new InvalidOperationException("Member not found.");

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync() ?? new FitnessSettings();

        // Club price beats list price; an explicit override beats both.
        var clubPrice = plan.ClubPrices.FirstOrDefault(c => c.ClubId == request.ClubId && c.IsAvailable);
        var basePrice = request.PriceOverride ?? clubPrice?.Price ?? plan.Price;
        var joiningFee = request.WaiveJoiningFee ? 0m : clubPrice?.JoiningFee ?? plan.JoiningFee;

        // ── Promotion ────────────────────────────────────────────────────────

        PromotionRule? promotion = null;
        PromoCode? code = null;

        if (!string.IsNullOrWhiteSpace(request.PromoCodeText))
        {
            code = await db.PromoCodes.ForTenant(tenant)
                .Include(c => c.PromotionRule)
                .FirstOrDefaultAsync(c => c.CodeText.ToUpper() == request.PromoCodeText.ToUpper().Trim());

            if (code is null) throw new InvalidOperationException("That promotional code was not recognised.");
            if (code.ExpiresOn is not null && code.ExpiresOn < now)
                throw new InvalidOperationException("That promotional code has expired.");
            if (code.MaxUses > 0 && code.UseCount >= code.MaxUses)
                throw new InvalidOperationException("That promotional code has been fully redeemed.");

            promotion = code.PromotionRule;
        }
        else if (request.PromotionRuleId is not null)
        {
            promotion = await db.Promotions.ForTenant(tenant)
                .FirstOrDefaultAsync(p => p.Id == request.PromotionRuleId);
        }

        decimal? promoPrice = null;
        var promoPeriods = 0;

        if (promotion is not null && promotion.IsActive)
        {
            promoPrice = promotion.DiscountKind switch
            {
                DiscountKind.Percentage => Math.Round(basePrice * (1 - promotion.Value / 100m), 2),
                DiscountKind.FixedAmount => Math.Max(0, basePrice - promotion.Value),
                DiscountKind.OverridePrice => promotion.Value,
                DiscountKind.FreePeriods => 0m,
                _ => null,
            };

            promoPeriods = promotion.DiscountKind == DiscountKind.FreePeriods
                ? (int)promotion.Value
                : promotion.PeriodCount;

            if (promotion.DiscountKind == DiscountKind.WaiveJoiningFee) joiningFee = 0m;

            promotion.RedemptionCount++;
            if (code is not null) code.UseCount++;
        }

        // ── Dates ────────────────────────────────────────────────────────────

        var startsOn = request.StartsOn.Date;

        DateTime? minimumTermEnds = plan.MinimumTermMonths > 0
            ? startsOn.AddMonths(plan.MinimumTermMonths)
            : null;

        DateTime? endsOn = plan.DurationMonths is > 0
            ? startsOn.AddMonths(plan.DurationMonths.Value)
            : plan.Kind is PlanKind.SessionPack or PlanKind.TimePass && plan.ValidForDays > 0
                ? startsOn.AddDays(plan.ValidForDays)
                : null;

        DateTime? coolingOff = settings.DefaultCoolingOffDays > 0
            ? startsOn.AddDays(settings.DefaultCoolingOffDays)
            : null;

        var billingDay = request.BillingDayOfMonth
            ?? (plan.BillingAnchor == BillingAnchor.FixedDayOfMonth ? settings.FixedBillingDayOfMonth : startsOn.Day);

        var agreement = new Agreement
        {
            AgreementNumber = await numbering.NextAgreementNumberAsync(),
            MemberId = request.MemberId,
            PlanId = request.PlanId,
            PlanVersion = plan.Version,
            ClubId = request.ClubId,
            Status = startsOn > now.Date ? AgreementStatus.Pending : AgreementStatus.Active,

            StartsOn = startsOn,
            MinimumTermEndsOn = minimumTermEnds,
            EndsOn = endsOn,
            CoolingOffEndsOn = coolingOff,

            Price = basePrice,
            CurrencyCode = clubPrice?.CurrencyCode ?? plan.CurrencyCode,
            TaxPercent = plan.TaxPercent,
            BillingPeriod = plan.BillingPeriod,
            BillingAnchor = plan.BillingAnchor,
            BillingDayOfMonth = plan.BillingPeriod == BillingPeriod.OneOff ? null : billingDay,
            NoticePeriodDays = plan.NoticePeriodDays,
            AutoRenews = plan.AutoRenews,
            PriceLocked = request.PriceLocked,

            PromotionRuleId = promotion?.Id,
            PromoCodeId = code?.Id,
            PromotionalPrice = promoPrice,
            PromotionalPeriodsRemaining = promoPeriods,

            CreditsGranted = plan.CreditCount,
            CreditsRemaining = plan.CreditCount,
            CreditsExpireOn = plan.CreditCount > 0 && plan.ValidForDays > 0
                ? startsOn.AddDays(plan.ValidForDays)
                : null,

            PaymentMethodRefId = request.PaymentMethodRefId,
            PayerMemberId = request.PayerMemberId,
            CorporateAccountId = request.CorporateAccountId,
            ThirdPartyPayerId = request.ThirdPartyPayerId,

            TotalInstalments = plan.Kind == PlanKind.TermMembership && plan.DurationMonths is > 0
                ? plan.DurationMonths.Value
                : 0,

            SoldByStaffId = request.SoldByStaffId,
            LeadId = request.LeadId,
        }.StampNew(tenant, userId);

        db.Agreements.Add(agreement);

        // Credits become a spendable balance rather than a number on the agreement, so the
        // booking engine has one place to look.
        if (plan.CreditCount > 0)
        {
            db.SessionCredits.Add(new SessionCredit
            {
                MemberId = request.MemberId,
                AgreementId = agreement.Id,
                Kind = plan.Kind == PlanKind.SessionPack ? EntitlementKind.PersonalTraining : EntitlementKind.ClassBooking,
                Granted = plan.CreditCount,
                Remaining = plan.CreditCount,
                ExpiresOn = agreement.CreditsExpireOn,
                UnitValue = plan.CreditCount == 0 ? 0 : Math.Round(basePrice / plan.CreditCount, 2),
            }.StampNew(tenant, userId));
        }

        member.NextBillingOn = startsOn;
        if (member.JoinedOn is null) member.JoinedOn = startsOn;
        member.FirstJoinedOn ??= startsOn;
        if (member.Status is MemberStatus.Lead or MemberStatus.Trial) member.Status = MemberStatus.Active;
        member.StampUpdated(userId);

        await db.SaveChangesAsync();

        // ── Schedule and first invoice ───────────────────────────────────────

        await BuildScheduleAsync(agreement, plan, joiningFee, userId);
        await db.SaveChangesAsync();
        await billing.RebuildScheduleAsync(agreement.Id, userId);

        return (await GetAsync(agreement.Id))!;
    }

    // ── Signing ──────────────────────────────────────────────────────────────

    public async Task<AgreementSignatureDto> SignAsync(SignAgreementDto request, Guid userId)
    {
        var agreement = await db.Agreements.ForTenant(tenant)
            .Include(a => a.Plan)
            .FirstOrDefaultAsync(a => a.Id == request.AgreementId)
            ?? throw new InvalidOperationException("Agreement not found.");

        var templateId = request.TemplateId ?? agreement.Plan?.AgreementTemplateId;
        var template = templateId is null
            ? null
            : await db.AgreementTemplates.ForTenant(tenant).FirstOrDefaultAsync(t => t.Id == templateId);

        if (template?.RequiresGuardianSignature == true && string.IsNullOrWhiteSpace(request.GuardianName))
            throw new InvalidOperationException("This agreement needs a parent or guardian signature.");

        var signature = new AgreementSignature
        {
            AgreementId = agreement.Id,
            TemplateId = templateId,
            TemplateVersion = template?.Version ?? 1,
            SignerName = request.SignerName,
            GuardianName = request.GuardianName,
            GuardianRelationship = request.GuardianRelationship,
            Status = SignatureStatus.Signed,
            SignedAt = DateTime.UtcNow,
            SignatureImageUrl = request.SignatureImageUrl,
            CapturedVia = request.CapturedVia ?? "Front desk",
        }.StampNew(tenant, userId);

        db.AgreementSignatures.Add(signature);

        agreement.SignedOn = signature.SignedAt;
        agreement.SignatureImageUrl = request.SignatureImageUrl;
        if (agreement.Status == AgreementStatus.Draft) agreement.Status = AgreementStatus.Active;
        agreement.StampUpdated(userId);

        await db.SaveChangesAsync();

        return new AgreementSignatureDto
        {
            Id = signature.Id,
            AgreementId = signature.AgreementId,
            TemplateId = signature.TemplateId,
            TemplateVersion = signature.TemplateVersion,
            SignerName = signature.SignerName,
            GuardianName = signature.GuardianName,
            GuardianRelationship = signature.GuardianRelationship,
            Status = signature.Status,
            SignedAt = signature.SignedAt,
            SignatureImageUrl = signature.SignatureImageUrl,
            CapturedVia = signature.CapturedVia,
        };
    }

    public async Task<AgreementSignatureDto> RequestRemoteSignatureAsync(RequestRemoteSignatureDto request, Guid userId)
    {
        var agreement = await db.Agreements.ForTenant(tenant)
            .Include(a => a.Member)
            .Include(a => a.Plan)
            .FirstOrDefaultAsync(a => a.Id == request.AgreementId)
            ?? throw new InvalidOperationException("Agreement not found.");

        var signature = new AgreementSignature
        {
            AgreementId = agreement.Id,
            TemplateId = agreement.Plan?.AgreementTemplateId,
            SignerName = agreement.Member is null ? string.Empty : FitnessMapper.FullName(agreement.Member),
            Status = SignatureStatus.Pending,
            RemoteToken = Guid.NewGuid().ToString("N"),
            RemoteTokenExpiresOn = DateTime.UtcNow.AddHours(request.ExpiryHours),
            CapturedVia = "Remote link",
        }.StampNew(tenant, userId);

        db.AgreementSignatures.Add(signature);

        db.MessageLog.Add(new MessageLog
        {
            MemberId = agreement.MemberId,
            ClubId = agreement.ClubId,
            Channel = request.Channel,
            Status = MessageStatus.Queued,
            Recipient = request.Channel == MessageChannel.Email ? agreement.Member?.Email : agreement.Member?.Phone,
            Subject = "Please sign your membership agreement",
            BodyPreview = request.Message ?? "Your agreement is ready to sign.",
            QueuedAt = DateTime.UtcNow,
        }.StampNew(tenant, userId));

        await db.SaveChangesAsync();

        return new AgreementSignatureDto
        {
            Id = signature.Id,
            AgreementId = signature.AgreementId,
            SignerName = signature.SignerName,
            Status = signature.Status,
            CapturedVia = signature.CapturedVia,
        };
    }

    // ── Plan change ──────────────────────────────────────────────────────────

    /// <summary>
    /// What an upgrade or downgrade will actually cost, and when it takes effect.
    ///
    /// The explanation string matters as much as the number: a member who is shown "£12.90 for
    /// the 9 days remaining on your old plan, then £39 a month from 1 March" accepts it, and a
    /// member shown "£12.90" phones up.
    /// </summary>
    public async Task<PlanChangePreviewDto> PreviewPlanChangeAsync(Guid agreementId, Guid newPlanId, DateTime? effectiveOn)
    {
        var now = DateTime.UtcNow;

        var agreement = await db.Agreements.ForTenant(tenant)
            .Include(a => a.Plan)
            .FirstOrDefaultAsync(a => a.Id == agreementId)
            ?? throw new InvalidOperationException("Agreement not found.");

        var newPlan = await db.Plans.ForTenant(tenant)
            .Include(p => p.ClubPrices.Where(c => !c.IsDeleted))
            .FirstOrDefaultAsync(p => p.Id == newPlanId)
            ?? throw new InvalidOperationException("Plan not found.");

        var path = await db.PlanChangePaths.ForTenant(tenant)
            .FirstOrDefaultAsync(p => p.FromPlanId == agreement.PlanId && p.ToPlanId == newPlanId);

        var newPrice = newPlan.ClubPrices.FirstOrDefault(c => c.ClubId == agreement.ClubId)?.Price ?? newPlan.Price;
        var isUpgrade = newPrice > agreement.Price;

        // With no configured path, upgrades take effect now and downgrades at the next period —
        // which is what almost every club does, and stops a downgrade being a same-day refund.
        var immediate = path?.EffectiveImmediately ?? isUpgrade;
        var proration = path?.Proration ?? ProrationRule.Daily;

        var effective = effectiveOn?.Date
            ?? (immediate ? now.Date : agreement.NextBillingOn?.Date ?? now.Date);

        var preview = new PlanChangePreviewDto
        {
            AgreementId = agreementId,
            NewPlanId = newPlanId,
            NewPlanName = newPlan.Name,
            IsAllowed = true,
            EffectiveImmediately = immediate,
            EffectiveOn = effective,
            CurrentPrice = agreement.Price,
            NewPrice = newPrice,
            PriceDifference = newPrice - agreement.Price,
            ChangeFee = path?.ChangeFee ?? 0m,
            RestartsMinimumTerm = path?.RestartsMinimumTerm ?? false,
            RequiresApproval = path?.RequiresApproval ?? false,
        };

        // Downgrading inside a minimum term is usually a contract breach.
        if (!isUpgrade && agreement.MinimumTermEndsOn is not null && agreement.MinimumTermEndsOn > effective)
        {
            preview.IsAllowed = false;
            preview.BlockReason =
                $"This membership is committed until {agreement.MinimumTermEndsOn:d MMM yyyy}. " +
                "A downgrade before then needs a manager to approve it.";
            preview.RequiresApproval = true;
        }

        // Proration, when the change lands mid-period.
        if (immediate && proration == ProrationRule.Daily && agreement.NextBillingOn is not null)
        {
            var periodStart = agreement.LastBilledOn ?? agreement.StartsOn;
            var periodEnd = agreement.NextBillingOn.Value;
            var totalDays = Math.Max(1, (periodEnd - periodStart).Days);
            var remainingDays = Math.Max(0, (periodEnd - effective).Days);

            var unusedOld = Math.Round(agreement.Price * remainingDays / totalDays, 2);
            var newPortion = Math.Round(newPrice * remainingDays / totalDays, 2);

            preview.ProrationCredit = unusedOld;
            preview.ProrationCharge = newPortion;
            preview.DueNow = Math.Max(0, newPortion - unusedOld) + preview.ChangeFee;

            preview.Explanation =
                $"{remainingDays} days remain in the current period. " +
                $"You are credited {unusedOld:0.00} for the unused part of {agreement.Plan?.Name}, " +
                $"and charged {newPortion:0.00} for {newPlan.Name} over the same days" +
                (preview.ChangeFee > 0 ? $", plus a {preview.ChangeFee:0.00} change fee" : "") +
                $". Net due today: {preview.DueNow:0.00}.";
        }
        else
        {
            preview.DueNow = preview.ChangeFee;
            preview.Explanation = immediate
                ? $"The new price applies from today. Nothing is prorated{(preview.ChangeFee > 0 ? $", but a {preview.ChangeFee:0.00} change fee applies" : "")}."
                : $"Your current plan runs to {effective:d MMM yyyy}, then {newPlan.Name} starts at {newPrice:0.00}.";
        }

        preview.NextBillingOn = immediate
            ? effective.AddPeriod(newPlan.BillingPeriod)
            : effective;
        preview.NextBillingAmount = newPrice;

        if (preview.RestartsMinimumTerm && newPlan.MinimumTermMonths > 0)
            preview.NewMinimumTermEndsOn = effective.AddMonths(newPlan.MinimumTermMonths);

        return preview;
    }

    public async Task<AgreementDetailDto> ChangePlanAsync(ChangePlanDto request, Guid userId)
    {
        var preview = await PreviewPlanChangeAsync(request.AgreementId, request.NewPlanId, request.EffectiveOn);

        if (!preview.IsAllowed && !preview.RequiresApproval)
            throw new InvalidOperationException(preview.BlockReason ?? "This plan change is not allowed.");

        var agreement = await db.Agreements.ForTenant(tenant)
            .Include(a => a.Plan)
            .FirstOrDefaultAsync(a => a.Id == request.AgreementId)
            ?? throw new InvalidOperationException("Agreement not found.");

        var newPlan = await db.Plans.ForTenant(tenant)
            .Include(p => p.ClubPrices.Where(c => !c.IsDeleted))
            .FirstAsync(p => p.Id == request.NewPlanId);

        var oldPlanId = agreement.PlanId;
        var oldPrice = agreement.Price;
        var newPrice = request.PriceOverride ?? preview.NewPrice;
        var fee = request.WaiveChangeFee ? 0m : preview.ChangeFee;

        var amendment = new AgreementAmendment
        {
            AgreementId = agreement.Id,
            Kind = newPrice > oldPrice ? AgreementChangeKind.Upgrade : AgreementChangeKind.Downgrade,
            EffectiveOn = preview.EffectiveOn,
            PreviousPrice = oldPrice,
            NewPrice = newPrice,
            PreviousPlanId = oldPlanId,
            NewPlanId = request.NewPlanId,
            ChangeFee = fee,
            ProrationAmount = preview.ProrationCharge - preview.ProrationCredit,
            Reason = request.Reason,
            ApprovedByUserId = preview.RequiresApproval ? userId : null,
        }.StampNew(tenant, userId);

        db.Amendments.Add(amendment);

        // The agreement carries the new terms from the effective date. Its history lives in the
        // amendment rows, which is why the old price is not simply overwritten and forgotten.
        agreement.PlanId = request.NewPlanId;
        agreement.PlanVersion = newPlan.Version;
        agreement.Price = newPrice;
        agreement.BillingPeriod = newPlan.BillingPeriod;
        agreement.NoticePeriodDays = newPlan.NoticePeriodDays;

        if (preview.RestartsMinimumTerm && newPlan.MinimumTermMonths > 0)
            agreement.MinimumTermEndsOn = preview.EffectiveOn.AddMonths(newPlan.MinimumTermMonths);

        agreement.NextBillingOn = preview.NextBillingOn;
        agreement.StampUpdated(userId);

        await db.SaveChangesAsync();
        await billing.RebuildScheduleAsync(agreement.Id, userId);

        // Anything due today becomes a real invoice rather than a note in the amendment.
        if (request.CollectDueNow && preview.DueNow > 0)
        {
            var lines = new List<InvoiceLineDto>();

            if (preview.ProrationCharge > 0)
            {
                lines.Add(new InvoiceLineDto
                {
                    ChargeKind = ChargeKind.ProRata,
                    LineDescription = $"{newPlan.Name} — remainder of the current period",
                    Quantity = 1,
                    UnitPrice = preview.ProrationCharge,
                    LineTotal = preview.ProrationCharge,
                    ProrationExplanation = preview.Explanation,
                });
            }

            if (preview.ProrationCredit > 0)
            {
                lines.Add(new InvoiceLineDto
                {
                    ChargeKind = ChargeKind.Adjustment,
                    LineDescription = $"Credit for unused {agreement.Plan?.Name}",
                    Quantity = 1,
                    UnitPrice = -preview.ProrationCredit,
                    LineTotal = -preview.ProrationCredit,
                });
            }

            if (fee > 0)
            {
                lines.Add(new InvoiceLineDto
                {
                    ChargeKind = ChargeKind.AdminFee,
                    LineDescription = "Plan change fee",
                    Quantity = 1,
                    UnitPrice = fee,
                    LineTotal = fee,
                });
            }

            if (lines.Count > 0)
                await billing.CreateAdHocInvoiceAsync(agreement.MemberId, agreement.ClubId, lines, userId);
        }

        return (await GetAsync(agreement.Id))!;
    }

    // ── Freezes ──────────────────────────────────────────────────────────────

    /// <summary>
    /// What a freeze will do, before anyone agrees to it.
    ///
    /// The three numbers that matter are the fee, the days added to the commitment, and which
    /// charges move. A freeze that silently keeps billing is the fastest route to a chargeback,
    /// so the skipped charges are listed explicitly rather than implied.
    /// </summary>
    public async Task<FreezePreviewDto> PreviewFreezeAsync(RequestFreezeDto request)
    {
        var now = DateTime.UtcNow;

        var agreement = await db.Agreements.ForTenant(tenant)
            .Include(a => a.Plan)
            .FirstOrDefaultAsync(a => a.Id == request.AgreementId)
            ?? throw new InvalidOperationException("Agreement not found.");

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync() ?? new FitnessSettings();

        var start = request.StartsOn.Date;
        var end = request.EndsOn.Date;
        var days = Math.Max(0, (end - start).Days + 1);

        var yearStart = now.AddYears(-1);
        var usedThisYear = await db.Freezes.ForTenant(tenant)
            .Where(f => f.MemberId == agreement.MemberId && f.CountsAgainstAllowance && f.StartsOn >= yearStart)
            .SumAsync(f => (int?)((f.ActuallyEndedOn ?? f.EndsOn) - f.StartsOn).Days) ?? 0;

        var allowance = settings.MaxFreezeDaysPerYear;
        var counts = !request.IsMedical;

        var preview = new FreezePreviewDto
        {
            IsAllowed = true,
            StartsOn = start,
            EndsOn = end,
            FreezeDays = days,
            FreezeDaysUsedThisYear = usedThisYear,
            FreezeDaysAllowance = allowance,
            FreezeDaysRemaining = Math.Max(0, allowance - usedThisYear),
        };

        if (end < start)
        {
            preview.IsAllowed = false;
            preview.BlockReason = "A freeze cannot end before it starts.";
            return preview;
        }

        if (counts && allowance > 0 && usedThisYear + days > allowance)
        {
            preview.IsAllowed = false;
            preview.BlockReason =
                $"This freeze is {days} days, but only {Math.Max(0, allowance - usedThisYear)} of the " +
                $"{allowance}-day annual allowance is left. A manager can override it, or record it as medical.";
        }

        var overlapping = await db.Freezes.ForTenant(tenant)
            .AnyAsync(f => f.AgreementId == agreement.Id && !f.IsReleased
                        && f.StartsOn <= end && f.EndsOn >= start);

        if (overlapping)
        {
            preview.IsAllowed = false;
            preview.BlockReason = "There is already a freeze covering some of those dates.";
        }

        // Fee: waived for medical, otherwise per started month.
        var months = Math.Max(1, (int)Math.Ceiling(days / 30.44));
        var perPeriod = request.FeeOverride ?? settings.DefaultFreezeFeePerMonth;
        preview.Fee = request.WaiveFee || request.IsMedical ? 0m : Math.Round(perPeriod * months, 2);

        preview.SkippedCharges = [.. (await db.BillingSchedules.ForTenant(tenant)
            .Where(s => s.AgreementId == agreement.Id && !s.IsBilled
                     && s.DueOn >= start && s.DueOn <= end)
            .OrderBy(s => s.DueOn)
            .ToListAsync())
            .Select(FitnessMapper.ToDto)];

        // The freeze pushes the commitment out by the frozen days, so twelve months stays twelve
        // months of service rather than twelve months of calendar.
        preview.NewMinimumTermEndsOn = agreement.MinimumTermEndsOn?.AddDays(days);
        preview.NewNextBillingOn = agreement.NextBillingOn is null || agreement.NextBillingOn > end
            ? agreement.NextBillingOn ?? end.AddDays(1)
            : end.AddDays(1);

        preview.Explanation =
            $"Membership pauses for {days} days from {start:d MMM yyyy}. " +
            (preview.SkippedCharges.Count > 0
                ? $"{preview.SkippedCharges.Count} payment{(preview.SkippedCharges.Count == 1 ? "" : "s")} " +
                  $"totalling {preview.SkippedCharges.Sum(c => c.Amount):0.00} will be skipped, and billing restarts on {preview.NewNextBillingOn:d MMM yyyy}. "
                : $"Billing restarts on {preview.NewNextBillingOn:d MMM yyyy}. ") +
            (preview.Fee > 0 ? $"A freeze fee of {preview.Fee:0.00} applies. " : "No freeze fee. ") +
            (preview.NewMinimumTermEndsOn is not null
                ? $"The committed end date moves to {preview.NewMinimumTermEndsOn:d MMM yyyy}."
                : "");

        return preview;
    }

    public async Task<MembershipFreezeDto> FreezeAsync(RequestFreezeDto request, Guid userId)
    {
        var preview = await PreviewFreezeAsync(request);
        if (!preview.IsAllowed)
            throw new InvalidOperationException(preview.BlockReason ?? "This freeze is not allowed.");

        var now = DateTime.UtcNow;

        var agreement = await db.Agreements.ForTenant(tenant)
            .FirstOrDefaultAsync(a => a.Id == request.AgreementId)
            ?? throw new InvalidOperationException("Agreement not found.");

        var freeze = new MembershipFreeze
        {
            AgreementId = agreement.Id,
            MemberId = agreement.MemberId,
            StartsOn = preview.StartsOn,
            EndsOn = preview.EndsOn,
            Reason = request.Reason,
            ReasonNote = request.ReasonNote,
            FeePerPeriod = request.FeeOverride ?? 0m,
            TotalFeeCharged = preview.Fee,
            IsMedical = request.IsMedical,
            CountsAgainstAllowance = !request.IsMedical,
            SupportingDocumentId = request.SupportingDocumentId,
            ApprovedByUserId = userId,
            ApprovedAt = now,
        }.StampNew(tenant, userId);

        db.Freezes.Add(freeze);

        // Skip the charges inside the window rather than deleting them, so the history says what
        // happened and why.
        var toSkip = await db.BillingSchedules.ForTenant(tenant)
            .Where(s => s.AgreementId == agreement.Id && !s.IsBilled
                     && s.DueOn >= preview.StartsOn && s.DueOn <= preview.EndsOn)
            .ToListAsync();

        foreach (var charge in toSkip)
        {
            charge.IsSkipped = true;
            charge.SkipReason = $"Frozen {preview.StartsOn:d MMM} – {preview.EndsOn:d MMM}";
            charge.StampUpdated(userId);
        }

        // A freeze that starts today takes effect now; a future one is applied by the nightly job.
        if (preview.StartsOn <= now.Date)
        {
            agreement.Status = AgreementStatus.Frozen;

            var member = await db.Members.ForTenant(tenant).FirstAsync(m => m.Id == agreement.MemberId);
            member.Status = MemberStatus.Frozen;
            member.StampUpdated(userId);
        }

        agreement.MinimumTermEndsOn = preview.NewMinimumTermEndsOn;
        agreement.NextBillingOn = preview.NewNextBillingOn;
        agreement.StampUpdated(userId);

        if (preview.Fee > 0)
        {
            await billing.CreateAdHocInvoiceAsync(agreement.MemberId, agreement.ClubId, [
                new InvoiceLineDto
                {
                    ChargeKind = ChargeKind.FreezeFee,
                    LineDescription = $"Freeze fee — {preview.StartsOn:d MMM} to {preview.EndsOn:d MMM yyyy}",
                    Quantity = 1,
                    UnitPrice = preview.Fee,
                    LineTotal = preview.Fee,
                },
            ], userId);
        }

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(freeze, now);
    }

    public async Task<MembershipFreezeDto> EndFreezeAsync(EndFreezeDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var freeze = await db.Freezes.ForTenant(tenant)
            .Include(f => f.Agreement)
            .FirstOrDefaultAsync(f => f.Id == request.FreezeId)
            ?? throw new InvalidOperationException("Freeze not found.");

        if (freeze.IsReleased) return FitnessMapper.ToDto(freeze, now);

        var endOn = (request.EndOn ?? now).Date;
        freeze.ActuallyEndedOn = endOn;
        freeze.IsReleased = true;

        // Recompute the extension from what was actually frozen, not what was planned — an early
        // return should not extend the commitment by days the member did not lose.
        freeze.DaysExtended = Math.Max(0, (endOn - freeze.StartsOn).Days);
        freeze.StampUpdated(userId);

        var agreement = freeze.Agreement;
        if (agreement is not null)
        {
            agreement.Status = AgreementStatus.Active;

            // Give back the days between the early return and the planned end.
            if (endOn < freeze.EndsOn && agreement.MinimumTermEndsOn is not null)
            {
                var reclaimed = (freeze.EndsOn - endOn).Days;
                agreement.MinimumTermEndsOn = agreement.MinimumTermEndsOn.Value.AddDays(-reclaimed);
            }

            agreement.NextBillingOn = endOn.AddDays(1);
            agreement.StampUpdated(userId);

            // Un-skip the charges that fall after the early return.
            var restore = await db.BillingSchedules.ForTenant(tenant)
                .Where(s => s.AgreementId == agreement.Id && s.IsSkipped && !s.IsBilled && s.DueOn > endOn)
                .ToListAsync();

            foreach (var charge in restore)
            {
                charge.IsSkipped = false;
                charge.SkipReason = null;
                charge.StampUpdated(userId);
            }

            var member = await db.Members.ForTenant(tenant).FirstAsync(m => m.Id == agreement.MemberId);
            member.Status = MemberStatus.Active;
            member.NextBillingOn = agreement.NextBillingOn;
            member.StampUpdated(userId);
        }

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(freeze, now);
    }

    public async Task<List<MembershipFreezeDto>> GetFreezesAsync(Guid? clubId, Guid? memberId, bool activeOnly)
    {
        var now = DateTime.UtcNow;

        var freezes = await db.Freezes.ForTenant(tenant)
            .Include(f => f.Agreement)
            .WhereIf(memberId is not null, f => f.MemberId == memberId)
            .WhereIf(clubId is not null, f => f.Agreement!.ClubId == clubId)
            .WhereIf(activeOnly, f => !f.IsReleased && f.EndsOn >= now.Date)
            .OrderByDescending(f => f.StartsOn)
            .ToListAsync();

        var memberIds = freezes.Select(f => f.MemberId).Distinct().ToList();
        var names = await db.Members.ForTenant(tenant)
            .Where(m => memberIds.Contains(m.Id))
            .Select(m => new { m.Id, Name = m.FirstName + " " + m.LastName })
            .ToDictionaryAsync(m => m.Id, m => m.Name);

        return [.. freezes.Select(f =>
        {
            var dto = FitnessMapper.ToDto(f, now);
            dto.MemberName = names.GetValueOrDefault(f.MemberId);
            return dto;
        })];
    }

    /// <summary>
    /// Releases freezes whose end date has passed, and starts ones whose date has arrived.
    ///
    /// Runs nightly. Without it, a member whose freeze ended on Sunday is still locked out on
    /// Monday morning, which is exactly the kind of thing that generates a complaint about
    /// something nobody did wrong.
    /// </summary>
    public async Task<int> ReleaseDueFreezesAsync()
    {
        var today = DateTime.UtcNow.Date;
        var changed = 0;

        var ending = await db.Freezes.ForTenant(tenant)
            .Include(f => f.Agreement)
            .Where(f => !f.IsReleased && f.EndsOn < today)
            .ToListAsync();

        foreach (var freeze in ending)
        {
            freeze.IsReleased = true;
            freeze.ActuallyEndedOn = freeze.EndsOn;
            freeze.DaysExtended = (freeze.EndsOn - freeze.StartsOn).Days;

            if (freeze.Agreement is not null)
            {
                freeze.Agreement.Status = AgreementStatus.Active;
                freeze.Agreement.NextBillingOn = freeze.EndsOn.AddDays(1);
            }

            var member = await db.Members.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == freeze.MemberId);
            if (member is not null)
            {
                member.Status = MemberStatus.Active;
                member.NextBillingOn = freeze.EndsOn.AddDays(1);
            }

            changed++;
        }

        var starting = await db.Freezes.ForTenant(tenant)
            .Include(f => f.Agreement)
            .Where(f => !f.IsReleased && f.StartsOn <= today && f.EndsOn >= today)
            .ToListAsync();

        foreach (var freeze in starting.Where(f => f.Agreement?.Status == AgreementStatus.Active))
        {
            freeze.Agreement!.Status = AgreementStatus.Frozen;

            var member = await db.Members.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == freeze.MemberId);
            if (member is not null) member.Status = MemberStatus.Frozen;

            changed++;
        }

        await db.SaveChangesAsync();
        return changed;
    }

    // ── Suspensions ──────────────────────────────────────────────────────────

    public async Task<MembershipSuspensionDto> SuspendAsync(SuspendMemberDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var member = await db.Members.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == request.MemberId)
            ?? throw new InvalidOperationException("Member not found.");

        var suspension = new MembershipSuspension
        {
            MemberId = request.MemberId,
            AgreementId = request.AgreementId,
            StartsOn = now.Date,
            EndsOn = request.EndsOn,
            Reason = request.Reason,
            ReasonNote = request.ReasonNote,
            ContinuesBilling = request.ContinuesBilling,
            AutoLiftsWhenResolved = request.AutoLiftsWhenResolved,
            ImposedByUserId = userId,
        }.StampNew(tenant, userId);

        db.Suspensions.Add(suspension);

        member.Status = MemberStatus.Suspended;
        member.StampUpdated(userId);

        db.MemberNotes.Add(new MemberNote
        {
            MemberId = member.Id,
            Kind = InteractionKind.SystemEvent,
            Body = $"Suspended: {request.Reason}" + (request.ReasonNote is not null ? $" — {request.ReasonNote}" : ""),
            OccurredAt = now,
            IsPinned = true,
        }.StampNew(tenant, userId));

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(suspension, now);
    }

    public async Task<MembershipSuspensionDto> LiftSuspensionAsync(Guid suspensionId, string? reason, Guid userId)
    {
        var now = DateTime.UtcNow;

        var suspension = await db.Suspensions.ForTenant(tenant)
            .Include(s => s.Member)
            .FirstOrDefaultAsync(s => s.Id == suspensionId)
            ?? throw new InvalidOperationException("Suspension not found.");

        suspension.LiftedOn = now;
        suspension.LiftedByUserId = userId;
        suspension.StampUpdated(userId);

        if (suspension.Member is not null)
        {
            var stillSuspended = await db.Suspensions.ForTenant(tenant)
                .AnyAsync(s => s.MemberId == suspension.MemberId && s.Id != suspensionId && s.LiftedOn == null);

            if (!stillSuspended) suspension.Member.Status = MemberStatus.Active;
            suspension.Member.StampUpdated(userId);
        }

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(suspension, now);
    }

    /// <summary>
    /// Lifts suspensions whose reason has gone away — the balance was paid, the waiver was signed.
    ///
    /// Runs nightly, and matters because the alternative is a member who paid at the desk on
    /// Friday and is still locked out on Saturday because nobody remembered to clear the flag.
    /// </summary>
    public async Task<int> LiftResolvedSuspensionsAsync()
    {
        var now = DateTime.UtcNow;
        var lifted = 0;

        var candidates = await db.Suspensions.ForTenant(tenant)
            .Include(s => s.Member)
            .Where(s => s.LiftedOn == null && s.AutoLiftsWhenResolved)
            .ToListAsync();

        foreach (var suspension in candidates)
        {
            if (suspension.Member is null) continue;

            var resolved = suspension.Reason switch
            {
                SuspensionReason.UnpaidBalance => suspension.Member.AccountBalance <= 0,
                SuspensionReason.MissingWaiver => await db.WaiverSignatures.ForTenant(tenant)
                    .AnyAsync(w => w.MemberId == suspension.MemberId
                                && w.Status == SignatureStatus.Signed
                                && (w.ExpiresOn == null || w.ExpiresOn > now)),
                SuspensionReason.MissingMedicalClearance => suspension.Member.MedicalClearance == ClearanceStatus.Approved,
                _ => suspension.EndsOn is not null && suspension.EndsOn < now,
            };

            if (!resolved) continue;

            suspension.LiftedOn = now;
            suspension.Member.Status = MemberStatus.Active;
            lifted++;
        }

        await db.SaveChangesAsync();
        return lifted;
    }

    // ── Cancellation ─────────────────────────────────────────────────────────

    /// <summary>
    /// The save conversation, computed.
    ///
    /// Everything a receptionist needs while a member is standing there saying they want to
    /// leave: when it actually ends, what it costs, what they lose, and which offers have
    /// historically worked for this reason at this club. The suggested offers are the point —
    /// "would a freeze suit you better?" saves more memberships than any discount, and nobody
    /// thinks of it under pressure.
    /// </summary>
    public async Task<CancellationPreviewDto> PreviewCancellationAsync(Guid agreementId, DateTime? requestedEffectiveOn)
    {
        var now = DateTime.UtcNow;

        var agreement = await db.Agreements.ForTenant(tenant)
            .Include(a => a.Member)
            .Include(a => a.Plan)
            .FirstOrDefaultAsync(a => a.Id == agreementId)
            ?? throw new InvalidOperationException("Agreement not found.");

        var noticeEnds = now.Date.AddDays(agreement.NoticePeriodDays);
        var effective = requestedEffectiveOn?.Date ?? noticeEnds;
        if (effective < noticeEnds) effective = noticeEnds;

        var inMinimumTerm = agreement.MinimumTermEndsOn is not null && agreement.MinimumTermEndsOn > effective;
        var inCoolingOff = agreement.CoolingOffEndsOn is not null && agreement.CoolingOffEndsOn > now;

        var preview = new CancellationPreviewDto
        {
            AgreementId = agreementId,
            RequestedOn = now,
            EffectiveOn = effective,
            NoticePeriodDays = agreement.NoticePeriodDays,
            IsInMinimumTerm = inMinimumTerm,
            MinimumTermEndsOn = agreement.MinimumTermEndsOn,
            IsInCoolingOff = inCoolingOff,
        };

        if (inMinimumTerm && agreement.MinimumTermEndsOn is not null)
        {
            preview.MonthsRemainingInTerm = Math.Max(0,
                (int)Math.Ceiling((agreement.MinimumTermEndsOn.Value - effective).TotalDays / 30.44));

            var plan = agreement.Plan;
            var flat = plan?.EarlyTerminationFee ?? 0m;
            var percent = plan?.EarlyTerminationPercentOfRemaining ?? 0m;
            var remainingValue = agreement.Price * preview.MonthsRemainingInTerm;

            preview.EarlyTerminationFee = Math.Round(flat + remainingValue * percent / 100m, 2);
        }

        // Inside the cooling-off window, everything paid goes back and no fee applies.
        if (inCoolingOff)
        {
            preview.EarlyTerminationFee = 0m;
            preview.EffectiveOn = now.Date;
            preview.RefundDue = await db.Payments.ForTenant(tenant)
                .Where(p => p.MemberId == agreement.MemberId && p.Status == PaymentStatus.Succeeded
                         && p.ReceivedOn >= agreement.StartsOn)
                .SumAsync(p => (decimal?)(p.Amount - p.RefundedAmount)) ?? 0m;
        }

        preview.OutstandingBalance = agreement.Member?.AccountBalance ?? 0m;

        // Charges that still fall due before the effective date.
        preview.FinalCharge = await db.BillingSchedules.ForTenant(tenant)
            .Where(s => s.AgreementId == agreementId && !s.IsBilled && !s.IsSkipped
                     && s.DueOn >= now.Date && s.DueOn < preview.EffectiveOn)
            .SumAsync(s => (decimal?)s.Amount) ?? 0m;

        preview.NetDue = preview.FinalCharge + preview.EarlyTerminationFee + preview.OutstandingBalance - preview.RefundDue;

        preview.Explanation =
            (inCoolingOff
                ? $"This is inside the {agreement.CoolingOffEndsOn:d MMM} cooling-off period, so it ends today and everything paid is refunded. "
                : $"Notice is {agreement.NoticePeriodDays} days, so membership ends on {preview.EffectiveOn:d MMM yyyy}. ") +
            (preview.FinalCharge > 0 ? $"One further payment of {preview.FinalCharge:0.00} falls due before then. " : "") +
            (preview.EarlyTerminationFee > 0
                ? $"Leaving {preview.MonthsRemainingInTerm} months inside the committed term carries a {preview.EarlyTerminationFee:0.00} early-termination fee. "
                : "") +
            (preview.OutstandingBalance > 0 ? $"There is {preview.OutstandingBalance:0.00} outstanding on the account. " : "");

        preview.BookingsToCancel = [.. (await db.ClassBookings.ForTenant(tenant)
            .Where(k => k.MemberId == agreement.MemberId && k.Status == BookingStatus.Booked
                     && k.ClassOccurrence!.StartsAt > preview.EffectiveOn)
            .Include(k => k.ClassOccurrence).ThenInclude(o => o!.ClassType)
            .Take(20)
            .ToListAsync())
            .Select(k => new UpcomingBookingDto
            {
                Id = k.Id,
                BookingType = "Class",
                Title = k.ClassOccurrence?.ClassType?.Name ?? "Class",
                StartsAt = k.ClassOccurrence!.StartsAt,
                EndsAt = k.ClassOccurrence.EndsAt,
                Status = k.Status,
            })];

        var credits = await db.SessionCredits.ForTenant(tenant)
            .Where(c => c.MemberId == agreement.MemberId && !c.IsExpired && c.Remaining > 0)
            .ToListAsync();

        preview.UnusedCredits = credits.Sum(c => c.Remaining);
        preview.UnusedCreditValue = credits.Sum(c => c.Remaining * c.UnitValue);

        preview.SuggestedOffers = await SuggestOffersAsync(agreement, preview, now);

        return preview;
    }

    public async Task<CancellationRequestDto> RequestCancellationAsync(RequestCancellationDto request, Guid userId)
    {
        var now = DateTime.UtcNow;
        var preview = await PreviewCancellationAsync(request.AgreementId, request.RequestedEffectiveOn);

        var agreement = await db.Agreements.ForTenant(tenant)
            .FirstOrDefaultAsync(a => a.Id == request.AgreementId)
            ?? throw new InvalidOperationException("Agreement not found.");

        var fee = request.WaiveEarlyTerminationFee ? 0m : preview.EarlyTerminationFee;

        var cancellation = new CancellationRequest
        {
            AgreementId = agreement.Id,
            MemberId = agreement.MemberId,
            RequestedOn = now,
            EffectiveOn = preview.EffectiveOn,
            Reason = request.Reason,
            ReasonNote = request.ReasonNote,
            Channel = request.Channel ?? "Front desk",
            EarlyTerminationFee = fee,
            RefundDue = preview.RefundDue,
            OutstandingBalance = preview.OutstandingBalance,
        }.StampNew(tenant, userId);

        db.CancellationRequests.Add(cancellation);

        agreement.Status = AgreementStatus.NoticeGiven;
        agreement.CancellationEffectiveOn = preview.EffectiveOn;
        agreement.LeaveReason = request.Reason;
        agreement.LeaveNote = request.ReasonNote;
        agreement.EarlyTerminationFeeCharged = fee;
        agreement.StampUpdated(userId);

        if (request.WaiveEarlyTerminationFee && preview.EarlyTerminationFee > 0)
        {
            db.MemberNotes.Add(new MemberNote
            {
                MemberId = agreement.MemberId,
                Kind = InteractionKind.SystemEvent,
                Body = $"Early-termination fee of {preview.EarlyTerminationFee:0.00} waived — {request.WaiverReason}",
                OccurredAt = now,
            }.StampNew(tenant, userId));
        }

        await db.SaveChangesAsync();

        var saved = await db.CancellationRequests.ForTenant(tenant)
            .Include(c => c.Agreement)
            .Include(c => c.Offers.Where(o => !o.IsDeleted))
            .FirstAsync(c => c.Id == cancellation.Id);

        var dto = FitnessMapper.ToDto(saved);
        dto.LifetimeValue = await db.Payments.ForTenant(tenant)
            .Where(p => p.MemberId == agreement.MemberId && p.Status == PaymentStatus.Succeeded)
            .SumAsync(p => (decimal?)p.Amount) ?? 0m;
        dto.TenureMonths = (int)((now - agreement.StartsOn).TotalDays / 30.44);

        return dto;
    }

    public async Task<SaveOfferDto> MakeSaveOfferAsync(MakeSaveOfferDto request, Guid userId)
    {
        var offer = new SaveOffer
        {
            CancellationRequestId = request.CancellationRequestId,
            Kind = request.Kind,
            Summary = request.Summary,
            DiscountValue = request.DiscountValue,
            PeriodCount = request.PeriodCount,
            AlternativePlanId = request.AlternativePlanId,
            OfferedAt = DateTime.UtcNow,
            OfferedByStaffId = userId,
        }.StampNew(tenant, userId);

        db.SaveOffers.Add(offer);
        await db.SaveChangesAsync();

        return FitnessMapper.ToDto(offer);
    }

    /// <summary>
    /// A member accepting a save offer, and the agreement actually changing as a result.
    ///
    /// The offer is not just recorded — accepting a "freeze instead" creates the freeze, accepting
    /// a discount rewrites the promotional price. An offer that is logged but not applied is how
    /// a save turns into a complaint next month.
    /// </summary>
    public async Task<CancellationRequestDto> RespondToOfferAsync(RespondToSaveOfferDto request, Guid userId)
    {
        var now = DateTime.UtcNow;

        var offer = await db.SaveOffers.ForTenant(tenant)
            .Include(o => o.CancellationRequest).ThenInclude(c => c!.Agreement)
            .FirstOrDefaultAsync(o => o.Id == request.SaveOfferId)
            ?? throw new InvalidOperationException("Offer not found.");

        offer.WasAccepted = request.Accepted;
        offer.RespondedAt = now;
        offer.DeclineNote = request.Accepted ? null : request.Note;
        offer.StampUpdated(userId);

        var cancellation = offer.CancellationRequest;
        var agreement = cancellation?.Agreement;

        if (request.Accepted && cancellation is not null && agreement is not null)
        {
            cancellation.WasSaved = true;
            cancellation.SavedOn = now;
            cancellation.IsProcessed = true;
            cancellation.StampUpdated(userId);

            agreement.Status = AgreementStatus.Active;
            agreement.CancellationEffectiveOn = null;
            agreement.LeaveReason = null;
            agreement.EarlyTerminationFeeCharged = 0m;
            agreement.StampUpdated(userId);

            switch (offer.Kind)
            {
                case SaveOfferKind.FreezeInstead:
                    await FreezeAsync(new RequestFreezeDto
                    {
                        AgreementId = agreement.Id,
                        StartsOn = now.Date,
                        EndsOn = now.Date.AddMonths(offer.PeriodCount ?? 1),
                        Reason = FreezeReason.Other,
                        ReasonNote = "Freeze offered in place of cancellation",
                        WaiveFee = true,
                    }, userId);
                    break;

                case SaveOfferKind.DowngradeInstead or SaveOfferKind.PlanChange when offer.AlternativePlanId is not null:
                    await ChangePlanAsync(new ChangePlanDto
                    {
                        AgreementId = agreement.Id,
                        NewPlanId = offer.AlternativePlanId.Value,
                        Reason = "Offered in place of cancellation",
                        WaiveChangeFee = true,
                    }, userId);
                    break;

                case SaveOfferKind.FreeMonth:
                    agreement.PromotionalPrice = 0m;
                    agreement.PromotionalPeriodsRemaining = offer.PeriodCount ?? 1;
                    await billing.RebuildScheduleAsync(agreement.Id, userId);
                    break;

                case SaveOfferKind.DiscountedPeriods when offer.DiscountValue is not null:
                    agreement.PromotionalPrice = Math.Max(0, agreement.Price - offer.DiscountValue.Value);
                    agreement.PromotionalPeriodsRemaining = offer.PeriodCount ?? 3;
                    await billing.RebuildScheduleAsync(agreement.Id, userId);
                    break;
            }

            var member = await db.Members.ForTenant(tenant).FirstAsync(m => m.Id == agreement.MemberId);
            member.Status = agreement.Status == AgreementStatus.Frozen ? MemberStatus.Frozen : MemberStatus.Active;
            member.StampUpdated(userId);

            db.MemberNotes.Add(new MemberNote
            {
                MemberId = agreement.MemberId,
                Kind = InteractionKind.SystemEvent,
                Body = $"Cancellation withdrawn — accepted: {offer.Summary}",
                OccurredAt = now,
                IsPinned = true,
            }.StampNew(tenant, userId));
        }

        await db.SaveChangesAsync();

        var saved = await db.CancellationRequests.ForTenant(tenant)
            .Include(c => c.Agreement)
            .Include(c => c.Offers.Where(o => !o.IsDeleted))
            .FirstAsync(c => c.Id == offer.CancellationRequestId);

        return FitnessMapper.ToDto(saved);
    }

    public async Task<AgreementDetailDto> ProcessCancellationAsync(Guid requestId, Guid userId)
    {
        var now = DateTime.UtcNow;

        var cancellation = await db.CancellationRequests.ForTenant(tenant)
            .Include(c => c.Agreement)
            .FirstOrDefaultAsync(c => c.Id == requestId)
            ?? throw new InvalidOperationException("Cancellation request not found.");

        if (cancellation.WasSaved)
            throw new InvalidOperationException("This member was saved — the request no longer applies.");

        var agreement = cancellation.Agreement
            ?? throw new InvalidOperationException("Agreement not found.");

        agreement.Status = AgreementStatus.Cancelled;
        agreement.CancelledOn = now;
        agreement.EndsOn = cancellation.EffectiveOn;
        agreement.NextBillingOn = null;
        agreement.StampUpdated(userId);

        // Nothing further is billed after the effective date.
        var future = await db.BillingSchedules.ForTenant(tenant)
            .Where(s => s.AgreementId == agreement.Id && !s.IsBilled && s.DueOn >= cancellation.EffectiveOn)
            .ToListAsync();

        foreach (var charge in future)
        {
            charge.IsSkipped = true;
            charge.SkipReason = "Membership cancelled";
            charge.StampUpdated(userId);
        }

        if (cancellation.EarlyTerminationFee > 0)
        {
            await billing.CreateAdHocInvoiceAsync(agreement.MemberId, agreement.ClubId, [
                new InvoiceLineDto
                {
                    ChargeKind = ChargeKind.EarlyTerminationFee,
                    LineDescription = "Early termination fee",
                    Quantity = 1,
                    UnitPrice = cancellation.EarlyTerminationFee,
                    LineTotal = cancellation.EarlyTerminationFee,
                },
            ], userId);
        }

        if (cancellation.RefundDue > 0)
        {
            await billing.IssueRefundAsync(new IssueRefundDto
            {
                MemberId = agreement.MemberId,
                Amount = cancellation.RefundDue,
                Reason = "Cancelled within the cooling-off period",
                ToOriginalMethod = true,
            }, userId);
        }

        // Cancel what they had booked beyond the end date, giving credits back.
        var bookings = await db.ClassBookings.ForTenant(tenant)
            .Where(k => k.MemberId == agreement.MemberId && k.Status == BookingStatus.Booked
                     && k.ClassOccurrence!.StartsAt > cancellation.EffectiveOn)
            .ToListAsync();

        foreach (var booking in bookings)
        {
            booking.Status = BookingStatus.Cancelled;
            booking.CancelledAt = now;
            booking.StampUpdated(userId);
        }

        var member = await db.Members.ForTenant(tenant).FirstAsync(m => m.Id == agreement.MemberId);

        var otherActive = await db.Agreements.ForTenant(tenant)
            .AnyAsync(a => a.MemberId == member.Id && a.Id != agreement.Id && a.Status == AgreementStatus.Active);

        if (!otherActive)
        {
            member.Status = MemberStatus.Cancelled;
            member.LeftOn = cancellation.EffectiveOn;
            member.NextBillingOn = null;
        }

        member.StampUpdated(userId);

        cancellation.IsProcessed = true;
        cancellation.ProcessedByUserId = userId;

        // A win-back task, because a member who left in March is the cheapest lead the club will
        // see in September — and nobody remembers to call them without a prompt.
        if (!cancellation.WinBackTaskCreated)
        {
            db.RetentionTasks.Add(new RetentionTask
            {
                MemberId = member.Id,
                ClubId = agreement.ClubId,
                Title = $"Win-back call — left in {cancellation.EffectiveOn:MMMM}",
                Detail = $"Left because: {cancellation.Reason}. {cancellation.ReasonNote}",
                Trigger = "Cancellation",
                DueOn = cancellation.EffectiveOn.AddDays(90),
                Priority = 3,
            }.StampNew(tenant, userId));

            cancellation.WinBackTaskCreated = true;
        }

        cancellation.StampUpdated(userId);
        await db.SaveChangesAsync();

        return (await GetAsync(agreement.Id))!;
    }

    public async Task<List<CancellationRequestDto>> GetPendingCancellationsAsync(Guid? clubId)
    {
        var requests = await db.CancellationRequests.ForTenant(tenant)
            .Include(c => c.Agreement).ThenInclude(a => a!.Member)
            .Include(c => c.Agreement).ThenInclude(a => a!.Plan)
            .Include(c => c.Offers.Where(o => !o.IsDeleted))
            .Where(c => !c.IsProcessed && !c.WasSaved)
            .WhereIf(clubId is not null, c => c.Agreement!.ClubId == clubId)
            .OrderBy(c => c.EffectiveOn)
            .ToListAsync();

        return [.. requests.Select(c =>
        {
            var dto = FitnessMapper.ToDto(c);
            dto.MemberName = c.Agreement?.Member is null ? null : FitnessMapper.FullName(c.Agreement.Member);
            dto.PlanName = c.Agreement?.Plan?.Name;
            return dto;
        })];
    }

    /// <summary>Ends agreements whose cancellation date has arrived. Runs nightly.</summary>
    public async Task<int> ProcessDueCancellationsAsync()
    {
        var today = DateTime.UtcNow.Date;

        var due = await db.CancellationRequests.ForTenant(tenant)
            .Where(c => !c.IsProcessed && !c.WasSaved && c.EffectiveOn <= today)
            .Select(c => c.Id)
            .ToListAsync();

        foreach (var id in due) await ProcessCancellationAsync(id, Guid.Empty);
        return due.Count;
    }

    // ── Internals ────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds the forward billing schedule.
    ///
    /// Materialised rather than computed on demand because it is what a freeze shifts, an
    /// amendment rewrites, and a member asks about at the desk. A rolling membership gets a
    /// two-year horizon, which the nightly job extends.
    /// </summary>
    private async Task BuildScheduleAsync(Agreement agreement, MembershipPlan plan, decimal joiningFee, Guid userId)
    {
        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync() ?? new FitnessSettings();

        // Joining and admin fees are due immediately, in the first period.
        var upfront = joiningFee + plan.AdminFee + plan.CardFee;
        if (upfront > 0)
        {
            db.BillingSchedules.Add(new BillingSchedule
            {
                AgreementId = agreement.Id,
                MemberId = agreement.MemberId,
                ClubId = agreement.ClubId,
                DueOn = agreement.StartsOn,
                PeriodNumber = 0,
                PeriodStart = agreement.StartsOn,
                PeriodEnd = agreement.StartsOn,
                ChargeKind = ChargeKind.JoiningFee,
                Amount = upfront,
                TaxAmount = Math.Round(upfront * plan.TaxPercent / 100m, 2),
                CurrencyCode = agreement.CurrencyCode,
            }.StampNew(tenant, userId));
        }

        if (plan.BillingPeriod == BillingPeriod.OneOff)
        {
            db.BillingSchedules.Add(new BillingSchedule
            {
                AgreementId = agreement.Id,
                MemberId = agreement.MemberId,
                ClubId = agreement.ClubId,
                DueOn = agreement.StartsOn,
                PeriodNumber = 1,
                PeriodStart = agreement.StartsOn,
                PeriodEnd = agreement.EndsOn ?? agreement.StartsOn,
                ChargeKind = plan.Kind == PlanKind.SessionPack ? ChargeKind.SessionPackage : ChargeKind.MembershipDues,
                Amount = agreement.Price,
                TaxAmount = Math.Round(agreement.Price * plan.TaxPercent / 100m, 2),
                CurrencyCode = agreement.CurrencyCode,
            }.StampNew(tenant, userId));

            agreement.NextBillingOn = agreement.StartsOn;
            return;
        }

        // ── Recurring ────────────────────────────────────────────────────────

        var periodStart = agreement.StartsOn;
        var horizon = agreement.EndsOn ?? agreement.StartsOn.AddYears(2);
        var periodNumber = 1;
        var promoLeft = agreement.PromotionalPeriodsRemaining;

        // A fixed billing day means the first period is short, and is prorated to match.
        if (agreement.BillingAnchor == BillingAnchor.FixedDayOfMonth && agreement.BillingDayOfMonth is not null)
        {
            var firstFullStart = NextFixedDay(agreement.StartsOn, agreement.BillingDayOfMonth.Value);

            if (firstFullStart > agreement.StartsOn && plan.JoinProration != ProrationRule.None)
            {
                var days = (firstFullStart - agreement.StartsOn).Days;
                var periodDays = FitnessQueryExtensions.DaysIn(plan.BillingPeriod, agreement.StartsOn);
                var prorated = plan.JoinProration == ProrationRule.Daily
                    ? Math.Round(agreement.Price * days / periodDays, 2)
                    : agreement.Price;

                if (prorated > 0)
                {
                    db.BillingSchedules.Add(new BillingSchedule
                    {
                        AgreementId = agreement.Id,
                        MemberId = agreement.MemberId,
                        ClubId = agreement.ClubId,
                        DueOn = agreement.StartsOn,
                        PeriodNumber = 0,
                        PeriodStart = agreement.StartsOn,
                        PeriodEnd = firstFullStart.AddDays(-1),
                        ChargeKind = ChargeKind.ProRata,
                        Amount = prorated,
                        TaxAmount = Math.Round(prorated * plan.TaxPercent / 100m, 2),
                        CurrencyCode = agreement.CurrencyCode,
                        AdjustmentNote = $"{days} days at {agreement.Price:0.00} per {plan.BillingPeriod.ToString().ToLower()}",
                    }.StampNew(tenant, userId));
                }
            }

            periodStart = firstFullStart;
        }

        while (periodStart < horizon && periodNumber <= 60)
        {
            var periodEnd = periodStart.AddPeriod(plan.BillingPeriod).AddDays(-1);
            var amount = promoLeft > 0 ? agreement.PromotionalPrice ?? agreement.Price : agreement.Price;
            if (promoLeft > 0) promoLeft--;

            db.BillingSchedules.Add(new BillingSchedule
            {
                AgreementId = agreement.Id,
                MemberId = agreement.MemberId,
                ClubId = agreement.ClubId,
                DueOn = periodStart,
                PeriodNumber = periodNumber,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                ChargeKind = agreement.TotalInstalments > 0 ? ChargeKind.Instalment : ChargeKind.MembershipDues,
                Amount = amount,
                TaxAmount = Math.Round(amount * plan.TaxPercent / 100m, 2),
                CurrencyCode = agreement.CurrencyCode,
                OriginalAmount = amount != agreement.Price ? agreement.Price : null,
                AdjustmentNote = amount != agreement.Price ? "Promotional rate" : null,
            }.StampNew(tenant, userId));

            periodStart = periodStart.AddPeriod(plan.BillingPeriod);
            periodNumber++;

            if (agreement.TotalInstalments > 0 && periodNumber > agreement.TotalInstalments) break;
        }

        // The annual maintenance fee large clubs charge on a fixed month.
        if (plan.AnnualMaintenanceFee > 0 && plan.AnnualFeeMonth is not null)
        {
            var feeYear = agreement.StartsOn.Month <= plan.AnnualFeeMonth.Value
                ? agreement.StartsOn.Year
                : agreement.StartsOn.Year + 1;

            var feeDate = new DateTime(feeYear, plan.AnnualFeeMonth.Value, 1);

            db.BillingSchedules.Add(new BillingSchedule
            {
                AgreementId = agreement.Id,
                MemberId = agreement.MemberId,
                ClubId = agreement.ClubId,
                DueOn = feeDate,
                PeriodNumber = 100,
                PeriodStart = feeDate,
                PeriodEnd = feeDate.AddYears(1).AddDays(-1),
                ChargeKind = ChargeKind.AnnualMaintenanceFee,
                Amount = plan.AnnualMaintenanceFee,
                TaxAmount = Math.Round(plan.AnnualMaintenanceFee * plan.TaxPercent / 100m, 2),
                CurrencyCode = agreement.CurrencyCode,
            }.StampNew(tenant, userId));
        }

        agreement.NextBillingOn = agreement.StartsOn;
    }

    private static DateTime NextFixedDay(DateTime from, int dayOfMonth)
    {
        var day = Math.Clamp(dayOfMonth, 1, 28);
        var candidate = new DateTime(from.Year, from.Month, day);
        return candidate > from ? candidate : candidate.AddMonths(1);
    }

    /// <summary>
    /// Offers worth making, given why this member is leaving and what has worked before.
    ///
    /// The historic accept rate is per reason and per club, because "a free month" works on
    /// "too expensive" and does nothing at all for "moved away" — and a consultant reading a rate
    /// of 4% will stop wasting the offer.
    /// </summary>
    private async Task<List<SaveOfferSuggestionDto>> SuggestOffersAsync(
        Agreement agreement, CancellationPreviewDto preview, DateTime now)
    {
        var suggestions = new List<SaveOfferSuggestionDto>();

        var history = await db.SaveOffers.ForTenant(tenant)
            .Include(o => o.CancellationRequest)
            .Where(o => o.CancellationRequest!.Reason == agreement.LeaveReason && o.RespondedAt != null)
            .GroupBy(o => o.Kind)
            .Select(g => new { Kind = g.Key, Total = g.Count(), Accepted = g.Count(o => o.WasAccepted) })
            .ToListAsync();

        int RateFor(SaveOfferKind kind)
        {
            var row = history.FirstOrDefault(h => h.Kind == kind);
            return row is null || row.Total == 0 ? 0 : row.Accepted * 100 / row.Total;
        }

        var reason = agreement.LeaveReason;

        if (reason is LeaveReason.TooExpensive or LeaveReason.ChangedJob or null)
        {
            var cheaper = await db.Plans.ForTenant(tenant)
                .Where(p => p.IsActive && !p.IsPrivate && p.Kind == PlanKind.RecurringMembership && p.Price < agreement.Price)
                .OrderByDescending(p => p.Price)
                .FirstOrDefaultAsync();

            if (cheaper is not null)
            {
                suggestions.Add(new SaveOfferSuggestionDto
                {
                    Kind = SaveOfferKind.DowngradeInstead,
                    Label = $"Move to {cheaper.Name} at {cheaper.Price:0.00}",
                    Rationale = $"Saves {agreement.Price - cheaper.Price:0.00} a period and keeps them a member.",
                    AlternativePlanId = cheaper.Id,
                    Value = cheaper.Price,
                    HistoricAcceptRatePercent = RateFor(SaveOfferKind.DowngradeInstead),
                });
            }

            suggestions.Add(new SaveOfferSuggestionDto
            {
                Kind = SaveOfferKind.DiscountedPeriods,
                Label = "20% off for three months",
                Rationale = "A short discount usually costs less than replacing them.",
                Value = Math.Round(agreement.Price * 0.2m, 2),
                PeriodCount = 3,
                HistoricAcceptRatePercent = RateFor(SaveOfferKind.DiscountedPeriods),
            });
        }

        if (reason is LeaveReason.Injury or LeaveReason.Medical or LeaveReason.Pregnancy
                   or LeaveReason.TooBusy or LeaveReason.TemporaryBreak or LeaveReason.ChangedJob)
        {
            suggestions.Add(new SaveOfferSuggestionDto
            {
                Kind = SaveOfferKind.FreezeInstead,
                Label = "Freeze for three months instead",
                Rationale = "They want a break, not an ending — a freeze keeps the membership alive.",
                PeriodCount = 3,
                HistoricAcceptRatePercent = RateFor(SaveOfferKind.FreezeInstead),
            });
        }

        if (reason is LeaveReason.NotUsingIt or LeaveReason.ClassesNotSuitable)
        {
            suggestions.Add(new SaveOfferSuggestionDto
            {
                Kind = SaveOfferKind.FreePtSession,
                Label = "A free PT session and a fresh plan",
                Rationale = preview.UnusedCredits > 0
                    ? $"They still have {preview.UnusedCredits} unused credits — they lost momentum rather than interest."
                    : "Members who stop coming usually stopped knowing what to do.",
                HistoricAcceptRatePercent = RateFor(SaveOfferKind.FreePtSession),
            });
        }

        if (preview.IsInMinimumTerm && preview.EarlyTerminationFee > 0)
        {
            suggestions.Add(new SaveOfferSuggestionDto
            {
                Kind = SaveOfferKind.FreeMonth,
                Label = "A month free rather than charging the termination fee",
                Rationale = $"The fee is {preview.EarlyTerminationFee:0.00}. A month free costs {agreement.Price:0.00} and may keep them.",
                PeriodCount = 1,
                HistoricAcceptRatePercent = RateFor(SaveOfferKind.FreeMonth),
            });
        }

        return [.. suggestions.OrderByDescending(s => s.HistoricAcceptRatePercent)];
    }
}
