using Nexcore.SharedKernel;
using Sales.Domain.Enums;

namespace Sales.Domain.Entities;

/// <summary>
/// Loyalty program configuration - defines earn rates, redemption rules, and tier thresholds.
/// One active program per company/branch.
/// </summary>
public class LoyaltyProgram : BaseEntity
{
    public string ProgramName { get; set; } = string.Empty;
    // ? Earn Rules
    /// <summary>Points earned per 1 unit of currency spent (e.g., 1 point per $1).</summary>
    public decimal PointsPerCurrencyUnit { get; set; } = 1m;

    /// <summary>Minimum order amount to earn points.</summary>
    public decimal? MinOrderAmountToEarn { get; set; }

    // ? Redemption Rules
    /// <summary>Currency value of 1 point when redeemed (e.g., $0.01 per point).</summary>
    public decimal PointValueInCurrency { get; set; } = 0.01m;

    /// <summary>Minimum points required to redeem.</summary>
    public decimal MinPointsToRedeem { get; set; } = 100m;

    /// <summary>Maximum % of order total that can be paid with points.</summary>
    public decimal MaxRedemptionPercentage { get; set; } = 50m;

    // ? Expiry
    /// <summary>Days after earning before points expire. Null = no expiry.</summary>
    public int? PointsExpiryDays { get; set; }

    // ? Tier Thresholds
    public decimal BronzeThreshold { get; set; }
    public decimal SilverThreshold { get; set; }
    public decimal GoldThreshold { get; set; }
    public decimal PlatinumThreshold { get; set; }
    public decimal DiamondThreshold { get; set; }
}
