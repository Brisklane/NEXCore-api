namespace Accounting.Application.DTOs;

/// <summary>
/// Response DTO for account balance report
/// </summary>
public class AccountBalanceReportDto
{
    public Guid AccountId { get; set; }
    public required string AccountNumber { get; set; }
    public required string AccountName { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal Debits { get; set; }
    public decimal Credits { get; set; }
    public decimal ClosingBalance { get; set; }
}
