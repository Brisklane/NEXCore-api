using Nexcore.SharedKernel;

namespace Inventory.Domain.Entities;

/// <summary>
/// Item Shipping
/// Physical and logistics properties of an item.
/// Required for AliExpress, Daraz, and any marketplace that calculates shipping costs.
/// Also used in Cash &amp; Carry for pallet/carton planning.
/// </summary>
public class ItemShipping : BaseEntity
{
    public Guid ItemId { get; set; }

    // ?? Physical Dimensions ???????????????????????????????????????????????

    /// <summary>Weight in kilograms</summary>
    public decimal? WeightKg { get; set; }

    /// <summary>Length in centimeters</summary>
    public decimal? LengthCm { get; set; }

    /// <summary>Width in centimeters</summary>
    public decimal? WidthCm { get; set; }

    /// <summary>Height in centimeters</summary>
    public decimal? HeightCm { get; set; }

    /// <summary>
    /// Volumetric weight in KG (calculated: L × W × H / 5000 for air freight).
    /// Stored to avoid recalculation on every query.
    /// </summary>
    public decimal? VolumetricWeightKg { get; set; }

    // ?? Packaging ????????????????????????????????????????????????????????

    /// <summary>
    /// Country of origin ISO code (e.g., CN, PK, IN, US).
    /// Required for customs declarations on AliExpress / Daraz cross-border.
    /// </summary>
    public string? CountryOfOrigin { get; set; }

    /// <summary>HS (Harmonized System) tariff code for customs</summary>
    public string? HsCode { get; set; }

    /// <summary>Units per inner carton (e.g., 12 PCS per box)</summary>
    public int? UnitsPerCarton { get; set; }

    /// <summary>Cartons per pallet — used in Cash &amp; Carry and wholesale</summary>
    public int? CartonsPerPallet { get; set; }

    /// <summary>Carton weight in KG (full carton)</summary>
    public decimal? CartonWeightKg { get; set; }

    /// <summary>Carton length in centimeters</summary>
    public decimal? CartonLengthCm { get; set; }

    /// <summary>Carton width in centimeters</summary>
    public decimal? CartonWidthCm { get; set; }

    /// <summary>Carton height in centimeters</summary>
    public decimal? CartonHeightCm { get; set; }

    // ?? Shipping Flags ???????????????????????????????????????????????????

    /// <summary>Whether the item requires special handling (fragile, hazmat, oversized)</summary>
    public bool RequiresSpecialHandling { get; set; }

    /// <summary>Special handling notes (e.g., "Fragile — this side up")</summary>
    public string? HandlingNotes { get; set; }

    /// <summary>Whether item is a dangerous good / hazmat (affects marketplace eligibility)</summary>
    public bool IsHazmat { get; set; }

    /// <summary>Whether item can be shipped internationally</summary>
    public bool IsShippableInternational { get; set; } = true;

    // Navigation property
    public Item? Item { get; set; }
}
