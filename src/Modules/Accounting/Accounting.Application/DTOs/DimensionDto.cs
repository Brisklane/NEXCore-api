namespace Accounting.Application.DTOs;

/// <summary>
/// DTO for dimension
/// </summary>
public class DimensionDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
}
