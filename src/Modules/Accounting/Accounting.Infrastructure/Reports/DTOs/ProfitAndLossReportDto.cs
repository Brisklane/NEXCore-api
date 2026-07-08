namespace Accounting.Infrastructure.Reports.DTOs;

/// <summary>
/// Profit & Loss Statement (Income Statement)
/// </summary>
public class ProfitAndLossReportDto : FinancialReportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    
    // Revenue
    public decimal TotalRevenue { get; set; }
    public List<ProfitAndLossLineDto> RevenueLines { get; set; } = new();
    
    // Expenses
    public decimal TotalExpenses { get; set; }
    public List<ProfitAndLossLineDto> ExpenseLines { get; set; } = new();
    
    // Results
    public decimal GrossProfit { get; set; }
    public decimal OperatingProfit { get; set; }
    public decimal NetProfit { get; set; }
}
