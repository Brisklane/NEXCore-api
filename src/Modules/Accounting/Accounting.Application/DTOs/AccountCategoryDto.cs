namespace Accounting.Application.DTOs;

/// <summary>
/// DTO for account category
/// </summary>
public class AccountCategoryDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public required string Name { get; set; }
    public required string Type { get; set; }
    public required string NormalBalance { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
}
