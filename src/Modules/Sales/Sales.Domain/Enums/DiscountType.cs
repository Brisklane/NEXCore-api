namespace Sales.Domain.Enums;

/// <summary>
/// Type of sales discount applied to a line or order.
/// </summary>
public enum DiscountType
{
    Percentage = 0,
    FixedAmount = 1,
    BuyXGetY = 2,
    VolumeDiscount = 3,
    TradeDiscount = 4,
    LoyaltyDiscount = 5
}
