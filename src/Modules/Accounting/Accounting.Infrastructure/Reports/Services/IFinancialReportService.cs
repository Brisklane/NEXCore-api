using Accounting.Infrastructure.Reports.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Accounting.Infrastructure.Reports.Services;

/// <summary>
/// Core financial reporting service
/// All reports derive from JournalEntry, JournalLine, and LedgerAccount
/// </summary>
public interface IFinancialReportService
{
    // Phase 1: Core Reports (MUST HAVE)
    
    /// <summary>
    /// Trial Balance - Foundation of all accounting
    /// Debit = Credit validation
    /// </summary>
    Task<TrialBalanceReportDto> GetTrialBalanceAsync(Guid ledgerId, DateTime reportDate);
    
    /// <summary>
    /// General Ledger - All transactions for all accounts
    /// </summary>
    Task<GeneralLedgerReportDto> GetGeneralLedgerAsync(Guid ledgerId, DateTime fromDate, DateTime toDate);
    
    /// <summary>
    /// Account Ledger (Statement) - Single account transactions
    /// </summary>
    Task<AccountLedgerReportDto> GetAccountLedgerAsync(Guid accountId, DateTime fromDate, DateTime toDate);
    
    /// <summary>
    /// Profit & Loss Statement (Income Statement)
    /// Revenue - Expenses = Profit
    /// </summary>
    Task<ProfitAndLossReportDto> GetProfitAndLossAsync(Guid ledgerId, DateTime fromDate, DateTime toDate);
    
    /// <summary>
    /// Balance Sheet - Financial position at a date
    /// Assets = Liabilities + Equity
    /// </summary>
    Task<BalanceSheetReportDto> GetBalanceSheetAsync(Guid ledgerId, DateTime reportDate);
    
    // Phase 2: Ledger & Transaction Reports
    
    /// <summary>
    /// Journal Report - All journal entries
    /// </summary>
    Task<JournalReportDto> GetJournalReportAsync(Guid ledgerId, DateTime fromDate, DateTime toDate);
    
    /// <summary>
    /// Journal Audit Report - Who changed what
    /// Compliance required
    /// </summary>
    Task<JournalAuditReportDto> GetJournalAuditReportAsync(Guid ledgerId, DateTime fromDate, DateTime toDate);
    
    // Phase 3: Subledger Reports
    
    /// <summary>
    /// Accounts Receivable Aging - Customer aging buckets
    /// </summary>
    Task<AccountsReceivableAgingReportDto> GetAccountsReceivableAgingAsync(DateTime asOfDate);
    
    /// <summary>
    /// Accounts Payable Aging - Vendor aging buckets
    /// </summary>
    Task<AccountsPayableAgingReportDto> GetAccountsPayableAgingAsync(DateTime asOfDate);
    
    /// <summary>
    /// Customer Statement - All transactions per customer
    /// </summary>
    Task<CustomerStatementReportDto> GetCustomerStatementAsync(string customerCode, DateTime fromDate, DateTime toDate);
    
    /// <summary>
    /// Vendor Statement - All transactions per vendor
    /// </summary>
    Task<VendorStatementReportDto> GetVendorStatementAsync(string vendorCode, DateTime fromDate, DateTime toDate);
    
    // Phase 4: Tax & Compliance
    
    /// <summary>
    /// Tax Summary Report - Tax aggregation
    /// </summary>
    Task<TaxSummaryReportDto> GetTaxSummaryAsync(Guid ledgerId, DateTime fromDate, DateTime toDate);
    
    // Phase 5: Analysis & Audit
    
    /// <summary>
    /// Cash Flow Statement
    /// </summary>
    Task<CashFlowStatementDto> GetCashFlowAsync(Guid ledgerId, DateTime fromDate, DateTime toDate);
    
    /// <summary>
    /// Unposted Journals - Draft entries
    /// </summary>
    Task<UnpostedJournalsReportDto> GetUnpostedJournalsAsync(Guid ledgerId);
    
    /// <summary>
    /// Trial Balance by Dimension
    /// </summary>
    Task<TrialBalanceByDimensionReportDto> GetTrialBalanceByDimensionAsync(Guid ledgerId, Guid dimensionId, DateTime reportDate);
    
    /// <summary>
    /// P&L by Dimension
    /// </summary>
    Task<ProfitAndLossByDimensionReportDto> GetProfitAndLossByDimensionAsync(Guid ledgerId, Guid dimensionId, DateTime fromDate, DateTime toDate);
    
    /// <summary>
    /// Reconciliation Report - Bank vs Ledger
    /// </summary>
    Task<ReconciliationReportDto> GetReconciliationReportAsync(Guid accountId, DateTime asOfDate);
    
    /// <summary>
    /// Daily Transaction Report
    /// </summary>
    Task<DailyTransactionReportDto> GetDailyTransactionReportAsync(Guid ledgerId, DateTime reportDate);
}
