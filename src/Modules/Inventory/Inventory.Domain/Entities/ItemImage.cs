using Nexcore.SharedKernel;

namespace Inventory.Domain.Entities;

/// <summary>
/// Item Image
/// Stores one image per row at a specific resolution/size.
/// An item can have many images (e.g., Thumbnail, Small, Medium, Large, Original)
/// and multiple images per resolution (gallery).
/// </summary>
public class ItemImage : BaseEntity
{
    /// <summary>
    /// Item this image belongs to
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Image URL or storage path
    /// </summary>
    public string Url { get; set; } = null!;

    /// <summary>
    /// Resolution / size label: Thumbnail | Small | Medium | Large | Original
    /// </summary>
    public string Resolution { get; set; } = "Original";

    /// <summary>
    /// Width in pixels
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// Height in pixels
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// File size in bytes
    /// </summary>
    public long? FileSizeBytes { get; set; }

    /// <summary>
    /// MIME type (e.g., image/jpeg, image/webp)
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Alt text for accessibility / SEO
    /// </summary>
    public string? AltText { get; set; }

    /// <summary>
    /// Display order within the item gallery
    /// </summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Whether this is the primary/cover image for the item
    /// </summary>
    public bool IsPrimary { get; set; }

    // Navigation properties
    public Item? Item { get; set; }
}
