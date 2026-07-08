using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using Core.Application.DTOs.ValidateDtos;

namespace Core.Application.DTOs;

/// <summary>
/// Request DTO for updating branch details
/// </summary>
public class UpdateBranchRequestDto
{
    /// <summary>
    /// Branch ID (required for updates, null for new branches)
    /// </summary>
    public Guid? BranchId { get; set; }

    public string? Code { get; set; }
    [Required]
    public string Name { get; set; } = string.Empty;
    [Required]
    public string BranchType { get; set; } = string.Empty;
    [Required]
    public string PhoneNumber { get; set; } = string.Empty;
    [Required]
    public string Email { get; set; } = string.Empty;
    [Required]
    public string ManagerName { get; set; } = string.Empty;
    [Required]
    public byte[] BranchLogo { get; set; } = Array.Empty<byte>();
    [Required]
    public string StreetAddress { get; set; } = string.Empty;
    [Required]
    public string City { get; set; } = string.Empty;
    [Required]
    public string State { get; set; } = string.Empty;
    [Required]
    public string PostalCode { get; set; } = string.Empty;
    [Required]
    public decimal? Latitude { get; set; }
    [Required]
    public decimal? Longitude { get; set; }
    [Required]
    public bool IsActive { get; set; } = true;
    [Required]
    public ICollection<UpdateBusinessUnitRequestDto> BusinessUnits { get; set; } = new List<UpdateBusinessUnitRequestDto>();
}
