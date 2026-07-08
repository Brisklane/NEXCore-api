namespace Accounting.Infrastructure.Reports.DTOs;

/// <summary>
/// Vendor Statement Report
/// </summary>
public class VendorStatementReportDto : FinancialReportDto
{
    public string VendorName { get; set; } = string.Empty;
    public string VendorCode { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public List<StatementTransactionDto> Transactions { get; set; } = new();
    public decimal ClosingBalance { get; set; }
}
