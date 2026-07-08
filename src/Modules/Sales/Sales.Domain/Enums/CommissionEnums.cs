namespace Sales.Domain.Enums;

/// <summary>
/// How commission is calculated on a sale.
/// </summary>
public enum CommissionBasis
{
    /// <summary>Percentage of the order/line net amount.</summary>
    PercentageOfNet = 0,

    /// <summary>Percentage of the gross (pre-discount) amount.</summary>
    PercentageOfGross = 1,

    /// <summary>Fixed amount per order.</summary>
    FixedPerOrder = 2,

    /// <summary>Fixed amount per unit sold.</summary>
    FixedPerUnit = 3,

    /// <summary>Percentage of gross profit (revenue minus COGS).</summary>
    PercentageOfMargin = 4
}

/// <summary>
/// Lifecycle status of a commission entry.
/// </summary>
public enum CommissionStatus
{
    Pending = 0,
    Approved = 1,
    Paid = 2,
    Reversed = 3,
    OnHold = 4
}

/// <summary>
/// The period over which a sales target is measured.
/// </summary>
public enum TargetPeriod
{
    Monthly = 0,
    Quarterly = 1,
    HalfYearly = 2,
    Yearly = 3
}
