using Nexcore.SharedKernel;

namespace Sales.Domain.Entities;

/// <summary>Photo attached to a product review.</summary>
public class ProductReviewImage : BaseEntity
{
    public Guid ProductReviewId { get; set; }
    public ProductReview ProductReview { get; set; } = null!;

    public string ImageUrl { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}
