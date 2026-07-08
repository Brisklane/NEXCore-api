namespace Hr.Application.DTOs;

public class LookupTypeDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ModuleName { get; set; }
    public string? EntityName { get; set; }
    public bool IsActive { get; set; }
    public bool IsSystem { get; set; }
    public int SortOrder { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class LookupValueDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid LookupTypeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public bool IsTerminal { get; set; }
    public Guid? ParentLookupValueId { get; set; }
    public DateTime? EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string? MetadataJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateLookupTypeDto
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ModuleName { get; set; }
    public string? EntityName { get; set; }
    public int SortOrder { get; set; }
}

public class CreateLookupValueDto
{
    public Guid LookupTypeId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}
