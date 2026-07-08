using Nexcore.SharedKernel.Helpers;

namespace Nexcore.SharedKernel.Constants;

/// <summary>
/// Well-known, per-company deterministic IDs shared between modules at seed time.
///
/// Both modules call the same method with the same companyId and get back the
/// identical GUID (via <see cref="CrossModuleGuid.Derive"/>), so each company gets
/// its own unique IDs with no primary-key collisions across tenants.
///
/// This is the single source of truth for the POS ↔ Inventory warehouse link:
/// Sales seeds <c>PosStore.DefaultWarehouseId</c> (and POS order/delivery lines) from
/// these helpers, and Inventory seeds its MAIN-WH / RETAIL-WH warehouses with the same
/// IDs — so a POS stock deduction targets the exact warehouse the stock lives in.
///
/// Sequence ranges (see <see cref="CrossModuleGuid"/>):
///   3001–3099  Sales/Inventory shared warehouses
/// </summary>
public static class CrossModuleIds
{
    /// <summary>Deterministic ID of the company's main (back-store) warehouse.</summary>
    public static Guid MainWarehouseId(Guid companyId) => CrossModuleGuid.Derive(companyId, 3001);

    /// <summary>Deterministic ID of the company's retail (shop-floor / POS) warehouse.</summary>
    public static Guid RetailWarehouseId(Guid companyId) => CrossModuleGuid.Derive(companyId, 3002);
}
