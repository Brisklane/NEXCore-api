namespace Sales.Domain.Enums;

/// <summary>
/// Lifecycle state of a home-chef / marketplace vendor's onboarding application.
/// Only relevant for stores created through the self-registration flow.
/// Admin-created stores leave this as null on PosStore.
/// </summary>
public enum VendorOnboardingStatus
{
    Draft = 0,        // seller started filling the form but hasn't submitted yet
    Submitted = 1,    // seller submitted — awaiting platform review
    UnderReview = 2,  // platform team is actively reviewing the application
    Approved = 3,     // approved — store is live on the marketplace
    Rejected = 4,     // rejected — seller may reapply after corrections
    Suspended = 5,    // previously approved but suspended by the platform
}
