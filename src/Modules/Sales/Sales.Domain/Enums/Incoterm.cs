namespace Sales.Domain.Enums;

/// <summary>
/// Incoterms (International Commercial Terms) for shipment responsibility.
/// Standard in SAP SD, Oracle OM, and Dynamics 365 SCM.
/// </summary>
public enum Incoterm
{
    EXW = 0,   // Ex Works
    FCA = 1,   // Free Carrier
    CPT = 2,   // Carriage Paid To
    CIP = 3,   // Carriage and Insurance Paid To
    DAP = 4,   // Delivered At Place
    DPU = 5,   // Delivered at Place Unloaded
    DDP = 6,   // Delivered Duty Paid
    FAS = 7,   // Free Alongside Ship
    FOB = 8,   // Free On Board
    CFR = 9,   // Cost and Freight
    CIF = 10   // Cost, Insurance and Freight
}
