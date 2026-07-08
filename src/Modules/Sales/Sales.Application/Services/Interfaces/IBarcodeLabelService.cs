using Sales.Application.DTOs;

namespace Sales.Application.Services.Interfaces;

/// <summary>
/// Renders product barcode labels (price tags / shelf labels) to PDF using a
/// <c>PosBarcodeLabelTemplate</c> for size and content, with a real barcode image embedded.
/// </summary>
public interface IBarcodeLabelService
{
    /// <summary>
    /// Render one or more copies of a single product's label to a PDF (one label per page).
    /// Returns null if an explicit TemplateId was supplied but not found.
    /// </summary>
    Task<byte[]?> RenderLabelPdfAsync(RenderBarcodeLabelDto dto);

    /// <summary>
    /// Render labels for several products in one PDF (each product × its Copies, one label per page).
    /// Returns null if an explicit TemplateId was supplied but not found.
    /// </summary>
    Task<byte[]?> RenderBatchPdfAsync(RenderBarcodeLabelBatchDto dto);
}
