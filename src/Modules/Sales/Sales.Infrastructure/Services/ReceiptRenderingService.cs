using System.Text;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Events;
using Sales.Application.DTOs;
using Sales.Application.Services.Interfaces;
using Sales.Domain.Entities;
using Sales.Infrastructure.Repositories.Interfaces;

namespace Sales.Infrastructure.Services;

/// <summary>
/// Renders a POS transaction into thermal-printer text (58mm = 32 cols, 80mm = 48 cols).
/// The receipt template (header/footer/display flags) is resolved from the terminal, then the store,
/// then the branch default; when none is configured, sensible defaults are used so a receipt always prints.
/// </summary>
public class ReceiptRenderingService : IReceiptRenderingService
{
    private const int Width58 = 32;
    private const int Width80 = 48;

    private readonly IPosTransactionRepository _transactions;
    private readonly IPosReceiptTemplateRepository _templates;
    private readonly IPosTerminalRepository _terminals;
    private readonly IPosStoreRepository _stores;
    private readonly IPosCashierRepository _cashiers;
    private readonly ISalesOrderRepository _orders;
    private readonly ISalesInvoiceRepository _invoices;
    private readonly IBarcodeService _barcodes;
    private readonly IEventPublisher _events;
    private readonly ILogger<ReceiptRenderingService> _logger;

    public ReceiptRenderingService(
        IPosTransactionRepository transactions,
        IPosReceiptTemplateRepository templates,
        IPosTerminalRepository terminals,
        IPosStoreRepository stores,
        IPosCashierRepository cashiers,
        ISalesOrderRepository orders,
        ISalesInvoiceRepository invoices,
        IBarcodeService barcodes,
        IEventPublisher events,
        ILogger<ReceiptRenderingService> logger)
    {
        _transactions = transactions;
        _templates    = templates;
        _terminals    = terminals;
        _stores       = stores;
        _cashiers     = cashiers;
        _orders       = orders;
        _invoices     = invoices;
        _barcodes     = barcodes;
        _events       = events;
        _logger       = logger;
    }

    /// <summary>
    /// Generates the 1D barcode and/or QR images for a document number, honouring the template's
    /// ShowBarcode / ShowQrCode / BarcodeSymbology. If the symbology itself is QR, the barcode slot is a QR.
    /// </summary>
    private (byte[]? barcode, byte[]? qr) BuildCodes(PosReceiptTemplate? t, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return (null, null);

        byte[]? barcode = null, qr = null;
        var symbology = t?.BarcodeSymbology ?? "Code128";
        var symIsQr = symbology.Replace("-", "").Replace("_", "").Trim().ToUpperInvariant() is "QR" or "QRCODE";
        try
        {
            if (t?.ShowBarcode ?? true)
            {
                if (symIsQr) qr = _barcodes.GeneratePng(value!, "QR", 220, 220);
                else         barcode = _barcodes.GeneratePng(value!, symbology, 520, 120);
            }
            if ((t?.ShowQrCode ?? false) && qr is null)
                qr = _barcodes.GeneratePng(value!, "QR", 180, 180);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Barcode generation failed for {Value}", value);
        }
        return (barcode, qr);
    }

