using Distribution.Application.DTOs;
using Distribution.Application.Services.Interfaces;
using Distribution.Domain.Entities;
using Distribution.Domain.Enums;
using Distribution.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Events;

namespace Distribution.Infrastructure.Services;

/// <summary>
/// Price resolution.
///
/// "Why this price?" is the most-asked question at a distributor's counter, and the honest answer
/// is a chain of rules, not a number. So resolution walks the scopes most-specific-first and
/// returns every candidate it considered along with the one that won — the rep can show the
/// retailer the working, which is the difference between a conversation and an argument.
///
/// Order of specificity: Outlet → Contract → Partner → PartnerTier → Territory → Channel → Company.
/// Within a scope, higher <see cref="ChannelPriceList.Priority"/> wins; ties break on the most
/// recent effective date so a scheduled price change takes over on its own day without anyone
/// having to switch it on.
/// </summary>
public class PricingService(
    DistributionDbContext db,
    IDistributionTenant tenant,
    IEventPublisher events,
    DistributionNumbering numbering) : IPricingService
{
    /// <summary>Most specific first. Resolution stops at the first scope that produces a price.</summary>
    private static readonly PriceScope[] ScopeOrder =
    [
        PriceScope.Outlet,
        PriceScope.Contract,
        PriceScope.Partner,
        PriceScope.PartnerTier,
        PriceScope.Territory,
        PriceScope.Channel,
        PriceScope.Company,
    ];

    public async Task<PriceResolutionDto> ResolvePriceAsync(
        Guid itemId, string uom, decimal quantity, Guid? outletId, Guid? partnerId, DateTime? asOf)
    {
        var results = await ResolvePricesAsync([(itemId, uom, quantity)], outletId, partnerId, asOf);
        return results[0];
    }

    public async Task<List<PriceResolutionDto>> ResolvePricesAsync(
        List<(Guid ItemId, string Uom, decimal Quantity)> lines, Guid? outletId, Guid? partnerId, DateTime? asOf)
    {
        var at = (asOf ?? DateTime.UtcNow).Date;
        var context = await LoadContextAsync(outletId, partnerId);
        var candidateLists = await LoadCandidateListsAsync(context, at);

        var itemIds = lines.Select(l => l.ItemId).Distinct().ToList();
        var listIds = candidateLists.Select(l => l.Id).ToList();

        var allLines = await db.PriceListLines.ForTenant(tenant)
            .Include(l => l.Slabs)
            .Where(l => listIds.Contains(l.PriceListId) && itemIds.Contains(l.ItemId))
            .ToListAsync();

        var results = new List<PriceResolutionDto>();

        foreach (var (itemId, uom, quantity) in lines)
        {
            var dto = new PriceResolutionDto { ItemId = itemId, Uom = uom };
            var matched = false;

            foreach (var scope in ScopeOrder)
            {
                var listsInScope = candidateLists
                    .Where(l => l.Scope == scope)
                    .OrderByDescending(l => l.Priority)
                    .ThenByDescending(l => l.EffectiveFrom);

                foreach (var list in listsInScope)
                {
                    var line = allLines.FirstOrDefault(l =>
                        l.PriceListId == list.Id && l.ItemId == itemId &&
                        string.Equals(l.Uom, uom, StringComparison.OrdinalIgnoreCase) &&
                        (l.EffectiveFrom is null || l.EffectiveFrom.Value.Date <= at) &&
                        (l.EffectiveTo is null || l.EffectiveTo.Value.Date >= at));

                    if (line is null)
                    {
                        dto.Considered.Add(new PriceCandidateDto
                        {
                            Scope = list.Scope,
                            PriceListId = list.Id,
                            PriceListName = list.Name,
                            Priority = list.Priority,
                            SkipReason = "No line for this item and unit",
                        });
                        continue;
                    }

                    var (price, slabIndex) = ApplySlabs(line, quantity);

                    dto.ResolvedPrice = price;
                    dto.Mrp = line.Mrp;
                    dto.TaxPercent = line.TaxPercent;
                    dto.MinimumPrice = line.MinimumPrice;
                    dto.ItemName = line.ItemName;
                    dto.WinningScope = list.Scope;
                    dto.WinningPriceListId = list.Id;
                    dto.WinningPriceListName = list.Name;
                    dto.AppliedSlabIndex = slabIndex;

                    dto.Considered.Add(new PriceCandidateDto
                    {
                        Scope = list.Scope,
                        PriceListId = list.Id,
                        PriceListName = list.Name,
                        UnitPrice = price,
                        Priority = list.Priority,
                        IsWinner = true,
                    });

                    matched = true;
                    break;
                }

                if (matched) break;
            }

            // Nothing priced it. Rather than returning zero — which reads as "free" on a receipt —
            // fall back to the item's catalogue price from Inventory, and say so.
            if (!matched)
            {
                var fallback = await LookupCataloguePriceAsync(itemId);
                dto.ResolvedPrice = fallback.BasePrice;
                dto.ItemName = fallback.ProductName ?? string.Empty;
                dto.WinningScope = PriceScope.Company;
                dto.Considered.Add(new PriceCandidateDto
                {
                    Scope = PriceScope.Company,
                    PriceListName = "Item catalogue price",
                    UnitPrice = fallback.BasePrice,
                    IsWinner = true,
                    SkipReason = "No channel price list covered this item",
                });
            }

            results.Add(dto);
        }

        return results;
    }

    // ═══ Price lists ═════════════════════════════════════════════════════════

    public async Task<PaginatedResponse<PriceListDto>> ListPriceListsAsync(
        string? search, PriceScope? scope, Guid? partnerId, bool? activeOnly, PaginationParams pagination)
    {
        var today = DateTime.UtcNow.Date;
        var query = db.PriceLists.ForTenant(tenant)
            .WhereIf(scope.HasValue, p => p.Scope == scope)
            .WhereIf(partnerId.HasValue, p => p.PartnerId == partnerId)
            .WhereIf(activeOnly == true, p => p.IsActive && p.EffectiveFrom <= today
                                              && (p.EffectiveTo == null || p.EffectiveTo >= today));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(p => EF.Functions.ILike(p.Name, term) || EF.Functions.ILike(p.Code ?? "", term));
        }

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(p => p.EffectiveFrom).ThenBy(p => p.Name)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize)
            .Select(p => new
            {
                Entity = p,
                LineCount = p.Lines.Count(l => !l.IsDeleted),
            })
            .ToListAsync();

        var dtos = rows.Select(r =>
        {
            var dto = r.Entity.ToDto();
            dto.Lines = [];
            dto.LineCount = r.LineCount;
            return dto;
        });

        return PaginatedResponse<PriceListDto>.Ok(dtos, total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<PriceListDto?> GetPriceListAsync(Guid priceListId)
    {
        var entity = await db.PriceLists.ForTenant(tenant)
            .Include(p => p.Lines.Where(l => !l.IsDeleted)).ThenInclude(l => l.Slabs.Where(s => !s.IsDeleted))
            .FirstOrDefaultAsync(p => p.Id == priceListId);

        return entity?.ToDto();
    }

    public async Task<PriceListDto> SavePriceListAsync(Guid? priceListId, SavePriceListDto request, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("A price list needs a name.");

        ValidateScope(request);

        ChannelPriceList entity;
        if (priceListId.HasValue)
        {
            entity = await db.PriceLists.ForTenant(tenant)
                .Include(p => p.Lines).ThenInclude(l => l.Slabs)
                .FirstOrDefaultAsync(p => p.Id == priceListId)
                ?? throw new InvalidOperationException("That price list no longer exists.");

            // An approved list is history the moment an order references it. Editing one in place
            // would silently rewrite what a customer was charged last week.
            if (entity.IsApproved)
                throw new InvalidOperationException(
                    "This price list is approved and in use. Create a new version instead of editing it.");

            entity.StampUpdated(userId);
        }
        else
        {
            entity = new ChannelPriceList().StampNew(tenant, userId);
            entity.Code = string.IsNullOrWhiteSpace(request.Code)
                ? await numbering.NextMasterCodeAsync(db.PriceLists, "PRL")
                : request.Code;
            db.PriceLists.Add(entity);
        }

        entity.Name = request.Name.Trim();
        entity.Scope = request.Scope;
        entity.Channel = request.Channel;
        entity.TerritoryId = request.TerritoryId;
        entity.PartnerTier = request.PartnerTier;
        entity.PartnerId = request.PartnerId;
        entity.OutletId = request.OutletId;
        entity.CurrencyCode = request.CurrencyCode;
        entity.EffectiveFrom = request.EffectiveFrom == default ? DateTime.UtcNow.Date : request.EffectiveFrom;
        entity.EffectiveTo = request.EffectiveTo;
        entity.Priority = request.Priority;
        entity.IsTaxInclusive = request.IsTaxInclusive;
        entity.IsActive = request.IsActive;
        entity.Description = request.Description;

        SyncLines(entity, request.Lines, userId);

        await db.SaveChangesAsync();
        return (await GetPriceListAsync(entity.Id))!;
    }

    public async Task<PriceListDto> ApprovePriceListAsync(Guid priceListId, Guid userId)
    {
        var entity = await db.PriceLists.ForTenant(tenant)
            .Include(p => p.Lines)
            .FirstOrDefaultAsync(p => p.Id == priceListId)
            ?? throw new InvalidOperationException("That price list no longer exists.");

        if (entity.Lines.Count(l => !l.IsDeleted) == 0)
            throw new InvalidOperationException("An empty price list cannot be approved.");

        var belowFloor = entity.Lines
            .Where(l => !l.IsDeleted && l.MinimumPrice > 0 && l.UnitPrice < l.MinimumPrice)
            .Select(l => l.ItemName).Take(3).ToList();

        if (belowFloor.Count > 0)
            throw new InvalidOperationException(
                $"These lines are priced below their own floor: {string.Join(", ", belowFloor)}.");

        entity.IsApproved = true;
        entity.ApprovedAt = DateTime.UtcNow;
        entity.ApprovedByUserId = userId;
        entity.StampUpdated(userId);

        await db.SaveChangesAsync();
        return (await GetPriceListAsync(priceListId))!;
    }

    public async Task DeletePriceListAsync(Guid priceListId, Guid userId)
    {
        var entity = await db.PriceLists.ForTenant(tenant).FirstOrDefaultAsync(p => p.Id == priceListId)
            ?? throw new InvalidOperationException("That price list no longer exists.");

        var inUse = await db.Outlets.ForTenant(tenant).AnyAsync(o => o.PriceListId == priceListId)
                    || await db.Partners.ForTenant(tenant).AnyAsync(p => p.PriceListId == priceListId);

        if (inUse)
            throw new InvalidOperationException("This price list is assigned to outlets or partners. Unassign it first.");

        entity.StampDeleted(userId);
        await db.SaveChangesAsync();
    }

    // ═══ Margin ladder & MRP ═════════════════════════════════════════════════

    public async Task<PaginatedResponse<MarginLadderDto>> ListMarginLaddersAsync(
        Guid? itemId, Guid? partnerId, PaginationParams pagination)
    {
        var query = db.MarginLadders.ForTenant(tenant)
            .WhereIf(itemId.HasValue, m => m.ItemId == itemId)
            .WhereIf(partnerId.HasValue, m => m.PartnerId == partnerId);

        var total = await query.CountAsync();
        var rows = await query
            .OrderBy(m => m.ItemName).ThenByDescending(m => m.EffectiveFrom)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        var partnerNames = await db.Partners.ForTenant(tenant).ToDictionaryAsync(p => p.Id, p => p.Name);

        var dtos = rows.Select(m => new MarginLadderDto
        {
            Id = m.Id,
            ItemId = m.ItemId,
            ItemName = m.ItemName,
            ItemCode = m.ItemCode,
            TerritoryId = m.TerritoryId,
            PartnerId = m.PartnerId,
            PartnerName = m.PartnerId is null ? null : partnerNames.GetValueOrDefault(m.PartnerId.Value),
            Channel = m.Channel,
            Uom = m.Uom,
            CurrencyCode = m.CurrencyCode,
            LandedCost = m.LandedCost,
            PriceToDistributor = m.PriceToDistributor,
            PriceToWholesaler = m.PriceToWholesaler,
            PriceToRetailer = m.PriceToRetailer,
            Mrp = m.Mrp,
            CompanyMarginPercent = m.CompanyMarginPercent,
            DistributorMarginPercent = m.DistributorMarginPercent,
            WholesalerMarginPercent = m.WholesalerMarginPercent,
            RetailerMarginPercent = m.RetailerMarginPercent,
            EffectiveFrom = m.EffectiveFrom,
            EffectiveTo = m.EffectiveTo,
            Note = m.Note,
        });

        return PaginatedResponse<MarginLadderDto>.Ok(dtos, total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<MarginLadderDto> SaveMarginLadderAsync(Guid? id, MarginLadderDto request, Guid userId)
    {
        MarginLadder entity;
        if (id.HasValue)
        {
            entity = await db.MarginLadders.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == id)
                ?? throw new InvalidOperationException("That margin ladder no longer exists.");
            entity.StampUpdated(userId);
        }
        else
        {
            entity = new MarginLadder().StampNew(tenant, userId);
            db.MarginLadders.Add(entity);
        }

        entity.ItemId = request.ItemId;
        entity.ItemName = request.ItemName;
        entity.ItemCode = request.ItemCode;
        entity.TerritoryId = request.TerritoryId;
        entity.PartnerId = request.PartnerId;
        entity.Channel = request.Channel;
        entity.Uom = request.Uom;
        entity.CurrencyCode = request.CurrencyCode;
        entity.LandedCost = request.LandedCost;
        entity.PriceToDistributor = request.PriceToDistributor;
        entity.PriceToWholesaler = request.PriceToWholesaler;
        entity.PriceToRetailer = request.PriceToRetailer;
        entity.Mrp = request.Mrp;
        entity.EffectiveFrom = request.EffectiveFrom == default ? DateTime.UtcNow.Date : request.EffectiveFrom;
        entity.EffectiveTo = request.EffectiveTo;
        entity.Note = request.Note;

        // Margins are derived, never typed: a hand-entered percentage that disagrees with the
        // prices beside it is the fastest way to lose a negotiation.
        entity.CompanyMarginPercent = MarginPercent(entity.LandedCost, entity.PriceToDistributor);
        entity.DistributorMarginPercent = MarginPercent(entity.PriceToDistributor,
            entity.PriceToWholesaler > 0 ? entity.PriceToWholesaler : entity.PriceToRetailer);
        entity.WholesalerMarginPercent = MarginPercent(entity.PriceToWholesaler, entity.PriceToRetailer);
        entity.RetailerMarginPercent = MarginPercent(entity.PriceToRetailer, entity.Mrp);

        await db.SaveChangesAsync();

        request.Id = entity.Id;
        request.CompanyMarginPercent = entity.CompanyMarginPercent;
        request.DistributorMarginPercent = entity.DistributorMarginPercent;
        request.WholesalerMarginPercent = entity.WholesalerMarginPercent;
        request.RetailerMarginPercent = entity.RetailerMarginPercent;
        return request;
    }

    public async Task<PaginatedResponse<MrpRevisionDto>> ListMrpRevisionsAsync(
        Guid? itemId, PaginationParams pagination)
    {
        var query = db.MrpRevisions.ForTenant(tenant).WhereIf(itemId.HasValue, m => m.ItemId == itemId);

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(m => m.EffectiveFrom)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        var dtos = rows.Select(m => new MrpRevisionDto
        {
            Id = m.Id,
            ItemId = m.ItemId,
            ItemName = m.ItemName,
            BatchId = m.BatchId,
            BatchNumber = m.BatchNumber,
            Uom = m.Uom,
            OldMrp = m.OldMrp,
            NewMrp = m.NewMrp,
            EffectiveFrom = m.EffectiveFrom,
            TriggersPriceProtection = m.TriggersPriceProtection,
            ProtectionPerUnit = m.ProtectionPerUnit,
            ProtectionClaimWindowEnds = m.ProtectionClaimWindowEnds,
            Reason = m.Reason,
            ApprovedAt = m.ApprovedAt,
        });

        return PaginatedResponse<MrpRevisionDto>.Ok(dtos, total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<MrpRevisionDto> SaveMrpRevisionAsync(Guid? id, MrpRevisionDto request, Guid userId)
    {
        MrpRevision entity;
        if (id.HasValue)
        {
            entity = await db.MrpRevisions.ForTenant(tenant).FirstOrDefaultAsync(m => m.Id == id)
                ?? throw new InvalidOperationException("That MRP revision no longer exists.");
            entity.StampUpdated(userId);
        }
        else
        {
            entity = new MrpRevision().StampNew(tenant, userId);
            db.MrpRevisions.Add(entity);
        }

        entity.ItemId = request.ItemId;
        entity.ItemName = request.ItemName;
        entity.BatchId = request.BatchId;
        entity.BatchNumber = request.BatchNumber;
        entity.Uom = request.Uom;
        entity.OldMrp = request.OldMrp;
        entity.NewMrp = request.NewMrp;
        entity.EffectiveFrom = request.EffectiveFrom == default ? DateTime.UtcNow.Date : request.EffectiveFrom;
        entity.Reason = request.Reason;

        // A price cut on stock already in the trade owes the channel the difference. Working that
        // out here means the claim window opens automatically rather than after a complaint.
        var isCut = entity.NewMrp < entity.OldMrp;
        entity.TriggersPriceProtection = request.TriggersPriceProtection || isCut;
        entity.ProtectionPerUnit = entity.TriggersPriceProtection
            ? (request.ProtectionPerUnit > 0 ? request.ProtectionPerUnit : Math.Max(0, entity.OldMrp - entity.NewMrp))
            : 0;
        entity.ProtectionClaimWindowEnds = entity.TriggersPriceProtection
            ? request.ProtectionClaimWindowEnds ?? entity.EffectiveFrom.AddDays(45)
            : null;

        await db.SaveChangesAsync();

        request.Id = entity.Id;
        request.TriggersPriceProtection = entity.TriggersPriceProtection;
        request.ProtectionPerUnit = entity.ProtectionPerUnit;
        request.ProtectionClaimWindowEnds = entity.ProtectionClaimWindowEnds;
        return request;
    }

    // ═══ Internals ═══════════════════════════════════════════════════════════

    private record PriceContext(
        Guid? OutletId, Guid? PartnerId, Guid? OutletPriceListId, Guid? PartnerPriceListId,
        Guid? TerritoryId, OutletChannel? Channel, PartnerType? PartnerTier);

    private async Task<PriceContext> LoadContextAsync(Guid? outletId, Guid? partnerId)
    {
        Guid? outletPriceList = null, partnerPriceList = null, territoryId = null;
        OutletChannel? channel = null;
        PartnerType? tier = null;

        if (outletId.HasValue)
        {
            var o = await db.Outlets.ForTenant(tenant)
                .Where(x => x.Id == outletId)
                .Select(x => new { x.PriceListId, x.TerritoryId, x.Channel, x.PartnerId })
                .FirstOrDefaultAsync();

            if (o is not null)
            {
                outletPriceList = o.PriceListId;
                territoryId = o.TerritoryId;
                channel = o.Channel;
                partnerId ??= o.PartnerId;
            }
        }

        if (partnerId.HasValue)
        {
            var p = await db.Partners.ForTenant(tenant)
                .Where(x => x.Id == partnerId)
                .Select(x => new { x.PriceListId, x.TerritoryId, x.PartnerType })
                .FirstOrDefaultAsync();

            if (p is not null)
            {
                partnerPriceList = p.PriceListId;
                territoryId ??= p.TerritoryId;
                tier = p.PartnerType;
            }
        }

        return new PriceContext(outletId, partnerId, outletPriceList, partnerPriceList, territoryId, channel, tier);
    }

    private async Task<List<ChannelPriceList>> LoadCandidateListsAsync(PriceContext ctx, DateTime at)
        => await db.PriceLists.ForTenant(tenant)
            .Where(p => p.IsActive && p.IsApproved
                        && p.EffectiveFrom <= at
                        && (p.EffectiveTo == null || p.EffectiveTo >= at))
            .Where(p =>
                (p.Scope == PriceScope.Company) ||
                (p.Scope == PriceScope.Channel && ctx.Channel != null && p.Channel == ctx.Channel) ||
                (p.Scope == PriceScope.Territory && ctx.TerritoryId != null && p.TerritoryId == ctx.TerritoryId) ||
                (p.Scope == PriceScope.PartnerTier && ctx.PartnerTier != null && p.PartnerTier == ctx.PartnerTier) ||
                (p.Scope == PriceScope.Partner && ctx.PartnerId != null && p.PartnerId == ctx.PartnerId) ||
                (p.Scope == PriceScope.Outlet && ctx.OutletId != null && p.OutletId == ctx.OutletId) ||
                (p.Scope == PriceScope.Contract &&
                    ((ctx.OutletPriceListId != null && p.Id == ctx.OutletPriceListId) ||
                     (ctx.PartnerPriceListId != null && p.Id == ctx.PartnerPriceListId))))
            .ToListAsync();

    /// <summary>
    /// Applies a quantity break. The slab is chosen on the quantity as entered, in the line's own
    /// unit — a five-case order hits the five-case slab, not the hundred-and-twenty-piece one.
    /// </summary>
    private static (decimal Price, int? SlabIndex) ApplySlabs(ChannelPriceListLine line, decimal quantity)
    {
        var slabs = line.Slabs.Where(s => !s.IsDeleted).OrderBy(s => s.FromQuantity).ToList();
        if (slabs.Count == 0) return (line.UnitPrice, null);

        var match = slabs.LastOrDefault(s => quantity >= s.FromQuantity && (s.ToQuantity is null || quantity <= s.ToQuantity));
        if (match is null) return (line.UnitPrice, null);

        var price = match.UnitPrice > 0
            ? match.UnitPrice
            : line.UnitPrice * (1 - match.DiscountPercent / 100m);

        return (Math.Round(price, 4), slabs.IndexOf(match));
    }

    private async Task<ItemPriceData> LookupCataloguePriceAsync(Guid itemId)
    {
        var lookup = new ItemPriceLookupEvent { ProductId = itemId };
        await events.PublishAsync(lookup);

        // Inventory answers in-process. If nothing handled it, fall back rather than hang — a
        // counter waiting on a lookup that will never complete is worse than a zero it can see.
        var completed = await Task.WhenAny(lookup.Result.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        return completed == lookup.Result.Task ? await lookup.Result.Task : ItemPriceData.Empty;
    }

    private static decimal MarginPercent(decimal cost, decimal price)
        => price <= 0 ? 0 : Math.Round((price - cost) / price * 100, 4);

    private static void ValidateScope(SavePriceListDto request)
    {
        var missing = request.Scope switch
        {
            PriceScope.Channel when request.Channel is null => "a channel",
            PriceScope.Territory when request.TerritoryId is null => "a territory",
            PriceScope.PartnerTier when request.PartnerTier is null => "a partner tier",
            PriceScope.Partner when request.PartnerId is null => "a partner",
            PriceScope.Outlet when request.OutletId is null => "an outlet",
            _ => null,
        };

        if (missing is not null)
            throw new InvalidOperationException($"A {request.Scope} price list needs {missing}.");
    }

    private void SyncLines(ChannelPriceList entity, List<PriceListLineDto> incoming, Guid userId)
    {
        foreach (var existing in entity.Lines.Where(l => !l.IsDeleted).ToList())
            if (incoming.All(i => i.Id != existing.Id))
                existing.StampDeleted(userId);

        foreach (var dto in incoming)
        {
            var line = dto.Id != Guid.Empty ? entity.Lines.FirstOrDefault(l => l.Id == dto.Id) : null;
            if (line is null)
            {
                line = new ChannelPriceListLine { PriceListId = entity.Id }.StampNew(tenant, userId);
                entity.Lines.Add(line);
            }

            line.ItemId = dto.ItemId;
            line.ItemName = dto.ItemName;
            line.ItemCode = dto.ItemCode;
            line.BrandId = dto.BrandId;
            line.CategoryId = dto.CategoryId;
            line.Uom = string.IsNullOrWhiteSpace(dto.Uom) ? "PCS" : dto.Uom;
            line.UomFactor = dto.UomFactor <= 0 ? 1 : dto.UomFactor;
            line.UnitPrice = dto.UnitPrice;
            line.Mrp = dto.Mrp;
            line.MinimumPrice = dto.MinimumPrice;
            line.MaxDiscountPercent = dto.MaxDiscountPercent;
            line.TaxPercent = dto.TaxPercent;
            line.EffectiveFrom = dto.EffectiveFrom;
            line.EffectiveTo = dto.EffectiveTo;

            SyncSlabs(line, dto.Slabs, userId);
        }
    }

    private void SyncSlabs(ChannelPriceListLine line, List<PriceSlabDto> incoming, Guid userId)
    {
        foreach (var existing in line.Slabs.Where(s => !s.IsDeleted).ToList())
            if (incoming.All(i => i.Id != existing.Id))
                existing.StampDeleted(userId);

        var order = 0;
        foreach (var dto in incoming.OrderBy(s => s.FromQuantity))
        {
            var slab = dto.Id != Guid.Empty ? line.Slabs.FirstOrDefault(s => s.Id == dto.Id) : null;
            if (slab is null)
            {
                slab = new PriceSlab { PriceListLineId = line.Id }.StampNew(tenant, userId);
                line.Slabs.Add(slab);
            }

            slab.FromQuantity = dto.FromQuantity;
            slab.ToQuantity = dto.ToQuantity;
            slab.UnitPrice = dto.UnitPrice;
            slab.DiscountPercent = dto.DiscountPercent;
            slab.DisplayOrder = order++;
        }
    }
}
