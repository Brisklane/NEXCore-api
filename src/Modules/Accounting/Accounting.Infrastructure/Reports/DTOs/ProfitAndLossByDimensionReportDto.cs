namespace Accounting.Infrastructure.Reports.DTOs;

/// <summary>
/// P&L by Dimension Report
/// </summary>
public class ProfitAndLossByDimensionReportDto : FinancialReportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public string DimensionName { get; set; } = string.Empty;
    public List<DimensionProfitAndLossDto> DimensionProfits { get; set; } = new();
}
