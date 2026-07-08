using Sales.Domain.Enums;

namespace Sales.Application.DTOs;

/// <summary>
/// Payload pushed to the store's POS clients when a new online order arrives.
/// The POS screen uses this to play the arrival animation and update the
/// "Orders" button badge to <see cref="PendingCount"/>.
/// </summary>
public class OnlineOrderNotificationDto
{
    public Guid OrderId          { get; set; }
    public string OrderNumber    { get; set; } = string.Empty;
    public Guid StoreId          { get; set; }

    public Guid? ContactId       { get; set; }
    public string? ContactName   { get; set; }

    public int ItemCount         { get; set; }
    public decimal TotalAmount   { get; set; }
    public string CurrencyCode   { get; set; } = "USD";

    public SalesChannel SalesChannel       { get; set; }
    public FulfillmentType FulfillmentType { get; set; }
    public DateTime PlacedAt     { get; set; }

    /// <summary>Number of online orders still awaiting store acknowledgement (Placed) after this arrival.</summary>
    public int PendingCount      { get; set; }
}

/// <summary>
/// Lightweight pending-order count for a store. Returned by the REST count
/// endpoint (initial POS load / reconnect) and pushed on badge-only refreshes.
/// </summary>
public class PosPendingOrdersDto
{
    public Guid StoreId     { get; set; }

    /// <summary>Online orders awaiting store acknowledgement (Status = Placed).</summary>
    public int PendingCount { get; set; }
}
