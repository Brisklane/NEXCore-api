using Nexcore.SharedKernel;

namespace Accounting.Domain.Entities;

/// <summary>
/// Posting profile - rules for automatic account posting
/// </summary>
public class PostingProfile : BaseEntity
{
    /// <summary>
    /// Module name (e.g., "Sales", "POS", "School")
    /// </summary>
    public required string ModuleName { get; set; }

    /// <summary>
    /// Transaction type (e.g., "Invoice", "Payment")
    /// </summary>
    public required string TransactionType { get; set; }

    /// <summary>
    /// Debit account for this transaction
    /// </summary>
    public Guid DebitAccountId { get; set; }

    /// <summary>
    /// Credit account for this transaction
    /// </summary>
    public Guid CreditAccountId { get; set; }

    /// <summary>
    /// Tax account (optional)
    /// </summary>
    public Guid? TaxAccountId { get; set; }

    // Navigation properties
    public LedgerAccount? DebitAccount { get; set; }
    public LedgerAccount? CreditAccount { get; set; }
    public LedgerAccount? TaxAccount { get; set; }
}
