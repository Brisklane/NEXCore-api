using Accounting.Infrastructure.Reports.DTOs;
using Accounting.Infrastructure.Reports.Enums;
using Accounting.Infrastructure.Repositories.Interfaces;
using Accounting.Domain.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Accounting.Infrastructure.Reports.Services;

/// <summary>
/// Core financial reporting service - Phase 1 (Trial Balance, GL, P&L, BS)
/// All reports derived from JournalEntry, JournalLine, and LedgerAccount
/// </summary>
public class FinancialReportService : IFinancialReportService
{
    private readonly IJournalEntryRepository _journalEntryRepository;
    private readonly IJournalLineRepository _journalLineRepository;
    private readonly ILedgerAccountRepository _accountRepository;
    private readonly IAccountBalanceRepository _balanceRepository;
    private readonly ITaxCodeRepository _taxCodeRepository;
    private readonly IDimensionRepository _dimensionRepository;
    private readonly ILogger<FinancialReportService> _logger;

    public FinancialReportService(
        IJournalEntryRepository journalEntryRepository,
        IJournalLineRepository journalLineRepository,
        ILedgerAccountRepository accountRepository,
        IAccountBalanceRepository balanceRepository,
        ITaxCodeRepository taxCodeRepository,
        IDimensionRepository dimensionRepository,
        ILogger<FinancialReportService> logger)
    {
        _journalEntryRepository = journalEntryRepository;
        _journalLineRepository = journalLineRepository;
        _accountRepository = accountRepository;
        _balanceRepository = balanceRepository;
        _taxCodeRepository = taxCodeRepository;
        _dimensionRepository = dimensionRepository;
        _logger = logger;
    }

    public async Task<TrialBalanceReportDto> GetTrialBalanceAsync(Guid ledgerId, DateTime reportDate)
    {
        var report = new TrialBalanceReportDto 
        { 
            ReportName = ReportName.TrialBalance.GetDisplayName(), 
            ReportDate = reportDate, 
            GeneratedAt = DateTime.UtcNow 
        };
        
        var accounts = await _accountRepository.GetByLedgerIdAsync(ledgerId);
        var entries = await _journalEntryRepository.GetByLedgerIdAsync(ledgerId);
        var postedEntries = entries
            .Where(e => e.PostingDate <= reportDate && e.Status == JournalEntryStatus.Posted)
            .ToList();

        foreach (var account in accounts.Where(a => !a.IsDeleted))
        {
            var lines = await _journalLineRepository.GetByAccountIdAsync(account.Id);
            var relevantLines = lines
                .Where(l => postedEntries.Any(e => e.Id == l.JournalEntryId))
                .ToList();
            
            decimal debit = relevantLines.Sum(l => l.DebitAmount);
            decimal credit = relevantLines.Sum(l => l.CreditAmount);
            
            if (debit > 0 || credit > 0)
            {
                report.Lines.Add(new TrialBalanceLineDto
                {
                    AccountId = account.Id,
                    AccountNumber = account.AccountNumber,
                    AccountName = account.AccountName,
                    AccountType = account.Category?.Type.ToString() ?? "Unknown",
                    DebitBalance = debit,
                    CreditBalance = credit
                });
                report.TotalDebits += debit;
                report.TotalCredits += credit;
            }
        }
        
        return report;
    }

