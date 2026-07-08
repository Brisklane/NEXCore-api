using System.ComponentModel.DataAnnotations;

namespace Core.Application.DTOs;

/// <summary>
/// Request DTO for updating business unit details
/// </summary>
public class UpdateBusinessUnitRequestDto
{
    /// <summary>
    /// Business unit ID (required for updates, null for new units)
    /// </summary>
    public Guid? BusinessUnitId { get; set; }

    public string? Code { get; set; } 
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public string UnitType { get; set; } = string.Empty;
    [Required]
    public string Description { get; set; } = string.Empty;
    [Required]
    public string ManagerName { get; set; } = string.Empty;
    [Required]
    public string ManagerEmail { get; set; } = string.Empty;
    [Required]
    public bool IsActive { get; set; } = true;
}
