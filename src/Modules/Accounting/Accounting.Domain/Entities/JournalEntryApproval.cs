using Nexcore.SharedKernel;
using Nexcore.SharedKernel.Enums;

namespace Accounting.Domain.Entities;

/// <summary>
/// Journal Entry Approval entity - tracks approval workflow
/// Records all approval steps and decisions for audit trail and compliance
/// </summary>
public class JournalEntryApproval : BaseEntity
{
    /// <summary>
    /// Journal entry being approved
    /// </summary>
    public Guid JournalEntryId { get; set; }

    /// <summary>
    /// User performing the approval
    /// </summary>
    public Guid ApprovingUserId { get; set; }

    /// <summary>
    /// Name of user performing the approval (for reference/audit)
    /// </summary>
    public string? ApprovingUserName { get; set; }

    /// <summary>
    /// When the approval was performed
    /// </summary>
    public DateTime ApprovedAt { get; set; }

    /// <summary>
    /// Approval decision: Approved, Rejected, or Escalated
    /// Uses ApprovalDecision enum instead of hardcoded strings
    /// </summary>
    public required ApprovalDecision ApprovalDecision { get; set; }

    /// <summary>
    /// Comments or notes from the approver
    /// Examples: "Approved - Verified with manager", "Rejected - Missing cost center"
    /// </summary>
    public string? Comments { get; set; }

    /// <summary>
    /// Sequence number in the approval chain
    /// 1 = First approval, 2 = Second approval, 3 = Third approval, etc.
    /// Indicates which level of approval this is
    /// </summary>
    public int ApprovalSequence { get; set; }

    /// <summary>
    /// Role/title required for this approval level
    /// Uses ApprovalRole enum instead of hardcoded strings
    /// Examples: "Manager", "Finance Manager", "CFO", "VP Operations"
    /// </summary>
    public ApprovalRole? RequiredApprovalRole { get; set; }

    /// <summary>
    /// Flag indicating if approval is final and entry can be posted
    /// </summary>
    public bool IsFinalApproval { get; set; }

    /// <summary>
    /// Reference number of related approval (if escalated or part of chain)
    /// </summary>
    public string? RelatedApprovalReference { get; set; }

    /// <summary>
    /// IP address from which approval was performed (for audit trail)
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Browser/device info from which approval was performed (for audit trail)
    /// </summary>
    public string? UserAgent { get; set; }

    // Navigation properties
    /// <summary>
    /// Navigation to the journal entry being approved
    /// </summary>
    public JournalEntry? JournalEntry { get; set; }
}
