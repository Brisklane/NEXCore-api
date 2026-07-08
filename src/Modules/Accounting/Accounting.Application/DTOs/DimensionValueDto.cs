namespace Accounting.Application.DTOs;

/// <summary>
/// DTO for dimension value
/// </summary>
public class DimensionValueDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid DimensionId { get; set; }
    public required string ValueCode { get; set; }
    public required string ValueName { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
}
