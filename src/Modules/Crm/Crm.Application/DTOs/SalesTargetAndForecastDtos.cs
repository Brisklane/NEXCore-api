namespace Crm.Application.DTOs;

public class SalesTargetDto
{
    public Guid Id { get; set; }
    public Guid? UserId { get; set; }
    public Guid? TeamId { get; set; }
    public Guid? TerritoryId { get; set; }
    public int FiscalYear { get; set; }
    public int? FiscalQuarter { get; set; }
    public int? FiscalMonth { get; set; }
    public DateTime PeriodStartDate { get; set; }
    public DateTime PeriodEndDate { get; set; }
    public decimal TargetAmount { get; set; }
    public int? TargetDealsCount { get; set; }
    public decimal? ActualAmount { get; set; }
    public int? ActualDealsCount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateSalesTargetDto
{
    public Guid? UserId { get; set; }
    public Guid? TeamId { get; set; }
    public Guid? TerritoryId { get; set; }
    public int FiscalYear { get; set; }
    public int? FiscalQuarter { get; set; }
    public int? FiscalMonth { get; set; }
    public DateTime PeriodStartDate { get; set; }
    public DateTime PeriodEndDate { get; set; }
    public decimal TargetAmount { get; set; }
    public int? TargetDealsCount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public string? Description { get; set; }
}

public class UpdateSalesTargetDto : CreateSalesTargetDto { }

public class ForecastDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public int FiscalYear { get; set; }
    public int FiscalQuarter { get; set; }
    public int? FiscalMonth { get; set; }
    public DateTime PeriodStartDate { get; set; }
    public DateTime PeriodEndDate { get; set; }
    public decimal PipelineAmount { get; set; }
    public decimal BestCaseAmount { get; set; }
    public decimal CommitAmount { get; set; }
    public decimal ClosedAmount { get; set; }
    public decimal? AdjustedAmount { get; set; }
    public decimal? QuotaAmount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    public bool IsManagerAdjusted { get; set; }
    public bool IsSubmitted { get; set; }
    public DateTime? SubmittedDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateForecastDto
{
    public Guid UserId { get; set; }
    public int FiscalYear { get; set; }
    public int FiscalQuarter { get; set; }
    public int? FiscalMonth { get; set; }
    public DateTime PeriodStartDate { get; set; }
    public DateTime PeriodEndDate { get; set; }
    public decimal PipelineAmount { get; set; }
    public decimal BestCaseAmount { get; set; }
    public decimal CommitAmount { get; set; }
    public decimal ClosedAmount { get; set; }
    public decimal? AdjustedAmount { get; set; }
    public decimal? QuotaAmount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
}

public class UpdateForecastDto : CreateForecastDto { }
