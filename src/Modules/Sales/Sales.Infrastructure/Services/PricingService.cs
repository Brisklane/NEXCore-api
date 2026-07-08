using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Events;
using Sales.Application.DTOs;
using Sales.Application.Services.Interfaces;
using Sales.Domain.Entities;
using Sales.Domain.Enums;
using Sales.Infrastructure.Repositories.Interfaces;

namespace Sales.Infrastructure.Services;

/// <summary>
/// Central pricing &amp; promotion engine (v1 — per-line discounts).
///
/// Pipeline:
///   1. Resolve a server-authoritative unit price per line: price-list item → catalog base price.
///   2. Apply active per-line promotions (PercentageOff / FixedAmountOff / NewPrice) honouring
///      priority, stackability, conditions, price target and per-transaction caps.
///   3. Apply an optional order-level coupon (Percentage / FixedAmount).
///   4. Return the priced basket with totals.
///
/// BuyXGetY / FreeItem promotions and tax are intentionally out of scope for v1.
/// </summary>
public class PricingService : IPricingService
{
    private readonly IPriceListRepository _priceLists;
    private readonly IPromotionRepository _promotions;
    private readonly ICouponRepository _coupons;
    private readonly IPosStoreRepository _stores;
    private readonly ITaxRuleRepository _taxRules;
    private readonly IPosSettingsRepository _posSettings;
    private readonly IEventPublisher _events;
    private readonly ILogger<PricingService> _logger;

    public PricingService(
        IPriceListRepository priceLists,
        IPromotionRepository promotions,
        ICouponRepository coupons,
        IPosStoreRepository stores,
        ITaxRuleRepository taxRules,
        IPosSettingsRepository posSettings,
        IEventPublisher events,
        ILogger<PricingService> logger)
    {
        _priceLists = priceLists;
        _promotions = promotions;
        _coupons    = coupons;
        _stores     = stores;
        _taxRules   = taxRules;
        _posSettings = posSettings;
        _events     = events;
        _logger     = logger;
    }

    public async Task<PricedOrderDto> PriceOrderAsync(PriceOrderRequestDto request)
    {
        var result = new PricedOrderDto();
        var nowUtc  = DateTime.UtcNow;

        // ── 1. Resolve the effective price list ───────────────────────────────────
        var priceListId = request.PriceListId;
        if (priceListId is null && request.PosStoreId is not null)
        {
            var store = await _stores.GetByIdAsync(request.PosStoreId.Value);
            priceListId = store?.DefaultPriceListId;
        }
        PriceList? priceList = priceListId is null ? null : await _priceLists.GetWithItemsAsync(priceListId.Value);

        // ── 2. Resolve a server price for each line ───────────────────────────────
        var lines = new List<WorkingLine>();
        foreach (var reqLine in request.Lines)
        {
            var item = await LookupItemAsync(reqLine.ProductId);

            var listPrice = ResolveUnitPrice(priceList, reqLine, item, result.Warnings);

            lines.Add(new WorkingLine
            {
                Request      = reqLine,
                CategoryId   = item.CategoryId,
                ProductCode  = reqLine.ProductCode  ?? item.ProductCode,
                ProductName  = reqLine.ProductName  ?? item.ProductName,
                ListUnitPrice = listPrice,
                NetUnitPrice  = listPrice,
            });
        }

        var grossAmount = lines.Sum(l => l.ListUnitPrice * l.Request.Quantity);

        // ── 3. Apply per-line promotions ──────────────────────────────────────────
        await ApplyPromotionsAsync(lines, grossAmount, priceListId, request, nowUtc);

        var lineDiscount = lines.Sum(l => l.DiscountAmount);
        var subtotalAfterLines = grossAmount - lineDiscount;

        // ── 4. Apply an order-level coupon ────────────────────────────────────────
        var couponDiscount = await ApplyCouponAsync(request, lines, subtotalAfterLines, nowUtc, result);

        // ── 5. Resolve tax per line (on the net, discounted line amount) ──────────
        await ApplyTaxAsync(lines, request);
        var taxAmount = lines.Sum(l => l.TaxAmount);

        // ── 6. Totals ─────────────────────────────────────────────────────────────
        result.Lines = lines.Select(l => l.ToDto()).ToList();
        result.GrossAmount          = Round(grossAmount);
        result.LineDiscountAmount   = Round(lineDiscount);
        result.CouponDiscountAmount = Round(couponDiscount);
        result.DiscountAmount       = Round(lineDiscount + couponDiscount);
        result.SubtotalAmount       = Round(grossAmount - lineDiscount - couponDiscount);
        result.TaxAmount            = Round(taxAmount);
        result.TotalAmount          = result.SubtotalAmount + result.TaxAmount;
        return result;
    }

