using Nexcore.SharedKernel;

namespace Inventory.Domain.Entities;

/// <summary>
/// Item Comment
/// Internal notes or comments on an item (e.g., supplier notes, handling instructions).
/// Supports an audit trail of comments per item — useful for POS and purchasing teams.
/// </summary>
public class ItemComment : BaseEntity
{
    /// <summary>
    /// Item this comment belongs to
    /// </summary>
    public Guid ItemId { get; set; }

    /// <summary>
    /// Comment type: Internal | Supplier | Customer | Quality | Handling
    /// </summary>
    public string CommentType { get; set; } = "Internal";

    /// <summary>
    /// Comment text
    /// </summary>
    public string Comment { get; set; } = null!;

    /// <summary>
    /// User ID who authored this comment (from audit trail via BaseEntity.CreatedByUserId)
    /// Kept here explicitly for fast display without join
    /// </summary>
    public Guid AuthorUserId { get; set; }

    /// <summary>
    /// Whether this comment is pinned / highlighted
    /// </summary>
    public bool IsPinned { get; set; }

    // Navigation properties
    public Item? Item { get; set; }
}
