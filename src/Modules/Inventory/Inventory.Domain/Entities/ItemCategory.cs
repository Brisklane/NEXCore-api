using Nexcore.SharedKernel;

namespace Inventory.Domain.Entities;

/// <summary>
/// Item Category
/// Hierarchical categorization of inventory items
/// </summary>
public class ItemCategory : BaseEntity
{
    /// <summary>
    /// Category code
    /// </summary>
    public new string Code { get; set; } = null!;

    /// <summary>
    /// Category name
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Parent category ID for hierarchical categorization
    /// </summary>
    public Guid? ParentCategoryId { get; set; }

    // ?? Default GL Accounts ???????????????????????????????????????????????????
    // All items in this category inherit these unless they define their own override.

    /// <summary>Default inventory asset GL account (Balance Sheet) for items in this category</summary>
    public Guid? InventoryAccountId { get; set; }

    /// <summary>Default Cost of Goods Sold GL account (Income Statement)</summary>
    public Guid? CogsAccountId { get; set; }

    /// <summary>Default purchases / accounts-payable clearing GL account</summary>
    public Guid? PurchaseAccountId { get; set; }

    /// <summary>Default sales revenue GL account</summary>
    public Guid? SalesAccountId { get; set; }

    /// <summary>
    /// Image URL shown for this category on the customer app / online channel.
    /// Separate from internal admin images.
    /// </summary>
    public string? OnlineImageUrl { get; set; }

    /// <summary>
    /// Sort order when this category is displayed on the customer app menu.
    /// Lower = shown first.
    /// </summary>
    public int OnlineDisplayOrder { get; set; }

    // Navigation properties
    public ItemCategory? ParentCategory { get; set; }
    public ICollection<ItemCategory> ChildCategories { get; set; } = new List<ItemCategory>();
    public ICollection<Item> Items { get; set; } = new List<Item>();
}
