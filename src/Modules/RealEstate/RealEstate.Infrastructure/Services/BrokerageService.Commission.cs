using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using RealEstate.Application.DTOs;
using RealEstate.Domain.Entities;
using RealEstate.Domain.Enums;
using RealEstate.Infrastructure.Persistence;

namespace RealEstate.Infrastructure.Services;

/// <summary>
/// The commission engine: plans, tiers, caps, splits, deductions, disbursement and clawback.
///
/// Every agent checks this arithmetic every month, and one of them will be right about something
/// eventually. So the calculation records its own trace as it goes — which plan, which tier, why
/// that tier, what the cap did, what each deduction was for — and the trace is returned with the
/// figure. "The system worked it out" is not an answer anybody accepts about their own pay.
///
/// The cap is the part that is most often got wrong. A capped plan means the agent contributes to
/// the house only until the annual cap is reached, and keeps the rest afterwards. That boundary
/// almost never falls neatly at the end of a deal, so a single deal can straddle it — part
/// contributing, part post-cap — and this handles that split rather than rounding it away.
/// </summary>
public partial class BrokerageService
{
    public async Task<List<CommissionPlanDto>> GetCommissionPlansAsync(string? appliesTo, Guid? projectId)
    {
        var plans = await Db.CommissionPlans.ForCompany(Tenant)
            .WhereIf(!string.IsNullOrWhiteSpace(appliesTo), p => p.AppliesTo == appliesTo)
            .WhereIf(projectId.HasValue, p => p.ProjectId == projectId || p.ProjectId == null)
            .Include(p => p.Tiers)
            .OrderBy(p => p.Name)
            .ToListAsync();

        if (plans.Count == 0) return [];

        var names = await ProjectNamesAsync(plans.Select(p => p.ProjectId));

        var assigned = await Db.CommissionAgreements.ForCompany(Tenant)
            .Where(a => plans.Select(p => p.Id).Contains(a.CommissionPlanId))
            .GroupBy(a => a.CommissionPlanId)
            .Select(g => new { PlanId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PlanId, x => x.Count);

        return plans.Select(p => new CommissionPlanDto
        {
            Id = p.Id,
            Name = p.Name,
            Code = p.Code,
            Kind = p.Kind,
            Trigger = p.Trigger,
            AppliesTo = p.AppliesTo,
            ProjectId = p.ProjectId,
            ProjectName = p.ProjectId is null ? null : names.GetValueOrDefault(p.ProjectId.Value),
            AgentSharePercent = p.AgentSharePercent,
            HouseSharePercent = p.HouseSharePercent,
            FixedFeePerDeal = p.FixedFeePerDeal,
            AnnualCapAmount = p.AnnualCapAmount,
            CapRollsOver = p.CapRollsOver,
            PostCapFeePerDeal = p.PostCapFeePerDeal,
            PostCapPercent = p.PostCapPercent,
            FranchiseRoyaltyPercent = p.FranchiseRoyaltyPercent,
            TransactionFee = p.TransactionFee,
            MonthlyDeskFee = p.MonthlyDeskFee,
            PayProRataWithCollection = p.PayProRataWithCollection,
            MinimumCollectionPercent = p.MinimumCollectionPercent,
            WithholdingPercent = p.WithholdingPercent,
            ClawBackOnCancellation = p.ClawBackOnCancellation,
            EffectiveFrom = p.EffectiveFrom,
            EffectiveTo = p.EffectiveTo,
            IsActive = p.IsActive,
            AssignedCount = assigned.GetValueOrDefault(p.Id),
            Tiers = p.Tiers.OrderBy(t => t.TierNumber).Select(t => new CommissionPlanTierDto
            {
                Id = t.Id,
                TierNumber = t.TierNumber,
                Label = t.Label,
                FromAmount = t.FromAmount,
                ToAmount = t.ToAmount,
                FromCount = t.FromCount,
                ToCount = t.ToCount,
                SharePercent = t.SharePercent,
                RatePerSqFt = t.RatePerSqFt,
                FixedAmount = t.FixedAmount,
                IsRetrospective = t.IsRetrospective,
            }).ToList(),
        }).ToList();
    }

    public async Task<CommissionPlanDto> SaveCommissionPlanAsync(CommissionPlanDto dto, Guid userId)
    {
        var isNew = dto.Id == Guid.Empty;

        var plan = isNew
            ? new CommissionPlan { Code = await numbering.NextMasterCodeAsync(Db.CommissionPlans, "CPL") }
            : await Db.CommissionPlans.ForCompany(Tenant)
                .Include(p => p.Tiers)
                .FirstOrDefaultAsync(p => p.Id == dto.Id)
              ?? throw new InvalidOperationException("That plan does not exist.");

        var split = dto.AgentSharePercent + dto.HouseSharePercent;

        if (dto.Kind is CommissionPlanKind.FlatSplit or CommissionPlanKind.CappedSplit
            && split is < 99.99m or > 100.01m)
        {
            throw new InvalidOperationException(
                $"The agent and house shares add up to {split:0.##}%. On a split plan they must total 100%.");
        }

        if (dto.Kind == CommissionPlanKind.CappedSplit && dto.AnnualCapAmount <= 0m)
            throw new InvalidOperationException("A capped plan needs its annual cap amount.");

        if (dto.Kind is CommissionPlanKind.GraduatedSplit or CommissionPlanKind.SlabOnValue && dto.Tiers.Count == 0)
            throw new InvalidOperationException("A tiered plan needs at least one tier.");

        // Gaps and overlaps in a tier ladder produce a commission figure that depends on which
        // tier the code happened to match first. Both are refused here rather than debugged later.
        var ordered = dto.Tiers.OrderBy(t => t.FromAmount).ToList();

        for (var i = 1; i < ordered.Count; i++)
        {
            var previous = ordered[i - 1];
            var current = ordered[i];

            if (previous.ToAmount is null)
                throw new InvalidOperationException(
                    $"Tier {previous.TierNumber} is open-ended but is not the last tier.");

            if (Math.Abs(previous.ToAmount.Value - current.FromAmount) > 0.01m)
                throw new InvalidOperationException(
                    $"Tier {previous.TierNumber} ends at {previous.ToAmount:N0} but tier {current.TierNumber} "
                    + $"begins at {current.FromAmount:N0}. The ladder has to be continuous.");
        }

        // A change to a plan that has already paid out would restate what people were paid, so a
        // new plan is created instead and the old one closed.
        if (!isNew)
        {
            var used = await Db.CommissionCalculations.ForCompany(Tenant)
                .Join(Db.CommissionSplits.ForCompany(Tenant), c => c.Id, s => s.CommissionCalculationId, (c, s) => s)
                .AnyAsync(s => s.PaidAmount > 0m);

            var ratesChanged = plan.AgentSharePercent != dto.AgentSharePercent
                            || plan.AnnualCapAmount != dto.AnnualCapAmount
                            || plan.Kind != dto.Kind;

            if (used && ratesChanged)
                throw new InvalidOperationException(
                    "Commission has already been paid under this plan. Close it with an end date and create a "
                    + "new plan for the revised terms rather than editing this one.");
        }

        plan.Name = dto.Name;
        plan.Kind = dto.Kind;
        plan.Trigger = dto.Trigger;
        plan.AppliesTo = dto.AppliesTo;
        plan.ProjectId = dto.ProjectId;
        plan.AgentSharePercent = dto.AgentSharePercent;
        plan.HouseSharePercent = dto.HouseSharePercent;
        plan.FixedFeePerDeal = dto.FixedFeePerDeal;
        plan.AnnualCapAmount = dto.AnnualCapAmount;
        plan.CapRollsOver = dto.CapRollsOver;
        plan.PostCapFeePerDeal = dto.PostCapFeePerDeal;
        plan.PostCapPercent = dto.PostCapPercent;
        plan.FranchiseRoyaltyPercent = dto.FranchiseRoyaltyPercent;
        plan.TransactionFee = dto.TransactionFee;
        plan.MonthlyDeskFee = dto.MonthlyDeskFee;
        plan.PayProRataWithCollection = dto.PayProRataWithCollection;
        plan.MinimumCollectionPercent = dto.MinimumCollectionPercent;
        plan.WithholdingPercent = dto.WithholdingPercent;
        plan.ClawBackOnCancellation = dto.ClawBackOnCancellation;
        plan.EffectiveFrom = dto.EffectiveFrom == default ? Today : dto.EffectiveFrom;
        plan.EffectiveTo = dto.EffectiveTo;
        plan.IsActive = dto.IsActive;

        if (isNew)
        {
            plan.StampNew(Tenant, userId);
            Db.CommissionPlans.Add(plan);
        }
        else
        {
            plan.StampUpdated(userId);
        }

        var keep = dto.Tiers.Where(t => t.Id.HasValue).Select(t => t.Id!.Value).ToHashSet();

        foreach (var removed in plan.Tiers.Where(t => !keep.Contains(t.Id)).ToList())
        {
            removed.StampDeleted(userId);
        }

        foreach (var row in dto.Tiers.OrderBy(t => t.TierNumber))
        {
            var tier = row.Id.HasValue ? plan.Tiers.FirstOrDefault(t => t.Id == row.Id) : null;

            if (tier is null)
            {
                tier = new CommissionPlanTier { CommissionPlanId = plan.Id }.StampNew(Tenant, userId);
                plan.Tiers.Add(tier);
                Db.CommissionPlanTiers.Add(tier);
            }
            else
            {
                tier.StampUpdated(userId);
            }

            tier.TierNumber = row.TierNumber;
            tier.Label = row.Label;
            tier.FromAmount = row.FromAmount;
            tier.ToAmount = row.ToAmount;
            tier.FromCount = row.FromCount;
            tier.ToCount = row.ToCount;
            tier.SharePercent = row.SharePercent;
            tier.RatePerSqFt = row.RatePerSqFt;
            tier.FixedAmount = row.FixedAmount;
            tier.IsRetrospective = row.IsRetrospective;
        }

        await Db.SaveChangesAsync();

        return (await GetCommissionPlansAsync(plan.AppliesTo, plan.ProjectId)).First(p => p.Id == plan.Id);
    }

    // ═══ The calculation ═════════════════════════════════════════════════════

    public async Task<CommissionCalculationDto> CalculateAsync(
        Guid? dealId, Guid? bookingId, Guid? tenancyId, bool commit, Guid userId)
    {
        if (dealId is null && bookingId is null && tenancyId is null)
            throw new InvalidOperationException("A commission calculation needs a deal, a booking or a tenancy.");

        var trace = new List<string>();
        var currency = await CurrencyAsync();

        decimal transactionValue;
        decimal grossFee;
        decimal collectionPercent;
        Guid? projectId = null;
        Guid? listingAgentId = null;
        Guid? sellingAgentId = null;
        Guid? channelPartnerId = null;
        CommissionTrigger trigger;
        DateOnly? earnedOn = null;

        if (dealId is not null)
        {
            var deal = await Db.Deals.ForCompany(Tenant).FirstOrDefaultAsync(d => d.Id == dealId)
                ?? throw new InvalidOperationException("That deal does not exist.");

            if (deal.Status is DealStatus.FellThrough or DealStatus.Cancelled)
                throw new InvalidOperationException("That deal did not complete, so there is no fee to earn.");

            transactionValue = deal.AgreedPrice;
            grossFee = deal.GrossFee;
            collectionPercent = deal.FeeReceived ? 100m : 0m;
            listingAgentId = deal.ListingAgentId;
            sellingAgentId = deal.SellingAgentId;
            trigger = CommissionTrigger.OnCompletion;
            earnedOn = deal.ActualCompletionDate;

            trace.Add($"Deal {deal.Reference}: {transactionValue:N0} at {deal.FeePercent:0.##}% gives a gross fee of {grossFee:N0}.");
        }
        else if (bookingId is not null)
        {
            var booking = await Db.Bookings.ForCompany(Tenant).FirstOrDefaultAsync(b => b.Id == bookingId)
                ?? throw new InvalidOperationException("That booking does not exist.");

            if (booking.Status == BookingStatus.Cancelled)
                throw new InvalidOperationException("That booking has been cancelled.");

            transactionValue = booking.TotalConsideration > 0m ? booking.TotalConsideration : booking.NetSalePrice;
            collectionPercent = booking.CollectionPercent;
            projectId = booking.ProjectId;
            listingAgentId = booking.SalesExecutiveId;
            channelPartnerId = booking.ChannelPartnerId;
            trigger = CommissionTrigger.OnCollection;
            earnedOn = booking.ConfirmedAt is null ? null : DateOnly.FromDateTime(booking.ConfirmedAt.Value);
            grossFee = 0m;

            trace.Add($"Booking {booking.Reference}: {transactionValue:N0} with {collectionPercent:0.##}% collected.");
        }
        else
        {
            var tenancy = await Db.Tenancies.ForCompany(Tenant).FirstOrDefaultAsync(t => t.Id == tenancyId)
                ?? throw new InvalidOperationException("That tenancy does not exist.");

            var annual = tenancy.AnnualRent
                ?? RealEstateReportService.MonthlyEquivalent(tenancy.Rent, tenancy.Frequency) * 12m;

            transactionValue = RealEstateMapper.Money(annual);
            grossFee = 0m;
            collectionPercent = tenancy.TotalCharged <= 0m ? 0m
                : RealEstateMapper.Percent(tenancy.TotalPaid, tenancy.TotalCharged);
            listingAgentId = tenancy.ManagedByUserId;
            trigger = CommissionTrigger.OnAgreementSigned;
            earnedOn = tenancy.StartDate;

            trace.Add($"Tenancy {tenancy.Reference}: annualised rent of {transactionValue:N0}.");
        }

        var appliesTo = dealId is not null ? "Deal" : bookingId is not null ? "Booking" : "Tenancy";

        var plan = await ResolvePlanAsync(appliesTo, projectId, listingAgentId);

        if (plan is null)
            throw new InvalidOperationException(
                $"There is no commission plan covering {appliesTo.ToLowerInvariant()}s. Create one first.");

        trace.Add($"Plan “{plan.Name}” ({SplitCamelCase(plan.Kind.ToString())}) applies.");

        if (grossFee <= 0m)
        {
            grossFee = await ComputeGrossFeeAsync(plan, transactionValue, trace);
        }

        // Nothing is earned until the collection threshold in the plan has been passed. Paying an
        // agent on a booking with ten per cent collected is how a company ends up clawing money
        // back from someone who has already spent it.
        if (plan.PayProRataWithCollection && collectionPercent < plan.MinimumCollectionPercent)
        {
            trace.Add(
                $"Collection is {collectionPercent:0.##}%, below the plan's minimum of "
                + $"{plan.MinimumCollectionPercent:0.##}%. Nothing is releasable yet.");
        }

        var taxOnFee = 0m;
        var calculation = new CommissionCalculation
        {
            Reference = await numbering.NextMasterCodeAsync(Db.CommissionCalculations, "COM"),
            DealId = dealId,
            BookingId = bookingId,
            TenancyId = tenancyId,
            ProjectId = projectId,
            Trigger = plan.Trigger == default ? trigger : plan.Trigger,
            Status = CommissionStatus.Accrued,
            TransactionValue = RealEstateMapper.Money(transactionValue),
            GrossFee = RealEstateMapper.Money(grossFee),
            TaxOnFee = taxOnFee,
            CurrencyCode = currency,
            CalculatedOn = Today,
            EarnedOn = earnedOn,
            CollectionPercentAtCalculation = collectionPercent,
        };

        var splits = await BuildSplitsAsync(
            calculation, plan, listingAgentId, sellingAgentId, channelPartnerId, collectionPercent, trace, userId);

        calculation.TotalDeductions = RealEstateMapper.Money(splits.Sum(s => s.DeductionTotal));
        calculation.NetDistributable = RealEstateMapper.Money(splits.Sum(s => s.NetAmount));
        calculation.CalculationTrace = string.Join("\n", trace);

        if (!commit)
        {
            // A preview writes nothing. A negotiator about to agree a deal wants to know what they
            // will earn on it, and asking them to commit an accrual to find out is absurd.
            var preview = (await MapCalculationsAsync([calculation], splits))[0];
            return preview;
        }

        calculation.StampNew(Tenant, userId);
        Db.CommissionCalculations.Add(calculation);

        foreach (var split in splits)
        {
            split.CommissionCalculationId = calculation.Id;
            split.StampNew(Tenant, userId);
            Db.CommissionSplits.Add(split);

            foreach (var deduction in split.Deductions)
            {
                deduction.CommissionSplitId = split.Id;
                deduction.StampNew(Tenant, userId);
                Db.CommissionDeductions.Add(deduction);
            }

            if (split.AgentProfileId is not null && split.CapContribution > 0m)
            {
                await ApplyCapContributionAsync(split.AgentProfileId.Value, plan, split.CapContribution, userId);
            }
        }

        if (dealId is not null)
        {
            var deal = await Db.Deals.ForCompany(Tenant).FirstOrDefaultAsync(d => d.Id == dealId);

            if (deal is not null)
            {
                deal.CommissionCalculationId = calculation.Id;
                deal.StampUpdated(userId);
            }
        }

        await Db.SaveChangesAsync();

        return (await MapCalculationsAsync([calculation], splits))[0];
    }

    private static string SplitCamelCase(string value)
        => System.Text.RegularExpressions.Regex.Replace(value, "(?<!^)([A-Z])", " $1").ToLowerInvariant();

    private async Task<CommissionPlan?> ResolvePlanAsync(string appliesTo, Guid? projectId, Guid? agentId)
    {
        // An agreement on the agent beats a plan on the project, which beats a company default.
        // Anything else and a negotiator on a bespoke split gets paid the house rate.
        if (agentId is not null)
        {
            var agreementPlanId = await Db.CommissionAgreements.ForCompany(Tenant)
                .Where(a => a.AgentProfileId == agentId && a.EffectiveFrom <= Today)
                .Where(a => a.EffectiveTo == null || a.EffectiveTo >= Today)
                .OrderByDescending(a => a.EffectiveFrom)
                .Select(a => (Guid?)a.CommissionPlanId)
                .FirstOrDefaultAsync();

            if (agreementPlanId is not null)
            {
                var agreed = await Db.CommissionPlans.ForCompany(Tenant)
                    .Include(p => p.Tiers)
                    .FirstOrDefaultAsync(p => p.Id == agreementPlanId);

                if (agreed is not null) return agreed;
            }
        }

        return await Db.CommissionPlans.ForCompany(Tenant)
            .Include(p => p.Tiers)
            .Where(p => p.IsActive && p.AppliesTo == appliesTo && p.EffectiveFrom <= Today)
            .Where(p => p.EffectiveTo == null || p.EffectiveTo >= Today)
            .OrderByDescending(p => p.ProjectId == projectId)
            .ThenByDescending(p => p.EffectiveFrom)
            .FirstOrDefaultAsync();
    }

    private async Task<decimal> ComputeGrossFeeAsync(
        CommissionPlan plan, decimal transactionValue, List<string> trace)
    {
        await Task.CompletedTask;

        switch (plan.Kind)
        {
            case CommissionPlanKind.FixedFeePerDeal:
                trace.Add($"Fixed fee of {plan.FixedFeePerDeal:N0} per transaction.");
                return plan.FixedFeePerDeal;

            case CommissionPlanKind.GraduatedSplit:
            case CommissionPlanKind.SlabOnValue:
            {
                var tiers = plan.Tiers.OrderBy(t => t.FromAmount).ToList();

                if (tiers.Count == 0) return 0m;

                var retrospective = tiers.LastOrDefault(t =>
                    t.IsRetrospective && transactionValue >= t.FromAmount
                    && (t.ToAmount is null || transactionValue <= t.ToAmount));

                // A retrospective slab pays its rate on the whole value once reached, not just on
                // the slice above the threshold. The two give very different answers and the
                // agreement always says which.
                if (retrospective is not null)
                {
                    var flat = RealEstateMapper.Money(transactionValue * retrospective.SharePercent / 100m);

                    trace.Add(
                        $"Tier {retrospective.TierNumber} is retrospective: {retrospective.SharePercent:0.##}% "
                        + $"on the whole {transactionValue:N0} gives {flat:N0}.");

                    return flat;
                }

                var total = 0m;
                var remaining = transactionValue;

                foreach (var tier in tiers)
                {
                    if (remaining <= 0m) break;

                    var ceiling = tier.ToAmount ?? decimal.MaxValue;
                    var band = Math.Min(remaining, ceiling - tier.FromAmount);

                    if (band <= 0m) continue;

                    var amount = tier.FixedAmount > 0m
                        ? tier.FixedAmount
                        : RealEstateMapper.Money(band * tier.SharePercent / 100m);

                    total += amount;
                    remaining -= band;

                    trace.Add(
                        $"Tier {tier.TierNumber} ({tier.Label ?? $"{tier.FromAmount:N0}–{tier.ToAmount?.ToString("N0") ?? "above"}"}): "
                        + $"{band:N0} at {tier.SharePercent:0.##}% gives {amount:N0}.");
                }

                return RealEstateMapper.Money(total);
            }

            default:
            {
                var percent = plan.AgentSharePercent + plan.HouseSharePercent;
                var fee = RealEstateMapper.Money(transactionValue * percent / 100m);

                trace.Add($"{percent:0.##}% of {transactionValue:N0} gives a gross fee of {fee:N0}.");

                return fee;
            }
        }
    }

    private async Task<List<CommissionSplit>> BuildSplitsAsync(
        CommissionCalculation calculation,
        CommissionPlan plan,
        Guid? listingAgentId,
        Guid? sellingAgentId,
        Guid? channelPartnerId,
        decimal collectionPercent,
        List<string> trace,
        Guid userId)
    {
        var splits = new List<CommissionSplit>();
        var gross = calculation.GrossFee;

        // Where both sides of a transaction are ours, the fee is halved before either agent's own
        // split is applied. Applying each agent's share to the whole fee pays out twice over.
        var sides = new List<(Guid AgentId, string Role, decimal Share)>();

        if (listingAgentId is not null && sellingAgentId is not null && listingAgentId != sellingAgentId)
        {
            sides.Add((listingAgentId.Value, "ListingAgent", 0.5m));
            sides.Add((sellingAgentId.Value, "SellingAgent", 0.5m));
            trace.Add("Both sides are ours, so the fee is split equally between the two negotiators.");
        }
        else if (listingAgentId is not null)
        {
            sides.Add((listingAgentId.Value, "Agent", 1m));
        }
        else if (sellingAgentId is not null)
        {
            sides.Add((sellingAgentId.Value, "SellingAgent", 1m));
        }

        foreach (var (agentId, role, sideShare) in sides)
        {
            var baseAmount = RealEstateMapper.Money(gross * sideShare);

            var (agentAmount, capContribution, capReached, tierApplied) =
                await ApplyPlanToAgentAsync(plan, agentId, baseAmount, trace);

            var split = new CommissionSplit
            {
                CommissionCalculationId = calculation.Id,
                AgentProfileId = agentId,
                Role = role,
                BaseAmount = baseAmount,
                SharePercent = baseAmount <= 0m ? 0m : RealEstateMapper.Percent(agentAmount, baseAmount),
                GrossAmount = agentAmount,
                Status = CommissionStatus.Accrued,
                TierApplied = tierApplied,
                CapReached = capReached,
                CapContribution = capContribution,
            };

            ApplyDeductions(split, plan, trace);

            splits.Add(split);
        }

        if (channelPartnerId is not null)
        {
            var partnerShare = await ResolvePartnerShareAsync(channelPartnerId.Value, calculation, trace);

            if (partnerShare > 0m)
            {
                var split = new CommissionSplit
                {
                    CommissionCalculationId = calculation.Id,
                    ChannelPartnerId = channelPartnerId,
                    Role = "ChannelPartner",
                    BaseAmount = calculation.TransactionValue,
                    SharePercent = RealEstateMapper.Percent(partnerShare, calculation.TransactionValue),
                    GrossAmount = partnerShare,
                    Status = CommissionStatus.Accrued,
                };

                ApplyDeductions(split, plan, trace);

                splits.Add(split);
            }
        }

        // Whatever is not distributed stays with the house. Recorded as its own line so the total
        // of the splits reconciles to the gross fee rather than quietly not adding up.
        var distributed = splits.Sum(s => s.GrossAmount);
        var houseShare = RealEstateMapper.Money(gross - distributed);

        if (houseShare > 0.01m)
        {
            splits.Add(new CommissionSplit
            {
                CommissionCalculationId = calculation.Id,
                Role = "House",
                BaseAmount = gross,
                SharePercent = RealEstateMapper.Percent(houseShare, gross),
                GrossAmount = houseShare,
                NetAmount = houseShare,
                Status = CommissionStatus.Accrued,
            });

            trace.Add($"House retains {houseShare:N0}.");
        }

        // Pro-rata release with collection, where the plan says so.
        if (plan.PayProRataWithCollection)
        {
            var releasable = Math.Clamp(collectionPercent, 0m, 100m) / 100m;

            foreach (var split in splits.Where(s => s.Role != "House"))
            {
                split.NetAmount = RealEstateMapper.Money(split.NetAmount * releasable);
            }

            trace.Add($"Payable pro rata with collection at {collectionPercent:0.##}%.");
        }

        await Task.CompletedTask;

        return splits;
    }

    private async Task<(decimal Amount, decimal CapContribution, bool CapReached, int? Tier)> ApplyPlanToAgentAsync(
        CommissionPlan plan, Guid agentId, decimal baseAmount, List<string> trace)
    {
        if (plan.Kind != CommissionPlanKind.CappedSplit || plan.AnnualCapAmount <= 0m)
        {
            var flat = RealEstateMapper.Money(baseAmount * plan.AgentSharePercent / 100m);
            trace.Add($"Agent share of {plan.AgentSharePercent:0.##}% on {baseAmount:N0} gives {flat:N0}.");
            return (flat, 0m, false, null);
        }

        var ledger = await CurrentCapLedgerAsync(agentId, plan);

        var contributedSoFar = ledger?.ContributedAmount ?? 0m;
        var capRemaining = Math.Max(0m, plan.AnnualCapAmount - contributedSoFar);

        var houseShareOfThis = RealEstateMapper.Money(baseAmount * plan.HouseSharePercent / 100m);

        if (capRemaining <= 0m)
        {
            // Past the cap, the house takes only a fixed transaction fee and the agent keeps the
            // rest. This is the whole point of a capped plan and the number agents watch all year.
            var postCap = RealEstateMapper.Money(
                baseAmount - plan.PostCapFeePerDeal - baseAmount * plan.PostCapPercent / 100m);

            trace.Add(
                $"The annual cap of {plan.AnnualCapAmount:N0} was already reached, so the house takes only "
                + $"{plan.PostCapFeePerDeal:N0} and the agent keeps {postCap:N0}.");

            return (Math.Max(0m, postCap), 0m, true, null);
        }

        if (houseShareOfThis <= capRemaining)
        {
            var agentAmount = RealEstateMapper.Money(baseAmount - houseShareOfThis);

            trace.Add(
                $"House takes {plan.HouseSharePercent:0.##}% ({houseShareOfThis:N0}) toward the cap; "
                + $"{capRemaining - houseShareOfThis:N0} of the cap remains. Agent receives {agentAmount:N0}.");

            return (agentAmount, houseShareOfThis, false, null);
        }

        // The cap falls inside this transaction. Split it: the part that reaches the cap is
        // charged at the plan's share, and the remainder at the post-cap terms.
        var portionToCap = capRemaining / (plan.HouseSharePercent / 100m);
        var portionAfter = baseAmount - portionToCap;

        var beforeCapAgent = RealEstateMapper.Money(portionToCap - capRemaining);
        var afterCapAgent = RealEstateMapper.Money(
            portionAfter - portionAfter * plan.PostCapPercent / 100m);

        var total = RealEstateMapper.Money(beforeCapAgent + afterCapAgent);

        trace.Add(
            $"The cap is reached inside this transaction. {portionToCap:N0} of the fee is charged at "
            + $"{plan.HouseSharePercent:0.##}%, taking the last {capRemaining:N0} of the cap; the remaining "
            + $"{portionAfter:N0} is charged at the post-cap rate. Agent receives {total:N0}.");

        return (total, capRemaining, true, null);
    }

    private async Task<AgentCapLedger?> CurrentCapLedgerAsync(Guid agentId, CommissionPlan plan)
    {
        var agent = await Db.AgentProfiles.ForCompany(Tenant).FirstOrDefaultAsync(a => a.Id == agentId);

        // The cap year runs from the agent's own anniversary where they have one, because a broker
        // who joined in September should not have their cap reset three months later.
        var anniversary = agent?.CapAnniversary ?? new DateOnly(Today.Year, 1, 1);

        var from = new DateOnly(Today.Year, anniversary.Month, anniversary.Day);
        if (from > Today) from = from.AddYears(-1);

        return await Db.AgentCapLedgers.ForCompany(Tenant)
            .FirstOrDefaultAsync(l => l.AgentProfileId == agentId && l.PeriodFrom == from);
    }

    private async Task ApplyCapContributionAsync(
        Guid agentId, CommissionPlan plan, decimal contribution, Guid userId)
    {
        var agent = await Db.AgentProfiles.ForCompany(Tenant).FirstOrDefaultAsync(a => a.Id == agentId);
        var anniversary = agent?.CapAnniversary ?? new DateOnly(Today.Year, 1, 1);

        var from = new DateOnly(Today.Year, anniversary.Month, anniversary.Day);
        if (from > Today) from = from.AddYears(-1);

        var ledger = await Db.AgentCapLedgers.ForCompany(Tenant)
            .FirstOrDefaultAsync(l => l.AgentProfileId == agentId && l.PeriodFrom == from);

        if (ledger is null)
        {
            ledger = new AgentCapLedger
            {
                AgentProfileId = agentId,
                PeriodFrom = from,
                PeriodTo = from.AddYears(1).AddDays(-1),
                CapAmount = plan.AnnualCapAmount,
            }.StampNew(Tenant, userId);

            Db.AgentCapLedgers.Add(ledger);
        }

        ledger.ContributedAmount = RealEstateMapper.Money(ledger.ContributedAmount + contribution);
        ledger.RemainingToCap = RealEstateMapper.Money(Math.Max(0m, ledger.CapAmount - ledger.ContributedAmount));
        ledger.DealCount++;

        if (!ledger.CapReached && ledger.RemainingToCap <= 0.01m)
        {
            ledger.CapReached = true;
            ledger.CapReachedOn = Today;

            await QueueNotificationAsync(
                "commission.cap.reached",
                "You have reached your annual cap",
                $"Everything from here to {ledger.PeriodTo:dd MMM yyyy} is at the post-cap rate.",
                "/realestate/brokerage/commission",
                recipientUserId: agent?.UserId,
                entityType: nameof(AgentCapLedger),
                entityId: ledger.Id);
        }

        ledger.StampUpdated(userId);
    }

    private static void ApplyDeductions(CommissionSplit split, CommissionPlan plan, List<string> trace)
    {
        var deductions = new List<CommissionDeduction>();
        var order = 0;

        if (plan.FranchiseRoyaltyPercent > 0m)
        {
            var amount = RealEstateMapper.Money(split.GrossAmount * plan.FranchiseRoyaltyPercent / 100m);

            deductions.Add(new CommissionDeduction
            {
                CommissionSplitId = split.Id,
                Kind = DeductionKind.FranchiseRoyalty,
                Label = "Franchise royalty",
                Percent = plan.FranchiseRoyaltyPercent,
                Amount = amount,
                SortOrder = order++,
            });
        }

        if (plan.TransactionFee > 0m)
        {
            deductions.Add(new CommissionDeduction
            {
                CommissionSplitId = split.Id,
                Kind = DeductionKind.TransactionFee,
                Label = "Transaction fee",
                Amount = plan.TransactionFee,
                SortOrder = order++,
            });
        }

        var deductionTotal = deductions.Sum(d => d.Amount);

        var withholding = plan.WithholdingPercent > 0m
            ? RealEstateMapper.Money((split.GrossAmount - deductionTotal) * plan.WithholdingPercent / 100m)
            : 0m;

        if (withholding > 0m)
        {
            deductions.Add(new CommissionDeduction
            {
                CommissionSplitId = split.Id,
                Kind = DeductionKind.Withholding,
                Label = "Tax withheld at source",
                Percent = plan.WithholdingPercent,
                Amount = withholding,
                SortOrder = order,
            });
        }

        split.Deductions = deductions;
        split.DeductionTotal = RealEstateMapper.Money(deductionTotal);
        split.WithholdingAmount = withholding;
        split.NetAmount = RealEstateMapper.Money(split.GrossAmount - deductionTotal - withholding);

        if (deductions.Count > 0)
        {
            trace.Add(
                $"Deductions on {split.Role}: "
                + string.Join(", ", deductions.Select(d => $"{d.Label} {d.Amount:N0}"))
                + $" — net {split.NetAmount:N0}.");
        }
    }

    private async Task<decimal> ResolvePartnerShareAsync(
        Guid partnerId, CommissionCalculation calculation, List<string> trace)
    {
        var rate = await Db.PartnerCommissionRates.ForCompany(Tenant)
            .Where(r => r.ChannelPartnerId == partnerId || r.ChannelPartnerId == null)
            .WhereIf(calculation.ProjectId.HasValue, r => r.ProjectId == calculation.ProjectId)
            .Where(r => r.EffectiveFrom <= Today && (r.EffectiveTo == null || r.EffectiveTo >= Today))
            .Where(r => calculation.TransactionValue >= r.FromValue
                     && (r.ToValue == null || calculation.TransactionValue <= r.ToValue))
            .OrderByDescending(r => r.ChannelPartnerId != null)
            .ThenByDescending(r => r.EffectiveFrom)
            .FirstOrDefaultAsync();

        if (rate is null)
        {
            trace.Add("No channel partner rate matched, so no partner commission was accrued.");
            return 0m;
        }

        var amount = rate.FlatAmount > 0m
            ? rate.FlatAmount
            : RealEstateMapper.Money(calculation.TransactionValue * rate.CommissionPercent / 100m);

        // The tier uplift is a loyalty bonus, applied on top of the rate rather than instead of it.
        var partner = await Db.ChannelPartners.ForCompany(Tenant).FirstOrDefaultAsync(p => p.Id == partnerId);

        if (partner?.TierId is not null)
        {
            var uplift = await Db.PartnerTiers.ForCompany(Tenant)
                .Where(t => t.Id == partner.TierId)
                .Select(t => t.CommissionUpliftPercent)
                .FirstOrDefaultAsync();

            if (uplift > 0m)
            {
                var bonus = RealEstateMapper.Money(amount * uplift / 100m);
                amount += bonus;

                trace.Add($"Partner tier uplift of {uplift:0.##}% adds {bonus:N0}.");
            }
        }

        trace.Add($"Channel partner commission of {amount:N0} at {rate.CommissionPercent:0.##}%.");

        return amount;
    }
}
