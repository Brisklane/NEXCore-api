namespace Accounting.Infrastructure.Reports.DTOs;

public class UnpostedJournalDto
{
    public Guid JournalEntryId { get; set; }
    public string JournalNumber { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}
