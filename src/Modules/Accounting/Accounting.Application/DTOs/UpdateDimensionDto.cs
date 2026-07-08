namespace Accounting.Application.DTOs;

/// <summary>
/// DTO for updating dimension
/// </summary>
public class UpdateDimensionDto
{
    public string? Name { get; set; }
    public bool? IsActive { get; set; }
    public string? Description { get; set; }
}
