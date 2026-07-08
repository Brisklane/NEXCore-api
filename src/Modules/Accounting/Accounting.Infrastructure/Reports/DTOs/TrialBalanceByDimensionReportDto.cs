namespace Accounting.Infrastructure.Reports.DTOs;

/// <summary>
/// Trial Balance by Dimension Report
/// </summary>
public class TrialBalanceByDimensionReportDto : FinancialReportDto
{
    public DateTime ReportDate { get; set; }
    public string DimensionName { get; set; } = string.Empty;
    public List<DimensionTrialBalanceDto> DimensionBalances { get; set; } = new();
}
