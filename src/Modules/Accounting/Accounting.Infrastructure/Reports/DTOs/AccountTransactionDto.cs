namespace Accounting.Infrastructure.Reports.DTOs;

public class AccountTransactionDto
{
    public DateTime TransactionDate { get; set; }
    public string JournalNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal RunningBalance { get; set; }
}
