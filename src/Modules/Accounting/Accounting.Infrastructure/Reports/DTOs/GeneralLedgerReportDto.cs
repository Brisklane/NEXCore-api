namespace Accounting.Infrastructure.Reports.DTOs;

/// <summary>
/// General Ledger Report - All transactions for all accounts
/// </summary>
public class GeneralLedgerReportDto : FinancialReportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<GeneralLedgerAccountDto> Accounts { get; set; } = new();
}
