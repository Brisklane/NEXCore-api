namespace Sales.Domain.Enums;

/// <summary>
/// Rider / delivery agent availability and assignment status.
/// </summary>
public enum RiderStatus
{
    Offline = 0,
    Available = 1,
    Busy = 2,           // on an active delivery
    OnBreak = 3,
    Suspended = 4,
    Inactive = 5
}
