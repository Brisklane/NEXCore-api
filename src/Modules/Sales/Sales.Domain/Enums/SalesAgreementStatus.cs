namespace Sales.Domain.Enums;

/// <summary>
/// Sales agreement / blanket order lifecycle.
/// </summary>
public enum SalesAgreementStatus
{
    Draft = 0,
    Active = 1,
    Expired = 2,
    Terminated = 3,
    Fulfilled = 4
}
