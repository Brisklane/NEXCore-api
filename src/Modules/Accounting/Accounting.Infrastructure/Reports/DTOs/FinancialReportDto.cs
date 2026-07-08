namespace Accounting.Infrastructure.Reports.DTOs;

/// <summary>
/// Base DTO for all financial reports
/// </summary>
public abstract class FinancialReportDto
{
    public string ReportName { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public Guid? BusinessUnitId { get; set; }
}
