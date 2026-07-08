using Nexcore.SharedKernel;
using Sales.Domain.Enums;

namespace Sales.Domain.Entities;

/// <summary>
/// Audit trail of every status change on a SalesOrder.
/// Used for order tracking timeline in the customer app,
/// POS session reports, and B2B order status notifications.
/// </summary>
public class SalesOrderStatusHistory : BaseEntity
{
    public Guid SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = null!;

    public SalesOrderStatus Status { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Who triggered the change: Customer, Cashier, Rider, System, SalesRep.
    /// </summary>
    public string? ChangedBy { get; set; }
    public string? Note { get; set; }

    /// <summary>GPS of rider at the moment of delivery status change.</summary>
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
}
