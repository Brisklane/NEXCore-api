namespace Accounting.Infrastructure.Reports.DTOs;

public class GeneralLedgerAccountDto
{
    public Guid AccountId { get; set; }
    public string AccountNumber { get; set; } = string.Empty;
    public string AccountName { get; set; } = string.Empty;
    public decimal OpeningBalance { get; set; }
    public List<GeneralLedgerTransactionDto> Transactions { get; set; } = new();
    public decimal ClosingBalance { get; set; }
}
