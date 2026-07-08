namespace Accounting.Infrastructure.Reports.DTOs;

/// <summary>
/// Balance Sheet - Financial position at a date
/// </summary>
public class BalanceSheetReportDto : FinancialReportDto
{
    public DateTime ReportDate { get; set; }
    
    // Assets
    public decimal CurrentAssets { get; set; }
    public decimal NonCurrentAssets { get; set; }
    public decimal TotalAssets { get; set; }
    public List<BalanceSheetItemDto> AssetLines { get; set; } = new();
    
    // Liabilities
    public decimal CurrentLiabilities { get; set; }
    public decimal NonCurrentLiabilities { get; set; }
    public decimal TotalLiabilities { get; set; }
    public List<BalanceSheetItemDto> LiabilityLines { get; set; } = new();
    
    // Equity
    public decimal TotalEquity { get; set; }
    public List<BalanceSheetItemDto> EquityLines { get; set; } = new();
    
    // Verification
    public bool IsBalanced => Math.Abs(TotalAssets - (TotalLiabilities + TotalEquity)) < 0.01m;
}