    // ── Price resolution ─────────────────────────────────────────────────────────

    private static decimal ResolveUnitPrice(PriceList? priceList, PriceOrderLineRequestDto line,
                                            ItemPriceData item, List<string> warnings)
    {
        // Price-list item wins (respecting quantity breaks and validity)
        var now = DateTime.UtcNow;
        var pli = priceList?.Items?
            .Where(p => p.ProductId == line.ProductId && p.IsActive
                        && p.ValidFrom <= now && (p.ValidTo == null || p.ValidTo >= now)
                        && (p.MinQuantity == null || line.Quantity >= p.MinQuantity)
                        && (p.MaxQuantity == null || line.Quantity <= p.MaxQuantity))
            .OrderByDescending(p => p.MinQuantity ?? 0)
            .FirstOrDefault();
        if (pli is not null) return pli.UnitPrice;

        // Catalog base price next
        if (item.BasePrice > 0) return item.BasePrice;

        warnings.Add($"No price found for product {line.ProductCode ?? line.ProductId.ToString()} — priced at 0.");
        return 0m;
    }

    // ── Promotion application ──────────────────────────────────────────────────────

    private async Task ApplyPromotionsAsync(List<WorkingLine> lines, decimal grossAmount,
                                            Guid? priceListId, PriceOrderRequestDto request, DateTime nowUtc)
    {
        var date = DateOnly.FromDateTime(nowUtc);
        var time = TimeOnly.FromDateTime(nowUtc);

        var active = await _promotions.GetActiveAsync(date, time);   // ordered by Priority desc, Items included

        var eligible = active.Where(p =>
            // auto-applied, or activated by the supplied promotion code
            (p.IsAutoApplied ||
                (!string.IsNullOrWhiteSpace(p.PromotionCode) &&
                 string.Equals(p.PromotionCode, request.PromotionCode, StringComparison.OrdinalIgnoreCase)))
            // customer targeting (v1: AllCustomers, SpecificContact, PriceList; loyalty-gated skipped)
            && IsCustomerEligible(p, request.ContactId, priceListId)
            // order-level minimum
            && (p.MinOrderAmount is null || grossAmount >= p.MinOrderAmount)
            // global usage cap
            && (p.MaxUsageCount is null || p.CurrentUsageCount < p.MaxUsageCount));

        foreach (var promo in eligible)
        {
            foreach (var rule in promo.Items.Where(i => !i.IsDeleted))
            {
                switch (rule.DiscountType)
                {
                    case PromotionDiscountType.PercentageOff:
                    case PromotionDiscountType.FixedAmountOff:
                    case PromotionDiscountType.NewPrice:
                        ApplyPerLineDiscount(promo, rule, lines, grossAmount);
                        break;
                    case PromotionDiscountType.BuyXGetYFree:
                        ApplyBuyXGetY(promo, rule, lines, grossAmount);
                        break;
                    case PromotionDiscountType.FreeItem:
                        ApplyFreeItem(promo, rule, lines, grossAmount);
                        break;
                }
            }
        }
    }

    /// <summary>PercentageOff / FixedAmountOff / NewPrice applied to each matching line.</summary>
    private static void ApplyPerLineDiscount(Promotion promo, PromotionItem rule, List<WorkingLine> lines, decimal grossAmount)
    {
        foreach (var line in lines)
        {
            if (line.Locked) continue;                       // a non-stackable promo already claimed this line
            if (!RuleMatchesLine(rule, line)) continue;
            if (!RuleConditionMet(rule, line.Request.Quantity, grossAmount)) continue;
            if (rule.PriceTarget == PromotionPriceTarget.RegularPrice && line.DiscountAmount > 0) continue;

            var discountPerUnit = rule.DiscountType switch
            {
                PromotionDiscountType.PercentageOff  => line.NetUnitPrice * rule.Value / 100m,
                PromotionDiscountType.FixedAmountOff => rule.Value,
                PromotionDiscountType.NewPrice       => Math.Max(0m, line.NetUnitPrice - rule.Value),
                _                                    => 0m,
            };
            if (discountPerUnit <= 0) continue;

            var eligibleQty = rule.MaxDiscountedQuantity.HasValue
                ? Math.Min(line.Request.Quantity, rule.MaxDiscountedQuantity.Value)
                : line.Request.Quantity;

            var lineDiscount = discountPerUnit * eligibleQty;
            ApplyLineDiscount(promo, line, lineDiscount);
        }
    }

