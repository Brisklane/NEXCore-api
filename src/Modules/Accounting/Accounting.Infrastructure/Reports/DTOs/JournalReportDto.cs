namespace Accounting.Infrastructure.Reports.DTOs;

/// <summary>
/// Journal Report - List of all journal entries
/// </summary>
public class JournalReportDto : FinancialReportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<JournalReportLineDto> Entries { get; set; } = new();
    public decimal TotalDebits { get; set; }
    public decimal TotalCredits { get; set; }
    public int EntryCount { get; set; }
}
