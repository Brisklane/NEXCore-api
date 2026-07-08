using Nexcore.SharedKernel;
using Nexcore.SharedKernel.ValueObjects;

namespace Accounting.Domain.Entities;

/// <summary>
/// Ledger entity - represents a company's general ledger
/// Top-level container for all accounting entries
/// </summary>
public class Ledger : BaseEntity
{
    /// <summary>
    /// Ledger name (e.g., "Main Ledger", "Subsidiary Ledger")
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Base currency code for this ledger
    /// </summary>
    public required string BaseCurrencyCode { get; set; }

    /// <summary>
    /// Fiscal calendar reference
    /// </summary>
    public Guid FiscalCalendarId { get; set; }

    /// <summary>
    /// Indicates if this is the default ledger for the company
    /// </summary>
    public bool IsDefault { get; set; }

    // Navigation properties
    public ICollection<LedgerAccount> Accounts { get; set; } = [];
    public ICollection<JournalEntry> JournalEntries { get; set; } = [];
}