    /// <summary>
    /// BuyXGetYFree. Same-item (FreeItemId null/==ItemId): free units come from the line's own
    /// quantity — floor(qty / (Buy+Get)) × Get. Different free item: floor(triggerQty / Buy) × Get
    /// applied to the free product's line (must be in the basket). Free units are zeroed at their
    /// current net price.
    /// </summary>
    private static void ApplyBuyXGetY(Promotion promo, PromotionItem rule, List<WorkingLine> lines, decimal grossAmount)
    {
        if (rule.BuyQuantity is not > 0 || rule.GetQuantity is not > 0) return;
        var freeItemId = rule.FreeItemId ?? rule.ItemId;
        if (freeItemId is null) return;                      // category-only same-item BOGO not supported in v1
        if (rule.IsConditional && rule.ConditionType == PromotionConditionType.MinOrderAmount
            && grossAmount < (rule.ConditionAmount ?? 0)) return;

        var sameItem    = rule.FreeItemId is null || rule.FreeItemId == rule.ItemId;
        var triggerQty  = lines.Where(l => RuleMatchesLine(rule, l)).Sum(l => l.Request.Quantity);
        if (triggerQty <= 0) return;

        var freeUnits = sameItem
            ? Math.Floor(triggerQty / (rule.BuyQuantity.Value + rule.GetQuantity.Value)) * rule.GetQuantity.Value
            : Math.Floor(triggerQty / rule.BuyQuantity.Value) * rule.GetQuantity.Value;

        if (rule.MaxDiscountedQuantity.HasValue)
            freeUnits = Math.Min(freeUnits, rule.MaxDiscountedQuantity.Value);
        if (freeUnits <= 0) return;

        DiscountFreeUnits(promo, lines.Where(l => l.Request.ProductId == freeItemId.Value), freeUnits);
    }

    /// <summary>
    /// FreeItem: grants GetQuantity (default 1) free units of FreeItemId. The free product must be
    /// present in the basket — its units are zeroed.
    /// </summary>
    private static void ApplyFreeItem(Promotion promo, PromotionItem rule, List<WorkingLine> lines, decimal grossAmount)
    {
        if (rule.FreeItemId is null) return;
        if (rule.IsConditional && rule.ConditionType == PromotionConditionType.MinOrderAmount
            && grossAmount < (rule.ConditionAmount ?? 0)) return;

        var freeUnits = rule.GetQuantity ?? 1m;
        if (rule.MaxDiscountedQuantity.HasValue)
            freeUnits = Math.Min(freeUnits, rule.MaxDiscountedQuantity.Value);
        if (freeUnits <= 0) return;

        DiscountFreeUnits(promo, lines.Where(l => l.Request.ProductId == rule.FreeItemId.Value), freeUnits);
    }

    /// <summary>Zeroes up to <paramref name="freeUnits"/> units across the given free-item lines.</summary>
    private static void DiscountFreeUnits(Promotion promo, IEnumerable<WorkingLine> freeLines, decimal freeUnits)
    {
        var remaining = freeUnits;
        foreach (var line in freeLines)
        {
            if (remaining <= 0) break;
            if (line.Locked) continue;

            var applyQty  = Math.Min(remaining, line.Request.Quantity);
            var discount  = applyQty * line.NetUnitPrice;     // make those units free at current net price
            ApplyLineDiscount(promo, line, discount);
            remaining -= applyQty;
        }
    }

    /// <summary>Adds a capped discount to a line, records the promotion, and applies the stack lock.</summary>
    private static void ApplyLineDiscount(Promotion promo, WorkingLine line, decimal discount)
    {
        if (line.Locked) return;

        var maxRemaining = (line.ListUnitPrice * line.Request.Quantity) - line.DiscountAmount;
        discount = Math.Min(discount, maxRemaining);          // never discount below zero
        if (discount <= 0) return;

        line.DiscountAmount += discount;
        line.RecalcNet();
        line.AppliedPromotions.Add(new AppliedPromotionDto
        {
            PromotionId    = promo.Id,
            PromotionName  = promo.Name,
            DiscountAmount = Round(discount),
        });

        if (!promo.IsStackable) line.Locked = true;
    }

    private static bool IsCustomerEligible(Promotion p, Guid? contactId, Guid? priceListId) => p.TargetType switch
    {
        PromotionTargetType.AllCustomers    => true,
        PromotionTargetType.SpecificContact => p.TargetContactId.HasValue && p.TargetContactId == contactId,
        PromotionTargetType.PriceList       => p.RequiredPriceListId.HasValue && p.RequiredPriceListId == priceListId,
        PromotionTargetType.LoyaltyTier     => false,   // v1: loyalty tier not resolved here
        _                                   => false,
    };

