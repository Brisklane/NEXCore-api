using Crm.Domain.Enums;

namespace Crm.Application.DTOs;

public class KnowledgeArticleDto
{
    public Guid Id { get; set; }
    public string ArticleNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? UrlName { get; set; }
    public string? ArticleType { get; set; }
    public string? CategoryGroup { get; set; }
    public ArticleStatus Status { get; set; }
    public string? Summary { get; set; }
    public string Body { get; set; } = string.Empty;
    public bool IsVisibleInApp { get; set; }
    public bool IsVisibleInCsp { get; set; }
    public bool IsVisibleInPkb { get; set; }
    public DateTime? PublishedDate { get; set; }
    public int VersionNumber { get; set; }
    public Guid? OwnerId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateKnowledgeArticleDto
{
    public string Title { get; set; } = string.Empty;
    public string? UrlName { get; set; }
    public string? ArticleType { get; set; }
    public string? CategoryGroup { get; set; }
    public string? Summary { get; set; }
    public string Body { get; set; } = string.Empty;
    public bool IsVisibleInApp { get; set; } = true;
    public bool IsVisibleInCsp { get; set; }
    public bool IsVisibleInPkb { get; set; }
    public Guid? OwnerId { get; set; }
}

public class UpdateKnowledgeArticleDto : CreateKnowledgeArticleDto { }

public class EntitlementDto
{
    public Guid Id { get; set; }
    public string EntitlementName { get; set; } = string.Empty;
    public Guid AccountId { get; set; }
    public string? AccountName { get; set; }
    public Guid? ContactId { get; set; }
    public string? ServiceLevelName { get; set; }
    public string? Type { get; set; }
    public bool IsPerIncident { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? CasesPerEntitlement { get; set; }
    public int CasesUsed { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateEntitlementDto
{
    public string EntitlementName { get; set; } = string.Empty;
    public Guid AccountId { get; set; }
    public Guid? ContactId { get; set; }
    public string? ServiceLevelName { get; set; }
    public string? Type { get; set; }
    public bool IsPerIncident { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? CasesPerEntitlement { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateEntitlementDto : CreateEntitlementDto { }
