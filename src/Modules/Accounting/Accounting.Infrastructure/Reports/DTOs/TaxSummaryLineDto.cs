namespace Accounting.Infrastructure.Reports.DTOs;

public class TaxSummaryLineDto
{
    public string TaxCode { get; set; } = string.Empty;
    public string TaxName { get; set; } = string.Empty;
    public decimal Percentage { get; set; }
    public decimal TaxableAmount { get; set; }
    public decimal TaxAmount { get; set; }
}
