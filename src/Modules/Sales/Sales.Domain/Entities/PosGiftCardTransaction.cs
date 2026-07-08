using Nexcore.SharedKernel;

namespace Sales.Domain.Entities;

/// <summary>
/// Ledger entry for every load, redemption, or adjustment on a gift card.
/// Provides the full balance history for audit and dispute resolution.
/// </summary>
public class PosGiftCardTransaction : BaseEntity
{
    public Guid PosGiftCardId { get; set; }
    public PosGiftCard PosGiftCard { get; set; } = null!;

    /// <summary>Load, Redeem, Refund, Adjustment, Expiry.</summary>
    public string TransactionType { get; set; } = string.Empty;

    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }

    /// <summary>POS transaction that triggered this entry.</summary>
    public Guid? PosTransactionId { get; set; }
    public PosTransaction? PosTransaction { get; set; }

    public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
}
