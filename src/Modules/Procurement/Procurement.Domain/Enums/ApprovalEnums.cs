namespace Procurement.Domain.Enums;

public enum ProcurementApprovalStatus
{
    Pending,
    Approved,
    Rejected,
    Delegated,
    Recalled,
    Escalated
}

public enum ProcurementApprovalAction
{
    Submitted,
    Approved,
    Rejected,
    Delegated,
    Recalled,
    Escalated,
    Commented
}

public enum ApprovalDocumentType
{
    PurchaseRequisition,
    PurchaseOrder,
    VendorRegistration,
    PurchaseInvoice,
    PurchaseContract,
    VendorPayment
}
