namespace Accounting.Infrastructure.Reports.DTOs;

/// <summary>
/// Trial Balance Report - Foundation of all accounting
/// </summary>
public class TrialBalanceReportDto : FinancialReportDto
{
    public DateTime ReportDate { get; set; }
    public List<TrialBalanceLineDto> Lines { get; set; } = new();
    public decimal TotalDebits { get; set; }
    public decimal TotalCredits { get; set; }
    public bool IsBalanced => Math.Abs(TotalDebits - TotalCredits) < 0.01m;
}
