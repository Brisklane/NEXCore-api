namespace Accounting.Infrastructure.Reports.DTOs;

public class GeneralLedgerTransactionDto
{
    public DateTime TransactionDate { get; set; }
    public string JournalNumber { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public decimal RunningBalance { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
}
