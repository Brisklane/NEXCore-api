using System.ComponentModel.DataAnnotations;

namespace Core.Application.DTOs.ValidateDtos;

public class CreateBusinessUnitDto
{
    /// <summary>Business unit id — populated on GET responses so the client can update existing units; null when creating.</summary>
    public Guid? BusinessUnitId { get; set; }
    public string? Code { get; set; } = string.Empty;
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public string UnitType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ManagerName { get; set; } = string.Empty;
    public string ManagerEmail { get; set; } = string.Empty;
    [Required]
    public bool IsActive { get; set; } = true;
}
