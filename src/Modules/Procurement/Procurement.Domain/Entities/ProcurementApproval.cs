using Nexcore.SharedKernel;
using Procurement.Domain.Enums;

namespace Procurement.Domain.Entities;

/// <summary>
/// Generic approval record for any procurement document (Requisition, PO, Invoice, etc.).
/// Single table covers all document types, discriminated by DocumentType.
/// </summary>
public class ProcurementApproval : BaseEntity
{
    // ─── Document Reference ────────────────────────────────────────────────────
    public ApprovalDocumentType DocumentType { get; set; }
    /// <summary>FK to the specific document (RequisitionId, PurchaseOrderId, etc.).</summary>
    public Guid DocumentId { get; set; }

    // ─── Workflow ──────────────────────────────────────────────────────────────
    public Guid? WorkflowId { get; set; }
    public ApprovalWorkflow? Workflow { get; set; }

    public Guid? WorkflowStepId { get; set; }
    public ApprovalWorkflowStep? WorkflowStep { get; set; }

    public int StepNumber { get; set; }
    public string? StepName { get; set; }

    // ─── Approver ──────────────────────────────────────────────────────────────
    public Guid ApproverId { get; set; }
    public string? ApproverName { get; set; }

    // ─── Result ────────────────────────────────────────────────────────────────
    public ProcurementApprovalStatus Status { get; set; } = ProcurementApprovalStatus.Pending;
    public ProcurementApprovalAction? Action { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }
    public DateTime? ActionDate { get; set; }

    public string? Comments { get; set; }

    // ─── Delegation ────────────────────────────────────────────────────────────
    public Guid? DelegatedToUserId { get; set; }
    public string? DelegatedToName { get; set; }
    public string? DelegationReason { get; set; }

    public bool IsEscalated { get; set; }
    public DateTime? EscalatedAt { get; set; }

    // ─── Navigation ────────────────────────────────────────────────────────────
    public PurchaseRequisition? Requisition { get; set; }
}
