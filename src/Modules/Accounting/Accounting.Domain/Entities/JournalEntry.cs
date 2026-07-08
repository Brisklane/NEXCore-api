using Nexcore.SharedKernel;

namespace Accounting.Domain.Entities;

/// <summary>
/// Journal entry - header for a set of balanced debit/credit transactions
/// </summary>
public class JournalEntry : BaseEntity
{
    /// <summary>
    /// Ledger reference
    /// </summary>
    public Guid LedgerId { get; set; }

    /// <summary>
    /// Journal entry number (unique per company)
    /// </summary>
    public required string JournalNumber { get; set; }

    /// <summary>
    /// Reference number (e.g., invoice number, receipt number)
    /// </summary>
    public string? ReferenceNumber { get; set; }

    /// <summary>
    /// Type of document (Invoice, Receipt, Transfer, etc.)
    /// </summary>
    public required string DocumentType { get; set; }

    /// <summary>
    /// Date when entry was posted
    /// </summary>
    public DateTime PostingDate { get; set; }

    /// <summary>
    /// Date of the original document
    /// </summary>
    public DateTime DocumentDate { get; set; }

    /// <summary>
    /// Description of the journal entry
    /// </summary>
    public new required string Description { get; set; }

    /// <summary>
    /// Currency code for the entry
    /// </summary>
    public required string CurrencyCode { get; set; }

    /// <summary>
    /// Exchange rate if different from base currency
    /// </summary>
    public decimal ExchangeRate { get; set; } = 1;

    /// <summary>
    /// Total debit amount
    /// </summary>
    public decimal TotalDebit { get; set; }

    /// <summary>
    /// Total credit amount
    /// </summary>
    public decimal TotalCredit { get; set; }

    /// <summary>
    /// Current status of the entry
    /// </summary>
    public required JournalEntryStatus Status { get; set; } = JournalEntryStatus.Draft;

    /// <summary>
    /// User who posted the entry
    /// </summary>
    public Guid? PostedByUserId { get; set; }

    /// <summary>
    /// When the entry was posted
    /// </summary>
    public DateTime? PostedAt { get; set; }

    /// <summary>
    /// Source system that created the entry
    /// </summary>
    public string? SourceSystem { get; set; }

    /// <summary>
    /// Reference to source system entity
    /// </summary>
    public string? SourceReferenceId { get; set; }

    /// <summary>
    /// If this is a reversal, reference to original entry
    /// </summary>
    public Guid? ReversalOfJournalId { get; set; }

    // Navigation properties
    public Ledger? Ledger { get; set; }
    public ICollection<JournalLine> Lines { get; set; } = [];
    public ICollection<JournalAudit> AuditTrail { get; set; } = [];
}
