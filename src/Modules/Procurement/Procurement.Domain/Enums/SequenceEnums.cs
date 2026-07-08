namespace Procurement.Domain.Enums;

/// <summary>
/// All procurement document types that have a configurable numbering sequence.
/// Stored as its string name in the database.
/// </summary>
public enum ProcurementDocumentType
{
    PurchaseRequisition,
    PurchaseOrder,
    RequestForQuotation,
    GoodsReceipt,
    PurchaseInvoice,
    PurchaseContract,
    Vendor,
    VendorPayment,
    PurchaseReturn,
    VendorDebitNote,
}

/// <summary>When the running counter resets back to 1.</summary>
public enum SequenceResetPeriod
{
    Never = 0,
    Yearly = 1,
    Monthly = 2,
}

/// <summary>How the year portion is formatted in the generated number.</summary>
public enum SequenceYearFormat
{
    Full = 0,   // 2026
    Short = 1,  // 26
}
