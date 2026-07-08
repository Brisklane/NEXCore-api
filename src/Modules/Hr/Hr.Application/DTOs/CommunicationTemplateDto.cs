namespace Hr.Application.DTOs;

public class CommunicationTemplateDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string TemplateCode { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public Guid TemplateTypeLookupValueId { get; set; }
    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? PlaceholdersJson { get; set; }
    public string? LanguageCode { get; set; }
    public int Version { get; set; }
    public bool IsActive { get; set; }
    public bool IsSystemTemplate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateCommunicationTemplateDto
{
    public string TemplateCode { get; set; } = string.Empty;
    public string TemplateName { get; set; } = string.Empty;
    public Guid TemplateTypeLookupValueId { get; set; }
    public string? Subject { get; set; }
    public string Body { get; set; } = string.Empty;
    public string? PlaceholdersJson { get; set; }
    public string? LanguageCode { get; set; }
}

public class UpdateCommunicationTemplateDto
{
    public string? TemplateName { get; set; }
    public Guid? TemplateTypeLookupValueId { get; set; }
    public string? Subject { get; set; }
    public string? Body { get; set; }
    public string? PlaceholdersJson { get; set; }
    public string? LanguageCode { get; set; }
    public bool? IsActive { get; set; }
}
