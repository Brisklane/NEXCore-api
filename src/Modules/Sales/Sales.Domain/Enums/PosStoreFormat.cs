namespace Sales.Domain.Enums;

/// <summary>
/// Operational format / channel of the POS store — how customers interact with it.
/// Orthogonal to <see cref="PosStoreType"/> (a Restaurant can be Physical or DriveThrough).
/// </summary>
public enum PosStoreFormat
{
    Physical = 0,        // walk-in counter or floor
    Virtual = 1,         // no physical presence — phone/online orders fulfilled by staff
    DriveThrough = 2,
    Kiosk = 3,           // self-service kiosk
    ClickAndCollect = 4, // customer orders online, picks up in-store
    Warehouse = 5,       // cash-and-carry from warehouse
}