    public async Task<ThermalReceiptDto?> RenderThermalAsync(Guid posTransactionId, string? paperSizeOverride = null)
    {
        var txn = await _transactions.GetWithLinesAsync(posTransactionId);
        if (txn is null) return null;

        var template = await ResolveTemplateAsync(txn);
        var store    = await _stores.GetByIdAsync(txn.PosStoreId);
        var cashier  = await _cashiers.GetByIdAsync(txn.PosCashierId);
        var order    = txn.SalesOrderId.HasValue ? await _orders.GetByIdAsync(txn.SalesOrderId.Value) : null;

        var paperSize = Normalize(paperSizeOverride ?? template?.PaperSize ?? "Thermal80mm");
        var width     = paperSize == "Thermal58mm" ? Width58 : Width80;

        var sb = new StringBuilder();

        // ── Header ──
        var businessName = template?.HeaderBusinessName ?? store?.TradingName;
        if (!string.IsNullOrWhiteSpace(businessName)) AppendCentered(sb, businessName!, width, upper: true);
        AppendCenteredIf(sb, template?.HeaderAddressLine1, width);
        AppendCenteredIf(sb, template?.HeaderAddressLine2, width);
        if (!string.IsNullOrWhiteSpace(template?.HeaderPhone)) AppendCentered(sb, $"Tel: {template!.HeaderPhone}", width);
        if (!string.IsNullOrWhiteSpace(template?.TaxRegistrationNumber)) AppendCentered(sb, $"Tax #: {template!.TaxRegistrationNumber}", width);
        AppendCenteredIf(sb, template?.HeaderMessage, width);
        Rule(sb, width);

        // ── Transaction info ──
        AppendLeftRight(sb, "Receipt#", txn.ReceiptNumber ?? txn.TransactionNumber, width);
        AppendLeftRight(sb, "Txn#", txn.TransactionNumber, width);
        AppendLeftRight(sb, "Date", txn.TransactionDate.ToString("yyyy-MM-dd HH:mm"), width);
        if ((template?.ShowCashierName ?? true) && !string.IsNullOrWhiteSpace(cashier?.DisplayName))
            AppendLeftRight(sb, "Cashier", cashier!.DisplayName, width);
        if ((template?.ShowCustomerName ?? true) && !string.IsNullOrWhiteSpace(order?.ContactName))
            AppendLeftRight(sb, "Customer", order!.ContactName!, width);
        Rule(sb, width);

        // ── Line items ──
        var showDiscountLine = template?.ShowDiscountLine ?? true;
        foreach (var line in txn.Lines.OrderBy(l => l.LineNumber))
        {
            foreach (var part in Wrap(line.ProductName, width)) sb.AppendLine(part);
            var qtyXprice = $"  {Qty(line.Quantity)} x {Money(line.UnitPrice)}";
            AppendLeftRight(sb, qtyXprice, Money(line.LineAmount), width);
            if (showDiscountLine && line.DiscountAmount > 0)
                AppendLeftRight(sb, "  Discount", $"-{Money(line.DiscountAmount)}", width);
        }
        Rule(sb, width);

        // ── Totals ──
        AppendLeftRight(sb, "Subtotal", Money(txn.SubtotalAmount), width);
        if (showDiscountLine && txn.DiscountAmount > 0)
            AppendLeftRight(sb, "Discount", $"-{Money(txn.DiscountAmount)}", width);
        if ((template?.ShowTaxBreakdown ?? true) && txn.TaxAmount > 0)
            AppendLeftRight(sb, "Tax", Money(txn.TaxAmount), width);
        if (txn.RoundingAmount != 0)
            AppendLeftRight(sb, "Rounding", Money(txn.RoundingAmount), width);
        AppendLeftRight(sb, "TOTAL", Money(txn.TotalAmount), width);

        // ── Tender / change ──
        sb.AppendLine();
        foreach (var p in txn.Payments)
            AppendLeftRight(sb, p.TenderType.ToString(), Money(p.Amount), width);
        AppendLeftRight(sb, "Tendered", Money(txn.TenderedAmount), width);
        AppendLeftRight(sb, "Change", Money(txn.ChangeAmount), width);

        // ── Footer ──
        Rule(sb, width);
        AppendCenteredIf(sb, template?.FooterMessage ?? "Thank you for shopping with us!", width);
        foreach (var part in Wrap(template?.ReturnPolicy ?? string.Empty, width)) AppendCentered(sb, part, width);
        if (template?.ShowBarcode ?? true)
        {
            sb.AppendLine();
            AppendCentered(sb, $"*{txn.ReceiptNumber ?? txn.TransactionNumber}*", width);
        }

        return new ThermalReceiptDto
        {
            PaperSize         = paperSize,
            CharactersPerLine = width,
            ReceiptNumber     = txn.ReceiptNumber ?? txn.TransactionNumber,
            Content           = sb.ToString(),
        };
    }

    // ── Full A4 HTML receipt (POS transaction) ─────────────────────────────────

