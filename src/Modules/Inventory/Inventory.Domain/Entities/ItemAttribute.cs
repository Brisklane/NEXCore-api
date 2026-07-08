using Nexcore.SharedKernel;

namespace Inventory.Domain.Entities;

/// <summary>
/// Attribute Definition
/// Defines a reusable attribute type (e.g., Color, Size, Weight, Material).
/// Attributes are shared across items and categories.
/// </summary>
public class AttributeDefinition : BaseEntity
{
    /// <summary>
    /// Attribute code (e.g., COLOR, SIZE, WEIGHT)
    /// </summary>
    public new string Code { get; set; } = null!;

    /// <summary>
    /// Attribute name (e.g., Color, Size, Weight)
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Data type of the attribute value: Text | Number | Boolean | Date | List
    /// </summary>
    public string DataType { get; set; } = "Text";

    /// <summary>
    /// Unit of measure for the value (e.g., kg, cm, mm) - relevant for Number type
    /// </summary>
    public string? Unit { get; set; }

    /// <summary>
    /// Comma-separated allowed values for List type (e.g., "Red,Green,Blue")
    /// </summary>
    public string? AllowedValues { get; set; }

    /// <summary>
    /// Whether this attribute is required on items
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Whether this attribute can be used as a product variant filter (e.g., Color, Size)
    /// </summary>
    public bool IsVariant { get; set; }

    /// <summary>
    /// Display order in forms / reports
    /// </summary>
    public int DisplayOrder { get; set; }

    // Navigation properties
    public ICollection<ItemAttribute> ItemAttributes { get; set; } = new List<ItemAttribute>();
}

/// <summary>
/// Item Attribute
/// Stores the value of a specific AttributeDefinition for a specific Item.
/// Example: Item "T-Shirt XL" ? Attribute "Color" = "Red", "Size" = "XL"
/// </summary>
public class ItemAttribute : BaseEntity
{
    /// <summary>
    /// Item this attribute value belongs to
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Attribute definition (what attribute this is)
    /// </summary>
    public Guid AttributeDefinitionId { get; set; }

    /// <summary>
    /// Stored value as string (convert to DataType at read time)
    /// </summary>
    public string Value { get; set; } = null!;

    // Navigation properties
    public Item? Item { get; set; }
    public AttributeDefinition? AttributeDefinition { get; set; }
}
