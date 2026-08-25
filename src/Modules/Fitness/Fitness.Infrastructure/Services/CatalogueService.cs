using Fitness.Application.DTOs;
using Fitness.Application.Services.Interfaces;
using Fitness.Domain.Entities;
using Fitness.Domain.Enums;
using Fitness.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Events;
using AppointmentServiceEntity = Fitness.Domain.Entities.AppointmentService;

namespace Fitness.Infrastructure.Services;

/// <summary>
/// Everything the club can sell: plans, prices, entitlements, promotions and services.
///
/// The one rule that runs through it: **changing a plan does not change what existing members
/// pay.** A price rise bumps the plan version and applies to new joins only, unless an operator
/// explicitly asks for it to apply to live agreements — which is a decision with legal weight in
/// several markets, not a side effect of editing a form.
/// </summary>
public class CatalogueService(
    FitnessDbContext db,
    IFitnessTenant tenant) : ICatalogueService
{
    // ── Plans ────────────────────────────────────────────────────────────────

    public async Task<List<MembershipPlanDto>> GetPlansAsync(Guid? clubId, PlanKind? kind, bool sellableOnly)
    {
        var now = DateTime.UtcNow;

        var plans = await db.Plans.ForTenant(tenant)
            .WhereIf(kind is not null, p => p.Kind == kind)
            .WhereIf(sellableOnly, p => p.IsActive && !p.IsPrivate
                                     && (p.SellableFrom == null || p.SellableFrom <= now)
                                     && (p.SellableTo == null || p.SellableTo >= now))
            .WhereIf(clubId is not null, p => p.RestrictedToClubId == null || p.RestrictedToClubId == clubId)
            .Include(p => p.ClubPrices.Where(c => !c.IsDeleted))
            .Include(p => p.Entitlements.Where(e => !e.IsDeleted)).ThenInclude(e => e.TimeBands.Where(t => !t.IsDeleted))
            .OrderBy(p => p.Kind).ThenBy(p => p.DisplayOrder).ThenBy(p => p.Name)
            .ToListAsync();

        var ids = plans.Select(p => p.Id).ToList();
        var monthAgo = now.AddDays(-30);

        var agreements = await db.Agreements.ForTenant(tenant)
            .Where(a => ids.Contains(a.PlanId))
            .Select(a => new { a.PlanId, a.Status, a.Price, a.BillingPeriod, a.StartsOn })
            .ToListAsync();

        var clubNames = await db.Clubs.ForTenant(tenant)
            .Select(c => new { c.Id, c.Name })
            .ToDictionaryAsync(c => c.Id, c => c.Name);

        return [.. plans.Select(p =>
        {
            var dto = FitnessMapper.ToDto(p);

            var active = agreements.Where(a => a.PlanId == p.Id && a.Status == AgreementStatus.Active).ToList();
            dto.ActiveMemberCount = active.Count;
            dto.MonthlyRecurringRevenue = Math.Round(
                active.Sum(a => FitnessQueryExtensions.ToMonthly(a.Price, a.BillingPeriod)), 2);
            dto.SoldLast30Days = agreements.Count(a => a.PlanId == p.Id && a.StartsOn >= monthAgo);

            foreach (var price in dto.ClubPrices) price.ClubName = clubNames.GetValueOrDefault(price.ClubId);

            return dto;
        })];
    }

    public async Task<MembershipPlanDto?> GetPlanAsync(Guid planId)
    {
        var plan = await db.Plans.ForTenant(tenant)
            .Include(p => p.ClubPrices.Where(c => !c.IsDeleted))
            .Include(p => p.Entitlements.Where(e => !e.IsDeleted)).ThenInclude(e => e.TimeBands.Where(t => !t.IsDeleted))
            .FirstOrDefaultAsync(p => p.Id == planId);

        if (plan is null) return null;

        var dto = FitnessMapper.ToDto(plan);

        dto.ActiveMemberCount = await db.Agreements.ForTenant(tenant)
            .CountAsync(a => a.PlanId == planId && a.Status == AgreementStatus.Active);

        var clubNames = await db.Clubs.ForTenant(tenant)
            .Select(c => new { c.Id, c.Name })
            .ToDictionaryAsync(c => c.Id, c => c.Name);

        foreach (var price in dto.ClubPrices) price.ClubName = clubNames.GetValueOrDefault(price.ClubId);

        return dto;
    }

    public async Task<MembershipPlanDto> SavePlanAsync(Guid? planId, SavePlanDto request, Guid userId)
    {
        MembershipPlan plan;
        var priceChanged = false;
        decimal oldPrice = 0;

        if (planId is null)
        {
            plan = new MembershipPlan().StampNew(tenant, userId);
            db.Plans.Add(plan);
        }
        else
        {
            plan = await db.Plans.ForTenant(tenant)
                .Include(p => p.ClubPrices.Where(c => !c.IsDeleted))
                .Include(p => p.Entitlements.Where(e => !e.IsDeleted)).ThenInclude(e => e.TimeBands)
                .FirstOrDefaultAsync(p => p.Id == planId)
                ?? throw new InvalidOperationException("Plan not found.");

            oldPrice = plan.Price;
            priceChanged = plan.Price != request.Price;

            // The version is what an agreement records, so it has to move when terms move.
            if (priceChanged || plan.MinimumTermMonths != request.MinimumTermMonths
                             || plan.NoticePeriodDays != request.NoticePeriodDays)
                plan.Version++;

            plan.StampUpdated(userId);
        }

        Apply(plan, request);

        // ── Club prices ──────────────────────────────────────────────────────

        var existingPrices = plan.ClubPrices.Where(c => !c.IsDeleted).ToList();
        var keptPriceIds = request.ClubPrices.Where(c => c.Id != Guid.Empty).Select(c => c.Id).ToHashSet();

        foreach (var gone in existingPrices.Where(c => !keptPriceIds.Contains(c.Id))) gone.StampDeleted(userId);

        foreach (var priceDto in request.ClubPrices)
        {
            var price = existingPrices.FirstOrDefault(c => c.Id == priceDto.Id);
            if (price is null)
            {
                price = new PlanPrice { PlanId = plan.Id }.StampNew(tenant, userId);
                db.PlanPrices.Add(price);
            }
            else
            {
                price.StampUpdated(userId);
            }

            price.ClubId = priceDto.ClubId;
            price.Price = priceDto.Price;
            price.JoiningFee = priceDto.JoiningFee;
            price.CurrencyCode = priceDto.CurrencyCode;
            price.EffectiveFrom = priceDto.EffectiveFrom;
            price.EffectiveTo = priceDto.EffectiveTo;
            price.IsAvailable = priceDto.IsAvailable;
        }

        // ── Entitlements ─────────────────────────────────────────────────────

        foreach (var existing in plan.Entitlements.Where(e => !e.IsDeleted))
        {
            foreach (var band in existing.TimeBands.Where(t => !t.IsDeleted)) band.StampDeleted(userId);
            existing.StampDeleted(userId);
        }

        foreach (var entitlementDto in request.Entitlements)
        {
            var entitlement = new PlanEntitlement
            {
                PlanId = plan.Id,
                Kind = entitlementDto.Kind,
                TargetId = entitlementDto.TargetId,
                TargetName = entitlementDto.TargetName,
                Limit = entitlementDto.Limit,
                Quantity = entitlementDto.Quantity,
                OverageFee = entitlementDto.OverageFee,
                AllowOverage = entitlementDto.AllowOverage,
            }.StampNew(tenant, userId);

            db.PlanEntitlements.Add(entitlement);

            foreach (var bandDto in entitlementDto.TimeBands)
            {
                db.AccessTimeBands.Add(new AccessTimeBand
                {
                    EntitlementId = entitlement.Id,
                    Name = bandDto.Name,
                    DaysOfWeekMask = bandDto.DaysOfWeekMask,
                    StartsAt = bandDto.StartsAt,
                    EndsAt = bandDto.EndsAt,
                }.StampNew(tenant, userId));
            }
        }

        await db.SaveChangesAsync();

        // ── Repricing live agreements ────────────────────────────────────────

        if (priceChanged && request.ApplyPriceChangeToExisting && planId is not null)
            await RepriceExistingAsync(plan, oldPrice, userId);

        return (await GetPlanAsync(plan.Id))!;
    }

    public async Task DeletePlanAsync(Guid planId, Guid userId)
    {
        var plan = await db.Plans.ForTenant(tenant).FirstOrDefaultAsync(p => p.Id == planId)
            ?? throw new InvalidOperationException("Plan not found.");

        var active = await db.Agreements.ForTenant(tenant)
            .CountAsync(a => a.PlanId == planId && a.Status == AgreementStatus.Active);

        if (active > 0)
            throw new InvalidOperationException(
                $"{active} members are on this plan. Move them first, or mark it inactive so it stops being sold.");

        plan.StampDeleted(userId);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// What can be sold at this club today, priced for it.
    ///
    /// One call rather than five, because a join wizard and a till both need the whole catalogue
    /// at once and neither wants to assemble it from four requests.
    /// </summary>
    public async Task<SalesCatalogueDto> GetSalesCatalogueAsync(Guid clubId)
    {
        var now = DateTime.UtcNow;

        var club = await db.Clubs.ForTenant(tenant).FirstOrDefaultAsync(c => c.Id == clubId)
            ?? throw new InvalidOperationException("Club not found.");

        var plans = await GetPlansAsync(clubId, null, sellableOnly: true);

        // Resolve to the club's own price so the wizard shows what will actually be charged.
        foreach (var plan in plans)
        {
            var clubPrice = plan.ClubPrices.FirstOrDefault(p => p.ClubId == clubId && p.IsAvailable);
            if (clubPrice is null) continue;

            plan.Price = clubPrice.Price;
            if (clubPrice.JoiningFee is not null) plan.JoiningFee = clubPrice.JoiningFee.Value;
        }

        var catalogue = new SalesCatalogueDto
        {
            ClubId = clubId,
            ClubName = club.Name,
            CurrencyCode = club.CurrencyCode,
            Memberships = [.. plans.Where(p => p.Kind is PlanKind.RecurringMembership or PlanKind.TermMembership)],
            Packs = [.. plans.Where(p => p.Kind == PlanKind.SessionPack)],
            Passes = [.. plans.Where(p => p.Kind == PlanKind.TimePass)],
            Services = await GetServicesAsync(clubId, true),
            ActivePromotions = await GetPromotionsAsync(clubId, true),
        };

        // Retail lines come from Inventory; Fitness never holds a second item master.
        catalogue.Products = [.. plans
            .Where(p => p.Kind == PlanKind.RetailProduct)
            .Select(p => new RetailProductDto
            {
                Id = p.Id,
                InventoryItemId = p.InventoryItemId,
                Name = p.Name,
                Category = "Pro shop",
                ImageUrl = p.ImageUrl,
                Price = p.Price,
                TaxPercent = p.TaxPercent,
                TracksStock = p.InventoryItemId is not null,
                IsActive = p.IsActive,
            })];

        return catalogue;
    }

    // ── Promotions ───────────────────────────────────────────────────────────

    public async Task<List<PromotionRuleDto>> GetPromotionsAsync(Guid? clubId, bool activeOnly)
    {
        var now = DateTime.UtcNow;

        var promotions = await db.Promotions.ForTenant(tenant)
            .WhereIf(clubId is not null, p => p.ClubId == clubId || p.ClubId == null)
            .WhereIf(activeOnly, p => p.IsActive
                                   && (p.ActiveFrom == null || p.ActiveFrom <= now)
                                   && (p.ActiveTo == null || p.ActiveTo >= now)
                                   && (p.MaxRedemptions == 0 || p.RedemptionCount < p.MaxRedemptions))
            .OrderBy(p => p.DisplayOrder).ThenBy(p => p.Name)
            .ToListAsync();

        var ids = promotions.Select(p => p.Id).ToList();

        var codes = await db.PromoCodes.ForTenant(tenant)
            .Where(c => ids.Contains(c.PromotionRuleId))
            .ToListAsync();

        var planNames = await db.Plans.ForTenant(tenant)
            .Select(p => new { p.Id, p.Name })
            .ToDictionaryAsync(p => p.Id, p => p.Name);

        return [.. promotions.Select(p =>
        {
            var dto = FitnessMapper.ToDto(p);
            if (p.PlanId is not null) dto.PlanName = planNames.GetValueOrDefault(p.PlanId.Value);
            dto.Codes = [.. codes.Where(c => c.PromotionRuleId == p.Id).Select(FitnessMapper.ToDto)];
            return dto;
        })];
    }

    public async Task<PromotionRuleDto> SavePromotionAsync(Guid? id, PromotionRuleDto request, Guid userId)
    {
        PromotionRule promotion;
        if (id is null)
        {
            promotion = new PromotionRule().StampNew(tenant, userId);
            db.Promotions.Add(promotion);
        }
        else
        {
            promotion = await db.Promotions.ForTenant(tenant).FirstOrDefaultAsync(p => p.Id == id)
                ?? throw new InvalidOperationException("Promotion not found.");
            promotion.StampUpdated(userId);
        }

        promotion.Name = request.Name;
        promotion.PlanId = request.PlanId;
        promotion.ClubId = request.ClubId;
        promotion.DiscountKind = request.DiscountKind;
        promotion.Value = request.Value;
        promotion.PeriodCount = request.PeriodCount;
        promotion.ActiveFrom = request.ActiveFrom;
        promotion.ActiveTo = request.ActiveTo;
        promotion.NewMembersOnly = request.NewMembersOnly;
        promotion.MaxRedemptions = request.MaxRedemptions;
        promotion.CampaignId = request.CampaignId;
        promotion.DisplayOrder = request.DisplayOrder;
        promotion.IsActive = request.IsActive;

        await db.SaveChangesAsync();

        // Codes are reconciled by their text, so an existing code keeps its redemption count.
        var existing = await db.PromoCodes.ForTenant(tenant)
            .Where(c => c.PromotionRuleId == promotion.Id)
            .ToListAsync();

        var keptTexts = request.Codes.Select(c => c.CodeText.ToUpperInvariant()).ToHashSet();

        foreach (var gone in existing.Where(c => !keptTexts.Contains(c.CodeText.ToUpperInvariant())))
            gone.StampDeleted(userId);

        foreach (var codeDto in request.Codes)
        {
            var text = codeDto.CodeText.Trim().ToUpperInvariant();
            var code = existing.FirstOrDefault(c => c.CodeText.ToUpperInvariant() == text);

            if (code is null)
            {
                var clash = await db.PromoCodes.ForTenant(tenant)
                    .AnyAsync(c => c.CodeText.ToUpper() == text && c.PromotionRuleId != promotion.Id);

                if (clash) throw new InvalidOperationException($"The code '{text}' is already in use.");

                code = new PromoCode { PromotionRuleId = promotion.Id, CodeText = text }.StampNew(tenant, userId);
                db.PromoCodes.Add(code);
            }
            else
            {
                code.StampUpdated(userId);
            }

            code.MaxUses = codeDto.MaxUses;
            code.OnePerMember = codeDto.OnePerMember;
            code.ExpiresOn = codeDto.ExpiresOn;
            code.IssuedToMemberId = codeDto.IssuedToMemberId;
            code.IsActive = codeDto.IsActive;
        }

        await db.SaveChangesAsync();
        return (await GetPromotionsAsync(promotion.ClubId, false)).First(p => p.Id == promotion.Id);
    }

    /// <summary>
    /// Checks a promotional code and works out what it is worth on this plan.
    ///
    /// Every failure returns a sentence rather than a boolean, because "that code has expired" and
    /// "that code is for new members only" send a consultant in completely different directions.
    /// </summary>
    public async Task<PromoCodeResultDto> CheckPromoCodeAsync(PromoCodeCheckDto request)
    {
        var now = DateTime.UtcNow;
        var text = (request.CodeText ?? string.Empty).Trim().ToUpperInvariant();

        var code = await db.PromoCodes.ForTenant(tenant)
            .Include(c => c.PromotionRule)
            .FirstOrDefaultAsync(c => c.CodeText.ToUpper() == text);

        if (code is null)
            return new PromoCodeResultDto { IsValid = false, Reason = "That code was not recognised." };

        if (!code.IsActive)
            return new PromoCodeResultDto { IsValid = false, Reason = "That code is no longer active." };

        if (code.ExpiresOn is not null && code.ExpiresOn < now)
            return new PromoCodeResultDto { IsValid = false, Reason = $"That code expired on {code.ExpiresOn:d MMM yyyy}." };

        if (code.MaxUses > 0 && code.UseCount >= code.MaxUses)
            return new PromoCodeResultDto { IsValid = false, Reason = "That code has been fully redeemed." };

        var promotion = code.PromotionRule;
        if (promotion is null || !promotion.IsActive)
            return new PromoCodeResultDto { IsValid = false, Reason = "The offer behind that code has ended." };

        if (promotion.ActiveFrom is not null && promotion.ActiveFrom > now)
            return new PromoCodeResultDto { IsValid = false, Reason = $"That offer starts on {promotion.ActiveFrom:d MMM yyyy}." };

        if (promotion.ActiveTo is not null && promotion.ActiveTo < now)
            return new PromoCodeResultDto { IsValid = false, Reason = "That offer has ended." };

        if (promotion.PlanId is not null && request.PlanId is not null && promotion.PlanId != request.PlanId)
        {
            var planName = await db.Plans.ForTenant(tenant)
                .Where(p => p.Id == promotion.PlanId).Select(p => p.Name).FirstOrDefaultAsync();

            return new PromoCodeResultDto { IsValid = false, Reason = $"That code only applies to {planName}." };
        }

        if (promotion.ClubId is not null && request.ClubId is not null && promotion.ClubId != request.ClubId)
            return new PromoCodeResultDto { IsValid = false, Reason = "That code is for a different club." };

        if (code.IssuedToMemberId is not null && code.IssuedToMemberId != request.MemberId)
            return new PromoCodeResultDto { IsValid = false, Reason = "That code was issued to somebody else." };

        if (request.MemberId is not null && promotion.NewMembersOnly)
        {
            var hasHistory = await db.Agreements.ForTenant(tenant)
                .AnyAsync(a => a.MemberId == request.MemberId);

            if (hasHistory)
                return new PromoCodeResultDto { IsValid = false, Reason = "That offer is for new members only." };
        }

        if (code.OnePerMember && request.MemberId is not null)
        {
            var used = await db.Agreements.ForTenant(tenant)
                .AnyAsync(a => a.MemberId == request.MemberId && a.PromoCodeId == code.Id);

            if (used)
                return new PromoCodeResultDto { IsValid = false, Reason = "That code has already been used on this account." };
        }

        var result = new PromoCodeResultDto
        {
            IsValid = true,
            PromotionRuleId = promotion.Id,
            PromotionName = promotion.Name,
            DiscountKind = promotion.DiscountKind,
            Value = promotion.Value,
            PeriodCount = promotion.PeriodCount,
        };

        if (request.PlanId is not null)
        {
            var plan = await db.Plans.ForTenant(tenant)
                .Include(p => p.ClubPrices.Where(c => !c.IsDeleted))
                .FirstOrDefaultAsync(p => p.Id == request.PlanId);

            if (plan is not null)
            {
                var basePrice = plan.ClubPrices.FirstOrDefault(c => c.ClubId == request.ClubId)?.Price ?? plan.Price;

                result.OriginalPrice = basePrice;
                result.DiscountedPrice = promotion.DiscountKind switch
                {
                    DiscountKind.Percentage => Math.Round(basePrice * (1 - promotion.Value / 100m), 2),
                    DiscountKind.FixedAmount => Math.Max(0, basePrice - promotion.Value),
                    DiscountKind.OverridePrice => promotion.Value,
                    DiscountKind.FreePeriods => 0m,
                    _ => basePrice,
                };

                result.SavingPerPeriod = basePrice - result.DiscountedPrice;
            }
        }

        return result;
    }

    // ── Services ─────────────────────────────────────────────────────────────

    public async Task<List<AppointmentServiceDto>> GetServicesAsync(Guid? clubId, bool activeOnly)
    {
        var services = await db.Services.ForTenant(tenant)
            .WhereIf(activeOnly, s => s.IsActive)
            .OrderBy(s => s.DisplayOrder).ThenBy(s => s.Name)
            .ToListAsync();

        return [.. services.Select(FitnessMapper.ToDto)];
    }

    public async Task<AppointmentServiceDto> SaveServiceAsync(Guid? id, AppointmentServiceDto request, Guid userId)
    {
        AppointmentServiceEntity service;
        if (id is null)
        {
            service = new AppointmentServiceEntity().StampNew(tenant, userId);
            db.Services.Add(service);
        }
        else
        {
            service = await db.Services.ForTenant(tenant).FirstOrDefaultAsync(s => s.Id == id)
                ?? throw new InvalidOperationException("Service not found.");
            service.StampUpdated(userId);
        }

        service.Name = request.Name;
        service.Kind = request.Kind;
        service.DurationMinutes = request.DurationMinutes;
        service.BufferMinutes = request.BufferMinutes;
        service.Price = request.Price;
        service.TaxPercent = request.TaxPercent;
        service.MaxParticipants = Math.Max(1, request.MaxParticipants);
        service.RequiredResourceId = request.RequiredResourceId;
        service.RequiredRoomId = request.RequiredRoomId;
        service.FreeCancelHours = request.FreeCancelHours;
        service.LateCancelOutcome = request.LateCancelOutcome;
        service.NoShowOutcome = request.NoShowOutcome;
        service.ColourHex = request.ColourHex;
        service.Description = request.Description;
        service.DisplayOrder = request.DisplayOrder;
        service.BookableOnline = request.BookableOnline;
        service.IsActive = request.IsActive;

        await db.SaveChangesAsync();
        return FitnessMapper.ToDto(service);
    }

    public async Task<List<PlanChangePathDto>> GetChangePathsAsync(Guid fromPlanId)
    {
        var paths = await db.PlanChangePaths.ForTenant(tenant)
            .Where(p => p.FromPlanId == fromPlanId)
            .ToListAsync();

        var planNames = await db.Plans.ForTenant(tenant)
            .Select(p => new { p.Id, p.Name })
            .ToDictionaryAsync(p => p.Id, p => p.Name);

        return [.. paths.Select(p =>
        {
            var dto = FitnessMapper.ToDto(p);
            dto.FromPlanName = planNames.GetValueOrDefault(p.FromPlanId);
            dto.ToPlanName = planNames.GetValueOrDefault(p.ToPlanId);
            return dto;
        })];
    }

    // ═══ Internals ═══════════════════════════════════════════════════════════

    /// <summary>
    /// Applies a plan price change to agreements already running.
    ///
    /// Skips anyone whose rate is locked, and notes the change on every member it touches — a
    /// price rise a member first learns about from their bank statement is a cancellation.
    /// </summary>
    private async Task RepriceExistingAsync(MembershipPlan plan, decimal oldPrice, Guid userId)
    {
        var now = DateTime.UtcNow;

        var agreements = await db.Agreements.ForTenant(tenant)
            .Where(a => a.PlanId == plan.Id && a.Status == AgreementStatus.Active && !a.PriceLocked)
            .ToListAsync();

        foreach (var agreement in agreements)
        {
            db.Amendments.Add(new AgreementAmendment
            {
                AgreementId = agreement.Id,
                Kind = AgreementChangeKind.PriceChange,
                EffectiveOn = agreement.NextBillingOn ?? now.Date,
                PreviousPrice = agreement.Price,
                NewPrice = plan.Price,
                Reason = $"Plan price changed from {oldPrice:0.00} to {plan.Price:0.00}",
                ApprovedByUserId = userId,
            }.StampNew(tenant, userId));

            agreement.Price = plan.Price;
            agreement.PlanVersion = plan.Version;
            agreement.StampUpdated(userId);

            db.MemberNotes.Add(new MemberNote
            {
                MemberId = agreement.MemberId,
                Kind = InteractionKind.SystemEvent,
                Body = $"Membership price changed from {oldPrice:0.00} to {plan.Price:0.00}, " +
                       $"effective {agreement.NextBillingOn:d MMM yyyy}.",
                OccurredAt = now,
            }.StampNew(tenant, userId));

            // The unbilled tail of the schedule has to move with it.
            var future = await db.BillingSchedules.ForTenant(tenant)
                .Where(s => s.AgreementId == agreement.Id && !s.IsBilled
                         && s.DueOn > now.Date
                         && (s.ChargeKind == ChargeKind.MembershipDues || s.ChargeKind == ChargeKind.Instalment))
                .ToListAsync();

            foreach (var row in future)
            {
                row.OriginalAmount ??= row.Amount;
                row.Amount = plan.Price;
                row.TaxAmount = Math.Round(plan.Price * plan.TaxPercent / 100m, 2);
                row.AdjustmentNote = "Plan price change";
                row.StampUpdated(userId);
            }
        }

        await db.SaveChangesAsync();
    }

    private static void Apply(MembershipPlan p, SavePlanDto r)
    {
        p.Code = r.Code;
        p.Name = r.Name;
        p.Kind = r.Kind;
        p.MarketingBlurb = r.MarketingBlurb;
        p.ImageUrl = r.ImageUrl;
        p.ColourHex = r.ColourHex;
        p.DisplayOrder = r.DisplayOrder;
        p.Price = r.Price;
        p.CurrencyCode = r.CurrencyCode;
        p.TaxPercent = r.TaxPercent;
        p.PriceIncludesTax = r.PriceIncludesTax;
        p.BillingPeriod = r.BillingPeriod;
        p.BillingAnchor = r.BillingAnchor;
        p.JoinProration = r.JoinProration;
        p.CancelProration = r.CancelProration;
        p.JoiningFee = r.JoiningFee;
        p.AdminFee = r.AdminFee;
        p.CardFee = r.CardFee;
        p.AnnualMaintenanceFee = r.AnnualMaintenanceFee;
        p.AnnualFeeMonth = r.AnnualFeeMonth;
        p.MinimumTermMonths = r.MinimumTermMonths;
        p.DurationMonths = r.DurationMonths;
        p.NoticePeriodDays = r.NoticePeriodDays;
        p.AutoRenews = r.AutoRenews;
        p.EarlyTerminationFee = r.EarlyTerminationFee;
        p.EarlyTerminationPercentOfRemaining = r.EarlyTerminationPercentOfRemaining;
        p.CreditCount = r.CreditCount;
        p.ValidForDays = r.ValidForDays;
        p.CreditsTransferable = r.CreditsTransferable;
        p.CreditsRefundable = r.CreditsRefundable;
        p.RestrictedToClubId = r.RestrictedToClubId;
        p.AllowsCrossClubAccess = r.AllowsCrossClubAccess;
        p.VisitsPerPeriod = r.VisitsPerPeriod;
        p.VisitLimitBasis = r.VisitLimitBasis;
        p.GuestPassesPerPeriod = r.GuestPassesPerPeriod;
        p.BookingWindowDays = r.BookingWindowDays;
        p.MaxConcurrentBookings = r.MaxConcurrentBookings;
        p.SellableFrom = r.SellableFrom;
        p.SellableTo = r.SellableTo;
        p.SellableAtDesk = r.SellableAtDesk;
        p.SellableOnline = r.SellableOnline;
        p.SellableInApp = r.SellableInApp;
        p.SellableAtKiosk = r.SellableAtKiosk;
        p.IsPrivate = r.IsPrivate;
        p.MinimumAge = r.MinimumAge;
        p.MaximumAge = r.MaximumAge;
        p.RequiredProof = r.RequiredProof;
        p.WaiverTemplateId = r.WaiverTemplateId;
        p.AgreementTemplateId = r.AgreementTemplateId;
        p.RequiresHealthScreening = r.RequiresHealthScreening;
        p.InventoryItemId = r.InventoryItemId;
        p.RecognitionBasis = r.RecognitionBasis;
        p.RevenueAccountId = r.RevenueAccountId;
        p.DeferredRevenueAccountId = r.DeferredRevenueAccountId;
        p.IsActive = r.IsActive;
        p.Description = r.Description;
    }
}
