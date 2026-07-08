namespace Accounting.Infrastructure.Reports.Enums;

/// <summary>
/// Enumeration of account types used in financial reports
/// Replaces hardcoded strings like "Revenue", "Expense", "Asset", etc.
/// </summary>
public enum AccountTypeFilter
{
    /// <summary>
    /// Asset account - items of value owned by the company
    /// </summary>
    Asset = 1,

    /// <summary>
    /// Liability account - debts and obligations
    /// </summary>
    Liability = 2,

    /// <summary>
    /// Equity account - owner's stake in the company
    /// </summary>
    Equity = 3,

    /// <summary>
    /// Revenue account - income from operations
    /// </summary>
    Revenue = 4,

    /// <summary>
    /// Expense account - costs of operations
    /// </summary>
    Expense = 5
}
