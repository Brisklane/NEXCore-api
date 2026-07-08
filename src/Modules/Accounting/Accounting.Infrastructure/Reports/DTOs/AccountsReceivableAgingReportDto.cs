namespace Accounting.Infrastructure.Reports.DTOs;

/// <summary>
/// Accounts Receivable Aging Report
/// </summary>
public class AccountsReceivableAgingReportDto : FinancialReportDto
{
    public DateTime ReportDate { get; set; }
    public List<CustomerAgingDto> Customers { get; set; } = new();
    public decimal Total_0_30 { get; set; }
    public decimal Total_30_60 { get; set; }
    public decimal Total_60_90 { get; set; }
    public decimal Total_Over90 { get; set; }
    public decimal GrandTotal { get; set; }
}
