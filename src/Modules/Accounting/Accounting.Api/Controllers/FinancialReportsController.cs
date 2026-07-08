using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nexcore.SharedKernel.Api;
using Accounting.Infrastructure.Reports.Services;
using Accounting.Infrastructure.Reports.DTOs;
using System;
using System.Threading.Tasks;

namespace Accounting.Api.Controllers;

/// <summary>
/// Financial Reports Controller
/// Provides access to all accounting and financial reports
/// All reports derived from JournalEntry, JournalLine, and LedgerAccount
/// </summary>
[ApiController]
[Route("api/v1/reports")]
[Produces("application/json")]
[Authorize]
public class FinancialReportsController : ControllerBase
{
    private readonly IFinancialReportService _reportService;
    private readonly ILogger<FinancialReportsController> _logger;

    public FinancialReportsController(
        IFinancialReportService reportService,
        ILogger<FinancialReportsController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    // ============================================================================
    // PHASE 1: CORE FINANCIAL REPORTS (MUST HAVE)
    // ============================================================================

    /// <summary>
    /// Get Trial Balance Report
    /// Foundation of all accounting - Debit = Credit validation
    /// </summary>
    /// <param name="ledgerId">Ledger ID</param>
    /// <param name="reportDate">Report Date</param>
    [HttpGet("trial-balance")]
    [ProducesResponseType(typeof(ApiResponse<TrialBalanceReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTrialBalance([FromQuery] Guid ledgerId, [FromQuery] DateTime reportDate)
    {
        try
        {
            _logger.LogInformation("Request: Trial Balance for Ledger {LedgerId}, Date: {Date}", ledgerId, reportDate);
            
            var report = await _reportService.GetTrialBalanceAsync(ledgerId, reportDate);
            
            return Ok(new ApiResponse<TrialBalanceReportDto>
            {
                Success = true,
                Message = $"Trial Balance as of {reportDate:yyyy-MM-dd}. Balanced: {report.IsBalanced}",
                Data = report
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Trial Balance");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    /// <summary>
    /// Get General Ledger Report
    /// All transactions for all accounts with running balance
    /// </summary>
    [HttpGet("general-ledger")]
    [ProducesResponseType(typeof(ApiResponse<GeneralLedgerReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetGeneralLedger([FromQuery] Guid ledgerId, [FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
    {
        try
        {
            _logger.LogInformation("Request: General Ledger for Ledger {LedgerId}, Period: {From} to {To}", 
                ledgerId, fromDate, toDate);
            
            var report = await _reportService.GetGeneralLedgerAsync(ledgerId, fromDate, toDate);
            
            return Ok(new ApiResponse<GeneralLedgerReportDto>
            {
                Success = true,
                Message = $"General Ledger for {report.Accounts.Count} accounts",
                Data = report
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting General Ledger");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    /// <summary>
    /// Get Account Ledger (Account Statement)
    /// Single account with all transactions and running balance
    /// </summary>
    [HttpGet("account-ledger/{accountId}")]
    [ProducesResponseType(typeof(ApiResponse<AccountLedgerReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAccountLedger(Guid accountId, [FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
    {
        try
        {
            _logger.LogInformation("Request: Account Ledger for Account {AccountId}, Period: {From} to {To}", 
                accountId, fromDate, toDate);
            
            var report = await _reportService.GetAccountLedgerAsync(accountId, fromDate, toDate);
            
            return Ok(new ApiResponse<AccountLedgerReportDto>
            {
                Success = true,
                Message = $"Account Ledger for {report.AccountName}",
                Data = report
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Account Ledger");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    /// <summary>
    /// Get Profit & Loss Statement (Income Statement)
    /// Revenue - Expenses = Profit
    /// </summary>
    [HttpGet("profit-and-loss")]
    [ProducesResponseType(typeof(ApiResponse<ProfitAndLossReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetProfitAndLoss([FromQuery] Guid ledgerId, [FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
    {
        try
        {
            _logger.LogInformation("Request: P&L for Ledger {LedgerId}, Period: {From} to {To}", 
                ledgerId, fromDate, toDate);
            
            var report = await _reportService.GetProfitAndLossAsync(ledgerId, fromDate, toDate);
            
            return Ok(new ApiResponse<ProfitAndLossReportDto>
            {
                Success = true,
                Message = $"P&L: Revenue {report.TotalRevenue:C}, Expenses {report.TotalExpenses:C}, Net Profit {report.NetProfit:C}",
                Data = report
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting P&L");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    /// <summary>
    /// Get Balance Sheet Report
    /// Financial position at a date: Assets = Liabilities + Equity
    /// </summary>
    [HttpGet("balance-sheet")]
    [ProducesResponseType(typeof(ApiResponse<BalanceSheetReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetBalanceSheet([FromQuery] Guid ledgerId, [FromQuery] DateTime reportDate)
    {
        try
        {
            _logger.LogInformation("Request: Balance Sheet for Ledger {LedgerId}, Date: {Date}", ledgerId, reportDate);
            
            var report = await _reportService.GetBalanceSheetAsync(ledgerId, reportDate);
            
            return Ok(new ApiResponse<BalanceSheetReportDto>
            {
                Success = true,
                Message = $"Balance Sheet as of {reportDate:yyyy-MM-dd}. Balanced: {report.IsBalanced}",
                Data = report
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Balance Sheet");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    // ============================================================================
    // PHASE 2: LEDGER & TRANSACTION REPORTS
    // ============================================================================

    /// <summary>
    /// Get Journal Report
    /// All journal entries in period
    /// </summary>
    [HttpGet("journal")]
    [ProducesResponseType(typeof(ApiResponse<JournalReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetJournal([FromQuery] Guid ledgerId, [FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
    {
        try
        {
            _logger.LogInformation("Request: Journal Report for Ledger {LedgerId}, Period: {From} to {To}", 
                ledgerId, fromDate, toDate);
            
            var report = await _reportService.GetJournalReportAsync(ledgerId, fromDate, toDate);
            
            return Ok(new ApiResponse<JournalReportDto>
            {
                Success = true,
                Message = $"Journal Report: {report.EntryCount} entries",
                Data = report
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Journal Report");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    /// <summary>
    /// Get Journal Audit Report
    /// Who created/modified/posted journal entries (compliance required)
    /// </summary>
    [HttpGet("journal-audit")]
    [Authorize(Policy = "AUDIT_VIEW")]
    [ProducesResponseType(typeof(ApiResponse<JournalAuditReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetJournalAudit([FromQuery] Guid ledgerId, [FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
    {
        try
        {
            _logger.LogInformation("Request: Journal Audit Report for Ledger {LedgerId}, Period: {From} to {To}", 
                ledgerId, fromDate, toDate);
            
            var report = await _reportService.GetJournalAuditReportAsync(ledgerId, fromDate, toDate);
            
            return Ok(new ApiResponse<JournalAuditReportDto>
            {
                Success = true,
                Message = "Journal Audit Report",
                Data = report
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Journal Audit Report");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    // ============================================================================
    // PHASE 3: SUBLEDGER REPORTS
    // ============================================================================

    /// <summary>
    /// Get Accounts Receivable Aging Report
    /// Customer aging buckets (0-30, 30-60, 60-90, 90+)
    /// </summary>
    [HttpGet("ar-aging")]
    [ProducesResponseType(typeof(ApiResponse<AccountsReceivableAgingReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetARAging([FromQuery] DateTime asOfDate)
    {
        try
        {
            _logger.LogInformation("Request: AR Aging Report as of {Date}", asOfDate);
            
            var report = await _reportService.GetAccountsReceivableAgingAsync(asOfDate);
            
            return Ok(new ApiResponse<AccountsReceivableAgingReportDto>
            {
                Success = true,
                Message = $"AR Aging Report as of {asOfDate:yyyy-MM-dd}",
                Data = report
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting AR Aging Report");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    /// <summary>
    /// Get Accounts Payable Aging Report
    /// Vendor aging buckets (0-30, 30-60, 60-90, 90+)
    /// </summary>
    [HttpGet("ap-aging")]
    [ProducesResponseType(typeof(ApiResponse<AccountsPayableAgingReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAPAging([FromQuery] DateTime asOfDate)
    {
        try
        {
            _logger.LogInformation("Request: AP Aging Report as of {Date}", asOfDate);
            
            var report = await _reportService.GetAccountsPayableAgingAsync(asOfDate);
            
            return Ok(new ApiResponse<AccountsPayableAgingReportDto>
            {
                Success = true,
                Message = $"AP Aging Report as of {asOfDate:yyyy-MM-dd}",
                Data = report
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting AP Aging Report");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    /// <summary>
    /// Get Customer Statement Report
    /// All transactions for a customer
    /// </summary>
    [HttpGet("customer-statement/{customerCode}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerStatementReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCustomerStatement(string customerCode, [FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
    {
        try
        {
            _logger.LogInformation("Request: Customer Statement for {CustomerCode}, Period: {From} to {To}", 
                customerCode, fromDate, toDate);
            
            var report = await _reportService.GetCustomerStatementAsync(customerCode, fromDate, toDate);
            
            return Ok(new ApiResponse<CustomerStatementReportDto>
            {
                Success = true,
                Message = $"Customer Statement for {customerCode}",
                Data = report
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Customer Statement");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    /// <summary>
    /// Get Vendor Statement Report
    /// All transactions for a vendor
    /// </summary>
    [HttpGet("vendor-statement/{vendorCode}")]
    [ProducesResponseType(typeof(ApiResponse<VendorStatementReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetVendorStatement(string vendorCode, [FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
    {
        try
        {
            _logger.LogInformation("Request: Vendor Statement for {VendorCode}, Period: {From} to {To}", 
                vendorCode, fromDate, toDate);
            
            var report = await _reportService.GetVendorStatementAsync(vendorCode, fromDate, toDate);
            
            return Ok(new ApiResponse<VendorStatementReportDto>
            {
                Success = true,
                Message = $"Vendor Statement for {vendorCode}",
                Data = report
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Vendor Statement");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    // ============================================================================
    // PHASE 4: TAX & COMPLIANCE
    // ============================================================================

    /// <summary>
    /// Get Tax Summary Report
    /// Tax aggregation by tax code
    /// </summary>
    [HttpGet("tax-summary")]
    [ProducesResponseType(typeof(ApiResponse<TaxSummaryReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTaxSummary([FromQuery] Guid ledgerId, [FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
    {
        try
        {
            _logger.LogInformation("Request: Tax Summary for Ledger {LedgerId}, Period: {From} to {To}", 
                ledgerId, fromDate, toDate);
            
            var report = await _reportService.GetTaxSummaryAsync(ledgerId, fromDate, toDate);
            
            return Ok(new ApiResponse<TaxSummaryReportDto>
            {
                Success = true,
                Message = "Tax Summary Report",
                Data = report
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Tax Summary");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    // ============================================================================
    // PHASE 5: ANALYSIS & AUDIT
    // ============================================================================

    /// <summary>
    /// Get Cash Flow Statement
    /// Operating, Investing, Financing activities
    /// </summary>
    [HttpGet("cash-flow")]
    [ProducesResponseType(typeof(ApiResponse<CashFlowStatementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetCashFlow([FromQuery] Guid ledgerId, [FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
    {
        try
        {
            _logger.LogInformation("Request: Cash Flow Statement for Ledger {LedgerId}, Period: {From} to {To}", 
                ledgerId, fromDate, toDate);
            
            var report = await _reportService.GetCashFlowAsync(ledgerId, fromDate, toDate);
            
            return Ok(new ApiResponse<CashFlowStatementDto>
            {
                Success = true,
                Message = "Cash Flow Statement",
                Data = report
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Cash Flow Statement");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    /// <summary>
    /// Get Unposted Journals Report
    /// All draft journal entries
    /// </summary>
    [HttpGet("unposted-journals")]
    [ProducesResponseType(typeof(ApiResponse<UnpostedJournalsReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUnpostedJournals([FromQuery] Guid ledgerId)
    {
        try
        {
            _logger.LogInformation("Request: Unposted Journals for Ledger {LedgerId}", ledgerId);
            
            var report = await _reportService.GetUnpostedJournalsAsync(ledgerId);
            
            return Ok(new ApiResponse<UnpostedJournalsReportDto>
            {
                Success = true,
                Message = $"Unposted Journals: {report.TotalCount} entries",
                Data = report
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Unposted Journals");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    /// <summary>
    /// Get Trial Balance by Dimension Report
    /// Department/Branch wise trial balance
    /// </summary>
    [HttpGet("trial-balance-by-dimension/{dimensionId}")]
    [ProducesResponseType(typeof(ApiResponse<TrialBalanceByDimensionReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetTrialBalanceByDimension(Guid dimensionId, [FromQuery] Guid ledgerId, [FromQuery] DateTime reportDate)
    {
        try
        {
            _logger.LogInformation("Request: Trial Balance by Dimension {DimensionId}, Date: {Date}", dimensionId, reportDate);
            
            var report = await _reportService.GetTrialBalanceByDimensionAsync(ledgerId, dimensionId, reportDate);
            
            return Ok(new ApiResponse<TrialBalanceByDimensionReportDto>
            {
                Success = true,
                Message = "Trial Balance by Dimension",
                Data = report
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Trial Balance by Dimension");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    /// <summary>
    /// Get P&L by Dimension Report
    /// Department/Branch wise profit and loss
    /// </summary>
    [HttpGet("profit-and-loss-by-dimension/{dimensionId}")]
    [ProducesResponseType(typeof(ApiResponse<ProfitAndLossByDimensionReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetProfitAndLossByDimension(Guid dimensionId, [FromQuery] Guid ledgerId, [FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
    {
        try
        {
            _logger.LogInformation("Request: P&L by Dimension {DimensionId}, Period: {From} to {To}", 
                dimensionId, fromDate, toDate);
            
            var report = await _reportService.GetProfitAndLossByDimensionAsync(ledgerId, dimensionId, fromDate, toDate);
            
            return Ok(new ApiResponse<ProfitAndLossByDimensionReportDto>
            {
                Success = true,
                Message = "P&L by Dimension",
                Data = report
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting P&L by Dimension");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    /// <summary>
    /// Get Reconciliation Report
    /// Bank vs Ledger reconciliation
    /// </summary>
    [HttpGet("reconciliation/{accountId}")]
    [ProducesResponseType(typeof(ApiResponse<ReconciliationReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetReconciliation(Guid accountId, [FromQuery] DateTime asOfDate)
    {
        try
        {
            _logger.LogInformation("Request: Reconciliation Report for Account {AccountId}, Date: {Date}", accountId, asOfDate);
            
            var report = await _reportService.GetReconciliationReportAsync(accountId, asOfDate);
            
            return Ok(new ApiResponse<ReconciliationReportDto>
            {
                Success = true,
                Message = $"Reconciliation Report. Reconciled: {report.IsReconciled}",
                Data = report
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Reconciliation Report");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }

    /// <summary>
    /// Get Daily Transaction Report
    /// All transactions for a specific date
    /// </summary>
    [HttpGet("daily-transactions")]
    [ProducesResponseType(typeof(ApiResponse<DailyTransactionReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetDailyTransactions([FromQuery] Guid ledgerId, [FromQuery] DateTime reportDate)
    {
        try
        {
            _logger.LogInformation("Request: Daily Transactions for Ledger {LedgerId}, Date: {Date}", ledgerId, reportDate);
            
            var report = await _reportService.GetDailyTransactionReportAsync(ledgerId, reportDate);
            
            return Ok(new ApiResponse<DailyTransactionReportDto>
            {
                Success = true,
                Message = $"Daily Transactions: {report.TransactionCount} entries",
                Data = report
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting Daily Transactions");
            return BadRequest(new ApiErrorResponse { Message = ex.Message });
        }
    }
}
