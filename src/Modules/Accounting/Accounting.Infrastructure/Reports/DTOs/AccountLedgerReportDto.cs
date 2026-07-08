namespace Accounting.Infrastructure.Reports.DTOs;

/// <summary>
/// Account Ledger Report - Single account statement
/// </summary>
public class AccountLedgerReportDto : FinancialReportDto
{
    public Guid AccountId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public List<AccountTransactionDto> Transactions { get; set; } = new();
    public decimal ClosingBalance { get; set; }
    public decimal TotalDebits { get; set; }
    public decimal TotalCredits { get; set; }
}
