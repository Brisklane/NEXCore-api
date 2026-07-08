namespace Procurement.Domain.Enums;

public enum RFQStatus
{
    Draft,
    Sent,
    PartiallyReceived,
    FullyReceived,
    Awarded,
    Cancelled,
    Closed
}

public enum RFQVendorStatus
{
    Invited,
    Acknowledged,
    Responded,
    Declined,
    NoResponse
}

public enum QuotationStatus
{
    Draft,
    Submitted,
    UnderEvaluation,
    Shortlisted,
    Accepted,
    Rejected,
    Expired
}