    public async Task<GeneralLedgerReportDto> GetGeneralLedgerAsync(Guid ledgerId, DateTime fromDate, DateTime toDate)
    {
        var report = new GeneralLedgerReportDto 
        { 
            ReportName = ReportName.GeneralLedger.GetDisplayName(), 
            FromDate = fromDate, 
            ToDate = toDate, 
            GeneratedAt = DateTime.UtcNow 
        };
        
        var accounts = await _accountRepository.GetByLedgerIdAsync(ledgerId);
        var entries = await _journalEntryRepository.GetByLedgerIdAsync(ledgerId);
        
        foreach (var account in accounts.Where(a => !a.IsDeleted))
        {
            var accountReport = new GeneralLedgerAccountDto 
            { 
                AccountId = account.Id, 
                AccountNumber = account.AccountNumber, 
                AccountName = account.AccountName 
            };
            
            var lines = await _journalLineRepository.GetByAccountIdAsync(account.Id);
            var openingBalance = lines
                .Where(l => entries.Any(e => e.Id == l.JournalEntryId && e.PostingDate < fromDate && e.Status == JournalEntryStatus.Posted))
                .AsEnumerable()
                .Sum(l => l.DebitAmount - l.CreditAmount);
            
            accountReport.OpeningBalance = openingBalance;
            decimal runningBalance = openingBalance;
            
            var periodLines = lines
                .Where(l => entries.Any(e => e.Id == l.JournalEntryId && e.PostingDate >= fromDate && e.PostingDate <= toDate && e.Status == JournalEntryStatus.Posted))
                .OrderBy(l => entries.FirstOrDefault(e => e.Id == l.JournalEntryId)?.PostingDate)
                .ToList();
            
            foreach (var line in periodLines)
            {
                var entry = entries.FirstOrDefault(e => e.Id == line.JournalEntryId);
                if (entry == null) continue;
                
                runningBalance += line.DebitAmount - line.CreditAmount;
                accountReport.Transactions.Add(new GeneralLedgerTransactionDto
                {
                    TransactionDate = entry.PostingDate,
                    JournalNumber = entry.JournalNumber,
                    Description = entry.Description,
                    DocumentType = entry.DocumentType,
                    DebitAmount = line.DebitAmount,
                    CreditAmount = line.CreditAmount,
                    RunningBalance = runningBalance,
                    ReferenceNumber = entry.ReferenceNumber!
                });
            }
            
            accountReport.ClosingBalance = runningBalance;
            if (accountReport.Transactions.Any()) 
                report.Accounts.Add(accountReport);
        }
        
        return report;
    }

    public async Task<AccountLedgerReportDto> GetAccountLedgerAsync(Guid accountId, DateTime fromDate, DateTime toDate)
    {
        var account = await _accountRepository.GetByIdAsync(accountId);
        if (account == null) 
            throw new InvalidOperationException($"Account {accountId} not found");
        
        var report = new AccountLedgerReportDto 
        { 
            ReportName = ReportName.AccountLedger.GetDisplayName(), 
            AccountId = accountId, 
            AccountNumber = account.AccountNumber, 
            AccountName = account.AccountName, 
            FromDate = fromDate, 
            ToDate = toDate, 
            GeneratedAt = DateTime.UtcNow 
        };
        
        var lines = await _journalLineRepository.GetByAccountIdAsync(accountId);
        var entries = await _journalEntryRepository.GetByDateRangeAsync(fromDate.AddYears(-100), toDate);
        
        var openingLines = lines
            .Where(l => entries.Any(e => e.Id == l.JournalEntryId && e.PostingDate < fromDate && e.Status == JournalEntryStatus.Posted))
            .ToList();
        
        report.OpeningBalance = openingLines.AsEnumerable().Sum(l => l.DebitAmount - l.CreditAmount);
        decimal runningBalance = report.OpeningBalance;
        
        var periodLines = lines
            .Where(l => entries.Any(e => e.Id == l.JournalEntryId && e.PostingDate >= fromDate && e.PostingDate <= toDate && e.Status == JournalEntryStatus.Posted))
            .OrderBy(l => entries.FirstOrDefault(e => e.Id == l.JournalEntryId)?.PostingDate)
            .ToList();
        
        foreach (var line in periodLines)
        {
            var entry = entries.FirstOrDefault(e => e.Id == line.JournalEntryId);
            if (entry == null) continue;
            
            runningBalance += line.DebitAmount - line.CreditAmount;
            report.TotalDebits += line.DebitAmount;
            report.TotalCredits += line.CreditAmount;
            report.Transactions.Add(new AccountTransactionDto 
            { 
                TransactionDate = entry.PostingDate, 
                JournalNumber = entry.JournalNumber, 
                Description = entry.Description, 
                DebitAmount = line.DebitAmount, 
                CreditAmount = line.CreditAmount, 
                RunningBalance = runningBalance 
            });
        }
        
        report.ClosingBalance = runningBalance;
        return report;
    }

