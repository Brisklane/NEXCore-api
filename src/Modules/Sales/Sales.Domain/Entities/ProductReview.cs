using Nexcore.SharedKernel;

namespace Sales.Domain.Entities;

/// <summary>
/// Customer product review submitted through the OrderDyaa app.
/// </summary>
public class ProductReview : BaseEntity
{
    /// <summary>Cross-module reference to Crm.Contact. ID only.</summary>
    public Guid ContactId { get; set; }

    /// <summary>FK to Inventory Item / Product catalog.</summary>
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;

    /// <summary>SalesOrder this review is linked to (covers all channels).</summary>
    public Guid? SalesOrderId { get; set; }
    public SalesOrder? SalesOrder { get; set; }

    /// <summary>Overall rating 1–5.</summary>
    public int Rating { get; set; }

    public string? Title { get; set; }
    public string? Body { get; set; }

    public bool IsVerifiedPurchase { get; set; }
    public bool IsApproved { get; set; }

    public DateTime ReviewDate { get; set; } = DateTime.UtcNow;

    public ICollection<ProductReviewImage> Images { get; set; } = new List<ProductReviewImage>();
}