    public async Task<string?> RenderHtmlReceiptAsync(Guid posTransactionId)
    {
        var txn = await _transactions.GetWithLinesAsync(posTransactionId);
        if (txn is null) return null;

        var template    = await ResolveTemplateAsync(txn);
        var store       = await _stores.GetByIdAsync(txn.PosStoreId);
        var cashier     = await _cashiers.GetByIdAsync(txn.PosCashierId);
        var order       = txn.SalesOrderId.HasValue ? await _orders.GetByIdAsync(txn.SalesOrderId.Value) : null;
        var currency    = order?.CurrencyCode ?? string.Empty;
        var resolvedLogo = await ResolveLogoAsync(template, txn.CompanyId);

        var body = new StringBuilder();

        // Header
        body.Append(HtmlHeader(template, store,
            title: "Sales Receipt",
            resolvedLogoUrl: resolvedLogo,
            metaRows:
            [
                ("Receipt #", txn.ReceiptNumber ?? txn.TransactionNumber),
                ("Transaction #", txn.TransactionNumber),
                ("Date", txn.TransactionDate.ToString("yyyy-MM-dd HH:mm")),
            ]));

        // Sold-to / cashier strip
        var info = new List<string>();
        if ((template?.ShowCashierName ?? true) && !string.IsNullOrWhiteSpace(cashier?.DisplayName))
            info.Add($"<strong>Cashier:</strong> {E(cashier!.DisplayName)}");
        if ((template?.ShowCustomerName ?? true) && !string.IsNullOrWhiteSpace(order?.ContactName))
            info.Add($"<strong>Customer:</strong> {E(order!.ContactName!)}");
        if (info.Count > 0)
            body.Append($"<div class=\"meta\"><div>{string.Join("<br>", info)}</div></div>");

        // Items
        body.Append("<table><thead><tr><th>#</th><th>Item</th><th class=\"r\">Qty</th>"
                  + "<th class=\"r\">Unit</th><th class=\"r\">Disc</th><th class=\"r\">Amount</th></tr></thead><tbody>");
        var n = 1;
        foreach (var l in txn.Lines.OrderBy(l => l.LineNumber))
        {
            body.Append($"<tr><td>{n++}</td><td>{E(l.ProductName)}{VariantSuffix(l.VariantName)}</td>"
                      + $"<td class=\"r\">{Qty(l.Quantity)}</td><td class=\"r\">{Money(l.UnitPrice)}</td>"
                      + $"<td class=\"r\">{(l.DiscountAmount > 0 ? "-" + Money(l.DiscountAmount) : "-")}</td>"
                      + $"<td class=\"r\">{Money(l.LineAmount)}</td></tr>");
        }
        body.Append("</tbody></table>");

        // Totals
        var totals = new List<(string, string, bool)>
        {
            ("Subtotal", Money(txn.SubtotalAmount), false),
        };
        if ((template?.ShowDiscountLine ?? true) && txn.DiscountAmount > 0)
            totals.Add(("Discount", "-" + Money(txn.DiscountAmount), false));
        if ((template?.ShowTaxBreakdown ?? true) && txn.TaxAmount > 0)
            totals.Add(("Tax", Money(txn.TaxAmount), false));
        totals.Add(("Total", $"{currency} {Money(txn.TotalAmount)}".Trim(), true));
        foreach (var p in txn.Payments)
            totals.Add((p.TenderType.ToString(), Money(p.Amount), false));
        totals.Add(("Tendered", Money(txn.TenderedAmount), false));
        totals.Add(("Change", Money(txn.ChangeAmount), false));
        body.Append(HtmlTotals(totals));

        // Footer (with real barcode/QR images)
        var receiptNo = txn.ReceiptNumber ?? txn.TransactionNumber;
        var (barcode, qr) = BuildCodes(template, receiptNo);
        body.Append(HtmlFooter(template, (barcode is not null || qr is not null) ? receiptNo : null, barcode, qr));

        return HtmlDocument($"Receipt {receiptNo}", body.ToString());
    }

    // ── Full A4 HTML invoice document (Sales Invoice) ──────────────────────────

