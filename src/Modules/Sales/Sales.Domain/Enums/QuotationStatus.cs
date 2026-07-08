namespace Sales.Domain.Enums;

/// <summary>
/// Quotation lifecycle status.
/// Maps to SAP Quotation, Dynamics Quote, Oracle Quote.
/// </summary>
public enum QuotationStatus
{
    Draft = 0,
    Sent = 1,
    Accepted = 2,
    Rejected = 3,
    Expired = 4,
    Converted = 5   // converted to Sales Order
}
