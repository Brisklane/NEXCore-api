namespace Accounting.Infrastructure.Reports.DTOs;

/// <summary>
/// Reconciliation Report - Bank vs Ledger
/// </summary>
public class ReconciliationReportDto : FinancialReportDto
{
    public DateTime AsOfDate { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public decimal LedgerBalance { get; set; }
    public decimal BankBalance { get; set; }
    public List<OutstandingItemDto> OutstandingItems { get; set; } = new();
    public decimal ReconciledBalance { get; set; }
    public bool IsReconciled => Math.Abs(LedgerBalance - ReconciledBalance) < 0.01m;
}
