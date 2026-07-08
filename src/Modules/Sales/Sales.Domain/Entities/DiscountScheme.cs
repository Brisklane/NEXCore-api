using Nexcore.SharedKernel;
using Sales.Domain.Enums;

namespace Sales.Domain.Entities;

/// <summary>
/// Discount / promotion scheme.
/// Aligned with SAP condition records (VKOND), Oracle Modifiers, Dynamics Trade Agreements.
/// </summary>
public class DiscountScheme : BaseEntity
{
    public new string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DiscountType DiscountType { get; set; }

    /// <summary>Percentage value (0-100) or fixed amount depending on DiscountType.</summary>
    public decimal DiscountValue { get; set; }

    // ? Applicability
    /// <summary>Apply to a specific customer (null = all customers).</summary>
    public Guid? CustomerId { get; set; }

    /// <summary>Apply to a specific customer group (null = all groups).</summary>
    public Guid? CustomerGroupId { get; set; }

    /// <summary>Apply to a specific product (null = all products).</summary>
    public Guid? ProductId { get; set; }

    /// <summary>Apply to a specific product category (null = all categories).</summary>
    public Guid? ProductCategoryId { get; set; }

    // ? Validity
    public DateTime ValidFrom { get; set; }
    public DateTime? ValidTo { get; set; }

    /// <summary>Minimum order amount to qualify for the discount.</summary>
    public decimal? MinOrderAmount { get; set; }

    /// <summary>Minimum quantity to qualify.</summary>
    public decimal? MinQuantity { get; set; }

    public bool IsCumulative { get; set; }  // stacks with other discounts
}
