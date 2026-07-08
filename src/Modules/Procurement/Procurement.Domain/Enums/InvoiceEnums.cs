namespace Procurement.Domain.Enums;

/// <summary>
/// Lifecycle status of a vendor bill (AP invoice).
/// Aligned with SAP MIRO, Oracle AP Invoice, Odoo vendor.bill.
/// </summary>
public enum PurchaseInvoiceStatus
{
    Draft,
    UnderApproval,
    Posted,
    OnHold,
    PartiallyPaid,
    FullyPaid,
    Cancelled,
    Disputed
}

/// <summary>
/// Three-way matching status: PO vs Goods Receipt vs Invoice.
/// Core AP control in SAP/Oracle/Dynamics.
/// </summary>
public enum InvoiceMatchingStatus
{
    NotMatched,
    PartiallyMatched,
    FullyMatched,
    MatchException
}

public enum DiscrepancyType
{
    Quantity,
    UnitPrice,
    Quality,
    Specification,
    TaxAmount,
    Other
}

public enum InvoicePaymentStatus
{
    NotPaid,
    InPayment,
    PartiallyPaid,
    FullyPaid,
    Reversed
}
