using System.Runtime.InteropServices;
using Sales.Application.Services.Interfaces;
using SkiaSharp;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;

namespace Sales.Infrastructure.Services;

/// <summary>
/// Barcode/QR generation via ZXing.Net (pixel data) rendered to PNG with SkiaSharp
/// (SkiaSharp ships transitively with QuestPDF — no extra native dependency).
/// </summary>
public class BarcodeService : IBarcodeService
{
    public byte[] GeneratePng(string value, string? symbology = "Code128", int widthPx = 400, int heightPx = 150)
    {
        if (string.IsNullOrEmpty(value)) value = " ";
        if (widthPx <= 0) widthPx = 400;
        if (heightPx <= 0) heightPx = 150;

        var format = MapFormat(symbology);
        var writer = new BarcodeWriterPixelData
        {
            Format  = format,
            Options = BuildOptions(format, widthPx, heightPx),
        };

        var pixelData = writer.Write(value);   // BGRA, 4 bytes/pixel

        var info = new SKImageInfo(pixelData.Width, pixelData.Height, SKColorType.Bgra8888, SKAlphaType.Opaque);
        using var bitmap = new SKBitmap(info);
        Marshal.Copy(pixelData.Pixels, 0, bitmap.GetPixels(), pixelData.Pixels.Length);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static BarcodeFormat MapFormat(string? symbology) =>
        (symbology ?? "Code128").Replace("-", "").Replace("_", "").Trim().ToUpperInvariant() switch
        {
            "CODE128"        => BarcodeFormat.CODE_128,
            "CODE39"         => BarcodeFormat.CODE_39,
            "EAN13"          => BarcodeFormat.EAN_13,
            "EAN8"           => BarcodeFormat.EAN_8,
            "UPCA"           => BarcodeFormat.UPC_A,
            "QR" or "QRCODE" => BarcodeFormat.QR_CODE,
            _                => BarcodeFormat.CODE_128,
        };

    private static EncodingOptions BuildOptions(BarcodeFormat format, int width, int height)
    {
        if (format == BarcodeFormat.QR_CODE)
        {
            var side = Math.Max(64, Math.Min(width, height));
            return new QrCodeEncodingOptions { Width = side, Height = side, Margin = 1 };
        }

        return new EncodingOptions { Width = width, Height = height, Margin = 5, PureBarcode = true };
    }
}
