namespace Accounting.Application.DTOs;

/// <summary>
/// DTO for updating dimension value
/// </summary>
public class UpdateDimensionValueDto
{
    public string? ValueName { get; set; }
    public bool? IsActive { get; set; }
    public string? Description { get; set; }
}
