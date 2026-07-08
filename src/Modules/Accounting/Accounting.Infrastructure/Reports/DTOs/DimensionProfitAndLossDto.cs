namespace Accounting.Infrastructure.Reports.DTOs;

public class DimensionProfitAndLossDto
{
    public string DimensionValue { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetProfit { get; set; }
    public decimal ProfitMargin { get; set; }
}
