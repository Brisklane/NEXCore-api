namespace Core.Application.DTOs;

public class BusinessUnitResponseDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public Guid BranchId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? UnitType { get; set; }
    public string? Description { get; set; }
    public string? ManagerName { get; set; }
    public string? ManagerEmail { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
