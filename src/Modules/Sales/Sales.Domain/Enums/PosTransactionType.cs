namespace Sales.Domain.Enums;

/// <summary>
/// POS transaction type — distinguishes the nature of the transaction.
/// </summary>
public enum PosTransactionType
{
    Sale = 0,
    Refund = 1,
    Void = 2,
    Exchange = 3,       // return + new sale in one transaction
    LayAway = 4,        // deposit-based deferred sale
    LayAwayPickup = 5,  // final payment and collection of lay-away
    Quote = 6           // saved quote / proforma at POS (no stock deduction)
}
