namespace Sales.Domain.Enums;

/// <summary>
/// Sales channel through which the order was placed.
/// </summary>
public enum SalesChannel
{
    DirectSales  = 0,
    OnlineStore  = 1,
    Distributor  = 2,
    Retailer     = 3,
    B2BPortal    = 4,
    Telesales    = 5,
    FieldSales   = 6,
    Marketplace  = 7,

    /// <summary>Walk-in customer served at a physical POS counter.</summary>
    PosWalkIn    = 8,

    /// <summary>Online order placed via the customer app — fulfilled from a store.</summary>
    OnlineApp    = 9,

    /// <summary>Phone order taken by a cashier and keyed into POS.</summary>
    PhoneOrder   = 10
}
