using Nexcore.SharedKernel;

namespace Sales.Domain.Entities;

/// <summary>
/// Customer wishlist item (save for later).
/// </summary>
public class WishlistItem : BaseEntity
{
    /// <summary>Cross-module reference to Crm.Contact. ID only.</summary>
    public Guid ContactId { get; set; }

    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;

    public Guid? VariantId { get; set; }
    public string? VariantName { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}
