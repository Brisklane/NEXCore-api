namespace Sales.Domain.Enums;

/// <summary>
/// Type of POS payment tender.
/// </summary>
public enum PosTenderType
{
    Cash = 0,
    CreditCard = 1,
    DebitCard = 2,
    MobileWallet = 3,
    QrCode = 4,
    GiftCard = 5,
    LoyaltyPoints = 6,
    StoreCredit = 7,
    SplitPayment = 8
}
