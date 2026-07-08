namespace Sales.Domain.Enums;

/// <summary>
/// POS transaction / session status.
/// </summary>
public enum PosSessionStatus
{
    Open = 0,
    Closed = 1,
    Reconciled = 2,
    Suspended = 3
}
