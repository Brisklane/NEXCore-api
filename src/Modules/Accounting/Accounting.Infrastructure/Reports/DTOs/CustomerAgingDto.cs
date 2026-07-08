namespace Accounting.Infrastructure.Reports.DTOs;

public class CustomerAgingDto
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerCode { get; set; } = string.Empty;
    public decimal Amount_0_30 { get; set; }
    public decimal Amount_30_60 { get; set; }
    public decimal Amount_60_90 { get; set; }
    public decimal Amount_Over90 { get; set; }
    public decimal TotalAmount { get; set; }
}
