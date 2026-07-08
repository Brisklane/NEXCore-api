namespace Sales.Domain.Enums;

/// <summary>
/// Type of loyalty transaction (earn / redeem / adjust / expire).
/// </summary>
public enum LoyaltyTransactionType
{
    Earned = 0,
    Redeemed = 1,
    Adjusted = 2,       // manual correction
    Expired = 3,
    Bonus = 4,
    Refunded = 5
}