    private static bool RuleMatchesLine(PromotionItem rule, WorkingLine line) =>
        (rule.ItemId.HasValue && rule.ItemId == line.Request.ProductId) ||
        (rule.ItemCategoryId.HasValue && line.CategoryId.HasValue && rule.ItemCategoryId == line.CategoryId);

    private static bool RuleConditionMet(PromotionItem rule, decimal lineQuantity, decimal grossAmount)
    {
        if (!rule.IsConditional) return true;
        return rule.ConditionType switch
        {
            PromotionConditionType.None           => true,
            PromotionConditionType.MinQuantity    => lineQuantity >= (rule.ConditionQuantity ?? 0),
            PromotionConditionType.ExactQuantity  => lineQuantity == (rule.ConditionQuantity ?? -1),
            PromotionConditionType.MinOrderAmount => grossAmount >= (rule.ConditionAmount ?? 0),
            _                                     => true,
        };
    }

    // ── Coupon application ─────────────────────────────────────────────────────────

    private async Task<decimal> ApplyCouponAsync(PriceOrderRequestDto request, List<WorkingLine> lines,
                                                 decimal subtotalAfterLines, DateTime nowUtc, PricedOrderDto result)
    {
        if (string.IsNullOrWhiteSpace(request.CouponCode)) return 0m;

        var coupon = await _coupons.GetByCodeAsync(request.CouponCode.Trim());
        if (coupon is null || coupon.Status != CouponStatus.Active)
        {
            result.Warnings.Add($"Coupon '{request.CouponCode}' is not valid — ignored.");
            return 0m;
        }
        if (coupon.ValidFrom > nowUtc || (coupon.ValidTo.HasValue && coupon.ValidTo < nowUtc))
        {
            result.Warnings.Add($"Coupon '{coupon.Code}' is outside its validity window — ignored.");
            return 0m;
        }
        if (coupon.MaxUsageCount.HasValue && coupon.UsageCount >= coupon.MaxUsageCount)
        {
            result.Warnings.Add($"Coupon '{coupon.Code}' has reached its usage limit — ignored.");
            return 0m;
        }
        if (!string.IsNullOrWhiteSpace(coupon.ApplicableChannel) &&
            !string.Equals(coupon.ApplicableChannel, request.SalesChannel.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            result.Warnings.Add($"Coupon '{coupon.Code}' is not valid for this channel — ignored.");
            return 0m;
        }
        if (coupon.TargetCustomerId.HasValue && coupon.TargetCustomerId != request.ContactId)
        {
            result.Warnings.Add($"Coupon '{coupon.Code}' is issued to a different customer — ignored.");
            return 0m;
        }

        // Base the coupon on the restricted product/category if set, else the whole net subtotal
        decimal couponBase = subtotalAfterLines;
        if (coupon.RestrictedToProductId.HasValue || coupon.RestrictedToCategoryId.HasValue)
        {
            couponBase = lines
                .Where(l => (coupon.RestrictedToProductId.HasValue && l.Request.ProductId == coupon.RestrictedToProductId)
                         || (coupon.RestrictedToCategoryId.HasValue && l.CategoryId == coupon.RestrictedToCategoryId))
                .Sum(l => l.LineAmount);
        }

        if (coupon.MinOrderAmount.HasValue && subtotalAfterLines < coupon.MinOrderAmount)
        {
            result.Warnings.Add($"Order below the minimum for coupon '{coupon.Code}' — ignored.");
            return 0m;
        }

        decimal discount = coupon.DiscountType switch
        {
            DiscountType.Percentage  => couponBase * coupon.DiscountValue / 100m,
            DiscountType.FixedAmount => coupon.DiscountValue,
            _                        => 0m,
        };

        if (coupon.DiscountType is not (DiscountType.Percentage or DiscountType.FixedAmount))
        {
            result.Warnings.Add($"Coupon '{coupon.Code}' type {coupon.DiscountType} is not supported yet — ignored.");
            return 0m;
        }

        if (coupon.MaxDiscountAmount.HasValue)
            discount = Math.Min(discount, coupon.MaxDiscountAmount.Value);
        discount = Math.Min(discount, couponBase);   // never exceed the base
        if (discount <= 0) return 0m;

        result.CouponCode = coupon.Code;
        return discount;
    }

    // ── Tax resolution ─────────────────────────────────────────────────────────────

    private async Task ApplyTaxAsync(List<WorkingLine> lines, PriceOrderRequestDto request)
    {
        // Honor the POS "Auto-apply tax" switch. When the tenant turns it off, no tax is added —
        // this is the controllable off-switch (the seeded tax rule otherwise always charges its rate,
        // and deactivating inventory Tax Definitions does not affect it since the rule uses a snapshot).
        var settings = await _posSettings.GetCurrentAsync();
        if (settings is not null && !settings.AutoApplyTax)
        {
            foreach (var line in lines) { line.TaxRate = 0m; line.TaxAmount = 0m; }
            return;
        }

        foreach (var line in lines)
        {
            var rate = await ResolveEffectiveTaxRateAsync(line.Request.TaxCategory, request);
            line.TaxRate   = rate;
            line.TaxAmount = line.LineAmount * rate / 100m;   // tax-exclusive: added on top of the net line
        }
    }

    private readonly Dictionary<TaxCategory, decimal> _taxRateCache = [];

    /// <summary>
    /// Resolves the combined effective tax rate (%) for a category via the tax-rule engine.
    /// Compound rates are folded into a single effective percentage (basis = 100).
    /// Cached per category — customer/channel context is constant within a request.
    /// </summary>
    private async Task<decimal> ResolveEffectiveTaxRateAsync(TaxCategory category, PriceOrderRequestDto request)
    {
        if (_taxRateCache.TryGetValue(category, out var cached)) return cached;

        decimal effective = 0m;
        try
        {
            var group = await _taxRules.ResolveAsync(
                category, request.CustomerCountryCode, request.CustomerType, request.SalesChannel);

            if (group is not null)
            {
                decimal tax = 0m;
                foreach (var r in group.Rates.OrderBy(r => r.Sequence))
                {
                    var basis = r.IsCompound ? 100m + tax : 100m;
                    tax += basis * r.SnapshotRate / 100m;
                }
                effective = tax;   // base is 100, so the accumulated tax IS the percentage
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Tax resolution failed for category {Category} — taxed at 0%", category);
        }

        _taxRateCache[category] = effective;
        return effective;
    }

    // ── Cross-module item lookup ───────────────────────────────────────────────────

    private readonly Dictionary<Guid, ItemPriceData> _itemCache = [];

    private async Task<ItemPriceData> LookupItemAsync(Guid productId)
    {
        if (productId == Guid.Empty) return ItemPriceData.Empty;
        if (_itemCache.TryGetValue(productId, out var cached)) return cached;

        try
        {
            var lookup = new ItemPriceLookupEvent { ProductId = productId };
            await _events.PublishAsync(lookup);
            var data = await lookup.Result.Task;
            _itemCache[productId] = data;
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Item price lookup failed for product {ProductId}", productId);
            return ItemPriceData.Empty;
        }
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);

    // ── Working line (mutable during pricing) ──────────────────────────────────────

    private sealed class WorkingLine
    {
        public PriceOrderLineRequestDto Request { get; init; } = null!;
        public Guid? CategoryId { get; init; }
        public string? ProductCode { get; init; }
        public string? ProductName { get; init; }
        public decimal ListUnitPrice { get; init; }
        public decimal NetUnitPrice { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TaxRate { get; set; }
        public decimal TaxAmount { get; set; }
        public bool Locked { get; set; }
        public List<AppliedPromotionDto> AppliedPromotions { get; } = [];

        public decimal LineAmount => (ListUnitPrice * Request.Quantity) - DiscountAmount;

        public void RecalcNet() =>
            NetUnitPrice = Request.Quantity > 0
                ? ListUnitPrice - (DiscountAmount / Request.Quantity)
                : ListUnitPrice;

        public PricedLineDto ToDto() => new()
        {
            ProductId       = Request.ProductId,
            ProductCode     = ProductCode,
            ProductName     = ProductName,
            VariantId       = Request.VariantId,
            CategoryId      = CategoryId,
            Quantity        = Request.Quantity,
            UnitOfMeasure   = Request.UnitOfMeasure,
            ListUnitPrice   = Round(ListUnitPrice),
            DiscountPerUnit = Request.Quantity > 0 ? Round(DiscountAmount / Request.Quantity) : 0m,
            DiscountAmount  = Round(DiscountAmount),
            NetUnitPrice    = Round(NetUnitPrice),
            LineAmount      = Round(LineAmount),
            TaxCategory     = Request.TaxCategory,
            TaxRate         = Round(TaxRate),
            TaxAmount       = Round(TaxAmount),
            AppliedPromotions = AppliedPromotions,
        };
    }
}
