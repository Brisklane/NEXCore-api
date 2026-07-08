namespace Sales.Domain.Enums;

/// <summary>
/// Type of notification sent to customer or rider.
/// </summary>
public enum NotificationType
{
    OrderConfirmation = 0,
    OrderStatusUpdate = 1,
    RiderAssigned = 2,
    OutForDelivery = 3,
    Delivered = 4,
    PaymentReceived = 5,
    PromotionalOffer = 6,
    LoyaltyPoints = 7,
    RefundProcessed = 8,
    Custom = 9
}
