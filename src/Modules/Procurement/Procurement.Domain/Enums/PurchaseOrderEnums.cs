namespace Procurement.Domain.Enums;

public enum PurchaseOrderStatus
{
    Draft,
    Confirmed,
    SentToVendor,
    Acknowledged,
    PartiallyReceived,
    FullyReceived,
    PartiallyInvoiced,
    FullyInvoiced,
    Closed,
    Cancelled
}

public enum PurchaseOrderLineStatus
{
    Open,
    PartiallyReceived,
    FullyReceived,
    Cancelled,
    Closed
}

public enum AmendmentStatus
{
    Draft,
    Submitted,
    Approved,
    Rejected,
    Applied
}

/// <summary>
/// Incoterms 2020 — defines where risk transfers from vendor to buyer.
/// Aligned with SAP/Oracle Incoterms field.
/// </summary>
public enum Incoterm
{
    EXW, // Ex Works
    FCA, // Free Carrier
    CPT, // Carriage Paid To
    CIP, // Carriage and Insurance Paid To
    DAP, // Delivered at Place
    DPU, // Delivered at Place Unloaded
    DDP, // Delivered Duty Paid
    FAS, // Free Alongside Ship
    FOB, // Free on Board
    CFR, // Cost and Freight
    CIF  // Cost, Insurance and Freight
}
