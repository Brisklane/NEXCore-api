using Sales.Application.DTOs;
using Sales.Domain.Enums;

namespace Sales.Application.Services.Interfaces;

/// <summary>
/// Generic document numbering service.
/// Produces sequential, formatted numbers for any document type
/// (SalesOrder, Invoice, Quotation, Payment, Delivery, CreditNote …).
///
/// Thread safety: each call to <see cref="GetNextNumberAsync"/> uses a serializable
/// DB transaction to atomically read-and-increment the counter, preventing duplicates
/// even under high concurrency.
///
/// Fallback: when no sequence is configured for a document type, a random
/// short code is generated so the system stays operational.
/// </summary>
public interface IDocumentSequenceService
{
    // ── Number allocation (called at document creation time) ──────────────────

    /// <summary>
    /// Atomically allocate the next formatted number for a document type.
    /// If the counter needs a yearly/monthly reset it is applied first.
    /// Falls back to a random short code when no sequence is configured.
    /// </summary>
    Task<SequenceNumber> GetNextNumberAsync(DocumentType documentType);

    // ── Configuration CRUD ────────────────────────────────────────────────────

    Task<List<DocumentSequenceDto>> GetAllAsync();
    Task<DocumentSequenceDto?> GetByDocumentTypeAsync(DocumentType documentType);
    Task<DocumentSequenceDto> CreateAsync(CreateDocumentSequenceDto dto);
    Task<DocumentSequenceDto> UpdateAsync(Guid id, UpdateDocumentSequenceDto dto);
    Task<DocumentSequenceDto> ResetCounterAsync(Guid id, ResetSequenceDto dto);

    // ── Format preview (no allocation) ────────────────────────────────────────

    /// <summary>
    /// Preview what a number would look like with the given format settings
    /// without allocating or changing any counter.
    /// </summary>
    PreviewSequenceFormatResultDto PreviewFormat(PreviewSequenceFormatDto dto);
}
