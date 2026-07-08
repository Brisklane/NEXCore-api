namespace Accounting.Infrastructure.Reports.DTOs;

/// <summary>
/// Journal Audit Report - Who changed what
/// </summary>
public class JournalAuditReportDto : FinancialReportDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<JournalAuditLineDto> AuditRecords { get; set; } = new();
}
