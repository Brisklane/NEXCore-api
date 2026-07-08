namespace Sales.Domain.Enums;

/// <summary>
/// Delivery / shipment status.
/// </summary>
public enum DeliveryStatus
{
    Draft = 0,
    Scheduled = 1,
    PickingInProgress = 2,
    Picked = 3,
    Packed = 4,
    Shipped = 5,
    Delivered = 6,
    Failed = 7,
    Returned = 8
}
