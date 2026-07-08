namespace Sales.Domain.Enums;

/// <summary>
/// Visual / marketing category of a store offer card shown on the customer app.
/// </summary>
public enum StoreOfferType
{
    Featured = 0,    // general highlight / hero card
    FlashDeal = 1,   // time-limited offer (shows countdown)
    Combo = 2,       // bundle deal (e.g. Meal Deal)
    NewArrival = 3,  // newly added item or category
    Seasonal = 4,    // tied to a season or occasion
    HappyHour = 5,   // time-window deal
    Sponsored = 6,   // paid placement / advertisement
    Other = 99,
}
