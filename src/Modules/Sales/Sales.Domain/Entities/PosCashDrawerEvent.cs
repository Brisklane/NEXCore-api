using Nexcore.SharedKernel;

namespace Sales.Domain.Entities;

/// <summary>
/// Audit log of every cash drawer open event.
/// Each open must be tied to either a transaction (payment/refund/change)
/// or a manual cash movement — never an unexplained open.
/// </summary>
public class PosCashDrawerEvent : BaseEntity
{
    public Guid PosCashDrawerId { get; set; }
    public PosCashDrawer PosCashDrawer { get; set; } = null!;

    public Guid PosSessionId { get; set; }
    public PosSession PosSession { get; set; } = null!;

    /// <summary>Cashier who triggered the drawer open.</summary>
    public Guid CashierId { get; set; }
    public PosCashier Cashier { get; set; } = null!;

    /// <summary>Transaction that caused this open (null for manual opens).</summary>
    public Guid? PosTransactionId { get; set; }
    public PosTransaction? PosTransaction { get; set; }

    /// <summary>
    /// Reason for opening: Sale, Refund, CashIn, CashOut, SafeDrop,
    /// ManualOpen (requires manager override).
    /// </summary>
    public string OpenReason { get; set; } = string.Empty;

    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
}
