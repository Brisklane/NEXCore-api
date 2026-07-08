namespace Sales.Domain.Enums;

/// <summary>
/// Status of a rider dispatch assignment.
/// </summary>
public enum RiderAssignmentStatus
{
    Assigned = 0,
    Accepted = 1,
    Rejected = 2,
    PickedUp = 3,
    OutForDelivery = 4,
    Delivered = 5,
    Failed = 6,
    Reassigned = 7,
    Cancelled = 8
}
