namespace Sales.Application.Services.Interfaces;

/// <summary>
/// Generates barcode / QR images. Used for product labels and for printable receipts.
/// </summary>
public interface IBarcodeService
{
    /// <summary>
    /// Render a barcode as a PNG. <paramref name="symbology"/> is one of
    /// Code128, Code39, EAN-13, EAN-8, UPC-A, QR (defaults to Code128).
    /// For QR codes the image is square (uses the smaller of width/height).
    /// </summary>
    byte[] GeneratePng(string value, string? symbology = "Code128", int widthPx = 400, int heightPx = 150);
}
