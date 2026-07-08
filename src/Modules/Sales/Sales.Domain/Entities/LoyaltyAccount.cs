using Nexcore.SharedKernel;
using Sales.Domain.Enums;

namespace Sales.Domain.Entities;

/// <summary>
/// Loyalty account for a customer — tracks points balance, tier, and history.
/// One account per Customer regardless of CustomerType.
/// </summary>
public class LoyaltyAccount : BaseEntity
{
    /// <summary>Cross-module reference to Crm.Contact. ID only.</summary>
    public Guid ContactId { get; set; }

    public LoyaltyTier Tier { get; set; } = LoyaltyTier.None;

    public decimal PointsBalance { get; set; }
    public decimal LifetimePointsEarned { get; set; }
    public decimal LifetimePointsRedeemed { get; set; }

    /// <summary>Points that have expired and been removed from balance.</summary>
    public decimal LifetimePointsExpired { get; set; }

    /// <summary>Total spend amount contributing to tier calculation.</summary>
    public decimal TierSpendAmount { get; set; }

    public DateTime? TierExpiryDate { get; set; }
    public DateTime? LastActivityDate { get; set; }

    public ICollection<LoyaltyTransaction> Transactions { get; set; } = new List<LoyaltyTransaction>();
}
