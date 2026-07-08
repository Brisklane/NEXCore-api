namespace Sales.Domain.Enums;

public enum PromotionStatus
{
    Draft = 0,
    Active = 1,
    Paused = 2,
    Expired = 3,
    Cancelled = 4,
}

/// <summary>How the discount is calculated on matching items.</summary>
public enum PromotionDiscountType
{
    PercentageOff = 0,   // e.g. 10% off
    FixedAmountOff = 1,  // e.g. Rs 50 off
    NewPrice = 2,        // override item price to a fixed value
    BuyXGetYFree = 3,    // buy X units, get Y units free (same or different item)
    FreeItem = 4,        // add a specified free item unconditionally
}

/// <summary>Which base price the discount is applied against.</summary>
public enum PromotionPriceTarget
{
    AnyPrice = 0,      // applies regardless of current price
    RegularPrice = 1,  // only applies when item is at regular (non-sale) price
}

/// <summary>Condition that must be met for this promotion item to trigger.</summary>
public enum PromotionConditionType
{
    None = 0,
    MinQuantity = 1,     // basket must contain >= X units of this item
    MinOrderAmount = 2,  // basket subtotal >= X
    ExactQuantity = 3,   // exactly X units of this item
}

/// <summary>Which customers are eligible for the promotion.</summary>
public enum PromotionTargetType
{
    AllCustomers = 0,
    LoyaltyTier = 1,     // requires a minimum loyalty tier
    PriceList = 2,       // only when a specific price list is active for the customer
    SpecificContact = 3, // personalised one-contact offer
}

/// <summary>
/// Bitmask of days of the week on which the promotion runs.
/// Store as int; use bitwise operations to check active days.
/// </summary>
[Flags]
public enum ScheduledDays
{
    None      = 0,
    Monday    = 1 << 0,   // 1
    Tuesday   = 1 << 1,   // 2
    Wednesday = 1 << 2,   // 4
    Thursday  = 1 << 3,   // 8
    Friday    = 1 << 4,   // 16
    Saturday  = 1 << 5,   // 32
    Sunday    = 1 << 6,   // 64
    Weekdays  = Monday | Tuesday | Wednesday | Thursday | Friday,
    Weekend   = Saturday | Sunday,
    EveryDay  = Weekdays | Weekend,
}
