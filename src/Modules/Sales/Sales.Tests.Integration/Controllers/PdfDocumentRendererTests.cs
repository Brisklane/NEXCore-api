using Sales.Infrastructure.Services;

namespace Sales.Tests.Integration.Controllers;

/// <summary>
/// Pure (no-DB) smoke tests that the QuestPDF layout actually generates valid PDF bytes — catches
/// runtime layout/license errors the compiler can't. Not part of the DB collection.
/// </summary>
public class PdfDocumentRendererTests
{
    private static PdfDocModel SampleModel(bool withTax) => new()
    {
        Title       = "Invoice",
        SellerName  = "Acme Retail Co.",
        SellerLines = ["123 Market Road", "Tel: +1-555-0100  •  sales@acme.test", "Tax #: TRN-99887766"],
        Meta        = [("Invoice #", "INV-2026-00001"), ("Date", "2026-06-08"), ("Status", "Issued")],
        BillToTitle = "BILL TO",
        BillToLines = ["Zara Khan", "42 Garden Lane", "Lahore, PK"],
        ShowTaxColumn = withTax,
        Lines =
        [
            new PdfLineModel(1, "Premium Widget (Blue)", "2", "100.00", "-10.00", "27.00", "190.00"),
            new PdfLineModel(2, "Standard Gadget", "1", "50.00", "-", "7.50", "50.00"),
        ],
        Totals =
        [
            ("Subtotal", "240.00", false),
            ("Discount", "-10.00", false),
            ("Tax", "34.50", false),
            ("Total", "USD 264.50", true),
            ("Paid", "100.00", false),
            ("Balance Due", "USD 164.50", true),
        ],
        FooterMessage = "Thank you for your business!",
        ReturnPolicy  = "Returns accepted within 30 days with receipt.",
        BarcodeText   = "INV-2026-00001",
    };

    [Fact]
    public void Render_Invoice_ProducesValidPdfBytes()
    {
        var bytes = PdfDocumentRenderer.Render(SampleModel(withTax: true));

        bytes.Should().NotBeNullOrEmpty();
        bytes.Length.Should().BeGreaterThan(1000);
        // PDF magic number "%PDF"
        bytes[0].Should().Be((byte)'%');
        bytes[1].Should().Be((byte)'P');
        bytes[2].Should().Be((byte)'D');
        bytes[3].Should().Be((byte)'F');
    }

    [Fact]
    public void Render_Receipt_NoTaxColumn_ProducesValidPdfBytes()
    {
        var bytes = PdfDocumentRenderer.Render(SampleModel(withTax: false));

        bytes.Should().NotBeNullOrEmpty();
        bytes[0].Should().Be((byte)'%');
        bytes[1].Should().Be((byte)'P');
        bytes[2].Should().Be((byte)'D');
        bytes[3].Should().Be((byte)'F');
    }
}
