using Nexcore.SharedKernel;

namespace Procurement.Domain.Entities;

/// <summary>
/// Single step in an approval workflow.
/// Supports role-based or user-specific approvers, escalation, and parallel approvals.
/// </summary>
public class ApprovalWorkflowStep : BaseEntity
{
    public Guid WorkflowId { get; set; }
    public ApprovalWorkflow Workflow { get; set; } = null!;

    public int StepNumber { get; set; }
    public required string StepName { get; set; }

    // ─── Approver Assignment ───────────────────────────────────────────────────
    /// <summary>Specific user assigned as approver. Takes priority over role.</summary>
    public Guid? ApproverUserId { get; set; }
    /// <summary>Role name for role-based routing (e.g., "PurchaseManager", "CFO").</summary>
    public string? ApproverRole { get; set; }

    // ─── Amount Condition ──────────────────────────────────────────────────────
    public decimal? AmountThreshold { get; set; }

    // ─── Parallel Approval ─────────────────────────────────────────────────────
    /// <summary>When true, multiple approvers can act simultaneously.</summary>
    public bool IsParallelStep { get; set; }
    /// <summary>Number of approvals needed if parallel step (e.g., 2 out of 3).</summary>
    public int RequiredApprovals { get; set; } = 1;

    // ─── Escalation ────────────────────────────────────────────────────────────
    public int EscalationAfterDays { get; set; } = 3;
    public Guid? EscalationUserId { get; set; }

    public bool IsOptional { get; set; }
    public string? Instructions { get; set; }
}
