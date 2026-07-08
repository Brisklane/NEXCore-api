namespace Accounting.Infrastructure.Reports.DTOs;

public class DimensionTrialBalanceDto
{
    public string DimensionValue { get; set; } = string.Empty;
    public List<TrialBalanceLineDto> BalanceLines { get; set; } = new();
    public decimal TotalDebits { get; set; }
    public decimal TotalCredits { get; set; }
}
