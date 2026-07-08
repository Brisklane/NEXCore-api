namespace Sales.Domain.Enums;

/// <summary>
/// Payment terms aligned with SAP payment terms (e.g., NT30, 2/10 Net30).
/// </summary>
public enum PaymentTerms
{
    Immediate = 0,
    Net15 = 1,
    Net30 = 2,
    Net45 = 3,
    Net60 = 4,
    Net90 = 5,
    TwoTenNet30 = 6,     // 2% discount if paid within 10 days, net 30
    EndOfMonth = 7,
    CashOnDelivery = 8,
    AdvancePayment = 9
}
