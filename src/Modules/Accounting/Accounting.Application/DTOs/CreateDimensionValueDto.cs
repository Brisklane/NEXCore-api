namespace Accounting.Application.DTOs;

/// <summary>
/// DTO for creating dimension value
/// </summary>
public class CreateDimensionValueDto
{
    public required Guid DimensionId { get; set; }
    public required string ValueCode { get; set; }
    public required string ValueName { get; set; }
    public string? Description { get; set; }
}
