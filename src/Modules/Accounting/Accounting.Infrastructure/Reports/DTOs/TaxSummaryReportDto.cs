namespace Accounting.Infrastructure.Reports.DTOs;

/// <summary>
/// Tax Summary Report
/// </summary>
public class TaxSummaryReportDto : FinancialReportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<TaxSummaryLineDto> TaxLines { get; set; } = new();
    public decimal TotalTaxableAmount { get; set; }
    public decimal TotalTaxAmount { get; set; }
}