    public async Task<ProfitAndLossReportDto> GetProfitAndLossAsync(Guid ledgerId, DateTime fromDate, DateTime toDate)
    {
        var report = new ProfitAndLossReportDto 
        { 
            ReportName = ReportName.ProfitAndLoss.GetDisplayName(), 
            FromDate = fromDate, 
            ToDate = toDate, 
            GeneratedAt = DateTime.UtcNow 
        };
        
        var accounts = await _accountRepository.GetByLedgerIdAsync(ledgerId);
        var entries = await _journalEntryRepository.GetByDateRangeAsync(fromDate, toDate);
        var postedEntries = entries.Where(e => e.Status == JournalEntryStatus.Posted).ToList();
        
        foreach (var account in accounts.Where(a => !a.IsDeleted))
        {
            var accountType = account.Category?.Type.ToString() ?? "";
            
            // ? Use enum-based filtering instead of hardcoded strings
            bool isRevenueAccount = AccountTypeFilter.Revenue.Matches(accountType);
            bool isExpenseAccount = AccountTypeFilter.Expense.Matches(accountType);
            
            if (!isRevenueAccount && !isExpenseAccount) 
                continue;
            
            var lines = await _journalLineRepository.GetByAccountIdAsync(account.Id);
            var amount = lines
                .Where(l => postedEntries.Any(e => e.Id == l.JournalEntryId))
                .AsEnumerable()
                .Sum(l => l.DebitAmount - l.CreditAmount);
            
            if (amount == 0) 
                continue;
            
            var line = new ProfitAndLossLineDto 
            { 
                AccountId = account.Id, 
                AccountNumber = account.AccountNumber, 
                AccountName = account.AccountName, 
                Amount = Math.Abs(amount) 
            };
            
            if (isRevenueAccount) 
            { 
                report.TotalRevenue += Math.Abs(amount); 
                report.RevenueLines.Add(line); 
            }
            else if (isExpenseAccount) 
            { 
                report.TotalExpenses += Math.Abs(amount); 
                report.ExpenseLines.Add(line); 
            }
        }
        
        report.GrossProfit = report.TotalRevenue - report.TotalExpenses;
        report.OperatingProfit = report.GrossProfit;
        report.NetProfit = report.OperatingProfit;
        
        foreach (var line in report.RevenueLines.Concat(report.ExpenseLines))
            line.PercentageOfRevenue = report.TotalRevenue > 0 ? (line.Amount / report.TotalRevenue) * 100 : 0;
        
        return report;
    }

    public async Task<BalanceSheetReportDto> GetBalanceSheetAsync(Guid ledgerId, DateTime reportDate)
    {
        var report = new BalanceSheetReportDto 
        { 
            ReportName = ReportName.BalanceSheet.GetDisplayName(), 
            ReportDate = reportDate, 
            GeneratedAt = DateTime.UtcNow 
        };
        
        var accounts = await _accountRepository.GetByLedgerIdAsync(ledgerId);
        var entries = await _journalEntryRepository.GetByLedgerIdAsync(ledgerId);
        var postedEntries = entries
            .Where(e => e.PostingDate <= reportDate && e.Status == JournalEntryStatus.Posted)
            .ToList();
        
        foreach (var account in accounts.Where(a => !a.IsDeleted))
        {
            var accountType = account.Category?.Type.ToString() ?? "";
            
            // ? Use enum-based filtering instead of hardcoded strings
            bool isAssetAccount = AccountTypeFilter.Asset.Matches(accountType);
            bool isLiabilityAccount = AccountTypeFilter.Liability.Matches(accountType);
            bool isEquityAccount = AccountTypeFilter.Equity.Matches(accountType);
            
            if (!isAssetAccount && !isLiabilityAccount && !isEquityAccount) 
                continue;
            
            var lines = await _journalLineRepository.GetByAccountIdAsync(account.Id);
            var amount = lines
                .Where(l => postedEntries.Any(e => e.Id == l.JournalEntryId))
                .AsEnumerable()
                .Sum(l => l.DebitAmount - l.CreditAmount);
            
            if (Math.Abs(amount) < 0.01m) 
                continue;
            
            var item = new BalanceSheetItemDto 
            { 
                AccountId = account.Id, 
                AccountNumber = account.AccountNumber, 
                AccountName = account.AccountName, 
                Amount = Math.Abs(amount) 
            };
            
            if (isAssetAccount) 
            { 
                report.TotalAssets += Math.Abs(amount); 
                item.Classification = "Current"; 
                report.CurrentAssets += Math.Abs(amount); 
                report.AssetLines.Add(item); 
            }
            else if (isLiabilityAccount) 
            { 
                report.TotalLiabilities += Math.Abs(amount); 
                item.Classification = "Current"; 
                report.CurrentLiabilities += Math.Abs(amount); 
                report.LiabilityLines.Add(item); 
            }
            else if (isEquityAccount) 
            { 
                report.TotalEquity += Math.Abs(amount); 
                report.EquityLines.Add(item); 
            }
        }
        
        return report;
    }

