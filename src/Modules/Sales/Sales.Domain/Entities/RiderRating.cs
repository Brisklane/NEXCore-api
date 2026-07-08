using Nexcore.SharedKernel;

namespace Sales.Domain.Entities;

/// <summary>
/// Customer rating submitted for a rider after delivery.
/// </summary>
public class RiderRating : BaseEntity
{
    public Guid RiderId { get; set; }
    public Rider Rider { get; set; } = null!;

    public Guid SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = null!;

    /// <summary>Cross-module reference to Crm.Contact. ID only.</summary>
    public Guid? ContactId { get; set; }

    /// <summary>Rating 1–5 stars.</summary>
    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime RatedAt { get; set; } = DateTime.UtcNow;
}
