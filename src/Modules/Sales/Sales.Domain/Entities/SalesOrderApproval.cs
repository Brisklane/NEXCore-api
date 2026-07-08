using Nexcore.SharedKernel;

namespace Sales.Domain.Entities;

/// <summary>
/// Approval record for a Sales Order (e.g., high-value orders requiring CFO sign-off).
/// </summary>
public class SalesOrderApproval : BaseEntity
{
    public Guid SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = null!;

    public int ApprovalLevel { get; set; }
    public Guid ApproverId { get; set; }

    /// <summary>Pending, Approved, Rejected</summary>
    public string Status { get; set; } = "Pending";

    public string? Comments { get; set; }
    public DateTime? DecisionDate { get; set; }
}
