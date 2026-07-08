using Nexcore.SharedKernel;

namespace Procurement.Domain.Entities;

/// <summary>
/// Hierarchical vendor classification (e.g., Raw Materials > Metals > Steel).
/// </summary>
public class VendorCategory : BaseEntity
{
    public required string Name { get; set; }
    public new required string Code { get; set; }

    public Guid? ParentCategoryId { get; set; }
    public VendorCategory? ParentCategory { get; set; }
    public ICollection<VendorCategory> SubCategories { get; set; } = [];

    public int SortOrder { get; set; }

    public ICollection<Vendor> Vendors { get; set; } = [];
}
