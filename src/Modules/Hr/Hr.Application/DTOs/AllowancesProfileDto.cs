namespace Hr.Application.DTOs;

public class AllowancesProfileDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public string AllowancesProfileCode { get; set; } = string.Empty;
    public string AllowancesProfileName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateAllowancesProfileDto
{
    public string AllowancesProfileCode { get; set; } = string.Empty;
    public string AllowancesProfileName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateAllowancesProfileDto
{
    public string? AllowancesProfileName { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}
