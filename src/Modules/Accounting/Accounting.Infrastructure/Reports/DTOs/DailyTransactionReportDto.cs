namespace Accounting.Infrastructure.Reports.DTOs;

/// <summary>
/// Daily Transaction Report
/// </summary>
public class DailyTransactionReportDto : FinancialReportDto
{
    public DateTime ReportDate { get; set; }
    public List<DailyTransactionLineDto> Transactions { get; set; } = new();
    public int TransactionCount { get; set; }
    public decimal TotalDebits { get; set; }
    public decimal TotalCredits { get; set; }
}
