namespace Accounting.Application.DTOs;

/// <summary>
/// DTO for updating budget
/// </summary>
public class UpdateBudgetDto
{
    public decimal? Amount { get; set; }
    public decimal? VarianceThresholdPercentage { get; set; }
    public bool? IsActive { get; set; }
    public string? Description { get; set; }
}
