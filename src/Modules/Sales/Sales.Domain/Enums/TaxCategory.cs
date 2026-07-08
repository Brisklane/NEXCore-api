namespace Sales.Domain.Enums;

/// <summary>
/// Tax category applied to a product or line.
/// </summary>
public enum TaxCategory
{
    Standard = 0,
    Reduced = 1,
    ZeroRated = 2,
    Exempt = 3,
    ReverseCharge = 4
}