    public async Task<string?> RenderHtmlInvoiceAsync(Guid salesInvoiceId)
    {
        var inv = await _invoices.GetWithLinesAsync(salesInvoiceId);
        if (inv is null) return null;

        var template     = await _templates.GetDefaultAsync(inv.BranchId);
        var currency     = inv.CurrencyCode;
        var resolvedLogo = await ResolveLogoAsync(template, inv.CompanyId);

        var body = new StringBuilder();

        body.Append(HtmlHeader(template, store: null,
            title: "Invoice",
            resolvedLogoUrl: resolvedLogo,
            metaRows:
            [
                ("Invoice #", inv.InvoiceNumber),
                ("Invoice Date", inv.InvoiceDate.ToString("yyyy-MM-dd")),
                ("Due Date", inv.DueDate.ToString("yyyy-MM-dd")),
                ("Status", inv.Status.ToString()),
            ]));

        // Bill-to
        var billTo = new List<string?> { inv.BillToName ?? inv.ContactName, inv.BillToStreet,
            string.Join(", ", new[] { inv.BillToCity, inv.BillToState, inv.BillToPostalCode }
                .Where(s => !string.IsNullOrWhiteSpace(s))), inv.BillToCountry };
        var billLines = billTo.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => E(s!)).ToList();
        if (billLines.Count > 0)
            body.Append($"<div class=\"meta\"><div><div class=\"muted\">BILL TO</div>{string.Join("<br>", billLines)}</div></div>");

        // Items
        body.Append("<table><thead><tr><th>#</th><th>Description</th><th class=\"r\">Qty</th>"
                  + "<th class=\"r\">Unit</th><th class=\"r\">Disc</th><th class=\"r\">Tax</th><th class=\"r\">Amount</th></tr></thead><tbody>");
        var n = 1;
        foreach (var l in inv.Lines.OrderBy(l => l.LineNumber))
        {
            body.Append($"<tr><td>{n++}</td><td>{E(l.ProductName)}</td>"
                      + $"<td class=\"r\">{Qty(l.Quantity)}</td><td class=\"r\">{Money(l.UnitPrice)}</td>"
                      + $"<td class=\"r\">{(l.DiscountAmount > 0 ? "-" + Money(l.DiscountAmount) : "-")}</td>"
                      + $"<td class=\"r\">{Money(l.TaxAmount)}</td><td class=\"r\">{Money(l.TotalAmount)}</td></tr>");
        }
        body.Append("</tbody></table>");

        // Totals
        var totals = new List<(string, string, bool)>
        {
            ("Subtotal", Money(inv.SubtotalAmount), false),
        };
        if (inv.DiscountAmount > 0) totals.Add(("Discount", "-" + Money(inv.DiscountAmount), false));
        if (inv.TaxAmount > 0)      totals.Add(("Tax", Money(inv.TaxAmount), false));
        if (inv.ShippingAmount > 0) totals.Add(("Shipping", Money(inv.ShippingAmount), false));
        totals.Add(("Total", $"{currency} {Money(inv.TotalAmount)}".Trim(), true));
        if (inv.PaidAmount > 0)     totals.Add(("Paid", Money(inv.PaidAmount), false));
        totals.Add(("Balance Due", $"{currency} {Money(inv.BalanceDue)}".Trim(), true));
        body.Append(HtmlTotals(totals));

        if (!string.IsNullOrWhiteSpace(inv.PaymentReference))
            body.Append($"<p class=\"muted\">Payment reference: {E(inv.PaymentReference)}</p>");
        if (!string.IsNullOrWhiteSpace(inv.Notes))
            body.Append($"<p class=\"muted\">{E(inv.Notes)}</p>");

        var (barcode, qr) = BuildCodes(template, inv.InvoiceNumber);
        body.Append(HtmlFooter(template, (barcode is not null || qr is not null) ? inv.InvoiceNumber : null, barcode, qr));

