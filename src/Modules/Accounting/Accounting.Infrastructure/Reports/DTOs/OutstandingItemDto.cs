namespace Accounting.Infrastructure.Reports.DTOs;

public class OutstandingItemDto
{
    public DateTime ItemDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string ItemType { get; set; } = string.Empty; // Outstanding Check, Deposit in Transit
}
