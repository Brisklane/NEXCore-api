namespace Procurement.Domain.Enums;

public enum VendorPaymentStatus
{
    Draft,
    Approved,
    Processing,
    Sent,
    Cleared,
    Cancelled,
    Returned
}

public enum VendorPaymentMethod
{
    Cash,
    BankTransfer,
    Check,
    CreditCard,
    DirectDebit,
    OnlinePayment,
    Letter_of_Credit
}
