using Nexcore.SharedKernel;
using Sales.Domain.Enums;

namespace Sales.Domain.Entities;

/// <summary>
/// Approval Policy - defines WHEN a SalesOrder requires approval.
///
/// A policy is active when all of its Conditions evaluate to true simultaneously.
/// When triggered, it creates ApprovalRequest records for each Step in sequence.
///
/// Aligned with:
///   Odoo  - sale.order approval (order confirmation setting + custom rules)
///   Dynamics 365 - Workflow Approval (Sales Order)
///   SAP   - Release Strategy (VTLA / VKM1)
/// </summary>
public class ApprovalPolicy : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// Lower number = evaluated first when multiple policies could match.
    /// Only the highest-priority matching policy is applied per order.
    /// </summary>
    public int Priority { get; set; } = 10;

    // ? Navigation
    public ICollection<ApprovalPolicyCondition> Conditions { get; set; } = new List<ApprovalPolicyCondition>();
    public ICollection<ApprovalPolicyStep> Steps { get; set; } = new List<ApprovalPolicyStep>();
    public ICollection<ApprovalRequest> Requests { get; set; } = new List<ApprovalRequest>();
}
