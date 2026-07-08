using Nexcore.SharedKernel;

namespace Accounting.Domain.Entities;

/// <summary>
/// Journal audit - audit trail for journal entry changes
/// </summary>
public class JournalAudit : BaseEntity
{
    /// <summary>
    /// Journal entry reference
    /// </summary>
    public Guid JournalEntryId { get; set; }

    /// <summary>
    /// Action performed (Created, Posted, Reversed, etc.)
    /// </summary>
    public required string Action { get; set; }

    /// <summary>
    /// User who performed the action
    /// </summary>
    public Guid PerformedByUserId { get; set; }

    /// <summary>
    /// When the action was performed
    /// </summary>
    public DateTime PerformedAt { get; set; }

    /// <summary>
    /// JSON representation of old values
    /// </summary>
    public string? OldValueJson { get; set; }

    /// <summary>
    /// JSON representation of new values
    /// </summary>
    public string? NewValueJson { get; set; }

    /// <summary>
    /// Comments or notes about the action
    /// </summary>
    public string? Comments { get; set; }

    // Navigation properties
    public JournalEntry? JournalEntry { get; set; }
}
