using Sales.Domain.Enums;

namespace Sales.Application.DTOs;

// ── Request ─────────────────────────────────────────────────────────────────────

/// <summary>
/// Request to price a basket. Used both as a live preview (POS / order screen, no persistence)
/// and internally when a Sales Order is created, so prices and promotions are resolved by the
/// server identically for every channel.
/// </summary>
public class PriceOrderRequestDto
{
    public Guid? ContactId { get; set; }

    /// <summary>Explicit price list. When null, the store's default price list is used (if any).</summary>
    public Guid? PriceListId { get; set; }

    /// <summary>POS store — used to resolve the default price list when one isn't supplied.</summary>
    public Guid? PosStoreId { get; set; }

    public string? CouponCode { get; set; }

    /// <summary>Optional code to activate a code-gated promotion (non-auto-applied).</summary>
    public string? PromotionCode { get; set; }

    public SalesChannel SalesChannel { get; set; } = SalesChannel.DirectSales;

    /// <summary>Ship-to / bill-to country (ISO alpha-2) — used to resolve the tax rule. Null = any.</summary>
    public string? CustomerCountryCode { get; set; }

    /// <summary>Customer type for tax-rule matching ("B2B", "B2C", "WalkIn"…). Null = any.</summary>
    public string? CustomerType { get; set; }

    public List<PriceOrderLineRequestDto> Lines { get; set; } = [];
}

public class PriceOrderLineRequestDto
{
    public Guid ProductId { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public Guid? VariantId { get; set; }
    public decimal Quantity { get; set; }
    public string? UnitOfMeasure { get; set; }

    /// <summary>Product tax classification — drives tax-rule resolution. Defaults to Standard.</summary>
    public TaxCategory TaxCategory { get; set; } = TaxCategory.Standard;
}

// ── Result ──────────────────────────────────────────────────────────────────────

/// <summary>Server-priced basket: resolved prices, applied promotions, coupon, and totals.</summary>
public class PricedOrderDto
{
    public List<PricedLineDto> Lines { get; set; } = [];

    /// <summary>Sum of gross line amounts before any discount (list price × qty).</summary>
    public decimal GrossAmount { get; set; }

    /// <summary>Total of per-line promotion discounts.</summary>
    public decimal LineDiscountAmount { get; set; }

    /// <summary>Order-level coupon discount.</summary>
    public decimal CouponDiscountAmount { get; set; }

    /// <summary>All discounts combined (line + coupon).</summary>
    public decimal DiscountAmount { get; set; }

    /// <summary>Net of discounts, before tax.</summary>
    public decimal SubtotalAmount { get; set; }

    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public string? CouponCode { get; set; }

    /// <summary>Non-fatal notes (e.g. price fell back to client value, coupon ignored).</summary>
    public List<string> Warnings { get; set; } = [];
}

public class PricedLineDto
{
    public Guid ProductId { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public Guid? VariantId { get; set; }
    public Guid? CategoryId { get; set; }

    public decimal Quantity { get; set; }
    public string? UnitOfMeasure { get; set; }

    /// <summary>Server-resolved list price per unit (price list → base price → fallback).</summary>
    public decimal ListUnitPrice { get; set; }

    public decimal DiscountPerUnit { get; set; }
    /// <summary>Total promotion discount on this line (DiscountPerUnit × discounted qty).</summary>
    public decimal DiscountAmount { get; set; }

    public decimal NetUnitPrice { get; set; }
    /// <summary>Net line amount after line discounts (NetUnitPrice × Quantity).</summary>
    public decimal LineAmount { get; set; }

    // ── Tax ──
    public TaxCategory TaxCategory { get; set; }
    /// <summary>Effective combined tax rate (%) resolved for this line.</summary>
    public decimal TaxRate { get; set; }
    /// <summary>Tax on the net line amount.</summary>
    public decimal TaxAmount { get; set; }

    public List<AppliedPromotionDto> AppliedPromotions { get; set; } = [];
}

public class AppliedPromotionDto
{
    public Guid PromotionId { get; set; }
    public string PromotionName { get; set; } = string.Empty;
    public decimal DiscountAmount { get; set; }
}
