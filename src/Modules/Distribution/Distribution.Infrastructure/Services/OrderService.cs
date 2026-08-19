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
/// Order capture, pricing, approval and allocation — where pricing, schemes, credit and stock meet.
///
/// <see cref="QuoteAsync"/> and <see cref="CreateAsync"/> share one pipeline on purpose. The
/// number the retailer is shown at the counter and the number that lands on the invoice have to
/// come from the same code, or the field spends its week apologising for the difference.
///
/// The pipeline is: resolve price → evaluate schemes → materialise free-goods lines → total →
/// check credit → decide whether approval is needed.
/// </summary>
public class OrderService(
    DistributionDbContext db,
    IDistributionTenant tenant,
    DistributionNumbering numbering,
    IPricingService pricing,
    ISchemeService schemes,
    ICreditService credit,
    IEventPublisher events) : IOrderService
{
    // ═══ Quote ═══════════════════════════════════════════════════════════════

    public async Task<OrderQuoteDto> QuoteAsync(QuoteOrderDto request)
    {
        var asOf = request.OrderDate ?? DateTime.UtcNow;
        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();

        var quote = new OrderQuoteDto
        {
            OutletId = request.OutletId,
            PartnerId = request.PartnerId,
            CurrencyCode = settings?.BaseCurrencyCode ?? "USD",
        };

        if (request.Lines.Count == 0) return quote;

        var priced = await BuildLinesAsync(request.Lines, request.OutletId, request.PartnerId, asOf, quote.Warnings);

        var applications = settings?.AutoApplySchemes ?? true
            ? await schemes.EvaluateAsync(request.OutletId, request.PartnerId, priced, asOf)
            : [];

        ApplySchemeBenefits(priced, applications);
        var freeLines = BuildFreeGoodsLines(priced, applications);
        priced.AddRange(freeLines);

        Total(quote, priced, applications);

        quote.Lines = priced;
        quote.AppliedSchemes = applications;

        if (settings?.ShowNextSlabPrompt ?? true)
            quote.NextSlabHints = await schemes.GetNextSlabHintsAsync(
                request.OutletId, request.PartnerId, priced.Where(l => !l.IsFreeGoods).ToList(), asOf);

        await CheckAvailabilityAsync(priced, request.WarehouseId, request.VanUnitId, quote.Warnings);
        await CheckAuthorisationAsync(priced, request.PartnerId, quote.Blockers);

        quote.Credit = await credit.CheckAsync(
            request.OutletId, request.PartnerId, quote.TotalAmount,
            request.VanUnitId.HasValue ? settings?.CreditEnforcementAtVanSale : settings?.CreditEnforcementAtOrder);

        if (!quote.Credit.IsAllowed) quote.Blockers.Add(quote.Credit.Message ?? "Blocked on credit.");
        else if (quote.Credit.RequiresOverride || !string.IsNullOrWhiteSpace(quote.Credit.Message))
            quote.Warnings.Add(quote.Credit.Message ?? string.Empty);

        if (settings is not null && settings.MinimumOrderValue > 0 && quote.TotalAmount < settings.MinimumOrderValue)
            quote.Blockers.Add($"Minimum order value is {settings.MinimumOrderValue:N2}.");

        (quote.RequiresApproval, quote.ApprovalReason) =
            await NeedsApprovalAsync(priced, quote, request.FieldRepId, settings);

        quote.Warnings.RemoveAll(string.IsNullOrWhiteSpace);
        return quote;
    }

    // ═══ Lifecycle ═══════════════════════════════════════════════════════════

    public async Task<DistributionOrderDto> CreateAsync(CreateOrderDto request, Guid userId)
    {
        if (request.Lines.Count == 0)
            throw new InvalidOperationException("An order needs at least one line.");
        if (request.OutletId is null && request.PartnerId is null)
            throw new InvalidOperationException("An order must name an outlet or a partner.");

        // Idempotency first: a double-tap in the market must never produce two orders.
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existing = await LoadFullAsync(null, request.IdempotencyKey);
            if (existing is not null) return existing.ToDto();
        }

        await GuardOutletIsBillableAsync(request.OutletId);

        var asOf = request.OrderDate ?? DateTime.UtcNow;
        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();

        var quote = await QuoteAsync(new QuoteOrderDto
        {
            OutletId = request.OutletId,
            PartnerId = request.PartnerId,
            WarehouseId = request.WarehouseId,
            VanUnitId = request.VanUnitId,
            FieldRepId = request.FieldRepId,
            OrderDate = asOf,
            Lines = request.Lines,
        });

        if (quote.Blockers.Count > 0)
            throw new InvalidOperationException(quote.Blockers[0]);

        var order = new DistributionOrder
        {
            OrderNumber = await numbering.NextOrderNumberAsync(asOf),
            Source = request.Source,
            Kind = request.Kind,
            Status = DistributionOrderStatus.Draft,
            OutletId = request.OutletId,
            PartnerId = request.PartnerId,
            CustomerName = request.CustomerName,
            CustomerPhone = request.CustomerPhone,
            VisitId = request.VisitId,
            FieldDayId = request.FieldDayId,
            FieldRepId = request.FieldRepId,
            RouteId = request.RouteId,
            WarehouseId = request.WarehouseId,
            VanUnitId = request.VanUnitId,
            OrderDate = asOf,
            RequestedDeliveryDate = request.RequestedDeliveryDate,
            CurrencyCode = quote.CurrencyCode,
            FreightAmount = request.FreightAmount,
            ExternalReference = request.ExternalReference,
            Note = request.Note,
            IdempotencyKey = request.IdempotencyKey,
            RequiresApproval = quote.RequiresApproval,
        }.StampNew(tenant, userId);

        order.TerritoryId = await ResolveTerritoryAsync(request.OutletId, request.PartnerId);

        var display = 0;
        foreach (var line in quote.Lines)
        {
            order.Lines.Add(new DistributionOrderLine
            {
                OrderId = order.Id,
                DisplayOrder = display++,
                ItemId = line.ItemId,
                ItemName = line.ItemName,
                ItemCode = line.ItemCode,
                BrandId = line.BrandId,
                CategoryId = line.CategoryId,
                BatchId = line.BatchId,
                BatchNumber = line.BatchNumber,
                ExpiryDate = line.ExpiryDate,
                Uom = line.Uom,
                UomFactor = line.UomFactor,
                Quantity = line.Quantity,
                BaseQuantity = line.BaseQuantity,
                UnitPrice = line.UnitPrice,
                Mrp = line.Mrp,
                DiscountPercent = line.DiscountPercent,
                DiscountAmount = line.DiscountAmount,
                SchemeDiscountAmount = line.SchemeDiscountAmount,
                TaxPercent = line.TaxPercent,
                TaxAmount = line.TaxAmount,
                LineTotal = line.LineTotal,
                UnitCost = line.UnitCost,
                MarginAmount = line.MarginAmount,
                IsFreeGoods = line.IsFreeGoods,
                SchemeId = line.SchemeId,
                SchemeName = line.SchemeName,
                PriceScope = line.PriceScope,
                IsPriceOverridden = line.IsPriceOverridden,
                PriceOverrideReason = line.PriceOverrideReason,
                Note = line.Note,
            }.StampNew(tenant, userId));
        }

        ApplyTotals(order, quote);
        db.Orders.Add(order);

        AddStatusEvent(order, DistributionOrderStatus.Draft, DistributionOrderStatus.Draft, "Order created", userId);
        await db.SaveChangesAsync();

        await PersistSchemeApplicationsAsync(order, quote.AppliedSchemes, userId);

        if (request.SubmitImmediately || request.IsVanSale)
            await TransitionAsync(order, DistributionOrderStatus.Submitted, "Submitted", userId);

        // A van sale settles at the counter: the goods leave now, so the order is complete now.
        if (request.IsVanSale)
        {
            if (request.VanUnitId is null)
                throw new InvalidOperationException("A van sale needs a van.");
            await SettleVanSaleAsync(order, request.VanUnitId.Value, userId);
        }
        else if (!order.RequiresApproval && request.SubmitImmediately)
        {
            await TransitionAsync(order, DistributionOrderStatus.Approved, "Auto-approved", userId);
        }
        else if (order.RequiresApproval)
        {
            await RaiseApprovalAsync(order, quote.ApprovalReason, userId);
        }

        if (request.Collection is not null)
        {
            request.Collection.OutletId ??= order.OutletId;
            request.Collection.PartnerId ??= order.PartnerId;
            request.Collection.VisitId ??= order.VisitId;
            request.Collection.FieldDayId ??= order.FieldDayId;
            request.Collection.FieldRepId ??= order.FieldRepId;
            await credit.RecordCollectionAsync(request.Collection, userId);
        }

        await UpdateVisitAndDayAsync(order, userId);
        await db.SaveChangesAsync();

        return (await GetAsync(order.Id))!;
    }

    public async Task<DistributionOrderDto?> GetAsync(Guid orderId)
    {
        var entity = await LoadFullAsync(orderId, null);
        if (entity is null) return null;

        var dto = entity.ToDto();
        dto.AppliedSchemes = (await db.SchemeApplications.ForTenant(tenant)
                .Where(a => a.OrderId == orderId && !a.IsReversed).ToListAsync())
            .Select(a => a.ToDto()).ToList();

        if (entity.FieldRepId.HasValue)
            dto.FieldRepName = await db.FieldReps.ForTenant(tenant)
                .Where(r => r.Id == entity.FieldRepId).Select(r => r.FullName).FirstOrDefaultAsync();

        if (entity.RouteId.HasValue)
            dto.RouteName = await db.Routes.ForTenant(tenant)
                .Where(r => r.Id == entity.RouteId).Select(r => r.Name).FirstOrDefaultAsync();

        return dto;
    }

    public async Task<DistributionOrderDto> UpdateAsync(Guid orderId, UpdateOrderDto request, Guid userId)
    {
        var order = await LoadFullAsync(orderId, null)
            ?? throw new InvalidOperationException("That order no longer exists.");

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();

        // Once picking starts the warehouse is acting on this document. Editing it then is how a
        // truck leaves with the wrong goods.
        if (order.Status >= DistributionOrderStatus.Picking)
            throw new InvalidOperationException("This order is already being picked and can no longer be edited.");

        var window = settings?.OrderEditWindowMinutes ?? 30;
        if (window > 0 && order.Status >= DistributionOrderStatus.Approved
            && order.ApprovedAt is not null
            && (DateTime.UtcNow - order.ApprovedAt.Value).TotalMinutes > window)
            throw new InvalidOperationException(
                $"The {window}-minute edit window on an approved order has passed. Cancel and re-enter it.");

        order.RequestedDeliveryDate = request.RequestedDeliveryDate ?? order.RequestedDeliveryDate;
        order.PromisedDeliveryDate = request.PromisedDeliveryDate ?? order.PromisedDeliveryDate;
        order.WarehouseId = request.WarehouseId ?? order.WarehouseId;
        order.FreightAmount = request.FreightAmount ?? order.FreightAmount;
        order.Note = request.Note ?? order.Note;
        order.StampUpdated(userId);

        if (request.Lines is not null)
        {
            var quote = await QuoteAsync(new QuoteOrderDto
            {
                OutletId = order.OutletId,
                PartnerId = order.PartnerId,
                WarehouseId = order.WarehouseId,
                VanUnitId = order.VanUnitId,
                FieldRepId = order.FieldRepId,
                OrderDate = order.OrderDate,
                Lines = request.Lines,
            });

            if (quote.Blockers.Count > 0) throw new InvalidOperationException(quote.Blockers[0]);

            foreach (var existing in order.Lines.Where(l => !l.IsDeleted).ToList())
                existing.StampDeleted(userId);

            var display = 0;
            foreach (var line in quote.Lines)
            {
                order.Lines.Add(new DistributionOrderLine
                {
                    OrderId = order.Id,
                    DisplayOrder = display++,
                    ItemId = line.ItemId,
                    ItemName = line.ItemName,
                    ItemCode = line.ItemCode,
                    BrandId = line.BrandId,
                    CategoryId = line.CategoryId,
                    Uom = line.Uom,
                    UomFactor = line.UomFactor,
                    Quantity = line.Quantity,
                    BaseQuantity = line.BaseQuantity,
                    UnitPrice = line.UnitPrice,
                    Mrp = line.Mrp,
                    DiscountPercent = line.DiscountPercent,
                    DiscountAmount = line.DiscountAmount,
                    SchemeDiscountAmount = line.SchemeDiscountAmount,
                    TaxPercent = line.TaxPercent,
                    TaxAmount = line.TaxAmount,
                    LineTotal = line.LineTotal,
                    UnitCost = line.UnitCost,
                    MarginAmount = line.MarginAmount,
                    IsFreeGoods = line.IsFreeGoods,
                    SchemeId = line.SchemeId,
                    SchemeName = line.SchemeName,
                    PriceScope = line.PriceScope,
                }.StampNew(tenant, userId));
            }

            ApplyTotals(order, quote);

            // Old scheme applications belong to a basket that no longer exists.
            await db.SchemeApplications.ForTenant(tenant)
                .Where(a => a.OrderId == orderId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(a => a.IsReversed, true)
                    .SetProperty(a => a.ReversalReason, "Order lines were changed"));

            await db.SaveChangesAsync();
            await PersistSchemeApplicationsAsync(order, quote.AppliedSchemes, userId);
        }

        await db.SaveChangesAsync();
        return (await GetAsync(orderId))!;
    }

    public async Task<DistributionOrderDto> SubmitAsync(Guid orderId, Guid userId)
    {
        var order = await LoadFullAsync(orderId, null)
            ?? throw new InvalidOperationException("That order no longer exists.");

        if (order.Status != DistributionOrderStatus.Draft)
            throw new InvalidOperationException("Only a draft order can be submitted.");

        await TransitionAsync(order, DistributionOrderStatus.Submitted, "Submitted", userId);

        if (order.RequiresApproval) await RaiseApprovalAsync(order, null, userId);
        else await TransitionAsync(order, DistributionOrderStatus.Approved, "Auto-approved", userId);

        await db.SaveChangesAsync();
        return (await GetAsync(orderId))!;
    }

    public async Task<DistributionOrderDto> DecideAsync(OrderDecisionDto request, Guid userId)
    {
        var order = await LoadFullAsync(request.OrderId, null)
            ?? throw new InvalidOperationException("That order no longer exists.");

        if (order.Status is not (DistributionOrderStatus.Submitted or DistributionOrderStatus.PendingApproval))
            throw new InvalidOperationException("This order is not waiting for a decision.");

        var step = order.Approvals
            .Where(a => a.IsApproved is null)
            .OrderBy(a => a.StepNumber)
            .FirstOrDefault();

        if (step is not null)
        {
            step.IsApproved = request.IsApproved;
            step.DecidedAt = DateTime.UtcNow;
            step.ApproverUserId = userId;
            step.Comment = request.Comment;
            step.StampUpdated(userId);
        }

        if (!request.IsApproved)
        {
            if (string.IsNullOrWhiteSpace(request.Comment))
                throw new InvalidOperationException("Rejecting an order needs a reason.");

            order.RejectionReason = request.Comment;
            await TransitionAsync(order, DistributionOrderStatus.Rejected, request.Comment, userId);
            await ReverseSchemeApplicationsAsync(order.Id, "Order rejected", userId);
        }
        else
        {
            var remaining = order.Approvals.Any(a => a.IsApproved is null);
            if (remaining)
            {
                await TransitionAsync(order, DistributionOrderStatus.PendingApproval, "Awaiting the next approver", userId);
            }
            else
            {
                order.ApprovedAt = DateTime.UtcNow;
                order.ApprovedByUserId = userId;
                await TransitionAsync(order, DistributionOrderStatus.Approved, request.Comment ?? "Approved", userId);
            }
        }

        await db.SaveChangesAsync();
        return (await GetAsync(request.OrderId))!;
    }

    public async Task<DistributionOrderDto> HoldAsync(Guid orderId, string reason, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Holding an order needs a reason.");

        var order = await LoadFullAsync(orderId, null)
            ?? throw new InvalidOperationException("That order no longer exists.");

        if (order.Status >= DistributionOrderStatus.Dispatched)
            throw new InvalidOperationException("This order has already left the building.");

        order.HoldReason = reason;
        await TransitionAsync(order, DistributionOrderStatus.OnHold, reason, userId);
        await db.SaveChangesAsync();

        return (await GetAsync(orderId))!;
    }

    public async Task<DistributionOrderDto> ReleaseHoldAsync(Guid orderId, Guid userId)
    {
        var order = await LoadFullAsync(orderId, null)
            ?? throw new InvalidOperationException("That order no longer exists.");

        if (order.Status != DistributionOrderStatus.OnHold)
            throw new InvalidOperationException("This order is not on hold.");

        order.HoldReason = null;
        await TransitionAsync(order, DistributionOrderStatus.Approved, "Hold released", userId);
        await db.SaveChangesAsync();

        return (await GetAsync(orderId))!;
    }

    public async Task<DistributionOrderDto> CancelAsync(Guid orderId, CancelOrderDto request, Guid userId)
    {
        var order = await LoadFullAsync(orderId, null)
            ?? throw new InvalidOperationException("That order no longer exists.");

        if (order.Status >= DistributionOrderStatus.Dispatched)
            throw new InvalidOperationException(
                "This order has been dispatched. Raise a return rather than cancelling it.");

        if (request.ReasonCodeId == Guid.Empty)
            throw new InvalidOperationException("Cancelling an order needs a reason.");

        order.CancelReasonCodeId = request.ReasonCodeId;
        order.CancelNote = request.Note;

        await ReleaseAllocationAsync(orderId, userId);
        await ReverseSchemeApplicationsAsync(orderId, "Order cancelled", userId);
        await TransitionAsync(order, DistributionOrderStatus.Cancelled, request.Note, userId);
        await db.SaveChangesAsync();

        await credit.RecalculateAsync(order.OutletId, order.PartnerId);
        return (await GetAsync(orderId))!;
    }

    public async Task<PaginatedResponse<OrderSummaryDto>> ListAsync(
        string? search, DistributionOrderStatus? status, OrderSource? source, Guid? outletId,
        Guid? partnerId, Guid? routeId, Guid? fieldRepId, Guid? warehouseId,
        DateTime? from, DateTime? to, bool? awaitingApproval, PaginationParams pagination)
    {
        var query = db.Orders.ForTenant(tenant)
            .Include(o => o.Outlet).Include(o => o.Partner)
            .WhereIf(status.HasValue, o => o.Status == status)
            .WhereIf(source.HasValue, o => o.Source == source)
            .WhereIf(outletId.HasValue, o => o.OutletId == outletId)
            .WhereIf(partnerId.HasValue, o => o.PartnerId == partnerId)
            .WhereIf(routeId.HasValue, o => o.RouteId == routeId)
            .WhereIf(fieldRepId.HasValue, o => o.FieldRepId == fieldRepId)
            .WhereIf(warehouseId.HasValue, o => o.WarehouseId == warehouseId)
            .WhereIf(from.HasValue, o => o.OrderDate >= from)
            .WhereIf(to.HasValue, o => o.OrderDate <= to)
            .WhereIf(awaitingApproval == true, o => o.RequiresApproval && o.ApprovedAt == null
                                                    && o.Status != DistributionOrderStatus.Rejected
                                                    && o.Status != DistributionOrderStatus.Cancelled);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(o =>
                EF.Functions.ILike(o.OrderNumber, term) ||
                (o.Outlet != null && EF.Functions.ILike(o.Outlet.Name, term)) ||
                (o.Partner != null && EF.Functions.ILike(o.Partner.Name, term)));
        }

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.CreatedAt)
            .Skip((pagination.PageNumber - 1) * pagination.PageSize)
            .Take(pagination.PageSize).ToListAsync();

        var dtos = rows.Select(r => r.ToSummary()).ToList();

        var repIds = rows.Where(r => r.FieldRepId.HasValue).Select(r => r.FieldRepId!.Value).Distinct().ToList();
        var routeIds = rows.Where(r => r.RouteId.HasValue).Select(r => r.RouteId!.Value).Distinct().ToList();

        var reps = await db.FieldReps.ForTenant(tenant)
            .Where(r => repIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.FullName);
        var routes = await db.Routes.ForTenant(tenant)
            .Where(r => routeIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.Name);

        for (var i = 0; i < rows.Count; i++)
        {
            if (rows[i].FieldRepId.HasValue) dtos[i].FieldRepName = reps.GetValueOrDefault(rows[i].FieldRepId!.Value);
            if (rows[i].RouteId.HasValue) dtos[i].RouteName = routes.GetValueOrDefault(rows[i].RouteId!.Value);
        }

        return PaginatedResponse<OrderSummaryDto>.Ok(dtos, total, pagination.PageNumber, pagination.PageSize);
    }

    // ═══ Catalogue ═══════════════════════════════════════════════════════════

    public async Task<List<CatalogueItemDto>> GetCatalogueAsync(
        Guid? outletId, Guid? partnerId, Guid? warehouseId, Guid? vanUnitId, string? search,
        Guid? categoryId, Guid? brandId)
    {
        // The catalogue is assembled from what this tenant actually prices, not from the whole
        // item master: a rep should never scroll past a thousand SKUs their outlet cannot buy.
        var asOf = DateTime.UtcNow;

        var priceLines = await db.PriceListLines.ForTenant(tenant)
            .Include(l => l.PriceList)
            .Where(l => l.PriceList != null && l.PriceList.IsActive && l.PriceList.IsApproved
                        && l.PriceList.EffectiveFrom <= asOf
                        && (l.PriceList.EffectiveTo == null || l.PriceList.EffectiveTo >= asOf))
            .WhereIf(categoryId.HasValue, l => l.CategoryId == categoryId)
            .WhereIf(brandId.HasValue, l => l.BrandId == brandId)
            .ToListAsync();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            priceLines = priceLines.Where(l =>
                l.ItemName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                (l.ItemCode ?? "").Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        var items = new List<CatalogueItemDto>();

        foreach (var group in priceLines.GroupBy(l => l.ItemId))
        {
            var first = group.First();
            var resolutions = new List<CatalogueUomDto>();

            foreach (var uomGroup in group.GroupBy(l => l.Uom))
            {
                var resolved = await pricing.ResolvePriceAsync(
                    group.Key, uomGroup.Key, 1, outletId, partnerId, asOf);

                resolutions.Add(new CatalogueUomDto
                {
                    Uom = uomGroup.Key,
                    Factor = uomGroup.First().UomFactor,
                    UnitPrice = resolved.ResolvedPrice,
                    IsDefault = uomGroup.First().UomFactor == 1,
                });
            }

            items.Add(new CatalogueItemDto
            {
                ItemId = group.Key,
                ItemName = first.ItemName,
                ItemCode = first.ItemCode,
                BrandId = first.BrandId,
                CategoryId = first.CategoryId,
                Mrp = first.Mrp,
                TaxPercent = first.TaxPercent,
                Uoms = resolutions.OrderBy(u => u.Factor).ToList(),
            });
        }

        await DecorateCatalogueAsync(items, outletId, partnerId, warehouseId, vanUnitId, asOf);
        return items.OrderByDescending(i => i.IsFocusItem).ThenBy(i => i.ItemName).ToList();
    }

    // ═══ Allocation ══════════════════════════════════════════════════════════

    public async Task<List<StockAllocationDto>> AllocateAsync(AllocateOrderDto request, Guid userId)
    {
        var order = await LoadFullAsync(request.OrderId, null)
            ?? throw new InvalidOperationException("That order no longer exists.");

        if (order.Status < DistributionOrderStatus.Approved)
            throw new InvalidOperationException("Approve the order before allocating stock to it.");
        if (order.Status >= DistributionOrderStatus.Picked)
            throw new InvalidOperationException("This order has already been picked.");

        var settings = await db.Settings.ForTenant(tenant).FirstOrDefaultAsync();
        var strategy = request.Strategy ?? settings?.DefaultAllocationStrategy ?? AllocationStrategy.Fefo;
        var warehouseId = request.WarehouseId ?? order.WarehouseId;

        await ReleaseAllocationAsync(order.Id, userId);

        var allocations = new List<StockAllocation>();

        foreach (var line in order.Lines.Where(l => !l.IsDeleted))
        {
            var allocation = new StockAllocation
            {
                OrderId = order.Id,
                OrderLineId = line.Id,
                ItemId = line.ItemId,
                WarehouseId = warehouseId,
                BatchId = line.BatchId,
                BatchNumber = line.BatchNumber,
                ExpiryDate = line.ExpiryDate,
                Quantity = line.BaseQuantity,
                Strategy = strategy,
                IsHardAllocation = request.HardAllocate,
                AllocatedAt = DateTime.UtcNow,
                // Soft holds lapse on their own; a hold that never expires quietly consumes stock
                // for orders nobody is going to fulfil.
                ExpiresAt = request.HardAllocate
                    ? null
                    : DateTime.UtcNow.AddHours(settings?.SoftAllocationHoldHours ?? 24),
            }.StampNew(tenant, userId);

            allocations.Add(allocation);
            db.StockAllocations.Add(allocation);

            line.AllocatedQuantity = line.BaseQuantity;
            line.StampUpdated(userId);
        }

        await TransitionAsync(order, DistributionOrderStatus.Allocated, "Stock allocated", userId);
        await db.SaveChangesAsync();

        // Inventory owns the ledger: reserve there rather than keeping a private count.
        if (warehouseId.HasValue)
            await events.PublishAsync(new StockReservationChangedEvent
            {
                ReferenceId = order.Id,
                ReferenceType = "DistributionOrder",
                WarehouseId = warehouseId.Value,
                CompanyId = tenant.CompanyId,
                BranchId = tenant.BranchId,
                BusinessUnitId = tenant.BusinessUnitId,
                CreatedByUserId = userId,
                Lines = allocations.Select(a => new StockReservationLine
                {
                    ProductId = a.ItemId,
                    WarehouseId = a.WarehouseId,
                    BinId = a.BinId,
                    Quantity = a.Quantity,
                }).ToList(),
            });

        return allocations.Select(a => new StockAllocationDto
        {
            Id = a.Id,
            OrderId = a.OrderId,
            OrderLineId = a.OrderLineId,
            ItemId = a.ItemId,
            WarehouseId = a.WarehouseId,
            BatchId = a.BatchId,
            BatchNumber = a.BatchNumber,
            ExpiryDate = a.ExpiryDate,
            Quantity = a.Quantity,
            Strategy = a.Strategy,
            IsHardAllocation = a.IsHardAllocation,
            AllocatedAt = a.AllocatedAt,
            ExpiresAt = a.ExpiresAt,
        }).ToList();
    }

    public async Task ReleaseAllocationAsync(Guid orderId, Guid userId)
    {
        var open = await db.StockAllocations.ForTenant(tenant)
            .Where(a => a.OrderId == orderId && a.ReleasedAt == null).ToListAsync();

        if (open.Count == 0) return;

        foreach (var allocation in open)
        {
            allocation.ReleasedAt = DateTime.UtcNow;
            allocation.StampUpdated(userId);
        }

        var warehouseId = open.FirstOrDefault(a => a.WarehouseId.HasValue)?.WarehouseId;
        await db.SaveChangesAsync();

        if (warehouseId.HasValue)
            await events.PublishAsync(new StockReservationChangedEvent
            {
                ReferenceId = orderId,
                ReferenceType = "DistributionOrder",
                WarehouseId = warehouseId.Value,
                CompanyId = tenant.CompanyId,
                BranchId = tenant.BranchId,
                BusinessUnitId = tenant.BusinessUnitId,
                CreatedByUserId = userId,
                Lines = open.Select(a => new StockReservationLine
                {
                    ProductId = a.ItemId,
                    WarehouseId = a.WarehouseId,
                    BinId = a.BinId,
                    Quantity = -a.Quantity,
                }).ToList(),
            });
    }

    public async Task<DistributionOrderDto> ConsolidateAsync(List<Guid> orderIds, Guid userId)
    {
        if (orderIds.Count < 2)
            throw new InvalidOperationException("Consolidation needs at least two orders.");

        var orders = await db.Orders.ForTenant(tenant)
            .Include(o => o.Lines.Where(l => !l.IsDeleted))
            .Where(o => orderIds.Contains(o.Id))
            .ToListAsync();

        if (orders.Count != orderIds.Count)
            throw new InvalidOperationException("One of those orders no longer exists.");

        var destinations = orders.Select(o => o.OutletId ?? o.PartnerId).Distinct().ToList();
        if (destinations.Count > 1)
            throw new InvalidOperationException("Only orders going to the same destination can be consolidated.");

        if (orders.Any(o => o.Status >= DistributionOrderStatus.Picking))
            throw new InvalidOperationException("One of these orders is already being picked.");

        var primary = orders.OrderBy(o => o.OrderDate).First();

        foreach (var other in orders.Where(o => o.Id != primary.Id))
        {
            foreach (var line in other.Lines.Where(l => !l.IsDeleted))
            {
                var match = primary.Lines.FirstOrDefault(l =>
                    !l.IsDeleted && l.ItemId == line.ItemId && l.Uom == line.Uom
                    && l.UnitPrice == line.UnitPrice && l.IsFreeGoods == line.IsFreeGoods);

                if (match is not null)
                {
                    match.Quantity += line.Quantity;
                    match.BaseQuantity += line.BaseQuantity;
                    match.DiscountAmount += line.DiscountAmount;
                    match.SchemeDiscountAmount += line.SchemeDiscountAmount;
                    match.TaxAmount += line.TaxAmount;
                    match.LineTotal += line.LineTotal;
                    match.MarginAmount += line.MarginAmount;
                    match.StampUpdated(userId);
                }
                else
                {
                    primary.Lines.Add(new DistributionOrderLine
                    {
                        OrderId = primary.Id,
                        DisplayOrder = primary.Lines.Count,
                        ItemId = line.ItemId,
                        ItemName = line.ItemName,
                        ItemCode = line.ItemCode,
                        BrandId = line.BrandId,
                        CategoryId = line.CategoryId,
                        Uom = line.Uom,
                        UomFactor = line.UomFactor,
                        Quantity = line.Quantity,
                        BaseQuantity = line.BaseQuantity,
                        UnitPrice = line.UnitPrice,
                        Mrp = line.Mrp,
                        DiscountAmount = line.DiscountAmount,
                        SchemeDiscountAmount = line.SchemeDiscountAmount,
                        TaxPercent = line.TaxPercent,
                        TaxAmount = line.TaxAmount,
                        LineTotal = line.LineTotal,
                        UnitCost = line.UnitCost,
                        MarginAmount = line.MarginAmount,
                        IsFreeGoods = line.IsFreeGoods,
                        SchemeId = line.SchemeId,
                        SchemeName = line.SchemeName,
                    }.StampNew(tenant, userId));
                }
            }

            other.CancelNote = $"Consolidated into {primary.OrderNumber}";
            AddStatusEvent(other, other.Status, DistributionOrderStatus.Cancelled, other.CancelNote, userId);
            other.Status = DistributionOrderStatus.Cancelled;
            other.StampUpdated(userId);
        }

        RecomputeTotals(primary);
        primary.Note = $"{primary.Note} Consolidated from {orders.Count} orders.".Trim();
        primary.StampUpdated(userId);

        await db.SaveChangesAsync();
        return (await GetAsync(primary.Id))!;
    }

    // ═══ Pipeline internals ══════════════════════════════════════════════════

    /// <summary>Resolves price, tax and cost for each requested line. Nothing is persisted.</summary>
    private async Task<List<DistributionOrderLineDto>> BuildLinesAsync(
        List<SaveOrderLineDto> requested, Guid? outletId, Guid? partnerId, DateTime asOf, List<string> warnings)
    {
        var resolutions = await pricing.ResolvePricesAsync(
            requested.Select(l => (l.ItemId, l.Uom, l.Quantity)).ToList(), outletId, partnerId, asOf);

        var uomFactors = await db.PriceListLines.ForTenant(tenant)
            .Where(l => requested.Select(r => r.ItemId).Contains(l.ItemId))
            .Select(l => new { l.ItemId, l.Uom, l.UomFactor, l.BrandId, l.CategoryId })
            .ToListAsync();

        var lines = new List<DistributionOrderLineDto>();

        for (var i = 0; i < requested.Count; i++)
        {
            var input = requested[i];
            var resolved = resolutions[i];

            if (input.Quantity <= 0)
                throw new InvalidOperationException($"{resolved.ItemName} has no quantity.");

            var meta = uomFactors.FirstOrDefault(u => u.ItemId == input.ItemId
                && string.Equals(u.Uom, input.Uom, StringComparison.OrdinalIgnoreCase));

            var factor = meta?.UomFactor ?? 1;
            var unitPrice = input.UnitPrice ?? resolved.ResolvedPrice;

            if (input.UnitPrice.HasValue && resolved.MinimumPrice > 0 && input.UnitPrice < resolved.MinimumPrice)
                warnings.Add($"{resolved.ItemName} is priced below its floor of {resolved.MinimumPrice:N2}.");

            var gross = unitPrice * input.Quantity;
            var discount = Math.Round(gross * input.DiscountPercent / 100m, 4);
            var net = gross - discount;
            var tax = Math.Round(net * resolved.TaxPercent / 100m, 4);

            lines.Add(new DistributionOrderLineDto
            {
                DisplayOrder = i,
                ItemId = input.ItemId,
                ItemName = resolved.ItemName,
                BrandId = meta?.BrandId,
                CategoryId = meta?.CategoryId,
                BatchId = input.BatchId,
                Uom = input.Uom,
                UomFactor = factor,
                Quantity = input.Quantity,
                BaseQuantity = input.Quantity * factor,
                UnitPrice = unitPrice,
                Mrp = resolved.Mrp,
                DiscountPercent = input.DiscountPercent,
                DiscountAmount = discount,
                TaxPercent = resolved.TaxPercent,
                TaxAmount = tax,
                LineTotal = net + tax,
                PriceScope = resolved.WinningScope,
                IsPriceOverridden = input.UnitPrice.HasValue && input.UnitPrice != resolved.ResolvedPrice,
                PriceOverrideReason = input.PriceOverrideReason,
                Note = input.Note,
            });
        }

        return lines;
    }

    /// <summary>Pushes on-invoice scheme discounts down onto the lines that earned them.</summary>
    private static void ApplySchemeBenefits(
        List<DistributionOrderLineDto> lines, List<SchemeApplicationDto> applications)
    {
        var onInvoice = applications
            .Where(a => a.SettlementMode == SchemeSettlementMode.OnInvoice && a.DiscountAmount > 0)
            .ToList();

        if (onInvoice.Count == 0 || lines.Count == 0) return;

        var totalDiscount = onInvoice.Sum(a => a.DiscountAmount);
        var basis = lines.Where(l => !l.IsFreeGoods).Sum(l => l.LineTotal);
        if (basis <= 0) return;

        // Spread pro-rata across the lines, then push any rounding remainder onto the largest one
        // so the sum of the lines always equals the order total exactly.
        decimal applied = 0;
        var eligible = lines.Where(l => !l.IsFreeGoods).OrderByDescending(l => l.LineTotal).ToList();

        for (var i = 0; i < eligible.Count; i++)
        {
            var line = eligible[i];
            var share = i == eligible.Count - 1
                ? totalDiscount - applied
                : Math.Round(totalDiscount * (line.LineTotal / basis), 4);

            line.SchemeDiscountAmount = share;
            line.LineTotal -= share;
            line.SchemeId = onInvoice[0].SchemeId;
            line.SchemeName = onInvoice[0].SchemeName;
            applied += share;
        }
    }

    /// <summary>
    /// Turns free-goods entitlements into real lines. They cost stock and cost margin, so they
    /// have to exist on the document rather than being a note.
    /// </summary>
    private static List<DistributionOrderLineDto> BuildFreeGoodsLines(
        List<DistributionOrderLineDto> lines, List<SchemeApplicationDto> applications)
    {
        var result = new List<DistributionOrderLineDto>();
        var order = lines.Count;

        foreach (var application in applications.Where(a => a.FreeQuantity > 0 && a.FreeItemId.HasValue))
        {
            var template = lines.FirstOrDefault(l => l.ItemId == application.FreeItemId) ?? lines.FirstOrDefault();

            result.Add(new DistributionOrderLineDto
            {
                DisplayOrder = order++,
                ItemId = application.FreeItemId!.Value,
                ItemName = application.FreeItemName ?? template?.ItemName ?? "Free goods",
                ItemCode = template?.ItemCode,
                BrandId = template?.BrandId,
                CategoryId = template?.CategoryId,
                Uom = template?.Uom ?? "PCS",
                UomFactor = template?.UomFactor ?? 1,
                Quantity = application.FreeQuantity,
                BaseQuantity = application.FreeQuantity * (template?.UomFactor ?? 1),
                UnitPrice = 0,
                Mrp = template?.Mrp ?? 0,
                LineTotal = 0,
                UnitCost = template?.UnitCost ?? 0,
                MarginAmount = -(application.FreeQuantity * (template?.UnitCost ?? 0)),
                IsFreeGoods = true,
                SchemeId = application.SchemeId,
                SchemeName = application.SchemeName,
                Note = application.BenefitDescription,
            });
        }

        return result;
    }

    private static void Total(OrderQuoteDto quote, List<DistributionOrderLineDto> lines, List<SchemeApplicationDto> applications)
    {
        quote.SubTotal = lines.Sum(l => l.UnitPrice * l.Quantity);
        quote.DiscountAmount = lines.Sum(l => l.DiscountAmount);
        quote.SchemeDiscountAmount = lines.Sum(l => l.SchemeDiscountAmount);
        quote.TaxAmount = lines.Sum(l => l.TaxAmount);
        quote.FreeGoodsValue = applications.Sum(a => a.FreeQuantity) > 0
            ? lines.Where(l => l.IsFreeGoods).Sum(l => l.Quantity * l.UnitCost)
            : 0;
        quote.TotalAmount = lines.Sum(l => l.LineTotal);
        quote.MarginAmount = lines.Sum(l => l.MarginAmount);
        quote.MarginPercent = quote.SubTotal == 0 ? 0 : Math.Round(quote.MarginAmount / quote.SubTotal * 100, 2);
    }

    private static void ApplyTotals(DistributionOrder order, OrderQuoteDto quote)
    {
        order.SubTotal = quote.SubTotal;
        order.DiscountAmount = quote.DiscountAmount;
        order.SchemeDiscountAmount = quote.SchemeDiscountAmount;
        order.FreeGoodsValue = quote.FreeGoodsValue;
        order.TaxAmount = quote.TaxAmount;
        order.TotalAmount = quote.TotalAmount + order.FreightAmount;
        order.MarginAmount = quote.MarginAmount;
        order.CostAmount = quote.Lines.Sum(l => l.UnitCost * l.Quantity);
        order.LineCount = quote.Lines.Count;
        order.TotalQuantity = quote.Lines.Sum(l => l.BaseQuantity);
    }

    private static void RecomputeTotals(DistributionOrder order)
    {
        var lines = order.Lines.Where(l => !l.IsDeleted).ToList();
        order.SubTotal = lines.Sum(l => l.UnitPrice * l.Quantity);
        order.DiscountAmount = lines.Sum(l => l.DiscountAmount);
        order.SchemeDiscountAmount = lines.Sum(l => l.SchemeDiscountAmount);
        order.TaxAmount = lines.Sum(l => l.TaxAmount);
        order.TotalAmount = lines.Sum(l => l.LineTotal) + order.FreightAmount;
        order.MarginAmount = lines.Sum(l => l.MarginAmount);
        order.CostAmount = lines.Sum(l => l.UnitCost * l.Quantity);
        order.LineCount = lines.Count;
        order.TotalQuantity = lines.Sum(l => l.BaseQuantity);
        order.FreeGoodsValue = lines.Where(l => l.IsFreeGoods).Sum(l => l.Quantity * l.UnitCost);
    }

    private async Task<(bool Required, string? Reason)> NeedsApprovalAsync(
        List<DistributionOrderLineDto> lines, OrderQuoteDto quote, Guid? fieldRepId, DistributionSettings? settings)
    {
        if (settings is null) return (false, null);

        if (quote.Credit.RequiresOverride)
            return (true, quote.Credit.Message ?? "Credit limit exceeded");

        if (settings.MarginFloorPercent > 0 && quote.MarginPercent < settings.MarginFloorPercent)
            return (true, $"Margin {quote.MarginPercent:N2}% is below the floor of {settings.MarginFloorPercent:N2}%");

        var deepest = lines.Count == 0 ? 0 : lines.Max(l => l.DiscountPercent);

        if (fieldRepId.HasValue)
        {
            var authority = await db.FieldReps.ForTenant(tenant)
                .Where(r => r.Id == fieldRepId).Select(r => r.DiscountAuthorityPercent).FirstOrDefaultAsync();

            if (authority > 0 && deepest > authority)
                return (true, $"Discount of {deepest:N2}% is above this rep's authority of {authority:N2}%");
        }

        if (settings.DiscountApprovalThreshold > 0
            && quote.DiscountAmount + quote.SchemeDiscountAmount > settings.DiscountApprovalThreshold)
            return (true, $"Total discount is above the approval threshold of {settings.DiscountApprovalThreshold:N2}");

        if (lines.Any(l => l.IsPriceOverridden))
            return (true, "A price was overridden on this order");

        return (false, null);
    }

    private async Task CheckAvailabilityAsync(
        List<DistributionOrderLineDto> lines, Guid? warehouseId, Guid? vanUnitId, List<string> warnings)
    {
        if (vanUnitId.HasValue)
        {
            var balances = await db.VanStockBalances.ForTenant(tenant)
                .Where(b => b.VanUnitId == vanUnitId && b.Compartment == VanCompartment.Sellable)
                .ToListAsync();

            foreach (var line in lines)
            {
                var available = balances.Where(b => b.ItemId == line.ItemId)
                    .Sum(b => b.Quantity - b.ReservedQuantity);

                line.AvailableQuantity = available;
                if (available < line.BaseQuantity)
                    warnings.Add($"Only {available:N0} of {line.ItemName} is on the van.");
            }
            return;
        }

        if (!warehouseId.HasValue) return;

        foreach (var line in lines)
        {
            var lookup = new StockAvailabilityLookupEvent
            {
                ProductId = line.ItemId,
                WarehouseId = warehouseId.Value,
                CompanyId = tenant.CompanyId,
            };

            await events.PublishAsync(lookup);
            var completed = await Task.WhenAny(lookup.Result.Task, Task.Delay(TimeSpan.FromSeconds(3)));
            if (completed != lookup.Result.Task) continue;

            var data = await lookup.Result.Task;
            // Fail open on an unresolved lookup: a transient Inventory hiccup must not stop the
            // market from selling.
            if (!data.Resolved) continue;

            line.AvailableQuantity = data.QuantityOnHand;
            if (data.QuantityOnHand < line.BaseQuantity)
                warnings.Add($"Only {data.QuantityOnHand:N0} of {line.ItemName} is in stock.");
        }
    }

    private async Task CheckAuthorisationAsync(
        List<DistributionOrderLineDto> lines, Guid? partnerId, List<string> blockers)
    {
        if (partnerId is null) return;

        var authorisations = await db.PartnerAuthorisations.ForTenant(tenant)
            .Where(a => a.PartnerId == partnerId
                        && a.EffectiveFrom <= DateTime.UtcNow
                        && (a.EffectiveTo == null || a.EffectiveTo >= DateTime.UtcNow))
            .ToListAsync();

        // No authorisation rows at all means the partner may carry anything. Rows present means
        // the list is a whitelist, and ordering off it is refused with a readable reason.
        if (authorisations.Count == 0) return;

        foreach (var line in lines.Where(l => !l.IsFreeGoods))
        {
            var allowed = authorisations.Any(a =>
                (a.ItemId is not null && a.ItemId == line.ItemId) ||
                (a.BrandId is not null && a.BrandId == line.BrandId) ||
                (a.CategoryId is not null && a.CategoryId == line.CategoryId));

            if (!allowed)
                blockers.Add($"This partner is not authorised to carry {line.ItemName}.");
        }
    }

    private async Task DecorateCatalogueAsync(
        List<CatalogueItemDto> items, Guid? outletId, Guid? partnerId,
        Guid? warehouseId, Guid? vanUnitId, DateTime asOf)
    {
        var itemIds = items.Select(i => i.ItemId).ToList();

        if (outletId.HasValue)
        {
            var history = await db.OrderLines.ForTenant(tenant)
                .Where(l => itemIds.Contains(l.ItemId)
                            && db.Orders.ForTenant(tenant).Any(o => o.Id == l.OrderId && o.OutletId == outletId))
                .GroupBy(l => l.ItemId)
                .Select(g => new
                {
                    ItemId = g.Key,
                    LastQuantity = g.OrderByDescending(x => x.CreatedAt).First().Quantity,
                    LastAt = g.Max(x => x.CreatedAt),
                })
                .ToDictionaryAsync(x => x.ItemId);

            foreach (var item in items)
            {
                if (history.TryGetValue(item.ItemId, out var h))
                {
                    item.LastOrderedQuantity = h.LastQuantity;
                    item.LastOrderedAt = h.LastAt;
                    // A rough suggestion: repeat the last order. It is what a rep does anyway, and
                    // an over-clever model they do not trust gets ignored.
                    item.SuggestedQuantity = h.LastQuantity;
                }
                else
                {
                    item.IsNeverBought = true;
                }
            }
        }

        var liveSchemes = await schemes.GetApplicableSchemesAsync(outletId, partnerId, asOf);
        foreach (var item in items)
        {
            var applicable = liveSchemes.Where(s =>
                s.Products.Count == 0 || s.Products.Any(p => p.ItemId == item.ItemId && !p.IsExcluded)).ToList();

            item.ActiveSchemes = applicable.Select(s => s.Name).Take(3).ToList();
            item.IsFocusItem = applicable.Any(s => s.Kind == TradeSchemeKind.SamplingFreeIssue
                                                   || s.Priority > 0);
        }

        if (vanUnitId.HasValue)
        {
            var balances = await db.VanStockBalances.ForTenant(tenant)
                .Where(b => b.VanUnitId == vanUnitId && b.Compartment == VanCompartment.Sellable)
                .GroupBy(b => b.ItemId)
                .Select(g => new
                {
                    ItemId = g.Key,
                    Quantity = g.Sum(x => x.Quantity - x.ReservedQuantity),
                    NearestExpiry = g.Min(x => x.ExpiryDate),
                })
                .ToDictionaryAsync(x => x.ItemId);

            foreach (var item in items)
                if (balances.TryGetValue(item.ItemId, out var b))
                {
                    item.AvailableQuantity = b.Quantity;
                    item.NearestExpiryDate = b.NearestExpiry;
                }
        }

        if (partnerId.HasValue)
        {
            var authorisations = await db.PartnerAuthorisations.ForTenant(tenant)
                .Where(a => a.PartnerId == partnerId).ToListAsync();

            if (authorisations.Count > 0)
                foreach (var item in items)
                {
                    item.IsAuthorised = authorisations.Any(a =>
                        (a.ItemId is not null && a.ItemId == item.ItemId) ||
                        (a.BrandId is not null && a.BrandId == item.BrandId) ||
                        (a.CategoryId is not null && a.CategoryId == item.CategoryId));

                    if (!item.IsAuthorised)
                        item.UnauthorisedReason = "Not authorised for this partner";
                }
        }
    }

    // ═══ State & side effects ════════════════════════════════════════════════

    private async Task TransitionAsync(
        DistributionOrder order, DistributionOrderStatus to, string? note, Guid userId)
    {
        AddStatusEvent(order, order.Status, to, note, userId);
        order.Status = to;
        order.StampUpdated(userId);

        if (to == DistributionOrderStatus.Dispatched) order.DispatchedAt = DateTime.UtcNow;
        if (to == DistributionOrderStatus.Delivered) order.DeliveredAt = DateTime.UtcNow;

        await Task.CompletedTask;
    }

    private void AddStatusEvent(
        DistributionOrder order, DistributionOrderStatus from, DistributionOrderStatus to, string? note, Guid userId)
        => order.StatusEvents.Add(new OrderStatusEvent
        {
            OrderId = order.Id,
            FromStatus = from,
            ToStatus = to,
            OccurredAt = DateTime.UtcNow,
            ActorUserId = userId,
            Note = note,
        }.StampNew(tenant, userId));

    private async Task RaiseApprovalAsync(DistributionOrder order, string? reason, Guid userId)
    {
        order.Approvals.Add(new OrderApproval
        {
            OrderId = order.Id,
            StepNumber = 1,
            StepName = "Manager approval",
            TriggerReason = reason,
        }.StampNew(tenant, userId));

        await TransitionAsync(order, DistributionOrderStatus.PendingApproval, reason, userId);
        await db.SaveChangesAsync();
    }

    private async Task PersistSchemeApplicationsAsync(
        DistributionOrder order, List<SchemeApplicationDto> applications, Guid userId)
    {
        if (applications.Count == 0) return;

        foreach (var application in applications)
        {
            db.SchemeApplications.Add(new SchemeApplication
            {
                SchemeId = application.SchemeId,
                SchemeName = application.SchemeName,
                SchemeKind = application.SchemeKind,
                OrderId = order.Id,
                OutletId = order.OutletId,
                PartnerId = order.PartnerId,
                AppliedAt = order.OrderDate,
                QualifyingQuantity = application.QualifyingQuantity,
                QualifyingValue = application.QualifyingValue,
                SlabNumber = application.SlabNumber,
                FreeQuantity = application.FreeQuantity,
                FreeItemId = application.FreeItemId,
                FreeItemName = application.FreeItemName,
                DiscountAmount = application.DiscountAmount,
                PayoutAmount = application.PayoutAmount,
                PointsAwarded = application.PointsAwarded,
                BenefitValue = application.BenefitValue,
                SettlementMode = application.SettlementMode,
                BenefitDescription = application.BenefitDescription,
            }.StampNew(tenant, userId));

            // Consume the budget as the benefit is granted, and stop the scheme when it runs out
            // rather than trusting somebody to be watching.
            var scheme = await db.Schemes.ForCompany(tenant).FirstOrDefaultAsync(s => s.Id == application.SchemeId);
            if (scheme is null) continue;

            scheme.ConsumedAmount += application.BenefitValue;
            scheme.ApplicationCount++;
            scheme.QualifyingSalesValue += application.QualifyingValue;
            scheme.StampUpdated(userId);

            if (scheme.StopWhenBudgetExhausted && scheme.BudgetAmount > 0
                && scheme.ConsumedAmount >= scheme.BudgetAmount)
                scheme.Status = SchemeStatus.Exhausted;

            db.SchemeBudgetLedger.Add(new SchemeBudgetLedger
            {
                SchemeId = scheme.Id,
                OccurredAt = DateTime.UtcNow,
                Amount = -application.BenefitValue,
                BalanceAfter = scheme.BudgetAmount - scheme.ConsumedAmount,
                Reason = $"Applied on order {order.OrderNumber}",
                OrderId = order.Id,
                ActorUserId = userId,
            }.StampNew(tenant, userId));
        }

        await db.SaveChangesAsync();
    }

    private async Task ReverseSchemeApplicationsAsync(Guid orderId, string reason, Guid userId)
    {
        var applications = await db.SchemeApplications.ForTenant(tenant)
            .Where(a => a.OrderId == orderId && !a.IsReversed).ToListAsync();

        foreach (var application in applications)
        {
            application.IsReversed = true;
            application.ReversalReason = reason;
            application.StampUpdated(userId);

            var scheme = await db.Schemes.ForCompany(tenant).FirstOrDefaultAsync(s => s.Id == application.SchemeId);
            if (scheme is null) continue;

            scheme.ConsumedAmount = Math.Max(0, scheme.ConsumedAmount - application.BenefitValue);
            scheme.ApplicationCount = Math.Max(0, scheme.ApplicationCount - 1);
            if (scheme.Status == SchemeStatus.Exhausted && scheme.ConsumedAmount < scheme.BudgetAmount)
                scheme.Status = SchemeStatus.Active;
            scheme.StampUpdated(userId);

            db.SchemeBudgetLedger.Add(new SchemeBudgetLedger
            {
                SchemeId = scheme.Id,
                OccurredAt = DateTime.UtcNow,
                Amount = application.BenefitValue,
                BalanceAfter = scheme.BudgetAmount - scheme.ConsumedAmount,
                Reason = reason,
                OrderId = orderId,
                ActorUserId = userId,
            }.StampNew(tenant, userId));
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// A van sale is complete at the counter: stock leaves the van now, the order is delivered
    /// and invoiced now, and the movements are written so settlement can reconcile them tonight.
    /// </summary>
    private async Task SettleVanSaleAsync(DistributionOrder order, Guid vanUnitId, Guid userId)
    {
        var balances = await db.VanStockBalances.ForTenant(tenant)
            .Where(b => b.VanUnitId == vanUnitId && b.Compartment == VanCompartment.Sellable)
            .ToListAsync();

        foreach (var line in order.Lines.Where(l => !l.IsDeleted))
        {
            var remaining = line.BaseQuantity;

            // FEFO on the van too: the shortest-dated stock leaves first, or the van becomes a
            // rolling expiry write-off.
            foreach (var balance in balances
                .Where(b => b.ItemId == line.ItemId && b.Quantity > 0)
                .OrderBy(b => b.ExpiryDate ?? DateTime.MaxValue))
            {
                if (remaining <= 0) break;

                var take = Math.Min(remaining, balance.Quantity);
                balance.Quantity -= take;
                balance.LastMovementAt = DateTime.UtcNow;
                balance.StampUpdated(userId);
                remaining -= take;

                db.VanStockMovements.Add(new VanStockMovement
                {
                    VanUnitId = vanUnitId,
                    FieldDayId = order.FieldDayId,
                    VisitId = order.VisitId,
                    Kind = line.IsFreeGoods ? VanMovementKind.FreeIssue : VanMovementKind.Sale,
                    OccurredAt = DateTime.UtcNow,
                    ItemId = line.ItemId,
                    ItemName = line.ItemName,
                    BatchId = balance.BatchId,
                    BatchNumber = balance.BatchNumber,
                    ExpiryDate = balance.ExpiryDate,
                    Compartment = VanCompartment.Sellable,
                    Uom = balance.Uom,
                    Quantity = -take,
                    UnitCost = balance.UnitCost,
                    UnitPrice = line.UnitPrice,
                    Value = take * line.UnitPrice,
                    BalanceAfter = balance.Quantity,
                    ReferenceId = order.Id,
                    ReferenceType = "DistributionOrder",
                    ReferenceNumber = order.OrderNumber,
                    FieldRepId = order.FieldRepId,
                }.StampNew(tenant, userId));
            }

            if (remaining > 0)
                throw new InvalidOperationException(
                    $"The van does not have enough {line.ItemName}: {remaining:N0} short.");

            line.PickedQuantity = line.BaseQuantity;
            line.DispatchedQuantity = line.BaseQuantity;
            line.DeliveredQuantity = line.BaseQuantity;
            line.StampUpdated(userId);
        }

        order.FillRatePercent = 100;
        await TransitionAsync(order, DistributionOrderStatus.Delivered, "Sold from the van", userId);
        await TransitionAsync(order, DistributionOrderStatus.Invoiced, "Invoiced at the counter", userId);

        await db.SaveChangesAsync();
    }

    private async Task UpdateVisitAndDayAsync(DistributionOrder order, Guid userId)
    {
        if (order.VisitId.HasValue)
        {
            var visit = await db.Visits.ForTenant(tenant).FirstOrDefaultAsync(v => v.Id == order.VisitId);
            if (visit is not null)
            {
                visit.OrderId = order.Id;
                visit.OrderValue += order.TotalAmount;
                visit.LinesSold = order.LineCount;
                visit.IsProductive = true;
                visit.Status = VisitStatus.OrderTaken;
                visit.StampUpdated(userId);
            }
        }

        if (order.FieldDayId.HasValue)
        {
            var day = await db.FieldDays.ForTenant(tenant).FirstOrDefaultAsync(d => d.Id == order.FieldDayId);
            if (day is not null)
            {
                day.OrderValue += order.TotalAmount;
                if (order.Status >= DistributionOrderStatus.Delivered) day.InvoicedValue += order.TotalAmount;
                day.DistinctLinesSold += order.LineCount;
                day.StampUpdated(userId);
            }
        }

        if (order.OutletId.HasValue)
        {
            var outlet = await db.Outlets.ForTenant(tenant).FirstOrDefaultAsync(o => o.Id == order.OutletId);
            if (outlet is not null)
            {
                outlet.LastOrderAt = order.OrderDate;
                outlet.FirstOrderAt ??= order.OrderDate;
                outlet.LifetimeSales += order.TotalAmount;
                outlet.StampUpdated(userId);
            }
        }
    }

    private async Task GuardOutletIsBillableAsync(Guid? outletId)
    {
        if (outletId is null) return;

        var outlet = await db.Outlets.ForTenant(tenant)
            .Where(o => o.Id == outletId)
            .Select(o => new { o.Name, o.IsApproved, o.Status })
            .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("That outlet no longer exists.");

        if (!outlet.IsApproved)
            throw new InvalidOperationException(
                $"{outlet.Name} is still waiting for approval and cannot be invoiced yet.");

        if (outlet.Status is OutletStatus.Blacklisted or OutletStatus.PermanentlyClosed)
            throw new InvalidOperationException($"{outlet.Name} is {outlet.Status} and cannot be sold to.");
    }

    private async Task<Guid?> ResolveTerritoryAsync(Guid? outletId, Guid? partnerId)
    {
        if (outletId.HasValue)
        {
            var t = await db.Outlets.ForTenant(tenant)
                .Where(o => o.Id == outletId).Select(o => o.TerritoryId).FirstOrDefaultAsync();
            if (t.HasValue) return t;
        }

        if (partnerId.HasValue)
            return await db.Partners.ForTenant(tenant)
                .Where(p => p.Id == partnerId).Select(p => p.TerritoryId).FirstOrDefaultAsync();

        return null;
    }

    private async Task<DistributionOrder?> LoadFullAsync(Guid? orderId, string? idempotencyKey)
        => await db.Orders.ForTenant(tenant)
            .Include(o => o.Outlet).Include(o => o.Partner)
            .Include(o => o.Lines.Where(l => !l.IsDeleted))
            .Include(o => o.StatusEvents)
            .Include(o => o.Approvals.Where(a => !a.IsDeleted))
            .FirstOrDefaultAsync(o => orderId != null ? o.Id == orderId : o.IdempotencyKey == idempotencyKey);
}
