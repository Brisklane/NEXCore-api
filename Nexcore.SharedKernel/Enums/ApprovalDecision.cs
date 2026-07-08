namespace Nexcore.SharedKernel.Enums;

/// <summary>Outcome of an approval step, recorded on the approval workflow and audit trail.</summary>
public enum ApprovalDecision
{
    Approved = 1,
    Rejected = 2,
    Escalated = 3,             // routed to a higher authority
    OnHold = 4,                // paused for investigation/clarification
    ConditionallyApproved = 5, // approved subject to conditions/exceptions
    Pending = 6                // awaiting a decision
}
