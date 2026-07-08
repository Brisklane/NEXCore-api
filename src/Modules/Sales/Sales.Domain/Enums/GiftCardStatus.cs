namespace Sales.Domain.Enums;

/// <summary>
/// Status of a POS Gift Card.
/// </summary>
public enum GiftCardStatus
{
    Active = 0,
    Redeemed = 1,     // fully used
    PartiallyUsed = 2,
    Expired = 3,
    Cancelled = 4,
    Blocked = 5
}
