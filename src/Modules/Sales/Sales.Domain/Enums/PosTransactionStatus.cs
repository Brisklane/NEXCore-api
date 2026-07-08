namespace Sales.Domain.Enums;

/// <summary>
/// Status of a POS transaction (sale, refund, void).
/// </summary>
public enum PosTransactionStatus
{
    InProgress = 0,
    Completed = 1,
    Voided = 2,
    Refunded = 3,
    PartiallyRefunded = 4,
    Held = 5          // parked / on-hold
}
