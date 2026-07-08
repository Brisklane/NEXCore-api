namespace Procurement.Domain.Enums;

public enum VendorStatus
{
    PendingApproval,
    Active,
    Inactive,
    Blocked,
    Archived
}

public enum VendorType
{
    Company,
    Individual
}

public enum VendorAddressType
{
    Billing,
    Shipping,
    Both,
    Registered
}

public enum VendorRatingCategory
{
    Quality,
    Delivery,
    Price,
    Service,
    Compliance
}

public enum VendorOnboardingStatus
{
    Draft,
    Submitted,
    UnderReview,
    Approved,
    Rejected
}
