using Sales.Application.DTOs;
using Sales.Application.Services.Interfaces;
using Sales.Domain.Entities;
using Sales.Infrastructure.Repositories.Interfaces;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using D = Sales.Application.Constants.LabelDesignerDefaults;

namespace Sales.Infrastructure.Services;

/// <summary>
/// Renders product barcode labels to PDF, sized and styled per the template
/// (physical mm size or continuous roll, fonts, borders), with ZXing-generated barcodes embedded.
/// One label per page; one page per copy.
/// </summary>
public class BarcodeLabelService : IBarcodeLabelService
{
    private readonly IPosBarcodeLabelTemplateRepository _templates;
    private readonly IBarcodeService _barcodes;

    public BarcodeLabelService(IPosBarcodeLabelTemplateRepository templates, IBarcodeService barcodes)
    {
        _templates = templates;
        _barcodes  = barcodes;
    }

    public async Task<byte[]?> RenderLabelPdfAsync(RenderBarcodeLabelDto dto)
    {
        var template = await ResolveTemplateAsync(dto.TemplateId);
        if (dto.TemplateId.HasValue && template is null) return null;
        template ??= Default();

        var item = new BarcodeLabelItemDto
        {
            BarcodeValue = dto.BarcodeValue,
            ProductName  = dto.ProductName,
            Sku          = dto.Sku,
            Price        = dto.Price,
            Copies       = dto.Copies,
        };
        return Render(template, dto.Symbology ?? template.BarcodeSymbology, [item]);
    }

    public async Task<byte[]?> RenderBatchPdfAsync(RenderBarcodeLabelBatchDto dto)
    {
        var template = await ResolveTemplateAsync(dto.TemplateId);
        if (dto.TemplateId.HasValue && template is null) return null;
        template ??= Default();

        var items = (dto.Items ?? []).Where(i => !string.IsNullOrWhiteSpace(i.BarcodeValue)).ToList();
        return Render(template, dto.Symbology ?? template.BarcodeSymbology, items);
    }

    // ── Rendering ──────────────────────────────────────────────────────────────

    private byte[] Render(PosBarcodeLabelTemplate t, string symbology, IReadOnlyList<BarcodeLabelItemDto> items)
    {
        var isQr = symbology.Replace("-", "").Replace("_", "").Trim().ToUpperInvariant() is "QR" or "QRCODE";

        // Generate each distinct barcode once.
        var cache = new Dictionary<string, byte[]>();
        byte[] Barcode(string value) =>
            cache.TryGetValue(value, out var png) ? png
                : cache[value] = _barcodes.GeneratePng(value, symbology, isQr ? 400 : 700, isQr ? 400 : 220);

        var nameFont  = (float)Math.Clamp(t.ProductNameFontPt <= 0 ? D.ProductNameFontDefault : t.ProductNameFontPt, D.ProductNameFontMin, D.ProductNameFontMax);
        var priceFont = (float)Math.Clamp(t.PriceFontPt       <= 0 ? D.PriceFontDefault       : t.PriceFontPt,       D.PriceFontMin,       D.PriceFontMax);
        var barcodeH  = (float)Math.Clamp(t.BarcodeHeightPt   <= 0 ? D.BarcodeHeightDefault   : t.BarcodeHeightPt,   D.BarcodeHeightMin,   D.BarcodeHeightMax);

        return Document.Create(doc =>
        {
            foreach (var item in items)
            {
                var png = Barcode(item.BarcodeValue);
                var copies = Math.Clamp(item.Copies <= 0 ? 1 : item.Copies, 1, 500);

                for (var i = 0; i < copies; i++)
                {
                    doc.Page(page =>
                    {
                        if (t.RollPaper) page.ContinuousSize((float)t.LabelWidthMm, Unit.Millimetre);
                        else             page.Size((float)t.LabelWidthMm, (float)t.LabelHeightMm, Unit.Millimetre);

                        page.Margin(1.5f, Unit.Millimetre);
                        page.DefaultTextStyle(s => s.FontSize(nameFont));

                        page.Content()
                            .Element(c => t.ShowBorders ? c.Border(0.7f).BorderColor(Colors.Black) : c)
                            .Padding(2)
                            .Column(col =>
                            {
                                col.Spacing(1);

                                if (!string.IsNullOrWhiteSpace(t.HeaderText))
                                    col.Item().AlignCenter().Text(t.HeaderText).Bold().FontSize(nameFont - 1);

                                if (t.ShowProductName && !string.IsNullOrWhiteSpace(item.ProductName))
                                    col.Item().AlignCenter().Text(item.ProductName).SemiBold().FontSize(nameFont);

                                col.Item().AlignCenter().Height(barcodeH).Image(png);

                                if (t.ShowBarcodeValue && !string.IsNullOrWhiteSpace(item.BarcodeValue))
                                    col.Item().AlignCenter().Text(item.BarcodeValue).FontSize(Math.Max(5, nameFont - 2));

                                if (t.ShowSku && !string.IsNullOrWhiteSpace(item.Sku))
                                    col.Item().AlignCenter().Text($"SKU: {item.Sku}").FontSize(Math.Max(5, nameFont - 3)).FontColor(Colors.Grey.Darken1);

                                if (t.ShowPrice && item.Price.HasValue)
                                    col.Item().AlignCenter().Text($"{t.CurrencySymbol}{item.Price.Value:N2}".Trim()).Bold().FontSize(priceFont);
                            });
                    });
                }
            }
        }).GeneratePdf();
    }

    private async Task<PosBarcodeLabelTemplate?> ResolveTemplateAsync(Guid? templateId)
        => templateId.HasValue
            ? await _templates.GetByIdAsync(templateId.Value)
            : await _templates.GetDefaultAsync(Guid.Empty);

    private static PosBarcodeLabelTemplate Default() => new()
    {
        TemplateName      = "Default Label",
        LabelWidthMm      = 50,
        LabelHeightMm     = 30,
        BarcodeSymbology  = "Code128",
        ShowProductName   = true,
        ShowPrice         = true,
        ShowSku           = true,
        ShowBarcodeValue  = true,
        ProductNameFontPt = 8,
        PriceFontPt       = 11,
        BarcodeHeightPt   = 12,
        ShowBorders       = true,
    };
}
