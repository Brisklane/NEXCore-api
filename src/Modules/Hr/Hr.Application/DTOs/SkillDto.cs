namespace Hr.Application.DTOs;

public class SkillDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid SkillCategoryId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? SkillTypeLookupValueId { get; set; }
    public bool IsActive { get; set; }
    public bool IsCoreSkill { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateSkillDto
{
    public Guid SkillCategoryId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? SkillTypeLookupValueId { get; set; }
    public bool IsCoreSkill { get; set; }
}

public class UpdateSkillDto
{
    public Guid? SkillCategoryId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Guid? SkillTypeLookupValueId { get; set; }
    public bool? IsActive { get; set; }
    public bool? IsCoreSkill { get; set; }
}
