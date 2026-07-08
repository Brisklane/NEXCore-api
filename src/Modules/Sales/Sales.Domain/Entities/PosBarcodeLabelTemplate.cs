using Nexcore.SharedKernel;

namespace Sales.Domain.Entities;

/// <summary>
/// Product barcode-label (price tag / shelf label) template.
/// Defines the physical label size and what is printed on it. Branch-scoped via BaseEntity;
/// one template per branch can be the <see cref="IsDefault"/>.
/// </summary>
public class PosBarcodeLabelTemplate : BaseEntity
{
    public string TemplateName { get; set; } = string.Empty;

    // ── Physical size (millimetres) ────────────────────────────────────────────
    public double LabelWidthMm { get; set; } = 50;
    public double LabelHeightMm { get; set; } = 30;

    /// <summary>Code128, Code39, EAN-13, EAN-8, UPC-A, QR.</summary>
    public string BarcodeSymbology { get; set; } = "Code128";

    // ── Content toggles ────────────────────────────────────────────────────────
    /// <summary>Optional store/brand line printed at the top of the label.</summary>
    public string? HeaderText { get; set; }
    public bool ShowProductName { get; set; } = true;
    public bool ShowPrice { get; set; } = true;
    public bool ShowSku { get; set; } = true;
    /// <summary>Print the human-readable barcode value under the bars.</summary>
    public bool ShowBarcodeValue { get; set; } = true;

    /// <summary>Currency symbol/prefix printed before the price (e.g. "$", "Rs"). Optional.</summary>
    public string? CurrencySymbol { get; set; }

    // ── Visual styling (price-tag designer) ─────────────────────────────────────
    /// <summary>Product name font size in points (designer slider 4–12).</summary>
    public double ProductNameFontPt { get; set; } = 8;
    /// <summary>Price font size in points (designer slider 6–16).</summary>
    public double PriceFontPt { get; set; } = 11;
    /// <summary>Barcode image height in points (designer slider 8–20).</summary>
    public double BarcodeHeightPt { get; set; } = 12;
    /// <summary>Draw a border around each label.</summary>
    public bool ShowBorders { get; set; } = true;
    /// <summary>Continuous roll stock — label height grows with content (no fixed page height).</summary>
    public bool RollPaper { get; set; }

    public bool IsDefault { get; set; }
}
