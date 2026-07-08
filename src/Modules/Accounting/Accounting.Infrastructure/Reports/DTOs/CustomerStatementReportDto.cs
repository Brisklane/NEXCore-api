namespace Accounting.Infrastructure.Reports.DTOs;

/// <summary>
/// Customer Statement Report
/// </summary>
public class CustomerStatementReportDto : FinancialReportDto
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public List<StatementTransactionDto> Transactions { get; set; } = new();
    public decimal ClosingBalance { get; set; }
}
