using Nexcore.SharedKernel;

namespace Sales.Domain.Entities;

/// <summary>
/// Approval workflow record for a quotation.
/// Supports multi-level approval (e.g., discount > 20% requires manager approval).
/// </summary>
public class QuotationApproval : BaseEntity
{
    public Guid QuotationId { get; set; }
    public Quotation Quotation { get; set; } = null!;

    public int ApprovalLevel { get; set; }
    public Guid ApproverId { get; set; }   // FK to Users

    /// <summary>Pending, Approved, Rejected</summary>
    public string Status { get; set; } = "Pending";

    public string? Comments { get; set; }
    public DateTime? DecisionDate { get; set; }
}
