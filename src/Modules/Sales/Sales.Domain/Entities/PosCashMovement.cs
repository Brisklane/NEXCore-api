using Nexcore.SharedKernel;
using Sales.Domain.Enums;

namespace Sales.Domain.Entities;

/// <summary>
/// Cash-in / cash-out movement during a POS session (e.g., petty cash, safe drops).
/// Every movement must be authorised by a cashier and logged for audit.
/// </summary>
public class PosCashMovement : BaseEntity
{
    public Guid PosSessionId { get; set; }
    public PosSession PosSession { get; set; } = null!;

    /// <summary>Cashier who performed or authorised the cash movement.</summary>
    public Guid PosCashierId { get; set; }
    public PosCashier PosCashier { get; set; } = null!;

    public PosCashMovementType MovementType { get; set; }

    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    public DateTime MovementDate { get; set; } = DateTime.UtcNow;
}