        return HtmlDocument($"Invoice {inv.InvoiceNumber}", body.ToString());
    }

    // ── PDF (QuestPDF) ─────────────────────────────────────────────────────────

    public async Task<byte[]?> RenderPdfReceiptAsync(Guid posTransactionId)
    {
        var txn = await _transactions.GetWithLinesAsync(posTransactionId);
        if (txn is null) return null;

        var template = await ResolveTemplateAsync(txn);
        var store    = await _stores.GetByIdAsync(txn.PosStoreId);
        var cashier  = await _cashiers.GetByIdAsync(txn.PosCashierId);
        var order    = txn.SalesOrderId.HasValue ? await _orders.GetByIdAsync(txn.SalesOrderId.Value) : null;
        var currency = order?.CurrencyCode ?? string.Empty;

        var meta = new List<(string, string)>
        {
            ("Receipt #", txn.ReceiptNumber ?? txn.TransactionNumber),
            ("Transaction #", txn.TransactionNumber),
            ("Date", txn.TransactionDate.ToString("yyyy-MM-dd HH:mm")),
        };
        if ((template?.ShowCashierName ?? true) && !string.IsNullOrWhiteSpace(cashier?.DisplayName))
            meta.Add(("Cashier", cashier!.DisplayName));
        if ((template?.ShowCustomerName ?? true) && !string.IsNullOrWhiteSpace(order?.ContactName))
            meta.Add(("Customer", order!.ContactName!));

        var lines = txn.Lines.OrderBy(l => l.LineNumber).Select((l, i) => new PdfLineModel(
            i + 1,
            l.ProductName + (string.IsNullOrWhiteSpace(l.VariantName) ? "" : $" ({l.VariantName})"),
            Qty(l.Quantity), Money(l.UnitPrice),
            l.DiscountAmount > 0 ? "-" + Money(l.DiscountAmount) : "-",
            Money(l.TaxAmount), Money(l.LineAmount))).ToList();

        var showDiscount = template?.ShowDiscountLine ?? true;
        var totals = new List<(string, string, bool)> { ("Subtotal", Money(txn.SubtotalAmount), false) };
        if (showDiscount && txn.DiscountAmount > 0) totals.Add(("Discount", "-" + Money(txn.DiscountAmount), false));
        if ((template?.ShowTaxBreakdown ?? true) && txn.TaxAmount > 0) totals.Add(("Tax", Money(txn.TaxAmount), false));
        totals.Add(("Total", $"{currency} {Money(txn.TotalAmount)}".Trim(), true));
        foreach (var p in txn.Payments) totals.Add((p.TenderType.ToString(), Money(p.Amount), false));
        totals.Add(("Tendered", Money(txn.TenderedAmount), false));
        totals.Add(("Change", Money(txn.ChangeAmount), false));

        var receiptNo = txn.ReceiptNumber ?? txn.TransactionNumber;
        var (barcode, qr) = BuildCodes(template, receiptNo);

        var model = new PdfDocModel
        {
            Title         = "Sales Receipt",
            SellerName    = template?.HeaderBusinessName ?? store?.TradingName ?? "",
            SellerLines   = SellerLines(template),
            Meta          = meta,
            ShowTaxColumn = false,
            Lines         = lines,
            Totals        = totals,
            FooterMessage = template?.FooterMessage ?? "Thank you for shopping with us!",
            ReturnPolicy  = template?.ReturnPolicy,
            BarcodeText   = (barcode is not null || qr is not null) ? receiptNo : null,
            BarcodeImage  = barcode,
            QrImage       = qr,
        };

        return PdfDocumentRenderer.Render(model);
    }

    public async Task<byte[]?> RenderPdfInvoiceAsync(Guid salesInvoiceId)
    {
        var inv = await _invoices.GetWithLinesAsync(salesInvoiceId);
        if (inv is null) return null;

        var template = await _templates.GetDefaultAsync(inv.BranchId);
        var currency = inv.CurrencyCode;

        var billTo = new[] { inv.BillToName ?? inv.ContactName, inv.BillToStreet,
                string.Join(", ", new[] { inv.BillToCity, inv.BillToState, inv.BillToPostalCode }
                    .Where(s => !string.IsNullOrWhiteSpace(s))), inv.BillToCountry }
            .Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!).ToList();

        var lines = inv.Lines.OrderBy(l => l.LineNumber).Select((l, i) => new PdfLineModel(
            i + 1, l.ProductName,
            Qty(l.Quantity), Money(l.UnitPrice),
            l.DiscountAmount > 0 ? "-" + Money(l.DiscountAmount) : "-",
            Money(l.TaxAmount), Money(l.TotalAmount))).ToList();

        var totals = new List<(string, string, bool)> { ("Subtotal", Money(inv.SubtotalAmount), false) };
        if (inv.DiscountAmount > 0) totals.Add(("Discount", "-" + Money(inv.DiscountAmount), false));
        if (inv.TaxAmount > 0)      totals.Add(("Tax", Money(inv.TaxAmount), false));
        if (inv.ShippingAmount > 0) totals.Add(("Shipping", Money(inv.ShippingAmount), false));
        totals.Add(("Total", $"{currency} {Money(inv.TotalAmount)}".Trim(), true));
        if (inv.PaidAmount > 0)     totals.Add(("Paid", Money(inv.PaidAmount), false));
        totals.Add(("Balance Due", $"{currency} {Money(inv.BalanceDue)}".Trim(), true));

        var (barcode, qr) = BuildCodes(template, inv.InvoiceNumber);

        var model = new PdfDocModel
        {
            Title         = "Invoice",
            SellerName    = template?.HeaderBusinessName ?? "",
            SellerLines   = SellerLines(template),
            Meta          =
            [
                ("Invoice #", inv.InvoiceNumber),
                ("Invoice Date", inv.InvoiceDate.ToString("yyyy-MM-dd")),
                ("Due Date", inv.DueDate.ToString("yyyy-MM-dd")),
                ("Status", inv.Status.ToString()),
            ],
            BillToTitle   = "BILL TO",
            BillToLines   = billTo,
            ShowTaxColumn = true,
            Lines         = lines,
            Totals        = totals,
            FooterMessage = template?.FooterMessage
                            ?? (string.IsNullOrWhiteSpace(inv.PaymentReference) ? "" : $"Payment reference: {inv.PaymentReference}"),
            ReturnPolicy  = template?.ReturnPolicy,
            BarcodeText   = (barcode is not null || qr is not null) ? inv.InvoiceNumber : null,
            BarcodeImage  = barcode,
            QrImage       = qr,
        };

        return PdfDocumentRenderer.Render(model);
    }

    private static List<string> SellerLines(PosReceiptTemplate? t)
    {
        var lines = new List<string>();
        if (t is null) return lines;
        if (!string.IsNullOrWhiteSpace(t.HeaderAddressLine1)) lines.Add(t.HeaderAddressLine1!);
        if (!string.IsNullOrWhiteSpace(t.HeaderAddressLine2)) lines.Add(t.HeaderAddressLine2!);
        var contact = new[] { string.IsNullOrWhiteSpace(t.HeaderPhone) ? null : "Tel: " + t.HeaderPhone, t.HeaderEmail }
            .Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s!);
        if (contact.Any()) lines.Add(string.Join("  •  ", contact));
        if (!string.IsNullOrWhiteSpace(t.TaxRegistrationNumber)) lines.Add("Tax #: " + t.TaxRegistrationNumber);
        return lines;
    }

    // ── HTML building blocks ───────────────────────────────────────────────────

    private static string HtmlHeader(PosReceiptTemplate? t, PosStore? store, string title,
        IReadOnlyList<(string Label, string Value)> metaRows, string? resolvedLogoUrl = null)
    {
        var businessName = t?.HeaderBusinessName ?? store?.TradingName ?? "";
        var sb = new StringBuilder();
        sb.Append("<div class=\"hdr\"><div>");
        if (!string.IsNullOrWhiteSpace(resolvedLogoUrl))
            sb.Append($"<img class=\"logo\" src=\"{resolvedLogoUrl}\" alt=\"logo\"/>");
        if (!string.IsNullOrWhiteSpace(businessName)) sb.Append($"<div class=\"biz\">{E(businessName)}</div>");
        var addr = new[] { t?.HeaderAddressLine1, t?.HeaderAddressLine2 }.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => E(s!));
        if (addr.Any()) sb.Append($"<div class=\"muted\">{string.Join("<br>", addr)}</div>");
        var contact = new[]
        {
            string.IsNullOrWhiteSpace(t?.HeaderPhone) ? null : "Tel: " + t!.HeaderPhone,
            t?.HeaderEmail,
        }.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => E(s!));
        if (contact.Any()) sb.Append($"<div class=\"muted\">{string.Join(" &bull; ", contact)}</div>");
        if (!string.IsNullOrWhiteSpace(t?.TaxRegistrationNumber)) sb.Append($"<div class=\"muted\">Tax #: {E(t!.TaxRegistrationNumber!)}</div>");
        sb.Append("</div><div class=\"head-right\">");
        sb.Append($"<div class=\"title\">{E(title)}</div>");
        foreach (var (label, value) in metaRows)
            sb.Append($"<div class=\"muted\"><span>{E(label)}:</span> {E(value)}</div>");
        sb.Append("</div></div>");
        return sb.ToString();
    }

    private static string HtmlTotals(IReadOnlyList<(string Label, string Value, bool Grand)> rows)
    {
        var sb = new StringBuilder("<table class=\"totals\"><tbody>");
        foreach (var (label, value, grand) in rows)
            sb.Append($"<tr class=\"{(grand ? "grand" : "")}\"><td>{E(label)}</td><td class=\"r\">{E(value)}</td></tr>");
        sb.Append("</tbody></table>");
        return sb.ToString();
    }

    private static string HtmlFooter(PosReceiptTemplate? t, string? codeValue, byte[]? barcode = null, byte[]? qr = null)
    {
        var sb = new StringBuilder("<div class=\"footer\">");
        sb.Append($"{E(t?.FooterMessage ?? "Thank you for shopping with us!")}");
        if (!string.IsNullOrWhiteSpace(t?.ReturnPolicy)) sb.Append($"<br>{E(t!.ReturnPolicy!)}");
        if (barcode is not null)
            sb.Append($"<div class=\"code\"><img alt=\"barcode\" src=\"data:image/png;base64,{Convert.ToBase64String(barcode)}\"/></div>");
        if (qr is not null)
            sb.Append($"<div class=\"code\"><img alt=\"qr\" style=\"height:90px\" src=\"data:image/png;base64,{Convert.ToBase64String(qr)}\"/></div>");
        if (!string.IsNullOrWhiteSpace(codeValue))
            sb.Append($"<div class=\"barcode\">{(barcode is not null || qr is not null ? E(codeValue!) : "*" + E(codeValue!) + "*")}</div>");
        sb.Append("</div>");
        return sb.ToString();
    }

    private static string HtmlDocument(string title, string body) =>
        $"<!DOCTYPE html><html><head><meta charset=\"utf-8\"/><title>{E(title)}</title><style>{Css}</style></head>"
      + $"<body><div class=\"doc\">{body}</div></body></html>";

    private const string Css =
        "body{font-family:Arial,Helvetica,sans-serif;color:#222;margin:0;padding:24px;font-size:13px}" +
        ".doc{max-width:800px;margin:0 auto}" +
        ".hdr{display:flex;justify-content:space-between;align-items:flex-start;border-bottom:2px solid #333;padding-bottom:12px;margin-bottom:8px}" +
        ".hdr .biz{font-size:18px;font-weight:bold}" +
        ".hdr .logo{max-height:64px;margin-bottom:6px}" +
        ".head-right{text-align:right}" +
        ".muted{color:#666;font-size:12px}" +
        ".title{font-size:22px;font-weight:bold;text-transform:uppercase;letter-spacing:1px;margin-bottom:4px}" +
        ".meta{margin:12px 0}" +
        "table{width:100%;border-collapse:collapse;margin-top:8px}" +
        "th,td{padding:6px 8px;text-align:left}" +
        "thead th{border-bottom:1px solid #333;font-size:12px;text-transform:uppercase}" +
        "tbody td{border-bottom:1px solid #eee}" +
        ".r{text-align:right}" +
        ".totals{margin-top:12px;margin-left:auto;width:300px}" +
        ".totals td{padding:4px 8px;border:none}" +
        ".totals .grand td{font-weight:bold;font-size:15px;border-top:2px solid #333}" +
        ".footer{margin-top:24px;border-top:1px solid #ddd;padding-top:12px;color:#555;font-size:12px;text-align:center}" +
        ".code{margin-top:8px}.code img{height:48px;max-width:100%}" +
        ".barcode{margin-top:4px;font-size:12px;letter-spacing:1px}" +
        "@media print{body{padding:0}.doc{max-width:none}}";

    private static string VariantSuffix(string? variant) =>
        string.IsNullOrWhiteSpace(variant) ? "" : $" <span class=\"muted\">({E(variant)})</span>";

    private static string E(string s) => System.Net.WebUtility.HtmlEncode(s);

    // ── Logo resolution: template custom → company default ────────────────────

    private async Task<string?> ResolveLogoAsync(PosReceiptTemplate? template, Guid companyId)
    {
        if (!string.IsNullOrWhiteSpace(template?.LogoUrl))
            return template.LogoUrl;

        try
        {
            var evt = new CompanyLogoLookupEvent { CompanyId = companyId };
            await _events.PublishAsync(evt);
            var bytes = await evt.Result.Task.WaitAsync(TimeSpan.FromSeconds(5));
            if (bytes is { Length: > 0 })
                return $"data:{DetectMimeType(bytes)};base64,{Convert.ToBase64String(bytes)}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not resolve company logo for {CompanyId}", companyId);
        }

        return null;
    }

    private static string DetectMimeType(byte[] b)
    {
        if (b.Length >= 2 && b[0] == 0x89 && b[1] == 0x50) return "image/png";
        if (b.Length >= 2 && b[0] == 0xFF && b[1] == 0xD8) return "image/jpeg";
        if (b.Length >= 4 && b[0] == 0x52 && b[1] == 0x49) return "image/webp";
        return "image/png";
    }

    // ── Template resolution: terminal → store → branch default ─────────────────

    private async Task<PosReceiptTemplate?> ResolveTemplateAsync(PosTransaction txn)
    {
        try
        {
            var terminal = await _terminals.GetByIdAsync(txn.PosTerminalId);
            if (terminal?.ReceiptTemplateId is Guid tId)
            {
                var t = await _templates.GetByIdAsync(tId);
                if (t is not null) return t;
            }

            var store = await _stores.GetByIdAsync(txn.PosStoreId);
            if (store?.ReceiptTemplateId is Guid sId)
            {
                var t = await _templates.GetByIdAsync(sId);
                if (t is not null) return t;
            }

            return await _templates.GetDefaultAsync(txn.BranchId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Receipt template resolution failed for txn {TxnId} — using defaults", txn.Id);
            return null;
        }
    }

    // ── Text helpers ───────────────────────────────────────────────────────────

    private static string Normalize(string paper) =>
        paper.Equals("Thermal58mm", StringComparison.OrdinalIgnoreCase) ? "Thermal58mm" : "Thermal80mm";

    private static string Money(decimal d) => d.ToString("N2");
    private static string Qty(decimal q)   => q.ToString("0.##");

    private static void Rule(StringBuilder sb, int width) => sb.AppendLine(new string('-', width));

    private static void AppendCentered(StringBuilder sb, string text, int width, bool upper = false)
    {
        foreach (var part in Wrap(upper ? text.ToUpperInvariant() : text, width))
        {
            var pad = Math.Max(0, (width - part.Length) / 2);
            sb.AppendLine(new string(' ', pad) + part);
        }
    }

    private static void AppendCenteredIf(StringBuilder sb, string? text, int width)
    {
        if (!string.IsNullOrWhiteSpace(text)) AppendCentered(sb, text!, width);
    }

    /// <summary>Left text and right text on one line; left is truncated if the two would collide.</summary>
    private static void AppendLeftRight(StringBuilder sb, string left, string right, int width)
    {
        right ??= string.Empty;
        var maxLeft = Math.Max(0, width - right.Length - 1);
        if (left.Length > maxLeft) left = left[..maxLeft];
        var gap = Math.Max(1, width - left.Length - right.Length);
        sb.AppendLine(left + new string(' ', gap) + right);
    }

    private static IEnumerable<string> Wrap(string text, int width)
    {
        if (string.IsNullOrEmpty(text)) yield break;

        foreach (var rawLine in text.Replace("\r", "").Split('\n'))
        {
            var remaining = rawLine.Trim();
            if (remaining.Length == 0) { yield return string.Empty; continue; }

            while (remaining.Length > width)
            {
                var cut = remaining.LastIndexOf(' ', Math.Min(width - 1, remaining.Length - 1));
                if (cut <= 0) cut = width;            // single long word — hard split
                yield return remaining[..cut].TrimEnd();
                remaining = remaining[cut..].TrimStart();
            }
            yield return remaining;
        }
    }
}
