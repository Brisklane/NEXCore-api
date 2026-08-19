using Distribution.Application.DTOs;
using Distribution.Application.Services.Interfaces;
using Distribution.Domain.Entities;
using Distribution.Domain.Enums;
using Distribution.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;

namespace Distribution.Infrastructure.Services;

/// <summary>
/// The trade scheme engine.
///
/// <see cref="EvaluateAsync"/> is the hot path — it runs on every quantity change at the counter —
/// and it is built around one requirement: **determinism**. The same basket, for the same outlet,
/// on the same date, must produce the same benefit every time, because six weeks later a
/// distributor will file a claim against exactly this calculation and the two numbers have to
/// agree. That is why evaluation orders schemes explicitly (priority, then scheme number), never
/// depends on database row order, and records what it decided rather than recomputing it later.
///
/// Evaluation runs in four passes:
///   1. **Gather** the schemes live for this outlet on this date.
///   2. **Compute** each scheme's benefit against the basket independently.
///   3. **Resolve stacking** — exclusive wins alone, best-of-group keeps one, combinable all apply.
///   4. **Cap** against per-order, per-outlet and remaining-budget ceilings.
/// </summary>
public class SchemeService(
    DistributionDbContext db,
    IDistributionTenant tenant,
    DistributionNumbering numbering) : ISchemeService
{
    // ═══ CRUD ════════════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<TradeSchemeDto>> ListSchemesAsync(
        string? search, TradeSchemeKind? kind, SchemeStatus? status, Guid? territoryId,
        bool? activeOnly, PaginationParams pagination)
    {
        var today = DateTime.UtcNow.Date;
        var query = db.Schemes.ForCompany(tenant)
            .WhereIf(kind.HasValue, s => s.Kind == kind)
            .WhereIf(status.HasValue, s => s.Status == status)
            .WhereIf(activeOnly == true, s => s.Status == SchemeStatus.Active
                                              && s.ValidFrom <= today && s.ValidTo >= today);

        if (territoryId.HasValue)
        {
            var scoped = db.SchemeScopes.ForCompany(tenant)
                .Where(sc => sc.TerritoryId == territoryId && !sc.IsExcluded)
                .Select(sc => sc.SchemeId);
            query = query.Where(s => scoped.Contains(s.Id) || !db.SchemeScopes.ForCompany(tenant).Any(x => x.SchemeId == s.Id));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(s => EF.Functions.ILike(s.Name, term) || EF.Functions.ILike(s.SchemeNumber, term));
        }

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(s => s.ValidFrom).ThenBy(s => s.Name)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Include(s => s.Slabs.Where(x => !x.IsDeleted))
            .ToListAsync();

        var dtos = rows.Select(r =>
        {
            var dto = r.ToDto();
            dto.Products = [];
            dto.Scopes = [];
            return dto;
        });

        return PaginatedResponse<TradeSchemeDto>.Ok(dtos, total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<TradeSchemeDto?> GetSchemeAsync(Guid schemeId)
    {
        var entity = await LoadFullAsync(schemeId);
        if (entity is null) return null;

        var dto = entity.ToDto();
        await DecorateScopeNamesAsync(dto);
        return dto;
    }

    public async Task<TradeSchemeDto> SaveSchemeAsync(Guid? schemeId, SaveTradeSchemeDto request, Guid userId)
    {
        Validate(request);

        TradeScheme entity;
        if (schemeId.HasValue)
        {
            entity = await LoadFullAsync(schemeId.Value)
                ?? throw new InvalidOperationException("That scheme no longer exists.");

            // Once a scheme has fired, its rules are the evidence behind every benefit it gave.
            // Editing them in place would silently rewrite what customers were promised.
            if (entity.ApplicationCount > 0)
                throw new InvalidOperationException(
                    "This scheme has already been applied to orders. Pause it and create a replacement instead of editing it.");

            entity.StampUpdated(userId);
        }
        else
        {
            entity = new TradeScheme().StampNew(tenant, userId);
            entity.SchemeNumber = await numbering.NextSchemeNumberAsync(DateTime.UtcNow);
            entity.Status = SchemeStatus.Draft;
            db.Schemes.Add(entity);
        }

        entity.Name = request.Name.Trim();
        entity.Kind = request.Kind;
        entity.SettlementMode = request.SettlementMode;
        entity.Stacking = request.Stacking;
        entity.StackingGroup = request.StackingGroup;
        entity.Priority = request.Priority;
        entity.ValidFrom = request.ValidFrom.Date;
        entity.ValidTo = request.ValidTo.Date;
        entity.ActiveDays = request.ActiveDays;
        entity.MinQuantity = request.MinQuantity;
        entity.MinValue = request.MinValue;
        entity.FreeQuantity = request.FreeQuantity;
        entity.FreeItemId = request.FreeItemId;
        entity.FreeItemUom = request.FreeItemUom;
        entity.DiscountPercent = request.DiscountPercent;
        entity.DiscountAmount = request.DiscountAmount;
        entity.MaxBenefitPerOrder = request.MaxBenefitPerOrder;
        entity.MaxBenefitPerOutlet = request.MaxBenefitPerOutlet;
        entity.IsRecurringPerBlock = request.IsRecurringPerBlock;
        entity.IsPeriodScheme = request.IsPeriodScheme;
        entity.PaymentWithinDays = request.PaymentWithinDays;
        entity.DisplayDurationDays = request.DisplayDurationDays;
        entity.DisplayPayout = request.DisplayPayout;
        entity.RequiresPhotoEvidence = request.RequiresPhotoEvidence;
        entity.CurrencyCode = request.CurrencyCode;
        entity.StopWhenBudgetExhausted = request.StopWhenBudgetExhausted;
        entity.CommunicationPackUrl = request.CommunicationPackUrl;
        entity.TermsAndConditions = request.TermsAndConditions;
        entity.Note = request.Note;

        var budgetDelta = request.BudgetAmount - entity.BudgetAmount;
        entity.BudgetAmount = request.BudgetAmount;

        if (request.FreeItemId.HasValue)
            entity.FreeItemName = request.Products
                .FirstOrDefault(p => p.ItemId == request.FreeItemId)?.ItemName ?? entity.FreeItemName;

        SyncSlabs(entity, request.Slabs, userId);
        SyncProducts(entity, request.Products, userId);
        SyncScopes(entity, request.Scopes, userId);

        await db.SaveChangesAsync();

        if (budgetDelta != 0)
            await WriteBudgetEntryAsync(entity, budgetDelta,
                schemeId.HasValue ? "Budget adjusted on edit" : "Initial budget", userId);

        return (await GetSchemeAsync(entity.Id))!;
    }

    public async Task<TradeSchemeDto> DecideSchemeAsync(Guid schemeId, SchemeDecisionDto request, Guid userId)
    {
        var entity = await db.Schemes.ForCompany(tenant).FirstOrDefaultAsync(s => s.Id == schemeId)
            ?? throw new InvalidOperationException("That scheme no longer exists.");

        if (entity.Status is not (SchemeStatus.Draft or SchemeStatus.PendingApproval))
            throw new InvalidOperationException("Only a draft or pending scheme can be approved or rejected.");

        if (request.IsApproved)
        {
            entity.Status = DateTime.UtcNow.Date >= entity.ValidFrom ? SchemeStatus.Active : SchemeStatus.Approved;
            entity.ApprovedAt = DateTime.UtcNow;
            entity.ApprovedByUserId = userId;
            entity.RejectionReason = null;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(request.Comment))
                throw new InvalidOperationException("Rejecting a scheme needs a reason.");
            entity.Status = SchemeStatus.Cancelled;
            entity.RejectionReason = request.Comment;
        }

        entity.StampUpdated(userId);
        await db.SaveChangesAsync();
        return (await GetSchemeAsync(schemeId))!;
    }

    public async Task<TradeSchemeDto> ChangeStatusAsync(Guid schemeId, SchemeStatus status, string? reason, Guid userId)
    {
        var entity = await db.Schemes.ForCompany(tenant).FirstOrDefaultAsync(s => s.Id == schemeId)
            ?? throw new InvalidOperationException("That scheme no longer exists.");

        if (status is SchemeStatus.Cancelled && string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Cancelling a scheme needs a reason.");

        entity.Status = status;
        if (!string.IsNullOrWhiteSpace(reason)) entity.RejectionReason = reason;
        entity.StampUpdated(userId);

        await db.SaveChangesAsync();
        return (await GetSchemeAsync(schemeId))!;
    }

    public async Task DeleteSchemeAsync(Guid schemeId, Guid userId)
    {
        var entity = await db.Schemes.ForCompany(tenant).FirstOrDefaultAsync(s => s.Id == schemeId)
            ?? throw new InvalidOperationException("That scheme no longer exists.");

        if (entity.ApplicationCount > 0)
            throw new InvalidOperationException(
                "This scheme has been applied to orders and cannot be deleted. Cancel it instead.");

        entity.StampDeleted(userId);
        await db.SaveChangesAsync();
    }

    // ═══ Evaluation ══════════════════════════════════════════════════════════

    public async Task<List<SchemeApplicationDto>> EvaluateAsync(
        Guid? outletId, Guid? partnerId, List<DistributionOrderLineDto> lines, DateTime asOf)
    {
        if (lines.Count == 0) return [];

        var schemes = await GatherApplicableAsync(outletId, partnerId, asOf);
        if (schemes.Count == 0) return [];

        // Pass 2: compute each scheme in isolation.
        var computed = new List<SchemeApplicationDto>();
        foreach (var scheme in schemes)
        {
            var result = Compute(scheme, lines, asOf);
            if (result is not null) computed.Add(result);
        }

        if (computed.Count == 0) return [];

        // Pass 3: stacking.
        var accepted = ResolveStacking(schemes, computed);

        // Pass 4: caps. Per-outlet history and remaining budget are both real ceilings.
        await ApplyCapsAsync(schemes, accepted, outletId, asOf);

        return accepted.Where(a => a.BenefitValue > 0 || a.FreeQuantity > 0).ToList();
    }

    public async Task<List<NextSlabHintDto>> GetNextSlabHintsAsync(
        Guid? outletId, Guid? partnerId, List<DistributionOrderLineDto> lines, DateTime asOf)
    {
        var schemes = await GatherApplicableAsync(outletId, partnerId, asOf);
        var hints = new List<NextSlabHintDto>();

        foreach (var scheme in schemes.Where(s => s.Slabs.Count > 0))
        {
            var qualifying = QualifyingLines(scheme, lines);
            if (qualifying.Count == 0) continue;

            var isValueBased = scheme.Kind == TradeSchemeKind.ValueSlab;
            var current = isValueBased
                ? qualifying.Sum(l => l.LineTotal)
                : qualifying.Sum(l => l.BaseQuantity);

            var slabs = scheme.Slabs.OrderBy(s => s.SlabNumber).ToList();
            var currentSlab = MatchSlab(slabs, current, isValueBased);
            var nextSlab = slabs.FirstOrDefault(s =>
                (isValueBased ? s.FromValue : s.FromQuantity) > current);

            if (nextSlab is null) continue;

            var threshold = isValueBased ? nextSlab.FromValue : nextSlab.FromQuantity;
            var shortfall = threshold - current;
            if (shortfall <= 0) continue;

            var currentBenefit = currentSlab is null ? 0 : SlabBenefitValue(currentSlab, qualifying);
            var nextBenefit = SlabBenefitValue(nextSlab, qualifying);

            var unitLabel = qualifying[0].Uom;
            hints.Add(new NextSlabHintDto
            {
                SchemeId = scheme.Id,
                SchemeName = scheme.Name,
                ItemId = qualifying.Count == 1 ? qualifying[0].ItemId : null,
                ItemName = qualifying.Count == 1 ? qualifying[0].ItemName : null,
                Uom = unitLabel,
                CurrentQuantity = current,
                RequiredQuantity = threshold,
                ShortfallQuantity = shortfall,
                CurrentBenefit = currentBenefit,
                NextBenefit = nextBenefit,
                ExtraBenefit = Math.Max(0, nextBenefit - currentBenefit),
                Message = isValueBased
                    ? $"Add {shortfall:N0} more in value to reach {nextSlab.Label ?? $"slab {nextSlab.SlabNumber}"}."
                    : $"Add {shortfall:N0} more to reach {nextSlab.Label ?? $"slab {nextSlab.SlabNumber}"}.",
            });
        }

        return hints.OrderByDescending(h => h.ExtraBenefit).ToList();
    }

    public async Task<List<TradeSchemeDto>> GetApplicableSchemesAsync(Guid? outletId, Guid? partnerId, DateTime asOf)
    {
        var schemes = await GatherApplicableAsync(outletId, partnerId, asOf);
        return schemes.Select(s => s.ToDto()).ToList();
    }

    // ═══ Simulation & performance ════════════════════════════════════════════

    public async Task<SchemeSimulationDto> SimulateAsync(
        Guid? schemeId, SaveTradeSchemeDto? draft, DateTime from, DateTime to)
    {
        TradeScheme scheme;

        if (schemeId.HasValue)
        {
            scheme = await LoadFullAsync(schemeId.Value)
                ?? throw new InvalidOperationException("That scheme no longer exists.");
        }
        else if (draft is not null)
        {
            Validate(draft);
            scheme = BuildTransient(draft);
        }
        else
        {
            throw new InvalidOperationException("Give a scheme to simulate, or a draft definition.");
        }

        // Replay the period's real orders against the scheme's rules. Not a projection — this is
        // what it would actually have cost, which is a far easier number to defend in a budget
        // meeting than a percentage guess.
        var orders = await db.Orders.ForTenant(tenant)
            .Include(o => o.Lines)
            .Where(o => o.OrderDate >= from && o.OrderDate <= to
                        && o.Status != DistributionOrderStatus.Cancelled
                        && o.Status != DistributionOrderStatus.Rejected
                        && o.Status != DistributionOrderStatus.Draft)
            .ToListAsync();

        var result = new SchemeSimulationDto
        {
            SchemeId = schemeId,
            SchemeName = scheme.Name,
            SimulatedFrom = from,
            SimulatedTo = to,
        };

        var slabBreakdown = new Dictionary<int, SchemeSimulationSlabDto>();
        var beneficiaries = new HashSet<Guid>();

        foreach (var order in orders)
        {
            var lines = order.Lines.Where(l => !l.IsDeleted).Select(l => l.ToDto()).ToList();
            var application = Compute(scheme, lines, order.OrderDate);
            if (application is null) continue;

            result.QualifyingOrderCount++;
            result.QualifyingSalesValue += application.QualifyingValue;
            result.QualifyingQuantity += application.QualifyingQuantity;
            result.EstimatedBenefitValue += application.BenefitValue;
            result.EstimatedDiscountCost += application.DiscountAmount + application.PayoutAmount;

            if (application.FreeQuantity > 0)
            {
                var unitCost = lines.FirstOrDefault(l => l.ItemId == application.FreeItemId)?.UnitCost
                               ?? lines.Average(l => l.UnitCost);
                result.EstimatedFreeGoodsCost += application.FreeQuantity * unitCost;
            }

            if (order.OutletId.HasValue) beneficiaries.Add(order.OutletId.Value);

            if (application.SlabNumber.HasValue)
            {
                var slabNo = application.SlabNumber.Value;
                if (!slabBreakdown.TryGetValue(slabNo, out var row))
                {
                    row = new SchemeSimulationSlabDto
                    {
                        SlabNumber = slabNo,
                        Label = scheme.Slabs.FirstOrDefault(s => s.SlabNumber == slabNo)?.Label,
                    };
                    slabBreakdown[slabNo] = row;
                }
                row.OrderCount++;
                row.QualifyingQuantity += application.QualifyingQuantity;
                row.BenefitValue += application.BenefitValue;
            }
        }

        result.BeneficiaryOutletCount = beneficiaries.Count;
        result.TotalEstimatedCost = result.EstimatedBenefitValue;
        result.CostAsPercentOfSales = DistributionMapper.Percent(result.TotalEstimatedCost, result.QualifyingSalesValue);

        // A 20% headroom on the replayed cost: schemes that work grow their own volume, and a
        // budget that runs out mid-month is worse than one that is slightly generous.
        result.SuggestedBudget = Math.Ceiling(result.TotalEstimatedCost * 1.2m);
        result.SlabBreakdown = slabBreakdown.Values.OrderBy(s => s.SlabNumber).ToList();

        return result;
    }

    public async Task<SchemePerformanceDto> GetPerformanceAsync(Guid schemeId)
    {
        var scheme = await LoadFullAsync(schemeId)
            ?? throw new InvalidOperationException("That scheme no longer exists.");

        var applications = await db.SchemeApplications.ForTenant(tenant)
            .Where(a => a.SchemeId == schemeId && !a.IsReversed)
            .ToListAsync();

        var dto = new SchemePerformanceDto
        {
            SchemeId = schemeId,
            SchemeName = scheme.Name,
            Kind = scheme.Kind,
            Status = scheme.Status,
            ValidFrom = scheme.ValidFrom,
            ValidTo = scheme.ValidTo,
            BudgetAmount = scheme.BudgetAmount,
            ConsumedAmount = scheme.ConsumedAmount,
            RemainingBudget = scheme.BudgetAmount - scheme.ConsumedAmount,
            ApplicationCount = applications.Count,
            BeneficiaryOutletCount = applications.Where(a => a.OutletId.HasValue).Select(a => a.OutletId!.Value).Distinct().Count(),
            QualifyingSalesValue = applications.Sum(a => a.QualifyingValue),
        };

        dto.EligibleOutletCount = await CountEligibleOutletsAsync(scheme);
        dto.RedemptionRatePercent = DistributionMapper.Percent(dto.BeneficiaryOutletCount, dto.EligibleOutletCount);

        // Baseline: the same span of days immediately before the scheme started. Crude, but it is
        // the comparison a trade marketer actually makes, and a seasonal adjustment would need a
        // year of history that most new SKUs do not have.
        var span = (scheme.ValidTo - scheme.ValidFrom).Days + 1;
        var baseFrom = scheme.ValidFrom.AddDays(-span);
        var baseTo = scheme.ValidFrom.AddDays(-1);

        var itemIds = scheme.Products.Where(p => p.ItemId.HasValue && !p.IsExcluded)
            .Select(p => p.ItemId!.Value).ToList();

        var baseline = await SalesForWindowAsync(baseFrom, baseTo, itemIds);
        var actual = await SalesForWindowAsync(scheme.ValidFrom, scheme.ValidTo, itemIds);

        dto.BaselineSalesValue = baseline.Value;
        dto.UpliftValue = actual.Value - baseline.Value;
        dto.UpliftPercent = DistributionMapper.Percent(dto.UpliftValue, baseline.Value);
        dto.IncrementalQuantity = actual.Quantity - baseline.Quantity;
        dto.CostPerIncrementalUnit = dto.IncrementalQuantity <= 0
            ? 0
            : Math.Round(scheme.ConsumedAmount / dto.IncrementalQuantity, 4);

        dto.ByItem = await ItemPerformanceAsync(scheme, itemIds, baseFrom, baseTo);

        return dto;
    }

    public async Task<List<SchemeBudgetEntryDto>> GetBudgetLedgerAsync(Guid schemeId)
        => await db.SchemeBudgetLedger.ForTenant(tenant)
            .Where(l => l.SchemeId == schemeId)
            .OrderByDescending(l => l.OccurredAt)
            .Select(l => new SchemeBudgetEntryDto
            {
                OccurredAt = l.OccurredAt,
                Amount = l.Amount,
                BalanceAfter = l.BalanceAfter,
                Reason = l.Reason,
                OrderId = l.OrderId,
                ClaimId = l.ClaimId,
            })
            .ToListAsync();

    public async Task<TradeSchemeDto> AdjustBudgetAsync(Guid schemeId, decimal amount, string reason, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("A budget change needs a reason.");

        var entity = await db.Schemes.ForCompany(tenant).FirstOrDefaultAsync(s => s.Id == schemeId)
            ?? throw new InvalidOperationException("That scheme no longer exists.");

        entity.BudgetAmount += amount;
        if (entity.BudgetAmount < entity.ConsumedAmount)
            throw new InvalidOperationException(
                "That would leave the budget below what has already been spent on this scheme.");

        // Topping up a scheme that stopped itself should start it again.
        if (entity.Status == SchemeStatus.Exhausted && entity.BudgetAmount > entity.ConsumedAmount)
            entity.Status = SchemeStatus.Active;

        entity.StampUpdated(userId);
        await db.SaveChangesAsync();
        await WriteBudgetEntryAsync(entity, amount, reason, userId);

        return (await GetSchemeAsync(schemeId))!;
    }

    public async Task<PaginatedResponse<SchemeApplicationDto>> ListApplicationsAsync(
        Guid? schemeId, Guid? outletId, Guid? partnerId, DateTime? from, DateTime? to,
        PaginationParams pagination)
    {
        var query = db.SchemeApplications.ForTenant(tenant)
            .WhereIf(schemeId.HasValue, a => a.SchemeId == schemeId)
            .WhereIf(outletId.HasValue, a => a.OutletId == outletId)
            .WhereIf(partnerId.HasValue, a => a.PartnerId == partnerId)
            .WhereIf(from.HasValue, a => a.AppliedAt >= from)
            .WhereIf(to.HasValue, a => a.AppliedAt <= to);

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(a => a.AppliedAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        var dtos = rows.Select(r => r.ToDto()).ToList();

        var outletIds = rows.Where(r => r.OutletId.HasValue).Select(r => r.OutletId!.Value).Distinct().ToList();
        var orderIds = rows.Where(r => r.OrderId.HasValue).Select(r => r.OrderId!.Value).Distinct().ToList();

        var outletNames = await db.Outlets.ForTenant(tenant)
            .Where(o => outletIds.Contains(o.Id)).ToDictionaryAsync(o => o.Id, o => o.Name);
        var orderNumbers = await db.Orders.ForTenant(tenant)
            .Where(o => orderIds.Contains(o.Id)).ToDictionaryAsync(o => o.Id, o => o.OrderNumber);

        foreach (var dto in dtos)
        {
            if (dto.OutletId.HasValue) dto.OutletName = outletNames.GetValueOrDefault(dto.OutletId.Value);
            if (dto.OrderId.HasValue) dto.OrderNumber = orderNumbers.GetValueOrDefault(dto.OrderId.Value);
        }

        return PaginatedResponse<SchemeApplicationDto>.Ok(dtos, total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<List<ClaimSummaryDto>> GenerateDeferredClaimsAsync(
        DateTime periodStart, DateTime periodEnd, Guid userId)
    {
        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();
        var slaDays = settings?.ClaimSettlementSlaDays ?? 15;

        // Deferred applications that have not yet become a claim. The system already knows what it
        // owes; making a distributor type it back would be theatre.
        var pending = await db.SchemeApplications.ForTenant(tenant)
            .Where(a => a.SettlementMode == SchemeSettlementMode.Deferred
                        && !a.IsSettled && a.ClaimId == null && !a.IsReversed
                        && a.AppliedAt >= periodStart && a.AppliedAt <= periodEnd)
            .ToListAsync();

        if (pending.Count == 0) return [];

        var created = new List<ChannelClaim>();

        foreach (var group in pending.GroupBy(a => new { a.PartnerId, a.SchemeId }))
        {
            if (group.Key.PartnerId is null) continue;

            var scheme = await db.Schemes.ForCompany(tenant).FirstOrDefaultAsync(s => s.Id == group.Key.SchemeId);

            var claim = new ChannelClaim
            {
                ClaimNumber = await numbering.NextClaimNumberAsync(DateTime.UtcNow),
                Kind = ClaimKind.Scheme,
                Status = ClaimStatus.Submitted,
                PartnerId = group.Key.PartnerId,
                SchemeId = group.Key.SchemeId,
                PeriodStart = periodStart,
                PeriodEnd = periodEnd,
                SubmittedOn = DateTime.UtcNow,
                CurrencyCode = scheme?.CurrencyCode ?? settings?.BaseCurrencyCode ?? "USD",
                IsSystemGenerated = true,
                Note = $"Raised automatically from {group.Count()} deferred scheme applications.",
            }.StampNew(tenant, userId);

            var order = 0;
            foreach (var application in group)
            {
                claim.Lines.Add(new ChannelClaimLine
                {
                    ClaimId = claim.Id,
                    DisplayOrder = order++,
                    ItemId = application.FreeItemId,
                    ItemName = application.FreeItemName,
                    SourceOrderId = application.OrderId,
                    SchemeApplicationId = application.Id,
                    Quantity = application.QualifyingQuantity,
                    ClaimedAmount = application.BenefitValue,
                    ComputedAmount = application.BenefitValue,
                    Uom = "PCS",
                }.StampNew(tenant, userId));

                application.ClaimId = claim.Id;
                application.StampUpdated(userId);
            }

            claim.ClaimedAmount = claim.Lines.Sum(l => l.ClaimedAmount);
            claim.ComputedAmount = claim.ClaimedAmount;
            claim.VarianceAmount = 0;

            claim.StatusEvents.Add(new ClaimStatusEvent
            {
                ClaimId = claim.Id,
                FromStatus = ClaimStatus.Draft,
                ToStatus = ClaimStatus.Submitted,
                OccurredAt = DateTime.UtcNow,
                ActorName = "System",
                Note = "Generated from deferred scheme applications.",
            }.StampNew(tenant, userId));

            db.Claims.Add(claim);
            created.Add(claim);
        }

        await db.SaveChangesAsync();
        return created.Select(c => c.ToSummary(slaDays)).ToList();
    }

    // ═══ The engine ══════════════════════════════════════════════════════════

    /// <summary>
    /// Pass 1 — every scheme live for this outlet on this date, in deterministic order.
    /// </summary>
    private async Task<List<TradeScheme>> GatherApplicableAsync(Guid? outletId, Guid? partnerId, DateTime asOf)
    {
        var date = asOf.Date;
        var weekday = ((int)date.DayOfWeek).ToString();

        var schemes = await db.Schemes.ForCompany(tenant)
            .Include(s => s.Slabs.Where(x => !x.IsDeleted))
            .Include(s => s.Products.Where(x => !x.IsDeleted))
            .Include(s => s.Scopes.Where(x => !x.IsDeleted))
            .Where(s => s.Status == SchemeStatus.Active && s.ValidFrom <= date && s.ValidTo >= date)
            .ToListAsync();

        // Context for scope matching, loaded once rather than per scheme.
        OutletChannel? channel = null;
        OutletGrade? grade = null;
        Guid? territoryId = null;
        PartnerType? tier = null;
        var routeIds = new List<Guid>();

        if (outletId.HasValue)
        {
            var o = await db.Outlets.ForTenant(tenant)
                .Where(x => x.Id == outletId)
                .Select(x => new { x.Channel, x.Grade, x.TerritoryId, x.PartnerId })
                .FirstOrDefaultAsync();

            if (o is not null)
            {
                channel = o.Channel;
                grade = o.Grade;
                territoryId = o.TerritoryId;
                partnerId ??= o.PartnerId;
            }

            routeIds = await db.RouteOutlets.ForTenant(tenant)
                .Where(r => r.OutletId == outletId).Select(r => r.RouteId).ToListAsync();
        }

        if (partnerId.HasValue)
        {
            var p = await db.Partners.ForTenant(tenant)
                .Where(x => x.Id == partnerId)
                .Select(x => new { x.PartnerType, x.TerritoryId })
                .FirstOrDefaultAsync();

            if (p is not null)
            {
                tier = p.PartnerType;
                territoryId ??= p.TerritoryId;
            }
        }

        return schemes
            .Where(s =>
            {
                if (!string.IsNullOrWhiteSpace(s.ActiveDays))
                {
                    var days = s.ActiveDays.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    if (!days.Contains(weekday)) return false;
                }

                if (s.StopWhenBudgetExhausted && s.BudgetAmount > 0 && s.ConsumedAmount >= s.BudgetAmount)
                    return false;

                return MatchesScope(s, outletId, partnerId, channel, grade, tier, territoryId, routeIds);
            })
            // Deterministic order: priority, then scheme number. Never database order.
            .OrderByDescending(s => s.Priority)
            .ThenBy(s => s.SchemeNumber, StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// An empty scope set means everyone. Exclusions always beat inclusions, so a national scheme
    /// with one carved-out wholesaler behaves the way the trade marketer described it.
    /// </summary>
    private static bool MatchesScope(
        TradeScheme scheme, Guid? outletId, Guid? partnerId, OutletChannel? channel,
        OutletGrade? grade, PartnerType? tier, Guid? territoryId, List<Guid> routeIds)
    {
        var scopes = scheme.Scopes.ToList();
        if (scopes.Count == 0) return true;

        bool Matches(TradeSchemeScope s) =>
            (s.OutletId is null || s.OutletId == outletId) &&
            (s.PartnerId is null || s.PartnerId == partnerId) &&
            (s.Channel is null || s.Channel == channel) &&
            (s.OutletGrade is null || s.OutletGrade == grade) &&
            (s.PartnerTier is null || s.PartnerTier == tier) &&
            (s.TerritoryId is null || s.TerritoryId == territoryId) &&
            (s.RouteId is null || routeIds.Contains(s.RouteId.Value)) &&
            // A scope row with nothing set matches nothing rather than everything; an all-null
            // row is a mistake, and treating it as "all" would silently widen the scheme.
            (s.OutletId is not null || s.PartnerId is not null || s.Channel is not null ||
             s.OutletGrade is not null || s.PartnerTier is not null || s.TerritoryId is not null ||
             s.RouteId is not null);

        if (scopes.Where(s => s.IsExcluded).Any(Matches)) return false;

        var inclusions = scopes.Where(s => !s.IsExcluded).ToList();
        return inclusions.Count == 0 || inclusions.Any(Matches);
    }

    /// <summary>
    /// Pass 2 — one scheme against one basket. Pure: no database, no clock beyond the date passed
    /// in, which is what makes the result reproducible when a claim is audited.
    /// </summary>
    private static SchemeApplicationDto? Compute(TradeScheme scheme, List<DistributionOrderLineDto> lines, DateTime asOf)
    {
        // Free-goods lines never earn further benefit; a scheme that pays on its own giveaway
        // compounds without limit.
        var qualifying = QualifyingLines(scheme, lines);
        if (qualifying.Count == 0) return null;

        var quantity = qualifying.Sum(l => l.BaseQuantity);
        var value = qualifying.Sum(l => l.LineTotal);

        var application = new SchemeApplicationDto
        {
            SchemeId = scheme.Id,
            SchemeName = scheme.Name,
            SchemeKind = scheme.Kind,
            AppliedAt = asOf,
            QualifyingQuantity = quantity,
            QualifyingValue = value,
            SettlementMode = scheme.SettlementMode,
        };

        switch (scheme.Kind)
        {
            case TradeSchemeKind.QuantityFreeGoods:
            {
                if (scheme.MinQuantity <= 0 || quantity < scheme.MinQuantity) return null;

                // Recurring pays per whole block ("buy 10 get 1" on 35 units pays 3), otherwise once.
                var blocks = scheme.IsRecurringPerBlock ? Math.Floor(quantity / scheme.MinQuantity) : 1;
                application.FreeQuantity = blocks * scheme.FreeQuantity;
                application.FreeItemId = scheme.FreeItemId ?? qualifying[0].ItemId;
                application.FreeItemName = scheme.FreeItemName ?? qualifying[0].ItemName;

                var unitPrice = qualifying.FirstOrDefault(l => l.ItemId == application.FreeItemId)?.UnitPrice
                                ?? qualifying[0].UnitPrice;
                application.BenefitValue = application.FreeQuantity * unitPrice;
                application.BenefitDescription =
                    $"{application.FreeQuantity:N0} × {application.FreeItemName} free on {quantity:N0} purchased";
                break;
            }

            case TradeSchemeKind.QuantitySlab:
            case TradeSchemeKind.ValueSlab:
            {
                var isValueBased = scheme.Kind == TradeSchemeKind.ValueSlab;
                var basis = isValueBased ? value : quantity;
                var slab = MatchSlab(scheme.Slabs.OrderBy(s => s.SlabNumber).ToList(), basis, isValueBased);
                if (slab is null) return null;

                application.SlabNumber = slab.SlabNumber;
                application.FreeQuantity = slab.FreeQuantity;
                application.FreeItemId = slab.FreeItemId ?? scheme.FreeItemId;
                application.FreeItemName = slab.FreeItemName ?? scheme.FreeItemName;
                application.PayoutAmount = slab.PayoutAmount;
                application.PointsAwarded = slab.PointsAwarded;
                application.DiscountAmount = slab.DiscountAmount > 0
                    ? slab.DiscountAmount
                    : Math.Round(value * slab.DiscountPercent / 100m, 4);

                var freeValue = slab.FreeQuantity > 0
                    ? slab.FreeQuantity * (qualifying.FirstOrDefault(l => l.ItemId == application.FreeItemId)?.UnitPrice
                                           ?? qualifying[0].UnitPrice)
                    : 0;

                application.BenefitValue = application.DiscountAmount + application.PayoutAmount + freeValue;
                application.BenefitDescription = slab.Label
                    ?? $"Slab {slab.SlabNumber} on {(isValueBased ? value : quantity):N0}";
                break;
            }

            case TradeSchemeKind.PercentageDiscount:
            {
                if (scheme.MinQuantity > 0 && quantity < scheme.MinQuantity) return null;
                if (scheme.MinValue > 0 && value < scheme.MinValue) return null;

                application.DiscountAmount = Math.Round(value * scheme.DiscountPercent / 100m, 4);
                application.BenefitValue = application.DiscountAmount;
                application.BenefitDescription = $"{scheme.DiscountPercent:N2}% off qualifying lines";
                break;
            }

            case TradeSchemeKind.FlatAmountOff:
            {
                if (scheme.MinQuantity > 0 && quantity < scheme.MinQuantity) return null;
                if (scheme.MinValue > 0 && value < scheme.MinValue) return null;

                var blocks = scheme.IsRecurringPerBlock && scheme.MinQuantity > 0
                    ? Math.Floor(quantity / scheme.MinQuantity)
                    : 1;

                application.DiscountAmount = Math.Round(scheme.DiscountAmount * blocks, 4);
                application.BenefitValue = application.DiscountAmount;
                application.BenefitDescription = $"{application.DiscountAmount:N2} off";
                break;
            }

            case TradeSchemeKind.ComboAssortment:
            {
                // Every required SKU must be present in at least its required quantity. The number
                // of complete baskets is the smallest ratio across the requirements.
                var required = scheme.Products.Where(p => p.IsQualifying && !p.IsExcluded && p.RequiredQuantity > 0).ToList();
                if (required.Count == 0) return null;

                decimal? baskets = null;
                foreach (var requirement in required)
                {
                    var have = qualifying
                        .Where(l => MatchesProduct(requirement, l))
                        .Sum(l => l.BaseQuantity);

                    if (have < requirement.RequiredQuantity) return null;

                    var possible = Math.Floor(have / requirement.RequiredQuantity);
                    baskets = baskets is null ? possible : Math.Min(baskets.Value, possible);
                }

                var count = scheme.IsRecurringPerBlock ? baskets ?? 1 : 1;
                application.FreeQuantity = scheme.FreeQuantity * count;
                application.FreeItemId = scheme.FreeItemId;
                application.FreeItemName = scheme.FreeItemName;
                application.DiscountAmount = Math.Round(scheme.DiscountAmount * count, 4);

                var comboFreeValue = application.FreeQuantity *
                    (qualifying.FirstOrDefault(l => l.ItemId == scheme.FreeItemId)?.UnitPrice ?? qualifying[0].UnitPrice);

                application.BenefitValue = application.DiscountAmount + comboFreeValue;
                application.BenefitDescription = $"{count:N0} × combo qualified";
                break;
            }

            case TradeSchemeKind.SamplingFreeIssue:
            {
                if (scheme.MinQuantity > 0 && quantity < scheme.MinQuantity) return null;
                application.FreeQuantity = scheme.FreeQuantity;
                application.FreeItemId = scheme.FreeItemId;
                application.FreeItemName = scheme.FreeItemName;
                application.BenefitValue = 0; // Zero-value issue: it costs stock, not revenue.
                application.BenefitDescription = $"{scheme.FreeQuantity:N0} × {scheme.FreeItemName} sample";
                break;
            }

            case TradeSchemeKind.Liquidation:
            {
                // Only pays on lines actually carrying near-expiry stock, which is the whole point.
                var nearExpiry = qualifying.Where(l => l.ExpiryDate is not null
                                                       && (l.ExpiryDate.Value - asOf).TotalDays <= 90).ToList();
                if (nearExpiry.Count == 0) return null;

                var nearValue = nearExpiry.Sum(l => l.LineTotal);
                application.QualifyingValue = nearValue;
                application.QualifyingQuantity = nearExpiry.Sum(l => l.BaseQuantity);
                application.DiscountAmount = Math.Round(nearValue * scheme.DiscountPercent / 100m, 4);
                application.BenefitValue = application.DiscountAmount;
                application.BenefitDescription = $"{scheme.DiscountPercent:N0}% liquidation on short-dated stock";
                break;
            }

            case TradeSchemeKind.LoyaltyPoints:
            {
                if (scheme.MinValue > 0 && value < scheme.MinValue) return null;
                var slab = MatchSlab(scheme.Slabs.OrderBy(s => s.SlabNumber).ToList(), value, true);
                application.PointsAwarded = slab?.PointsAwarded ?? scheme.FreeQuantity;
                application.SlabNumber = slab?.SlabNumber;
                application.BenefitValue = 0; // Points are a liability, not a discount on this order.
                application.BenefitDescription = $"{application.PointsAwarded:N0} points earned";
                break;
            }

            case TradeSchemeKind.TradeOfferBundle:
            {
                if (scheme.MinQuantity > 0 && quantity < scheme.MinQuantity) return null;
                application.DiscountAmount = scheme.DiscountAmount > 0
                    ? scheme.DiscountAmount
                    : Math.Round(value * scheme.DiscountPercent / 100m, 4);
                application.BenefitValue = application.DiscountAmount;
                application.BenefitDescription = scheme.Name;
                break;
            }

            // Cash discount and display schemes are not earned at order entry: one depends on when
            // the money arrives, the other on a photo taken weeks later. Both become claims.
            case TradeSchemeKind.CashDiscount:
            case TradeSchemeKind.Display:
            default:
                return null;
        }

        if (application.BenefitValue <= 0 && application.FreeQuantity <= 0 && application.PointsAwarded <= 0)
            return null;

        // Per-order ceiling, applied before stacking so a capped scheme competes on its real value.
        if (scheme.MaxBenefitPerOrder > 0 && application.BenefitValue > scheme.MaxBenefitPerOrder)
        {
            var ratio = scheme.MaxBenefitPerOrder / application.BenefitValue;
            application.DiscountAmount = Math.Round(application.DiscountAmount * ratio, 4);
            application.FreeQuantity = Math.Floor(application.FreeQuantity * ratio);
            application.PayoutAmount = Math.Round(application.PayoutAmount * ratio, 4);
            application.BenefitValue = scheme.MaxBenefitPerOrder;
            application.BenefitDescription += " (capped)";
        }

        return application;
    }

    /// <summary>
    /// Pass 3 — stacking. Exclusive wins alone; best-of-group keeps the single most valuable
    /// member; everything else combines.
    /// </summary>
    private static List<SchemeApplicationDto> ResolveStacking(
        List<TradeScheme> schemes, List<SchemeApplicationDto> computed)
    {
        var byId = schemes.ToDictionary(s => s.Id);

        var exclusive = computed
            .Where(a => byId[a.SchemeId].Stacking == SchemeStacking.Exclusive)
            .OrderByDescending(a => a.BenefitValue)
            .FirstOrDefault();

        // An exclusive scheme only wins if it is genuinely worth more than everything else put
        // together — otherwise the customer is worse off for having qualified for it.
        if (exclusive is not null)
        {
            var combinedRest = computed.Where(a => a != exclusive).Sum(a => a.BenefitValue);
            if (exclusive.BenefitValue >= combinedRest) return [exclusive];
        }

        var result = new List<SchemeApplicationDto>();
        var handledGroups = new HashSet<string>();

        foreach (var application in computed.OrderByDescending(a => a.BenefitValue))
        {
            var scheme = byId[application.SchemeId];

            if (scheme.Stacking == SchemeStacking.Exclusive) continue;

            if (scheme.Stacking == SchemeStacking.BestOfGroup)
            {
                var group = scheme.StackingGroup ?? scheme.Id.ToString();
                if (!handledGroups.Add(group)) continue;
            }

            result.Add(application);
        }

        return result;
    }

    /// <summary>
    /// Pass 4 — the ceilings that need history: what this outlet has already taken from the
    /// scheme, and what is left in the budget.
    /// </summary>
    private async Task ApplyCapsAsync(
        List<TradeScheme> schemes, List<SchemeApplicationDto> accepted, Guid? outletId, DateTime asOf)
    {
        var byId = schemes.ToDictionary(s => s.Id);

        foreach (var application in accepted.ToList())
        {
            var scheme = byId[application.SchemeId];

            if (scheme.MaxBenefitPerOutlet > 0 && outletId.HasValue)
            {
                var alreadyTaken = await db.SchemeApplications.ForTenant(tenant)
                    .Where(a => a.SchemeId == scheme.Id && a.OutletId == outletId && !a.IsReversed
                                && a.AppliedAt >= scheme.ValidFrom && a.AppliedAt <= scheme.ValidTo)
                    .SumAsync(a => (decimal?)a.BenefitValue) ?? 0;

                var headroom = scheme.MaxBenefitPerOutlet - alreadyTaken;
                if (headroom <= 0)
                {
                    accepted.Remove(application);
                    continue;
                }

                if (application.BenefitValue > headroom)
                    Scale(application, headroom / application.BenefitValue, headroom, "outlet cap reached");
            }

            if (scheme.StopWhenBudgetExhausted && scheme.BudgetAmount > 0)
            {
                var remaining = scheme.BudgetAmount - scheme.ConsumedAmount;
                if (remaining <= 0)
                {
                    accepted.Remove(application);
                    continue;
                }

                if (application.BenefitValue > remaining)
                    Scale(application, remaining / application.BenefitValue, remaining, "budget nearly exhausted");
            }
        }

        static void Scale(SchemeApplicationDto a, decimal ratio, decimal cappedValue, string why)
        {
            a.DiscountAmount = Math.Round(a.DiscountAmount * ratio, 4);
            a.PayoutAmount = Math.Round(a.PayoutAmount * ratio, 4);
            a.FreeQuantity = Math.Floor(a.FreeQuantity * ratio);
            a.BenefitValue = cappedValue;
            a.BenefitDescription += $" ({why})";
        }
    }

    // ═══ Helpers ═════════════════════════════════════════════════════════════

    private static List<DistributionOrderLineDto> QualifyingLines(
        TradeScheme scheme, List<DistributionOrderLineDto> lines)
    {
        var products = scheme.Products.ToList();
        var candidates = lines.Where(l => !l.IsFreeGoods && l.BaseQuantity > 0).ToList();

        // No product rows means the scheme covers everything.
        if (products.Count == 0) return candidates;

        var excluded = products.Where(p => p.IsExcluded).ToList();
        var included = products.Where(p => !p.IsExcluded && p.IsQualifying).ToList();

        return candidates
            .Where(l => excluded.All(p => !MatchesProduct(p, l)))
            .Where(l => included.Count == 0 || included.Any(p => MatchesProduct(p, l)))
            .ToList();
    }

    private static bool MatchesProduct(TradeSchemeProduct product, DistributionOrderLineDto line)
        => (product.ItemId is null || product.ItemId == line.ItemId)
           && (product.BrandId is null || product.BrandId == line.BrandId)
           && (product.CategoryId is null || product.CategoryId == line.CategoryId)
           // A row with no key at all matches nothing, for the same reason an empty scope does.
           && (product.ItemId is not null || product.BrandId is not null || product.CategoryId is not null);

    private static TradeSchemeSlab? MatchSlab(List<TradeSchemeSlab> slabs, decimal basis, bool isValueBased)
        => slabs.LastOrDefault(s =>
        {
            var from = isValueBased ? s.FromValue : s.FromQuantity;
            var to = isValueBased ? s.ToValue : s.ToQuantity;
            return basis >= from && (to is null || basis <= to);
        });

    private static decimal SlabBenefitValue(TradeSchemeSlab slab, List<DistributionOrderLineDto> qualifying)
    {
        var value = qualifying.Sum(l => l.LineTotal);
        var unitPrice = qualifying.Count > 0 ? qualifying[0].UnitPrice : 0;

        return Math.Round(
            (slab.DiscountAmount > 0 ? slab.DiscountAmount : value * slab.DiscountPercent / 100m)
            + slab.PayoutAmount
            + slab.FreeQuantity * unitPrice, 4);
    }

    private async Task<TradeScheme?> LoadFullAsync(Guid schemeId)
        => await db.Schemes.ForCompany(tenant)
            .Include(s => s.Slabs.Where(x => !x.IsDeleted))
            .Include(s => s.Products.Where(x => !x.IsDeleted))
            .Include(s => s.Scopes.Where(x => !x.IsDeleted))
            .FirstOrDefaultAsync(s => s.Id == schemeId);

    private static TradeScheme BuildTransient(SaveTradeSchemeDto draft)
    {
        var scheme = new TradeScheme
        {
            Name = draft.Name,
            Kind = draft.Kind,
            SettlementMode = draft.SettlementMode,
            Stacking = draft.Stacking,
            ValidFrom = draft.ValidFrom,
            ValidTo = draft.ValidTo,
            MinQuantity = draft.MinQuantity,
            MinValue = draft.MinValue,
            FreeQuantity = draft.FreeQuantity,
            FreeItemId = draft.FreeItemId,
            DiscountPercent = draft.DiscountPercent,
            DiscountAmount = draft.DiscountAmount,
            MaxBenefitPerOrder = draft.MaxBenefitPerOrder,
            MaxBenefitPerOutlet = draft.MaxBenefitPerOutlet,
            IsRecurringPerBlock = draft.IsRecurringPerBlock,
            CurrencyCode = draft.CurrencyCode,
        };

        foreach (var s in draft.Slabs)
            scheme.Slabs.Add(new TradeSchemeSlab
            {
                SlabNumber = s.SlabNumber,
                FromQuantity = s.FromQuantity,
                ToQuantity = s.ToQuantity,
                FromValue = s.FromValue,
                ToValue = s.ToValue,
                FreeQuantity = s.FreeQuantity,
                DiscountPercent = s.DiscountPercent,
                DiscountAmount = s.DiscountAmount,
                PayoutAmount = s.PayoutAmount,
                PointsAwarded = s.PointsAwarded,
                FreeItemId = s.FreeItemId,
                Label = s.Label,
            });

        foreach (var p in draft.Products)
            scheme.Products.Add(new TradeSchemeProduct
            {
                ItemId = p.ItemId,
                BrandId = p.BrandId,
                CategoryId = p.CategoryId,
                IsQualifying = p.IsQualifying,
                RequiredQuantity = p.RequiredQuantity,
                IsExcluded = p.IsExcluded,
            });

        foreach (var s in draft.Scopes)
            scheme.Scopes.Add(new TradeSchemeScope
            {
                Channel = s.Channel,
                OutletGrade = s.OutletGrade,
                PartnerTier = s.PartnerTier,
                TerritoryId = s.TerritoryId,
                RouteId = s.RouteId,
                PartnerId = s.PartnerId,
                OutletId = s.OutletId,
                IsExcluded = s.IsExcluded,
            });

        return scheme;
    }

    private static void Validate(SaveTradeSchemeDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("A scheme needs a name.");

        if (request.ValidTo < request.ValidFrom)
            throw new InvalidOperationException("The scheme's end date is before its start date.");

        if (request.Kind == TradeSchemeKind.QuantityFreeGoods)
        {
            if (request.MinQuantity <= 0)
                throw new InvalidOperationException("A buy-N-get-M scheme needs a qualifying quantity.");
            if (request.FreeQuantity <= 0)
                throw new InvalidOperationException("A buy-N-get-M scheme needs a free quantity.");
        }

        if (request.Kind is TradeSchemeKind.QuantitySlab or TradeSchemeKind.ValueSlab && request.Slabs.Count == 0)
            throw new InvalidOperationException("A slab scheme needs at least one slab.");

        // Overlapping slabs make the benefit depend on evaluation order, which is exactly what a
        // claim dispute cannot survive.
        var ordered = request.Slabs
            .OrderBy(s => request.Kind == TradeSchemeKind.ValueSlab ? s.FromValue : s.FromQuantity)
            .ToList();

        for (var i = 1; i < ordered.Count; i++)
        {
            var prevTo = request.Kind == TradeSchemeKind.ValueSlab ? ordered[i - 1].ToValue : ordered[i - 1].ToQuantity;
            var thisFrom = request.Kind == TradeSchemeKind.ValueSlab ? ordered[i].FromValue : ordered[i].FromQuantity;

            if (prevTo is null)
                throw new InvalidOperationException("Only the top slab can be open-ended.");
            if (thisFrom <= prevTo)
                throw new InvalidOperationException($"Slab {i + 1} overlaps the one before it.");
        }

        if (request.Kind == TradeSchemeKind.ComboAssortment
            && request.Products.Count(p => p.IsQualifying && p.RequiredQuantity > 0) < 2)
            throw new InvalidOperationException("A combo scheme needs at least two required products.");
    }

    private async Task DecorateScopeNamesAsync(TradeSchemeDto dto)
    {
        if (dto.Scopes.Count == 0) return;

        var territoryIds = dto.Scopes.Where(s => s.TerritoryId.HasValue).Select(s => s.TerritoryId!.Value).ToList();
        var routeIds = dto.Scopes.Where(s => s.RouteId.HasValue).Select(s => s.RouteId!.Value).ToList();
        var partnerIds = dto.Scopes.Where(s => s.PartnerId.HasValue).Select(s => s.PartnerId!.Value).ToList();
        var outletIds = dto.Scopes.Where(s => s.OutletId.HasValue).Select(s => s.OutletId!.Value).ToList();

        var territories = await db.Territories.ForTenant(tenant)
            .Where(t => territoryIds.Contains(t.Id)).ToDictionaryAsync(t => t.Id, t => t.Name);
        var routes = await db.Routes.ForTenant(tenant)
            .Where(r => routeIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.Name);
        var partners = await db.Partners.ForTenant(tenant)
            .Where(p => partnerIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.Name);
        var outlets = await db.Outlets.ForTenant(tenant)
            .Where(o => outletIds.Contains(o.Id)).ToDictionaryAsync(o => o.Id, o => o.Name);

        foreach (var scope in dto.Scopes)
        {
            if (scope.TerritoryId.HasValue) scope.TerritoryName = territories.GetValueOrDefault(scope.TerritoryId.Value);
            if (scope.RouteId.HasValue) scope.RouteName = routes.GetValueOrDefault(scope.RouteId.Value);
            if (scope.PartnerId.HasValue) scope.PartnerName = partners.GetValueOrDefault(scope.PartnerId.Value);
            if (scope.OutletId.HasValue) scope.OutletName = outlets.GetValueOrDefault(scope.OutletId.Value);
        }
    }

    private async Task<int> CountEligibleOutletsAsync(TradeScheme scheme)
    {
        var scopes = scheme.Scopes.Where(s => !s.IsExcluded).ToList();
        var query = db.Outlets.ForTenant(tenant).Where(o => o.Status == OutletStatus.Active);

        if (scopes.Count > 0)
        {
            var channels = scopes.Where(s => s.Channel.HasValue).Select(s => s.Channel!.Value).ToList();
            var grades = scopes.Where(s => s.OutletGrade.HasValue).Select(s => s.OutletGrade!.Value).ToList();
            var territories = scopes.Where(s => s.TerritoryId.HasValue).Select(s => s.TerritoryId!.Value).ToList();

            if (channels.Count > 0) query = query.Where(o => channels.Contains(o.Channel));
            if (grades.Count > 0) query = query.Where(o => grades.Contains(o.Grade));
            if (territories.Count > 0) query = query.Where(o => o.TerritoryId != null && territories.Contains(o.TerritoryId.Value));
        }

        return await query.CountAsync();
    }

    private async Task<(decimal Value, decimal Quantity)> SalesForWindowAsync(
        DateTime from, DateTime to, List<Guid> itemIds)
    {
        var query = db.OrderLines.ForTenant(tenant)
            .Where(l => db.Orders.ForTenant(tenant).Any(o =>
                o.Id == l.OrderId && o.OrderDate >= from && o.OrderDate <= to
                && o.Status != DistributionOrderStatus.Cancelled
                && o.Status != DistributionOrderStatus.Rejected
                && o.Status != DistributionOrderStatus.Draft));

        if (itemIds.Count > 0) query = query.Where(l => itemIds.Contains(l.ItemId));

        var result = await query
            .GroupBy(_ => 1)
            .Select(g => new { Value = g.Sum(x => x.LineTotal), Quantity = g.Sum(x => x.BaseQuantity) })
            .FirstOrDefaultAsync();

        return (result?.Value ?? 0, result?.Quantity ?? 0);
    }

    private async Task<List<SchemeItemPerformanceDto>> ItemPerformanceAsync(
        TradeScheme scheme, List<Guid> itemIds, DateTime baseFrom, DateTime baseTo)
    {
        if (itemIds.Count == 0) return [];

        var actual = await db.OrderLines.ForTenant(tenant)
            .Where(l => itemIds.Contains(l.ItemId)
                        && db.Orders.ForTenant(tenant).Any(o => o.Id == l.OrderId
                            && o.OrderDate >= scheme.ValidFrom && o.OrderDate <= scheme.ValidTo))
            .GroupBy(l => new { l.ItemId, l.ItemName })
            .Select(g => new { g.Key.ItemId, g.Key.ItemName, Quantity = g.Sum(x => x.BaseQuantity) })
            .ToListAsync();

        var baseline = await db.OrderLines.ForTenant(tenant)
            .Where(l => itemIds.Contains(l.ItemId)
                        && db.Orders.ForTenant(tenant).Any(o => o.Id == l.OrderId
                            && o.OrderDate >= baseFrom && o.OrderDate <= baseTo))
            .GroupBy(l => l.ItemId)
            .Select(g => new { ItemId = g.Key, Quantity = g.Sum(x => x.BaseQuantity) })
            .ToDictionaryAsync(x => x.ItemId, x => x.Quantity);

        var benefits = await db.SchemeApplications.ForTenant(tenant)
            .Where(a => a.SchemeId == scheme.Id && !a.IsReversed && a.FreeItemId != null)
            .GroupBy(a => a.FreeItemId!.Value)
            .Select(g => new { ItemId = g.Key, Value = g.Sum(x => x.BenefitValue) })
            .ToDictionaryAsync(x => x.ItemId, x => x.Value);

        return actual.Select(a =>
        {
            var baseQty = baseline.GetValueOrDefault(a.ItemId);
            return new SchemeItemPerformanceDto
            {
                ItemId = a.ItemId,
                ItemName = a.ItemName,
                QualifyingQuantity = a.Quantity,
                BaselineQuantity = baseQty,
                UpliftPercent = DistributionMapper.Percent(a.Quantity - baseQty, baseQty),
                BenefitValue = benefits.GetValueOrDefault(a.ItemId),
            };
        })
        .OrderByDescending(x => x.QualifyingQuantity)
        .ToList();
    }

    private async Task WriteBudgetEntryAsync(TradeScheme scheme, decimal amount, string reason, Guid userId)
    {
        db.SchemeBudgetLedger.Add(new SchemeBudgetLedger
        {
            SchemeId = scheme.Id,
            OccurredAt = DateTime.UtcNow,
            Amount = amount,
            BalanceAfter = scheme.BudgetAmount - scheme.ConsumedAmount,
            Reason = reason,
            ActorUserId = userId,
        }.StampNew(tenant, userId));

        await db.SaveChangesAsync();
    }

    private void SyncSlabs(TradeScheme entity, List<TradeSchemeSlabDto> incoming, Guid userId)
    {
        foreach (var existing in entity.Slabs.Where(s => !s.IsDeleted).ToList())
            if (incoming.All(i => i.Id != existing.Id))
                existing.StampDeleted(userId);

        var number = 0;
        var ordered = incoming.OrderBy(s => s.FromQuantity).ThenBy(s => s.FromValue).ToList();

        foreach (var dto in ordered)
        {
            var slab = dto.Id != Guid.Empty ? entity.Slabs.FirstOrDefault(s => s.Id == dto.Id) : null;
            if (slab is null)
            {
                slab = new TradeSchemeSlab { SchemeId = entity.Id }.StampNew(tenant, userId);
                entity.Slabs.Add(slab);
            }

            slab.SlabNumber = ++number;
            slab.FromQuantity = dto.FromQuantity;
            slab.ToQuantity = dto.ToQuantity;
            slab.FromValue = dto.FromValue;
            slab.ToValue = dto.ToValue;
            slab.FreeQuantity = dto.FreeQuantity;
            slab.DiscountPercent = dto.DiscountPercent;
            slab.DiscountAmount = dto.DiscountAmount;
            slab.PayoutAmount = dto.PayoutAmount;
            slab.PointsAwarded = dto.PointsAwarded;
            slab.FreeItemId = dto.FreeItemId;
            slab.FreeItemName = dto.FreeItemName;
            slab.Label = dto.Label;
        }
    }

    private void SyncProducts(TradeScheme entity, List<TradeSchemeProductDto> incoming, Guid userId)
    {
        foreach (var existing in entity.Products.Where(p => !p.IsDeleted).ToList())
            if (incoming.All(i => i.Id != existing.Id))
                existing.StampDeleted(userId);

        foreach (var dto in incoming)
        {
            var product = dto.Id != Guid.Empty ? entity.Products.FirstOrDefault(p => p.Id == dto.Id) : null;
            if (product is null)
            {
                product = new TradeSchemeProduct { SchemeId = entity.Id }.StampNew(tenant, userId);
                entity.Products.Add(product);
            }

            product.ItemId = dto.ItemId;
            product.ItemName = dto.ItemName;
            product.BrandId = dto.BrandId;
            product.CategoryId = dto.CategoryId;
            product.IsQualifying = dto.IsQualifying;
            product.RequiredQuantity = dto.RequiredQuantity;
            product.Uom = dto.Uom;
            product.UomFactor = dto.UomFactor <= 0 ? 1 : dto.UomFactor;
            product.IsExcluded = dto.IsExcluded;
        }
    }

    private void SyncScopes(TradeScheme entity, List<TradeSchemeScopeDto> incoming, Guid userId)
    {
        foreach (var existing in entity.Scopes.Where(s => !s.IsDeleted).ToList())
            if (incoming.All(i => i.Id != existing.Id))
                existing.StampDeleted(userId);

        foreach (var dto in incoming)
        {
            var scope = dto.Id != Guid.Empty ? entity.Scopes.FirstOrDefault(s => s.Id == dto.Id) : null;
            if (scope is null)
            {
                scope = new TradeSchemeScope { SchemeId = entity.Id }.StampNew(tenant, userId);
                entity.Scopes.Add(scope);
            }

            scope.Channel = dto.Channel;
            scope.OutletGrade = dto.OutletGrade;
            scope.PartnerTier = dto.PartnerTier;
            scope.TerritoryId = dto.TerritoryId;
            scope.RouteId = dto.RouteId;
            scope.PartnerId = dto.PartnerId;
            scope.OutletId = dto.OutletId;
            scope.IsExcluded = dto.IsExcluded;
        }
    }
}
