using System.ComponentModel.DataAnnotations;

namespace Core.Application.DTOs.ValidateDtos;

public class CreateBranchDto
{
    /// <summary>Branch id — populated on GET responses so the client can update existing branches; null when creating.</summary>
    public Guid? BranchId { get; set; }
    public string? Code { get; set; }
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public string BranchType { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string ManagerName { get; set; } = string.Empty;
    public byte[] BranchLogo { get; set; } = Array.Empty<byte>();
    public string StreetAddress { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    [Required]
    public bool IsActive { get; set; } = true;
    [Required]
    public ICollection<CreateBusinessUnitDto> BusinessUnits { get; set; } = new List<CreateBusinessUnitDto>();
}
