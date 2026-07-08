using Nexcore.SharedKernel;

namespace Sales.Domain.Entities;

/// <summary>
/// An ordered step in an ApprovalPolicy workflow.
///
/// Steps execute sequentially (StepOrder ascending).
/// Each step routes to a specific approver role or user.
/// Step N is only activated once Step N-1 is Approved.
/// </summary>
public class ApprovalPolicyStep : BaseEntity
{
    public Guid ApprovalPolicyId { get; set; }
    public ApprovalPolicy ApprovalPolicy { get; set; } = null!;

    /// <summary>Execution order. Lower = earlier.</summary>
    public int StepOrder { get; set; }

    public string StepName { get; set; } = string.Empty;

    /// <summary>
    /// Role name that can approve this step (e.g., "SalesManager", "CFO").
    /// Resolved to actual users at runtime via Auth/HR module.
    /// </summary>
    public string? ApproverRole { get; set; }

    /// <summary>
    /// Specific user ID override (takes precedence over ApproverRole if set).
    /// Cross-module Guid to Auth.User.
    /// </summary>
    public Guid? ApproverUserId { get; set; }

    /// <summary>
    /// Hours before this step is considered overdue and escalated.
    /// Null = no auto-escalation.
    /// </summary>
    public int? EscalationHours { get; set; }

    /// <summary>User or role to escalate to after EscalationHours.</summary>
    public Guid? EscalationUserId { get; set; }
    public string? EscalationRole { get; set; }
}
