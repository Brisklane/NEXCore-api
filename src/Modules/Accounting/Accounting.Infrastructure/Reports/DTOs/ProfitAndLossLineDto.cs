namespace Accounting.Infrastructure.Reports.DTOs;

public class ProfitAndLossLineDto
{
    public Guid AccountId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal PercentageOfRevenue { get; set; }
}
