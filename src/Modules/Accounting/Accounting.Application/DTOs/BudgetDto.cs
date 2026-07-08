namespace Accounting.Application.DTOs;

/// <summary>
/// DTO for budget
/// </summary>
public class BudgetDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid LedgerAccountId { get; set; }
    public Guid FiscalPeriodId { get; set; }
    public decimal Amount { get; set; }
    public required string BudgetType { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public decimal? VarianceThresholdPercentage { get; set; }
    public bool IsLocked { get; set; }
}
