using Nexcore.SharedKernel;
using Procurement.Domain.Enums;

namespace Procurement.Domain.Entities;

/// <summary>
/// Configurable approval workflow definition for procurement documents.
/// Supports amount-based routing, sequential steps, and role-based approvers.
/// Aligned with SAP Release Strategy, Oracle Approval Management Engine (AME),
/// Dynamics Workflow, Odoo multi-level approval.
/// </summary>
public class ApprovalWorkflow : BaseEntity
{
    public required string Name { get; set; }

    public ApprovalDocumentType DocumentType { get; set; }

    /// <summary>Minimum amount that triggers this workflow. Null = applies to all amounts.</summary>
    public decimal? MinimumAmount { get; set; }
    /// <summary>Maximum amount handled by this workflow. Null = no upper limit.</summary>
    public decimal? MaximumAmount { get; set; }

    public bool IsDefault { get; set; }
    public int Priority { get; set; }

    // ─── Navigation ────────────────────────────────────────────────────────────
    public ICollection<ApprovalWorkflowStep> Steps { get; set; } = [];
}
