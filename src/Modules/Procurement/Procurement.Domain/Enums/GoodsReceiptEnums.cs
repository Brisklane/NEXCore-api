namespace Procurement.Domain.Enums;

public enum GoodsReceiptStatus
{
    Draft,
    Posted,
    Cancelled
}

public enum ReceiptType
{
    Standard,
    Return,
    Transfer
}

public enum QualityInspectionStatus
{
    Pending,
    Passed,
    Failed,
    PartiallyPassed,
    Waived
}
