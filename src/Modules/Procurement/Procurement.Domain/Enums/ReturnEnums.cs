namespace Procurement.Domain.Enums;

public enum PurchaseReturnStatus
{
    Draft,
    Approved,
    Posted,
    Cancelled
}

public enum PurchaseReturnReason
{
    QualityDefect,
    WrongItem,
    ExcessQuantity,
    Damaged,
    ExpiredGoods,
    SpecificationMismatch,
    Other
}

public enum DebitNoteStatus
{
    Draft,
    Sent,
    Acknowledged,
    PartiallySettled,
    FullySettled,
    Cancelled
}
