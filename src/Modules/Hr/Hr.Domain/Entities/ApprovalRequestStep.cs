
using Nexcore.SharedKernel;

namespace Hr.Domain.Entities;

public class ApprovalRequestStep : BaseEntity
{
    public Guid ApprovalRequestId { get; set; }
    public Guid WorkflowConfigStepId { get; set; }
    public int StepLevel { get; set; }
    public string ApproverType { get; set; } = string.Empty;
    public string? ApproverValue { get; set; }
    public Guid? ApproverEmployeeId { get; set; }
    public bool Mandatory { get; set; } = true;
    public int SLAHours { get; set; }
    public string ExecutionType { get; set; } = string.Empty;
    public Guid StatusLookupValueId { get; set; }
    public string? Comments { get; set; }
    public string? RejectionReason { get; set; }
    public string? RejectionCategory { get; set; }
    public DateTime? ActionDate { get; set; }
    public Guid? DelegatedToEmployeeId { get; set; }
    public bool IsConditional { get; set; }
    public string? ConditionNotes { get; set; }
    public bool EscalatedFlag { get; set; }
    public Guid? EscalatedToEmployeeId { get; set; }

    public ApprovalRequest? ApprovalRequest { get; set; }
    public WorkflowConfigStep? WorkflowConfigStep { get; set; }
    public Employee? ApproverEmployee { get; set; }
    public Employee? DelegatedToEmployee { get; set; }
    public Employee? EscalatedToEmployee { get; set; }
    public LookupValue? StatusLookupValue { get; set; }
}
