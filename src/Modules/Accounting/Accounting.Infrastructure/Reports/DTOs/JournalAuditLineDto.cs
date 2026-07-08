namespace Accounting.Infrastructure.Reports.DTOs;

public class JournalAuditLineDto
{
    public Guid JournalEntryId { get; set; }
    public string JournalNumber { get; set; } = string.Empty;
    public DateTime AuditDate { get; set; }
    public string AuditType { get; set; } = string.Empty; // Created, Modified, Posted, Reversed
    public string PerformedBy { get; set; } = string.Empty;
    public string ChangeDescription { get; set; } = string.Empty;
}
