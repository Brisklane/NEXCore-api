using Nexcore.SharedKernel;

namespace Inventory.Domain.Entities;

/// <summary>
/// Brand
/// Manufacturer or brand associated with inventory items (e.g., Samsung, Nike, Bosch)
/// </summary>
public class Brand : BaseEntity
{
    /// <summary>
    /// Brand code (unique identifier)
    /// </summary>
    public new string Code { get; set; } = null!;

    /// <summary>
    /// Brand name
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Brand logo URL or storage path
    /// </summary>
    public string? LogoUrl { get; set; }

    /// <summary>
    /// Brand website
    /// </summary>
    public string? Website { get; set; }

    // Navigation properties
    public ICollection<Item> Items { get; set; } = new List<Item>();
}
