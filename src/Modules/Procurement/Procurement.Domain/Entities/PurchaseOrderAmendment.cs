using Nexcore.SharedKernel;
using Procurement.Domain.Enums;

namespace Procurement.Domain.Entities;

/// <summary>
/// Change order / amendment to a confirmed PO.
/// Tracks what changed and requires re-approval if significant.
/// Aligned with SAP PO Change (ME22N), Oracle PO Amendment.
/// </summary>
public class PurchaseOrderAmendment : BaseEntity
{
    public Guid OrderId { get; set; }
    public PurchaseOrder Order { get; set; } = null!;

    public int AmendmentNumber { get; set; }
    public DateTime AmendmentDate { get; set; } = DateTime.UtcNow;

    public Guid RequestedByUserId { get; set; }
    public string? RequestedByName { get; set; }

    public new required string Description { get; set; }
    public AmendmentStatus Status { get; set; } = AmendmentStatus.Draft;

    /// <summary>JSON snapshot of changed fields before amendment.</summary>
    public string? PreviousValues { get; set; }
    /// <summary>JSON snapshot of changed fields after amendment.</summary>
    public string? NewValues { get; set; }

    public bool RequiresReApproval { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? RejectionReason { get; set; }
    public string? Notes { get; set; }
}
