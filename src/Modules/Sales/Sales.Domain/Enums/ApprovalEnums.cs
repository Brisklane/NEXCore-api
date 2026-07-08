namespace Sales.Domain.Enums;

/// <summary>
/// What the approval policy condition is evaluated against.
/// </summary>
public enum ApprovalConditionField
{
    OrderTotalAmount = 0,
    DiscountPercentage = 1,
    CustomerType = 2,
    SalesChannel = 3,
    CreditCheckFailed = 4,
    PaymentTerms = 5
}

/// <summary>
/// Comparison operator for an approval policy condition.
/// </summary>
public enum ApprovalConditionOperator
{
    GreaterThan = 0,
    GreaterThanOrEqual = 1,
    LessThan = 2,
    LessThanOrEqual = 3,
    Equals = 4,
    NotEquals = 5
}

/// <summary>
/// What happens when an approval step is actioned.
/// </summary>
public enum ApprovalDecision
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Delegated = 3
}
