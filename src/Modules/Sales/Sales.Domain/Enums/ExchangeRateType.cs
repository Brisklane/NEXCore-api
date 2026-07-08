namespace Sales.Domain.Enums;

/// <summary>
/// Identifies which rate to use when multiple rates exist for the same currency pair on the same date.
/// Mirrors common banking practice: a bank publishes buying, selling, and official rates daily.
/// </summary>
public enum ExchangeRateType
{
    /// <summary>Central / State Bank official rate — used for regulatory reporting.</summary>
    Official = 0,

    /// <summary>Bank's rate when buying foreign currency from a customer (customer sells).</summary>
    Buying = 1,

    /// <summary>Bank's rate when selling foreign currency to a customer (customer buys).</summary>
    Selling = 2,

    /// <summary>Manually entered rate for a specific transaction or contract.</summary>
    Custom = 3,
}
