namespace Accounting.Application.DTOs;

/// <summary>
/// DTO for creating budget
/// </summary>
public class CreateBudgetDto
{
    public required Guid LedgerAccountId { get; set; }
    public required Guid FiscalPeriodId { get; set; }
    public required decimal Amount { get; set; }
    public required string BudgetType { get; set; }
    public decimal? VarianceThresholdPercentage { get; set; }
    public string? Description { get; set; }
}
