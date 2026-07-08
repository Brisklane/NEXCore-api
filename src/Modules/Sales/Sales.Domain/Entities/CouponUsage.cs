using Nexcore.SharedKernel;

namespace Sales.Domain.Entities;

/// <summary>
/// Tracks each use of a coupon to enforce per-customer and global usage limits.
/// </summary>
public class CouponUsage : BaseEntity
{
    public Guid CouponId { get; set; }
    public Coupon Coupon { get; set; } = null!;

    /// <summary>Cross-module reference to Crm.Contact. Null for anonymous POS sales. ID only.</summary>
    public Guid? ContactId { get; set; }

    /// <summary>SalesOrder this coupon was applied to (covers all channels).</summary>
    public Guid? SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }

    public Guid? PosTransactionId { get; set; }
    public PosTransaction? PosTransaction { get; set; }

    public decimal DiscountApplied { get; set; }
    public DateTime UsedAt { get; set; } = DateTime.UtcNow;
}
