namespace Crm.Application.DTOs;

public class TagDto
{
    public Guid Id { get; set; }
    public string TagName { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Description { get; set; }
}

public class CreateTagDto
{
    public string TagName { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Description { get; set; }
}

public class UpdateTagDto : CreateTagDto { }

public class EntityTagDto
{
    public Guid Id { get; set; }
    public Guid TagId { get; set; }
    public string? TagName { get; set; }
    public Guid EntityId { get; set; }
    public string EntityType { get; set; } = string.Empty;
}

public class AssignTagDto
{
    public Guid TagId { get; set; }
    public Guid EntityId { get; set; }
    public string EntityType { get; set; } = string.Empty;
}

public class EmailMessageDto
{
    public Guid Id { get; set; }
    public string Subject { get; set; } = string.Empty;
    public string? HtmlBody { get; set; }
    public string? TextBody { get; set; }
    public string? FromAddress { get; set; }
    public string? FromName { get; set; }
    public string? ToAddress { get; set; }
    public string? CcAddress { get; set; }
    public bool Incoming { get; set; }
    public DateTime? MessageDate { get; set; }
    public bool IsRead { get; set; }
    public bool IsTracked { get; set; }
    public int OpenCount { get; set; }
    public int ClickCount { get; set; }
    public Guid? RelatedToId { get; set; }
    public string? RelatedToType { get; set; }
    public Guid? SentByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateEmailMessageDto
{
    public string Subject { get; set; } = string.Empty;
    public string? HtmlBody { get; set; }
    public string? TextBody { get; set; }
    public string? FromAddress { get; set; }
    public string? FromName { get; set; }
    public string? ToAddress { get; set; }
    public string? CcAddress { get; set; }
    public string? BccAddress { get; set; }
    public bool Incoming { get; set; }
    public DateTime? MessageDate { get; set; }
    public bool IsTracked { get; set; }
    public Guid? RelatedToId { get; set; }
    public string? RelatedToType { get; set; }
    public string? MessageId { get; set; }
    public string? InReplyToId { get; set; }
    public string? ThreadIdentifier { get; set; }
}
