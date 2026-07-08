namespace Accounting.Application.DTOs;

/// <summary>
/// DTO for journal line
/// </summary>
public class JournalLineDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid JournalEntryId { get; set; }
    public Guid LedgerAccountId { get; set; }
    public string? Description { get; set; }
    public decimal DebitAmount { get; set; }
    public decimal CreditAmount { get; set; }
    public int LineNumber { get; set; }
}
