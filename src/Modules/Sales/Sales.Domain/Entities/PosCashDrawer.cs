using Nexcore.SharedKernel;

namespace Sales.Domain.Entities;

/// <summary>
/// Physical cash drawer device connected to a POS terminal.
/// Tracks open/close events for audit and loss-prevention purposes.
/// </summary>
public class PosCashDrawer : BaseEntity
{
    public string DrawerCode { get; set; } = string.Empty;
    public string? DrawerLabel { get; set; }        // e.g., "Drawer A", "Till 1"


    // ??? Navigation ???????????????????????????????????????????????????????????
    public ICollection<PosTerminal> Terminals { get; set; } = new List<PosTerminal>();
    public ICollection<PosCashDrawerEvent> Events { get; set; } = new List<PosCashDrawerEvent>();
}
