namespace Accounting.Infrastructure.Reports.DTOs;

public class JournalReportLineDto
{
    public Guid JournalEntryId { get; set; }
    public string JournalNumber { get; set; } = string.Empty;
    public DateTime PostingDate { get; set; }
    public DateTime DocumentDate { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public string Description { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
}
