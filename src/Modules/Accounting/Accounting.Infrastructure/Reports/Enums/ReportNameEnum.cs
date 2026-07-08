namespace Accounting.Infrastructure.Reports.Enums;

/// <summary>
/// Enumeration of all available financial reports
/// Used to eliminate hardcoded report names and provide type safety
/// </summary>
public enum ReportName
{
    /// <summary>
    /// Trial Balance - Foundation of accounting (Debit = Credit validation)
    /// </summary>
    TrialBalance = 1,

    /// <summary>
    /// General Ledger - All transactions for all accounts with running balance
    /// </summary>
    GeneralLedger = 2,

    /// <summary>
    /// Account Ledger - Single account statement with detailed transactions
    /// </summary>
    AccountLedger = 3,

    /// <summary>
    /// Profit & Loss - Business performance (Revenue - Expenses = Profit)
    /// </summary>
    ProfitAndLoss = 4,

    /// <summary>
    /// Balance Sheet - Financial position at a date (Assets = Liabilities + Equity)
    /// </summary>
    BalanceSheet = 5,

    /// <summary>
    /// Journal Report - All journal entries in a period
    /// </summary>
    Journal = 6,

    /// <summary>
    /// Journal Audit Report - Who created/modified/posted journal entries (compliance)
    /// </summary>
    JournalAudit = 7,

    /// <summary>
    /// Accounts Receivable Aging - Customer payment aging buckets (0-30, 30-60, 60-90, 90+)
    /// </summary>
    AccountsReceivableAging = 8,

    /// <summary>
    /// Accounts Payable Aging - Vendor payment aging buckets
    /// </summary>
    AccountsPayableAging = 9,

    /// <summary>
    /// Customer Statement - All transactions for a specific customer
    /// </summary>
    CustomerStatement = 10,

    /// <summary>
    /// Vendor Statement - All transactions for a specific vendor
    /// </summary>
    VendorStatement = 11,

    /// <summary>
    /// Tax Summary - Tax aggregation by tax code
    /// </summary>
    TaxSummary = 12,

    /// <summary>
    /// Cash Flow Statement - Operating, Investing, Financing activities
    /// </summary>
    CashFlow = 13,

    /// <summary>
    /// Unposted Journals - Draft entries awaiting posting
    /// </summary>
    UnpostedJournals = 14,

    /// <summary>
    /// Trial Balance by Dimension - Department/Branch-wise trial balance
    /// </summary>
    TrialBalanceByDimension = 15,

    /// <summary>
    /// Profit & Loss by Dimension - Department/Branch-wise P&L
    /// </summary>
    ProfitAndLossByDimension = 16,

    /// <summary>
    /// Reconciliation Report - Bank vs Ledger reconciliation
    /// </summary>
    Reconciliation = 17,

    /// <summary>
    /// Daily Transaction Report - All transactions for a specific date
    /// </summary>
    DailyTransactions = 18
}
