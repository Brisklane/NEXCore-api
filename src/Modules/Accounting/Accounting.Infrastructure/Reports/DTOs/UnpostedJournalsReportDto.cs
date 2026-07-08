namespace Accounting.Infrastructure.Reports.DTOs;

/// <summary>
/// Unposted Journals Report - Draft entries
/// </summary>
public class UnpostedJournalsReportDto : FinancialReportDto
{
    public DateTime ReportDate { get; set; }
    public List<UnpostedJournalDto> UnpostedEntries { get; set; } = new();
    public int TotalCount { get; set; }
    public decimal TotalAmount { get; set; }
}
