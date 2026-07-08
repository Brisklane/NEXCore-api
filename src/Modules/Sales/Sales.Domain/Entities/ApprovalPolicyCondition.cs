using Nexcore.SharedKernel;
using Sales.Domain.Enums;

namespace Sales.Domain.Entities;

/// <summary>
/// A single condition within an ApprovalPolicy.
/// ALL conditions on a policy must be true for the policy to trigger (AND logic).
/// </summary>
public class ApprovalPolicyCondition : BaseEntity
{
    public Guid ApprovalPolicyId { get; set; }
    public ApprovalPolicy ApprovalPolicy { get; set; } = null!;

    public ApprovalConditionField Field { get; set; }
    public ApprovalConditionOperator Operator { get; set; }

    /// <summary>
    /// Threshold value as string — interpreted based on Field type.
    /// Numeric fields: "10000.00". Enum fields: enum integer as string, e.g., "1".
    /// Boolean fields: "true" / "false".
    /// </summary>
    public string Value { get; set; } = string.Empty;
}
