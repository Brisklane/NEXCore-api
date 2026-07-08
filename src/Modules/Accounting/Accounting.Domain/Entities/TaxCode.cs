using Nexcore.SharedKernel;

namespace Accounting.Domain.Entities;

/// <summary>
/// Tax code - tax configuration and ledger account mapping
/// </summary>
public class TaxCode : BaseEntity
{
    /// <summary>
    /// Tax code value (e.g., "VAT20", "INCOME")
    /// </summary>
    public new required string Code { get; set; }

    /// <summary>
    /// Tax description
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Tax percentage
    /// </summary>
    public decimal Percentage { get; set; }

    /// <summary>
    /// Ledger account for tax entries
    /// </summary>
    public Guid? LedgerAccountId { get; set; }

    /// <summary>
    /// Indicates if tax is recoverable
    /// </summary>
    public bool IsRecoverable { get; set; }

    // Navigation properties
    public LedgerAccount? LedgerAccount { get; set; }
}
