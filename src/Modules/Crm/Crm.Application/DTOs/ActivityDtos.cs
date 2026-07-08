namespace Crm.Application.DTOs;

public class ActivityDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = "Task";
    public string Subject { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public DateTime? StartDateTime { get; set; }
    public DateTime? EndDateTime { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public int? DurationMinutes { get; set; }
    public string? CallType { get; set; }
    public string? CallPurpose { get; set; }
    public string? CallResult { get; set; }
    public string? Description { get; set; }
    public string? Comments { get; set; }
    public Guid? RelatedToId { get; set; }
    public string? RelatedToType { get; set; }
    public Guid? NameId { get; set; }
    public string? NameType { get; set; }
    public Guid? AssignedToId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateActivityDto
{
    public string Type { get; set; } = "Task";
    public string Subject { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public DateTime? StartDateTime { get; set; }
    public DateTime? EndDateTime { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public int? DurationMinutes { get; set; }
    public string? CallType { get; set; }
    public string? CallPurpose { get; set; }
    public string? CallResult { get; set; }
    public string? Description { get; set; }
    public string? Comments { get; set; }
    public Guid? RelatedToId { get; set; }
    public string? RelatedToType { get; set; }
    public Guid? NameId { get; set; }
    public string? NameType { get; set; }
    public Guid? AssignedToId { get; set; }
}

public class UpdateActivityDto : CreateActivityDto { }
