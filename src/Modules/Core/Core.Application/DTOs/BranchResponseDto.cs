namespace Core.Application.DTOs;

public class BranchResponseDto
{
    public Guid Id { get; set; }
    public Guid CompanyId { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public string? BranchType { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? ManagerName { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public byte[]? BranchLogo { get; set; }
    public required string StreetAddress { get; set; }
    public required string City { get; set; }
    public required string State { get; set; }
    public required string PostalCode { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