    public async Task<JournalReportDto> GetJournalReportAsync(Guid ledgerId, DateTime fromDate, DateTime toDate)
    {
        var report = new JournalReportDto 
        { 
            ReportName = ReportName.Journal.GetDisplayName(), 
            FromDate = fromDate, 
            ToDate = toDate, 
            GeneratedAt = DateTime.UtcNow 
        };
        
        var entries = await _journalEntryRepository.GetByDateRangeAsync(fromDate, toDate);
        var ledgerEntries = entries.Where(e => e.LedgerId == ledgerId).ToList();
        
        foreach (var entry in ledgerEntries.OrderBy(e => e.PostingDate))
        {
            report.Entries.Add(new JournalReportLineDto
            {
                JournalEntryId = entry.Id, 
                JournalNumber = entry.JournalNumber, 
                PostingDate = entry.PostingDate, 
                DocumentDate = entry.DocumentDate,
                DocumentType = entry.DocumentType, 
                Status = entry.Status.ToString(), 
                TotalDebit = entry.TotalDebit, 
                TotalCredit = entry.TotalCredit,
                Description = entry.Description, 
                ReferenceNumber = entry.ReferenceNumber!, 
                CreatedBy = entry.CreatedByUserId.ToString()
            });
            report.TotalDebits += entry.TotalDebit;
            report.TotalCredits += entry.TotalCredit;
            report.EntryCount++;
        }
        
        return report;
    }

    public  Task<JournalAuditReportDto> GetJournalAuditReportAsync(Guid ledgerId, DateTime fromDate, DateTime toDate)
    {
        return Task.FromResult(new JournalAuditReportDto 
        { 
            ReportName = ReportName.JournalAudit.GetDisplayName(), 
            FromDate = fromDate, 
            ToDate = toDate, 
            GeneratedAt = DateTime.UtcNow 
        });
    }

    public  Task<AccountsReceivableAgingReportDto> GetAccountsReceivableAgingAsync(DateTime asOfDate)
    {
        return Task.FromResult(new AccountsReceivableAgingReportDto 
        { 
            ReportName = ReportName.AccountsReceivableAging.GetDisplayName(), 
            ReportDate = asOfDate, 
            GeneratedAt = DateTime.UtcNow 
        });
    }

    public  Task<AccountsPayableAgingReportDto> GetAccountsPayableAgingAsync(DateTime asOfDate)
    {
        return Task.FromResult(new AccountsPayableAgingReportDto 
        { 
            ReportName = ReportName.AccountsPayableAging.GetDisplayName(), 
            ReportDate = asOfDate, 
            GeneratedAt = DateTime.UtcNow 
        });
    }

    public  Task<CustomerStatementReportDto> GetCustomerStatementAsync(string customerCode, DateTime fromDate, DateTime toDate)
    {
        return Task.FromResult(new CustomerStatementReportDto 
        { 
            ReportName = ReportName.CustomerStatement.GetDisplayName(), 
            FromDate = fromDate, 
            ToDate = toDate, 
            GeneratedAt = DateTime.UtcNow 
        });
    }

