using Nexcore.SharedKernel;

namespace Accounting.Domain.Entities;

/// <summary>
/// Classifies chart-of-accounts entries by their financial statement type (Asset, Liability,
/// Equity, Revenue, Expense) and the normal balance side that applies to accounts under it.
/// </summary>
public class AccountCategory : BaseEntity
{
    public required string Name { get; set; }

    /// <summary>Statement classification that drives which accounts roll up where.</summary>
    public required AccountType Type { get; set; }

    /// <summary>Debit for Assets/Expenses, Credit for Liabilities/Equity/Revenue.</summary>
    public required NormalBalance NormalBalance { get; set; }

    public ICollection<LedgerAccount> Accounts { get; set; } = [];
}
