namespace Accounting.Infrastructure.Reports.DTOs;

public class BalanceSheetItemDto
{
    public Guid AccountId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public string Classification { get; set; } = string.Empty; // Current/Non-Current
    public decimal Amount { get; set; }
}
