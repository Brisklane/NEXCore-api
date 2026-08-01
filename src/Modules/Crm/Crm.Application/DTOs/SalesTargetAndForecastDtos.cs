namespace Crm.Application.DTOs;

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
