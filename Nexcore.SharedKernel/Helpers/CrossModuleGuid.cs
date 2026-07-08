namespace Nexcore.SharedKernel.Helpers;

/// <summary>
/// Derives deterministic, per-company GUIDs for cross-module seed references.
///
/// Both modules (e.g. Manufacturing and Inventory) call Derive() with the same
/// companyId and sequence number and get back the identical GUID — so each
/// company gets its own unique IDs with no primary-key conflicts.
///
/// Sequence ranges (by convention):
///   1001–1099  Manufacturing finished goods
///   1101–1199  Manufacturing raw materials
///   2001–2099  Manufacturing warehouses
///   3001–3099  Sales/Inventory shared warehouses (see <see cref="Constants.CrossModuleIds"/>)
/// </summary>
public static class CrossModuleGuid
{
    /// <summary>
    /// Replaces the last 4 bytes of <paramref name="companyId"/> with
    /// <paramref name="sequence"/>, producing a stable per-company GUID.
    /// </summary>
    public static Guid Derive(Guid companyId, int sequence)
    {
        var b   = companyId.ToByteArray();
        var seq = BitConverter.GetBytes(sequence);
        b[12] = seq[0];
        b[13] = seq[1];
        b[14] = seq[2];
        b[15] = seq[3];
        return new Guid(b);
    }
}
