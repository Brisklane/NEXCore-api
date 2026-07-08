namespace Accounting.Infrastructure.Reports.DTOs;

/// <summary>
/// Accounts Payable Aging Report
/// </summary>
public class AccountsPayableAgingReportDto : FinancialReportDto
{
    public DateTime ReportDate { get; set; }
    public List<VendorAgingDto> Vendors { get; set; } = new();
    public decimal Total_0_30 { get; set; }
    public decimal Total_30_60 { get; set; }
    public decimal Total_60_90 { get; set; }
    public decimal Total_Over90 { get; set; }
    public decimal GrandTotal { get; set; }
}
