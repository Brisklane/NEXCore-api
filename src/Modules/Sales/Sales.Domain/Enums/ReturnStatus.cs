namespace Sales.Domain.Enums;

/// <summary>
/// Customer return / RMA lifecycle.
/// </summary>
public enum ReturnStatus
{
    Requested = 0,
    Approved = 1,
    Rejected = 2,
    GoodsReceived = 3,
    Inspected = 4,
    CreditIssued = 5,
    Closed = 6
}
