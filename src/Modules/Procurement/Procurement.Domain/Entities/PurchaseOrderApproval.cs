using Nexcore.SharedKernel;
using Procurement.Domain.Enums;

namespace Procurement.Domain.Entities;

/// <summary>
/// Individual approval step record for a Purchase Order.
/// Supports sequential and parallel approval workflows.
/// </summary>
public class PurchaseOrderApproval : BaseEntity
{
    public Guid OrderId { get; set; }
    public PurchaseOrder Order { get; set; } = null!;

    public int StepNumber { get; set; }
    public string? StepName { get; set; }

    public Guid ApproverId { get; set; }
    public string? ApproverName { get; set; }

    public ProcurementApprovalStatus Status { get; set; } = ProcurementApprovalStatus.Pending;
    public ProcurementApprovalAction? Action { get; set; }

    public DateTime? ActionDate { get; set; }
    public string? Comments { get; set; }

    /// <summary>If delegated, the user it was delegated to.</summary>
    public Guid? DelegatedToUserId { get; set; }

    public DateTime? DueDate { get; set; }
    public bool IsEscalated { get; set; }
}
