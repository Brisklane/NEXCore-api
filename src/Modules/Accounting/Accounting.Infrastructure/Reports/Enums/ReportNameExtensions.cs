using System;

namespace Accounting.Infrastructure.Reports.Enums;

/// <summary>
/// Extension methods for ReportName enum
/// Provides human-readable display names and descriptions
/// </summary>
public static class ReportNameExtensions
{
    /// <summary>
    /// Get display name for a report type
    /// </summary>
    public static string GetDisplayName(this ReportName reportName) => reportName switch
    {
        ReportName.TrialBalance => "Trial Balance",
        ReportName.GeneralLedger => "General Ledger",
        ReportName.AccountLedger => "Account Ledger",
        ReportName.ProfitAndLoss => "Profit & Loss",
        ReportName.BalanceSheet => "Balance Sheet",
        ReportName.Journal => "Journal",
        ReportName.JournalAudit => "Journal Audit",
        ReportName.AccountsReceivableAging => "Accounts Receivable Aging",
        ReportName.AccountsPayableAging => "Accounts Payable Aging",
        ReportName.CustomerStatement => "Customer Statement",
        ReportName.VendorStatement => "Vendor Statement",
        ReportName.TaxSummary => "Tax Summary",
        ReportName.CashFlow => "Cash Flow",
        ReportName.UnpostedJournals => "Unposted Journals",
        ReportName.TrialBalanceByDimension => "Trial Balance by Dimension",
        ReportName.ProfitAndLossByDimension => "P&L by Dimension",
        ReportName.Reconciliation => "Reconciliation",
        ReportName.DailyTransactions => "Daily Transactions",
        _ => "Unknown Report"
    };

    /// <summary>
    /// Get description for a report type
    /// </summary>
    public static string GetDescription(this ReportName reportName) => reportName switch
    {
        ReportName.TrialBalance => "Foundation of accounting - validates Debit = Credit",
        ReportName.GeneralLedger => "All transactions for all accounts with running balance",
        ReportName.AccountLedger => "Single account statement with detailed transactions",
        ReportName.ProfitAndLoss => "Business performance - Revenue minus Expenses",
        ReportName.BalanceSheet => "Financial position - Assets = Liabilities + Equity",
        ReportName.Journal => "All journal entries in a period",
        ReportName.JournalAudit => "Compliance tracking - who changed what",
        ReportName.AccountsReceivableAging => "Customer payment aging analysis",
        ReportName.AccountsPayableAging => "Vendor payment aging analysis",
        ReportName.CustomerStatement => "All transactions for a customer",
        ReportName.VendorStatement => "All transactions for a vendor",
        ReportName.TaxSummary => "Tax aggregation by tax code",
        ReportName.CashFlow => "Movement of cash by activity type",
        ReportName.UnpostedJournals => "Draft entries awaiting posting",
        ReportName.TrialBalanceByDimension => "Trial balance by department or branch",
        ReportName.ProfitAndLossByDimension => "P&L by department or branch",
        ReportName.Reconciliation => "Bank vs Ledger reconciliation",
        ReportName.DailyTransactions => "All transactions for a specific date",
        _ => "No description available"
    };
}
