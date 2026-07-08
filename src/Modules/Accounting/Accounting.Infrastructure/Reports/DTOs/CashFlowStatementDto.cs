namespace Accounting.Infrastructure.Reports.DTOs;

/// <summary>
/// Cash Flow Statement
/// </summary>
public class CashFlowStatementDto : FinancialReportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    
    // Operating Activities
    public decimal OperatingCashFlow { get; set; }
    public List<CashFlowActivityDto> OperatingActivities { get; set; } = new();
    
    // Investing Activities
    public decimal InvestingCashFlow { get; set; }
    public List<CashFlowActivityDto> InvestingActivities { get; set; } = new();
    
    // Financing Activities
    public decimal FinancingCashFlow { get; set; }
    public List<CashFlowActivityDto> FinancingActivities { get; set; } = new();
    
    // Net Change
    public decimal NetCashFlow { get; set; }
    public decimal OpeningCashBalance { get; set; }
    public decimal ClosingCashBalance { get; set; }
}
