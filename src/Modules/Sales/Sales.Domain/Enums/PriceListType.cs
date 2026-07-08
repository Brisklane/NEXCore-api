namespace Sales.Domain.Enums;

/// <summary>
/// Type of price list / pricing condition.
/// Mirrors SAP condition types and Dynamics price list types.
/// </summary>
public enum PriceListType
{
    Standard = 0,
    Promotional = 1,
    Wholesale = 2,
    Retail = 3,
    ContractBased = 4,
    CustomerSpecific = 5
}
