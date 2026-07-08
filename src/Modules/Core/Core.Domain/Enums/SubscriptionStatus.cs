namespace Core.Domain.Enums;

/// <summary>Lifecycle state of a tenant's subscription.</summary>
public enum SubscriptionStatus
{
    Trial = 0,
    Active = 1,
    Expired = 2,
    Cancelled = 3,
    Suspended = 4
}
