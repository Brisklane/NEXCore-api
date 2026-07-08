namespace Crm.Application.DTOs;

public class CaseDto
{
    public Guid Id { get; set; }
    public string CaseNumber { get; set; } = string.Empty;
    public string Status { get; set; } = "New";
    public string? CaseOrigin { get; set; }
    public string Priority { get; set; } = "Medium";
    public Guid? ContactId { get; set; }
    public string? ContactName { get; set; }
    public Guid? AccountId { get; set; }
    public string? AccountName { get; set; }
    public Guid? EntitlementId { get; set; }
    public string? Subject { get; set; }
    public string? Description { get; set; }
    public bool SendNotificationEmail { get; set; }
    public Guid? OwnerId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCaseDto
{
    public string? CaseOrigin { get; set; }
    public string Priority { get; set; } = "Medium";
    public Guid? ContactId { get; set; }
    public Guid? AccountId { get; set; }
    public Guid? EntitlementId { get; set; }
    public string? Subject { get; set; }
    public string? Description { get; set; }
    public bool SendNotificationEmail { get; set; }
    public Guid? OwnerId { get; set; }
}

public class UpdateCaseDto
{
    public string Status { get; set; } = "New";
    public string? CaseOrigin { get; set; }
    public string Priority { get; set; } = "Medium";
    public Guid? ContactId { get; set; }
    public Guid? AccountId { get; set; }
    public Guid? EntitlementId { get; set; }
    public string? Subject { get; set; }
    public string? Description { get; set; }
    public bool SendNotificationEmail { get; set; }
    public Guid? OwnerId { get; set; }
}

public class CaseCommentDto
{
    public Guid Id { get; set; }
    public Guid CaseId { get; set; }
    public string CommentBody { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public bool IsInternal { get; set; }
    public Guid? AuthorId { get; set; }
    public bool IsCustomerComment { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCaseCommentDto
{
    public string CommentBody { get; set; } = string.Empty;
    public bool IsPublished { get; set; }
    public bool IsInternal { get; set; }
}
