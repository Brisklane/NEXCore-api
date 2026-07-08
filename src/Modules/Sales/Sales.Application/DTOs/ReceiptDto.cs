namespace Sales.Application.DTOs;

/// <summary>
/// A rendered thermal receipt, ready to send to a 58mm / 80mm thermal printer.
/// <see cref="Content"/> is plain monospace text wrapped to <see cref="CharactersPerLine"/>.
/// The UI sends it to the printer as-is (and can generate a barcode/QR from <see cref="ReceiptNumber"/>).
/// </summary>
public class ThermalReceiptDto
{
    /// <summary>"Thermal58mm" or "Thermal80mm".</summary>
    public string PaperSize { get; set; } = "Thermal80mm";

    /// <summary>Printable width in characters (32 for 58mm, 48 for 80mm).</summary>
    public int CharactersPerLine { get; set; }

    /// <summary>The receipt number (= invoice number) — use for the printed barcode/QR.</summary>
    public string? ReceiptNumber { get; set; }

    /// <summary>The fully rendered, line-wrapped receipt text.</summary>
    public string Content { get; set; } = string.Empty;
}
