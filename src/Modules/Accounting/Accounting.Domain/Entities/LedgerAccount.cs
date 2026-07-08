using Nexcore.SharedKernel;
using Nexcore.SharedKernel.Enums;

namespace Accounting.Domain.Entities;

/// <summary>
/// Ledger account - individual accounts in the chart of accounts
/// </summary>
public class LedgerAccount : BaseEntity
{
    /// <summary>
    /// Ledger reference
    /// </summary>
    public Guid LedgerId { get; set; }

    /// <summary>
    /// Account number (unique per company)
    /// </summary>
    public required string AccountNumber { get; set; }

    /// <summary>
    /// Account name
    /// </summary>
    public required string AccountName { get; set; }

    /// <summary>
    /// Account category reference
    /// </summary>
    public Guid CategoryId { get; set; }

    /// <summary>
    /// Parent account for hierarchical structure
    /// </summary>
    public Guid? ParentAccountId { get; set; }

    /// <summary>
    /// Indicates if account can have journal entries posted
    /// </summary>
    public bool IsPostingAllowed { get; set; } = true;

    /// <summary>
    /// Indicates if account is a control account
    /// </summary>
    public bool IsControlAccount { get; set; }

    /// <summary>
    /// Specific currency for account (if different from ledger)
    /// </summary>
    public string? CurrencyCode { get; set; }

    /// <summary>
    /// Allows manual entry flag
    /// </summary>
    public bool AllowManualEntry { get; set; } = true;

    /// <summary>
    /// Indicates if this is a subledger account (detail) vs GL account (master)
    /// True = Subledger (e.g., Customer 001 within AR master account)
    /// False = GL Account (e.g., 1200 - Accounts Receivable master)
    /// </summary>
    public bool IsSubledgerAccount { get; set; }

    /// <summary>
    /// Type of subledger (if IsSubledgerAccount = true)
    /// Examples: "AR", "AP", "Inventory", "CostCenter"
    /// </summary>
    public SubledgerType? SubledgerType { get; set; }

    /// <summary>
    /// Reference to master GL account (if this is a subledger)
    /// Links detail subledger account to parent GL master account
    /// </summary>
    public Guid? SubledgerMasterAccountId { get; set; }

    // Navigation properties
    public Ledger? Ledger { get; set; }
    public AccountCategory? Category { get; set; }
    public LedgerAccount? ParentAccount { get; set; }
    public ICollection<LedgerAccount> ChildAccounts { get; set; } = [];
    public ICollection<JournalLine> JournalLines { get; set; } = [];
    public ICollection<AccountBalance> Balances { get; set; } = [];

    /// <summary>
    /// Navigation to master GL account (for subledger accounts)
    /// </summary>
    public LedgerAccount? SubledgerMasterAccount { get; set; }

    /// <summary>
    /// Navigation to child subledger accounts (for GL master accounts)
    /// </summary>
    public ICollection<LedgerAccount> SubledgerAccounts { get; set; } = [];
}
