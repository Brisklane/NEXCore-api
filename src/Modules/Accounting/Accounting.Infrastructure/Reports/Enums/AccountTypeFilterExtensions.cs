namespace Accounting.Infrastructure.Reports.Enums;

/// <summary>
/// Extension methods for AccountTypeFilter enum
/// Provides conversion between enum and string representations
/// </summary>
public static class AccountTypeFilterExtensions
{
    /// <summary>
    /// Check if account type is a balance sheet account (Asset, Liability, Equity)
    /// </summary>
    public static bool IsBalanceSheetAccount(this AccountTypeFilter typeFilter) => typeFilter switch
    {
        AccountTypeFilter.Asset or AccountTypeFilter.Liability or AccountTypeFilter.Equity => true,
        _ => false
    };

    /// <summary>
    /// Check if account type is an income statement account (Revenue, Expense)
    /// </summary>
    public static bool IsIncomeStatementAccount(this AccountTypeFilter typeFilter) => typeFilter switch
    {
        AccountTypeFilter.Revenue or AccountTypeFilter.Expense => true,
        _ => false
    };

    /// <summary>
    /// Get the string representation of the account type
    /// Used for comparison with AccountCategory.Type.ToString()
    /// </summary>
    public static string GetStringValue(this AccountTypeFilter typeFilter) => typeFilter switch
    {
        AccountTypeFilter.Asset => "Asset",
        AccountTypeFilter.Liability => "Liability",
        AccountTypeFilter.Equity => "Equity",
        AccountTypeFilter.Revenue => "Revenue",
        AccountTypeFilter.Expense => "Expense",
        _ => "Unknown"
    };

    /// <summary>
    /// Get display name for the account type
    /// </summary>
    public static string GetDisplayName(this AccountTypeFilter typeFilter) => typeFilter switch
    {
        AccountTypeFilter.Asset => "Asset Accounts",
        AccountTypeFilter.Liability => "Liability Accounts",
        AccountTypeFilter.Equity => "Equity Accounts",
        AccountTypeFilter.Revenue => "Revenue Accounts",
        AccountTypeFilter.Expense => "Expense Accounts",
        _ => "Unknown Accounts"
    };

    /// <summary>
    /// Check if a type string matches this account type
    /// </summary>
    public static bool Matches(this AccountTypeFilter typeFilter, string? typeString) =>
        typeFilter.GetStringValue().Equals(typeString, System.StringComparison.OrdinalIgnoreCase);
}
