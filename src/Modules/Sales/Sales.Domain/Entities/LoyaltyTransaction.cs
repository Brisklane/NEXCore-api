using Nexcore.SharedKernel;
using Sales.Domain.Enums;

namespace Sales.Domain.Entities;

/// <summary>
/// Individual loyalty points transaction - earn, redeem, expire, or adjust.
/// Full audit trail for the loyalty account ledger.
/// </summary>
public class LoyaltyTransaction : BaseEntity
{
    public Guid LoyaltyAccountId { get; set; }
    public LoyaltyAccount LoyaltyAccount { get; set; } = null!;

    public LoyaltyTransactionType TransactionType { get; set; }

    public decimal Points { get; set; }
    public decimal BalanceAfter { get; set; }

    /// <summary>Source order or POS transaction reference.</summary>
    public Guid? SourceOrderId { get; set; }

    /// <summary>Online, POS, B2B, Manual.</summary>
    public string? SourceChannel { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
}
