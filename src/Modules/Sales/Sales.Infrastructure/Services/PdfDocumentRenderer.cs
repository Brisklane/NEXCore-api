using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Sales.Infrastructure.Services;

/// <summary>
/// Renders a generic A4 sales document (receipt / invoice) to PDF bytes using QuestPDF.
/// Both POS receipts and invoices map to <see cref="PdfDocModel"/> and share this single layout.
/// All numeric values are pre-formatted to strings by the caller.
/// </summary>
internal static class PdfDocumentRenderer
{
    static PdfDocumentRenderer()
    {
        // Free for organisations/individuals under the QuestPDF Community licence threshold.
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public static byte[] Render(PdfDocModel m) =>
        Document.Create(doc =>
        {
            doc.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(32);
                page.DefaultTextStyle(t => t.FontSize(10).FontColor(Colors.Grey.Darken4));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            if (!string.IsNullOrWhiteSpace(m.SellerName))
                                left.Item().Text(m.SellerName).Bold().FontSize(16);
                            foreach (var line in m.SellerLines)
                                left.Item().Text(line).FontSize(9).FontColor(Colors.Grey.Medium);
                        });
                        row.ConstantItem(230).Column(right =>
                        {
                            right.Item().AlignRight().Text(m.Title).Bold().FontSize(20);
                            foreach (var (label, value) in m.Meta)
                                right.Item().AlignRight().Text($"{label}: {value}").FontSize(9).FontColor(Colors.Grey.Medium);
                        });
                    });
                    col.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Darken1);
                });

                page.Content().PaddingVertical(10).Column(col =>
                {
                    if (m.BillToLines.Count > 0)
                    {
                        col.Item().PaddingBottom(10).Column(b =>
                        {
                            b.Item().Text(m.BillToTitle ?? "BILL TO").FontSize(9).FontColor(Colors.Grey.Medium);
                            foreach (var line in m.BillToLines) b.Item().Text(line);
                        });
                    }

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(24);    // #
                            c.RelativeColumn();       // description
                            c.ConstantColumn(45);     // qty
                            c.ConstantColumn(62);     // unit
                            c.ConstantColumn(58);     // disc
                            if (m.ShowTaxColumn) c.ConstantColumn(58);  // tax
                            c.ConstantColumn(68);     // amount
                        });

                        table.Header(h =>
                        {
                            h.Cell().Element(HeaderCell).Text("#");
                            h.Cell().Element(HeaderCell).Text("Description");
                            h.Cell().Element(HeaderCell).AlignRight().Text("Qty");
                            h.Cell().Element(HeaderCell).AlignRight().Text("Unit");
                            h.Cell().Element(HeaderCell).AlignRight().Text("Disc");
                            if (m.ShowTaxColumn) h.Cell().Element(HeaderCell).AlignRight().Text("Tax");
                            h.Cell().Element(HeaderCell).AlignRight().Text("Amount");
                        });

                        foreach (var l in m.Lines)
                        {
                            table.Cell().Element(BodyCell).Text(l.No.ToString());
                            table.Cell().Element(BodyCell).Text(l.Description);
                            table.Cell().Element(BodyCell).AlignRight().Text(l.Qty);
                            table.Cell().Element(BodyCell).AlignRight().Text(l.Unit);
                            table.Cell().Element(BodyCell).AlignRight().Text(l.Disc);
                            if (m.ShowTaxColumn) table.Cell().Element(BodyCell).AlignRight().Text(l.Tax);
                            table.Cell().Element(BodyCell).AlignRight().Text(l.Amount);
                        }
                    });

                    col.Item().PaddingTop(12).AlignRight().Width(250).Column(totals =>
                    {
                        foreach (var (label, value, grand) in m.Totals)
                        {
                            totals.Item().PaddingTop(grand ? 4 : 0).Row(r =>
                            {
                                r.RelativeItem().Text(span => { var s = span.Span(label); if (grand) s.Bold().FontSize(12); });
                                r.ConstantItem(120).AlignRight().Text(span => { var s = span.Span(value); if (grand) s.Bold().FontSize(12); });
                            });
                        }
                    });
                });

                page.Footer().Column(col =>
                {
                    col.Item().PaddingTop(6).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten1);
                    if (!string.IsNullOrWhiteSpace(m.FooterMessage))
                        col.Item().AlignCenter().Text(m.FooterMessage).FontSize(9).FontColor(Colors.Grey.Medium);
                    if (!string.IsNullOrWhiteSpace(m.ReturnPolicy))
                        col.Item().AlignCenter().Text(m.ReturnPolicy).FontSize(8).FontColor(Colors.Grey.Medium);

                    if (m.BarcodeImage is not null)
                        col.Item().PaddingTop(4).AlignCenter().Height(38).Image(m.BarcodeImage);
                    if (m.QrImage is not null)
                        col.Item().PaddingTop(4).AlignCenter().Height(70).Image(m.QrImage);

                    // Human-readable value under the code (plain when an image is shown, else *placeholder*).
                    if (!string.IsNullOrWhiteSpace(m.BarcodeText))
                        col.Item().PaddingTop(2).AlignCenter()
                           .Text(m.BarcodeImage is not null || m.QrImage is not null ? m.BarcodeText : $"*{m.BarcodeText}*")
                           .FontSize(9);
                });
            });
        }).GeneratePdf();

    private static IContainer HeaderCell(IContainer c) =>
        c.PaddingVertical(4).BorderBottom(1).BorderColor(Colors.Grey.Darken1)
         .DefaultTextStyle(t => t.SemiBold().FontSize(9));

    private static IContainer BodyCell(IContainer c) =>
        c.PaddingVertical(3).BorderBottom((float)0.5).BorderColor(Colors.Grey.Lighten2);
}

// ── Model (populated by ReceiptRenderingService) ───────────────────────────────

internal sealed record PdfDocModel
{
    public string Title { get; init; } = string.Empty;
    public string SellerName { get; init; } = string.Empty;
    public IReadOnlyList<string> SellerLines { get; init; } = [];
    public IReadOnlyList<(string Label, string Value)> Meta { get; init; } = [];
    public string? BillToTitle { get; init; }
    public IReadOnlyList<string> BillToLines { get; init; } = [];
    public bool ShowTaxColumn { get; init; }
    public IReadOnlyList<PdfLineModel> Lines { get; init; } = [];
    public IReadOnlyList<(string Label, string Value, bool Grand)> Totals { get; init; } = [];
    public string FooterMessage { get; init; } = string.Empty;
    public string? ReturnPolicy { get; init; }
    public string? BarcodeText { get; init; }
    public byte[]? BarcodeImage { get; init; }
    public byte[]? QrImage { get; init; }
}

internal sealed record PdfLineModel(int No, string Description, string Qty, string Unit, string Disc, string Tax, string Amount);
