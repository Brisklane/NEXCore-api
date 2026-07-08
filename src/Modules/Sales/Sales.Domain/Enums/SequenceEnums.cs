namespace Sales.Domain.Enums;

/// <summary>
/// All document types that have a configurable numbering sequence.
/// Stored as its string name in the database (HasConversion&lt;string&gt;).
/// </summary>
public enum DocumentType
{
    Quotation,
    SalesOrder,
    Invoice,
    CreditNote,
    Payment,
    Delivery,
    SalesReturn,
    PosSession,
    PosTransaction,
    RiderAssignment,
}

/// <summary>
/// When the running counter resets back to 1.
/// </summary>
public enum SequenceResetPeriod
{
    /// <summary>Counter never resets — numbers keep climbing forever.</summary>
    Never = 0,

    /// <summary>Counter resets to 1 on the first document of each new calendar year.</summary>
    Yearly = 1,

    /// <summary>Counter resets to 1 on the first document of each new calendar month.</summary>
    Monthly = 2,

    /// <summary>Counter resets to 1 on the first document of each new calendar day.</summary>
    Daily = 3,
}

/// <summary>
/// How the year portion is formatted in the generated number.
/// </summary>
public enum SequenceYearFormat
{
    /// <summary>4-digit year: 2026</summary>
    Full = 0,

    /// <summary>2-digit year: 26</summary>
    Short = 1,
}
