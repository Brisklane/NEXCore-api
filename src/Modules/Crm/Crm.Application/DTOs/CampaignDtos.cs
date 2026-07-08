namespace Crm.Application.DTOs;

public class CampaignDto
{
    public Guid Id { get; set; }
    public string CampaignName { get; set; } = string.Empty;
    public bool Active { get; set; }
    public string Status { get; set; } = "Planned";
    public string? Type { get; set; }
    public Guid? ParentCampaignId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? ExpectedRevenue { get; set; }
    public decimal? BudgetedCost { get; set; }
    public decimal? ActualCost { get; set; }
    public int? NumSent { get; set; }
    public decimal? ExpectedResponsePercent { get; set; }
    public Guid? OwnerId { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCampaignDto
{
    public string CampaignName { get; set; } = string.Empty;
    public bool Active { get; set; }
    public string Status { get; set; } = "Planned";
    public string? Type { get; set; }
    public Guid? ParentCampaignId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? ExpectedRevenue { get; set; }
    public decimal? BudgetedCost { get; set; }
    public decimal? ActualCost { get; set; }
    public int? NumSent { get; set; }
    public decimal? ExpectedResponsePercent { get; set; }
    public Guid? OwnerId { get; set; }
    public string? Description { get; set; }
}

public class UpdateCampaignDto : CreateCampaignDto { }

public class CampaignMemberDto
{
    public Guid Id { get; set; }
    public Guid CampaignId { get; set; }
    public Guid? LeadId { get; set; }
    public Guid? ContactId { get; set; }
    public string? Status { get; set; }
    public DateTime? FirstRespondedDate { get; set; }
}

public class CreateCampaignMemberDto
{
    public Guid? LeadId { get; set; }
    public Guid? ContactId { get; set; }
    public string? Status { get; set; }
}
