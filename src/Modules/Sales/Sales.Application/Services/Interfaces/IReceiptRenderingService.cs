using Sales.Application.DTOs;

namespace Sales.Application.Services.Interfaces;

/// <summary>
/// Renders POS receipts. v1 produces a thermal (58mm/80mm) plain-text receipt for a POS transaction,
/// using the store/terminal's configured <c>PosReceiptTemplate</c> when one exists (else sensible defaults).
/// </summary>
public interface IReceiptRenderingService
{
    /// <summary>
    /// Render the thermal receipt for a completed POS transaction.
    /// Returns null if the transaction does not exist.
    /// <paramref name="paperSizeOverride"/> ("Thermal58mm"/"Thermal80mm") overrides the template width.
    /// </summary>
    Task<ThermalReceiptDto?> RenderThermalAsync(Guid posTransactionId, string? paperSizeOverride = null);

    /// <summary>
    /// Render a full A4 HTML receipt (header/logo + items + tender/change) for a POS transaction.
    /// Returns null if the transaction does not exist. Self-contained HTML (inline CSS, print-friendly).
    /// </summary>
    Task<string?> RenderHtmlReceiptAsync(Guid posTransactionId);

    /// <summary>
    /// Render a full A4 HTML invoice document (header/logo + bill-to + items + tax + balance due)
    /// for a Sales Invoice. Returns null if the invoice does not exist.
    /// </summary>
    Task<string?> RenderHtmlInvoiceAsync(Guid salesInvoiceId);

    /// <summary>Render the A4 PDF receipt for a POS transaction. Returns null if it does not exist.</summary>
    Task<byte[]?> RenderPdfReceiptAsync(Guid posTransactionId);

    /// <summary>Render the A4 PDF invoice document for a Sales Invoice. Returns null if it does not exist.</summary>
    Task<byte[]?> RenderPdfInvoiceAsync(Guid salesInvoiceId);
}
