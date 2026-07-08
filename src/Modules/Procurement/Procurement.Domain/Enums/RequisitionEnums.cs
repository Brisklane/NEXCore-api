namespace Procurement.Domain.Enums;

public enum RequisitionStatus
{
    Draft,
    Submitted,
    UnderApproval,
    Approved,
    Rejected,
    Cancelled,
    PartiallyFulfilled,
    Fulfilled
}

public enum RequisitionPriority
{
    Low,
    Normal,
    High,
    Urgent
}

public enum RequisitionLineStatus
{
    Open,
    PartiallyFulfilled,
    Fulfilled,
    Cancelled
}
