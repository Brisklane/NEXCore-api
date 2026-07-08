namespace Accounting.Application.DTOs;

/// <summary>
/// DTO for creating dimension
/// </summary>
public class CreateDimensionDto
{
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
}
