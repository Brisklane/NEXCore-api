using Nexcore.SharedKernel;

namespace Sales.Domain.Entities;

/// <summary>
/// Join entity — links an Inventory.TaxDefinition (by cross-module Guid) into a TaxGroup
/// with an explicit sequence and optional compound flag.
///
/// TaxDefinitionId is a cross-module FK to inv.TaxDefinitions — no EF navigation property.
/// Snapshot fields are copied from TaxDefinition at grouping time for fast reads.
/// </summary>
public class TaxGroupRate : BaseEntity
{
    public Guid TaxGroupId { get; set; }
    public TaxGroup TaxGroup { get; set; } = null!;

    /// <summary>
    /// Cross-module reference to Inventory.TaxDefinition.
    /// Never navigated in EF — resolved via Inventory module when full detail is needed.
    /// </summary>
    public Guid TaxDefinitionId { get; set; }

    // ??? Snapshots (copied from TaxDefinition at grouping time) ??????????????
    /// <summary>Snapshot of TaxDefinition.Code (e.g., "VAT15") for display without cross-module query.</summary>
    public string SnapshotCode { get; set; } = string.Empty;

    /// <summary>Snapshot of TaxDefinition.Rate at time of grouping.</summary>
    public decimal SnapshotRate { get; set; }

    /// <summary>
    /// Snapshot of TaxDefinition.TaxType stored as string (e.g., "VAT", "Excise").
    /// Avoids a cross-module enum reference — compared as string at runtime.
    /// </summary>
    public string SnapshotTaxType { get; set; } = string.Empty;

    /// <summary>
    /// Snapshot of TaxDefinition.InclusionType stored as string ("Exclusive" / "Inclusive").
    /// </summary>
    public string SnapshotInclusionType { get; set; } = string.Empty;

    // ??? Calculation Control ??????????????????????????????????????????????????
    /// <summary>Sequence determines calculation order. Lower = earlier.</summary>
    public int Sequence { get; set; }

    /// <summary>
    /// When true, this rate is applied on top of the running subtotal after previous rates
    /// (compound tax, e.g., Excise first, then VAT on the excise-inclusive amount).
    /// </summary>
    public bool IsCompound { get; set; }
}
