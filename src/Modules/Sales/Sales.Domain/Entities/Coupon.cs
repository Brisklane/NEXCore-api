using Nexcore.SharedKernel;
using Sales.Domain.Enums;

namespace Sales.Domain.Entities;

/// <summary>
/// Coupon / promo code that can be applied to online orders or POS transactions.
/// Supports percentage, fixed, free-delivery, and buy-X-get-Y types.
/// </summary>
public class Coupon : BaseEntity
{
    public new string Code { get; set; } = string.Empty;
    public string? Name { get; set; }
    public CouponStatus Status { get; set; } = CouponStatus.Active;
    public DiscountType DiscountType { get; set; }

    /// <summary>Discount value - percentage (0-100) or fixed amount depending on DiscountType.</summary>
    public decimal DiscountValue { get; set; }

    /// <summary>Cap the maximum discount amount (for percentage coupons).</summary>
    public decimal? MaxDiscountAmount { get; set; }

    // ? Validity
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }

    /// <summary>Maximum total times this coupon can be used across all customers.</summary>
    public int? MaxUsageCount { get; set; }
    public int UsageCount { get; set; }

    /// <summary>Maximum times a single customer can use this coupon.</summary>
    public int MaxUsagePerCustomer { get; set; } = 1;

    // ? Conditions
    public decimal? MinOrderAmount { get; set; }

    /// <summary>Null = valid for all channels. Otherwise restrict to Online/POS/B2B.</summary>
    public string? ApplicableChannel { get; set; }

    /// <summary>Specific customer ID this coupon is issued to. Null = public coupon.</summary>
    public Guid? TargetCustomerId { get; set; }

    /// <summary>Specific product or category restriction. Null = all products.</summary>
    public Guid? RestrictedToProductId { get; set; }
    public Guid? RestrictedToCategoryId { get; set; }

    public bool IsFreeDelivery { get; set; }
    public bool IsFirstOrderOnly { get; set; }

    public ICollection<CouponUsage> Usages { get; set; } = new List<CouponUsage>();
}
