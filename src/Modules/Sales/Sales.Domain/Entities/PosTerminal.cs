using Nexcore.SharedKernel;

namespace Sales.Domain.Entities;

/// <summary>
/// A POS terminal (cash register / tablet) within a store.
/// Inherits <c>BranchId</c> from <see cref="BaseEntity"/> - always scoped to a branch.
/// </summary>
public class PosTerminal : BaseEntity
{
    public string TerminalCode { get; set; } = string.Empty;
    public string TerminalName { get; set; } = string.Empty;

    public Guid PosStoreId { get; set; }
    public PosStore PosStore { get; set; } = null!;

    // ? Device
    public string? DeviceIdentifier { get; set; }   // hardware ID / MAC
    public string? IpAddress { get; set; }

    // ? Cash Drawer
    /// <summary>
    /// Cash drawer assigned to this terminal.
    /// One terminal can only have one active drawer at a time.
    /// </summary>
    public Guid? CashDrawerId { get; set; }
    public PosCashDrawer? CashDrawer { get; set; }

    // ? Receipt
    /// <summary>
    /// Terminal-level receipt template override.
    /// Falls back to PosStore.ReceiptTemplate when null.
    /// </summary>
    public Guid? ReceiptTemplateId { get; set; }
    public PosReceiptTemplate? ReceiptTemplate { get; set; }

    // ? State
    public bool IsOnline { get; set; }

    /// <summary>FK to the currently open PosSession (null when idle).</summary>
    public Guid? CurrentSessionId { get; set; }

    public ICollection<PosSession> Sessions { get; set; } = new List<PosSession>();
}