    public  Task<VendorStatementReportDto> GetVendorStatementAsync(string vendorCode, DateTime fromDate, DateTime toDate)
    {
        return Task.FromResult(new VendorStatementReportDto 
        { 
            ReportName = ReportName.VendorStatement.GetDisplayName(), 
            FromDate = fromDate, 
            ToDate = toDate, 
            GeneratedAt = DateTime.UtcNow 
        });
    }

    public  Task<TaxSummaryReportDto> GetTaxSummaryAsync(Guid ledgerId, DateTime fromDate, DateTime toDate)
    {
        return Task.FromResult(new TaxSummaryReportDto 
        { 
            ReportName = ReportName.TaxSummary.GetDisplayName(), 
            FromDate = fromDate, 
            ToDate = toDate, 
            GeneratedAt = DateTime.UtcNow 
        });
    }

    public  Task<CashFlowStatementDto> GetCashFlowAsync(Guid ledgerId, DateTime fromDate, DateTime toDate)
    {
        return Task.FromResult(new CashFlowStatementDto 
        { 
            ReportName = ReportName.CashFlow.GetDisplayName(), 
            FromDate = fromDate, 
            ToDate = toDate, 
            GeneratedAt = DateTime.UtcNow 
        });
    }

    public async Task<UnpostedJournalsReportDto> GetUnpostedJournalsAsync(Guid ledgerId)
    {
        var report = new UnpostedJournalsReportDto 
        { 
            ReportName = ReportName.UnpostedJournals.GetDisplayName(), 
            ReportDate = DateTime.UtcNow, 
            GeneratedAt = DateTime.UtcNow 
        };
        
        var entries = await _journalEntryRepository.GetByStatusAsync(JournalEntryStatus.Draft);
        var draft = entries.Where(e => e.LedgerId == ledgerId).ToList();
        
        foreach (var entry in draft)
        {
            report.UnpostedEntries.Add(new UnpostedJournalDto 
            { 
                JournalEntryId = entry.Id, 
                JournalNumber = entry.JournalNumber, 
                CreatedDate = entry.CreatedAt, 
                DocumentType = entry.DocumentType, 
                TotalDebit = entry.TotalDebit, 
                TotalCredit = entry.TotalCredit, 
                CreatedBy = entry.CreatedByUserId.ToString(), 
                Status = entry.Status.ToString() 
            });
            report.TotalAmount += entry.TotalDebit;
            report.TotalCount++;
        }
        
        return report;
    }

    public  Task<TrialBalanceByDimensionReportDto> GetTrialBalanceByDimensionAsync(Guid ledgerId, Guid dimensionId, DateTime reportDate)
    {
        return Task.FromResult(new TrialBalanceByDimensionReportDto 
        { 
            ReportName = ReportName.TrialBalanceByDimension.GetDisplayName(), 
            ReportDate = reportDate, 
            GeneratedAt = DateTime.UtcNow 
        });
    }

    public  Task<ProfitAndLossByDimensionReportDto> GetProfitAndLossByDimensionAsync(Guid ledgerId, Guid dimensionId, DateTime fromDate, DateTime toDate)
    {
        return Task.FromResult(new ProfitAndLossByDimensionReportDto 
        { 
            ReportName = ReportName.ProfitAndLossByDimension.GetDisplayName(), 
            FromDate = fromDate, 
            ToDate = toDate, 
            GeneratedAt = DateTime.UtcNow 
        });
    }

    public  Task<ReconciliationReportDto> GetReconciliationReportAsync(Guid accountId, DateTime asOfDate)
    {
        return Task.FromResult(new ReconciliationReportDto 
        { 
            ReportName = ReportName.Reconciliation.GetDisplayName(), 
            AsOfDate = asOfDate, 
            GeneratedAt = DateTime.UtcNow 
        });
    }

    public  Task<DailyTransactionReportDto> GetDailyTransactionReportAsync(Guid ledgerId, DateTime reportDate)
    {
        return Task.FromResult(new DailyTransactionReportDto 
        { 
            ReportName = ReportName.DailyTransactions.GetDisplayName(), 
            ReportDate = reportDate, 
            GeneratedAt = DateTime.UtcNow 
        });
    }
}
