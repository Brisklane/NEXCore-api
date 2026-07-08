using Nexcore.SharedKernel;

namespace Accounting.Domain.Entities;

/// <summary>
/// Account balance - snapshot of account balance for a fiscal period
/// </summary>
public class AccountBalance : BaseEntity
{
    /// <summary>
    /// Ledger account reference
    /// </summary>
    public Guid LedgerAccountId { get; set; }

    /// <summary>
    /// Fiscal period reference
    /// </summary>
    public Guid FiscalPeriodId { get; set; }

    /// <summary>
    /// Opening balance for the period
    /// </summary>
    public decimal OpeningBalance { get; set; }

    /// <summary>
    /// Total debits in the period
    /// </summary>
    public decimal DebitTotal { get; set; }

    /// <summary>
    /// Total credits in the period
    /// </summary>
    public decimal CreditTotal { get; set; }

    /// <summary>
    /// Closing balance for the period
    /// </summary>
    public decimal ClosingBalance { get; set; }

    /// <summary>
    /// Balance type (e.g., "Actual", "Budget")
    /// </summary>
    public string BalanceType { get; set; } = "Actual";

    // Navigation properties
    public LedgerAccount? LedgerAccount { get; set; }
}
