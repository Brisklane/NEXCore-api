namespace Crm.Application.DTOs;

public class DealDto
{
    public Guid Id { get; set; }
    public string OpportunityName { get; set; } = string.Empty;
    public Guid AccountId { get; set; }
    public string? AccountName { get; set; }
    public DateTime CloseDate { get; set; }
    public decimal? Amount { get; set; }
    public string Stage { get; set; } = string.Empty;
    public string? ForecastCategory { get; set; }
    public Guid? OwnerId { get; set; }
    public Guid? PipelineId { get; set; }
    public Guid? PipelineStageId { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateDealDto
{
    public string OpportunityName { get; set; } = string.Empty;
    public Guid AccountId { get; set; }
    public DateTime CloseDate { get; set; }
    public decimal? Amount { get; set; }
    public string Stage { get; set; } = "--None--";
    public string? ForecastCategory { get; set; }
    public Guid? OwnerId { get; set; }
    public Guid? PipelineId { get; set; }
    public Guid? PipelineStageId { get; set; }
    public string? Description { get; set; }
}

public class UpdateDealDto : CreateDealDto { }
