namespace Accounting.Application.DTOs;

/// <summary>
/// DTO for journal entry
/// </summary>
public class JournalEntryDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public required string JournalNumber { get; set; }
    public string? ReferenceNumber { get; set; }
    public required string DocumentType { get; set; }
    public DateTime PostingDate { get; set; }
    public DateTime DocumentDate { get; set; }
    public required string Description { get; set; }
    public required string CurrencyCode { get; set; }
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
    public required string Status { get; set; }
    public List<JournalLineDto> Lines { get; set; } = [];
}
